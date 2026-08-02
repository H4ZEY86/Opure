using System.Globalization;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using LibGit2Sharp;
using Opure.Filesystem.Contracts;
using Opure.Filesystem.Windows;
using Opure.Repository.Contracts;

namespace Opure.Repository.Git;

[SupportedOSPlatform("windows")]
public sealed class GitRepositoryIdentityDetector : IRepositoryIdentityDetector
{
    public RepositoryObservation Observe(
        RepositoryDetectionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        VerifiedWorkspaceRootReference projectRoot =
            WindowsPathReferenceResolver.AcquireRoot(
                new UntrustedPathText(request.DisplayPath));

        if (!projectRoot.RootIdentity.IsSameObject(request.RootIdentity))
        {
            return RepositoryObservation.Degraded(
                "REPOSITORY_PROJECT_ROOT_CHANGED",
                "The verified project root changed before repository observation; project access remains available.");
        }

        string root = Path.GetFullPath(projectRoot.DisplayPath);
        string dotGit = Path.Combine(root, ".git");

        if (!Directory.Exists(dotGit) && !File.Exists(dotGit))
        {
            return HasRepositoryAbove(root)
                ? RepositoryObservation.Degraded(
                    "REPOSITORY_ROOT_OUTSIDE_PROJECT",
                    "Git metadata resolves outside the verified project boundary; project access remains available.")
                : RepositoryObservation.NotDetected();
        }

        try
        {
            string metadataPath = ResolveMetadataPath(root, dotGit);

            if (!IsWithin(root, metadataPath))
            {
                return RepositoryObservation.Degraded(
                    "REPOSITORY_METADATA_OUTSIDE_PROJECT",
                    "Git administrative data resolves outside the verified project boundary; project access remains available.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            VerifiedWorkspaceRootReference metadataRoot =
                WindowsPathReferenceResolver.AcquireRoot(
                    new UntrustedPathText(metadataPath));
            string identity = CreateIdentity(metadataRoot.RootIdentity);
            using LibGit2Sharp.Repository repository = new(root);
            RepositoryStatus status = repository.RetrieveStatus(
                new StatusOptions
                {
                    IncludeUntracked = true,
                    RecurseUntrackedDirs = true,
                    ExcludeSubmodules = true
                });
            RepositoryWorkingTreeSummary summary = Summarise(status);
            bool detached = repository.Info.IsHeadDetached;
            string? head = repository.Head.Tip?.Id.Sha;
            string? branch = detached ? null : repository.Head.FriendlyName;
            (int remoteCount, string? remoteFingerprint) =
                ReadRemoteMetadata(metadataPath);
            RepositoryObservationState state = summary.Conflicted > 0
                ? RepositoryObservationState.Conflicted
                : detached
                    ? RepositoryObservationState.Detached
                    : summary.IsDirty
                        ? RepositoryObservationState.Dirty
                        : RepositoryObservationState.Ready;

            return new RepositoryObservation(
                "git",
                state,
                identity,
                head,
                branch,
                remoteFingerprint,
                remoteCount,
                summary,
                "REPOSITORY_OBSERVED",
                "Git repository identity and local working-tree state were observed without repository-write authority.");
        }
        catch (OperationCanceledException) when (
            cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is RepositoryNotFoundException or
                LibGit2SharpException or
                IOException or
                UnauthorizedAccessException or
                FormatException)
        {
            return RepositoryObservation.Degraded(
                "REPOSITORY_METADATA_DEGRADED",
                "Repository metadata could not be read safely; project access remains available.");
        }
    }

    private static string ResolveMetadataPath(string root, string dotGit)
    {
        if (Directory.Exists(dotGit))
        {
            return Path.GetFullPath(dotGit);
        }

        string pointer = File.ReadAllText(dotGit);

        if (!pointer.StartsWith("gitdir:", StringComparison.OrdinalIgnoreCase))
        {
            throw new FormatException("The Git metadata pointer is malformed.");
        }

        string value = pointer[7..].Trim();

        if (value.Length == 0)
        {
            throw new FormatException("The Git metadata pointer is empty.");
        }

        return Path.GetFullPath(
            Path.IsPathFullyQualified(value)
                ? value
                : Path.Combine(root, value));
    }

    private static bool HasRepositoryAbove(string root)
    {
        DirectoryInfo? parent = Directory.GetParent(root);

        while (parent is not null)
        {
            if (Directory.Exists(Path.Combine(parent.FullName, ".git")) ||
                File.Exists(Path.Combine(parent.FullName, ".git")))
            {
                return true;
            }

            parent = parent.Parent;
        }

        return false;
    }

    private static bool IsWithin(string root, string candidate)
    {
        string rootWithSeparator = Path.TrimEndingDirectorySeparator(root) +
            Path.DirectorySeparatorChar;
        string fullCandidate = Path.GetFullPath(candidate);
        return fullCandidate.StartsWith(
            rootWithSeparator,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateIdentity(FileObjectIdentity identity)
    {
        string canonical = string.Create(
            CultureInfo.InvariantCulture,
            $"git\n{identity.VolumeSerialNumber}\n{identity.FileId}\n{identity.Capability}");
        return Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static RepositoryWorkingTreeSummary Summarise(
        RepositoryStatus status)
    {
        int modified = 0;
        int staged = 0;
        int untracked = 0;
        int deleted = 0;
        int renamed = 0;
        int conflicted = 0;

        foreach (StatusEntry entry in status)
        {
            FileStatus value = entry.State;
            modified += Has(value, FileStatus.ModifiedInWorkdir) ? 1 : 0;
            staged += Has(value, FileStatus.NewInIndex) ||
                Has(value, FileStatus.ModifiedInIndex) ||
                Has(value, FileStatus.DeletedFromIndex) ||
                Has(value, FileStatus.RenamedInIndex) ||
                Has(value, FileStatus.TypeChangeInIndex) ? 1 : 0;
            untracked += Has(value, FileStatus.NewInWorkdir) ? 1 : 0;
            deleted += Has(value, FileStatus.DeletedFromWorkdir) ||
                Has(value, FileStatus.DeletedFromIndex) ? 1 : 0;
            renamed += Has(value, FileStatus.RenamedInWorkdir) ||
                Has(value, FileStatus.RenamedInIndex) ? 1 : 0;
            conflicted += Has(value, FileStatus.Conflicted) ? 1 : 0;
        }

        return new RepositoryWorkingTreeSummary(
            modified,
            staged,
            untracked,
            deleted,
            renamed,
            conflicted);
    }

    private static bool Has(FileStatus value, FileStatus flag) =>
        (value & flag) != 0;

    private static (int Count, string? Fingerprint) ReadRemoteMetadata(
        string metadataPath)
    {
        string configPath = Path.Combine(metadataPath, "config");

        if (!File.Exists(configPath))
        {
            return (0, null);
        }

        List<string> canonicalRemotes = [];
        bool inRemote = false;

        foreach (string rawLine in File.ReadLines(configPath))
        {
            string line = rawLine.Trim();

            if (line.Length > 0 && line[0] == '[')
            {
                inRemote = line.StartsWith(
                    "[remote ",
                    StringComparison.OrdinalIgnoreCase);
                continue;
            }

            if (!inRemote || !line.StartsWith("url", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            int equals = line.IndexOf('=', StringComparison.Ordinal);

            if (equals < 0)
            {
                continue;
            }

            canonicalRemotes.Add(CanonicaliseRemote(line[(equals + 1)..].Trim()));
        }

        if (canonicalRemotes.Count == 0)
        {
            return (0, null);
        }

        canonicalRemotes.Sort(StringComparer.Ordinal);
        string joined = string.Join('\n', canonicalRemotes);
        return (
            canonicalRemotes.Count,
            Convert.ToHexStringLower(
                SHA256.HashData(Encoding.UTF8.GetBytes(joined))));
    }

    private static string CanonicaliseRemote(string value)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out Uri? uri))
        {
            UriBuilder builder = new(uri)
            {
                UserName = string.Empty,
                Password = string.Empty,
                Fragment = string.Empty,
                Query = string.Empty
            };
            return builder.Uri.GetComponents(
                    UriComponents.SchemeAndServer | UriComponents.Path,
                    UriFormat.SafeUnescaped)
                .TrimEnd('/')
                .ToLowerInvariant();
        }

        int at = value.LastIndexOf('@');
        string withoutUser = at >= 0 ? value[(at + 1)..] : value;
        return withoutUser.Replace('\\', '/').TrimEnd('/').ToLowerInvariant();
    }
}
