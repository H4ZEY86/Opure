using Xunit;

namespace Opure.Bootstrap.Windows.Tests;

public sealed class BootstrapChannelTests
{
    [Fact]
    public void Stable_preview_and_development_roots_do_not_collide()
    {
        string localApplicationData = Path.GetFullPath(
            Path.Combine(
                Path.GetTempPath(),
                $"Opure-Bootstrap-Channels-{Guid.NewGuid():N}"));

        string stable = BootstrapDataRootResolver.Resolve(
            BootstrapChannel.Stable,
            localApplicationData);
        string preview = BootstrapDataRootResolver.Resolve(
            BootstrapChannel.Preview,
            localApplicationData);
        string development = BootstrapDataRootResolver.Resolve(
            BootstrapChannel.Development,
            localApplicationData);

        HashSet<string> roots = new(
            OperatingSystem.IsWindows()
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal)
        {
            stable,
            preview,
            development
        };

        Assert.Equal(3, roots.Count);
        Assert.False(Directory.Exists(stable));
        Assert.False(Directory.Exists(preview));
        Assert.False(Directory.Exists(development));
    }

    [Fact]
    public void Session_material_is_random_and_bounded()
    {
        BootstrapSession first = BootstrapSession.Create();
        BootstrapSession second = BootstrapSession.Create();

        Assert.NotEqual(first.SessionId, second.SessionId);
        Assert.NotEqual(first.SessionSecret, second.SessionSecret);
        Assert.Equal(32, first.SessionId.Length);
        Assert.Equal(43, first.SessionSecret.Length);
        Assert.DoesNotContain("=", first.SessionSecret, StringComparison.Ordinal);
    }
}
