using System.Diagnostics.CodeAnalysis;
using Opure.Ipc.Abstractions;
using Opure.Ipc.NamedPipes.Windows;

namespace Opure.Desktop.GatewayClient;

public static class RuntimeHealthEndpointEnvironment
{
    public static RuntimeHealthEndpoint? ReadCurrent()
    {
        Dictionary<string, string?> values = new(StringComparer.Ordinal)
        {
            ["OPURE_RUNTIME_PIPE_NAME"] = Environment.GetEnvironmentVariable(
                "OPURE_RUNTIME_PIPE_NAME"),
            ["OPURE_RUNTIME_BOOT_ID"] = Environment.GetEnvironmentVariable(
                "OPURE_RUNTIME_BOOT_ID")
        };

        if (TryCreate(values, out RuntimeHealthEndpoint? endpoint))
        {
            return endpoint;
        }

        string channel = Environment.GetEnvironmentVariable("OPURE_CHANNEL") ?? "Development";
        string dataRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string lockfilePath = Path.Combine(dataRoot, "Opure", channel, "runtime.lock");

        if (System.IO.File.Exists(lockfilePath))
        {
            try
            {
                using var stream = new System.IO.FileStream(
                    lockfilePath,
                    System.IO.FileMode.Open,
                    System.IO.FileAccess.Read,
                    System.IO.FileShare.ReadWrite | System.IO.FileShare.Delete);
                
                using var document = System.Text.Json.JsonDocument.Parse(stream);
                if (document.RootElement.TryGetProperty("pipeName", out var pipeName) &&
                    document.RootElement.TryGetProperty("bootId", out var bootId))
                {
                    values["OPURE_RUNTIME_PIPE_NAME"] = pipeName.GetString();
                    values["OPURE_RUNTIME_BOOT_ID"] = bootId.GetString();

                    if (TryCreate(values, out endpoint))
                    {
                        return endpoint;
                    }
                }
            }
            catch
            {
                // Ignore parsing errors and return null
            }
        }

        return null;
    }

    public static bool TryCreate(
        IReadOnlyDictionary<string, string?> values,
        [NotNullWhen(true)] out RuntimeHealthEndpoint? endpoint)
    {
        ArgumentNullException.ThrowIfNull(values);
        values.TryGetValue("OPURE_RUNTIME_PIPE_NAME", out string? pipeName);
        values.TryGetValue("OPURE_RUNTIME_BOOT_ID", out string? bootId);

        if (pipeName is null && bootId is null)
        {
            endpoint = null;
            return false;
        }

        RuntimeHealthEndpoint candidate = new(pipeName ?? string.Empty, bootId ?? string.Empty);

        if (!NamedPipeRuntimeHealthEndpoint.IsValid(candidate))
        {
            endpoint = null;
            return false;
        }

        endpoint = candidate;
        return true;
    }
}
