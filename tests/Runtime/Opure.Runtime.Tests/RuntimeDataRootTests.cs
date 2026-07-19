using Xunit;

namespace Opure.Runtime.Tests;

public sealed class RuntimeDataRootTests
{
    [Fact]
    public void Default_data_root_is_scoped_to_opure_development()
    {
        RuntimeDataRoot dataRoot = RuntimeDataRootResolver.Resolve(
            explicitDataRoot: null,
            allowTestOverride: false);

        string expectedSuffix = Path.Combine("Opure", "Development");
        Assert.EndsWith(
            expectedSuffix,
            dataRoot.FullPath,
            OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal);

        Assert.Equal("DevelopmentDefault", dataRoot.Scope);
    }

    [Fact]
    public void Test_data_root_requires_an_absolute_path()
    {
        Assert.Throws<ArgumentException>(
            () => RuntimeDataRootResolver.Resolve(
                "relative-path",
                allowTestOverride: true));
    }

    [Fact]
    public void Test_data_root_is_resolved_without_creating_it()
    {
        string candidate = Path.Combine(
            Path.GetTempPath(),
            $"Opure-Runtime-DataRoot-{Guid.NewGuid():N}");

        RuntimeDataRoot dataRoot = RuntimeDataRootResolver.Resolve(
            candidate,
            allowTestOverride: true);

        Assert.Equal(Path.GetFullPath(candidate), dataRoot.FullPath);
        Assert.Equal("TestOverride", dataRoot.Scope);
        Assert.False(Directory.Exists(candidate));
    }
}
