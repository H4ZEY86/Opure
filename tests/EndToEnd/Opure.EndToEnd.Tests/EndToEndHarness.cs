using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Opure.EndToEnd.Tests;

public sealed class EndToEndHarness : IDisposable
{
    public Process BootstrapProcess { get; }
    public string DataRoot { get; }

    public EndToEndHarness(string additionalArguments = "")
    {
        string repositoryRoot = GetRepositoryRoot();
        string configuration = "Debug";
#if RELEASE
        configuration = "Release";
#endif

        string bootstrapExecutable = Path.Combine(
            repositoryRoot,
            "artifacts",
            "bin",
            "Opure.Bootstrap.Windows",
            configuration,
            "Opure.Bootstrap.Windows.exe");

        if (!File.Exists(bootstrapExecutable))
        {
            throw new FileNotFoundException($"Bootstrap executable not found: {bootstrapExecutable}");
        }

        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify);
        DataRoot = Path.Combine(localAppData, "Opure", "Test");

        // Clean up any state from previous aborted runs
        if (Directory.Exists(DataRoot))
        {
            try { Directory.Delete(DataRoot, recursive: true); } catch { }
        }
        Directory.CreateDirectory(DataRoot);

        var startInfo = new ProcessStartInfo
        {
            FileName = bootstrapExecutable,
            Arguments = $"--channel Test --layout Development {additionalArguments}".Trim(),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        // Tell bootstrap to start in test mode so we can control desktop lifecycle
        startInfo.Environment["OPURE_BOOTSTRAP_TEST_MODE"] = "1";

        BootstrapProcess = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start Bootstrap process.");
            
        Task.Run(() => 
        {
            try {
                using var fs = new FileStream(Path.Combine(DataRoot, "bootstrap-log.txt"), FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                using var sw = new StreamWriter(fs);
                while (!BootstrapProcess.StandardOutput.EndOfStream)
                {
                    string? line = BootstrapProcess.StandardOutput.ReadLine();
                    if (line != null) sw.WriteLine(line);
                }
            } catch { }
        });
        Task.Run(() => 
        {
            try {
                using var fs = new FileStream(Path.Combine(DataRoot, "bootstrap-err.txt"), FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                using var sw = new StreamWriter(fs);
                while (!BootstrapProcess.StandardError.EndOfStream)
                {
                    string? line = BootstrapProcess.StandardError.ReadLine();
                    if (line != null) sw.WriteLine(line);
                }
            } catch { }
        });
    }

    private static string GetRepositoryRoot()
    {
        string? current = AppContext.BaseDirectory;
        while (current != null)
        {
            if (File.Exists(Path.Combine(current, "Opure.slnx")))
            {
                return current;
            }
            current = Path.GetDirectoryName(current);
        }
        throw new InvalidOperationException("Could not find repository root.");
    }

    public async Task<System.Collections.Generic.Dictionary<string, string>> GetTestSessionAsync(CancellationToken cancellationToken = default)
    {
        string sessionPath = Path.Combine(DataRoot, "test-session.json");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        
        while (!linked.Token.IsCancellationRequested)
        {
            if (File.Exists(sessionPath))
            {
                try
                {
                    string json = await File.ReadAllTextAsync(sessionPath, linked.Token);
                    var env = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, string>>(json);
                    if (env != null && env.ContainsKey("OPURE_IPC_PIPE"))
                    {
                        return env;
                    }
                }
                catch (IOException)
                {
                    // File might be in use, retry
                }
            }
            await Task.Delay(100, linked.Token);
        }
        
        throw new TimeoutException("Test session file was not created within the timeout.");
    }

    public void Dispose()
    {
        if (!BootstrapProcess.HasExited)
        {
            BootstrapProcess.Kill(true);
        }
        BootstrapProcess.Dispose();

        try
        {
            if (Directory.Exists(DataRoot))
            {
                Directory.Delete(DataRoot, recursive: true);
            }
        }
        catch { /* ignore cleanup errors */ }
    }
}


