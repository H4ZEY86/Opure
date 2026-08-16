using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Opure.Runtime.Contracts.Models;
using Opure.Workspace.Contracts;

namespace Opure.Runtime.Models;

public sealed class LocalToolchainProvider : IToolchainProvider
{
    private static readonly ToolTemplate[] Templates = new[]
    {
        new ToolTemplate(
            Id: "apply_patch",
            ExecutableName: "apply_patch",
            Arguments: Array.Empty<string>(),
            TimeoutMilliseconds: 30000,
            EffectClass: ToolEffectClass.MutatesWorkspace,
            EnvironmentPolicy: new ToolEnvironmentPolicy(Array.Empty<string>()),
            InputOutputPolicy: new ToolInputOutputPolicy(true, 1024 * 1024),
            ResourceClass: ResourceClass.Lightweight),
            
        new ToolTemplate(
            Id: "run_command",
            ExecutableName: "run_command",
            Arguments: Array.Empty<string>(),
            TimeoutMilliseconds: 60000,
            EffectClass: ToolEffectClass.MutatesWorkspace,
            EnvironmentPolicy: new ToolEnvironmentPolicy(Array.Empty<string>()),
            InputOutputPolicy: new ToolInputOutputPolicy(true, 1024 * 1024 * 10),
            ResourceClass: ResourceClass.Heavy),
            
        new ToolTemplate(
            Id: "read_file_range",
            ExecutableName: "read_file_range",
            Arguments: Array.Empty<string>(),
            TimeoutMilliseconds: 10000,
            EffectClass: ToolEffectClass.ReadOnly,
            EnvironmentPolicy: new ToolEnvironmentPolicy(Array.Empty<string>()),
            InputOutputPolicy: new ToolInputOutputPolicy(true, 1024 * 1024),
            ResourceClass: ResourceClass.Lightweight),
            
        new ToolTemplate(
            Id: "list_directory",
            ExecutableName: "list_directory",
            Arguments: Array.Empty<string>(),
            TimeoutMilliseconds: 10000,
            EffectClass: ToolEffectClass.ReadOnly,
            EnvironmentPolicy: new ToolEnvironmentPolicy(Array.Empty<string>()),
            InputOutputPolicy: new ToolInputOutputPolicy(true, 1024 * 1024),
            ResourceClass: ResourceClass.Lightweight),
            
        new ToolTemplate(
            Id: "inspect_diff",
            ExecutableName: "inspect_diff",
            Arguments: Array.Empty<string>(),
            TimeoutMilliseconds: 30000,
            EffectClass: ToolEffectClass.ReadOnly,
            EnvironmentPolicy: new ToolEnvironmentPolicy(Array.Empty<string>()),
            InputOutputPolicy: new ToolInputOutputPolicy(true, 1024 * 1024 * 5),
            ResourceClass: ResourceClass.Lightweight)
    };

    public async IAsyncEnumerable<ToolTemplate> GetAvailableToolsAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var template in Templates)
        {
            yield return template;
        }
        await Task.CompletedTask;
    }

    public Task<ToolRequestValidationResult> ValidateToolRequestAsync(ToolRequest request, CancellationToken cancellationToken)
    {
        if (request.ToolName is "apply_patch" or "run_command" or "read_file_range" or "list_directory" or "inspect_diff")
        {
            return Task.FromResult(ToolRequestValidationResult.Success(request.Arguments));
        }

        return Task.FromResult(ToolRequestValidationResult.Rejected($"Unknown tool: {request.ToolName}"));
    }
}
