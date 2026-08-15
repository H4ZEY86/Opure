using System;
using Opure.Patch.Contracts;

namespace Opure.Patch.Service;

/// <summary>
/// A high-performance, zero-allocation UTF-8 line reader for unified patches.
/// Ensures maximum line length, rejects BOMs, and rejects NUL bytes.
/// </summary>
public ref struct UnifiedDiffLineReader
{
    private ReadOnlySpan<byte> _remaining;
    private int _lineNumber;
    private bool _isFirstLine;
    
    public const int MaxLineLengthBytes = 32768; // 32 KB

    public UnifiedDiffLineReader(ReadOnlySpan<byte> payload)
    {
        _remaining = payload;
        _lineNumber = 0;
        _isFirstLine = true;
    }

    /// <summary>
    /// Reads the next line from the payload.
    /// </summary>
    /// <param name="line">The line contents excluding the terminator.</param>
    /// <param name="lineTerminator">The terminator (LF, CRLF, or empty if EOF).</param>
    /// <returns>True if a line was read, false if EOF.</returns>
    public bool TryReadLine(out ReadOnlySpan<byte> line, out ReadOnlySpan<byte> lineTerminator)
    {
        if (_remaining.IsEmpty)
        {
            line = default;
            lineTerminator = default;
            return false;
        }

        _lineNumber++;

        if (_isFirstLine)
        {
            _isFirstLine = false;
            // Reject UTF-8 BOM
            if (_remaining.Length >= 3 && _remaining[0] == 0xEF && _remaining[1] == 0xBB && _remaining[2] == 0xBF)
            {
                throw new PreconditionFailedException("Exact UTF-8 patch must not include a byte-order mark.");
            }
        }

        int newlineIndex = _remaining.IndexOf((byte)'\n');
        
        ReadOnlySpan<byte> currentLineBytes;
        ReadOnlySpan<byte> currentTerminator;

        if (newlineIndex >= 0)
        {
            if (newlineIndex > 0 && _remaining[newlineIndex - 1] == (byte)'\r')
            {
                currentLineBytes = _remaining.Slice(0, newlineIndex - 1);
                currentTerminator = _remaining.Slice(newlineIndex - 1, 2);
            }
            else
            {
                currentLineBytes = _remaining.Slice(0, newlineIndex);
                currentTerminator = _remaining.Slice(newlineIndex, 1);
            }
            _remaining = _remaining.Slice(newlineIndex + 1);
        }
        else
        {
            currentLineBytes = _remaining;
            currentTerminator = default;
            _remaining = default;
        }

        if (currentLineBytes.Length > MaxLineLengthBytes)
        {
            throw new PreconditionFailedException($"Line {_lineNumber} exceeds the maximum length of 32 KB.");
        }

        if (currentLineBytes.IndexOf((byte)0) >= 0)
        {
            throw new PreconditionFailedException($"Line {_lineNumber} contains invalid binary NUL byte.");
        }

        line = currentLineBytes;
        lineTerminator = currentTerminator;
        return true;
    }
}
