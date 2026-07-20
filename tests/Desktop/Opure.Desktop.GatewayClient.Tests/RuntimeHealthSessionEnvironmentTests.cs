using Opure.Desktop.GatewayClient;
using Opure.Ipc.Abstractions;
using Xunit;

namespace Opure.Desktop.GatewayClient.Tests;

public sealed class RuntimeHealthSessionEnvironmentTests
{
    private const string ValidSessionId = "0123456789abcdef0123456789abcdef";
    private const string ValidSessionSecret =
        "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFG";

    [Fact]
    public void TryCreate_rejects_null_values()
    {
        Assert.Throws<ArgumentNullException>(
            () => RuntimeHealthSessionEnvironment.TryCreate(null!, out _));
    }

    [Fact]
    public void TryCreate_returns_material_for_valid_values()
    {
        Dictionary<string, string?> values = new(StringComparer.Ordinal)
        {
            ["OPURE_BOOTSTRAP_SESSION_ID"] = ValidSessionId,
            ["OPURE_BOOTSTRAP_SESSION_SECRET"] = ValidSessionSecret
        };

        bool created = RuntimeHealthSessionEnvironment.TryCreate(
            values,
            out RuntimeHealthSessionMaterial? material);

        Assert.True(created);
        Assert.NotNull(material);
        Assert.Equal(ValidSessionId, material.SessionId);
        Assert.Equal(ValidSessionSecret, material.SessionSecret);
    }

    [Fact]
    public void TryCreate_accepts_generated_material()
    {
        RuntimeHealthSessionMaterial generated = RuntimeHealthSessionMaterial.Create();
        Dictionary<string, string?> values = new(StringComparer.Ordinal)
        {
            ["OPURE_BOOTSTRAP_SESSION_ID"] = generated.SessionId,
            ["OPURE_BOOTSTRAP_SESSION_SECRET"] = generated.SessionSecret
        };

        bool created = RuntimeHealthSessionEnvironment.TryCreate(
            values,
            out RuntimeHealthSessionMaterial? material);

        Assert.True(created);
        Assert.NotNull(material);
    }

    [Theory]
    [InlineData(null, ValidSessionSecret)]
    [InlineData("0123456789ABCDEF0123456789abcdef", ValidSessionSecret)]
    [InlineData("0123", ValidSessionSecret)]
    [InlineData(ValidSessionId, null)]
    [InlineData(ValidSessionId, "too-short")]
    [InlineData(ValidSessionId, "contains spaces and is exactly forty three!")]
    public void TryCreate_rejects_malformed_values(
        string? sessionId,
        string? sessionSecret)
    {
        Dictionary<string, string?> values = new(StringComparer.Ordinal)
        {
            ["OPURE_BOOTSTRAP_SESSION_ID"] = sessionId,
            ["OPURE_BOOTSTRAP_SESSION_SECRET"] = sessionSecret
        };

        bool created = RuntimeHealthSessionEnvironment.TryCreate(
            values,
            out RuntimeHealthSessionMaterial? material);

        Assert.False(created);
        Assert.Null(material);
    }

    [Fact]
    public void ReadCurrent_returns_null_when_environment_absent()
    {
        using EnvironmentVariableScope scope = new(
            ("OPURE_BOOTSTRAP_SESSION_ID", null),
            ("OPURE_BOOTSTRAP_SESSION_SECRET", null));

        Assert.Null(RuntimeHealthSessionEnvironment.ReadCurrent());
    }

    [Fact]
    public void ReadCurrent_returns_material_when_environment_valid()
    {
        using EnvironmentVariableScope scope = new(
            ("OPURE_BOOTSTRAP_SESSION_ID", ValidSessionId),
            ("OPURE_BOOTSTRAP_SESSION_SECRET", ValidSessionSecret));

        RuntimeHealthSessionMaterial? material =
            RuntimeHealthSessionEnvironment.ReadCurrent();

        Assert.NotNull(material);
        Assert.Equal(ValidSessionId, material.SessionId);
        Assert.Equal(ValidSessionSecret, material.SessionSecret);
    }
}
