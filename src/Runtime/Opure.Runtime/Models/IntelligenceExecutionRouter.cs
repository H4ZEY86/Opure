using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Opure.Runtime.Contracts.Models;
using Opure.TrustEvidence.Contracts;

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
            await foreach (var chunk in RouteRemoteIntelligenceAsync(remoteConfig, request, cancellationToken).ConfigureAwait(false))
            {
                yield return chunk;
            }
        }
        else
        {
            bool localFailed = false;
            bool hasFirst = false;
            StreamPayload? firstPayload = null;
            IAsyncEnumerator<StreamPayload>? localEnumerator = null;
            StreamPayload? diagnosticPayload = null;

            try
            {
                localEnumerator = _localRunner.RunModelAsync(workspaceId, manifestHash, request, cancellationToken).GetAsyncEnumerator(cancellationToken);
                
                // Try moving to the first element. If it fails here, we catch and fallback.
                hasFirst = await localEnumerator.MoveNextAsync().ConfigureAwait(false);
                if (hasFirst)
                {
                    firstPayload = localEnumerator.Current;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                localFailed = true;
                diagnosticPayload = new StreamPayload(false, $"[Diagnostic] Local model execution failed: {ex.Message}. Falling back to remote if configured.\n");
            }

            if (diagnosticPayload != null)
            {
                yield return diagnosticPayload;
            }

            if (localFailed)
            {
                if (localEnumerator != null)
                {
                    await localEnumerator.DisposeAsync().ConfigureAwait(false);
                }

                if (remoteConfig != null)
                {
                    await foreach (var chunk in RouteRemoteIntelligenceAsync(remoteConfig, request, cancellationToken).ConfigureAwait(false))
                    {
                        yield return chunk;
                    }
                }
                else
                {
                    yield return new StreamPayload(false, "Error: Local execution failed and no remote fallback configured.\n");
                }
            }
            else
            {
                try
                {
                    if (hasFirst)
                    {
                        yield return firstPayload!;
                        while (await localEnumerator!.MoveNextAsync().ConfigureAwait(false))
                        {
                            yield return localEnumerator.Current;
                        }
                    }
                }
                finally
                {
                    if (localEnumerator != null)
                    {
                        await localEnumerator.DisposeAsync().ConfigureAwait(false);
                    }
                }
            }
        }
    }

    private async IAsyncEnumerable<StreamPayload> RouteRemoteIntelligenceAsync(
        RemoteProviderConfiguration remoteConfig,
        ModelRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        int consecutiveToolCalls = 0;
        var currentRequest = request;
        
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
                        var toolResult = await _bridge.ExecuteToolAsync(toolRequest, ApproverIdentity.Agent("RemoteIntelligenceAgent"), cancellationToken).ConfigureAwait(false);
                        toolExecuted = true;
                        break; 
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
}
