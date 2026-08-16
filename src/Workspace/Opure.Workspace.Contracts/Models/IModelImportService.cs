using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Opure.Workspace.Contracts.Models;

/// <summary>
///     Provides controlled, hash-pinned import of model weight files into the trusted workspace.
///     Enforces that no model file enters the system without a verified manifest contract.
/// </summary>
public interface IModelImportService
{
    /// <summary>
    ///     Imports a model weight file into the trusted workspace with SHA-256 hash verification.
    ///     If a manifest with the same hash already exists, the import is idempotent.
    /// </summary>
    /// <param name="sourcePath">Absolute path to the source model weight file.</param>
    /// <param name="destinationSubPath">Optional subdirectory within the trusted workspace.</param>
    /// <param name="cancellationToken">Token to cancel the import operation.</param>
    /// <returns>A <see cref="ModelImportResult" /> indicating success or failure.</returns>
    Task<ModelImportResult> ImportModelAsync(
        string sourcePath,
        string? destinationSubPath = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     Represents the result of a model import operation.
/// </summary>
public sealed record ModelImportResult
{
    /// <summary>
    ///     Whether the import succeeded.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    ///     The associated <see cref="ModelHostManifest" /> if the import succeeded.
    /// </summary>
    public ModelHostManifest? Manifest { get; init; }

    /// <summary>
    ///     Whether the model was already verified (idempotent import).
    /// </summary>
    public bool AlreadyVerified { get; init; }

    /// <summary>
    ///     Error message if the import failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    ///     The destination path where the model was copied, if successful.
    /// </summary>
    public string DestinationPath { get; init; } = string.Empty;

    /// <summary>
    ///     Creates a successful import result.
    /// </summary>
    /// <param name="manifest">The verified manifest.</param>
    /// <param name="alreadyVerified">Whether this was an idempotent re-import.</param>
    /// <param name="destination">The destination path.</param>
    /// <returns>A new <see cref="ModelImportResult" /></returns>
    public static ModelImportResult Success(ModelHostManifest manifest, bool alreadyVerified = false,
        string? destination = null) =>
        new()
        {
            IsSuccess = true,
            Manifest = manifest,
            AlreadyVerified = alreadyVerified,
            ErrorMessage = null,
            DestinationPath = destination ?? manifest?.ModelPath ?? string.Empty
        };

    /// <summary>
    ///     Creates a failed import result.
    /// </summary>
    /// <param name="errorMessage">The error description.</param>
    /// <returns>A new <see cref="ModelImportResult" /></returns>
    public static ModelImportResult Fail(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage, DestinationPath = string.Empty };
}

/// <summary>
///     Defines the contract for a trusted workspace directory that model files may be imported into.
/// </summary>
public interface ITrustedWorkspaceDirectory
{
    /// <summary>
    ///     The trusted root directory path where model files may be stored.
    /// </summary>
    string TrustedRoot { get; }

    /// <summary>
    ///     Ensures the trusted directory exists; creates it if necessary.
    /// </summary>
    void EnsureExists();
}

/// <summary>
///     Represents a cryptographic contract that pins a model weight file to its expected SHA-256 hash.
///     This manifest is immutable and must be verified before any model host launch.
/// </summary>
public sealed record ModelHostManifest
{
    /// <summary>
    ///     The canonical absolute path of the model file within the trusted store.
    /// </summary>
    public string ModelPath { get; init; } = string.Empty;

    /// <summary>
    ///     The expected SHA-256 hash of the model weight file (32 bytes / 64 hex characters).
    /// </summary>
    public byte[] RequiredSha256 { get; init; } = Array.Empty<byte>();

    /// <summary>
    ///     The generation/revision of this manifest contract. Changes require a new manifest.
    /// </summary>
    public ushort ManifestRevision { get; init; }

    /// <summary>
    ///     The SHA-256 hash of the manifest itself, for manifest integrity verification.
    /// </summary>
    public string ManifestHash { get; init; } = string.Empty;

    /// <summary>
    ///     The checksum algorithm used. Currently only SHA-256 is supported.
    /// </summary>
    public ChecksumAlgorithm Algorithm { get; init; } = ChecksumAlgorithm.SHA256;

    /// <summary>
    ///     Model architecture/target identifier (e.g., "llama-2-7b-q4_0", "phi-3-mini-4k").
    /// </summary>
    public string ModelArchitecture { get; init; } = string.Empty;

    /// <summary>
    ///     Total number of model layers/parameters.
    /// </summary>
    public uint LayerCount { get; init; }

    /// <summary>
    ///     Total size of model weights in bytes.
    /// </summary>
    public ulong TotalWeightBytes { get; init; }

    /// <summary>
    ///     Optional system prompt embedded in the model context.
    /// </summary>
    public string? SystemPrompt { get; init; }

    /// <summary>
    ///     Returns a string representation of this manifest's key identifiers.
    /// </summary>
    /// <returns>A human-readable representation.</returns>
    public override string ToString() =>
        $"ModelManifest[Path={ModelPath}, Rev={ManifestRevision}, Hash={ManifestHash[..8]}..., Arch={ModelArchitecture}]";
}

/// <summary>
///     Enumerates the supported checksum algorithms for model manifest contracts.
/// </summary>
public enum ChecksumAlgorithm
{
    /// <summary>
    ///     SHA-256 hash algorithm.
    /// </summary>
    SHA256,

    /// <summary>
    ///     SHA-384 hash algorithm.
    /// </summary>
    SHA384,

    /// <summary>
    ///     SHA-512 hash algorithm.
    /// </summary>
    SHA512
}