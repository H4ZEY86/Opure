using Opure.Filesystem.Contracts;
using Xunit;

namespace Opure.Filesystem.Windows.Tests;

public sealed class LogicalWorkspacePathTests
{
    [Theory]
    [InlineData("src/main.cs")]
    [InlineData("SRC/Main.cs")]
    [InlineData("a.b/c-d_1")]
    public void ParseAcceptsPortableRelativePaths(string value)
    {
        LogicalWorkspacePath path = LogicalWorkspacePath.Parse(
            new UntrustedPathText(value));

        Assert.Equal(value, path.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("../escape")]
    [InlineData("a/../escape")]
    [InlineData("/absolute")]
    [InlineData(@"C:\absolute")]
    [InlineData(@"a\b")]
    [InlineData("file:stream")]
    [InlineData("file.")]
    [InlineData("file ")]
    [InlineData("CON")]
    [InlineData("nul.txt")]
    [InlineData("COM¹")]
    [InlineData("a//b")]
    public void ParseRejectsUnsafeForms(string value)
    {
        Assert.ThrowsAny<ArgumentException>(
            () => LogicalWorkspacePath.Parse(new UntrustedPathText(value)));
    }

    [Fact]
    public void RootRequiresExplicitPermission()
    {
        LogicalWorkspacePath root = LogicalWorkspacePath.Parse(
            new UntrustedPathText(string.Empty),
            allowWorkspaceRoot: true);

        Assert.True(root.IsWorkspaceRoot);
    }

    [Fact]
    public void FileIdentityRequiresCanonicalHex()
    {
        Assert.Throws<ArgumentException>(
            () => new FileObjectIdentity(
                1,
                "ABCDEF0123456789ABCDEF0123456789",
                FileIdentityCapability.WindowsFileId128));
    }
}
