using System;
using System.Collections.Generic;
using System.IO;

namespace Opure.Workspace.Execution;

public interface IExecutableResolver
{
    string Resolve(string executableName);
}

public class ExecutableResolver : IExecutableResolver
{
    private readonly Dictionary<string, string> _wellKnownPaths;

    public ExecutableResolver(Dictionary<string, string>? explicitPaths = null)
    {
        _wellKnownPaths = explicitPaths ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public string Resolve(string executableName)
    {
        if (string.IsNullOrWhiteSpace(executableName))
        {
            throw new ArgumentException("Executable name cannot be empty.", nameof(executableName));
        }

        if (Path.IsPathRooted(executableName))
        {
            if (File.Exists(executableName))
            {
                return executableName;
            }
            throw new InvalidOperationException($"Absolute path executable not found: {executableName}");
        }

        if (_wellKnownPaths.TryGetValue(executableName, out var absolutePath))
        {
            if (File.Exists(absolutePath))
            {
                return absolutePath;
            }
            throw new InvalidOperationException($"Well-known executable not found at resolved path: {absolutePath}");
        }

        throw new InvalidOperationException($"Executable '{executableName}' is not mapped to an absolute path and arbitrary PATH resolution is blocked.");
    }
}
