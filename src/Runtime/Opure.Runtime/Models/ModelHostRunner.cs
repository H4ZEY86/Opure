using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Opure.Runtime.Contracts.Models;
using Opure.Workspace.Contracts.Models;

namespace Opure.Runtime.Models;

public sealed class ModelHostRunner : IModelHostRunner
{
    private readonly IModelManifestStore _manifestStore;
    private readonly IModelHostProcessLauncher _launcher;
    private readonly IModelRequestRouter _router;
    private readonly IModelCommandBuilder _commandBuilder;
    private readonly ToolchainExecutionBridge _bridge;

    public ModelHostRunner(
        IModelManifestStore manifestStore,
        IModelHostProcessLauncher launcher,
        IModelRequestRouter router,
        IModelCommandBuilder commandBuilder,
        ToolchainExecutionBridge bridge)
    {
        _manifestStore = manifestStore ?? throw new ArgumentNullException(nameof(manifestStore));
        _launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
        _router = router ?? throw new ArgumentNullException(nameof(router));
        _commandBuilder = commandBuilder ?? throw new ArgumentNullException(nameof(commandBuilder));
        _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
    }

    public async IAsyncEnumerable<StreamPayload> RunModelAsync(
        string workspaceId,
        string manifestHash,
        ModelRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // 1. Validation via ModelManifestStore
        byte[] hashBytes;
        try
        {
            hashBytes = Convert.FromHexString(manifestHash);
        }
        catch (FormatException)
        {
            throw new ArgumentException("Invalid manifest hash format.", nameof(manifestHash));
        }

        var manifest = await _manifestStore.GetManifestForHashAsync(hashBytes, cancellationToken).ConfigureAwait(false);
        if (manifest == null)
        {
            throw new InvalidOperationException($"Failed to verify model manifest for hash: {manifestHash}");
        }

        // We assume the manifest contains the verified path to the executable model runner.
        string modelPath = manifest.ModelPath;

        // 2. Build Command Configuration
        var config = _commandBuilder.Build(modelPath, request);

        // 3. Launch via ModelHostProcessLauncher
        ModelHostSession session = await _launcher.LaunchAsync(config, cancellationToken).ConfigureAwait(false);

        try
        {
            // 4. Routing via ModelRequestRouter
            await foreach (var chunk in _router.RouteRequestAsync(session, request, cancellationToken).ConfigureAwait(false))
            {
                if (chunk.IsToolCall)
                {
                    var toolRequest = System.Text.Json.JsonSerializer.Deserialize(
                        chunk.Content,
                        ModelContractsJsonContext.Default.ToolRequest);
                    
                    if (toolRequest != null)
                    {
                        var toolResult = await _bridge.ExecuteToolAsync(toolRequest, cancellationToken).ConfigureAwait(false);
                        
                        if (session.Process != null && !session.Process.HasExited)
                        {
                            var stdin = session.Process.StandardInput;
                            var formattedResult = $"{{\"tool_result\": \"{toolRequest.ToolName}\", \"result\": \"{toolResult.Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "")}\"}}\n";
                            await stdin.WriteAsync(formattedResult.AsMemory(), cancellationToken).ConfigureAwait(false);
                            await stdin.FlushAsync(cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
                else
                {
                    yield return chunk;
                }
            }
        }
        finally
        {
            // Clean up Job Object handle
            if (session.JobObjectHandle != IntPtr.Zero)
            {
                CloseHandle(session.JobObjectHandle);
            }

            if (session.Process != null && !session.Process.HasExited)
            {
                try
                {
                    session.Process.Kill();
                }
                catch
                {
                    // Ignore errors during termination
                }
            }
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr hObject);
}
