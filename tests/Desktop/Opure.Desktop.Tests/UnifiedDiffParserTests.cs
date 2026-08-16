using System;
using System.Collections.Generic;
using System.Text;
using Opure.Desktop.Contracts;
using Opure.Patch.Contracts;
using Xunit;

namespace Opure.Desktop.Tests;

public sealed class UnifiedDiffParserTests
{
    [Fact]
    public void Parse_HandlesMultiHunkDiffs()
    {
        var proposals = new List<UnifiedPatchProposal>
        {
            new()
            {
                OriginalFileHeader = "file.txt",
                TargetFileHeader = "file.txt",
                Hunks = new List<UnifiedHunk>
                {
                    new()
                    {
                        OriginalStartLine = 1,
                        OriginalLineCount = 3,
                        TargetStartLine = 1,
                        TargetLineCount = 4,
                        Lines = new List<UnifiedHunkLine>
                        {
                            new() { Type = UnifiedHunkLineType.Context, Content = Encoding.UTF8.GetBytes("line1") },
                            new() { Type = UnifiedHunkLineType.Deletion, Content = Encoding.UTF8.GetBytes("line2") },
                            new() { Type = UnifiedHunkLineType.Addition, Content = Encoding.UTF8.GetBytes("line2_added") },
                            new() { Type = UnifiedHunkLineType.Addition, Content = Encoding.UTF8.GetBytes("line2_added2") },
                            new() { Type = UnifiedHunkLineType.Context, Content = Encoding.UTF8.GetBytes("line3") }
                        }
                    },
                    new()
                    {
                        OriginalStartLine = 10,
                        OriginalLineCount = 1,
                        TargetStartLine = 11,
                        TargetLineCount = 0,
                        Lines = new List<UnifiedHunkLine>
                        {
                            new() { Type = UnifiedHunkLineType.Deletion, Content = Encoding.UTF8.GetBytes("line10") }
                        }
                    }
                }
            }
        };

        var result = UnifiedDiffParser.Parse(proposals);

        // 2 headers + 2 hunk headers + 5 lines in hunk 1 + 1 line in hunk 2 = 10 lines
        Assert.Equal(10, result.Count);
        
        Assert.Equal("--- file.txt", result[0].Content);
        Assert.Equal("+++ file.txt", result[1].Content);
        Assert.Equal("@@ -1,3 +1,4 @@", result[2].Content);
        Assert.Equal(" line1", result[3].Content);
        Assert.Equal(DiffKind.Context, result[3].Kind);
        Assert.Equal(1, result[3].LineNumberOld);
        Assert.Equal(1, result[3].LineNumberNew);

        Assert.Equal("-line2", result[4].Content);
        Assert.Equal(DiffKind.Deleted, result[4].Kind);
        Assert.Equal(2, result[4].LineNumberOld);
        Assert.Null(result[4].LineNumberNew);

        Assert.Equal("+line2_added", result[5].Content);
        Assert.Equal(DiffKind.Added, result[5].Kind);
        Assert.Null(result[5].LineNumberOld);
        Assert.Equal(2, result[5].LineNumberNew);

        Assert.Equal("+line2_added2", result[6].Content);
        Assert.Equal(DiffKind.Added, result[6].Kind);
        Assert.Null(result[6].LineNumberOld);
        Assert.Equal(3, result[6].LineNumberNew);

        Assert.Equal(" line3", result[7].Content);
        Assert.Equal(DiffKind.Context, result[7].Kind);
        Assert.Equal(3, result[7].LineNumberOld);
        Assert.Equal(4, result[7].LineNumberNew);

        Assert.Equal("@@ -10,1 +11,0 @@", result[8].Content);
        
        Assert.Equal("-line10", result[9].Content);
        Assert.Equal(DiffKind.Deleted, result[9].Kind);
        Assert.Equal(10, result[9].LineNumberOld);
        Assert.Null(result[9].LineNumberNew);
    }
}
