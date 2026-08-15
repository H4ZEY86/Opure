using System;
using System.Text;
using Opure.Patch.Contracts;
using Xunit;

namespace Opure.Patch.Service.Tests;

public sealed class UnifiedDiffLineReaderTests
{
    [Fact]
    public void ReadLine_ValidUtf8WithoutBom_ReadsLinesSuccessfully()
    {
        string input = "Line 1\nLine 2\r\nLine 3";
        byte[] payload = Encoding.UTF8.GetBytes(input);

        var reader = new UnifiedDiffLineReader(payload);

        Assert.True(reader.TryReadLine(out var line1, out var term1));
        Assert.Equal("Line 1", Encoding.UTF8.GetString(line1));
        Assert.Equal("\n", Encoding.UTF8.GetString(term1));

        Assert.True(reader.TryReadLine(out var line2, out var term2));
        Assert.Equal("Line 2", Encoding.UTF8.GetString(line2));
        Assert.Equal("\r\n", Encoding.UTF8.GetString(term2));

        Assert.True(reader.TryReadLine(out var line3, out var term3));
        Assert.Equal("Line 3", Encoding.UTF8.GetString(line3));
        Assert.True(term3.IsEmpty);

        Assert.False(reader.TryReadLine(out _, out _));
    }

    [Fact]
    public void ReadLine_WithBom_ThrowsPreconditionFailedException()
    {
        byte[] payload = { 0xEF, 0xBB, 0xBF, (byte)'H', (byte)'i' };

        var reader = new UnifiedDiffLineReader(payload);

        bool threw = false;
        try
        {
            reader.TryReadLine(out _, out _);
        }
        catch (PreconditionFailedException ex)
        {
            threw = true;
            Assert.Contains("must not include a byte-order mark", ex.Message);
        }
        Assert.True(threw, "Expected PreconditionFailedException to be thrown.");
    }

    [Fact]
    public void ReadLine_WithNulByte_ThrowsPreconditionFailedException()
    {
        byte[] payload = { (byte)'O', (byte)'K', (byte)'\n', (byte)'B', 0x00, (byte)'D' };

        var reader = new UnifiedDiffLineReader(payload);

        Assert.True(reader.TryReadLine(out var line1, out _));
        Assert.Equal("OK", Encoding.UTF8.GetString(line1));

        bool threw = false;
        try
        {
            reader.TryReadLine(out _, out _);
        }
        catch (PreconditionFailedException ex)
        {
            threw = true;
            Assert.Contains("contains invalid binary NUL byte", ex.Message);
        }
        Assert.True(threw, "Expected PreconditionFailedException to be thrown.");
    }

    [Fact]
    public void ReadLine_ExceedsMaxLength_ThrowsPreconditionFailedException()
    {
        byte[] payload = new byte[32769];
        Array.Fill(payload, (byte)'A');

        var reader = new UnifiedDiffLineReader(payload);

        bool threw = false;
        try
        {
            reader.TryReadLine(out _, out _);
        }
        catch (PreconditionFailedException ex)
        {
            threw = true;
            Assert.Contains("exceeds the maximum length of 32 KB", ex.Message);
        }
        Assert.True(threw, "Expected PreconditionFailedException to be thrown.");
    }

    [Fact]
    public void ReadLine_EmptyPayload_ReturnsFalse()
    {
        var reader = new UnifiedDiffLineReader(ReadOnlySpan<byte>.Empty);

        Assert.False(reader.TryReadLine(out _, out _));
    }
}
