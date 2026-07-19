using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;

namespace Opure.Bootstrap.Windows;

internal sealed record BootstrapBinaryIdentity(
    string ExecutablePath,
    string ExecutableName,
    string AssemblyName,
    string ProductVersion,
    string ExecutableSha256);

internal static class BootstrapBinaryIdentityVerifier
{
    internal static BootstrapBinaryIdentity Verify(
        string executablePath,
        string expectedExecutableName,
        string expectedAssemblyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedExecutableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedAssemblyName);

        if (!Path.IsPathFullyQualified(executablePath))
        {
            throw new InvalidDataException(
                "Bootstrap child executable path is not absolute.");
        }

        string fullPath = Path.GetFullPath(executablePath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                "Expected Bootstrap child executable was not found.",
                fullPath);
        }

        if (!string.Equals(
                Path.GetFileName(fullPath),
                expectedExecutableName,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                "Bootstrap child executable name is unexpected.");
        }

        FileAttributes attributes = File.GetAttributes(fullPath);

        if ((attributes & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidDataException(
                "Bootstrap child executable must not be a reparse point.");
        }

        string companionAssembly = Path.ChangeExtension(fullPath, ".dll");

        if (!File.Exists(companionAssembly))
        {
            throw new FileNotFoundException(
                "Bootstrap child companion assembly was not found.",
                companionAssembly);
        }

        AssemblyName assemblyName = AssemblyName.GetAssemblyName(
            companionAssembly);

        if (!string.Equals(
                assemblyName.Name,
                expectedAssemblyName,
                StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                "Bootstrap child assembly identity is unexpected.");
        }

        string productVersion =
            FileVersionInfo.GetVersionInfo(companionAssembly).ProductVersion
            ?? assemblyName.Version?.ToString()
            ?? "unknown";

        string executableHash = Convert.ToHexStringLower(
            SHA256.HashData(File.ReadAllBytes(fullPath)));

        return new BootstrapBinaryIdentity(
            fullPath,
            expectedExecutableName,
            expectedAssemblyName,
            productVersion,
            executableHash);
    }
}
