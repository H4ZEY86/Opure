using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Opure.Desktop.Contracts;
using Opure.Runtime.Contracts.Models;
using Xunit;

namespace Opure.Desktop.Tests;

public sealed class LocalIntelligenceViewModelTests
{
    private sealed class MockLocalIntelligenceSource : ILocalIntelligenceSource
    {
        private readonly List<StreamPayload> _payloads;
        private readonly TimeSpan _delay;
        private readonly bool _throwCancel;

        public MockLocalIntelligenceSource(List<StreamPayload> payloads, TimeSpan delay = default, bool throwCancel = false)
        {
            _payloads = payloads;
            _delay = delay;
            _throwCancel = throwCancel;
        }

        public async IAsyncEnumerable<StreamPayload> GenerateStreamAsync(string prompt, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var payload in _payloads)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (_delay > TimeSpan.Zero)
                {
                    await Task.Delay(_delay, cancellationToken).ConfigureAwait(false);
                }
                cancellationToken.ThrowIfCancellationRequested();
                yield return payload;
            }

            if (_throwCancel)
            {
                throw new OperationCanceledException();
            }
        }
    }

    [Fact]
    public async Task Generates_text_and_ignores_tool_payloads_in_text_output()
    {
        var payloads = new List<StreamPayload>
        {
            new StreamPayload(false, "Hello "),
            new StreamPayload(false, "world!"),
            new StreamPayload(true, "{\"toolName\":\"read_file_range\",\"arguments\":{\"path\":\"doc.txt\",\"start\":1,\"end\":10}}")
        };

        var source = new MockLocalIntelligenceSource(payloads);
        var viewModel = new LocalIntelligenceViewModel(source);
        
        viewModel.Prompt = "Test";
        viewModel.GenerateCommand.Execute(null);

        while (viewModel.IsGenerating)
        {
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }

        Assert.Equal("Hello world!", viewModel.GeneratedText);
        Assert.Single(viewModel.ToolActivity);
        Assert.False(viewModel.ToolActivity[0].IsActive);
        Assert.Contains("Reading doc.txt", viewModel.ToolActivity[0].StatusText);
    }

    [Fact]
    public async Task Translates_tool_calls_to_formatted_activity_items()
    {
        var payloads = new List<StreamPayload>
        {
            new StreamPayload(true, JsonSerializer.Serialize(new ToolRequest("apply_patch", new Dictionary<string, object> { { "path", "test.txt" } }), ModelContractsJsonContext.Default.ToolRequest)),
            new StreamPayload(true, JsonSerializer.Serialize(new ToolRequest("run_command", new Dictionary<string, object> { { "command", "echo test" } }), ModelContractsJsonContext.Default.ToolRequest))
        };

        var source = new MockLocalIntelligenceSource(payloads);
        var viewModel = new LocalIntelligenceViewModel(source);
        
        viewModel.Prompt = "Test tools";
        viewModel.GenerateCommand.Execute(null);

        while (viewModel.IsGenerating)
        {
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }

        Assert.Empty(viewModel.GeneratedText);
        Assert.Equal(2, viewModel.ToolActivity.Count);
        
        Assert.False(viewModel.ToolActivity[0].IsActive);
        Assert.Equal("Staging workspace patch for test.txt...", viewModel.ToolActivity[0].StatusText);
        
        Assert.False(viewModel.ToolActivity[1].IsActive);
        Assert.Equal("Executing sandboxed command: echo test...", viewModel.ToolActivity[1].StatusText);
    }

    [Fact]
    public async Task Cancellation_halts_stream_and_deactivates_tools()
    {
        var payloads = new List<StreamPayload>
        {
            new StreamPayload(true, JsonSerializer.Serialize(new ToolRequest("list_directory", new Dictionary<string, object> { { "path", "." } }), ModelContractsJsonContext.Default.ToolRequest))
        };

        var source = new MockLocalIntelligenceSource(payloads, TimeSpan.FromMilliseconds(5000), true);
        var viewModel = new LocalIntelligenceViewModel(source);
        
        viewModel.Prompt = "Test cancel";
        viewModel.GenerateCommand.Execute(null);
        
        await Task.Delay(10, TestContext.Current.CancellationToken);
        Assert.True(viewModel.IsGenerating);
        viewModel.StopCommand.Execute(null);

        while (viewModel.IsGenerating)
        {
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }

        Assert.Contains("[Generation Stopped]", viewModel.GeneratedText);
        Assert.False(viewModel.IsGenerating);
        
        foreach(var tool in viewModel.ToolActivity)
        {
            Assert.False(tool.IsActive);
        }
    }
}
