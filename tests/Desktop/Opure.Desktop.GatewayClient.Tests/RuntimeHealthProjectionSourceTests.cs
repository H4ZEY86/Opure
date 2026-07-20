using Opure.Desktop.Contracts;
using Opure.Desktop.GatewayClient;
using Opure.Ipc.Abstractions;
using Xunit;

namespace Opure.Desktop.GatewayClient.Tests;

public sealed class RuntimeHealthProjectionSourceTests
{
    private const string ProductVersion = "1.2.3";

    private static RuntimeHealthProjectionSource CreateSource(
        DesktopSupervisorProjection supervisor,
        RuntimeHealthEndpoint? endpoint,
        RuntimeHealthSessionMaterial? sessionMaterial)
    {
        return new RuntimeHealthProjectionSource(
            ProductVersion,
            supervisor,
            () => endpoint,
            () => sessionMaterial);
    }

    [Fact]
    public void Constructor_rejects_blank_product_version()
    {
        Assert.Throws<ArgumentException>(
            () => new RuntimeHealthProjectionSource(
                "   ",
                DesktopSupervisorProjection.Disconnected,
                static () => null,
                static () => null));
    }

    [Fact]
    public void Constructor_rejects_null_supervisor_projection()
    {
        Assert.Throws<ArgumentNullException>(
            () => new RuntimeHealthProjectionSource(
                ProductVersion,
                null!,
                static () => null,
                static () => null));
    }

    [Fact]
    public void Constructor_rejects_null_endpoint_provider()
    {
        Assert.Throws<ArgumentNullException>(
            () => new RuntimeHealthProjectionSource(
                ProductVersion,
                DesktopSupervisorProjection.Disconnected,
                null!,
                static () => null));
    }

    [Fact]
    public void Constructor_rejects_null_session_provider()
    {
        Assert.Throws<ArgumentNullException>(
            () => new RuntimeHealthProjectionSource(
                ProductVersion,
                DesktopSupervisorProjection.Disconnected,
                static () => null,
                null!));
    }

    [Fact]
    public async Task RefreshAsync_reports_endpoint_invalid_when_endpoint_missing()
    {
        RuntimeHealthProjectionSource source = CreateSource(
            DesktopSupervisorProjection.Disconnected,
            endpoint: null,
            sessionMaterial: RuntimeHealthSessionMaterial.Create());

        DesktopRuntimeHealthSnapshot snapshot = await source.RefreshAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(
            DesktopRuntimeConnectionState.Unavailable,
            snapshot.ConnectionState);
        Assert.Equal(
            RuntimeHealthTransportErrorCodes.EndpointInvalid,
            snapshot.StableErrorCode);
        Assert.Equal(ProductVersion, snapshot.RuntimeProductVersion);
        Assert.Empty(snapshot.Services);
    }

    [Fact]
    public async Task RefreshAsync_reports_endpoint_invalid_when_session_missing()
    {
        RuntimeHealthEndpoint endpoint = new(
            "opure-stable-0123456789abcdef0123456789abcdef",
            "0123456789abcdef0123456789abcdef");
        RuntimeHealthProjectionSource source = CreateSource(
            DesktopSupervisorProjection.Disconnected,
            endpoint,
            sessionMaterial: null);

        DesktopRuntimeHealthSnapshot snapshot = await source.RefreshAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(
            DesktopRuntimeConnectionState.Unavailable,
            snapshot.ConnectionState);
        Assert.Equal(
            RuntimeHealthTransportErrorCodes.EndpointInvalid,
            snapshot.StableErrorCode);
    }

    [Fact]
    public async Task RefreshAsync_projects_safe_mode_supervisor_state()
    {
        DesktopSupervisorProjection supervisor = new(
            DesktopSupervisorMode.SafeMode,
            "Unavailable",
            RuntimeRestartCount: 3);
        RuntimeHealthProjectionSource source = CreateSource(
            supervisor,
            endpoint: null,
            sessionMaterial: null);

        DesktopRuntimeHealthSnapshot snapshot = await source.RefreshAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(
            DesktopRuntimeDisplayState.SafeMode,
            snapshot.DisplayState);
        Assert.False(snapshot.Retryable);
    }

    [Fact]
    public async Task RefreshAsync_projects_recovering_supervisor_state()
    {
        DesktopSupervisorProjection supervisor = new(
            DesktopSupervisorMode.Recovering,
            "Unavailable",
            RuntimeRestartCount: 1);
        RuntimeHealthProjectionSource source = CreateSource(
            supervisor,
            endpoint: null,
            sessionMaterial: null);

        DesktopRuntimeHealthSnapshot snapshot = await source.RefreshAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(
            DesktopRuntimeDisplayState.Starting,
            snapshot.DisplayState);
        Assert.True(snapshot.Retryable);
    }
}
