using System.Collections.Generic;
using System.Text;
using Opure.Desktop.Contracts;
using Opure.Patch.Contracts;

namespace Opure.Desktop;

public static class UnifiedDiffParser
{
    public static IReadOnlyList<DiffLineItem> Parse(IReadOnlyList<UnifiedPatchProposal> proposals)
    {
        var result = new List<DiffLineItem>();

        foreach (var proposal in proposals)
        {
            result.Add(new DiffLineItem(null, null, $"--- {proposal.OriginalFileHeader}", DiffKind.Context));
            result.Add(new DiffLineItem(null, null, $"+++ {proposal.TargetFileHeader}", DiffKind.Context));

            foreach (var hunk in proposal.Hunks)
            {
                result.Add(new DiffLineItem(null, null, $"@@ -{hunk.OriginalStartLine},{hunk.OriginalLineCount} +{hunk.TargetStartLine},{hunk.TargetLineCount} @@", DiffKind.Context));

                int oldLine = hunk.OriginalStartLine;
                int newLine = hunk.TargetStartLine;

                foreach (var line in hunk.Lines)
                {
                    string content = Encoding.UTF8.GetString(line.Content.Span);
                    switch (line.Type)
                    {
                        case UnifiedHunkLineType.Context:
                            result.Add(new DiffLineItem(oldLine++, newLine++, " " + content, DiffKind.Context));
                            break;
                        case UnifiedHunkLineType.Addition:
                            result.Add(new DiffLineItem(null, newLine++, "+" + content, DiffKind.Added));
                            break;
                        case UnifiedHunkLineType.Deletion:
                            result.Add(new DiffLineItem(oldLine++, null, "-" + content, DiffKind.Deleted));
                            break;
                    }
                }
            }
        }

        return result;
    }
}
