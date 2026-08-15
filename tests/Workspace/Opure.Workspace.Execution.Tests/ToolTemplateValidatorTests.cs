using System;
using Opure.Workspace.Contracts;
using Opure.Patch.Contracts;
using Xunit;

namespace Opure.Workspace.Execution.Tests;

public class ToolTemplateValidatorTests
{
    private static readonly string[] ValidArgs = new[] { "status" };
    private static readonly string[] ValidEnv = new[] { "SystemRoot" };

    private static ToolTemplate CreateValidTemplate()
    {
        return new ToolTemplate(
            "valid-template",
            "git.exe",
            ValidArgs,
            10000,
            ToolEffectClass.ReadOnly,
            new ToolEnvironmentPolicy(ValidEnv),
            new ToolInputOutputPolicy(false, 1024),
            ResourceClass.Lightweight);
    }

    [Fact]
    public void Validate_ValidTemplate_Passes()
    {
        var template = CreateValidTemplate();
        var ex = Record.Exception(() => ToolTemplateValidator.Validate(template));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyId_Throws(string? id)
    {
        var template = CreateValidTemplate() with { Id = id! };
        var ex = Assert.Throws<PreconditionFailedException>(() => ToolTemplateValidator.Validate(template));
        Assert.Contains("Template ID cannot be empty", ex.Message);
    }

    [Theory]
    [InlineData("cmd")]
    [InlineData("cmd.exe")]
    [InlineData("pwsh")]
    [InlineData("powershell")]
    [InlineData("bash")]
    [InlineData("sh")]
    [InlineData("wsl")]
    public void Validate_BannedShellExecutable_Throws(string exe)
    {
        var template = CreateValidTemplate() with { ExecutableName = exe };
        var ex = Assert.Throws<PreconditionFailedException>(() => ToolTemplateValidator.Validate(template));
        Assert.Contains("banned shell identifier", ex.Message);
    }

    [Theory]
    [InlineData("echo $VAR")]
    [InlineData("echo %VAR%")]
    [InlineData("rm -rf / & echo done")]
    [InlineData("cat foo | grep bar")]
    [InlineData("echo test > out.txt")]
    [InlineData("echo test < in.txt")]
    [InlineData("echo `date`")]
    [InlineData("echo line1\nline2")]
    public void Validate_BannedMetacharactersInArguments_Throws(string arg)
    {
        var template = CreateValidTemplate() with { Arguments = new[] { arg } };
        var ex = Assert.Throws<PreconditionFailedException>(() => ToolTemplateValidator.Validate(template));
        Assert.Contains("banned shell metacharacters", ex.Message);
    }

    [Theory]
    [InlineData("../secrets.txt")]
    [InlineData("..\\secrets.txt")]
    [InlineData("folder/../../secrets.txt")]
    public void Validate_DirectoryTraversalInArguments_Throws(string arg)
    {
        var template = CreateValidTemplate() with { Arguments = new[] { arg } };
        var ex = Assert.Throws<PreconditionFailedException>(() => ToolTemplateValidator.Validate(template));
        Assert.Contains("directory traversal sequences", ex.Message);
    }

    [Fact]
    public void Validate_ReadOnlyWithStdin_Throws()
    {
        var template = CreateValidTemplate() with 
        { 
            EffectClass = ToolEffectClass.ReadOnly,
            InputOutputPolicy = new ToolInputOutputPolicy(true, 1024) 
        };
        var ex = Assert.Throws<PreconditionFailedException>(() => ToolTemplateValidator.Validate(template));
        Assert.Contains("Read-only templates cannot support STDIN", ex.Message);
    }

    [Fact]
    public void Validate_InvalidTimeout_Throws()
    {
        var template = CreateValidTemplate() with { TimeoutMilliseconds = 0 };
        var ex = Assert.Throws<PreconditionFailedException>(() => ToolTemplateValidator.Validate(template));
        Assert.Contains("Timeout must be strictly positive", ex.Message);
    }
}
