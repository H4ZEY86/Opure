using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text.Json;
using Opure.Ipc.Abstractions;
using Opure.Ipc.NamedPipes.Windows;
using Opure.Recovery.Contracts;
using Opure.Recovery.Protocol;
using Opure.Recovery.Protocol.Point.V1;
using Opure.Recovery.Service;
using Opure.Project.Sqlite;
using Opure.Runtime.Contracts;
using Opure.Runtime.Contracts.Health.V1;
using Opure.Runtime.Handlers;
using Opure.TrustEvidence.Sqlite;
using Xunit;

namespace Opure.EndToEnd.Tests;

[SupportedOSPlatform("windows")]
public sealed class RecoveryPointCliPipelineTests : IDisposable
{
    private readonly string testRoot = Path.Combine(
        Path.GetTempPath(),
        "Opure.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task RecoveryCommandsUseAuthenticatedNamedPipeGrpcPipeline()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        RuntimeHealthEndpoint endpoint = NamedPipeRuntimeHealthEndpoint.Create(
            "Test",
            Guid.NewGuid().ToString("N"));
        RuntimeHealthSessionMaterial material = RuntimeHealthSessionMaterial.Create();
        string dataRoot = Path.Combine(testRoot, "data");
        string recoveryRoot = Path.Combine(testRoot, "recovery");
        using ProjectDatabase projectDatabase = ProjectDatabase.Open(
            dataRoot,
            cancellationToken);
        using TrustEvidenceDatabase trustDatabase = TrustEvidenceDatabase.Open(
            dataRoot,
            cancellationToken);
        LocalRecoveryPointService recoveryService = new(
            [
                trustDatabase.CreateBackupAdapter(),
                projectDatabase.CreateBackupAdapter()
            ],
            "1.0.0-test");
        RecoveryPointRequestHandler recoveryHandler = new(
            recoveryService,
            recoveryRoot,
            "Development");
        CreateRecoveryPointResponseMessage mismatchedChannel =
            await recoveryHandler.CreateRecoveryPointAsync(
                new CreateRecoveryPointRequestMessage
                {
                    ContractRevision = RecoveryPointContractPolicy.CurrentRevision,
                    ReleaseChannel = "Stable"
                },
                cancellationToken);
        Assert.False(mismatchedChannel.IsSuccess);
        Assert.Contains("does not match this Runtime channel", mismatchedChannel.ErrorMessage);
        Assert.False(Directory.Exists(recoveryRoot));

        await using NamedPipeGatewayServer server = await NamedPipeGatewayServer.StartAsync(
            endpoint,
            new StaticHealthHandler(endpoint.RuntimeBootId),
            new RuntimeHealthSessionPolicy(
                material,
                DateTimeOffset.UtcNow.AddMinutes(5)),
            cancellationToken,
            recoveryPointRequestHandler: recoveryHandler);

        CliResult create = await RunCliAsync(
            endpoint,
            material,
            "recovery create --channel Development",
            cancellationToken);
        Assert.True(
            create.ExitCode == 0,
            $"CLI create failed. Output: {create.StandardOutput} Error: {create.StandardError}");
        Assert.Contains("Same-device recovery only", create.StandardOutput);

        const string prefix = "Created local recovery point ";
        string idText = create.StandardOutput
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Single(line => line.StartsWith(prefix, StringComparison.Ordinal))[prefix.Length..]
            .TrimEnd('.');
        Assert.True(Guid.TryParse(idText, out Guid recoveryPointId));

        string manifestPath = Path.Combine(
            recoveryRoot,
            recoveryPointId.ToString("N"),
            "manifest.json");
        Assert.True(File.Exists(manifestPath));
        using JsonDocument manifest = JsonDocument.Parse(
            await File.ReadAllTextAsync(manifestPath, cancellationToken));
        JsonElement root = manifest.RootElement;
        Assert.Equal("same-device", root.GetProperty("ScopeClass").GetString());
        Assert.Equal("Development", root.GetProperty("Channel").GetString());
        Assert.Equal("1.0.0-test", root.GetProperty("ProductVersion").GetString());
        Assert.Equal((int)VerificationLevel.Structural, root.GetProperty("VerificationLevel").GetInt32());
        Assert.NotEmpty(root.GetProperty("CheckpointHashes").EnumerateArray());
        Assert.Equal(2, root.GetProperty("VerificationReceipts").GetArrayLength());
        Assert.Equal(2, root.GetProperty("Owners").EnumerateObject().Count());
        Assert.Equal(2, root.GetProperty("SupportedSchemas").GetArrayLength());
        Assert.True(File.Exists(Path.Combine(
            recoveryRoot,
            recoveryPointId.ToString("N"),
            ".commit")));

        RuntimeHealthEndpoint listEndpoint = NamedPipeRuntimeHealthEndpoint.Create(
            "Test",
            Guid.NewGuid().ToString("N"));
        RuntimeHealthSessionMaterial listMaterial = RuntimeHealthSessionMaterial.Create();
        await using NamedPipeGatewayServer listServer = await NamedPipeGatewayServer.StartAsync(
            listEndpoint,
            new StaticHealthHandler(listEndpoint.RuntimeBootId),
            new RuntimeHealthSessionPolicy(
                listMaterial,
                DateTimeOffset.UtcNow.AddMinutes(5)),
            cancellationToken,
            recoveryPointRequestHandler: recoveryHandler);
        CliResult list = await RunCliAsync(
            listEndpoint,
            listMaterial,
            "recovery list --channel Development",
            cancellationToken);
        Assert.True(
            list.ExitCode == 0,
            $"CLI list failed. Output: {list.StandardOutput} Error: {list.StandardError}");
        Assert.Contains(recoveryPointId.ToString("N"), list.StandardOutput);
        Assert.Contains("Structural", list.StandardOutput);

        RuntimeHealthEndpoint showEndpoint = NamedPipeRuntimeHealthEndpoint.Create(
            "Test",
            Guid.NewGuid().ToString("N"));
        RuntimeHealthSessionMaterial showMaterial = RuntimeHealthSessionMaterial.Create();
        await using NamedPipeGatewayServer showServer = await NamedPipeGatewayServer.StartAsync(
            showEndpoint,
            new StaticHealthHandler(showEndpoint.RuntimeBootId),
            new RuntimeHealthSessionPolicy(
                showMaterial,
                DateTimeOffset.UtcNow.AddMinutes(5)),
            cancellationToken,
            recoveryPointRequestHandler: recoveryHandler);
        CliResult show = await RunCliAsync(
            showEndpoint,
            showMaterial,
            $"recovery show --id {recoveryPointId:D} --channel Development",
            cancellationToken);
        Assert.True(
            show.ExitCode == 0,
            $"CLI show failed. Output: {show.StandardOutput} Error: {show.StandardError}");
        Assert.Contains(recoveryPointId.ToString("D"), show.StandardOutput);
        Assert.Contains("Structural verification: Structural", show.StandardOutput);
        Assert.Contains("Required owners: 2", show.StandardOutput);
        Assert.Contains("backup.recovery-point-created", show.StandardOutput);
        Assert.Contains("backup.verification-completed", show.StandardOutput);
    }

    public void Dispose()
    {
        if (Directory.Exists(testRoot))
        {
            Directory.Delete(testRoot, recursive: true);
        }
    }

    private static async Task<CliResult> RunCliAsync(
        RuntimeHealthEndpoint endpoint,
        RuntimeHealthSessionMaterial material,
        string arguments,
        CancellationToken cancellationToken)
    {
        string repositoryRoot = FindRepositoryRoot();
#if RELEASE
        const string configuration = "release";
#else
        const string configuration = "debug";
#endif
        string executablePath = Path.Combine(
            repositoryRoot,
            "artifacts",
            "bin",
            "Opure.Cli",
            configuration,
            "Opure.Cli.exe");
        ProcessStartInfo startInfo = new()
        {
            FileName = executablePath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.Environment["OPURE_RUNTIME_PIPE_NAME"] = endpoint.PipeName;
        startInfo.Environment["OPURE_RUNTIME_BOOT_ID"] = endpoint.RuntimeBootId;
        startInfo.Environment["OPURE_BOOTSTRAP_SESSION_ID"] = material.SessionId;
        startInfo.Environment["OPURE_BOOTSTRAP_SESSION_SECRET"] = material.SessionSecret;

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("The Opure CLI process could not be started.");
        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
        Task<string> standardError = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        return new CliResult(
            process.ExitCode,
            await standardOutput,
            await standardError);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Opure.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("The repository root could not be located.");
    }

    private sealed record CliResult(
        int ExitCode,
        string StandardOutput,
        string StandardError);

    private sealed class StaticHealthHandler(string bootId) : IRuntimeHealthRequestHandler
    {
        public Task<GetRuntimeHealthResponse> HandleAsync(
            GetRuntimeHealthRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new GetRuntimeHealthResponse
            {
                ContractRevision = RuntimeHealthContractPolicy.CurrentRevision,
                Health = new RuntimeHealthProjection
                {
                    RuntimeBootId = bootId,
                    ProductVersion = "1.0.0-test",
                    RuntimeMode = RuntimeMode.Normal,
                    Readiness = RuntimeReadiness.Ready,
                    OverallHealth = RuntimeHealthState.Healthy,
                    GeneratedUnixTimeMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                }
            });
        }
    }
}
