using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Opure.Configuration.Contracts;
using Opure.TrustEvidence.Contracts;

namespace Opure.Configuration;

/// <summary>
/// A proposed change to one setting value in a profile.
/// If ValueJson is null, the setting is removed from the profile.
/// </summary>
public sealed record ProfileProposedChange(string SettingId, string? ValueJson);

/// <summary>
/// Projection model for a setting within a profile editor UI.
/// </summary>
public sealed record ProfileEditorItem(
    string SettingId,
    string DisplayName,
    string Description,
    string? ConfiguredValueJson,
    string? DefaultValueJson,
    SettingValueTypeDefinition ValueType,
    string Category,
    string Editor,
    int Order,
    bool Advanced);

/// <summary>
/// Orchestrates reads, updates, and UI projections for configuration profiles.
/// Enforces transactional safety, setting validation, and immutable revision history.
/// </summary>
public sealed class ConfigurationService
{
    private readonly ConfigurationDatabase database;
    private readonly SettingDefinitionCatalogue settingCatalogue;
    private readonly ProductDefaultsCatalogue productDefaults;
    private readonly PolicyDefinitionCatalogue policyCatalogue;
    private readonly ITrustEvidenceOwnerIngestionPort evidencePort;
    private readonly IProductPolicyEvaluationPort policyEvaluationPort;

    public ConfigurationService(
        ConfigurationDatabase database,
        SettingDefinitionCatalogue settingCatalogue,
        ProductDefaultsCatalogue productDefaults,
        PolicyDefinitionCatalogue policyCatalogue,
        ITrustEvidenceOwnerIngestionPort evidencePort)
        : this(
            database,
            settingCatalogue,
            productDefaults,
            policyCatalogue,
            evidencePort,
            DeterministicProductPolicyEvaluationPort.Instance)
    {
    }

    public ConfigurationService(
        ConfigurationDatabase database,
        SettingDefinitionCatalogue settingCatalogue,
        ProductDefaultsCatalogue productDefaults,
        PolicyDefinitionCatalogue policyCatalogue,
        ITrustEvidenceOwnerIngestionPort evidencePort,
        IProductPolicyEvaluationPort policyEvaluationPort)
    {
        this.database = database ?? throw new ArgumentNullException(nameof(database));
        this.settingCatalogue = settingCatalogue ?? throw new ArgumentNullException(nameof(settingCatalogue));
        this.productDefaults = productDefaults ?? throw new ArgumentNullException(nameof(productDefaults));
        this.policyCatalogue = policyCatalogue ?? throw new ArgumentNullException(nameof(policyCatalogue));
        this.evidencePort = evidencePort ?? throw new ArgumentNullException(nameof(evidencePort));
        this.policyEvaluationPort = policyEvaluationPort ?? throw new ArgumentNullException(nameof(policyEvaluationPort));
    }

    /// <summary>
    /// Gets the latest revision of a profile.
    /// </summary>
    public ConfigurationProfile? GetProfile(
        string profileId,
        CancellationToken cancellationToken = default)
    {
        return database.GetLatestRevision(profileId, cancellationToken);
    }

    /// <summary>
    /// Reads a specific historical revision of a profile.
    /// </summary>
    public ConfigurationProfile? GetProfileRevision(
        string profileId,
        uint revision,
        CancellationToken cancellationToken = default)
    {
        return database.Read(profileId, revision, cancellationToken);
    }

    /// <summary>
    /// Returns the complete historical revisions of a profile.
    /// </summary>
    public IReadOnlyList<ConfigurationProfile> GetProfileHistory(
        string profileId,
        CancellationToken cancellationToken = default)
    {
        return database.GetHistory(profileId, cancellationToken);
    }

    /// <summary>
    /// Proposes and commits a set of changes to a profile.
    /// Spawns a new immutable profile revision in a database transaction.
    /// </summary>
    public ConfigurationProfile ProposeChanges(
        string profileId,
        IEnumerable<ProfileProposedChange> changes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(changes);

        // Load the latest revision of the profile
        ConfigurationProfile? latest = database.GetLatestRevision(profileId, cancellationToken)
            ?? throw new KeyNotFoundException($"Profile '{profileId}' does not exist.");

        // Copy existing values into a mutable dictionary
        Dictionary<string, string> newValues = latest.Values.ToDictionary(
            static kvp => kvp.Key,
            static kvp => kvp.Value);

        // Apply changes
        foreach (ProfileProposedChange change in changes)
        {
            if (change.ValueJson is null)
            {
                _ = newValues.Remove(change.SettingId);
            }
            else
            {
                newValues[change.SettingId] = change.ValueJson;
            }
        }

        // Construct new profile incrementing the revision
        ConfigurationProfile nextRevision = new(
            latest.ProfileId,
            revision: latest.Revision + 1,
            latest.DisplayName,
            latest.ProfileKind,
            latest.OwnerScope,
            latest.ParentProfileId,
            latest.ParentRevision,
            latest.SchemaVersion,
            latest.Classification,
            newValues,
            DateTimeOffset.UtcNow);

        // Validate values against Setting Definitions (throws on invalid, wrong type, or secret)
        nextRevision.Validate(settingCatalogue);

        // Commit to database
        database.Save(nextRevision, cancellationToken);

        return nextRevision;
    }

    /// <summary>
    /// Begins a configuration change transaction, returning a preview of the proposed state.
    /// If changes are invalid or violate policy, the preview marks the transaction as invalid and provides diagnostic errors.
    /// </summary>
    public ConfigurationChangeTransactionPreview BeginTransaction(
        ConfigurationChangeRequest request,
        ProjectSettingsSource? currentProjectSettings = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        ConfigurationProfile? latest = database.GetLatestRevision(request.TargetProfileId, cancellationToken)
            ?? throw new KeyNotFoundException($"Profile '{request.TargetProfileId}' does not exist.");

        Dictionary<string, string> newValues = latest.Values.ToDictionary(
            static kvp => kvp.Key,
            static kvp => kvp.Value);

        ProfileProposedChange[] requestedChanges = request.Changes.ToArray();
        foreach (ProfileProposedChange change in requestedChanges)
        {
            if (change.ValueJson is null)
            {
                _ = newValues.Remove(change.SettingId);
            }
            else
            {
                newValues[change.SettingId] = change.ValueJson;
            }
        }

        ConfigurationProfile provisionalProfile = new(
            latest.ProfileId,
            revision: latest.Revision + 1,
            latest.DisplayName,
            latest.ProfileKind,
            latest.OwnerScope,
            latest.ParentProfileId,
            latest.ParentRevision,
            latest.SchemaVersion,
            latest.Classification,
            newValues,
            DateTimeOffset.UtcNow);

        List<string> errors = [];
        try
        {
            provisionalProfile.Validate(settingCatalogue);
        }
        catch (ArgumentException ex)
        {
            errors.Add(ex.Message);
            return new ConfigurationChangeTransactionPreview(false, errors, null, null);
        }

        ConfigurationProfile userProfile = provisionalProfile.ProfileKind == "user"
            ? provisionalProfile
            : (database.GetLatestRevision("user.base", cancellationToken) ?? provisionalProfile); // Fallback to avoid null but real logic would fetch user profile

        if (provisionalProfile.ProfileKind == "project")
        {
            currentProjectSettings = new ProjectSettingsSource(provisionalProfile.ProfileId, provisionalProfile.Revision, provisionalProfile.CanonicalSha256, new Dictionary<string, string>(provisionalProfile.Values, StringComparer.Ordinal), true);
        }

        SettingMergeResult mergeResult = SettingMerger.Merge(
            settingCatalogue,
            productDefaults,
            userProfile,
            currentProjectSettings);

        if (!mergeResult.Success)
        {
            errors.Add($"Merge failed: {mergeResult.FailureReason}");
            return new ConfigurationChangeTransactionPreview(false, errors, null, null);
        }

        ProductPolicyEvaluationReceipt policyReceipt = EvaluatePolicyFailClosed(mergeResult);

        if (!policyReceipt.Success)
        {
            errors.Add($"Policy evaluation failed: {policyReceipt.FailureReason}");
            return new ConfigurationChangeTransactionPreview(false, errors, null, null);
        }

        EffectiveConfigurationSnapshotBuildResult previewResult = EffectiveConfigurationSnapshotBuilder.Build(
            settingCatalogue,
            productDefaults,
            policyCatalogue,
            mergeResult,
            policyReceipt,
            userProfile,
            currentProjectSettings);

        ConfigurationChangeApprovalBinding approvalBinding = new(
            ComputeProposalSha256(request, requestedChanges),
            latest.Revision,
            latest.CanonicalSha256,
            currentProjectSettings?.Generation,
            currentProjectSettings?.ContentHash);

        return new ConfigurationChangeTransactionPreview(
            true,
            errors,
            provisionalProfile,
            previewResult,
            approvalBinding);
    }

    /// <summary>
    /// Commits a previously validated transaction, saving the new profile revision and snapshot,
    /// and emitting authoritative trust evidence.
    /// </summary>
    public void CommitTransaction(
        ConfigurationChangeRequest originalRequest,
        ConfigurationChangeTransactionPreview preview,
        CancellationToken cancellationToken = default)
    {
        CommitTransaction(originalRequest, preview, null, cancellationToken);
    }

    public void CommitTransaction(
        ConfigurationChangeRequest originalRequest,
        ConfigurationChangeTransactionPreview preview,
        ProjectSettingsSource? currentProjectSettings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(originalRequest);
        ArgumentNullException.ThrowIfNull(preview);

        if (!preview.IsValid || preview.ProvisionalProfile is null ||
            preview.PreviewSnapshotResult is null || preview.ApprovalBinding is null)
        {
            throw new InvalidOperationException("Cannot commit an invalid transaction preview.");
        }

        ConfigurationChangeApprovalBinding binding = preview.ApprovalBinding;
        ProfileProposedChange[] currentChanges = originalRequest.Changes.ToArray();
        if (!CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(binding.ProposalSha256),
                Convert.FromHexString(ComputeProposalSha256(originalRequest, currentChanges))))
        {
            throw new InvalidOperationException("The approved configuration proposal has changed.");
        }

        ConfigurationProfile currentProfile = database.GetLatestRevision(
            originalRequest.TargetProfileId,
            cancellationToken) ?? throw new InvalidOperationException(
                "The configuration profile used for approval no longer exists.");
        if (currentProfile.Revision != binding.BaseProfileRevision ||
            !string.Equals(currentProfile.CanonicalSha256, binding.BaseProfileSha256, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The configuration approval is stale because the profile changed.");
        }

        if ((binding.WorkspaceGeneration is null) != (currentProjectSettings is null) ||
            (binding.WorkspaceGeneration is not null &&
             (currentProjectSettings!.Generation != binding.WorkspaceGeneration ||
              !string.Equals(
                  currentProjectSettings.ContentHash,
                  binding.WorkspaceContentHash,
                  StringComparison.Ordinal))))
        {
            throw new InvalidOperationException("The configuration approval is stale because the Workspace source changed.");
        }

        if (IsTestCrashPoint("ConfigurationBeforeCommit"))
        {
            Environment.Exit(71);
        }

        database.Save(preview.ProvisionalProfile, cancellationToken);

        string transactionId = Guid.NewGuid().ToString("N");

        EvidenceTypeDefinition evidenceType = FoundationEvidenceTypeCatalogue.Current.Definitions.Single(
            static d => d.EvidenceTypeId == "configuration.transaction-result" && d.Revision == 1);

        EvidenceRecordPayload payload = EvidenceRecordPayload.CreateInline(
            $"{{\"transaction_id\":\"{transactionId}\",\"is_valid\":true,\"error_count\":0}}",
            EvidenceDataClassification.Safe);

        if (IsTestCrashPoint("ConfigurationAfterCommitBeforeOutbox"))
        {
            Environment.Exit(71);
        }

        evidencePort.Ingest(
            new EvidenceIngestionRequest(
                messageId: Guid.NewGuid().ToString("N"),
                contractRevision: 1,
                record: new EvidenceRecord(
                    evidenceId: EvidenceRecord.CreateEvidenceId(),
                    evidenceType: evidenceType,
                    ownerServiceId: "opure.configuration",
                    ownerRecordId: transactionId,
                    ownerRecordRevision: 1,
                    authorityClass: EvidenceAuthorityClass.DeterministicValidationResult,
                    releaseChannel: EvidenceReleaseChannel.Development,
                    scope: EvidenceRecordScope.Global,
                    projectId: null,
                    operationId: null,
                    workflowInstanceId: null,
                    traceId: null,
                    spanId: null,
                    runtimeBootId: null,
                    subjectKind: EvidenceSubjectKind.Configuration,
                    subjectId: "transaction",
                    action: "configuration.transaction.commit",
                    outcome: "succeeded",
                    occurredAtUtc: DateTimeOffset.UtcNow,
                    observedAtUtc: DateTimeOffset.UtcNow,
                    ownerSequence: 1,
                    previousStreamSha256: null,
                    retentionClass: evidenceType.Retention.RetentionClass,
                    preservationState: EvidencePreservationState.NotPreserved,
                    payload: payload),
                declaredPayloadSha256: payload.PayloadSha256,
                declaredRecordSha256: "hash_placeholder",
                relationships: []),
            cancellationToken);
    }

    private static string ComputeSha256(string data)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(data);
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash);
    }

    /// <summary>
    /// Generates a flat projection of all catalogue settings, merging current profile values
    /// for a clean UI editor presentation.
    /// </summary>
    public IReadOnlyList<ProfileEditorItem> GetEditorProjection(
        string profileId,
        CancellationToken cancellationToken = default)
    {
        ConfigurationProfile? profile = database.GetLatestRevision(profileId, cancellationToken)
            ?? throw new KeyNotFoundException($"Profile '{profileId}' does not exist.");

        List<ProfileEditorItem> items = [];

        // Expose settings ordered by category and order metadata
        foreach (SettingDefinition definition in settingCatalogue.Definitions
                     .OrderBy(static d => d.Ui.Category, StringComparer.Ordinal)
                     .ThenBy(static d => d.Ui.Order))
        {
            _ = profile.Values.TryGetValue(definition.SettingId, out string? configuredValue);

            items.Add(new ProfileEditorItem(
                definition.SettingId,
                definition.DisplayName,
                definition.Description,
                configuredValue,
                definition.DefaultValueCanonicalJson,
                definition.ValueType,
                definition.Ui.Category,
                definition.Ui.Editor,
                definition.Ui.Order,
                definition.Ui.Advanced));
        }

        return items;
    }

    /// <summary>
    /// Retrieves the provenance trace for a specific setting from a snapshot, masking sensitive values.
    /// </summary>
    public EffectiveSettingProvenance? GetSettingProvenance(
        string snapshotId,
        string settingId,
        CancellationToken cancellationToken = default)
    {
        EffectiveSettingProvenance? provenance = database.GetSettingProvenance(snapshotId, settingId, cancellationToken);
        if (provenance is null)
        {
            return null;
        }

        // Retrieve setting definition to check sensitivity
        SettingDefinition? def = settingCatalogue.Definitions.FirstOrDefault(d => d.SettingId == settingId);
        bool isSensitive = def is not null &&
                           (def.Sensitivity == SettingSensitivity.Confidential ||
                            def.Sensitivity == SettingSensitivity.SecuritySensitive ||
                            def.Sensitivity == SettingSensitivity.SecretReference ||
                            def.Sensitivity == SettingSensitivity.ProhibitedSecretValue);

        if (!isSensitive)
        {
            return provenance;
        }

        // Redact values
        const string redacted = "\"***\"";

        List<EffectiveSettingProvenanceStep> redactedMergeSteps = [];
        foreach (EffectiveSettingProvenanceStep step in provenance.MergeSteps)
        {
            redactedMergeSteps.Add(new EffectiveSettingProvenanceStep(
                step.Source,
                step.SourceIdentifier,
                redacted,
                step.Applied,
                step.Explanation));
        }

        return new EffectiveSettingProvenance(
            provenance.SettingId,
            provenance.SnapshotId,
            provenance.RequestedSource,
            provenance.DefinitionRevision,
            redacted,
            redacted,
            redactedMergeSteps,
            provenance.PolicyDecisions,
            provenance.IsConstrainedByPolicy,
            provenance.Explanation);
    }

    /// <summary>
    /// Acquires, parses, merges and evaluates project settings. 
    /// Tracks observed vs valid source states as per FND-052.
    /// Does not save the snapshot if parsing, merge or policy fails, but records observation state.
    /// </summary>
    public ProjectSourceObservationState ObserveProjectSettings(
        string projectId,
        long generation,
        Opure.Workspace.Contracts.IWorkspaceSourceProvider provider,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        ArgumentNullException.ThrowIfNull(provider);

        DateTimeOffset observedAt = DateTimeOffset.UtcNow;

        ProjectSettingsSource source;
        try
        {
            source = ProjectSettingsAcquirer.Acquire(provider, projectId, generation);
        }
        catch (Exception ex) when (ex is StrictJsonException || ex is ArgumentException || ex is InvalidOperationException)
        {
            // Parse or schema validation failed
            ProjectSourceObservationState? current =
                database.GetProjectObservationState(projectId, cancellationToken);
            ProjectSourceObservationState failedState = new(
                projectId,
                generation,
                string.Empty, // Unknown hash if parse failed completely before hash calculation
                observedAt,
                current?.LatestValidGeneration,
                current?.LatestValidContentHash,
                current?.LatestValidSnapshotId,
                $"Parse or validation failed: {ex.Message}");
            
            database.RecordProjectObservation(failedState, cancellationToken);
            return failedState;
        }

        // Merge and Policy
        ConfigurationProfile userProfile = database.GetLatestRevision("user.base", cancellationToken) 
            ?? new ConfigurationProfile("user.base", 1, "User", "user", SettingScope.User, null, null, 1, "confidential", new Dictionary<string, string>(), DateTimeOffset.UtcNow);

        SettingMergeResult mergeResult = SettingMerger.Merge(
            settingCatalogue,
            productDefaults,
            userProfile,
            source);

        if (!mergeResult.Success)
        {
            ProjectSourceObservationState? current =
                database.GetProjectObservationState(projectId, cancellationToken);
            ProjectSourceObservationState mergeFailState = new(
                projectId,
                generation,
                source.ContentHash,
                observedAt,
                current?.LatestValidGeneration,
                current?.LatestValidContentHash,
                current?.LatestValidSnapshotId,
                $"Merge failed: {mergeResult.FailureReason}");
            
            database.RecordProjectObservation(mergeFailState, cancellationToken);
            return mergeFailState;
        }

        ProductPolicyEvaluationReceipt policyReceipt = EvaluatePolicyFailClosed(mergeResult);

        if (!policyReceipt.Success)
        {
            ProjectSourceObservationState? current =
                database.GetProjectObservationState(projectId, cancellationToken);
            ProjectSourceObservationState policyFailState = new(
                projectId,
                generation,
                source.ContentHash,
                observedAt,
                current?.LatestValidGeneration,
                current?.LatestValidContentHash,
                current?.LatestValidSnapshotId,
                $"Policy failed: {policyReceipt.FailureReason}");
            
            database.RecordProjectObservation(policyFailState, cancellationToken);
            return policyFailState;
        }

        // Build valid snapshot
        uint snapshotGeneration = checked(
            (database.GetCurrentSnapshot("Project", cancellationToken)
                ?.SnapshotGeneration ?? 0) + 1);
        EffectiveConfigurationSnapshotBuildResult buildResult = EffectiveConfigurationSnapshotBuilder.Build(
            settingCatalogue,
            productDefaults,
            policyCatalogue,
            mergeResult,
            policyReceipt,
            userProfile,
            source,
            snapshotGeneration);

        // Record valid state and snapshot
        if (IsTestCrashPoint("ConfigurationBeforeCommit"))
        {
            Environment.Exit(71);
        }

        database.SaveSnapshot(buildResult, "Project", cancellationToken);

        if (IsTestCrashPoint("ConfigurationAfterCommitBeforeOutbox"))
        {
            Environment.Exit(71);
        }

        ProjectSourceObservationState validState = new(
            projectId,
            generation,
            source.ContentHash,
            observedAt,
            generation,
            source.ContentHash,
            buildResult.Snapshot.SnapshotId,
            null);

        database.RecordProjectObservation(validState, cancellationToken);
        return validState;
    }

    private static bool IsTestCrashPoint(string crashPoint)
    {
        string? armFile = Environment.GetEnvironmentVariable(
            "OPURE_TEST_CRASH_ARM_FILE");
        return string.Equals(
            Environment.GetEnvironmentVariable("OPURE_BOOTSTRAP_TEST_MODE"),
            "1",
            StringComparison.Ordinal) &&
        string.Equals(
            Environment.GetEnvironmentVariable("OPURE_TEST_CRASH_POINT"),
            crashPoint,
            StringComparison.Ordinal) &&
        (string.IsNullOrWhiteSpace(armFile) || File.Exists(armFile));
    }

    private ProductPolicyEvaluationReceipt EvaluatePolicyFailClosed(SettingMergeResult mergeResult)
    {
        try
        {
            return policyEvaluationPort.Evaluate(policyCatalogue, settingCatalogue, mergeResult);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new ProductPolicyEvaluationReceipt(
                policyCatalogue.CatalogueRevision,
                policyCatalogue.CanonicalSha256,
                new Dictionary<string, PolicyKeyEvaluation>(StringComparer.Ordinal),
                decisions: [],
                success: false,
                failureReason: "Product Policy evaluation failed closed.");
        }
    }

    private static string ComputeProposalSha256(
        ConfigurationChangeRequest request,
        IReadOnlyList<ProfileProposedChange> changes)
    {
        StringBuilder canonical = new();
        AppendLengthPrefixed(canonical, request.TargetProfileId);
        AppendLengthPrefixed(canonical, request.SourceIdentifier);
        foreach (ProfileProposedChange change in changes)
        {
            AppendLengthPrefixed(canonical, change.SettingId);
            AppendLengthPrefixed(canonical, change.ValueJson);
        }

        return ComputeSha256(canonical.ToString());
    }

    private static void AppendLengthPrefixed(StringBuilder destination, string? value)
    {
        if (value is null)
        {
            _ = destination.Append("-1:");
            return;
        }

        _ = destination.Append(value.Length).Append(':').Append(value);
    }
}
