using Opure.Desktop.Contracts;
using Opure.Desktop.GatewayClient;
using Opure.Ipc.Abstractions;
using Xunit;

namespace Opure.Desktop.GatewayClient.Tests;

public sealed class RuntimeHealthGatewayClientTests
{
    private const string ProductVersion = "4.5.6";

    [Fact]
    public void CreateProjectionSource_from_environment_returns_source()
    {
        IDesktopRuntimeHealthSource source =
            RuntimeHealthGatewayClient.CreateProjectionSource(
                ProductVersion,
                DesktopSupervisorProjection.Disconnected);

        Assert.IsType<RuntimeHealthProjectionSource>(source);
    }

    [Fact]
    public void CreateProjectionSource_with_material_returns_source()
    {
        IDesktopRuntimeHealthSource source =
            RuntimeHealthGatewayClient.CreateProjectionSource(
                ProductVersion,
                DesktopSupervisorProjection.Disconnected,
                endpoint: null,
                sessionMaterial: null);

        Assert.IsType<RuntimeHealthProjectionSource>(source);
    }

    [Fact]
    public async Task CreateStateSourceAsync_returns_disconnected_source_without_material()
    {
        IDesktopShellStateSource source =
            await RuntimeHealthGatewayClient.CreateStateSourceAsync(
                ProductVersion,
                DesktopSupervisorProjection.Disconnected,
                endpoint: null,
                sessionMaterial: null,
                TestContext.Current.CancellationToken);

        DisconnectedDesktopShellStateSource disconnected =
            Assert.IsType<DisconnectedDesktopShellStateSource>(source);
        DesktopShellSnapshot snapshot = disconnected.GetCurrent();

        Assert.Equal(
            DesktopRuntimeConnectionState.Unavailable,
            snapshot.RuntimeConnectionState);
        Assert.Equal(ProductVersion, snapshot.ProductVersion);
    }

    [Fact]
    public async Task CreateStateSourceAsync_projects_safe_mode_supervisor()
    {
        DesktopSupervisorProjection supervisor = new(
            DesktopSupervisorMode.SafeMode,
            "Unavailable",
            RuntimeRestartCount: 5);

        IDesktopShellStateSource source =
            await RuntimeHealthGatewayClient.CreateStateSourceAsync(
                ProductVersion,
                supervisor,
                endpoint: null,
                sessionMaterial: null,
                TestContext.Current.CancellationToken);

        DesktopShellSnapshot snapshot = source.GetCurrent();

        Assert.Equal("Safe Mode", snapshot.RuntimeStatusTitle);
    }

    [Fact]
    public async Task CreateStateSourceAsync_rejects_blank_product_version()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await RuntimeHealthGatewayClient.CreateStateSourceAsync(
                "   ",
                DesktopSupervisorProjection.Disconnected,
                endpoint: null,
                sessionMaterial: null,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateStateSourceAsync_rejects_null_supervisor_projection()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await RuntimeHealthGatewayClient.CreateStateSourceAsync(
                ProductVersion,
                null!,
                endpoint: null,
                sessionMaterial: null,
                TestContext.Current.CancellationToken));
    }
}
