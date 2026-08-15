using System;
using System.Collections.Generic;
using System.Linq;
using Opure.Workspace.Contracts;
using Opure.Patch.Contracts;

namespace Opure.Workspace.Execution;

public static class ToolTemplateValidator
{
    private static readonly HashSet<string> BannedExecutables = new(StringComparer.OrdinalIgnoreCase)
    {
        "cmd",
        "cmd.exe",
        "powershell",
        "powershell.exe",
        "pwsh",
        "pwsh.exe",
        "bash",
        "bash.exe",
        "sh",
        "sh.exe",
        "wsl",
        "wsl.exe"
    };

    private static readonly char[] BannedMetacharacters = { '$', '%', '&', '|', '>', '<', '`', ';', '\n', '\r' };

    public static void Validate(ToolTemplate template)
    {
        if (string.IsNullOrWhiteSpace(template.Id))
        {
            throw new PreconditionFailedException("Template ID cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(template.ExecutableName))
        {
            throw new PreconditionFailedException("Executable name cannot be empty.");
        }

        if (BannedExecutables.Contains(template.ExecutableName))
        {
            throw new PreconditionFailedException($"Executable '{template.ExecutableName}' is a banned shell identifier.");
        }

        if (template.Arguments != null)
        {
            foreach (var arg in template.Arguments)
            {
                if (arg.IndexOfAny(BannedMetacharacters) >= 0)
                {
                    throw new PreconditionFailedException($"Argument '{arg}' contains banned shell metacharacters.");
                }

                if (arg.Contains("../") || arg.Contains("..\\"))
                {
                    throw new PreconditionFailedException($"Argument '{arg}' contains directory traversal sequences.");
                }
            }
        }

        if (template.TimeoutMilliseconds <= 0)
        {
            throw new PreconditionFailedException("Timeout must be strictly positive.");
        }

        if (template.EffectClass == ToolEffectClass.ReadOnly && template.InputOutputPolicy.SupportsStdin)
        {
            throw new PreconditionFailedException("Read-only templates cannot support STDIN.");
        }
    }
}
