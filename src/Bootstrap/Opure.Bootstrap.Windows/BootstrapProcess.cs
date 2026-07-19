using System.Diagnostics;

namespace Opure.Bootstrap.Windows;

internal enum BootstrapProcessClass
{
    Runtime = 0,
    Desktop = 1
}

internal sealed record BootstrapProcessStartRequest(
    BootstrapProcessClass ProcessClass,
    BootstrapBinaryIdentity Identity,
    IReadOnlyDictionary<string, string> Environment,
    IReadOnlyList<string> Arguments);

internal interface IBootstrapOwnedProcess : IAsyncDisposable
{
    int ProcessId { get; }

    DateTimeOffset StartTimeUtc { get; }

    bool HasExited { get; }

    int ExitCode { get; }

    ValueTask<string?> ReadOutputLineAsync(
        CancellationToken cancellationToken);

    Task<string> ReadErrorToEndAsync();

    Task<int> WaitForExitAsync();

    bool RequestGracefulStop();

    void KillTree();
}

internal interface IBootstrapProcessLauncher
{
    IBootstrapOwnedProcess Start(BootstrapProcessStartRequest request);
}

internal sealed class SystemBootstrapProcessLauncher :
    IBootstrapProcessLauncher,
    IDisposable
{
    private readonly IBootstrapProcessContainment containment;

    internal SystemBootstrapProcessLauncher()
        : this(new WindowsBootstrapJobObject())
    {
    }

    internal SystemBootstrapProcessLauncher(
        IBootstrapProcessContainment containment)
    {
        this.containment =
            containment ?? throw new ArgumentNullException(nameof(containment));
    }

    public IBootstrapOwnedProcess Start(
        BootstrapProcessStartRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        ProcessStartInfo startInfo = new()
        {
            FileName = request.Identity.ExecutablePath,
            WorkingDirectory =
                Path.GetDirectoryName(request.Identity.ExecutablePath)
                ?? throw new InvalidOperationException(
                    "Bootstrap child working directory is unavailable."),
            UseShellExecute = false,
            RedirectStandardInput =
                request.ProcessClass == BootstrapProcessClass.Runtime,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow =
                request.ProcessClass == BootstrapProcessClass.Runtime
        };

        foreach ((string name, string value) in request.Environment)
        {
            startInfo.Environment[name] = value;
        }

        foreach (string argument in request.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        Process process = new()
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };
        bool started = false;

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException(
                    "Bootstrap child process did not start.");
            }

            started = true;
            containment.Assign(process);

            return new SystemBootstrapOwnedProcess(
                process,
                request.ProcessClass);
        }
        catch
        {
            if (started && !process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit();
            }

            process.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        containment.Dispose();
    }
}

internal sealed class SystemBootstrapOwnedProcess : IBootstrapOwnedProcess
{
    private readonly Process process;
    private readonly BootstrapProcessClass processClass;

    internal SystemBootstrapOwnedProcess(
        Process process,
        BootstrapProcessClass processClass)
    {
        this.process = process ?? throw new ArgumentNullException(nameof(process));
        this.processClass = processClass;
    }

    public int ProcessId => process.Id;

    public DateTimeOffset StartTimeUtc =>
        new(process.StartTime.ToUniversalTime(), TimeSpan.Zero);

    public bool HasExited => process.HasExited;

    public int ExitCode => process.ExitCode;

    public async ValueTask<string?> ReadOutputLineAsync(
        CancellationToken cancellationToken)
    {
        return await process.StandardOutput
            .ReadLineAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<string> ReadErrorToEndAsync()
    {
        return process.StandardError.ReadToEndAsync();
    }

    public async Task<int> WaitForExitAsync()
    {
        await process.WaitForExitAsync().ConfigureAwait(false);
        return process.ExitCode;
    }

    public bool RequestGracefulStop()
    {
        if (process.HasExited)
        {
            return true;
        }

        if (processClass == BootstrapProcessClass.Runtime)
        {
            process.StandardInput.Close();
            return true;
        }

        return process.CloseMainWindow();
    }

    public void KillTree()
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
        }
    }

    public ValueTask DisposeAsync()
    {
        process.Dispose();
        return ValueTask.CompletedTask;
    }
}
