using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Opure.Recovery.Contracts;

namespace Opure.Recovery.Service;

public sealed class LocalRecoveryPointService
{
    private const string CommitMarkerFileName = ".commit";
    private const string ManifestFileName = "manifest.json";
    private static readonly JsonSerializerOptions ManifestJsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly IReadOnlyList<IBackupAdapter> adapters;
    private readonly string productVersion;

    public LocalRecoveryPointService(
        IEnumerable<IBackupAdapter> adapters,
        string? productVersion = null)
    {
        ArgumentNullException.ThrowIfNull(adapters);
        this.adapters = adapters.ToArray();
        this.productVersion = string.IsNullOrWhiteSpace(productVersion)
            ? "unknown"
            : productVersion;
    }

    public async Task<RecoveryPointManifest> CreateRecoveryPointAsync(
        string channel,
        string recoveryRootPath,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channel);
        ArgumentException.ThrowIfNullOrWhiteSpace(recoveryRootPath);

        Guid epochId = Guid.NewGuid();
        DateTimeOffset creationTimestamp = DateTimeOffset.UtcNow;
        string recoveryPointRoot = Path.Combine(
            recoveryRootPath,
            epochId.ToString("N"));
        BackupEpoch epoch = new(epochId, creationTimestamp)
        {
            StagingRootPath = recoveryPointRoot
        };

        Directory.CreateDirectory(recoveryRootPath);
        Directory.CreateDirectory(recoveryPointRoot);

        try
        {
            Dictionary<string, RecoveryOwnerSnapshot> owners =
                new(StringComparer.Ordinal);
            List<uint> supportedSchemas = [];

            foreach (IBackupAdapter adapter in adapters)
            {
                cancellationToken.ThrowIfCancellationRequested();
                BackupPreparationResult preparation = await adapter
                    .PrepareBackupAsync(epoch, cancellationToken)
                    .ConfigureAwait(false);
                if (!preparation.IsSuccess)
                {
                    throw new InvalidOperationException(
                        $"Backup owner {adapter.Identity.OwnerName} refused the recovery point: {preparation.RefusalReason}");
                }
            }

            foreach (IBackupAdapter adapter in adapters)
            {
                BackupCheckpointResult checkpoint = await adapter
                    .CreateCheckpointAsync(epoch, cancellationToken)
                    .ConfigureAwait(false);
                if (!checkpoint.IsSuccess)
                {
                    throw new InvalidOperationException(
                        $"Backup owner {adapter.Identity.OwnerName} could not create a checkpoint: {checkpoint.ErrorMessage}");
                }

                IReadOnlyCollection<FoundationStateInventoryItem> inventory =
                    await adapter.GetStateInventoryAsync(cancellationToken)
                        .ConfigureAwait(false);
                List<RecoveryFileSnapshot> files = [];
                foreach (FoundationStateInventoryItem item in inventory)
                {
                    string fullPath = Path.Combine(
                        recoveryPointRoot,
                        adapter.Identity.OwnerName,
                        item.RelativePath);
                    files.Add(new RecoveryFileSnapshot(
                        item.RelativePath,
                        item.Category,
                        item.Description,
                        ComputeSha256(fullPath)));
                }

                if (!owners.TryAdd(
                        adapter.Identity.OwnerName,
                        new RecoveryOwnerSnapshot(adapter.Identity, files)))
                {
                    throw new InvalidOperationException(
                        $"Backup owner {adapter.Identity.OwnerName} was registered more than once.");
                }

                supportedSchemas.Add(adapter.Identity.SupportedSchemaVersion);
            }

            string bindingHash = ComputeBindingHash(
                epochId,
                channel,
                productVersion,
                owners);
            RecoveryPointManifest provisionalManifest = new(
                epochId,
                epoch,
                "same-device",
                channel,
                owners,
                productVersion,
                supportedSchemas.Distinct().Order().ToArray(),
                [bindingHash],
                VerificationLevel.None,
                creationTimestamp,
                CreatorId: null,
                VerificationReceipts: []);

            bool isValid = await RecoveryPointVerifier.VerifyRecoveryPointAsync(
                provisionalManifest,
                recoveryPointRoot,
                channel,
                adapters,
                cancellationToken).ConfigureAwait(false);
            if (!isValid)
            {
                throw new InvalidOperationException(
                    "Recovery point structural verification failed in the disposable staging root.");
            }

            RecoveryPointManifest manifest = provisionalManifest with
            {
                VerificationLevel = VerificationLevel.Structural,
                VerificationReceipts =
                [
                    new EvidenceReceipt(
                        Guid.NewGuid(),
                        "backup.recovery-point-created",
                        creationTimestamp,
                        "opure.backup",
                        "Same-device recovery point created.",
                        bindingHash),
                    new EvidenceReceipt(
                        Guid.NewGuid(),
                        "backup.verification-completed",
                        DateTimeOffset.UtcNow,
                        "opure.backup",
                        "Structural verification completed in a disposable staging root.",
                        bindingHash)
                ]
            };

            byte[] manifestBytes = JsonSerializer.SerializeToUtf8Bytes(
                manifest,
                ManifestJsonOptions);
            await WriteNewFileAsync(
                Path.Combine(recoveryPointRoot, ManifestFileName),
                manifestBytes,
                cancellationToken).ConfigureAwait(false);

            byte[] commitBytes = Encoding.UTF8.GetBytes(
                Convert.ToHexString(SHA256.HashData(manifestBytes)).ToLowerInvariant());
            await WriteNewFileAsync(
                Path.Combine(recoveryPointRoot, CommitMarkerFileName),
                commitBytes,
                cancellationToken).ConfigureAwait(false);
            return manifest;
        }
        catch
        {
            TryDeleteIncompletePoint(recoveryPointRoot);
            throw;
        }
    }

    public static async Task<IReadOnlyList<RecoveryPointManifest>> ListRecoveryPointsAsync(
        string recoveryRootPath,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recoveryRootPath);
        if (!Directory.Exists(recoveryRootPath))
        {
            return [];
        }

        List<RecoveryPointManifest> results = [];
        foreach (string directory in Directory.GetDirectories(recoveryRootPath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            RecoveryPointManifest? manifest = await TryReadCommittedManifestAsync(
                directory,
                cancellationToken).ConfigureAwait(false);
            if (manifest is not null)
            {
                results.Add(manifest);
            }
        }

        return results
            .OrderByDescending(point => point.CreationTimestamp)
            .ToArray();
    }

    public async Task<bool> VerifyRecoveryPointAsync(
        Guid recoveryPointId,
        string channel,
        string recoveryRootPath,
        CancellationToken cancellationToken)
    {
        string recoveryPointRoot = Path.Combine(
            recoveryRootPath,
            recoveryPointId.ToString("N"));
        RecoveryPointManifest? manifest = await TryReadCommittedManifestAsync(
            recoveryPointRoot,
            cancellationToken).ConfigureAwait(false);
        if (manifest is null ||
            manifest.RecoveryPointId != recoveryPointId ||
            !string.Equals(manifest.Channel, channel, StringComparison.Ordinal))
        {
            return false;
        }

        return await RecoveryPointVerifier.VerifyRecoveryPointAsync(
            manifest,
            recoveryPointRoot,
            channel,
            adapters,
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<RecoveryPointManifest?> TryReadCommittedManifestAsync(
        string recoveryPointRoot,
        CancellationToken cancellationToken)
    {
        string manifestPath = Path.Combine(recoveryPointRoot, ManifestFileName);
        string commitPath = Path.Combine(recoveryPointRoot, CommitMarkerFileName);
        if (!File.Exists(manifestPath) || !File.Exists(commitPath))
        {
            return null;
        }

        byte[] manifestBytes = await File.ReadAllBytesAsync(
            manifestPath,
            cancellationToken).ConfigureAwait(false);
        string expectedHash = await File.ReadAllTextAsync(
            commitPath,
            cancellationToken).ConfigureAwait(false);
        string actualHash = Convert.ToHexString(
            SHA256.HashData(manifestBytes)).ToLowerInvariant();
        if (!string.Equals(expectedHash.Trim(), actualHash, StringComparison.Ordinal))
        {
            return null;
        }

        try
        {
            RecoveryPointManifest? manifest = JsonSerializer.Deserialize<RecoveryPointManifest>(
                manifestBytes,
                ManifestJsonOptions);
            string directoryName = Path.GetFileName(recoveryPointRoot);
            return manifest is not null &&
                Guid.TryParse(directoryName, out Guid directoryId) &&
                directoryId == manifest.RecoveryPointId
                    ? manifest
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static async Task WriteNewFileAsync(
        string path,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken)
    {
        await using FileStream stream = new(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            FileOptions.Asynchronous | FileOptions.WriteThrough);
        await stream.WriteAsync(content, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string ComputeSha256(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return string.Empty;
        }

        using FileStream stream = new(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static string ComputeBindingHash(
        Guid recoveryPointId,
        string channel,
        string productVersion,
        IReadOnlyDictionary<string, RecoveryOwnerSnapshot> owners)
    {
        StringBuilder binding = new();
        binding.Append(recoveryPointId.ToString("N"));
        binding.Append('|').Append(channel);
        binding.Append('|').Append(productVersion);
        foreach ((string ownerName, RecoveryOwnerSnapshot owner) in owners.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            binding.Append('|').Append(ownerName);
            binding.Append(':').Append(owner.Identity.AdapterRevision);
            binding.Append(':').Append(owner.Identity.SupportedSchemaVersion);
            foreach (RecoveryFileSnapshot file in owner.Files.OrderBy(file => file.RelativePath, StringComparer.Ordinal))
            {
                binding.Append('|').Append(file.RelativePath);
                binding.Append(':').Append(file.Sha256Hash);
            }
        }

        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(binding.ToString())))
            .ToLowerInvariant();
    }

    private static void TryDeleteIncompletePoint(string recoveryPointRoot)
    {
        if (!Directory.Exists(recoveryPointRoot))
        {
            return;
        }

        try
        {
            Directory.Delete(recoveryPointRoot, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
