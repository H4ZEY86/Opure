using System;
using System.Collections.Generic;
using System.Linq;
using Opure.Workspace.Contracts;

namespace Opure.Workspace.Execution;

public interface IToolTemplateCatalogue
{
    ToolTemplate? TryGetTemplate(string id);
}

public class ToolTemplateCatalogue : IToolTemplateCatalogue
{
    private static readonly string[] AllowedEnvVars = new[] { "SystemRoot", "TEMP", "TMP" };
    private static readonly string[] GitStatusArgs = new[] { "status" };
    private static readonly string[] GitDiffStatArgs = new[] { "diff", "--stat" };
    private static readonly string[] DotnetInfoArgs = new[] { "--info" };

    private readonly Dictionary<string, ToolTemplate> _templates;

    public ToolTemplateCatalogue()
    {
        var envPolicy = new ToolEnvironmentPolicy(AllowedEnvVars);
        var ioPolicy = new ToolInputOutputPolicy(false, 1024 * 1024 * 10); // 10MB

        _templates = new[]
        {
            new ToolTemplate(
                "git-status",
                "git.exe",
                GitStatusArgs,
                10000,
                ToolEffectClass.ReadOnly,
                envPolicy,
                ioPolicy,
                ResourceClass.Lightweight),
            new ToolTemplate(
                "git-diff-stat",
                "git.exe",
                GitDiffStatArgs,
                10000,
                ToolEffectClass.ReadOnly,
                envPolicy,
                ioPolicy,
                ResourceClass.Lightweight),
            new ToolTemplate(
                "dotnet-info",
                "dotnet.exe",
                DotnetInfoArgs,
                10000,
                ToolEffectClass.ReadOnly,
                envPolicy,
                ioPolicy,
                ResourceClass.Lightweight)
        }.ToDictionary(t => t.Id);
    }

    public ToolTemplate? TryGetTemplate(string id)
    {
        return _templates.TryGetValue(id, out var template) ? template : null;
    }
}
