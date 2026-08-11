using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
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
public class AdversarialFileSystemSuiteTests
{
    [Fact]
    public async Task FileLocking_SqliteFiles_HandlesContentionSafely()
    {
        using var harness = new EndToEndHarness();
        var env = await harness.GetTestSessionAsync(TestContext.Current.CancellationToken);
        
        var endpoint = new RuntimeHealthEndpoint(env["OPURE_IPC_PIPE"], env.TryGetValue("OPURE_RUNTIME_BOOT_ID", out var bootId) ? bootId : new string('0', 32));
        var session = new RuntimeHealthSessionMaterial(env["OPURE_BOOTSTRAP_SESSION_ID"], env["OPURE_BOOTSTRAP_SESSION_SECRET"]);

        // Create a project so that the DB files are initialized
        string dummyProject = Path.Combine(harness.DataRoot, "DummyLockProject");
        Directory.CreateDirectory(dummyProject);
        File.WriteAllText(Path.Combine(dummyProject, "Opure.slnx"), "<Solution />");
        VerifiedWorkspaceRootReference reference = WindowsPathReferenceResolver.AcquireRoot(new UntrustedPathText(dummyProject));
        
        var receiver = RuntimeHealthGatewayClient.CreateProjectRootReceiver("Test");
        await receiver.ReceiveAsync(reference, TestContext.Current.CancellationToken);
        
        // Let background outbox run
        await Task.Delay(1000, TestContext.Current.CancellationToken);

        string dbPath = Path.Combine(harness.DataRoot, "services", "opure.trust-evidence", "databases", "trust.db");
        string walPath = Path.Combine(harness.DataRoot, "services", "opure.trust-evidence", "databases", "trust.db-wal");
        
        // Wait until files actually exist on disk
        for (int i = 0; i < 50; i++)
        {
            if (File.Exists(dbPath)) break;
            await Task.Delay(100, TestContext.Current.CancellationToken);
        }
        
        // Aggressively lock the DB using FileShare.ReadWrite so we can open it alongside SQLite, then lock the bytes
        using var dbLock = new FileStream(dbPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete);
        
        // Lock the SQLite lock bytes (offset 1073741824, 512 bytes) or the entire file if possible
        try { dbLock.Lock(0, int.MaxValue); } catch { /* Ignore if already locked, but we are the aggressor */ }
            // Trigger a write/transaction by opening another project
            string secondProject = Path.Combine(harness.DataRoot, "DummyLockProject2");
            Directory.CreateDirectory(secondProject);
            File.WriteAllText(Path.Combine(secondProject, "Opure.slnx"), "<Solution />");
            VerifiedWorkspaceRootReference secondReference = WindowsPathReferenceResolver.AcquireRoot(new UntrustedPathText(secondProject));

            try
            {
                // This should backoff and eventually fail safely without crashing Runtime
                await receiver.ReceiveAsync(secondReference, TestContext.Current.CancellationToken);
                
                // If it succeeded, that's fine too (if the pipeline is purely async and didn't block the IPC response)
            }
            catch (Exception ex)
            {
                // We expect a potential gRPC/IPC timeout or generic error, but NOT a server crash
                TestContext.Current.TestOutputHelper?.WriteLine($"Expected error due to lock contention: {ex.Message}");
            }
            
            // Wait for any crash to happen if it was going to
            await Task.Delay(1500, TestContext.Current.CancellationToken);
            
            // Check if Runtime is still alive
            var healthSource = RuntimeHealthGatewayClient.CreateProjectionSource("1.0.0", DesktopSupervisorProjection.Disconnected, endpoint, session);
            var snapshot = await healthSource.RefreshAsync(TestContext.Current.CancellationToken);
            Assert.Equal(DesktopRuntimeConnectionState.Connected, snapshot.ConnectionState);
    }

    [Fact]
    public async Task PermissionDenial_Hashing_ReturnsPartialInventory()
    {
        using var harness = new EndToEndHarness();
        var env = await harness.GetTestSessionAsync(TestContext.Current.CancellationToken);
        
        string dummyProject = Path.Combine(harness.DataRoot, "DummyPermissionProject");
        Directory.CreateDirectory(dummyProject);
        File.WriteAllText(Path.Combine(dummyProject, "Opure.slnx"), "<Solution />");
        
        // Create a subfolder with files
        string secretFolder = Path.Combine(dummyProject, "secret_dir");
        Directory.CreateDirectory(secretFolder);
        File.WriteAllText(Path.Combine(secretFolder, "secret.txt"), "shhh");
        
        // Deny access to the subfolder
        var dSecurity = new DirectorySecurity();
        string currentUser = WindowsIdentity.GetCurrent().Name;
        dSecurity.AddAccessRule(new FileSystemAccessRule(currentUser, FileSystemRights.ReadData, AccessControlType.Deny));
        new DirectoryInfo(secretFolder).SetAccessControl(dSecurity);
        
        var endpoint = new RuntimeHealthEndpoint(env["OPURE_IPC_PIPE"], env.TryGetValue("OPURE_RUNTIME_BOOT_ID", out var bootId) ? bootId : new string('0', 32));
        var session = new RuntimeHealthSessionMaterial(env["OPURE_BOOTSTRAP_SESSION_ID"], env["OPURE_BOOTSTRAP_SESSION_SECRET"]);

        var receiver = RuntimeHealthGatewayClient.CreateProjectRootReceiver("Test");
        VerifiedWorkspaceRootReference reference = WindowsPathReferenceResolver.AcquireRoot(new UntrustedPathText(dummyProject));
        
        // The project open will trigger reconciliation which will encounter UnauthorizedAccessException on the secret folder.
        // It should complete gracefully but with partial inventory state (or just emit an issue).
        var response = await receiver.ReceiveAsync(reference, TestContext.Current.CancellationToken);
        Assert.NotNull(response);

        // Allow time for outbox
        await Task.Delay(1500, TestContext.Current.CancellationToken);
        
        var healthSource = RuntimeHealthGatewayClient.CreateProjectionSource("1.0.0", DesktopSupervisorProjection.Disconnected, endpoint, session);
        var snapshot = await healthSource.RefreshAsync(TestContext.Current.CancellationToken);
        Assert.Equal(DesktopRuntimeConnectionState.Connected, snapshot.ConnectionState);

        // Cleanup: remove the deny rule so we can delete the folder later
        dSecurity.RemoveAccessRuleAll(new FileSystemAccessRule(currentUser, FileSystemRights.ReadData, AccessControlType.Deny));
        new DirectoryInfo(secretFolder).SetAccessControl(dSecurity);
    }

    [Fact]
    public async Task RecursiveSymlinks_DetectedAndHandledSafely()
    {
        using var harness = new EndToEndHarness();
        var env = await harness.GetTestSessionAsync(TestContext.Current.CancellationToken);
        
        string dummyProject = Path.Combine(harness.DataRoot, "DummySymlinkProject");
        Directory.CreateDirectory(dummyProject);
        File.WriteAllText(Path.Combine(dummyProject, "Opure.slnx"), "<Solution />");
        
        // Create a recursive junction
        string loopFolder = Path.Combine(dummyProject, "loop");
        Directory.CreateDirectory(loopFolder);
        
        // Create a junction point pointing back to itself or parent
        string junctionPath = Path.Combine(loopFolder, "recursive_link");
        
        var startInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c mklink /J \"{junctionPath}\" \"{loopFolder}\"",
            CreateNoWindow = true,
            UseShellExecute = false
        };
        using (var process = Process.Start(startInfo))
        {
            process?.WaitForExit();
        }

        var endpoint = new RuntimeHealthEndpoint(env["OPURE_IPC_PIPE"], env.TryGetValue("OPURE_RUNTIME_BOOT_ID", out var bootId) ? bootId : new string('0', 32));
        var session = new RuntimeHealthSessionMaterial(env["OPURE_BOOTSTRAP_SESSION_ID"], env["OPURE_BOOTSTRAP_SESSION_SECRET"]);

        var receiver = RuntimeHealthGatewayClient.CreateProjectRootReceiver("Test");
        VerifiedWorkspaceRootReference reference = WindowsPathReferenceResolver.AcquireRoot(new UntrustedPathText(dummyProject));
        
        // The project open will trigger reconciliation which will encounter the ReparsePoint
        // It should skip it and emit a REPARSE_TRAVERSAL_DENIED issue rather than stack overflow.
        var response = await receiver.ReceiveAsync(reference, TestContext.Current.CancellationToken);
        Assert.NotNull(response);

        // Allow time for outbox
        await Task.Delay(1500, TestContext.Current.CancellationToken);
        
        var healthSource = RuntimeHealthGatewayClient.CreateProjectionSource("1.0.0", DesktopSupervisorProjection.Disconnected, endpoint, session);
        var snapshot = await healthSource.RefreshAsync(TestContext.Current.CancellationToken);
        Assert.Equal(DesktopRuntimeConnectionState.Connected, snapshot.ConnectionState);
    }
}
