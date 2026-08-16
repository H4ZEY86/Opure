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
            ResourceClass: ResourceClass.Heavy)
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
        // Basic validation for Stem 1
        if (request.ToolName is "apply_patch" or "run_command")
        {
            return Task.FromResult(ToolRequestValidationResult.Success(request.Arguments));
        }

        return Task.FromResult(ToolRequestValidationResult.Rejected($"Unknown tool: {request.ToolName}"));
    }
}
