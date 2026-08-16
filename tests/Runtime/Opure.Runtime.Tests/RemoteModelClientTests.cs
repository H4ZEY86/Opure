using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Opure.Runtime.Contracts.Models;
using Opure.Runtime.Models;
using Xunit;

namespace Opure.Runtime.Tests;

public class RemoteModelClientTests
{
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _sseContent;

        public MockHttpMessageHandler(string sseContent)
        {
            _sseContent = sseContent;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_sseContent, Encoding.UTF8, "text/event-stream")
            };
            return Task.FromResult(response);
        }
    }

    [Fact]
    public async Task RunRemoteModelAsync_StreamsTokens_Successfully()
    {
        // Arrange
        var payload1 = new StreamPayload(false, "Hello ");
        var payload2 = new StreamPayload(false, "World");
        var sseData = 
            $"data: {JsonSerializer.Serialize(payload1, ModelContractsJsonContext.Default.StreamPayload)}\n\n" +
            $"data: {JsonSerializer.Serialize(payload2, ModelContractsJsonContext.Default.StreamPayload)}\n\n" +
            $"data: [DONE]\n\n";

        var httpClient = new HttpClient(new MockHttpMessageHandler(sseData));
        var client = new RemoteModelClient(httpClient);

        var config = new RemoteProviderConfiguration
        {
            EndpointUrl = "https://api.openai.com/v1/chat/completions",
            TransientAuthToken = "test-token"
        };
        var request = ModelRequest.FromPrompt("Say hello");

        // Act
        var payloads = new List<StreamPayload>();
        await foreach (var p in client.RunRemoteModelAsync(config, request, TestContext.Current.CancellationToken))
        {
            payloads.Add(p);
        }

        // Assert
        Assert.Equal(2, payloads.Count);
        Assert.Equal("Hello ", payloads[0].Content);
        Assert.Equal("World", payloads[1].Content);
    }

    [Fact]
    public async Task RunRemoteModelAsync_RespectsCancellation()
    {
        // Arrange
        var httpClient = new HttpClient(new MockHttpMessageHandler("data: [DONE]\n\n"));
        var client = new RemoteModelClient(httpClient);

        var config = new RemoteProviderConfiguration
        {
            EndpointUrl = "https://api.openai.com/v1/chat/completions"
        };
        var request = ModelRequest.FromPrompt("Cancel me");
        
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // pre-cancel

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var p in client.RunRemoteModelAsync(config, request, cts.Token))
            {
            }
        });
    }
}
