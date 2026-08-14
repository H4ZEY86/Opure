using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Text;
using Opure.Filesystem.Contracts;
using Opure.Filesystem.Windows;
using Opure.Ipc.Abstractions;
using Opure.Ipc.NamedPipes.Windows;
using Opure.Project.Protocol;
using Opure.Project.Protocol.List.V1;
using Opure.Project.Protocol.Open.V1;
using Opure.Recovery.Protocol;
using Opure.Recovery.Protocol.Point.V1;
using Opure.Runtime.Contracts;
using Opure.Runtime.Contracts.Health.V1;
using Opure.TrustEvidence.Protocol;
using Opure.TrustEvidence.Protocol.Configuration.V1;
using Opure.TrustEvidence.Protocol.Overview.V1;
using Opure.TrustEvidence.Protocol.Project.V1;
using DomainVolumeClass = Opure.Filesystem.Contracts.FilesystemVolumeClass;
using ProjectListChannel = Opure.Project.Protocol.List.V1.ProjectListReleaseChannel;
using ProjectOpenChannel = Opure.Project.Protocol.Open.V1.ProjectReleaseChannel;
using WireFileIdentityCapability = Opure.Project.Protocol.Open.V1.FileIdentityCapability;
using WireVolumeClass = Opure.Project.Protocol.Open.V1.FilesystemVolumeClass;

namespace Opure.Cli;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Opure CLI");
            WriteUsage();
            return 1;
        }

        if (args.Length > 0 && string.Equals(args[0], "--daemon", StringComparison.OrdinalIgnoreCase))
        {
            return await HandleDaemonAsync(args[1..]);
        }

        string command = args[0].ToLowerInvariant();
        return command switch
        {
            "health" => await HandleHealthAsync(),
            "gate-a" => await HandleGateAAsync(args[1..]),
            "project" => await HandleProjectAsync(args[1..]),
            "recovery" => await HandleRecoveryAsync(args[1..]),
            "license" => await HandleLicenseAsync(args[1..]),
            "version" => HandleVersion(),
            _ => HandleUnknownCommand(command)
        };
    }

    private static async Task<int> HandleDaemonAsync(string[] args)
    {
        Console.WriteLine("Starting Opure daemon...");
        
        // Resolve Runtime executable next to the CLI executable
        string baseDir = AppContext.BaseDirectory;
        string runtimePath = Path.Combine(baseDir, "Opure.Runtime.exe");
        if (!File.Exists(runtimePath))
        {
            Console.Error.WriteLine($"Cannot find daemon executable at: {runtimePath}");
            return 1;
        }

        using Process process = new();
        process.StartInfo.FileName = runtimePath;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        
        // Pass arguments through to runtime
        foreach (string arg in args)
        {
            process.StartInfo.ArgumentList.Add(arg);
        }

        using CancellationTokenSource cts = new();
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
            Console.WriteLine("Stopping daemon...");
        };

        try
        {
            process.Start();

            // Mirror output
            _ = Task.Run(() => process.StandardOutput.BaseStream.CopyToAsync(Console.OpenStandardOutput(), cts.Token));
            _ = Task.Run(() => process.StandardError.BaseStream.CopyToAsync(Console.OpenStandardError(), cts.Token));

            await process.WaitForExitAsync(cts.Token);
            return process.ExitCode;
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                process.Kill();
            }
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Daemon failed: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> HandleLicenseAsync(string[] args)
    {
        if (args.Length != 2 || !string.Equals(args[0], "apply", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("Usage: opure license apply <key>");
            return 1;
        }

        string token = args[1];
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 2)
            {
                Console.Error.WriteLine("License rejected: invalid format.");
                return 1;
            }

            string payloadBase64Url = parts[0];
            var output = payloadBase64Url.Replace('-', '+').Replace('_', '/');
            switch (output.Length % 4)
            {
                case 2: output += "=="; break;
                case 3: output += "="; break;
            }
            byte[] payloadBytes = Convert.FromBase64String(output);
            string payloadJson = System.Text.Encoding.UTF8.GetString(payloadBytes);

            using var doc = System.Text.Json.JsonDocument.Parse(payloadJson);
            var root = doc.RootElement;
            string licenseId = root.GetProperty("LicenseId").GetString() ?? "Unknown";
            string licensedTo = root.GetProperty("LicensedTo").GetString() ?? "Unknown";

            Console.WriteLine($"License '{licenseId}' applied successfully to '{licensedTo}'.");
            
            // Persist the token to %LOCALAPPDATA%\Opure\license.dat
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string licensePath = Path.Combine(appData, "Opure", "license.dat");
            Directory.CreateDirectory(Path.GetDirectoryName(licensePath)!);
            await File.WriteAllTextAsync(licensePath, token);
            Console.WriteLine($"License saved to {licensePath}");
            
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"License verification failed: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> HandleGateAAsync(string[] args)
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("OPURE_GATE_A_TEST_MODE"),
                "1",
                StringComparison.Ordinal) ||
            args.Length != 4 ||
            args[0] is not ("probe" or "post-restart" or "recovery") ||
            !string.Equals(args[1], "--channel", StringComparison.Ordinal) ||
            !string.Equals(args[3], "--path-stdin", StringComparison.Ordinal))
        {
            Console.Error.WriteLine(
                "The Gate A probe is restricted to the bounded engineering harness.");
            return 1;
        }

        string channel = args[2];
        if (channel is not ("Development" or "Preview" or "Stable"))
        {
            Console.Error.WriteLine("Channel must be Development, Preview or Stable.");
            return 1;
        }

        bool postRestart = string.Equals(
            args[0],
            "post-restart",
            StringComparison.Ordinal);
        bool recoveryProbe = string.Equals(
            args[0],
            "recovery",
            StringComparison.Ordinal);

        int health = await HandleHealthAsync().ConfigureAwait(false);
        if (health != 0)
        {
            return health;
        }

        if (!TryGetRuntimeEndpoint(
                out RuntimeHealthEndpoint? endpoint,
                out RuntimeHealthSessionMaterial? sessionMaterial))
        {
            Console.Error.WriteLine("The Gate A session is unavailable.");
            return 1;
        }

        string? fixturePath = await Console.In.ReadLineAsync().ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(fixturePath) ||
            !Path.IsPathFullyQualified(fixturePath))
        {
            Console.Error.WriteLine("The Gate A probe requires one absolute fixture path.");
            return 1;
        }

        if (recoveryProbe)
        {
            return await RunGateARecoverySequenceAsync(
                endpoint,
                sessionMaterial,
                channel).ConfigureAwait(false);
        }

        int opened = await OpenProjectAsync(
            endpoint,
            sessionMaterial,
            channel,
            readPathFromStandardInput: true,
            fixturePath).ConfigureAwait(false);
        if (opened != 0)
        {
            return opened;
        }

        int listed = await HandleProjectAsync(
            ["list", "--channel", channel])
            .ConfigureAwait(false);
        if (listed != 0)
        {
            return listed;
        }

        if (postRestart)
        {
            TrustConfigurationSnapshotMessage? durableConfiguration =
                await QueryGateAConfigurationAsync(
                    endpoint,
                    sessionMaterial,
                    "DurableAfterRestart").ConfigureAwait(false);
            if (durableConfiguration is null ||
                durableConfiguration.ProjectGeneration < 1)
            {
                return 1;
            }

            if (await QueryGateATrustCentreAsync(
                    endpoint,
                    sessionMaterial,
                    durableConfiguration.ProjectId,
                    "Final").ConfigureAwait(false) != 0)
            {
                return 1;
            }

            return await RunGateARecoverySequenceAsync(
                endpoint,
                sessionMaterial,
                channel).ConfigureAwait(false);
        }

        int configuration = await RunGateAConfigurationSequenceAsync(
            endpoint,
            sessionMaterial,
            channel,
            fixturePath).ConfigureAwait(false);
        if (configuration != 0)
        {
            return configuration;
        }

        TrustConfigurationSnapshotMessage? current =
            await QueryGateAConfigurationAsync(
                endpoint,
                sessionMaterial,
                "TrustCentre").ConfigureAwait(false);
        if (current is null ||
            await QueryGateATrustCentreAsync(
                endpoint,
                sessionMaterial,
                current.ProjectId,
                "Initial").ConfigureAwait(false) != 0)
        {
            return 1;
        }

        return await VerifyInvalidSessionDenialAsync().ConfigureAwait(false);
    }

    private static async Task<int> QueryGateATrustCentreAsync(
        RuntimeHealthEndpoint endpoint,
        RuntimeHealthSessionMaterial sessionMaterial,
        string projectId,
        string stage)
    {
        await using NamedPipeTrustEvidenceClient client = new(
            endpoint,
            sessionMaterial);
        TrustOverviewResponseMessage overview = await client.QueryOverviewAsync(
            new TrustOverviewRequestMessage
            {
                ContractRevision = Opure.TrustEvidence.Contracts.TrustOverviewRequest.CurrentContractRevision,
                QueryId = Guid.NewGuid().ToString("N"),
                ReleaseChannel = TrustEvidenceReleaseChannel.Development,
                ProjectId = projectId
            },
            CancellationToken.None).ConfigureAwait(false);
        TrustProjectResponseMessage project = await client.QueryProjectAsync(
            new TrustProjectRequestMessage
            {
                ContractRevision = Opure.TrustEvidence.Contracts.TrustProjectRequest.CurrentContractRevision,
                QueryId = Guid.NewGuid().ToString("N"),
                ReleaseChannel = TrustEvidenceReleaseChannel.Development,
                ProjectId = projectId
            },
            CancellationToken.None).ConfigureAwait(false);
        if (overview.Disposition != TrustEvidenceQueryDisposition.Computed ||
            overview.Snapshot is null ||
            project.Disposition != TrustEvidenceQueryDisposition.Computed ||
            project.Snapshot is null)
        {
            Console.Error.WriteLine("The Gate A Trust Centre projections were unavailable.");
            return 1;
        }

        Console.WriteLine($"Trust Centre stage: {stage}");
        Console.WriteLine(
            $"Trust Overview: owner={overview.Snapshot.OwnerAvailability} completeness={overview.Snapshot.Completeness} records={overview.Snapshot.TotalRecordCount} generation={overview.Snapshot.ProjectionGeneration}");
        Console.WriteLine(
            $"Trust Project: project={project.Snapshot.ProjectId} owner={project.Snapshot.OwnerAvailability} completeness={project.Snapshot.Completeness} events={project.Snapshot.Events.Count} workspace={project.Snapshot.CurrentWorkspaceGeneration}");
        Console.WriteLine("Trust Configuration: projection=Computed authority=ConfigurationService");
        return 0;
    }

    private static async Task<int> RunGateARecoverySequenceAsync(
        RuntimeHealthEndpoint endpoint,
        RuntimeHealthSessionMaterial sessionMaterial,
        string channel)
    {
        await using NamedPipeRecoveryPointClient client = new(
            endpoint,
            sessionMaterial);
        CreateRecoveryPointResponseMessage created =
            await client.CreateRecoveryPointAsync(
                new CreateRecoveryPointRequestMessage
                {
                    ContractRevision = RecoveryPointContractPolicy.CurrentRevision,
                    ReleaseChannel = channel
                },
                CancellationToken.None).ConfigureAwait(false);
        if (!created.IsSuccess ||
            !Guid.TryParse(created.RecoveryPointId, out Guid recoveryPointId))
        {
            Console.Error.WriteLine("Gate A could not create a local Recovery Point.");
            return 1;
        }

        VerifyRecoveryPointResponseMessage verified =
            await client.VerifyRecoveryPointAsync(
                new VerifyRecoveryPointRequestMessage
                {
                    ContractRevision = RecoveryPointContractPolicy.CurrentRevision,
                    ReleaseChannel = channel,
                    RecoveryPointId = recoveryPointId.ToString("N")
                },
                CancellationToken.None).ConfigureAwait(false);
        ListRecoveryPointsResponseMessage listed =
            await client.ListRecoveryPointsAsync(
                new ListRecoveryPointsRequestMessage
                {
                    ContractRevision = RecoveryPointContractPolicy.CurrentRevision,
                    ReleaseChannel = channel
                },
                CancellationToken.None).ConfigureAwait(false);
        RecoveryPointSummaryMessage? summary = listed.Points.FirstOrDefault(
            point => string.Equals(
                point.RecoveryPointId,
                recoveryPointId.ToString("N"),
                StringComparison.Ordinal));
        if (!verified.IsSuccess ||
            summary is null ||
            !string.Equals(summary.ScopeClass, "same-device", StringComparison.Ordinal) ||
            !string.Equals(summary.VerificationState, "Structural", StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Gate A Recovery Point verification was incomplete.");
            return 1;
        }

        Console.WriteLine($"Recovery Point: {recoveryPointId:N}");
        Console.WriteLine(
            $"Recovery verification: {summary.VerificationState} scope={summary.ScopeClass} owners={summary.OwnerCount} receipts={summary.Receipts.Count}");
        return 0;
    }

    private static async Task<int> VerifyInvalidSessionDenialAsync()
    {
        if (!TryGetRuntimeEndpoint(
                out RuntimeHealthEndpoint? endpoint,
                out RuntimeHealthSessionMaterial? sessionMaterial))
        {
            Console.Error.WriteLine("The Gate A session is unavailable.");
            return 1;
        }

        RuntimeHealthSessionMaterial invalidMaterial = new(
            sessionMaterial.SessionId,
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
        await using NamedPipeRuntimeHealthClient client = new(
            endpoint,
            invalidMaterial);
        GetRuntimeHealthRequest request = new()
        {
            MinimumContractRevision = RuntimeHealthContractPolicy.CurrentRevision,
            MaximumContractRevision = RuntimeHealthContractPolicy.CurrentRevision,
            QueryId = Guid.NewGuid().ToString("N")
        };

        try
        {
            _ = await client.GetRuntimeHealthAsync(
                request,
                RuntimeHealthContractPolicy.DefaultDeadline,
                CancellationToken.None).ConfigureAwait(false);
        }
        catch (RuntimeHealthTransportException exception) when (
            string.Equals(
                exception.ErrorCode,
                RuntimeHealthTransportErrorCodes.SessionDenied,
                StringComparison.Ordinal))
        {
            Console.WriteLine("Invalid session: Denied");
            return 0;
        }

        Console.Error.WriteLine("The Runtime accepted invalid Gate A session material.");
        return 1;
    }

    private static async Task<int> RunGateAConfigurationSequenceAsync(
        RuntimeHealthEndpoint endpoint,
        RuntimeHealthSessionMaterial sessionMaterial,
        string channel,
        string fixturePath)
    {
        TrustConfigurationSnapshotMessage? initial =
            await QueryGateAConfigurationAsync(
                endpoint,
                sessionMaterial,
                "Initial").ConfigureAwait(false);
        if (initial is null || string.IsNullOrWhiteSpace(initial.ProjectId))
        {
            return 1;
        }

        string settingsDirectory = Path.Combine(fixturePath, ".opure");
        string settingsPath = Path.Combine(
            settingsDirectory,
            "project.settings.json");
        Directory.CreateDirectory(settingsDirectory);
        try
        {
            WriteGateAProjectSettings(
                settingsPath,
                initial.ProjectId,
                "debug");
            if (await ReopenGateAProjectAsync(
                    endpoint,
                    sessionMaterial,
                    channel,
                    fixturePath).ConfigureAwait(false) != 0)
            {
                return 1;
            }

            TrustConfigurationSnapshotMessage? changed =
                await QueryGateAConfigurationAsync(
                    endpoint,
                    sessionMaterial,
                    "ValidChange").ConfigureAwait(false);
            if (changed is null ||
                changed.Generation <= initial.Generation ||
                changed.ProjectGeneration <= initial.ProjectGeneration)
            {
                Console.Error.WriteLine(
                    "The valid project-settings change did not advance both generations.");
                return 1;
            }

            File.WriteAllText(
                settingsPath,
                "{\"schema\":",
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            if (await ReopenGateAProjectAsync(
                    endpoint,
                    sessionMaterial,
                    channel,
                    fixturePath).ConfigureAwait(false) != 0)
            {
                return 1;
            }

            TrustConfigurationSnapshotMessage? invalid =
                await QueryGateAConfigurationAsync(
                    endpoint,
                    sessionMaterial,
                    "InvalidSource").ConfigureAwait(false);
            if (invalid is null ||
                invalid.Generation != changed.Generation ||
                invalid.LatestObservedGeneration <= changed.ProjectGeneration ||
                invalid.LatestValidGeneration != changed.ProjectGeneration ||
                string.IsNullOrWhiteSpace(invalid.LastError))
            {
                Console.Error.WriteLine(
                    "Invalid project settings replaced or obscured the last-known-good snapshot.");
                return 1;
            }

            WriteGateAProjectSettings(
                settingsPath,
                initial.ProjectId,
                "warning");
            if (await ReopenGateAProjectAsync(
                    endpoint,
                    sessionMaterial,
                    channel,
                    fixturePath).ConfigureAwait(false) != 0)
            {
                return 1;
            }

            TrustConfigurationSnapshotMessage? repaired =
                await QueryGateAConfigurationAsync(
                    endpoint,
                    sessionMaterial,
                    "Repaired").ConfigureAwait(false);
            if (repaired is null ||
                repaired.Generation <= changed.Generation ||
                repaired.LatestValidGeneration <= changed.ProjectGeneration ||
                !string.IsNullOrWhiteSpace(repaired.LastError))
            {
                Console.Error.WriteLine(
                    "The repaired project settings did not become the current valid snapshot.");
                return 1;
            }

            return 0;
        }
        finally
        {
            if (File.Exists(settingsPath))
            {
                File.Delete(settingsPath);
            }
        }
    }

    private static async Task<int> ReopenGateAProjectAsync(
        RuntimeHealthEndpoint endpoint,
        RuntimeHealthSessionMaterial sessionMaterial,
        string channel,
        string fixturePath) =>
        await OpenProjectAsync(
            endpoint,
            sessionMaterial,
            channel,
            readPathFromStandardInput: true,
            fixturePath).ConfigureAwait(false);

    private static async Task<TrustConfigurationSnapshotMessage?>
        QueryGateAConfigurationAsync(
            RuntimeHealthEndpoint endpoint,
            RuntimeHealthSessionMaterial sessionMaterial,
            string stage)
    {
        await using NamedPipeTrustEvidenceClient client = new(
            endpoint,
            sessionMaterial);
        TrustConfigurationResponseMessage response =
            await client.QueryConfigurationAsync(
                new TrustConfigurationRequestMessage
                {
                    ContractRevision = TrustConfigurationContractPolicy.CurrentRevision,
                    QueryId = Guid.NewGuid().ToString("N"),
                    ReleaseChannel = TrustEvidenceReleaseChannel.Development,
                    Scope = "Project"
                },
                CancellationToken.None).ConfigureAwait(false);
        if (response.Disposition != TrustEvidenceQueryDisposition.Computed ||
            response.Snapshot is null)
        {
            Console.Error.WriteLine(
                $"Configuration query failed: {response.StableCode} - {response.SafeDetail}");
            return null;
        }

        TrustConfigurationSnapshotMessage snapshot = response.Snapshot;
        Console.WriteLine($"Configuration stage: {stage}");
        Console.WriteLine(
            $"Product Defaults: revision {snapshot.ProductDefaultsRevision} SHA-256 {snapshot.ProductDefaultsSha256}");
        Console.WriteLine(
            $"User Base Profile: {snapshot.UserProfileId} revision {snapshot.UserProfileRevision}");
        Console.WriteLine(
            $"Project settings content SHA-256: {snapshot.ProjectContentHash}");
        Console.WriteLine(
            $"Effective Configuration: {snapshot.SnapshotId} generation {snapshot.Generation}");
        Console.WriteLine(
            $"Configuration Workspace generation: {snapshot.ProjectGeneration}");
        Console.WriteLine(
            $"Latest observed Workspace generation: {snapshot.LatestObservedGeneration}");
        Console.WriteLine(
            $"Latest valid Workspace generation: {snapshot.LatestValidGeneration}");
        Console.WriteLine(
            $"Configuration source error: {(string.IsNullOrWhiteSpace(snapshot.LastError) ? "None" : "Present")}");
        foreach (TrustConfigurationEntryMessage entry in snapshot.Entries)
        {
            Console.WriteLine(
                $"  Configuration key {entry.SettingId}: requested={entry.RequestedValueJson} effective={entry.EffectiveValueJson} source={entry.WinningSource} provenance={entry.MergeTraceJson}");
        }

        return snapshot;
    }

    private static void WriteGateAProjectSettings(
        string settingsPath,
        string projectId,
        string logLevel)
    {
        string json = $$"""
            {
              "schema": "opure.project-settings/1",
              "project_id": "{{projectId}}",
              "settings": {
                "logging.level.default": "{{logLevel}}"
              }
            }
            """;
        File.WriteAllText(
            settingsPath,
            json,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static int HandleVersion()
    {
        Console.WriteLine("Opure CLI v0.1.0-preview.0");
        return 0;
    }

    private static int HandleUnknownCommand(string command)
    {
        Console.Error.WriteLine($"Unknown command: {command}");
        return 1;
    }

    private static void WriteUsage()
    {
        Console.WriteLine("Opure CLI");
        Console.WriteLine("Commands: health, version, project open|list, recovery create|list|show, license apply");
        Console.WriteLine("  opure project open --channel Development|Preview|Stable --path-stdin");
        Console.WriteLine("  opure project list [--channel Development|Preview|Stable]");
        Console.WriteLine("  opure recovery create [--channel Development|Preview|Stable]");
        Console.WriteLine("  opure recovery list [--channel Development|Preview|Stable]");
        Console.WriteLine("  opure recovery show --id <guid> [--channel Development|Preview|Stable]");
        Console.WriteLine("  opure license apply <key>");
    }

    private static async Task<int> HandleProjectAsync(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("A project subcommand is required: open or list.");
            return 1;
        }

        if (!TryParseProjectArguments(
                args[1..],
                out string channel,
                out bool readPathFromStandardInput,
                out string? argumentError))
        {
            Console.Error.WriteLine(argumentError);
            return 1;
        }

        if (!TryGetRuntimeEndpoint(
                out RuntimeHealthEndpoint? endpoint,
                out RuntimeHealthSessionMaterial? sessionMaterial))
        {
            Console.Error.WriteLine(
                "Project request failed: no bounded Opure Runtime session is available.");
            return 1;
        }

        try
        {
            return args[0].ToLowerInvariant() switch
            {
                "open" => await OpenProjectAsync(
                    endpoint,
                    sessionMaterial,
                    channel,
                    readPathFromStandardInput,
                    suppliedPath: null),
                "list" when !readPathFromStandardInput =>
                    await ListProjectsAsync(endpoint, sessionMaterial, channel),
                "list" => InvalidProjectListArguments(),
                _ => HandleUnknownProjectCommand(args[0])
            };
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("The Project request was cancelled.");
            return 1;
        }
        catch (Exception exception) when (
            exception is WindowsPathReferenceException or
                ProjectOpenTransportException or
                ProjectListTransportException or
                IOException or
                UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Project request failed: {exception.Message}");
            return 1;
        }
    }

    private static async Task<int> OpenProjectAsync(
        RuntimeHealthEndpoint endpoint,
        RuntimeHealthSessionMaterial sessionMaterial,
        string channel,
        bool readPathFromStandardInput,
        string? suppliedPath)
    {
        if (!readPathFromStandardInput)
        {
            Console.Error.WriteLine(
                "Project open requires --path-stdin so the absolute path is not exposed in the command line.");
            return 1;
        }

        if (!OperatingSystem.IsWindows())
        {
            Console.Error.WriteLine("Project open currently requires Windows.");
            return 1;
        }

        string? path = suppliedPath ??
            await Console.In.ReadLineAsync().ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path))
        {
            Console.Error.WriteLine("Project open requires one absolute path on standard input.");
            return 1;
        }

        VerifiedWorkspaceRootReference root =
            WindowsPathReferenceResolver.AcquireRoot(new UntrustedPathText(path));
        OpenProjectRequest request = new()
        {
            MinimumContractRevision = ProjectOpenContractPolicy.CurrentRevision,
            MaximumContractRevision = ProjectOpenContractPolicy.CurrentRevision,
            OperationId = Guid.NewGuid().ToString("N"),
            CorrelationId = Guid.NewGuid().ToString("N"),
            ReleaseChannel = ParseProjectOpenChannel(channel),
            DisplayName = "Selected project",
            Root = new ProjectRootIdentityClaim
            {
                DisplayPath = root.DisplayPath,
                VolumeClass = ToWireVolumeClass(root.VolumeClass),
                VolumeSerialNumber = root.RootIdentity.VolumeSerialNumber,
                FileId = root.RootIdentity.FileId,
                IdentityCapability = WireFileIdentityCapability.WindowsFileId128
            }
        };

        await using NamedPipeProjectOpenClient client = new(
            endpoint,
            sessionMaterial);
        OpenProjectResponse response = await client.OpenProjectAsync(
            request,
            ProjectOpenContractPolicy.DefaultDeadline,
            CancellationToken.None).ConfigureAwait(false);
        if (response.OutcomeCase == OpenProjectResponse.OutcomeOneofCase.Error)
        {
            Console.Error.WriteLine(
                $"Project open failed: {response.Error.Code} - {response.Error.SafeMessage}");
            return 1;
        }

        Console.WriteLine($"Project ID: {response.Project.ProjectId}");
        Console.WriteLine($"Disposition: {response.Project.Disposition}");
        Console.WriteLine($"Lifecycle: {response.Project.LifecycleState}");
        Console.WriteLine($"Root volume class: {response.Project.RootVolumeClass}");
        Console.WriteLine(
            $"Initial Workspace Snapshot: {response.Project.InitialSnapshotState}");
        Console.WriteLine(
            $"Workspace generation: {response.Project.InitialWorkspaceGeneration}");
        Console.WriteLine(
            $"Workspace generation SHA-256: {response.Project.InitialWorkspaceGenerationSha256}");
        return 0;
    }

    private static async Task<int> ListProjectsAsync(
        RuntimeHealthEndpoint endpoint,
        RuntimeHealthSessionMaterial sessionMaterial,
        string channel)
    {
        ListProjectsRequest request = new()
        {
            MinimumContractRevision = ProjectListContractPolicy.CurrentRevision,
            MaximumContractRevision = ProjectListContractPolicy.CurrentRevision,
            CorrelationId = Guid.NewGuid().ToString("N"),
            ReleaseChannel = ParseProjectListChannel(channel)
        };
        await using NamedPipeProjectListClient client = new(
            endpoint,
            sessionMaterial);
        ListProjectsResponse response = await client.ListAsync(
            request,
            CancellationToken.None).ConfigureAwait(false);
        if (response.Error is not null)
        {
            Console.Error.WriteLine(
                $"Project list failed: {response.Error.Code} - {response.Error.SafeMessage}");
            return 1;
        }

        foreach (ProjectListItem project in response.Projects)
        {
            Console.WriteLine(
                $"{project.ProjectId} Repository: {project.RepositoryClass} Availability: {project.Availability}");
        }

        return 0;
    }

    private static bool TryParseProjectArguments(
        string[] args,
        out string channel,
        out bool readPathFromStandardInput,
        [NotNullWhen(false)] out string? error)
    {
        channel = "Development";
        readPathFromStandardInput = false;
        error = null;

        for (int index = 0; index < args.Length; index++)
        {
            if (string.Equals(args[index], "--path-stdin", StringComparison.Ordinal))
            {
                readPathFromStandardInput = true;
                continue;
            }

            if (!string.Equals(args[index], "--channel", StringComparison.Ordinal) ||
                index + 1 >= args.Length)
            {
                error = $"Unknown or incomplete project option: {args[index]}";
                return false;
            }

            channel = args[++index];
            if (channel is not ("Development" or "Preview" or "Stable"))
            {
                error = "Channel must be Development, Preview or Stable.";
                return false;
            }
        }

        return true;
    }

    private static ProjectOpenChannel ParseProjectOpenChannel(string channel) =>
        channel switch
        {
            "Development" => ProjectOpenChannel.Development,
            "Preview" => ProjectOpenChannel.Preview,
            "Stable" => ProjectOpenChannel.Stable,
            _ => throw new ArgumentOutOfRangeException(
                nameof(channel),
                channel,
                "The release channel is unsupported.")
        };

    private static ProjectListChannel ParseProjectListChannel(string channel) =>
        channel switch
        {
            "Development" => ProjectListChannel.Development,
            "Preview" => ProjectListChannel.Preview,
            "Stable" => ProjectListChannel.Stable,
            _ => throw new ArgumentOutOfRangeException(
                nameof(channel),
                channel,
                "The release channel is unsupported.")
        };

    private static WireVolumeClass ToWireVolumeClass(
        DomainVolumeClass volumeClass) => volumeClass switch
        {
            DomainVolumeClass.FixedLocal => WireVolumeClass.FixedLocal,
            DomainVolumeClass.Removable => WireVolumeClass.Removable,
            DomainVolumeClass.Network => WireVolumeClass.Network,
            DomainVolumeClass.Unsupported => WireVolumeClass.Unsupported,
            _ => throw new ArgumentOutOfRangeException(
                nameof(volumeClass),
                volumeClass,
                "The volume class is unsupported.")
        };

    private static int HandleUnknownProjectCommand(string command)
    {
        Console.Error.WriteLine($"Unknown project command: {command}");
        return 1;
    }

    private static int InvalidProjectListArguments()
    {
        Console.Error.WriteLine("Project list does not accept --path-stdin.");
        return 1;
    }

    private static async Task<int> HandleRecoveryAsync(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("A recovery subcommand is required: create, list or show.");
            return 1;
        }

        if (!TryParseRecoveryArguments(
                args[1..],
                out string channel,
                out Guid? recoveryPointId,
                out string? argumentError))
        {
            Console.Error.WriteLine(argumentError);
            return 1;
        }

        if (!TryGetRuntimeEndpoint(
                out RuntimeHealthEndpoint? endpoint,
                out RuntimeHealthSessionMaterial? sessionMaterial))
        {
            Console.Error.WriteLine("Error: Could not locate a running Opure Runtime in this environment.");
            Console.Error.WriteLine("Run this command in the bounded Opure session created by Bootstrap.");
            return 1;
        }

        try
        {
            await using NamedPipeRecoveryPointClient client = new(endpoint, sessionMaterial);
            return args[0].ToLowerInvariant() switch
            {
                "create" => await CreateRecoveryPointAsync(client, channel),
                "list" => await ListRecoveryPointsAsync(client, channel),
                "show" => await ShowRecoveryPointAsync(client, channel, recoveryPointId),
                _ => HandleUnknownRecoveryCommand(args[0])
            };
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("The recovery request was cancelled.");
            return 1;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Recovery request failed: {exception.Message}");
            return 1;
        }
    }

    private static async Task<int> CreateRecoveryPointAsync(
        NamedPipeRecoveryPointClient client,
        string channel)
    {
        CreateRecoveryPointResponseMessage response = await client.CreateRecoveryPointAsync(
            new CreateRecoveryPointRequestMessage
            {
                ContractRevision = RecoveryPointContractPolicy.CurrentRevision,
                ReleaseChannel = channel
            },
            CancellationToken.None);

        if (!response.IsSuccess)
        {
            Console.Error.WriteLine($"Recovery point creation failed: {response.ErrorMessage}");
            return 1;
        }

        Console.WriteLine($"Created local recovery point {response.RecoveryPointId}.");
        Console.WriteLine("Scope: Same-device recovery only");
        return 0;
    }

    private static async Task<int> ListRecoveryPointsAsync(
        NamedPipeRecoveryPointClient client,
        string channel)
    {
        ListRecoveryPointsResponseMessage response = await QueryRecoveryPointsAsync(client, channel);
        if (response.Points.Count == 0)
        {
            Console.WriteLine("No local recovery points found.");
            return 0;
        }

        Console.WriteLine("ID                                   Created (local)             Structural verification");
        foreach (RecoveryPointSummaryMessage point in response.Points)
        {
            DateTimeOffset created = DateTimeOffset.FromUnixTimeMilliseconds(
                point.CreatedAtUnixTimeMilliseconds).ToLocalTime();
            Console.WriteLine($"{point.RecoveryPointId,-36} {created:yyyy-MM-dd HH:mm:ss zzz} {point.VerificationState}");
        }

        Console.WriteLine("Scope: Same-device recovery only");
        return 0;
    }

    private static async Task<int> ShowRecoveryPointAsync(
        NamedPipeRecoveryPointClient client,
        string channel,
        Guid? recoveryPointId)
    {
        if (recoveryPointId is null)
        {
            Console.Error.WriteLine("The show command requires --id <guid>.");
            return 1;
        }

        ListRecoveryPointsResponseMessage response = await QueryRecoveryPointsAsync(client, channel);
        RecoveryPointSummaryMessage? point = response.Points.FirstOrDefault(candidate =>
            Guid.TryParse(candidate.RecoveryPointId, out Guid candidateId) &&
            candidateId == recoveryPointId.Value);
        if (point is null)
        {
            Console.Error.WriteLine($"Recovery point {recoveryPointId.Value:D} was not found.");
            return 1;
        }

        DateTimeOffset created = DateTimeOffset.FromUnixTimeMilliseconds(
            point.CreatedAtUnixTimeMilliseconds).ToLocalTime();
        Console.WriteLine($"Recovery point ID: {recoveryPointId.Value:D}");
        Console.WriteLine($"Created: {created:O}");
        Console.WriteLine($"Channel: {channel}");
        Console.WriteLine($"Product version: {point.ProductVersion}");
        Console.WriteLine($"Required owners: {point.OwnerCount}");
        Console.WriteLine($"Schema versions: {string.Join(", ", point.SupportedSchemaVersions)}");
        Console.WriteLine($"Checkpoint hashes: {point.CheckpointHashes.Count}");
        Console.WriteLine($"Structural verification: {point.VerificationState}");
        Console.WriteLine("Creation and verification receipts:");
        foreach (RecoveryPointReceiptMessage receipt in point.Receipts)
        {
            DateTimeOffset receiptTimestamp = DateTimeOffset.FromUnixTimeMilliseconds(
                receipt.TimestampUnixTimeMilliseconds).ToLocalTime();
            Console.WriteLine($"  {receipt.EventType} at {receiptTimestamp:O}: {receipt.StatusMessage}");
        }

        Console.WriteLine("Scope: Same-device recovery only");
        return 0;
    }

    private static Task<ListRecoveryPointsResponseMessage> QueryRecoveryPointsAsync(
        NamedPipeRecoveryPointClient client,
        string channel) =>
        client.ListRecoveryPointsAsync(
            new ListRecoveryPointsRequestMessage
            {
                ContractRevision = RecoveryPointContractPolicy.CurrentRevision,
                ReleaseChannel = channel
            },
            CancellationToken.None);

    private static int HandleUnknownRecoveryCommand(string command)
    {
        Console.Error.WriteLine($"Unknown recovery command: {command}");
        return 1;
    }

    private static bool TryParseRecoveryArguments(
        string[] args,
        out string channel,
        out Guid? recoveryPointId,
        [NotNullWhen(false)] out string? error)
    {
        channel = "Development";
        recoveryPointId = null;
        error = null;

        for (int index = 0; index < args.Length; index += 2)
        {
            if (index + 1 >= args.Length)
            {
                error = $"Missing value for {args[index]}.";
                return false;
            }

            string value = args[index + 1];
            switch (args[index])
            {
                case "--channel":
                    if (value is not ("Development" or "Preview" or "Stable"))
                    {
                        error = "Channel must be Development, Preview or Stable.";
                        return false;
                    }

                    channel = value;
                    break;
                case "--id":
                    if (!Guid.TryParse(value, out Guid parsedId))
                    {
                        error = "Recovery point ID must be a GUID.";
                        return false;
                    }

                    recoveryPointId = parsedId;
                    break;
                default:
                    error = $"Unknown recovery option: {args[index]}";
                    return false;
            }
        }

        return true;
    }

    private static async Task<int> HandleHealthAsync()
    {
        if (!TryGetRuntimeEndpoint(out RuntimeHealthEndpoint? endpoint, out RuntimeHealthSessionMaterial? sessionMaterial))
        {
            Console.Error.WriteLine("Error: Could not locate a running Opure Runtime in this environment.");
            Console.Error.WriteLine("Ensure the CLI is running within a valid Opure Session.");
            return 1;
        }

        try
        {
            await using NamedPipeRuntimeHealthClient client = new(endpoint, sessionMaterial);
            GetRuntimeHealthRequest request = new()
            {
                MinimumContractRevision = RuntimeHealthContractPolicy.CurrentRevision,
                MaximumContractRevision = RuntimeHealthContractPolicy.CurrentRevision,
                QueryId = Guid.NewGuid().ToString("N")
            };

            using CancellationTokenSource cts = new(RuntimeHealthContractPolicy.DefaultDeadline);
            GetRuntimeHealthResponse response = await client.GetRuntimeHealthAsync(
                request,
                RuntimeHealthContractPolicy.DefaultDeadline,
                cts.Token);

            if (response.OutcomeCase == GetRuntimeHealthResponse.OutcomeOneofCase.Health)
            {
                var health = response.Health;
                Console.WriteLine($"Runtime Status: {health.OverallHealth}");
                Console.WriteLine($"Readiness: {health.Readiness}");
                Console.WriteLine($"Mode: {health.RuntimeMode}");
                Console.WriteLine($"Product Version: {health.ProductVersion}");
                Console.WriteLine($"Boot ID: {health.RuntimeBootId}");
                Console.WriteLine($"Services: {health.Services.Count}");
                foreach (var service in health.Services)
                {
                    Console.WriteLine($"  - {service.ServiceId}: {service.State}");
                }
                return 0;
            }
            else if (response.OutcomeCase == GetRuntimeHealthResponse.OutcomeOneofCase.Error)
            {
                Console.Error.WriteLine($"Runtime returned an error: {response.Error.Code} - {response.Error.SafeMessage}");
                return 1;
            }

            Console.Error.WriteLine("Runtime returned an unknown outcome.");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to connect to Runtime: {ex.Message}");
            return 1;
        }
    }

    private static bool TryGetRuntimeEndpoint(
        [NotNullWhen(true)] out RuntimeHealthEndpoint? endpoint,
        [NotNullWhen(true)] out RuntimeHealthSessionMaterial? sessionMaterial)
    {
        string? pipeName = Environment.GetEnvironmentVariable("OPURE_RUNTIME_PIPE_NAME");
        string? bootId = Environment.GetEnvironmentVariable("OPURE_RUNTIME_BOOT_ID");
        string? sessionId = Environment.GetEnvironmentVariable("OPURE_BOOTSTRAP_SESSION_ID");
        string? sessionSecret = Environment.GetEnvironmentVariable("OPURE_BOOTSTRAP_SESSION_SECRET");

        if (string.IsNullOrWhiteSpace(pipeName) || string.IsNullOrWhiteSpace(bootId))
        {
            endpoint = null;
            sessionMaterial = null;
            return false;
        }

        endpoint = new RuntimeHealthEndpoint(pipeName, bootId);

        if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(sessionSecret))
        {
            sessionMaterial = null;
            return false;
        }

        sessionMaterial = new RuntimeHealthSessionMaterial(sessionId, sessionSecret);
        return true;
    }
}
