using Xunit;

namespace Opure.Bootstrap.Windows.Tests;

public sealed class BootstrapPathTests
{
    private static readonly byte[] FakeExecutableContent =
    [
        0x4F,
        0x50,
        0x55,
        0x52,
        0x45
    ];

    [Fact]
    public void Development_paths_are_absolute_and_exact()
    {
        string root = Path.GetFullPath(
            Path.Combine(
                Path.GetTempPath(),
                $"Opure-Bootstrap-Paths-{Guid.NewGuid():N}"));

        BootstrapExecutablePaths paths = BootstrapPathResolver.Resolve(
            root,
            BootstrapLayout.Development,
            "Release");

        Assert.True(Path.IsPathFullyQualified(paths.RuntimeExecutable));
        Assert.True(Path.IsPathFullyQualified(paths.DesktopExecutable));
        Assert.EndsWith(
            Path.Combine(
                "artifacts",
                "bin",
                "Opure.Runtime",
                "release",
                "Opure.Runtime.exe"),
            paths.RuntimeExecutable,
            StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith(
            Path.Combine(
                "artifacts",
                "bin",
                "Opure.Desktop",
                "release",
                "Opure.Desktop.exe"),
            paths.DesktopExecutable,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Wrong_companion_assembly_identity_is_rejected()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            $"Opure-Bootstrap-Binary-{Guid.NewGuid():N}");

        Directory.CreateDirectory(root);

        try
        {
            string executable = Path.Combine(root, "Opure.Runtime.exe");
            string companion = Path.Combine(root, "Opure.Runtime.dll");

            File.WriteAllBytes(executable, FakeExecutableContent);
            File.Copy(
                typeof(BootstrapPathTests).Assembly.Location,
                companion);

            InvalidDataException exception = Assert.Throws<InvalidDataException>(
                () => BootstrapBinaryIdentityVerifier.Verify(
                    executable,
                    "Opure.Runtime.exe",
                    "Opure.Runtime"));

            Assert.Equal(
                "Bootstrap child assembly identity is unexpected.",
                exception.Message);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
