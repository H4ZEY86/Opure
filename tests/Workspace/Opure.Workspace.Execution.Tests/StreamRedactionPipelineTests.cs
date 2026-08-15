using System;
using Xunit;
using Opure.Workspace.Execution;

namespace Opure.Workspace.Execution.Tests;

public class StreamRedactionPipelineTests
{
    [Fact]
    public void Scrub_WithAnsiEscapes_StripsThem()
    {
        string input = "Hello \x1B[31mWorld\x1B[0m!";
        string result = StreamRedactionPipeline.Scrub(input, out bool redactionApplied, out bool encodingFaults);

        Assert.True(redactionApplied);
        Assert.False(encodingFaults);
        Assert.Equal("Hello World!", result);
    }

    [Fact]
    public void Scrub_WithAwsAccessKey_RedactsIt()
    {
        string input = "Here is my key: AKIAIOSFODNN7EXAMPLE for AWS.";
        string result = StreamRedactionPipeline.Scrub(input, out bool redactionApplied, out bool encodingFaults);

        Assert.True(redactionApplied);
        Assert.False(encodingFaults);
        Assert.Equal("Here is my key: [REDACTED] for AWS.", result);
    }

    [Fact]
    public void Scrub_WithJwt_RedactsIt()
    {
        string input = "Token: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        string result = StreamRedactionPipeline.Scrub(input, out bool redactionApplied, out bool encodingFaults);

        Assert.True(redactionApplied);
        Assert.False(encodingFaults);
        Assert.Equal("Token: [REDACTED]", result);
    }

    [Fact]
    public void Scrub_WithReplacementCharacter_FlagsEncodingFaults()
    {
        string input = "Invalid \uFFFD sequence";
        string result = StreamRedactionPipeline.Scrub(input, out bool redactionApplied, out bool encodingFaults);

        Assert.False(redactionApplied);
        Assert.True(encodingFaults);
        Assert.Equal(input, result);
    }
}
