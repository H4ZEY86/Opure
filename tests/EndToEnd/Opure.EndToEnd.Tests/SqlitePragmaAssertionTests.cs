using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Data.Sqlite;
using Opure.Desktop.Contracts;
using Opure.Desktop.GatewayClient;
using Opure.Ipc.Abstractions;
using Opure.Filesystem.Windows;
using Opure.Filesystem.Contracts;

namespace Opure.EndToEnd.Tests;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
[Collection("E2E")]
public class SqlitePragmaAssertionTests
{
    [Fact]
    public async Task OutOfBand_PragmaAssertions_EnforceHardening()
    {
        using var harness = new EndToEndHarness();
        var env = await harness.GetTestSessionAsync(TestContext.Current.CancellationToken);
        
        var endpoint = new RuntimeHealthEndpoint(env["OPURE_IPC_PIPE"], env.TryGetValue("OPURE_RUNTIME_BOOT_ID", out var bootId) ? bootId : new string('0', 32));
        var session = new RuntimeHealthSessionMaterial(env["OPURE_BOOTSTRAP_SESSION_ID"], env["OPURE_BOOTSTRAP_SESSION_SECRET"]);

        var healthSource = RuntimeHealthGatewayClient.CreateProjectionSource("1.0.0", DesktopSupervisorProjection.Disconnected, endpoint, session);
        await healthSource.RefreshAsync(TestContext.Current.CancellationToken);
        
        var receiver = RuntimeHealthGatewayClient.CreateProjectRootReceiver("Test");
        Assert.NotNull(receiver);
        
        string dummyProject = Path.Combine(harness.DataRoot, "DummyProject");
        Directory.CreateDirectory(dummyProject);
        File.WriteAllText(Path.Combine(dummyProject, "Opure.slnx"), "<Solution />");
        
        VerifiedWorkspaceRootReference reference = WindowsPathReferenceResolver.AcquireRoot(new UntrustedPathText(dummyProject));
        await receiver.ReceiveAsync(reference, TestContext.Current.CancellationToken);
        
        // Wait for the databases to be created
        await Task.Delay(1000, TestContext.Current.CancellationToken);
        
        string dbPath = Path.Combine(harness.DataRoot, "Runtime", "services", "opure.trust-evidence", "databases", "trust.db");
        if (!File.Exists(dbPath))
        {
            // Try different path if it's not under Runtime/services
            dbPath = Path.Combine(harness.DataRoot, "services", "opure.trust-evidence", "databases", "trust.db");
        }

        Assert.True(File.Exists(dbPath), $"Database file not found at {dbPath}");

        using var connection = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode;";
        var journalMode = (string)cmd.ExecuteScalar()!;
        Assert.Equal("wal", journalMode, ignoreCase: true);

        // trusted_schema is persistent in some SQLite versions, but usually per-connection.
        // What we CAN check out-of-band:
        // user_version is used in migrations
        cmd.CommandText = "PRAGMA user_version;";
        var userVersion = (long)cmd.ExecuteScalar()!;
        Assert.True(userVersion > 0, "user_version should be greater than 0");

        cmd.CommandText = "PRAGMA application_id;";
        var appId = (long)cmd.ExecuteScalar()!;
        Assert.True(appId > 0, "application_id should be set");
    }
}
