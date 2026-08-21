using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Opure.Runtime.Contracts.Mcp;
using Opure.Runtime.Contracts.Providers;
using Opure.Workspace.Execution.Models;

namespace Opure.Runtime.Mcp;

public sealed class McpStdioClient : IMcpClient, IDisposable
{
    private readonly McpServerProfile _profile;
    private Process? _process;
    private WindowsJobObject? _jobObject;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonRpcResponse>> _pendingRequests = new();
    private int _nextId;
    private CancellationTokenSource? _readLoopCts;
    private Task? _readLoopTask;

    public McpStdioClient(McpServerProfile profile)
    {
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
    }

    private void EnsureStarted()
    {
        if (_process != null && !_process.HasExited)
            return;

        var startInfo = new ProcessStartInfo
        {
            FileName = _profile.ExecutablePath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        _process = new Process { StartInfo = startInfo };
        _process.Start();

        try
        {
            _jobObject = new WindowsJobObject();
            _jobObject.AssignProcess(_process);
        }
        catch
        {
            _process.Kill(true);
            throw new InvalidOperationException("Failed to attach MCP process to Windows Job Object.");
        }

        _readLoopCts = new CancellationTokenSource();
        _readLoopTask = Task.Run(() => ReadLoopAsync(_process.StandardOutput, _readLoopCts.Token));
    }

    private async Task ReadLoopAsync(StreamReader reader, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
                if (line == null) break; // EOF

                if (string.IsNullOrWhiteSpace(line)) continue;

                var response = JsonSerializer.Deserialize(line, McpJsonSerializerContext.Default.JsonRpcResponse);
                if (response != null && response.Id != null)
                {
                    if (_pendingRequests.TryRemove(response.Id, out var tcs))
                    {
                        tcs.TrySetResult(response);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal exit
        }
        catch (Exception ex)
        {
            foreach (var tcs in _pendingRequests.Values)
            {
                tcs.TrySetException(ex);
            }
            _pendingRequests.Clear();
        }
    }

    private async Task<JsonRpcResponse> SendRequestAsync(string method, JsonElement? parameters, CancellationToken ct)
    {
        EnsureStarted();

        var id = Interlocked.Increment(ref _nextId).ToString();
        var request = new JsonRpcRequest("2.0", id, method, parameters);
        var json = JsonSerializer.Serialize(request, McpJsonSerializerContext.Default.JsonRpcRequest);

        var tcs = new TaskCompletionSource<JsonRpcResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        
        using var registration = ct.Register(() => 
        {
            if (_pendingRequests.TryRemove(id, out var pending))
            {
                pending.TrySetCanceled(ct);
            }
        });

        _pendingRequests[id] = tcs;

        await _process!.StandardInput.WriteLineAsync(json.AsMemory(), ct).ConfigureAwait(false);
        await _process.StandardInput.FlushAsync(ct).ConfigureAwait(false);

        return await tcs.Task.ConfigureAwait(false);
    }

    public async Task InitializeAsync(CancellationToken ct)
    {
        var clientInfo = new McpClientInfo("Opure", "1.0.0");
        var capabilitiesElement = JsonDocument.Parse("{}").RootElement;
        var initParams = new McpInitializeParams("2024-11-05", capabilitiesElement, clientInfo);
        
        var parameters = JsonSerializer.SerializeToElement(initParams, McpJsonSerializerContext.Default.McpInitializeParams);

        var response = await SendRequestAsync("initialize", parameters, ct).ConfigureAwait(false);
        
        if (response.Error != null)
        {
            throw new InvalidOperationException($"MCP initialize failed: {response.Error.Message}");
        }

        // Send initialized notification
        var notification = new JsonRpcRequest("2.0", null!, "notifications/initialized", null);
        var json = JsonSerializer.Serialize(notification, McpJsonSerializerContext.Default.JsonRpcRequest);
        await _process!.StandardInput.WriteLineAsync(json.AsMemory(), ct).ConfigureAwait(false);
        await _process.StandardInput.FlushAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<McpToolSchema>> ListToolsAsync(CancellationToken ct)
    {
        var response = await SendRequestAsync("tools/list", null, ct).ConfigureAwait(false);
        if (response.Error != null)
        {
            throw new InvalidOperationException($"MCP tools/list failed: {response.Error.Message}");
        }

        if (response.Result.HasValue)
        {
            var result = JsonSerializer.Deserialize(response.Result.Value, McpJsonSerializerContext.Default.McpListToolsResult);
            if (result?.Tools != null)
            {
                return result.Tools.Select(t => new McpToolSchema(
                    t.Name, 
                    t.Description ?? string.Empty, 
                    JsonSerializer.Serialize(t.InputSchema))).ToList();
            }
        }

        return Array.Empty<McpToolSchema>();
    }

    public async Task<(string Result, McpResultReceipt Receipt)> CallToolAsync(string toolName, string argumentsJson, McpPermission permission, CancellationToken ct)
    {
        if (permission.Status != ApprovalStatus.Active)
        {
            throw new UnauthorizedAccessException($"Permission status for MCP tool execution must be Active, but was {permission.Status}");
        }

        if (!permission.AllowedTools.Contains(toolName))
        {
            throw new UnauthorizedAccessException($"Tool '{toolName}' is not authorized in the given permission lease.");
        }

        JsonElement? argsElement = null;
        if (!string.IsNullOrWhiteSpace(argumentsJson))
        {
            argsElement = JsonDocument.Parse(argumentsJson).RootElement;
        }

        var callParams = new McpCallToolParams(toolName, argsElement);
        var parameters = JsonSerializer.SerializeToElement(callParams, McpJsonSerializerContext.Default.McpCallToolParams);

        var sw = Stopwatch.StartNew();
        var response = await SendRequestAsync("tools/call", parameters, ct).ConfigureAwait(false);
        sw.Stop();

        bool isSuccess = response.Error == null;
        string resultData = string.Empty;

        if (isSuccess && response.Result.HasValue)
        {
            var callResult = JsonSerializer.Deserialize(response.Result.Value, McpJsonSerializerContext.Default.McpCallToolResult);
            if (callResult != null)
            {
                resultData = JsonSerializer.Serialize(callResult.Content);
                isSuccess = callResult.IsError != true;
            }
        }
        else if (response.Error != null)
        {
            resultData = response.Error.Message;
        }

        var receipt = new McpResultReceipt(
            Guid.NewGuid().ToString("N"),
            DateTimeOffset.UtcNow,
            _profile.ServerId,
            toolName,
            sw.Elapsed,
            isSuccess
        );

        return (resultData, receipt);
    }

    public void Dispose()
    {
        _readLoopCts?.Cancel();
        _readLoopCts?.Dispose();
        
        if (_process != null && !_process.HasExited)
        {
            try { _process.Kill(true); } catch { }
        }

        _jobObject?.Dispose();
        _process?.Dispose();
    }
}
