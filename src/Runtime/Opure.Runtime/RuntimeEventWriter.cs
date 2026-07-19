using System.Text.Json;
using System.Text.Json.Serialization;
using Opure.Runtime.Contracts;

namespace Opure.Runtime;

public static class RuntimeEventWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static Task WriteLifecycleAsync(
        TextWriter output,
        int sequence,
        RuntimeLifecycleState state,
        RuntimeBootSnapshot bootSnapshot,
        string dataRootScope,
        string? shutdownReason,
        string? runtimeHealthPipe = null)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(bootSnapshot);

        string json = JsonSerializer.Serialize(
            new
            {
                @event = "runtime.lifecycle",
                sequence,
                state = state.ToString().ToLowerInvariant(),
                bootId = bootSnapshot.BootId,
                processId = bootSnapshot.ProcessId,
                productVersion = bootSnapshot.ProductVersion,
                contractVersion = bootSnapshot.ContractVersion,
                dataRootScope,
                shutdownReason,
                runtimeHealthPipe,
                networkAccess = "disabled"
            },
            SerializerOptions);

        return output.WriteLineAsync(json);
    }

    public static Task WriteFailureAsync(
        TextWriter output,
        RuntimeExitCode exitCode,
        string category,
        string safeMessage,
        string? exceptionType)
    {
        ArgumentNullException.ThrowIfNull(output);

        string json = JsonSerializer.Serialize(
            new
            {
                @event = "runtime.failure",
                category,
                exitCode = (int)exitCode,
                message = safeMessage,
                exceptionType
            },
            SerializerOptions);

        return output.WriteLineAsync(json);
    }
}
