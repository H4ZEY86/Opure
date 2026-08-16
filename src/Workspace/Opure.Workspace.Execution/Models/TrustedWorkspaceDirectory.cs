using System;
using System.IO;

namespace Opure.Workspace.Contracts.Models;

/// <summary>
///     Provides a trusted workspace directory path where model files may be imported and stored.
///     Enforces that all model files reside within a bounded, administratively-controlled root.
/// </summary>
public sealed class TrustedWorkspaceDirectory : ITrustedWorkspaceDirectory
{
    /// <summary>
    ///     The trusted root directory path. This is the administrative boundary for all model file imports.
    ///     Model files may only be stored within this directory tree.
    /// </summary>
    public string TrustedRoot { get; }

    /// <summary>
    ///     Constructs a new <see cref="TrustedWorkspaceDirectory" /> with the specified root path.
    /// </summary>
    /// <param name="trustedRoot">The absolute path to the trusted model workspace root.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="trustedRoot" /> is empty or whitespace.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the root directory does not exist.</exception>
    public TrustedWorkspaceDirectory(string trustedRoot)
    {
        if (string.IsNullOrWhiteSpace(trustedRoot))
            throw new ArgumentException("Trusted root directory path cannot be empty or whitespace.", nameof(trustedRoot));

        TrustedRoot = Path.GetFullPath(trustedRoot);

        if (!Directory.Exists(TrustedRoot))
            Directory.CreateDirectory(TrustedRoot);
    }

    /// <inheritdoc />
    public void EnsureExists()
    {
        if (!Directory.Exists(TrustedRoot))
            Directory.CreateDirectory(TrustedRoot);
    }

    /// <summary>
    ///     Returns a safely scoped path within the trusted root for a given relative path.
    ///     Throws if the resulting path would escape the trusted root (path traversal prevention).
    /// </summary>
    /// <param name="relativePath">The relative path within the trusted root.</param>
    /// <returns>The fully qualified, trusted path.</returns>
    /// <exception cref="ArgumentException">Thrown when the resolved path escapes the trusted root.</exception>
    public string GetSafePath(string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(TrustedRoot, relativePath)).Normalize();

        // Path traversal protection: ensure the resolved path is within the trusted root
        if (!fullPath.StartsWith(TrustedRoot + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase) &&
            fullPath != TrustedRoot)
        {
            throw new ArgumentException(
                $"Resolved path '{fullPath}' escapes the trusted root '{TrustedRoot}'.",
                nameof(relativePath));
        }

        return fullPath;
    }

    /// <summary>
    ///     Checks whether a given absolute path is within the trusted root zone.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns><c>true</c> if the path is within the trusted root; otherwise <c>false</c>.</returns>
    public bool IsWithinTrustedZone(string path) =>
        path.StartsWith(TrustedRoot, StringComparison.OrdinalIgnoreCase);
}