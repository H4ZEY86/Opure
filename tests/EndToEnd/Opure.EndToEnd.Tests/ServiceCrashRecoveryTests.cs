using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Opure.Desktop.Contracts;
using Opure.Desktop.GatewayClient;
using Opure.Ipc.Abstractions;
using Opure.Filesystem.Windows;
using Opure.Filesystem.Contracts;

namespace Opure.EndToEnd.Tests;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
[Collection("E2E")]
public class ServiceCrashRecoveryTests
{
    private static async Task<Process> GetRuntimeProcessAsync(int parentId)
    {
        for (int i = 0; i < 10; i++)
        {
            var processes = Process.GetProcessesByName("Opure.Runtime");
            if (processes.Length > 0)
                return processes.First();
                
            await Task.Delay(1000);
        }
        throw new InvalidOperationException("Runtime process not found.");
    }

    [Fact]
    public async Task ProjectService_CrashMidFlight_RecoversCorrectly()
    {
        var envVars = new Dictionary<string, string>
        {
            { "OPURE_TEST_CRASH_POINT", "ProjectServiceMidFlight" }
        };

        using var harness = new EndToEndHarness(environmentVariables: envVars);
        var env = await harness.GetTestSessionAsync(TestContext.Current.CancellationToken);
        
        var endpoint = new RuntimeHealthEndpoint(env["OPURE_IPC_PIPE"], env.TryGetValue("OPURE_RUNTIME_BOOT_ID", out var bootId) ? bootId : new string('0', 32));
        var session = new RuntimeHealthSessionMaterial(env["OPURE_BOOTSTRAP_SESSION_ID"], env["OPURE_BOOTSTRAP_SESSION_SECRET"]);

        var receiver = RuntimeHealthGatewayClient.CreateProjectRootReceiver("Test");
        
        string dummyProject = Path.Combine(harness.DataRoot, "DummyProject");
        Directory.CreateDirectory(dummyProject);
        File.WriteAllText(Path.Combine(dummyProject, "Opure.slnx"), "<Solution />");
        
        VerifiedWorkspaceRootReference reference = WindowsPathReferenceResolver.AcquireRoot(new UntrustedPathText(dummyProject));
        
        try
        {
            // This should cause the Runtime to crash
            _ = await receiver.ReceiveAsync(reference, TestContext.Current.CancellationToken);
        }
        catch
        {
            // Expected to fail due to broken pipe/crash
        }

        // Wait for Bootstrap to restart Runtime
        await Task.Delay(3000, TestContext.Current.CancellationToken);
        
        var newEnv = await harness.GetTestSessionAsync(TestContext.Current.CancellationToken);
        
        // Ensure Runtime is healthy again
        var newEndpoint = new RuntimeHealthEndpoint(newEnv["OPURE_IPC_PIPE"], newEnv.TryGetValue("OPURE_RUNTIME_BOOT_ID", out var newBootId) ? newBootId : new string('0', 32));
        var newSession = new RuntimeHealthSessionMaterial(newEnv["OPURE_BOOTSTRAP_SESSION_ID"], newEnv["OPURE_BOOTSTRAP_SESSION_SECRET"]);
        var healthSource = RuntimeHealthGatewayClient.CreateProjectionSource("1.0.0", DesktopSupervisorProjection.Disconnected, newEndpoint, newSession);
        
        var snapshot = await healthSource.RefreshAsync(TestContext.Current.CancellationToken);
        Assert.Equal(DesktopRuntimeConnectionState.Connected, snapshot.ConnectionState);
        
        // Since we crashed *after* commit but before snapshot/reconciliation, 
        // the project list should show it in a RecoveryRequired state (or opening)
        var listSource = RuntimeHealthGatewayClient.CreateProjectListSource("Test");
        var projection = await listSource.RefreshAsync(TestContext.Current.CancellationToken);
        
        // The project should be listed because the registration commit happened before the crash
        Assert.Contains(projection.Projects, p => p.DisplayName == "DummyProject");
    }

    [Fact]
    public async Task TrustEvidence_CrashDuringIngestion_RecoversCleanly()
    {
        var envVars = new Dictionary<string, string>
        {
            { "OPURE_TEST_CRASH_POINT", "TrustEvidenceIngestion" }
        };

        using var harness = new EndToEndHarness(environmentVariables: envVars);
        var env = await harness.GetTestSessionAsync(TestContext.Current.CancellationToken);
        
        var endpoint = new RuntimeHealthEndpoint(env["OPURE_IPC_PIPE"], env.TryGetValue("OPURE_RUNTIME_BOOT_ID", out var bootId) ? bootId : new string('0', 32));
        var session = new RuntimeHealthSessionMaterial(env["OPURE_BOOTSTRAP_SESSION_ID"], env["OPURE_BOOTSTRAP_SESSION_SECRET"]);

        var healthSource = RuntimeHealthGatewayClient.CreateProjectionSource("1.0.0", DesktopSupervisorProjection.Disconnected, endpoint, session);
        var snapshot = await healthSource.RefreshAsync(TestContext.Current.CancellationToken);
        
        // At this point we are connected. We need a way to ingest trust evidence.
        // The GatewayClient might not have a direct TrustEvidence ingress exposed to tests without going through another service.
        // But since the project open service inherently emits trust evidence during open (ProjectTrustEvidencePublisher),
        // we can trigger it via ProjectOpen!
        
        var receiver = RuntimeHealthGatewayClient.CreateProjectRootReceiver("Test");
        
        string dummyProject = Path.Combine(harness.DataRoot, "DummyProject2");
        Directory.CreateDirectory(dummyProject);
        File.WriteAllText(Path.Combine(dummyProject, "Opure.slnx"), "<Solution />");
        
        VerifiedWorkspaceRootReference reference = WindowsPathReferenceResolver.AcquireRoot(new UntrustedPathText(dummyProject));
        
        try
        {
            // Opening the project commits an open operation, which triggers the SqliteOutboxDispatcher
            // to send evidence to TrustEvidenceIngestion, hitting our crash point in TrustEvidenceServiceHost!
            _ = await receiver.ReceiveAsync(reference, TestContext.Current.CancellationToken);
        }
        catch
        {
            // Expected to crash
        }

        // Wait for Bootstrap to restart Runtime
        await Task.Delay(3000, TestContext.Current.CancellationToken);
        
        var newEnv = await harness.GetTestSessionAsync(TestContext.Current.CancellationToken);
        
        var newEndpoint = new RuntimeHealthEndpoint(newEnv["OPURE_IPC_PIPE"], newEnv.TryGetValue("OPURE_RUNTIME_BOOT_ID", out var newBootId) ? newBootId : new string('0', 32));
        var newSession = new RuntimeHealthSessionMaterial(newEnv["OPURE_BOOTSTRAP_SESSION_ID"], newEnv["OPURE_BOOTSTRAP_SESSION_SECRET"]);
        var newHealthSource = RuntimeHealthGatewayClient.CreateProjectionSource("1.0.0", DesktopSupervisorProjection.Disconnected, newEndpoint, newSession);
        
        var newSnapshot = await newHealthSource.RefreshAsync(TestContext.Current.CancellationToken);
        Assert.Equal(DesktopRuntimeConnectionState.Connected, newSnapshot.ConnectionState);
    }
}
