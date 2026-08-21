using System;
using Opure.Runtime.Contracts.Providers;
using Xunit;

namespace Opure.Runtime.Contracts.Tests.Providers;

public class DataHandlingRecordTests
{
    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        var uri = new Uri("https://api.example.com/terms");
        var retention = TimeSpan.FromDays(30);

        var record = new DataHandlingRecord("provider:example", uri, retention, false);

        Assert.Equal("provider:example", record.ProviderId);
        Assert.Equal(uri, record.TermsUrl);
        Assert.Equal(retention, record.RetentionDuration);
        Assert.False(record.UsesDataForTraining);
    }
}
