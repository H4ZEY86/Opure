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

        return TryCreate(values, out RuntimeHealthEndpoint? endpoint)
            ? endpoint
            : null;
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
