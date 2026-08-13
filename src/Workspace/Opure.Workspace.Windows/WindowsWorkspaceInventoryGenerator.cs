using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using Opure.Filesystem.Contracts;
using Opure.Filesystem.Windows;
using Opure.Workspace.Contracts;

namespace Opure.Workspace.Windows;

[SupportedOSPlatform("windows")]
public sealed class WindowsWorkspaceInventoryGenerator
{
    private const string ProjectSettingsDirectory = ".opure";
    private const string ProjectSettingsFile = "project.settings.json";

    private static readonly ReadOnlySet<string> ExcludedDirectories =
        new(new HashSet<string>(
        [
            ".git",
            ".vs",
            ".idea",
            ".cache",
            ".gradle",
            ".mypy_cache",
            ".pytest_cache",
            ".venv",
            "__pycache__",
            "artifacts",
            "bin",
            "node_modules",
            "obj",
            "packages",
            "target",
            "venv"
        ], StringComparer.OrdinalIgnoreCase));

    private static readonly ReadOnlySet<string> CredentialDirectories =
        new(new HashSet<string>(
        [
            ".aws",
            ".azure",
            ".gnupg",
            ".kube",
            ".ssh"
        ], StringComparer.OrdinalIgnoreCase));

    private static readonly EnumerationOptions EnumerationPolicy = new()
    {
        RecurseSubdirectories = false,
        IgnoreInaccessible = false,
        ReturnSpecialDirectories = false,
        AttributesToSkip = 0,
        MatchCasing = MatchCasing.PlatformDefault,
        MatchType = MatchType.Simple
    };

    internal Action<string>? BeforeInspect { get; init; }

    public WorkspaceInventoryResult Generate(
        string projectId,
        string rootReferenceId,
        VerifiedWorkspaceRootReference root,
        WorkspaceInventoryPolicy? policy = null,
        CancellationToken cancellationToken = default)
    {
        ValidateId(projectId, nameof(projectId));
        ValidateId(rootReferenceId, nameof(rootReferenceId));
        ArgumentNullException.ThrowIfNull(root);
        WorkspaceInventoryPolicy selected = policy ??
            WorkspaceInventoryPolicy.Default;
        ValidatePolicy(selected);
        cancellationToken.ThrowIfCancellationRequested();

        long started = Stopwatch.GetTimestamp();
        List<WorkspaceInventoryEntry> entries = [];
        List<WorkspaceInventoryIssue> issues = [];
        Queue<PendingDirectory> pending = new();
        pending.Enqueue(new PendingDirectory(
            LogicalWorkspacePath.Parse(
                new UntrustedPathText(string.Empty),
                allowWorkspaceRoot: true),
            Depth: 0));
        int enumerated = 0;
        int directories = 0;
        bool entryLimit = false;
        bool directoryLimit = false;
        bool depthLimit = false;
        bool durationLimit = false;
        bool partial = false;

        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Stopwatch.GetElapsedTime(started) >= selected.MaximumDuration)
            {
                durationLimit = true;
                partial = true;
                break;
            }

            if (directories >= selected.MaximumDirectoryCount)
            {
                directoryLimit = true;
                partial = true;
                break;
            }

            PendingDirectory current = pending.Dequeue();
            directories++;
            string directoryPath;

            try
            {
                using VerifiedWindowsPathReference directory =
                    WindowsPathReferenceResolver.ResolveExisting(
                        root,
                        current.LogicalPath);
                directoryPath = directory.Value.DisplayPath;
            }
            catch (Exception exception) when (IsInventoryFailure(exception))
            {
                issues.Add(CreateIssue(
                    current.LogicalPath.Value,
                    string.Empty,
                    "DIRECTORY_CHANGED_DURING_SCAN",
                    "A directory changed or became unavailable during inventory generation."));
                partial = true;
                continue;
            }

            try
            {
                foreach (string candidatePath in Directory.EnumerateFileSystemEntries(
                             directoryPath,
                             "*",
                             EnumerationPolicy))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (Stopwatch.GetElapsedTime(started) >= selected.MaximumDuration)
                    {
                        durationLimit = true;
                        partial = true;
                        break;
                    }

                    if (enumerated >= selected.MaximumEntryCount)
                    {
                        entryLimit = true;
                        partial = true;
                        break;
                    }

                    enumerated++;
                    ProcessEntry(
                        root,
                        current,
                        candidatePath,
                        selected,
                        entries,
                        issues,
                        pending,
                        ref partial,
                        ref depthLimit);
                }
            }
            catch (OperationCanceledException) when (
                cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (IsInventoryFailure(exception))
            {
                issues.Add(CreateIssue(
                    current.LogicalPath.Value,
                    string.Empty,
                    "DIRECTORY_ENUMERATION_CHANGED",
                    "A directory changed or could not be enumerated safely."));
                partial = true;
            }

            if (entryLimit || durationLimit)
            {
                break;
            }
        }

        if (AddLogicalPathCollisionIssues(entries, issues))
        {
            partial = true;
        }

        entries.Sort(static (left, right) =>
            StringComparer.Ordinal.Compare(left.LogicalPath, right.LogicalPath));
        issues.Sort(static (left, right) =>
        {
            int parent = StringComparer.Ordinal.Compare(
                left.ParentLogicalPath,
                right.ParentLogicalPath);
            return parent != 0
                ? parent
                : StringComparer.Ordinal.Compare(
                    left.EntryNameSha256,
                    right.EntryNameSha256);
        });

        return new WorkspaceInventoryResult(
            projectId,
            rootReferenceId,
            partial
                ? WorkspaceInventoryCompletion.Partial
                : WorkspaceInventoryCompletion.Complete,
            entries.AsReadOnly(),
            issues.AsReadOnly(),
            enumerated,
            directories,
            entryLimit,
            directoryLimit,
            depthLimit,
            durationLimit,
            Stopwatch.GetElapsedTime(started));
    }

    private static bool AddLogicalPathCollisionIssues(
        IReadOnlyList<WorkspaceInventoryEntry> entries,
        List<WorkspaceInventoryIssue> issues)
    {
        Dictionary<string, List<WorkspaceInventoryEntry>> groups =
            new(StringComparer.OrdinalIgnoreCase);
        foreach (WorkspaceInventoryEntry entry in entries)
        {
            string key = GetPortableLogicalPathKey(entry.LogicalPath);
            if (!groups.TryGetValue(key, out List<WorkspaceInventoryEntry>? group))
            {
                group = [];
                groups.Add(key, group);
            }

            group.Add(entry);
        }

        bool collisionDetected = false;
        foreach (List<WorkspaceInventoryEntry> group in groups.Values)
        {
            if (group.Count < 2)
            {
                continue;
            }

            collisionDetected = true;
            foreach (WorkspaceInventoryEntry entry in group)
            {
                issues.Add(CreateIssue(
                    string.Empty,
                    HashText(entry.LogicalPath),
                    "LOGICAL_PATH_COLLISION",
                    "Two or more entries collide under case-insensitive Unicode-normalised comparison."));
            }
        }

        return collisionDetected;
    }

    internal static bool HasPortableLogicalPathCollision(
        IEnumerable<string> logicalPaths)
    {
        ArgumentNullException.ThrowIfNull(logicalPaths);
        HashSet<string> keys = new(StringComparer.OrdinalIgnoreCase);
        foreach (string logicalPath in logicalPaths)
        {
            ArgumentNullException.ThrowIfNull(logicalPath);
            if (!keys.Add(GetPortableLogicalPathKey(logicalPath)))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetPortableLogicalPathKey(string logicalPath) =>
        logicalPath.Normalize(NormalizationForm.FormC);

    private void ProcessEntry(
        VerifiedWorkspaceRootReference root,
        PendingDirectory parent,
        string candidatePath,
        WorkspaceInventoryPolicy policy,
        List<WorkspaceInventoryEntry> entries,
        List<WorkspaceInventoryIssue> issues,
        Queue<PendingDirectory> pending,
        ref bool partial,
        ref bool depthLimit)
    {
        string leafName = Path.GetFileName(candidatePath);
        LogicalWorkspacePath logicalPath;

        try
        {
            LogicalWorkspacePath.ValidateLeafName(
                leafName,
                nameof(candidatePath));
            logicalPath = BuildLogicalPath(parent.LogicalPath, leafName);
        }
        catch (ArgumentException)
        {
            issues.Add(CreateIssue(
                parent.LogicalPath.Value,
                HashText(leafName),
                "ENTRY_NAME_UNSUPPORTED",
                "An entry name cannot be represented as a safe logical Workspace path."));
            partial = true;
            return;
        }

        try
        {
            BeforeInspect?.Invoke(logicalPath.Value);
            using VerifiedWindowsPathReference verified =
                WindowsPathReferenceResolver.InspectExisting(root, logicalPath);
            WindowsResolvedPath value = verified.Value;
            bool hidden =
                (value.Attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0;
            WorkspaceInventoryDisposition disposition;
            string reason;
            string reparseClass = string.Empty;
            WorkspaceInventoryEntryClass entryClass;

            if (value.ObjectType == FilesystemObjectType.ReparsePoint)
            {
                entryClass = WorkspaceInventoryEntryClass.ReparsePoint;
                disposition = WorkspaceInventoryDisposition.Denied;
                reason = "REPARSE_TRAVERSAL_DENIED";
                reparseClass = value.ReparseKind.ToString();
            }
            else if (value.ObjectType == FilesystemObjectType.Directory)
            {
                entryClass = WorkspaceInventoryEntryClass.Directory;
                reason = GetDirectoryExclusion(parent.LogicalPath, leafName);
                disposition = reason.Length == 0
                    ? WorkspaceInventoryDisposition.Included
                    : WorkspaceInventoryDisposition.Excluded;

                if (disposition == WorkspaceInventoryDisposition.Included)
                {
                    if (parent.Depth >= policy.MaximumDepth)
                    {
                        disposition = WorkspaceInventoryDisposition.Excluded;
                        reason = "TRAVERSAL_DEPTH_LIMIT_REACHED";
                        depthLimit = true;
                        partial = true;
                    }
                    else
                    {
                        pending.Enqueue(new PendingDirectory(
                            logicalPath,
                            parent.Depth + 1));
                    }
                }
            }
            else
            {
                entryClass = WorkspaceInventoryEntryClass.RegularFile;
                reason = GetFileExclusion(parent.LogicalPath, leafName);
                disposition = reason.Length == 0
                    ? WorkspaceInventoryDisposition.Included
                    : WorkspaceInventoryDisposition.Excluded;
            }

            entries.Add(new WorkspaceInventoryEntry(
                logicalPath.Value,
                entryClass,
                disposition,
                hidden,
                value.SizeBytes,
                value.LastWriteTimeUtc,
                HashIdentity(value.Identity),
                reason,
                reparseClass));
        }
        catch (Exception exception) when (IsInventoryFailure(exception))
        {
            issues.Add(CreateIssue(
                parent.LogicalPath.Value,
                HashText(leafName),
                "ENTRY_CHANGED_DURING_SCAN",
                "An entry changed or became unavailable before its identity could be verified."));
            partial = true;
        }
    }

    private static string GetDirectoryExclusion(
        LogicalWorkspacePath parent,
        string leafName)
    {
        if (string.Equals(
                parent.Value,
                ProjectSettingsDirectory,
                StringComparison.OrdinalIgnoreCase))
        {
            return "OPURE_PRIVATE_DIRECTORY_EXCLUDED";
        }

        if (CredentialDirectories.Contains(leafName))
        {
            return "KNOWN_CREDENTIAL_STORE_EXCLUDED";
        }

        return ExcludedDirectories.Contains(leafName)
            ? "BUILT_IN_DIRECTORY_EXCLUDED"
            : string.Empty;
    }

    internal static LogicalWorkspacePath BuildLogicalPath(
        LogicalWorkspacePath parent,
        string leafName)
    {
        ArgumentNullException.ThrowIfNull(parent);
        LogicalWorkspacePath.ValidateLeafName(leafName, nameof(leafName));
        string value = parent.IsWorkspaceRoot
            ? leafName
            : string.Concat(parent.Value, "/", leafName);
        return LogicalWorkspacePath.Parse(new UntrustedPathText(value));
    }

    private static string GetFileExclusion(
        LogicalWorkspacePath parent,
        string leafName)
    {
        if (string.Equals(
                parent.Value,
                ProjectSettingsDirectory,
                StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(
                leafName,
                ProjectSettingsFile,
                StringComparison.OrdinalIgnoreCase))
        {
            return "OPURE_PRIVATE_FILE_EXCLUDED";
        }

        return leafName.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase) ||
            leafName.EndsWith(".temp", StringComparison.OrdinalIgnoreCase) ||
            leafName.StartsWith('~')
                ? "TEMPORARY_FILE_EXCLUDED"
                : string.Empty;
    }

    private static WorkspaceInventoryIssue CreateIssue(
        string parentLogicalPath,
        string entryNameSha256,
        string stableCode,
        string safeDetail)
    {
        return new WorkspaceInventoryIssue(
            parentLogicalPath,
            entryNameSha256,
            stableCode,
            safeDetail);
    }

    private static string HashIdentity(FileObjectIdentity identity)
    {
        string material = string.Create(
            CultureInfo.InvariantCulture,
            $"opure-file-identity/1:{identity.VolumeSerialNumber:x16}:{identity.FileId}");
        return HashText(material);
    }

    private static string HashText(string value)
    {
        return Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    private static bool IsInventoryFailure(Exception exception) =>
        exception is WindowsPathReferenceException or
            IOException or
            UnauthorizedAccessException or
            DirectoryNotFoundException or
            FileNotFoundException;

    private static void ValidateId(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Length != 32 || value.Any(static character =>
                !char.IsAsciiDigit(character) &&
                character is not (>= 'a' and <= 'f')))
        {
            throw new ArgumentException(
                "A Workspace inventory authority must be a lower-case hexadecimal identity.",
                parameterName);
        }
    }

    private static void ValidatePolicy(WorkspaceInventoryPolicy policy)
    {
        if (policy.MaximumEntryCount is < 1 or > WorkspaceSnapshotBounds.MaximumFileCount ||
            policy.MaximumDirectoryCount is < 1 or > WorkspaceSnapshotBounds.MaximumFileCount ||
            policy.MaximumDepth is < 1 or > LogicalWorkspacePath.MaximumSegments ||
            policy.MaximumDuration <= TimeSpan.Zero ||
            policy.MaximumDuration > WorkspaceSnapshotBounds.MaximumDuration ||
            !Enum.IsDefined(policy.HiddenEntryPolicy))
        {
            throw new ArgumentOutOfRangeException(
                nameof(policy),
                "Workspace inventory limits exceed the owner contract.");
        }
    }

    private sealed record PendingDirectory(
        LogicalWorkspacePath LogicalPath,
        int Depth);
}
