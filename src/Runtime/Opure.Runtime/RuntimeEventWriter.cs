using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics;
using Opure.Observability.Contracts;
using Opure.Runtime.Contracts;
using Opure.Ipc.Abstractions;

namespace Opure.Runtime;

public static class RuntimeEventWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task WriteLifecycleAsync(
        TextWriter output,
        int sequence,
        RuntimeLifecycleState state,
        RuntimeBootSnapshot bootSnapshot,
        string dataRootScope,
        string? shutdownReason,
        string? runtimeHealthPipe = null,
        IOperationalLogger? operationalLogger = null)
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

        await output.WriteLineAsync(json).ConfigureAwait(false);

        if (operationalLogger is not null)
        {
            List<OperationalLogAttribute> attributes =
            [
                OperationalLogAttribute.Integer("sequence", sequence),
                OperationalLogAttribute.String(
                    "lifecycle.state",
                    state.ToString().ToLowerInvariant()),
                OperationalLogAttribute.String("dataRoot.scope", dataRootScope),
                OperationalLogAttribute.Integer("process.id", bootSnapshot.ProcessId),
                OperationalLogAttribute.String(
                    "contract.version",
                    bootSnapshot.ContractVersion),
                OperationalLogAttribute.String("network.access", "disabled")
            ];

            if (shutdownReason is not null)
            {
                attributes.Add(OperationalLogAttribute.String(
                    "shutdown.reason",
                    shutdownReason));
            }

            _ = await operationalLogger.WriteAsync(
                RuntimeOperationalEvents.ForLifecycle(state),
                attributes,
                traceId: Activity.Current?.TraceId.ToHexString())
                .ConfigureAwait(false);
        }
    }

    public static async Task WriteFailureAsync(
        TextWriter output,
        RuntimeExitCode exitCode,
        string category,
        string safeMessage,
        string? exceptionType,
        IOperationalLogger? operationalLogger = null)
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

        await output.WriteLineAsync(json).ConfigureAwait(false);

        if (operationalLogger is not null)
        {
            List<OperationalLogAttribute> attributes =
            [
                OperationalLogAttribute.Integer("exit.code", (int)exitCode),
                OperationalLogAttribute.String("failure.category", category)
            ];

            if (exceptionType is not null)
            {
                attributes.Add(OperationalLogAttribute.String(
                    "exception.type",
                    exceptionType));
            }

            _ = await operationalLogger.WriteAsync(
                RuntimeOperationalEvents.Failed,
                attributes,
                traceId: Activity.Current?.TraceId.ToHexString())
                .ConfigureAwait(false);
        }
    }

    public static async ValueTask WriteIpcSessionAsync(
        TextWriter output,
        RuntimeHealthAuthenticationEvent authenticationEvent,
        IOperationalLogger? operationalLogger = null)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(authenticationEvent);

        string json = JsonSerializer.Serialize(
            new
            {
                @event = authenticationEvent.Established
                    ? "ipc.session-established"
                    : "ipc.session-denied",
                reasonCode = authenticationEvent.ReasonCode,
                clientProcessId = authenticationEvent.ClientProcessId,
                containsSessionMaterial = false
            },
            SerializerOptions);

        await output.WriteLineAsync(json).ConfigureAwait(false);

        if (operationalLogger is not null)
        {
            OperationalLogEventDefinition definition =
                authenticationEvent.Established
                    ? RuntimeOperationalEvents.IpcSessionEstablished
                    : RuntimeOperationalEvents.IpcSessionDenied;
            List<OperationalLogAttribute> attributes =
            [
                OperationalLogAttribute.String(
                    "admission.reasonCode",
                    authenticationEvent.ReasonCode),
                OperationalLogAttribute.Boolean(
                    "session.materialIncluded",
                    false)
            ];

            if (authenticationEvent.ClientProcessId is int clientProcessId)
            {
                attributes.Add(OperationalLogAttribute.Integer(
                    "client.processId",
                    clientProcessId));
            }

            _ = await operationalLogger.WriteAsync(
                definition,
                attributes,
                traceId: Activity.Current?.TraceId.ToHexString())
                .ConfigureAwait(false);
        }
    }
}
