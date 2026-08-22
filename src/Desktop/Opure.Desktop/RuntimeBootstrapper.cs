using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;

namespace Opure.Desktop;

internal static class RuntimeBootstrapper
{
    private static Process? sidecarProcess;

    internal static void StartRuntime()
    {
        string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Opure_Boot_Debug.txt");
        File.WriteAllText(logPath, $"--- Opure Bootstrapper Log ---\nTime: {DateTime.Now}\n");

        string[] searchPaths = 
        {
            Path.Combine(AppContext.BaseDirectory, "Runtime", "Opure.Runtime.exe"),
            Path.Combine(AppContext.BaseDirectory, "Opure.Runtime.exe")
        };

        string? runtimePath = null;
        foreach (var path in searchPaths)
        {
            if (File.Exists(path))
            {
                runtimePath = path;
                break;
            }
        }

        File.AppendAllText(logPath, $"Resolved Path: {runtimePath}\nFile Exists: {(runtimePath != null ? File.Exists(runtimePath) : false)}\n");

        if (runtimePath is null)
        {
            string errorMsg = $"[Desktop] HEALTH_TRANSPORT_ENDPOINT_INVALID: Failed to locate Runtime sidecar. Attempted paths: {string.Join(", ", searchPaths)}";
            Console.Error.WriteLine(errorMsg);
            File.AppendAllText(logPath, $"{errorMsg}\n");
            return;
        }

        try
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = runtimePath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            string sessionId = Guid.NewGuid().ToString("N");
            string sessionSecret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            
            string channel = Environment.GetEnvironmentVariable("OPURE_CHANNEL") ?? "Development";
            string dataRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            
            startInfo.Environment["OPURE_BOOTSTRAP_MANAGED"] = "1";
            startInfo.Environment["OPURE_BOOTSTRAP_SESSION_ID"] = sessionId;
            startInfo.Environment["OPURE_BOOTSTRAP_SESSION_SECRET"] = sessionSecret;
            startInfo.Environment["OPURE_BOOTSTRAP_PARENT_PID"] = Environment.ProcessId.ToString();
            startInfo.Environment["OPURE_BOOTSTRAP_PARENT_START_UTC"] = Process.GetCurrentProcess().StartTime.ToUniversalTime().ToString("O");
            startInfo.Environment["OPURE_CHANNEL"] = channel;
            startInfo.Environment["OPURE_DATA_ROOT"] = Path.Combine(dataRoot, "Opure", channel);

            sidecarProcess = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            if (!sidecarProcess.Start())
            {
                string errorMsg = $"[Desktop] Failed to start Runtime sidecar process from path: {runtimePath}";
                Console.Error.WriteLine(errorMsg);
                File.AppendAllText(logPath, $"{errorMsg}\n");
                return;
            }

            Environment.SetEnvironmentVariable("OPURE_BOOTSTRAP_SESSION_ID", sessionId);
            Environment.SetEnvironmentVariable("OPURE_BOOTSTRAP_SESSION_SECRET", sessionSecret);

            bool isReady = false;
            while (!isReady && !sidecarProcess.StandardOutput.EndOfStream)
            {
                string? line = sidecarProcess.StandardOutput.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                File.AppendAllText(logPath, $"STDOUT: {line}\n");

                try
                {
                    using JsonDocument document = JsonDocument.Parse(line);
                    JsonElement root = document.RootElement;
                    
                    if (root.TryGetProperty("event", out JsonElement eventName) && 
                        eventName.GetString() == "runtime.lifecycle")
                    {
                        if (root.TryGetProperty("state", out JsonElement state) &&
                            state.GetString() == "ready")
                        {
                            if (root.TryGetProperty("bootId", out JsonElement bootIdElement) &&
                                root.TryGetProperty("runtimeHealthPipe", out JsonElement pipeNameElement))
                            {
                                Environment.SetEnvironmentVariable("OPURE_RUNTIME_PIPE_NAME", pipeNameElement.GetString());
                                Environment.SetEnvironmentVariable("OPURE_RUNTIME_BOOT_ID", bootIdElement.GetString());
                                isReady = true;
                                File.AppendAllText(logPath, $"Successfully parsed ready event. BootId: {bootIdElement.GetString()}\n");
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore non-json or malformed output during startup
                }
            }

            if (sidecarProcess.HasExited)
            {
                string stderr = sidecarProcess.StandardError.ReadToEnd();
                File.AppendAllText(logPath, $"Process exited unexpectedly with code {sidecarProcess.ExitCode}.\nSTDERR:\n{stderr}\n");
            }

            _ = Task.Run(async () => 
            {
                try
                {
                    while (!sidecarProcess.StandardOutput.EndOfStream)
                    {
                        await sidecarProcess.StandardOutput.ReadLineAsync().ConfigureAwait(false);
                    }
                }
                catch { }
            });
            _ = Task.Run(async () => 
            {
                try
                {
                    while (!sidecarProcess.StandardError.EndOfStream)
                    {
                        await sidecarProcess.StandardError.ReadLineAsync().ConfigureAwait(false);
                    }
                }
                catch { }
            });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Desktop] Failed to start Runtime sidecar. Attempted path: {runtimePath}. Error: {ex.Message}");
            File.AppendAllText(logPath, $"EXCEPTION:\n{ex.ToString()}\n");
            
            try 
            {
                if (sidecarProcess != null && sidecarProcess.HasExited)
                {
                    string stderr = sidecarProcess.StandardError.ReadToEnd();
                    File.AppendAllText(logPath, $"STDERR:\n{stderr}\n");
                }
            }
            catch { }
        }
    }
}
