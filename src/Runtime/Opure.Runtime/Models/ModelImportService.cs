using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Opure.Runtime.Contracts.Models;
using Opure.Workspace.Contracts.Models;

namespace Opure.Runtime.Models;

/// <summary>
///     Provides controlled, hash-pinned import of model weight files into the trusted workspace.
///     Enforces that no model file enters the system without a verified manifest contract.
/// </summary>
public sealed class ModelImportService : IModelImportService, IDisposable
{
    private readonly IModelHostModelValidator _validator;
    private readonly IModelManifestStore _manifestStore;
    private readonly ITrustedWorkspaceDirectory _workspaceDir;
    private readonly SemaphoreSlim _importLock = new(1, 1);

    public ModelImportService(
        IModelHostModelValidator validator,
        IModelManifestStore manifestStore,
        ITrustedWorkspaceDirectory workspaceDir)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _manifestStore = manifestStore ?? throw new ArgumentNullException(nameof(manifestStore));
        _workspaceDir = workspaceDir ?? throw new ArgumentNullException(nameof(workspaceDir));
    }

    public async Task<ModelImportResult> ImportModelAsync(
        string sourcePath,
        string? destinationSubPath = null,
        CancellationToken cancellationToken = default)
    {
        await _importLock.WaitAsync(cancellationToken);

        try
        {
            var resolvedSource = Path.GetFullPath(sourcePath).Normalize();

            // 1. Path integrity: ensure source is within allowed boundaries
            if (!IsPathWithinTrustedZone(resolvedSource))
                return ModelImportResult.Fail("Source path outside trusted model zone.");

            // 2. File existence and readability
            if (!File.Exists(resolvedSource))
                return ModelImportResult.Fail("Source model file does not exist.");

            // 3. Compute SHA-256 of source
            var sourceHash = ComputeSha256(resolvedSource);

            // 4. Check for existing manifest (idempotent import)
            var existingManifest = await _manifestStore.GetManifestForHashAsync(sourceHash, cancellationToken);
            if (existingManifest != null)
            {
                // Model already imported and verified; return existing manifest reference
                return ModelImportResult.Success(
                    existingManifest,
                    alreadyVerified: true,
                    destination: existingManifest.ModelPath);
            }

            // 5. Generate new manifest
            var fileInfo = new FileInfo(resolvedSource);
            var manifest = new ModelHostManifest
            {
                ModelPath = resolvedSource,
                RequiredSha256 = sourceHash,
                ManifestRevision = 1,
                ManifestHash = Convert.ToHexString(sourceHash).ToLowerInvariant(),
                ModelArchitecture = DetectArchitecture(resolvedSource),
                LayerCount = EstimateLayerCount(fileInfo.Length),
                TotalWeightBytes = (ulong)fileInfo.Length,
                SystemPrompt = ExtractSystemPrompt(resolvedSource)
            };

            // 6. Validate against any existing constraints
            await _validator.ValidateAsync(manifest.ModelPath, cancellationToken);

            // 7. Persist manifest
            await _manifestStore.StoreManifestAsync(manifest, cancellationToken);

            // 8. Copy to trusted workspace (if different from source)
            string destPath;
            if (string.IsNullOrEmpty(destinationSubPath))
                destPath = Path.Combine(_workspaceDir.TrustedRoot, Path.GetFileName(resolvedSource));
            else
                destPath = Path.Combine(_workspaceDir.TrustedRoot, destinationSubPath, Path.GetFileName(resolvedSource));

            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            File.Copy(resolvedSource, destPath, overwrite: true);

            // 9. Record import event for audit
            await _manifestStore.RecordImportAsync(manifest, cancellationToken);

            return ModelImportResult.Success(manifest, alreadyVerified: false, destination: destPath);
        }
        finally
        {
            _importLock.Release();
        }
    }

    private bool IsPathWithinTrustedZone(string path) =>
        path.StartsWith(_workspaceDir.TrustedRoot, StringComparison.OrdinalIgnoreCase);

    private static byte[] ComputeSha256(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(stream);
    }

    private static string DetectArchitecture(string filePath) =>
        Path.GetFileNameWithoutExtension(filePath).ToLowerInvariant() switch
        {
            var n when n.Contains("llama") => "llama",
            var n when n.Contains("phi") => "phi",
            var n when n.Contains("mistral") => "mistral",
            var n when n.Contains("gemma") => "gemma",
            _ => "unknown"
        };

    private static uint EstimateLayerCount(long fileSizeBytes) =>
        fileSizeBytes switch
        {
            > 2_000_000_000 => 100,     // ~7B parameters
            > 1_000_000_000 => 80,      // ~6B parameters
            > 500_000_000 => 60,        // ~3B parameters
            > 200_000_000 => 40,        // ~1.3B parameters
            > 100_000_000 => 24,        // ~350M parameters
            > 50_000_000 => 12,         // ~120M parameters
            > 0 => 6,                   // ~120M parameters (smallest)
            _ => 0
        };

    private static string? ExtractSystemPrompt(string filePath)
    {
        // In a full implementation, this would parse the GGUF header for a system prompt tag.
        // For now, return null as no system prompt is embedded by default.
        return null;
    }

    public void Dispose()
    {
        _importLock.Dispose();
    }
}