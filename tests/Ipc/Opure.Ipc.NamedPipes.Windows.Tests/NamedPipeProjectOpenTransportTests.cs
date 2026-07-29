using System.Runtime.Versioning;
using Opure.Desktop.GatewayClient;
using Opure.Filesystem.Contracts;
using Opure.Filesystem.Windows;
using Opure.Ipc.Abstractions;
using Opure.Ipc.NamedPipes.Windows;
using Opure.Project.Protocol;
using Opure.Project.Protocol.Open.V1;
using Opure.Runtime.Contracts;
using Opure.Runtime.Contracts.Health.V1;
using Xunit;
using WireIdentityCapability =
    Opure.Project.Protocol.Open.V1.FileIdentityCapability;
using WireVolumeClass =
    Opure.Project.Protocol.Open.V1.FilesystemVolumeClass;

namespace Opure.Ipc.NamedPipes.Windows.Tests;

[SupportedOSPlatform("windows")]
public sealed class NamedPipeProjectOpenTransportTests
{
    [Fact]
    public async Task AuthenticatedOpenProjectRoundTripUsesExistingRuntimePipe()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        RuntimeHealthEndpoint endpoint = CreateEndpoint();
        RuntimeHealthSessionMaterial material =
            RuntimeHealthSessionMaterial.Create();
        OpenProjectResponse expected = CreateResponse();

        await using NamedPipeRuntimeHealthServer server =
            await NamedPipeRuntimeHealthServer.StartAsync(
                endpoint,
                new StaticHealthHandler(endpoint.RuntimeBootId),
                CreatePolicy(material),
                cancellationToken,
                projectOpenRequestHandler:
                    new StaticProjectOpenHandler(expected));
        await using NamedPipeProjectOpenClient client = new(
            endpoint,
            material);

        OpenProjectResponse response = await client.OpenProjectAsync(
            CreateRequest(),
            ProjectOpenContractPolicy.DefaultDeadline,
            cancellationToken);

        Assert.Equal(expected, response);
        Assert.True(
            ProjectOpenContractPolicy.ValidateResponse(response).IsValid);
    }

    [Fact]
    public async Task WrongSessionMaterialIsDeniedBeforeProjectHandler()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        RuntimeHealthEndpoint endpoint = CreateEndpoint();
        RuntimeHealthSessionMaterial accepted =
            RuntimeHealthSessionMaterial.Create();
        CountingProjectOpenHandler handler = new();

        await using NamedPipeRuntimeHealthServer server =
            await NamedPipeRuntimeHealthServer.StartAsync(
                endpoint,
                new StaticHealthHandler(endpoint.RuntimeBootId),
                CreatePolicy(accepted),
                cancellationToken,
                projectOpenRequestHandler: handler);
        await using NamedPipeProjectOpenClient client = new(
            endpoint,
            RuntimeHealthSessionMaterial.Create());

        ProjectOpenTransportException exception =
            await Assert.ThrowsAsync<ProjectOpenTransportException>(
                () => client.OpenProjectAsync(
                    CreateRequest(),
                    ProjectOpenContractPolicy.DefaultDeadline,
                    cancellationToken));

        Assert.Equal(
            ProjectOpenTransportErrorCodes.SessionDenied,
            exception.ErrorCode);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task DesktopGatewayTransfersIdentityClaimAndReceivesSafeReceipt()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        RuntimeHealthEndpoint endpoint = CreateEndpoint();
        RuntimeHealthSessionMaterial material =
            RuntimeHealthSessionMaterial.Create();
        CapturingProjectOpenHandler handler = new();
        string rootPath = Directory.CreateDirectory(Path.Combine(
            Path.GetTempPath(),
            "Opure.ProjectGateway.Tests",
            Guid.NewGuid().ToString("N"))).FullName;

        try
        {
            await using NamedPipeRuntimeHealthServer server =
                await NamedPipeRuntimeHealthServer.StartAsync(
                    endpoint,
                    new StaticHealthHandler(endpoint.RuntimeBootId),
                    CreatePolicy(material),
                    cancellationToken,
                    projectOpenRequestHandler: handler);
            ProjectOpenGatewayReceiver receiver = new(
                endpoint,
                material,
                "Development");
            VerifiedWorkspaceRootReference root =
                WindowsPathReferenceResolver.AcquireRoot(
                    new UntrustedPathText(rootPath));

            VerifiedWorkspaceRootTransferReceipt receipt =
                await receiver.ReceiveAsync(root, cancellationToken);

            Assert.Equal("PROJECT_OPEN", receipt.StableCode);
            Assert.Equal("Open", receipt.AuthoritativeState);
            Assert.Equal(1, handler.RequestCount);
            Assert.Equal(
                root.RootIdentity.FileId,
                handler.LastRequest?.Root.FileId);
            Assert.Equal(
                root.RootIdentity.VolumeSerialNumber,
                handler.LastRequest?.Root.VolumeSerialNumber);
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    private static RuntimeHealthEndpoint CreateEndpoint()
    {
        return NamedPipeRuntimeHealthEndpoint.Create(
            "Development",
            Guid.NewGuid().ToString("N"));
    }

    private static RuntimeHealthSessionPolicy CreatePolicy(
        RuntimeHealthSessionMaterial material)
    {
        return new RuntimeHealthSessionPolicy(
            material,
            DateTimeOffset.UtcNow.AddMinutes(5));
    }

    private static OpenProjectRequest CreateRequest()
    {
        return new OpenProjectRequest
        {
            MinimumContractRevision =
                ProjectOpenContractPolicy.CurrentRevision,
            MaximumContractRevision =
                ProjectOpenContractPolicy.CurrentRevision,
            OperationId = "0123456789abcdef0123456789abcdef",
            CorrelationId = "abcdef0123456789abcdef0123456789",
            ReleaseChannel = ProjectReleaseChannel.Development,
            DisplayName = "Fixture project",
            Root = new ProjectRootIdentityClaim
            {
                DisplayPath = @"C:\fixture\project",
                VolumeClass = WireVolumeClass.FixedLocal,
                VolumeSerialNumber = 42,
                FileId = "00112233445566778899aabbccddeeff",
                IdentityCapability =
                    WireIdentityCapability.WindowsFileId128
            }
        };
    }

    private static OpenProjectResponse CreateResponse()
    {
        return new OpenProjectResponse
        {
            ContractRevision = ProjectOpenContractPolicy.CurrentRevision,
            Project = new OpenProjectSummary
            {
                OperationId =
                    "0123456789abcdef0123456789abcdef",
                ProjectId =
                    "fedcba9876543210fedcba9876543210",
                DisplayName = "Fixture project",
                ReleaseChannel = ProjectReleaseChannel.Development,
                Disposition = ProjectOpenDisposition.Created,
                LifecycleState = ProjectLifecycleState.Open,
                RootVolumeClass = WireVolumeClass.FixedLocal,
                InitialSnapshotState =
                    InitialWorkspaceSnapshotState.Ready,
                SafeDetail =
                    "The initial Workspace Snapshot is ready."
            }
        };
    }

    private sealed class StaticProjectOpenHandler(
        OpenProjectResponse response) :
        IProjectOpenRequestHandler
    {
        public Task<OpenProjectResponse> HandleAsync(
            OpenProjectRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(response);
        }
    }

    private sealed class CountingProjectOpenHandler :
        IProjectOpenRequestHandler
    {
        public int RequestCount { get; private set; }

        public Task<OpenProjectResponse> HandleAsync(
            OpenProjectRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();
            RequestCount++;
            return Task.FromResult(CreateResponse());
        }
    }

    private sealed class CapturingProjectOpenHandler :
        IProjectOpenRequestHandler
    {
        public int RequestCount { get; private set; }

        public OpenProjectRequest? LastRequest { get; private set; }

        public Task<OpenProjectResponse> HandleAsync(
            OpenProjectRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();
            RequestCount++;
            LastRequest = request.Clone();
            OpenProjectResponse response = CreateResponse();
            response.Project.OperationId = request.OperationId;
            response.Project.DisplayName = request.DisplayName;
            return Task.FromResult(response);
        }
    }

    private sealed class StaticHealthHandler(string bootId) :
        IRuntimeHealthRequestHandler
    {
        public Task<GetRuntimeHealthResponse> HandleAsync(
            GetRuntimeHealthRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new GetRuntimeHealthResponse
            {
                ContractRevision =
                    RuntimeHealthContractPolicy.CurrentRevision,
                Health = new RuntimeHealthProjection
                {
                    RuntimeBootId = bootId,
                    ProductVersion = "1.0.0-test",
                    RuntimeMode = RuntimeMode.Normal,
                    Readiness = RuntimeReadiness.Ready,
                    OverallHealth = RuntimeHealthState.Healthy,
                    GeneratedUnixTimeMilliseconds =
                        DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                }
            });
        }
    }
}
