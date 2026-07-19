using System.Text.Json;
using System.Text.Json.Serialization;

namespace Opure.Bootstrap.Windows;

internal static class BootstrapEventWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    internal static Task WriteLifecycleAsync(
        TextWriter output,
        string state,
        BootstrapChannel channel,
        string? reason = null)
    {
        return WriteAsync(
            output,
            new
            {
                @event = "bootstrap.lifecycle",
                state,
                channel = channel.ToString(),
                reason
            });
    }

    internal static Task WriteChildStartedAsync(
        TextWriter output,
        BootstrapSupervisedProcessIdentity processIdentity,
        BootstrapBinaryIdentity binaryIdentity)
    {
        ArgumentNullException.ThrowIfNull(processIdentity);
        ArgumentNullException.ThrowIfNull(binaryIdentity);

        return WriteAsync(
            output,
            new
            {
                @event = "bootstrap.child.started",
                processClass = processIdentity.ProcessClass
                    .ToString()
                    .ToLowerInvariant(),
                processIdentity.InstanceId,
                processIdentity.ProcessId,
                processIdentity.StartTimeUtc,
                executableName = binaryIdentity.ExecutableName,
                assemblyName = binaryIdentity.AssemblyName,
                processIdentity.ProductVersion,
                processIdentity.ExecutableSha256,
                jobOwned = true
            });
    }

    internal static Task WriteRuntimeReadyAsync(
        TextWriter output,
        BootstrapSupervisedProcessIdentity processIdentity)
    {
        ArgumentNullException.ThrowIfNull(processIdentity);

        return WriteAsync(
            output,
            new
            {
                @event = "bootstrap.child.ready",
                processClass = "runtime",
                processIdentity.InstanceId,
                processIdentity.BootId
            });
    }

    internal static Task WriteChildExitedAsync(
        TextWriter output,
        BootstrapSupervisedProcessIdentity processIdentity,
        int exitCode,
        BootstrapProcessExitKind exitKind,
        bool expected,
        bool restartEligible)
    {
        ArgumentNullException.ThrowIfNull(processIdentity);

        return WriteAsync(
            output,
            new
            {
                @event = "bootstrap.child.exited",
                processClass = processIdentity.ProcessClass
                    .ToString()
                    .ToLowerInvariant(),
                processIdentity.InstanceId,
                processIdentity.ProcessId,
                processIdentity.StartTimeUtc,
                exitCode,
                classification = ToWireValue(exitKind),
                expected,
                restartEligible
            });
    }

    internal static Task WriteSupervisorStateAsync(
        TextWriter output,
        BootstrapSupervisorMode mode,
        BootstrapProcessHealth runtimeHealth,
        int restartCount,
        string reason,
        TimeSpan? nextRestartDelay = null)
    {
        return WriteAsync(
            output,
            new
            {
                @event = "bootstrap.supervisor.state",
                mode = ToWireValue(mode),
                runtimeHealth = runtimeHealth.ToString().ToLowerInvariant(),
                restartCount,
                reason,
                nextRestartDelayMilliseconds = nextRestartDelay is null
                    ? (int?)null
                    : (int)nextRestartDelay.Value.TotalMilliseconds
            });
    }

    internal static Task WriteFailureAsync(
        TextWriter output,
        BootstrapExitCode exitCode,
        string category,
        string safeMessage,
        string? exceptionType = null)
    {
        return WriteAsync(
            output,
            new
            {
                @event = "bootstrap.failure",
                category,
                exitCode = (int)exitCode,
                message = safeMessage,
                exceptionType
            });
    }

    private static Task WriteAsync(
        TextWriter output,
        object value)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(value);

        string json = JsonSerializer.Serialize(
            value,
            SerializerOptions);

        return output.WriteLineAsync(json);
    }

    private static string ToWireValue(BootstrapProcessExitKind exitKind)
    {
        return exitKind switch
        {
            BootstrapProcessExitKind.CleanExit => "clean_exit",
            BootstrapProcessExitKind.Crash => "crash",
            BootstrapProcessExitKind.PolicyStop => "policy_stop",
            BootstrapProcessExitKind.Quarantine => "quarantine",
            _ => throw new ArgumentOutOfRangeException(
                nameof(exitKind),
                exitKind,
                null)
        };
    }

    private static string ToWireValue(BootstrapSupervisorMode mode)
    {
        return mode switch
        {
            BootstrapSupervisorMode.Normal => "normal",
            BootstrapSupervisorMode.Recovering => "recovering",
            BootstrapSupervisorMode.SafeMode => "safe_mode",
            _ => throw new ArgumentOutOfRangeException(
                nameof(mode),
                mode,
                null)
        };
    }
}
