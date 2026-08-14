using System.Security.Cryptography;
using System.Text;
using Opure.Patch.Contracts;
using Opure.Workspace.Contracts;
using Xunit;

namespace Opure.Patch.Service.Tests;

public sealed class PatchPreviewEngineTests
{
    private static readonly UTF8Encoding Utf8 = new(false, true);

    private sealed class FakeWorkspaceSourceProvider : IWorkspaceSourceProvider
    {
        public WorkspaceSourceResult Result { get; set; } = null!;
        public int CallCount { get; private set; }

        public WorkspaceSourceResult GetSourceBytes(string projectId, long generation, string logicalPath)
        {
            CallCount++;
            return Result;
        }
    }

    [Fact]
    public void GeneratePreview_WithCreateProposal_ReturnsDeterministicPreview_AndDoesNotMutateDisk()
    {
        // Arrange
        var fakeProvider = new FakeWorkspaceSourceProvider();
        
        var proposalContent = Utf8.GetBytes("Hello World\n");
        var expectedSha256 = Convert.ToHexStringLower(SHA256.HashData(proposalContent));
        
        var proposal = new ExactUtf8PatchProposal(
            "patch-123",
            1,
            "proj-1",
            "root-1",
            42,
            new string('0', 64),
            "file1.txt",
            ExactUtf8PatchOperationKind.Create,
            null,
            null,
            PatchLineEndingIntent.Lf,
            PatchCreatorKind.Developer,
            "feat: new file",
            DateTimeOffset.UtcNow,
            proposalContent);

        fakeProvider.Result = new WorkspaceSourceResult("proj-1", 42, "file1.txt", "", null, false);
        var engine = new PatchPreviewEngine(fakeProvider);

        // Act
        var preview = engine.GeneratePreview(proposal);

        // Assert
        Assert.NotNull(preview);
        Assert.Equal("patch-123", preview.PatchId);
        Assert.Equal(ExactUtf8PatchOperationKind.Create, preview.OperationKind);
        Assert.Null(preview.BeforeHashSha256);
        Assert.Equal(expectedSha256, preview.AfterHashSha256);
        Assert.Equal(PatchLineEndingIntent.PreserveExisting, preview.SourceLineEnding);
        Assert.Equal(PatchLineEndingIntent.Lf, preview.ResultingLineEnding);
        Assert.False(preview.HasHiddenOrBidiControls);
        Assert.False(preview.IsTruncated);
        Assert.Equal(PatchEffectIntentClass.Feature, preview.EffectIntentClass);
        Assert.NotEmpty(preview.PreviewDigestSha256);
        Assert.Equal(64, preview.PreviewDigestSha256.Length);
        
        Assert.Equal(1, fakeProvider.CallCount);
    }

    [Fact]
    public void GeneratePreview_WithReplaceProposal_ValidatesSourceAndProjectsChanges()
    {
        // Arrange
        var fakeProvider = new FakeWorkspaceSourceProvider();
        
        var oldContent = Utf8.GetBytes("Old content\r\n");
        var oldHash = Convert.ToHexStringLower(SHA256.HashData(oldContent));
        
        var newContent = Utf8.GetBytes("New content\n");
        var newHash = Convert.ToHexStringLower(SHA256.HashData(newContent));
        
        var proposal = new ExactUtf8PatchProposal(
            "patch-456",
            1,
            "proj-1",
            "root-1",
            42,
            new string('0', 64),
            "file2.txt",
            ExactUtf8PatchOperationKind.Replace,
            oldHash,
            oldContent.Length,
            PatchLineEndingIntent.Lf,
            PatchCreatorKind.Developer,
            "fix: typo",
            DateTimeOffset.UtcNow,
            newContent);

        fakeProvider.Result = new WorkspaceSourceResult("proj-1", 42, "file2.txt", oldHash, oldContent, true);
        var engine = new PatchPreviewEngine(fakeProvider);

        // Act
        var preview = engine.GeneratePreview(proposal);

        // Assert
        Assert.Equal(ExactUtf8PatchOperationKind.Replace, preview.OperationKind);
        Assert.Equal(oldHash, preview.BeforeHashSha256);
        Assert.Equal(newHash, preview.AfterHashSha256);
        Assert.Equal(PatchLineEndingIntent.CrLf, preview.SourceLineEnding);
        Assert.Equal(PatchLineEndingIntent.Lf, preview.ResultingLineEnding);
        Assert.Equal(PatchEffectIntentClass.BugFix, preview.EffectIntentClass);
        
        Assert.Equal(1, fakeProvider.CallCount);
    }

    [Fact]
    public void GeneratePreview_DetectsHiddenBidiControls()
    {
        // Arrange
        var fakeProvider = new FakeWorkspaceSourceProvider();
        
        // Includes U+202B (RLE)
        var newContent = Utf8.GetBytes("Console.WriteLine(\"Hello \u202B World\");");
        
        var proposal = new ExactUtf8PatchProposal(
            "patch-bidi",
            1,
            "proj-1",
            "root-1",
            42,
            new string('0', 64),
            "file3.txt",
            ExactUtf8PatchOperationKind.Create,
            null,
            null,
            PatchLineEndingIntent.Lf,
            PatchCreatorKind.Developer,
            "add bidi",
            DateTimeOffset.UtcNow,
            newContent);

        fakeProvider.Result = new WorkspaceSourceResult("proj-1", 42, "file3.txt", "", null, false);
        var engine = new PatchPreviewEngine(fakeProvider);

        // Act
        var preview = engine.GeneratePreview(proposal);

        // Assert
        Assert.True(preview.HasHiddenOrBidiControls);
    }

    [Fact]
    public void GeneratePreview_SourceDrift_ThrowsException()
    {
        // Arrange
        var fakeProvider = new FakeWorkspaceSourceProvider();
        
        var expectedOldContent = Utf8.GetBytes("Old");
        var expectedOldHash = Convert.ToHexStringLower(SHA256.HashData(expectedOldContent));
        
        var actualOldContent = Utf8.GetBytes("Drifted");
        var actualOldHash = Convert.ToHexStringLower(SHA256.HashData(actualOldContent));
        
        var newContent = Utf8.GetBytes("New");
        
        var proposal = new ExactUtf8PatchProposal(
            "patch-drift",
            1,
            "proj-1",
            "root-1",
            42,
            new string('0', 64),
            "file.txt",
            ExactUtf8PatchOperationKind.Replace,
            expectedOldHash, // Proposal expects "Old"
            expectedOldContent.Length,
            PatchLineEndingIntent.Lf,
            PatchCreatorKind.Developer,
            "update",
            DateTimeOffset.UtcNow,
            newContent);

        // Disk actually has "Drifted"
        fakeProvider.Result = new WorkspaceSourceResult("proj-1", 42, "file.txt", actualOldHash, actualOldContent, true);
        var engine = new PatchPreviewEngine(fakeProvider);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => engine.GeneratePreview(proposal));
        Assert.Contains("Source drift detected", ex.Message);
    }
}
