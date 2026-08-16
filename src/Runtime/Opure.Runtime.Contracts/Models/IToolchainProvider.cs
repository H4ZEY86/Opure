using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Opure.Workspace.Contracts;

namespace Opure.Runtime.Contracts.Models;

/// <summary>
/// A vendor-neutral provider that returns Gate B read-only tool templates 
/// and validates AI tool requests against active sandbox constraints.
/// </summary>
public interface IToolchainProvider
{
    IAsyncEnumerable<ToolTemplate> GetAvailableToolsAsync(CancellationToken cancellationToken);
    
    Task<ToolRequestValidationResult> ValidateToolRequestAsync(ToolRequest request, CancellationToken cancellationToken);
}
