using System;
using System.Collections.Generic;
using Opure.Runtime.Contracts.Providers;
using Xunit;

namespace Opure.Runtime.Contracts.Tests.Providers;

public class ProviderProfileTests
{
    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        var capabilities = new List<string> { "Chat", "Embeddings" };
        var uri = new Uri("https://api.example.com");

        var profile = new ProviderProfile("provider:example", "Example", uri, capabilities);

        Assert.Equal("provider:example", profile.Id);
        Assert.Equal("Example", profile.Name);
        Assert.Equal(uri, profile.EndpointUrl);
        Assert.Equal(capabilities, profile.Capabilities);
    }
}
