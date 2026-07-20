using Opure.Desktop.GatewayClient;
using Opure.Ipc.Abstractions;
using Opure.Ipc.NamedPipes.Windows;
using Xunit;

namespace Opure.Desktop.GatewayClient.Tests;

public sealed class RuntimeHealthEndpointEnvironmentTests
{
    private const string ValidBootId = "0123456789abcdef0123456789abcdef";

    [Fact]
    public void TryCreate_rejects_null_values()
    {
        Assert.Throws<ArgumentNullException>(
            () => RuntimeHealthEndpointEnvironment.TryCreate(null!, out _));
    }

    [Fact]
    public void TryCreate_returns_false_when_both_values_absent()
    {
        Dictionary<string, string?> values = new(StringComparer.Ordinal);

        bool created = RuntimeHealthEndpointEnvironment.TryCreate(
            values,
            out RuntimeHealthEndpoint? endpoint);

        Assert.False(created);
        Assert.Null(endpoint);
    }

    [Fact]
    public void TryCreate_returns_false_for_malformed_endpoint()
    {
        Dictionary<string, string?> values = new(StringComparer.Ordinal)
        {
            ["OPURE_RUNTIME_PIPE_NAME"] = "not-a-valid-pipe",
            ["OPURE_RUNTIME_BOOT_ID"] = "short"
        };

        bool created = RuntimeHealthEndpointEnvironment.TryCreate(
            values,
            out RuntimeHealthEndpoint? endpoint);

        Assert.False(created);
        Assert.Null(endpoint);
    }

    [Fact]
    public void TryCreate_returns_false_when_only_one_value_present()
    {
        Dictionary<string, string?> values = new(StringComparer.Ordinal)
        {
            ["OPURE_RUNTIME_PIPE_NAME"] = null,
            ["OPURE_RUNTIME_BOOT_ID"] = ValidBootId
        };

        bool created = RuntimeHealthEndpointEnvironment.TryCreate(
            values,
            out RuntimeHealthEndpoint? endpoint);

        Assert.False(created);
        Assert.Null(endpoint);
    }

    [Fact]
    public void TryCreate_returns_endpoint_for_valid_values()
    {
        RuntimeHealthEndpoint expected = NamedPipeRuntimeHealthEndpoint.Create(
            "stable",
            ValidBootId);
        Dictionary<string, string?> values = new(StringComparer.Ordinal)
        {
            ["OPURE_RUNTIME_PIPE_NAME"] = expected.PipeName,
            ["OPURE_RUNTIME_BOOT_ID"] = expected.RuntimeBootId
        };

        bool created = RuntimeHealthEndpointEnvironment.TryCreate(
            values,
            out RuntimeHealthEndpoint? endpoint);

        Assert.True(created);
        Assert.NotNull(endpoint);
        Assert.Equal(expected.PipeName, endpoint.PipeName);
        Assert.Equal(expected.RuntimeBootId, endpoint.RuntimeBootId);
    }

    [Fact]
    public void ReadCurrent_returns_null_when_environment_absent()
    {
        using EnvironmentVariableScope scope = new(
            ("OPURE_RUNTIME_PIPE_NAME", null),
            ("OPURE_RUNTIME_BOOT_ID", null));

        Assert.Null(RuntimeHealthEndpointEnvironment.ReadCurrent());
    }

    [Fact]
    public void ReadCurrent_returns_endpoint_when_environment_valid()
    {
        RuntimeHealthEndpoint expected = NamedPipeRuntimeHealthEndpoint.Create(
            "preview",
            ValidBootId);
        using EnvironmentVariableScope scope = new(
            ("OPURE_RUNTIME_PIPE_NAME", expected.PipeName),
            ("OPURE_RUNTIME_BOOT_ID", expected.RuntimeBootId));

        RuntimeHealthEndpoint? endpoint = RuntimeHealthEndpointEnvironment.ReadCurrent();

        Assert.NotNull(endpoint);
        Assert.Equal(expected.PipeName, endpoint.PipeName);
        Assert.Equal(expected.RuntimeBootId, endpoint.RuntimeBootId);
    }
}
