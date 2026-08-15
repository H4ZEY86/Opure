using System;
using System.Collections.Generic;
using System.Text;
using Opure.Patch.Contracts;

namespace Opure.Patch.Service;

public static class UnifiedDiffParser
{
    private const int MaxHunkSizeLines = 10000;

    public static UnifiedPatchProposal Parse(ReadOnlySpan<byte> payload)
    {
        var reader = new UnifiedDiffLineReader(payload);
        
        string? originalFile = null;
        string? targetFile = null;
        var hunks = new List<UnifiedHunk>();

        while (reader.TryReadLine(out var line, out var term))
        {
            if (line.StartsWith("--- "u8))
            {
                originalFile = ParseFileName(line, 4);
            }
            else if (line.StartsWith("+++ "u8))
            {
                targetFile = ParseFileName(line, 4);
                break;
            }
            else
            {
                throw new PreconditionFailedException("Invalid unified diff header. Expected --- and +++ lines.");
            }
        }

        if (originalFile == null || targetFile == null)
        {
            throw new PreconditionFailedException("Incomplete unified diff headers.");
        }

        while (reader.TryReadLine(out var line, out var term))
        {
            if (line.StartsWith("@@ "u8))
            {
                hunks.Add(ParseHunk(ref reader, line));
            }
            else
            {
                throw new PreconditionFailedException("Expected hunk header @@.");
            }
        }

        return new UnifiedPatchProposal
        {
            OriginalFileHeader = originalFile,
            TargetFileHeader = targetFile,
            Hunks = hunks
        };
    }

    private static string ParseFileName(ReadOnlySpan<byte> line, int prefixLength)
    {
        var pathBytes = line.Slice(prefixLength);
        var str = Encoding.UTF8.GetString(pathBytes);
        
        if (str.StartsWith("a/") || str.StartsWith("b/"))
        {
            return str.Substring(2);
        }
        if (str.StartsWith("\"a/") || str.StartsWith("\"b/"))
        {
            str = str.Trim('"');
            if (str.StartsWith("a/") || str.StartsWith("b/"))
            {
                return str.Substring(2);
            }
        }
        return str;
    }

    private static UnifiedHunk ParseHunk(ref UnifiedDiffLineReader reader, ReadOnlySpan<byte> headerLine)
    {
        var headerStr = Encoding.UTF8.GetString(headerLine);
        var parts = headerStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        if (parts.Length < 4 || parts[0] != "@@" || parts[3] != "@@")
        {
            throw new PreconditionFailedException("Malformed hunk header.");
        }

        ParseRange(parts[1], out int origStart, out int origCount);
        ParseRange(parts[2], out int targetStart, out int targetCount);

        var lines = new List<UnifiedHunkLine>();
        
        int currentOrig = 0;
        int currentTarget = 0;
        
        while (currentOrig < origCount || currentTarget < targetCount)
        {
            if (lines.Count >= MaxHunkSizeLines)
            {
                throw new PreconditionFailedException($"Hunk exceeds maximum allowed size of {MaxHunkSizeLines} lines.");
            }

            if (!reader.TryReadLine(out var line, out var terminator))
            {
                throw new PreconditionFailedException("Unexpected EOF within hunk context.");
            }

            if (line.StartsWith("\\ "u8))
            {
                // \ No newline at end of file
                continue;
            }

            UnifiedHunkLineType type;
            byte prefix = line.IsEmpty ? (byte)' ' : line[0];

            if (prefix == (byte)' ')
            {
                type = UnifiedHunkLineType.Context;
                currentOrig++;
                currentTarget++;
            }
            else if (prefix == (byte)'-')
            {
                type = UnifiedHunkLineType.Deletion;
                currentOrig++;
            }
            else if (prefix == (byte)'+')
            {
                type = UnifiedHunkLineType.Addition;
                currentTarget++;
            }
            else
            {
                if (line.IsEmpty)
                {
                    type = UnifiedHunkLineType.Context;
                    currentOrig++;
                    currentTarget++;
                }
                else
                {
                    throw new PreconditionFailedException("Invalid hunk line prefix. Expected space, +, or -.");
                }
            }

            ReadOnlySpan<byte> contentBytes = line.IsEmpty ? default : line.Slice(1);
            
            // To guarantee exact cryptographic match of context, we append the exact patch terminator
            byte[] fullContent = new byte[contentBytes.Length + terminator.Length];
            contentBytes.CopyTo(fullContent);
            terminator.CopyTo(fullContent.AsSpan(contentBytes.Length));
            
            lines.Add(new UnifiedHunkLine
            {
                Type = type,
                Content = fullContent
            });
        }
        
        return new UnifiedHunk
        {
            OriginalStartLine = origStart,
            OriginalLineCount = origCount,
            TargetStartLine = targetStart,
            TargetLineCount = targetCount,
            Lines = lines
        };
    }

    private static void ParseRange(string rangeToken, out int start, out int count)
    {
        var span = rangeToken.AsSpan(1); // skip + or -
        int commaIdx = span.IndexOf(',');
        if (commaIdx >= 0)
        {
            start = int.Parse(span.Slice(0, commaIdx));
            count = int.Parse(span.Slice(commaIdx + 1));
        }
        else
        {
            start = int.Parse(span);
            count = 1;
        }
    }
}
