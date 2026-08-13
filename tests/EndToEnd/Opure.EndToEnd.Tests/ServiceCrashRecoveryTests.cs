using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
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
        var originalRuntime = await GetRuntimeProcessAsync(harness.BootstrapProcess.Id);
        int originalRuntimeId = originalRuntime.Id;
        
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
        var restartedRuntime = await GetRuntimeProcessAsync(harness.BootstrapProcess.Id);
        Assert.NotEqual(originalRuntimeId, restartedRuntime.Id);
        Assert.NotEqual(env["OPURE_RUNTIME_BOOT_ID"], newEnv["OPURE_RUNTIME_BOOT_ID"]);
        
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
        var originalRuntime = await GetRuntimeProcessAsync(harness.BootstrapProcess.Id);
        int originalRuntimeId = originalRuntime.Id;
        
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
        var restartedRuntime = await GetRuntimeProcessAsync(harness.BootstrapProcess.Id);
        Assert.NotEqual(originalRuntimeId, restartedRuntime.Id);
        Assert.NotEqual(env["OPURE_RUNTIME_BOOT_ID"], newEnv["OPURE_RUNTIME_BOOT_ID"]);
        
        var newEndpoint = new RuntimeHealthEndpoint(newEnv["OPURE_IPC_PIPE"], newEnv.TryGetValue("OPURE_RUNTIME_BOOT_ID", out var newBootId) ? newBootId : new string('0', 32));
        var newSession = new RuntimeHealthSessionMaterial(newEnv["OPURE_BOOTSTRAP_SESSION_ID"], newEnv["OPURE_BOOTSTRAP_SESSION_SECRET"]);
        var newHealthSource = RuntimeHealthGatewayClient.CreateProjectionSource("1.0.0", DesktopSupervisorProjection.Disconnected, newEndpoint, newSession);
        
        var newSnapshot = await newHealthSource.RefreshAsync(TestContext.Current.CancellationToken);
        Assert.Equal(DesktopRuntimeConnectionState.Connected, newSnapshot.ConnectionState);
    }

    [Theory]
    [InlineData("WorkspaceGenerationBeforeCommit")]
    [InlineData("ConfigurationBeforeCommit")]
    [InlineData("ConfigurationAfterCommitBeforeOutbox")]
    public async Task OwnerCommitCrashPoint_RestartsRuntimeWithoutLosingProject(
        string crashPoint)
    {
        string crashArmFile = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData,
                Environment.SpecialFolderOption.DoNotVerify),
            "Opure",
            "Test",
            "owner-commit-crash.arm");
        var environmentVariables = new Dictionary<string, string>
        {
            ["OPURE_TEST_CRASH_POINT"] = crashPoint,
            ["OPURE_TEST_CRASH_ARM_FILE"] = crashArmFile
        };
        using var harness = new EndToEndHarness(environmentVariables: environmentVariables);
        var environment = await harness.GetTestSessionAsync(
            TestContext.Current.CancellationToken);
        var originalRuntime = await GetRuntimeProcessAsync(harness.BootstrapProcess.Id);
        int originalRuntimeId = originalRuntime.Id;
        var receiver = RuntimeHealthGatewayClient.CreateProjectRootReceiver("Test");
        string projectRoot = Path.Combine(harness.DataRoot, $"Project-{crashPoint}");
        Directory.CreateDirectory(projectRoot);
        File.WriteAllText(Path.Combine(projectRoot, "Opure.slnx"), "<Solution />");
        VerifiedWorkspaceRootReference reference =
            WindowsPathReferenceResolver.AcquireRoot(new UntrustedPathText(projectRoot));

        if (crashPoint.StartsWith("Configuration", StringComparison.Ordinal))
        {
            _ = await receiver.ReceiveAsync(
                reference,
                TestContext.Current.CancellationToken);
            var initialProjects = await RuntimeHealthGatewayClient
                .CreateProjectListSource("Test")
                .RefreshAsync(TestContext.Current.CancellationToken);
            var project = Assert.Single(
                initialProjects.Projects,
                project => project.DisplayName == Path.GetFileName(projectRoot));
            string settingsRoot = Path.Combine(projectRoot, ".opure");
            Directory.CreateDirectory(settingsRoot);
            await File.WriteAllTextAsync(
                Path.Combine(settingsRoot, "project.settings.json"),
                JsonSerializer.Serialize(new
                {
                    schema = "opure.project-settings/1",
                    project_id = project.ProjectId,
                    settings = new Dictionary<string, string>
                    {
                        ["logging.level.default"] = "debug"
                    }
                }),
                TestContext.Current.CancellationToken);
            reference = WindowsPathReferenceResolver.AcquireRoot(
                new UntrustedPathText(projectRoot));
        }

        await File.WriteAllTextAsync(
            crashArmFile,
            "armed",
            TestContext.Current.CancellationToken);

        try
        {
            _ = await receiver.ReceiveAsync(
                reference,
                TestContext.Current.CancellationToken);
        }
        catch (Exception exception) when (exception is
            ProjectOpenGatewayException or
            IOException or
            InvalidOperationException or
            OperationCanceledException)
        {
        }

        var newEnvironment = await harness.GetTestSessionAsync(
            TestContext.Current.CancellationToken);
        var restartedRuntime = await GetRuntimeProcessAsync(harness.BootstrapProcess.Id);
        Assert.NotEqual(originalRuntimeId, restartedRuntime.Id);
        Assert.NotEqual(
            environment["OPURE_RUNTIME_BOOT_ID"],
            newEnvironment["OPURE_RUNTIME_BOOT_ID"]);

        var endpoint = new RuntimeHealthEndpoint(
            newEnvironment["OPURE_IPC_PIPE"],
            newEnvironment["OPURE_RUNTIME_BOOT_ID"]);
        var session = new RuntimeHealthSessionMaterial(
            newEnvironment["OPURE_BOOTSTRAP_SESSION_ID"],
            newEnvironment["OPURE_BOOTSTRAP_SESSION_SECRET"]);
        var healthSource = RuntimeHealthGatewayClient.CreateProjectionSource(
            "1.0.0",
            DesktopSupervisorProjection.Disconnected,
            endpoint,
            session);
        var health = await healthSource.RefreshAsync(TestContext.Current.CancellationToken);
        Assert.Equal(DesktopRuntimeConnectionState.Connected, health.ConnectionState);

        var projectList = RuntimeHealthGatewayClient.CreateProjectListSource("Test");
        var projection = await projectList.RefreshAsync(TestContext.Current.CancellationToken);
        Assert.Contains(
            projection.Projects,
            project => project.DisplayName == Path.GetFileName(projectRoot));
    }
}
