using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Opure.Desktop.GatewayClient;

/// <summary>
/// Activates an Opure Pro licence by delegating to the Runtime CLI process.
/// The Desktop has no direct authority over the activation state; it routes
/// all writes through the Runtime's verifier.
/// </summary>
public static class LicenseGatewayClient
{
    public static async Task<bool> ApplyLicenseAsync(string licenseKey)
    {
        if (string.IsNullOrWhiteSpace(licenseKey))
        {
            return false;
        }

        try
        {
            string runtimeExe = ResolveRuntimeExecutable();

            if (!File.Exists(runtimeExe))
            {
                return false;
            }

            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = runtimeExe,
                ArgumentList = { "pro", "activate", licenseKey },
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                CreateNoWindow = true
            };

            process.Start();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            string output = await process.StandardOutput
                .ReadToEndAsync(cts.Token)
                .ConfigureAwait(false);

            await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);

            return process.ExitCode == 0 &&
                   output.Contains("Opure Pro activated", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static string ResolveRuntimeExecutable()
    {
        // In an installed environment the Runtime lives beside the Desktop.
        string baseDir = AppContext.BaseDirectory;
        string sideBySide = Path.Combine(baseDir, "Opure.Runtime.exe");
        if (File.Exists(sideBySide))
        {
            return sideBySide;
        }

        // Fall back to the Runtime subfolder (legacy staging layout).
        return Path.Combine(baseDir, "Runtime", "Opure.Runtime.exe");
    }
}
