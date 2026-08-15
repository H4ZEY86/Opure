using System;
using System.Security.Cryptography;
using System.Text;

namespace Opure.TrustEvidence.Contracts;

public sealed class CommandApproval
{
    public CommandApproval(
        string templateHash,
        string canonicalArguments,
        string workspaceSnapshotId,
        string executableAbsolutePath,
        string targetDirectory,
        string environmentPolicyJson,
        string resourceBudgetClass,
        string effectIntent,
        DateTimeOffset approvalTimestampUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(canonicalArguments);
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceSnapshotId);
        ArgumentException.ThrowIfNullOrWhiteSpace(executableAbsolutePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentPolicyJson);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceBudgetClass);
        ArgumentException.ThrowIfNullOrWhiteSpace(effectIntent);

        TemplateHash = templateHash;
        CanonicalArguments = canonicalArguments;
        WorkspaceSnapshotId = workspaceSnapshotId;
        ExecutableAbsolutePath = executableAbsolutePath;
        TargetDirectory = targetDirectory;
        EnvironmentPolicyJson = environmentPolicyJson;
        ResourceBudgetClass = resourceBudgetClass;
        EffectIntent = effectIntent;
        ApprovalTimestampUtc = approvalTimestampUtc;

        string input = $"{templateHash}:{canonicalArguments}:{workspaceSnapshotId}";
        Id = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(input)));
    }

    public string Id { get; }
    public string TemplateHash { get; }
    public string CanonicalArguments { get; }
    public string WorkspaceSnapshotId { get; }
    public string ExecutableAbsolutePath { get; }
    public string TargetDirectory { get; }
    public string EnvironmentPolicyJson { get; }
    public string ResourceBudgetClass { get; }
    public string EffectIntent { get; }
    public DateTimeOffset ApprovalTimestampUtc { get; }
}
