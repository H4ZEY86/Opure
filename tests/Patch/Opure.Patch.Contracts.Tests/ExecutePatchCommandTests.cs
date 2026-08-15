using System;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Opure.Patch.Contracts.Tests;

public class ExecutePatchCommandTests
{
    [Fact]
    public void Serialize_ExecutePatchCommand_RoundTripsCorrectly()
    {
        var command = new ExecutePatchCommand
        {
            PatchId = "patch-123",
            ApproverIdentity = "Developer",
            WorkspaceRootPath = "C:\\Opure",
            Proposals = new[]
            {
                new UnifiedPatchProposal
                {
                    OriginalFileHeader = "a/file.txt",
                    TargetFileHeader = "b/file.txt",
                    Hunks = new[]
                    {
                        new UnifiedHunk
                        {
                            OriginalStartLine = 1,
                            OriginalLineCount = 1,
                            TargetStartLine = 1,
                            TargetLineCount = 2,
                            Lines = new[]
                            {
                                new UnifiedHunkLine
                                {
                                    Type = UnifiedHunkLineType.Context,
                                    Content = new ReadOnlyMemory<byte>("line1\n"u8.ToArray())
                                },
                                new UnifiedHunkLine
                                {
                                    Type = UnifiedHunkLineType.Addition,
                                    Content = new ReadOnlyMemory<byte>("line2\n"u8.ToArray())
                                }
                            }
                        }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(command, PatchExecutionJsonContext.Default.ExecutePatchCommand);
        var deserialized = JsonSerializer.Deserialize(json, PatchExecutionJsonContext.Default.ExecutePatchCommand);

        Assert.NotNull(deserialized);
        Assert.Equal("patch-123", deserialized.PatchId);
        Assert.Single(deserialized.Proposals);
        var hunk = deserialized.Proposals[0].Hunks[0];
        Assert.Equal(2, hunk.Lines.Count);
        Assert.Equal("line2\n"u8.ToArray(), hunk.Lines[1].Content.ToArray());
    }
}
