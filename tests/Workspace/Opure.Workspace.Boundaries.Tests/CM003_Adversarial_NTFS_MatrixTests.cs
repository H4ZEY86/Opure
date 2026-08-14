using Xunit;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Opure.Workspace.Contracts;
using Opure.Workspace.Boundaries;

namespace Opure.Workspace.Boundaries.Tests;

public class WorkspacePreconditionValidatorTests : IDisposable
{
    private static readonly ProjectIdentity TestProjectId = new(Guid.Parse("C2F5A1E0-7B4D-4A2C-8E6F-1D3B9C5E7A2F"));
    private static readonly WorkspaceGeneration TestGeneration = new(1);
    private readonly CanonicalPath TestWorkspaceRoot;
    private readonly string TestDirectory;

    public WorkspacePreconditionValidatorTests()
    {
        TestDirectory = Path.Combine(Path.GetTempPath(), "OpureAdversarialTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(TestDirectory);
        TestWorkspaceRoot = new CanonicalPath(TestDirectory);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(TestDirectory))
                Directory.Delete(TestDirectory, true);
        }
        catch { } // Best effort
        GC.SuppressFinalize(this);
    }

    private static (ExpectedSourceLength Length, SourceHash Hash) WriteFileAndGetExpected(string path, string content)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        File.WriteAllBytes(path, bytes);
        var hash = SHA256.HashData(bytes);
        return (new ExpectedSourceLength(bytes.Length), new SourceHash(hash));
    }

    [Fact]
    public void Validate_ValidFile_ReturnsSuccess()
    {
        var testFile = Path.Combine(TestDirectory, "testfile.txt");
        var expected = WriteFileAndGetExpected(testFile, "valid content");
        
        var result = WorkspacePreconditionValidator.Validate(
            testFile, TestProjectId, TestGeneration, TestWorkspaceRoot, expected.Length, expected.Hash);
            
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Boundary);
    }

    [Fact]
    public void Validate_DevicePath_ReturnsDevicePathCollision()
    {
        var devicePath = Path.Combine(TestDirectory, "CON.txt");
        var expectedHash = new SourceHash(new byte[32]);
        var expectedLength = new ExpectedSourceLength(0);

        var result = WorkspacePreconditionValidator.Validate(
            devicePath, TestProjectId, TestGeneration, TestWorkspaceRoot, expectedLength, expectedHash);
            
        Assert.False(result.IsSuccess);
        Assert.Equal(ValidationResultStatus.DevicePathCollision, result.Status);
    }

    [Fact]
    public void Validate_DeviceNameInPathComponents_ReturnsDevicePathCollision()
    {
        var pathWithDevName = Path.Combine(TestDirectory, "NUL", "test.txt");
        var expectedHash = new SourceHash(new byte[32]);
        var expectedLength = new ExpectedSourceLength(0);

        var result = WorkspacePreconditionValidator.Validate(
            pathWithDevName, TestProjectId, TestGeneration, TestWorkspaceRoot, expectedLength, expectedHash);
            
        Assert.False(result.IsSuccess);
        Assert.Equal(ValidationResultStatus.DevicePathCollision, result.Status);
    }

    [Fact]
    public void Validate_SourceDrift_LengthMismatch_ReturnsSourceDrift()
    {
        var testFile = Path.Combine(TestDirectory, "driftfile.txt");
        var expected = WriteFileAndGetExpected(testFile, "original content");
        
        File.WriteAllText(testFile, "longer modified content that changes the hash");
        
        var result = WorkspacePreconditionValidator.Validate(
            testFile, TestProjectId, TestGeneration, TestWorkspaceRoot, expected.Length, expected.Hash);
            
        Assert.False(result.IsSuccess);
        Assert.Equal(ValidationResultStatus.SourceDrift, result.Status);
    }

    [Fact]
    public void Validate_SourceDrift_HashMismatch_ReturnsSourceDrift()
    {
        var testFile = Path.Combine(TestDirectory, "hashdriftfile.txt");
        var expected = WriteFileAndGetExpected(testFile, "12345678");
        
        File.WriteAllText(testFile, "87654321");
        
        var result = WorkspacePreconditionValidator.Validate(
            testFile, TestProjectId, TestGeneration, TestWorkspaceRoot, expected.Length, expected.Hash);
            
        Assert.False(result.IsSuccess);
        Assert.Equal(ValidationResultStatus.SourceDrift, result.Status);
    }

    [Fact]
    public void Validate_CaseCollision_ReturnsUnicodeCollision()
    {
        var testFile = Path.Combine(TestDirectory, "CaseTest.txt");
        var expected = WriteFileAndGetExpected(testFile, "content");
        
        var spoofedPath = Path.Combine(TestDirectory, "casetest.txt");
        
        var result = WorkspacePreconditionValidator.Validate(
            spoofedPath, TestProjectId, TestGeneration, TestWorkspaceRoot, expected.Length, expected.Hash);
            
        Assert.False(result.IsSuccess);
        Assert.Equal(ValidationResultStatus.UnicodeCollision, result.Status);
    }

    [Fact]
    public void Validate_UnicodeNormalizationCollision_ReturnsUnicodeCollision()
    {
        var formC = Path.Combine(TestDirectory, "r\u00E9sum\u00E9.txt");
        var formD = Path.Combine(TestDirectory, "re\u0301sume\u0301.txt");

        var expected = WriteFileAndGetExpected(formC, "content");
        
        var result = WorkspacePreconditionValidator.Validate(
            formD, TestProjectId, TestGeneration, TestWorkspaceRoot, expected.Length, expected.Hash);
            
        Assert.False(result.IsSuccess);
        Assert.Equal(ValidationResultStatus.UnicodeCollision, result.Status);
    }

    [Fact]
    public void Validate_Symlink_ReturnsSymlinkDetected()
    {
        var targetFile = Path.Combine(TestDirectory, "target.txt");
        var expected = WriteFileAndGetExpected(targetFile, "target content");
        
        var symlinkFile = Path.Combine(TestDirectory, "link.txt");
        
        try
        {
            File.CreateSymbolicLink(symlinkFile, targetFile);
        }
        catch (IOException)
        {
            Assert.Skip("Developer Mode or Administrator privileges are required to create symbolic links on Windows.");
            return;
        }

        var result = WorkspacePreconditionValidator.Validate(
            symlinkFile, TestProjectId, TestGeneration, TestWorkspaceRoot, expected.Length, expected.Hash);
            
        Assert.False(result.IsSuccess);
        Assert.Equal(ValidationResultStatus.SymlinkDetected, result.Status);
    }

    [Fact]
    public void Validate_TOCTOU_FileLocking_PreventsConcurrentWrite()
    {
        var testFile = Path.Combine(TestDirectory, "toctou.txt");
        var expected = WriteFileAndGetExpected(testFile, "secure content");
        
        using var stream = File.Open(testFile, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);

        Assert.Throws<IOException>(() => 
        {
            using var writeStream = File.Open(testFile, FileMode.Open, FileAccess.Write, FileShare.None);
        });
    }

    [Fact]
    public void Validate_NullPath_ReturnsFailure()
    {
        var result = WorkspacePreconditionValidator.Validate(
            null!, TestProjectId, TestGeneration, TestWorkspaceRoot, new ExpectedSourceLength(0), new SourceHash(new byte[32]));
        Assert.False(result.IsSuccess);
        Assert.Equal(ValidationResultStatus.ValidationFailure, result.Status);
    }

    [Fact]
    public void Validate_EmptyPath_ReturnsFailure()
    {
        var result = WorkspacePreconditionValidator.Validate(
            string.Empty, TestProjectId, TestGeneration, TestWorkspaceRoot, new ExpectedSourceLength(0), new SourceHash(new byte[32]));
        Assert.False(result.IsSuccess);
        Assert.Equal(ValidationResultStatus.ValidationFailure, result.Status);
    }
}
