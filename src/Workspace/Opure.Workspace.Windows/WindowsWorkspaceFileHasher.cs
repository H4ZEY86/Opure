using System.Buffers;
using System.Globalization;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using Opure.Filesystem.Contracts;
using Opure.Filesystem.Windows;
using Opure.Workspace.Contracts;

namespace Opure.Workspace.Windows;

[SupportedOSPlatform("windows")]
public sealed class WindowsWorkspaceFileHasher
{
    public const string Algorithm = "SHA-256";
    public const int AlgorithmVersion = 1;

    internal Action<int, string>? BeforeAttempt { get; init; }

    internal Action<int, long>? AfterChunkRead { get; init; }

    internal Action<int>? AfterContentRead { get; init; }

    public async ValueTask<WorkspaceFileHashResult> HashAsync(
        VerifiedWorkspaceRootReference root,
        WorkspaceInventoryEntry entry,
        WorkspaceFileHashPolicy? policy = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(entry);
        WorkspaceFileHashPolicy selected = policy ?? WorkspaceFileHashPolicy.Default;
        ValidatePolicy(selected);
        cancellationToken.ThrowIfCancellationRequested();

        if (entry.EntryClass != WorkspaceInventoryEntryClass.RegularFile ||
            entry.Disposition != WorkspaceInventoryDisposition.Included)
        {
            return CreateResult(
                entry,
                WorkspaceFileHashDisposition.Excluded,
                "INVENTORY_ENTRY_NOT_HASH_ELIGIBLE",
                "The inventory entry is not an included regular file.",
                contentHash: string.Empty,
                attempts: 0);
        }

        LogicalWorkspacePath logicalPath = LogicalWorkspacePath.Parse(
            new UntrustedPathText(entry.LogicalPath));

        for (int attempt = 1; attempt <= selected.MaximumAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            BeforeAttempt?.Invoke(attempt, logicalPath.Value);

            try
            {
                using VerifiedWindowsPathReference file =
                    WindowsPathReferenceResolver.ResolveFileForRead(root, logicalPath);
                WindowsResolvedPath before = file.Value;
                string identitySha256 = HashIdentity(before.Identity);

                if (!string.Equals(
                        entry.IdentitySha256,
                        identitySha256,
                        StringComparison.Ordinal))
                {
                    return CreateResult(
                        entry,
                        WorkspaceFileHashDisposition.Unstable,
                        "FILE_IDENTITY_CHANGED",
                        "The current file identity no longer matches the inventory entry.",
                        contentHash: string.Empty,
                        attempt,
                        identitySha256,
                        before.SizeBytes,
                        before.LastWriteTimeUtc);
                }

                if (before.SizeBytes > selected.MaximumFileSizeBytes)
                {
                    return CreateResult(
                        entry,
                        WorkspaceFileHashDisposition.Excluded,
                        "FILE_SIZE_LIMIT_EXCEEDED",
                        "The file exceeds the explicit Workspace hashing size limit.",
                        contentHash: string.Empty,
                        attempt,
                        identitySha256,
                        before.SizeBytes,
                        before.LastWriteTimeUtc);
                }

                byte[] buffer = ArrayPool<byte>.Shared.Rent(selected.BufferSizeBytes);

                try
                {
                    using IncrementalHash hash = IncrementalHash.CreateHash(
                        HashAlgorithmName.SHA256);
                    long offset = 0;

                    while (true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        int read = await file.ReadAsync(
                            buffer.AsMemory(0, selected.BufferSizeBytes),
                            offset,
                            cancellationToken).ConfigureAwait(false);

                        if (read == 0)
                        {
                            break;
                        }

                        offset = checked(offset + read);
                        if (offset > selected.MaximumFileSizeBytes)
                        {
                            return CreateResult(
                                entry,
                                WorkspaceFileHashDisposition.Excluded,
                                "FILE_SIZE_LIMIT_EXCEEDED",
                                "The file exceeded the explicit Workspace hashing size limit while being read.",
                                contentHash: string.Empty,
                                attempt,
                                identitySha256,
                                offset,
                                before.LastWriteTimeUtc);
                        }

                        hash.AppendData(buffer.AsSpan(0, read));
                        AfterChunkRead?.Invoke(attempt, offset);
                    }

                    AfterContentRead?.Invoke(attempt);
                    WindowsResolvedPath after =
                        WindowsPathReferenceResolver.RefreshMetadata(file);
                    WindowsPathReferenceResolver.Revalidate(root, file);

                    if (offset != before.SizeBytes ||
                        offset != after.SizeBytes ||
                        before.SizeBytes != after.SizeBytes ||
                        before.LastWriteTimeUtc != after.LastWriteTimeUtc)
                    {
                        if (attempt < selected.MaximumAttempts)
                        {
                            continue;
                        }

                        return CreateResult(
                            entry,
                            WorkspaceFileHashDisposition.Unstable,
                            "FILE_CHANGED_DURING_READ",
                            "The file size or modification state changed during hashing.",
                            contentHash: string.Empty,
                            attempt,
                            identitySha256,
                            after.SizeBytes,
                            after.LastWriteTimeUtc);
                    }

                    return CreateResult(
                        entry,
                        WorkspaceFileHashDisposition.Stable,
                        "FILE_HASH_STABLE",
                        "The content hash was produced from a stable verified file handle.",
                        Convert.ToHexStringLower(hash.GetHashAndReset()),
                        attempt,
                        identitySha256,
                        after.SizeBytes,
                        after.LastWriteTimeUtc);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(buffer);
                    ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
                }
            }
            catch (OperationCanceledException) when (
                cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (WindowsPathReferenceException exception) when (
                IsUnstablePathFailure(exception))
            {
                if (attempt == selected.MaximumAttempts)
                {
                    return CreateResult(
                        entry,
                        WorkspaceFileHashDisposition.Unstable,
                        "FILE_PATH_CHANGED_DURING_HASH",
                        "The verified file path or identity changed during hashing.",
                        contentHash: string.Empty,
                        attempt);
                }
            }
            catch (Exception exception) when (IsUnreadableFailure(exception))
            {
                if (attempt == selected.MaximumAttempts)
                {
                    return CreateResult(
                        entry,
                        WorkspaceFileHashDisposition.Unreadable,
                        "FILE_CONTENT_UNREADABLE",
                        "The file could not be opened or read through the verified Workspace boundary.",
                        contentHash: string.Empty,
                        attempt);
                }
            }
        }

        throw new InvalidOperationException(
            "The bounded Workspace hashing attempt loop ended unexpectedly.");
    }

    private static WorkspaceFileHashResult CreateResult(
        WorkspaceInventoryEntry entry,
        WorkspaceFileHashDisposition disposition,
        string reason,
        string safeDetail,
        string contentHash,
        int attempts,
        string? identitySha256 = null,
        long? sizeBytes = null,
        DateTimeOffset? lastWriteTimeUtc = null)
    {
        return new WorkspaceFileHashResult(
            entry.LogicalPath,
            disposition,
            reason,
            safeDetail,
            Algorithm,
            AlgorithmVersion,
            contentHash,
            identitySha256 ?? entry.IdentitySha256,
            sizeBytes ?? entry.SizeBytes,
            lastWriteTimeUtc ?? entry.LastWriteTimeUtc,
            attempts);
    }

    private static string HashIdentity(FileObjectIdentity identity)
    {
        string material = string.Create(
            CultureInfo.InvariantCulture,
            $"opure-file-identity/1:{identity.VolumeSerialNumber:x16}:{identity.FileId}");
        return Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(material)));
    }

    private static bool IsUnstablePathFailure(
        WindowsPathReferenceException exception) =>
        exception.Failure is WindowsPathFailure.ReparsePointDenied or
            WindowsPathFailure.ContainmentFailed or
            WindowsPathFailure.IdentityChanged;

    private static bool IsUnreadableFailure(Exception exception) =>
        exception is WindowsPathReferenceException or
            IOException or
            UnauthorizedAccessException;

    private static void ValidatePolicy(WorkspaceFileHashPolicy policy)
    {
        if (policy.MaximumFileSizeBytes is < 1 or
                > WorkspaceSnapshotBounds.MaximumObservedBytes ||
            policy.BufferSizeBytes is < 4096 or > 1024 * 1024 ||
            policy.MaximumAttempts is < 1 or > 3)
        {
            throw new ArgumentOutOfRangeException(
                nameof(policy),
                "Workspace hashing limits exceed the owner contract.");
        }
    }
}
