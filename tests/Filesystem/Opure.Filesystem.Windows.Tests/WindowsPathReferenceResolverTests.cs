using System.Runtime.Versioning;
using System.Diagnostics;
using Opure.Filesystem.Contracts;
using Xunit;

namespace Opure.Filesystem.Windows.Tests;

[SupportedOSPlatform("windows")]
public sealed class WindowsPathReferenceResolverTests : IDisposable
{
    private readonly string rootPath = Path.Combine(
        Path.GetTempPath(),
        "Opure.Filesystem.Tests",
        Guid.NewGuid().ToString("N"));

    public WindowsPathReferenceResolverTests()
    {
        Directory.CreateDirectory(rootPath);
    }

    [Fact]
    public void ResolveExistingReturnsHandleDerivedIdentity()
    {
        string directory = Directory.CreateDirectory(
            Path.Combine(rootPath, "Src")).FullName;
        File.WriteAllText(Path.Combine(directory, "Main.cs"), "content");
        WindowsRegisteredWorkspaceRoot root = WindowsPathReferenceResolver.RegisterRoot(
            new UntrustedPathText(rootPath));

        using VerifiedWindowsPathReference reference = WindowsPathReferenceResolver.ResolveExisting(
            root,
            LogicalWorkspacePath.Parse(new UntrustedPathText("src/main.cs")));

        Assert.Equal(FilesystemObjectType.RegularFile, reference.Value.ObjectType);
        Assert.Equal(FilesystemReparseKind.None, reference.Value.ReparseKind);
        Assert.Equal(root.Volume.SerialNumber, reference.Value.Identity.VolumeSerialNumber);
        Assert.EndsWith(
            Path.Combine("Src", "Main.cs"),
            reference.Value.FinalPath,
            StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(@"C:relative")]
    [InlineData(@"\root-relative")]
    [InlineData(@"\\server\share")]
    [InlineData(@"\\?\C:\device")]
    [InlineData(@"\\.\pipe\name")]
    [InlineData(@"\??\C:\native")]
    [InlineData(@"C:\unsafe\..\root")]
    [InlineData(@"C:\unsafe.\root")]
    [InlineData(@"C:\unsafe \root")]
    public void RegisterRootRejectsNonOrdinaryNamespaces(string value)
    {
        WindowsPathReferenceException exception =
            Assert.Throws<WindowsPathReferenceException>(
                () => WindowsPathReferenceResolver.RegisterRoot(
                    new UntrustedPathText(value)));

        Assert.Equal(WindowsPathFailure.InvalidNamespace, exception.Failure);
    }

    [Fact]
    public void ReparsePointIsDeniedBeforeTraversal()
    {
        string target = Directory.CreateDirectory(
            Path.Combine(rootPath, "target")).FullName;
        string link = Path.Combine(rootPath, "link");
        Directory.CreateSymbolicLink(link, target);
        WindowsRegisteredWorkspaceRoot root = WindowsPathReferenceResolver.RegisterRoot(
            new UntrustedPathText(rootPath));

        WindowsPathReferenceException exception =
            Assert.Throws<WindowsPathReferenceException>(
                () => WindowsPathReferenceResolver.ResolveExisting(
                    root,
                    LogicalWorkspacePath.Parse(new UntrustedPathText("link"))));

        Assert.Equal(WindowsPathFailure.ReparsePointDenied, exception.Failure);
    }

    [Fact]
    public void JunctionIsDeniedBeforeTraversal()
    {
        string target = Directory.CreateDirectory(
            Path.Combine(rootPath, "junction-target")).FullName;
        string link = Path.Combine(rootPath, "junction");
        using Process process = Process.Start(new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.SystemDirectory, "cmd.exe"),
            UseShellExecute = false,
            CreateNoWindow = true,
            ArgumentList =
            {
                "/d",
                "/c",
                "mklink",
                "/J",
                link,
                target
            }
        }) ?? throw new InvalidOperationException("Could not create a junction fixture.");
        process.WaitForExit();
        Assert.Equal(0, process.ExitCode);
        try
        {
            WindowsRegisteredWorkspaceRoot root =
                WindowsPathReferenceResolver.RegisterRoot(
                    new UntrustedPathText(rootPath));

            WindowsPathReferenceException exception =
                Assert.Throws<WindowsPathReferenceException>(
                    () => WindowsPathReferenceResolver.ResolveExisting(
                        root,
                        LogicalWorkspacePath.Parse(
                            new UntrustedPathText("junction"))));

            Assert.Equal(
                WindowsPathFailure.ReparsePointDenied,
                exception.Failure);
        }
        finally
        {
            Directory.Delete(link);
        }
    }

    [Fact]
    public void ReplacementIsDetectedWhileOriginalHandleRemainsHeld()
    {
        string original = Path.Combine(rootPath, "value.txt");
        File.WriteAllText(original, "original");
        WindowsRegisteredWorkspaceRoot root = WindowsPathReferenceResolver.RegisterRoot(
            new UntrustedPathText(rootPath));
        using VerifiedWindowsPathReference reference = WindowsPathReferenceResolver.ResolveExisting(
            root,
            LogicalWorkspacePath.Parse(new UntrustedPathText("value.txt")));
        string moved = Path.Combine(rootPath, "moved.txt");
        File.Move(original, moved);
        File.WriteAllText(original, "replacement");

        WindowsPathReferenceException exception =
            Assert.Throws<WindowsPathReferenceException>(
                () => WindowsPathReferenceResolver.Revalidate(root, reference));

        Assert.Equal(WindowsPathFailure.IdentityChanged, exception.Failure);
    }

    [Fact]
    public void NamedDataStreamIsDetectedButNeverAddressableAsLogicalPath()
    {
        string path = Path.Combine(rootPath, "value.txt");
        File.WriteAllText(path, "primary");
        File.WriteAllText(string.Concat(path, ":private"), "secondary");
        WindowsRegisteredWorkspaceRoot root =
            WindowsPathReferenceResolver.RegisterRoot(
                new UntrustedPathText(rootPath));

        using VerifiedWindowsPathReference reference =
            WindowsPathReferenceResolver.ResolveExisting(
                root,
                LogicalWorkspacePath.Parse(
                    new UntrustedPathText("value.txt")));

        Assert.True(reference.Value.HasNamedStreams);
        Assert.Throws<ArgumentException>(
            () => LogicalWorkspacePath.Parse(
                new UntrustedPathText("value.txt:private")));
    }

    [Fact]
    public void ReplacedWorkspaceRootIsDetected()
    {
        WindowsRegisteredWorkspaceRoot root = WindowsPathReferenceResolver.RegisterRoot(
            new UntrustedPathText(rootPath));
        string moved = string.Concat(rootPath, "-moved");
        Directory.Move(rootPath, moved);
        Directory.CreateDirectory(rootPath);

        try
        {
            WindowsPathReferenceException exception =
                Assert.Throws<WindowsPathReferenceException>(
                    () => WindowsPathReferenceResolver.ResolveExisting(
                        root,
                        LogicalWorkspacePath.Parse(
                            new UntrustedPathText(string.Empty),
                            allowWorkspaceRoot: true)));
            Assert.Equal(WindowsPathFailure.IdentityChanged, exception.Failure);
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
            Directory.Move(moved, rootPath);
        }
    }

    [Fact]
    public void DifferentVolumeSerialsCannotRepresentTheSameObject()
    {
        FileObjectIdentity first = new(
            1,
            "00112233445566778899aabbccddeeff",
            FileIdentityCapability.WindowsFileId128);
        FileObjectIdentity second = new(
            2,
            "00112233445566778899aabbccddeeff",
            FileIdentityCapability.WindowsFileId128);

        Assert.False(first.IsSameObject(second));
    }

    public void Dispose()
    {
        if (Directory.Exists(rootPath))
        {
            Directory.Delete(rootPath, recursive: true);
        }

        GC.SuppressFinalize(this);
    }
}
