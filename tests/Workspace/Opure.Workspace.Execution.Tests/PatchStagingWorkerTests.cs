using System;
using System.IO;
using System.Threading.Tasks;
using Opure.Patch.Contracts;
using Xunit;
using System.Runtime.Versioning;

namespace Opure.Workspace.Execution.Tests;

[SupportedOSPlatform("windows")]
public sealed class PatchStagingWorkerTests : IDisposable
{
    private readonly string _testWorkspaceRoot;

    public PatchStagingWorkerTests()
    {
        _testWorkspaceRoot = Path.Combine(Path.GetTempPath(), "Opure_PatchStagingWorkerTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testWorkspaceRoot);
    }

    public void Dispose()
    {
        try { Directory.Delete(_testWorkspaceRoot, true); } catch { }
    }

    [Fact]
    public async Task StagePatchAsync_AppliesUnifiedHunks_ProducesExactBytes()
    {
        var worker = new PatchStagingWorker(_testWorkspaceRoot);

        string originalFile = Path.Combine(_testWorkspaceRoot, "file.txt");
        await File.WriteAllBytesAsync(originalFile, "Line 1\nLine 2\nLine 3\n"u8.ToArray(), TestContext.Current.CancellationToken);

        var proposal = new UnifiedPatchProposal
        {
            OriginalFileHeader = "file.txt",
            TargetFileHeader = "file.txt",
            Hunks = new[]
            {
                new UnifiedHunk
                {
                    OriginalStartLine = 2,
                    OriginalLineCount = 2,
                    TargetStartLine = 2,
                    TargetLineCount = 2,
                    Lines = new[]
                    {
                        new UnifiedHunkLine
                        {
                            Type = UnifiedHunkLineType.Deletion,
                            Content = new ReadOnlyMemory<byte>("Line 2\n"u8.ToArray())
                        },
                        new UnifiedHunkLine
                        {
                            Type = UnifiedHunkLineType.Addition,
                            Content = new ReadOnlyMemory<byte>("Line 2 Modified\n"u8.ToArray())
                        },
                        new UnifiedHunkLine
                        {
                            Type = UnifiedHunkLineType.Context,
                            Content = new ReadOnlyMemory<byte>("Line 3\n"u8.ToArray())
                        }
                    }
                }
            }
        };

        string stagingDir = Path.Combine(_testWorkspaceRoot, ".opure-staging");
        Directory.CreateDirectory(stagingDir);

        var result = await worker.StagePatchAsync(proposal, stagingDir, TestContext.Current.CancellationToken);

        Assert.True(File.Exists(result.StagingPath));
        byte[] stagedBytes = await File.ReadAllBytesAsync(result.StagingPath, TestContext.Current.CancellationToken);
        
        string stagedText = System.Text.Encoding.UTF8.GetString(stagedBytes);
        Assert.Equal("Line 1\nLine 2 Modified\nLine 3\n", stagedText);
        Assert.Equal(21, result.OriginalSize); // "Line 1\nLine 2\nLine 3\n" is 7+7+7=21 bytes
    }
}
