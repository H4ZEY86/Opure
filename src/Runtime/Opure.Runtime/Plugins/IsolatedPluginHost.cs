using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Opure.Runtime.Contracts.Plugins;
using Opure.Runtime.Contracts.Providers;
using Opure.Workspace.Execution.Models;

namespace Opure.Runtime.Plugins;

public sealed class IsolatedPluginHost : IPluginHost, IDisposable
{
    private Process? _process;
    private WindowsJobObject? _jobObject;

    public Task StartAsync(PluginPackageRecord package, CapabilityLease lease, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(lease);

        if (package.State != PluginQuarantineState.Approved)
        {
            throw new PluginHostException($"Cannot start plugin. Quarantine state is {package.State}");
        }

        if (lease.Status != ApprovalStatus.Active)
        {
            throw new PluginHostException($"Cannot start plugin. Lease status is {lease.Status}");
        }

        if (lease.ExpiresAt.HasValue && lease.ExpiresAt.Value < DateTimeOffset.UtcNow)
        {
            throw new PluginHostException("Cannot start plugin. Lease has expired.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = package.InstalledPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        _process = new Process { StartInfo = startInfo };
        
        try
        {
            _process.Start();
        }
        catch (Exception ex)
        {
            throw new PluginHostException("Failed to start plugin process.", ex);
        }

        try
        {
            _jobObject = new WindowsJobObject();
            _jobObject.AssignProcess(_process);
        }
        catch (Exception ex)
        {
            try
            {
                _process.Kill(true);
            }
            catch
            {
                // Ignore kill errors during failure
            }
            throw new PluginHostException("Failed to attach plugin process to Windows Job Object.", ex);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        if (_process != null && !_process.HasExited)
        {
            try
            {
                _process.Kill(true);
            }
            catch
            {
                // Process may have already exited
            }
        }

        _jobObject?.Dispose();
        _jobObject = null;
        
        _process?.Dispose();
        _process = null;

        return Task.CompletedTask;
    }

    public async Task<string> SendCommandAsync(string payload, CancellationToken ct)
    {
        if (_process == null || _process.HasExited)
        {
            throw new PluginHostException("Plugin process is not running.");
        }

        await _process.StandardInput.WriteLineAsync(payload.AsMemory(), ct).ConfigureAwait(false);
        await _process.StandardInput.FlushAsync(ct).ConfigureAwait(false);

        var response = await _process.StandardOutput.ReadLineAsync(ct).ConfigureAwait(false);
        if (response == null)
        {
            throw new PluginHostException("Plugin process closed standard output unexpectedly.");
        }

        return response;
    }

    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
    }
}
