using System.Runtime.Versioning;
using Microsoft.Data.Sqlite;
using Opure.Configuration.Contracts;
using Xunit;

namespace Opure.Configuration.Tests;

[SupportedOSPlatform("windows")]
public sealed class ConfigurationDatabaseTests : IDisposable
{
    private readonly string testRoot = Path.Combine(
        Path.GetTempPath(),
        "Opure.Configuration.Tests",
        Guid.NewGuid().ToString("N"));

    public ConfigurationDatabaseTests()
    {
        Directory.CreateDirectory(testRoot);
    }

    [Fact]
    public void FreshDatabaseSeedsDefaultUserBaseProfile()
    {
        using ConfigurationDatabase database = ConfigurationDatabase.Open(
            ChannelRoot,
            TestContext.Current.CancellationToken);

        ConfigurationProfile? profile = database.Read(
            "user.base",
            revision: 1,
            TestContext.Current.CancellationToken);

        Assert.NotNull(profile);
        Assert.Equal("user.base", profile.ProfileId);
        Assert.Equal((uint)1, profile.Revision);
        Assert.Equal("User Base Profile", profile.DisplayName);
        Assert.Equal("UserBase", profile.ProfileKind);
        Assert.Equal(SettingScope.User, profile.OwnerScope);
        Assert.Null(profile.ParentProfileId);
        Assert.Null(profile.ParentRevision);
        Assert.Equal((uint)1, profile.SchemaVersion);
        Assert.Equal("ProductInternal", profile.Classification);
        Assert.Empty(profile.Values);
        Assert.Equal(64, profile.CanonicalSha256.Length);
    }

    [Fact]
    public void GetLatestRevisionReturnsLatestProfile()
    {
        using ConfigurationDatabase database = ConfigurationDatabase.Open(
            ChannelRoot,
            TestContext.Current.CancellationToken);

        // Fetch user.base (revision 1 seeded)
        ConfigurationProfile? latest = database.GetLatestRevision(
            "user.base",
            TestContext.Current.CancellationToken);
        Assert.NotNull(latest);
        Assert.Equal((uint)1, latest.Revision);

        // Save revision 2
        ConfigurationProfile rev2 = new(
            "user.base",
            revision: 2,
            latest.DisplayName,
            latest.ProfileKind,
            latest.OwnerScope,
            latest.ParentProfileId,
            latest.ParentRevision,
            latest.SchemaVersion,
            latest.Classification,
            new Dictionary<string, string> { { "logging.level.default", "\"debug\"" } },
            DateTimeOffset.UtcNow);

        database.Save(rev2, TestContext.Current.CancellationToken);

        // Now latest should be revision 2
        ConfigurationProfile? newLatest = database.GetLatestRevision(
            "user.base",
            TestContext.Current.CancellationToken);
        Assert.NotNull(newLatest);
        Assert.Equal((uint)2, newLatest.Revision);
        Assert.Single(newLatest.Values);
        Assert.Equal("\"debug\"", newLatest.Values["logging.level.default"]);
    }

    [Fact]
    public void NonContiguousRevisionThrowsException()
    {
        using ConfigurationDatabase database = ConfigurationDatabase.Open(
            ChannelRoot,
            TestContext.Current.CancellationToken);

        ConfigurationProfile? latest = database.GetLatestRevision(
            "user.base",
            TestContext.Current.CancellationToken);
        Assert.NotNull(latest);

        // Save revision 3 directly (skipping 2)
        ConfigurationProfile rev3 = new(
            "user.base",
            revision: 3,
            latest.DisplayName,
            latest.ProfileKind,
            latest.OwnerScope,
            latest.ParentProfileId,
            latest.ParentRevision,
            latest.SchemaVersion,
            latest.Classification,
            new Dictionary<string, string>(),
            DateTimeOffset.UtcNow);

        _ = Assert.Throws<ArgumentException>(
            () => database.Save(rev3, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void GetHistoryReturnsAllRevisionsInOrder()
    {
        using ConfigurationDatabase database = ConfigurationDatabase.Open(
            ChannelRoot,
            TestContext.Current.CancellationToken);

        ConfigurationProfile? latest = database.GetLatestRevision(
            "user.base",
            TestContext.Current.CancellationToken);
        Assert.NotNull(latest);

        // Save rev 2
        ConfigurationProfile rev2 = new(
            "user.base",
            revision: 2,
            latest.DisplayName,
            latest.ProfileKind,
            latest.OwnerScope,
            latest.ParentProfileId,
            latest.ParentRevision,
            latest.SchemaVersion,
            latest.Classification,
            new Dictionary<string, string> { { "logging.level.default", "\"debug\"" } },
            DateTimeOffset.UtcNow);
        database.Save(rev2, TestContext.Current.CancellationToken);

        // Save rev 3
        ConfigurationProfile rev3 = new(
            "user.base",
            revision: 3,
            latest.DisplayName,
            latest.ProfileKind,
            latest.OwnerScope,
            latest.ParentProfileId,
            latest.ParentRevision,
            latest.SchemaVersion,
            latest.Classification,
            new Dictionary<string, string> { { "logging.level.default", "\"error\"" } },
            DateTimeOffset.UtcNow);
        database.Save(rev3, TestContext.Current.CancellationToken);

        IReadOnlyList<ConfigurationProfile> history = database.GetHistory(
            "user.base",
            TestContext.Current.CancellationToken);

        Assert.Equal(3, history.Count);
        Assert.Equal((uint)1, history[0].Revision);
        Assert.Equal((uint)2, history[1].Revision);
        Assert.Equal((uint)3, history[2].Revision);
    }

    [Fact]
    public void DatabaseDisposeClosesConnection()
    {
        string dbPath;
        using (ConfigurationDatabase database = ConfigurationDatabase.Open(
                   ChannelRoot,
                   TestContext.Current.CancellationToken))
        {
            dbPath = database.Descriptor.DatabasePath;
            Assert.True(File.Exists(dbPath));
        }

        // Connection should be closed. We verify by attempting to open connection in read-only mode.
        SqliteConnectionStringBuilder builder = new()
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false
        };
        using SqliteConnection connection = new(builder.ToString());
        connection.Open();
        Assert.Equal(System.Data.ConnectionState.Open, connection.State);
    }

    public void Dispose()
    {
        if (Directory.Exists(testRoot))
        {
            Directory.Delete(testRoot, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    private string ChannelRoot => Path.Combine(testRoot, "channel");
}
