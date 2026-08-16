using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Opure.Runtime.Contracts.Models;

namespace Opure.Runtime.Models;

public sealed class IntelligenceExecutionRouter
{
    private readonly IModelHostRunner _localRunner;
    private readonly IRemoteModelClient _remoteClient;
    private readonly ToolchainExecutionBridge _bridge;

    public IntelligenceExecutionRouter(
        IModelHostRunner localRunner,
        IRemoteModelClient remoteClient,
        ToolchainExecutionBridge bridge)
    {
        _localRunner = localRunner ?? throw new ArgumentNullException(nameof(localRunner));
        _remoteClient = remoteClient ?? throw new ArgumentNullException(nameof(remoteClient));
        _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
    }

    public async IAsyncEnumerable<StreamPayload> RouteIntelligenceAsync(
        bool useRemote,
        RemoteProviderConfiguration remoteConfig,
        string workspaceId,
        string manifestHash,
        ModelRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (useRemote)
        {
            ArgumentNullException.ThrowIfNull(remoteConfig);
            // Remote multi-turn loop
            int consecutiveToolCalls = 0;
            var currentRequest = request; // We may need to clone/update this with tool results in a real impl.
            
            while (!cancellationToken.IsCancellationRequested)
            {
                bool toolExecuted = false;
                
                await foreach (var chunk in _remoteClient.RunRemoteModelAsync(remoteConfig, currentRequest, cancellationToken).ConfigureAwait(false))
                {
                    if (chunk.IsToolCall)
                    {
                        consecutiveToolCalls++;
                        if (consecutiveToolCalls > 10)
                        {
                            yield return new StreamPayload(false, "Error: Max consecutive tool calls exceeded.");
                            yield break;
                        }

                        var toolRequest = JsonSerializer.Deserialize(chunk.Content, ModelContractsJsonContext.Default.ToolRequest);
                        if (toolRequest != null)
                        {
                            var toolResult = await _bridge.ExecuteToolAsync(toolRequest, cancellationToken).ConfigureAwait(false);
                            // In a real remote provider, we would append the tool call and result to the currentRequest.ChatHistory
                            // and start a new request. For this stem, we yield the tool result back or update request.
                            // Assuming we just append to the request's messages (pseudo-code depending on ModelRequest structure).
                            // If ModelRequest doesn't support chat history easily yet, we just break and loop.
                            toolExecuted = true;
                            break; // break the foreach to start a new request
                        }
                    }
                    else
                    {
                        consecutiveToolCalls = 0;
                        yield return chunk;
                    }
                }
                
                if (!toolExecuted)
                {
                    break;
                }
            }
        }
        else
        {
            // Local loop (already handled inside ModelHostRunner, or we can refactor it here)
            await foreach (var chunk in _localRunner.RunModelAsync(workspaceId, manifestHash, request, cancellationToken).ConfigureAwait(false))
            {
                yield return chunk;
            }
        }
    }
}
