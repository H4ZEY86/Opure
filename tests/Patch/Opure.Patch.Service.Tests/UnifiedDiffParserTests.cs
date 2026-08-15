using System;
using System.Text;
using Opure.Patch.Contracts;
using Xunit;

namespace Opure.Patch.Service.Tests;

public class UnifiedDiffParserTests
{
    [Fact]
    public void Parse_ValidUnifiedDiff_ParsesSuccessfully()
    {
        string diff = 
            "--- a/file.txt\n" +
            "+++ b/file.txt\n" +
            "@@ -1,3 +1,3 @@\n" +
            " Context Line 1\n" +
            "-Deleted Line\n" +
            "+Added Line\n" +
            " Context Line 2\n";
            
        byte[] payload = Encoding.UTF8.GetBytes(diff);
        
        var proposal = UnifiedDiffParser.Parse(payload);
        
        Assert.Equal("file.txt", proposal.OriginalFileHeader);
        Assert.Equal("file.txt", proposal.TargetFileHeader);
        Assert.Single(proposal.Hunks);
        
        var hunk = proposal.Hunks[0];
        Assert.Equal(1, hunk.OriginalStartLine);
        Assert.Equal(3, hunk.OriginalLineCount);
        Assert.Equal(1, hunk.TargetStartLine);
        Assert.Equal(3, hunk.TargetLineCount);
        
        Assert.Equal(4, hunk.Lines.Count);
        
        Assert.Equal(UnifiedHunkLineType.Context, hunk.Lines[0].Type);
        Assert.Equal(UnifiedHunkLineType.Deletion, hunk.Lines[1].Type);
        Assert.Equal(UnifiedHunkLineType.Addition, hunk.Lines[2].Type);
        Assert.Equal(UnifiedHunkLineType.Context, hunk.Lines[3].Type);
    }

    [Fact]
    public void Parse_MissingHeaders_ThrowsPreconditionFailedException()
    {
        string diff = "@@ -1,3 +1,3 @@\n Context Line 1\n";
        byte[] payload = Encoding.UTF8.GetBytes(diff);
        
        Assert.Throws<PreconditionFailedException>(() => UnifiedDiffParser.Parse(payload));
    }

    [Fact]
    public void Parse_ExceedsMaxHunkSize_ThrowsPreconditionFailedException()
    {
        var sb = new StringBuilder();
        sb.Append("--- a/file.txt\n+++ b/file.txt\n@@ -1,10001 +1,10001 @@\n");
        for (int i = 0; i < 10001; i++)
        {
            sb.Append(" Context\n");
        }
        
        byte[] payload = Encoding.UTF8.GetBytes(sb.ToString());
        
        var exception = Assert.Throws<PreconditionFailedException>(() => UnifiedDiffParser.Parse(payload));
        Assert.Contains("exceeds maximum allowed size", exception.Message);
    }
}
