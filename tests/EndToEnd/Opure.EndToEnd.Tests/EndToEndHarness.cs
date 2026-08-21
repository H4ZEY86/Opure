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

    public EndToEndHarness(string additionalArguments = "", System.Collections.Generic.Dictionary<string, string>? environmentVariables = null)
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
            Arguments = $"--channel Test --layout Development --configuration {configuration} {additionalArguments}".Trim(),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        // Tell bootstrap to start in test mode so we can control desktop lifecycle
        startInfo.Environment["OPURE_BOOTSTRAP_TEST_MODE"] = "1";

        if (environmentVariables != null)
        {
            foreach (var kvp in environmentVariables)
            {
                startInfo.Environment[kvp.Key] = kvp.Value;
            }
        }

        BootstrapProcess = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start Bootstrap process.");
            
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
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
    var stdoutLog = new System.Text.StringBuilder();
    
    while (!linked.Token.IsCancellationRequested)
    {
        string? line = await BootstrapProcess.StandardOutput.ReadLineAsync(linked.Token);
        if (line != null)
        {
            stdoutLog.AppendLine(line);
            if (line.Contains("\"kind\":\"ipc.session\""))
            {
                try
                {
                    using var document = System.Text.Json.JsonDocument.Parse(line);
                    var root = document.RootElement;
                    if (root.TryGetProperty("OPURE_IPC_PIPE", out var pipe) &&
                        root.TryGetProperty("OPURE_RUNTIME_BOOT_ID", out var bootId) &&
                        root.TryGetProperty("OPURE_BOOTSTRAP_SESSION_ID", out var sessionId) &&
                        root.TryGetProperty("OPURE_BOOTSTRAP_SESSION_SECRET", out var sessionSecret))
                    {
                        var env = new System.Collections.Generic.Dictionary<string, string>
                        {
                            ["OPURE_IPC_PIPE"] = pipe.GetString()!,
                            ["OPURE_RUNTIME_BOOT_ID"] = bootId.GetString()!,
                            ["OPURE_BOOTSTRAP_SESSION_ID"] = sessionId.GetString()!,
                            ["OPURE_BOOTSTRAP_SESSION_SECRET"] = sessionSecret.GetString()!
                        };
                        Environment.SetEnvironmentVariable("OPURE_RUNTIME_PIPE_NAME", env["OPURE_IPC_PIPE"]);
                        Environment.SetEnvironmentVariable("OPURE_RUNTIME_BOOT_ID", env["OPURE_RUNTIME_BOOT_ID"]);
                        Environment.SetEnvironmentVariable("OPURE_BOOTSTRAP_SESSION_ID", env["OPURE_BOOTSTRAP_SESSION_ID"]);
                        Environment.SetEnvironmentVariable("OPURE_BOOTSTRAP_SESSION_SECRET", env["OPURE_BOOTSTRAP_SESSION_SECRET"]);
                        return env;
                    }
                }
                catch (System.Text.Json.JsonException) { }
            }
        }
        
        if (line == null && BootstrapProcess.HasExited)
        {
            throw new InvalidOperationException($"Bootstrap process exited prematurely with exit code {BootstrapProcess.ExitCode}. Stdout: {stdoutLog}");
        }
    }
    
    throw new TimeoutException("Test session line was not emitted within the timeout.");
}

    private static System.Collections.Generic.Dictionary<string, string>? ParseSessionJson(string line)
    {
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(line);
            var root = document.RootElement;
            if (root.TryGetProperty("OPURE_IPC_PIPE", out var pipe) &&
                root.TryGetProperty("OPURE_RUNTIME_BOOT_ID", out var bootId) &&
                root.TryGetProperty("OPURE_BOOTSTRAP_SESSION_ID", out var sessionId) &&
                root.TryGetProperty("OPURE_BOOTSTRAP_SESSION_SECRET", out var sessionSecret))
            {
                var env = new System.Collections.Generic.Dictionary<string, string>
                {
                    ["OPURE_IPC_PIPE"] = pipe.GetString()!,
                    ["OPURE_RUNTIME_BOOT_ID"] = bootId.GetString()!,
                    ["OPURE_BOOTSTRAP_SESSION_ID"] = sessionId.GetString()!,
                    ["OPURE_BOOTSTRAP_SESSION_SECRET"] = sessionSecret.GetString()!
                };
                Environment.SetEnvironmentVariable("OPURE_RUNTIME_PIPE_NAME", env["OPURE_IPC_PIPE"]);
                Environment.SetEnvironmentVariable("OPURE_RUNTIME_BOOT_ID", env["OPURE_RUNTIME_BOOT_ID"]);
                Environment.SetEnvironmentVariable("OPURE_BOOTSTRAP_SESSION_ID", env["OPURE_BOOTSTRAP_SESSION_ID"]);
                Environment.SetEnvironmentVariable("OPURE_BOOTSTRAP_SESSION_SECRET", env["OPURE_BOOTSTRAP_SESSION_SECRET"]);
                return env;
            }
        }
        catch (System.Text.Json.JsonException) { }
        return null;
    }

    private static System.Collections.Generic.Dictionary<string, string>? TryParseSessionLine(string line)
    {
        if (line.Contains("\"kind\":\"ipc.session\""))
        {
            return ParseSessionJson(line);
        }
        return null;
    }

    public void Dispose()
    {
        // 1. Terminate the Bootstrap process
        if (!BootstrapProcess.HasExited)
        {
            try { BootstrapProcess.Kill(true); } catch { }
        }
        BootstrapProcess.Dispose();

        // 2. Terminate any orphaned Runtime processes holding SQLite locks
        foreach (var process in Process.GetProcessesByName("Opure.Runtime"))
        {
            try 
            { 
                process.Kill(true); 
                process.WaitForExit(1000); // Give the OS a second to flush handles
            } 
            catch { }
        }

        // 3. Clear SQLite connection pools and add explicit backoff to release local file locks
        try
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            Thread.Sleep(100);
        }
        catch { }

        // 4. Now that handles are released, cleanly wipe the state for the next test
        if (Directory.Exists(DataRoot))
        {
            // Try up to 3 times to account for lingering OS locks
            for (int i = 0; i < 3; i++)
            {
                try 
                { 
                    Directory.Delete(DataRoot, recursive: true); 
                    break; 
                } 
                catch when (i < 2) 
                { 
                    Thread.Sleep(500); 
                }
                catch { }
            }
        }
    }
}
