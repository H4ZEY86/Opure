using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Opure.Patch.Contracts;
using Xunit;

namespace Opure.Patch.Service.Tests;

public class UnifiedPatchValidatorTests : IDisposable
{
    private readonly string _testWorkspaceRoot;

    public UnifiedPatchValidatorTests()
    {
        _testWorkspaceRoot = Path.Combine(Path.GetTempPath(), "OpureTestWorkspace_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testWorkspaceRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testWorkspaceRoot))
        {
            Directory.Delete(_testWorkspaceRoot, true);
        }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ValidateAsync_ValidMatch_Succeeds()
    {
        string filePath = Path.Combine(_testWorkspaceRoot, "file.txt");
        await File.WriteAllTextAsync(filePath, "Line 1\nLine 2\nLine 3\n", TestContext.Current.CancellationToken);

        string diff = 
            "--- a/file.txt\n" +
            "+++ b/file.txt\n" +
            "@@ -1,3 +1,3 @@\n" +
            " Line 1\n" +
            "-Line 2\n" +
            "+Line 2 Modified\n" +
            " Line 3\n";
            
        byte[] payload = Encoding.UTF8.GetBytes(diff);
        var proposal = UnifiedDiffParser.Parse(payload);

        var validator = new UnifiedPatchValidator(_testWorkspaceRoot);
        
        await validator.ValidateAsync(proposal, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ValidateAsync_ContextMismatch_ThrowsPreconditionFailedException()
    {
        string filePath = Path.Combine(_testWorkspaceRoot, "file.txt");
        // Target file has different context!
        await File.WriteAllTextAsync(filePath, "Line 1\nWrong Context\nLine 3\n", TestContext.Current.CancellationToken);

        string diff = 
            "--- a/file.txt\n" +
            "+++ b/file.txt\n" +
            "@@ -1,3 +1,3 @@\n" +
            " Line 1\n" +
            "-Line 2\n" +
            "+Line 2 Modified\n" +
            " Line 3\n";
            
        byte[] payload = Encoding.UTF8.GetBytes(diff);
        var proposal = UnifiedDiffParser.Parse(payload);

        var validator = new UnifiedPatchValidator(_testWorkspaceRoot);
        
        var ex = await Assert.ThrowsAsync<PreconditionFailedException>(
            () => validator.ValidateAsync(proposal, TestContext.Current.CancellationToken));
            
        Assert.Contains("Context mismatch", ex.Message);
    }

    [Fact]
    public async Task ValidateAsync_LineEndingMismatch_ThrowsPreconditionFailedException()
    {
        string filePath = Path.Combine(_testWorkspaceRoot, "file.txt");
        // Target file has CRLF!
        await File.WriteAllTextAsync(filePath, "Line 1\r\nLine 2\r\nLine 3\r\n", TestContext.Current.CancellationToken);

        // Patch has LF!
        string diff = 
            "--- a/file.txt\n" +
            "+++ b/file.txt\n" +
            "@@ -1,3 +1,3 @@\n" +
            " Line 1\n" +
            "-Line 2\n" +
            "+Line 2 Modified\n" +
            " Line 3\n";
            
        byte[] payload = Encoding.UTF8.GetBytes(diff);
        var proposal = UnifiedDiffParser.Parse(payload);

        var validator = new UnifiedPatchValidator(_testWorkspaceRoot);
        
        var ex = await Assert.ThrowsAsync<PreconditionFailedException>(
            () => validator.ValidateAsync(proposal, TestContext.Current.CancellationToken));
            
        // Because LF in patch doesn't match CRLF in target
        Assert.Contains("cryptographically", ex.Message);
    }

    [Fact]
    public async Task ValidateAsync_PathTraversal_ThrowsArgumentException()
    {
        string diff = 
            "--- a/../file.txt\n" +
            "+++ b/../file.txt\n" +
            "@@ -1,1 +1,1 @@\n" +
            " Context\n";
            
        byte[] payload = Encoding.UTF8.GetBytes(diff);
        var proposal = UnifiedDiffParser.Parse(payload);

        var validator = new UnifiedPatchValidator(_testWorkspaceRoot);
        
        await Assert.ThrowsAsync<ArgumentException>(
            () => validator.ValidateAsync(proposal, TestContext.Current.CancellationToken));
    }
}
