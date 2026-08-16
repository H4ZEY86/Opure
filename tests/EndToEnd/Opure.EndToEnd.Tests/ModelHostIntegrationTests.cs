using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Opure.Runtime.Contracts.Models;
using Opure.Runtime.Models;
using Opure.Workspace.Contracts.Models;
using Opure.Workspace.Execution;
using Opure.Workspace.Execution.Models;
using Xunit;

namespace Opure.EndToEnd.Tests;

public class ModelHostIntegrationTests
{
    [Fact]
    public async Task ModelHostRunner_FullLifecycle_SpawnsIsolatesAndRoutes()
    {
        // Arrange
        var scriptPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mock_model.cmd");
        System.IO.File.WriteAllText(scriptPath, "@echo off\nset /p dummy=\necho Chunk1\necho Chunk2\n");

        var manifestHash = new byte[32];
        Array.Fill(manifestHash, (byte)0x42);
        string hashStr = Convert.ToHexString(manifestHash);

        var manifestStoreFake = new FakeModelManifestStore(new ModelHostManifest
        {
            ModelPath = scriptPath,
            RequiredSha256 = manifestHash,
            ManifestRevision = 1,
            ManifestHash = hashStr,
            ModelArchitecture = "test",
            LayerCount = 1,
            TotalWeightBytes = 100
        });

        var launcher = new ModelHostProcessLauncher();
        var router = new ModelRequestRouter();
        var builder = new OllamaCommandBuilder();
        var runner = new ModelHostRunner(manifestStoreFake, launcher, router, builder);

        var request = ModelRequest.FromPrompt("Dummy prompt");
        
        // Act
        var tokens = string.Empty;
        await foreach (var chunk in runner.RunModelAsync("workspace-123", hashStr, request, TestContext.Current.CancellationToken))
        {
            tokens += chunk;
        }

        // Assert
        Assert.Contains("Chunk1", tokens);
        Assert.Contains("Chunk2", tokens);
    }

    [Fact]
    public async Task JobObject_KillsChildProcess_WhenHandleClosed()
    {
        // This test proves that the OS will terminate the child process when the job object is disposed.
        // We will spawn a process directly in a WindowsJobObject, close it, and ensure it dies.
        
        var jobObject = new WindowsJobObject();
        var startInfo = new ProcessStartInfo
        {
            FileName = "ping.exe",
            Arguments = "-n 30 127.0.0.1",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true
        };
        var process = new Process { StartInfo = startInfo };
        Assert.True(process.Start());
        
        jobObject.AssignProcess(process);
        
        // Act
        jobObject.Dispose();
        
        // Assert
        // We wait a brief moment for the OS to kill it
        process.WaitForExit(2000);
        Assert.True(process.HasExited, "The child process should have been killed when the job object was closed.");
    }

    private sealed class FakeModelManifestStore : IModelManifestStore
    {
        private readonly ModelHostManifest _manifest;

        public FakeModelManifestStore(ModelHostManifest manifest)
        {
            _manifest = manifest;
        }

        public Task<ModelHostManifest?> GetManifestAsync(string modelPath, CancellationToken cancellationToken = default) => Task.FromResult<ModelHostManifest?>(_manifest);

        public Task<ModelHostManifest?> GetManifestForHashAsync(byte[] sha256Hash, CancellationToken cancellationToken = default) => Task.FromResult<ModelHostManifest?>(_manifest);

        public Task StoreManifestAsync(ModelHostManifest manifest, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task RecordValidationAsync(string modelPath, byte[] computedHash, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task RecordImportAsync(ModelHostManifest manifest, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<ModelHostManifest>> ListManifestsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ModelHostManifest>>(new[] { _manifest });
    }
}
