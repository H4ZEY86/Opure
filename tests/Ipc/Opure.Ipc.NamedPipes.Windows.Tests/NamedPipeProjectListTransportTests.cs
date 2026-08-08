using System.Runtime.Versioning;
using Opure.Ipc.Abstractions;
using Opure.Ipc.NamedPipes.Windows;
using Opure.Project.Protocol;
using Opure.Project.Protocol.List.V1;
using Opure.Runtime.Contracts;
using Opure.Runtime.Contracts.Health.V1;
using Xunit;

namespace Opure.Ipc.NamedPipes.Windows.Tests;

[SupportedOSPlatform("windows")]
public sealed class NamedPipeProjectListTransportTests
{
    [Fact]
    public async Task AuthenticatedProjectListRoundTripUsesRuntimePipe()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        RuntimeHealthEndpoint endpoint = NamedPipeRuntimeHealthEndpoint.Create(
            "Development",
            Guid.NewGuid().ToString("N"));
        RuntimeHealthSessionMaterial material = RuntimeHealthSessionMaterial.Create();
        StaticProjectListHandler handler = new();
        await using NamedPipeGatewayServer server = await NamedPipeGatewayServer.StartAsync(
            endpoint,
            new StaticHealthHandler(endpoint.RuntimeBootId),
            new RuntimeHealthSessionPolicy(material, DateTimeOffset.UtcNow.AddMinutes(5)),
            cancellationToken,
            projectListRequestHandler: handler);
        await using NamedPipeProjectListClient client = new(endpoint, material);

        ListProjectsResponse response = await client.ListAsync(
            new ListProjectsRequest
            {
                MinimumContractRevision = ProjectListContractPolicy.CurrentRevision,
                MaximumContractRevision = ProjectListContractPolicy.CurrentRevision,
                CorrelationId = "0123456789abcdef0123456789abcdef",
                ReleaseChannel = ProjectListReleaseChannel.Development
            },
            cancellationToken);

        ProjectListItem project = Assert.Single(response.Projects);
        Assert.Equal("Fixture project", project.DisplayName);
        Assert.Equal(1, handler.ListCount);
    }

    private sealed class StaticProjectListHandler : IProjectListRequestHandler
    {
        public int ListCount { get; private set; }

        public Task<ListProjectsResponse> ListAsync(ListProjectsRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ListCount++;
            ListProjectsResponse response = new()
            {
                ContractRevision = ProjectListContractPolicy.CurrentRevision,
                GeneratedUnixTimeMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            response.Projects.Add(new ProjectListItem
            {
                ProjectId = "abcdef0123456789abcdef0123456789",
                DisplayName = "Fixture project",
                SafeLocationSummary = "Fixed local storage",
                RepositoryClass = "Git repository",
                LastOpenedUnixTimeMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Availability = ProjectAvailability.Available,
                LifecycleLabel = "Open",
                AccessibilityLabel = "Fixture project, Available, Git repository, Fixed local storage"
            });
            return Task.FromResult(response);
        }

        public Task<ProjectListCommandResponse> OpenAsync(ProjectListCommandRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(CreateCommandResponse(request, ProjectListCommandDisposition.Opened));

        public Task<ProjectListCommandResponse> RemoveAsync(ProjectListCommandRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(CreateCommandResponse(request, ProjectListCommandDisposition.RegistrationRemoved));

        private static ProjectListCommandResponse CreateCommandResponse(
            ProjectListCommandRequest request,
            ProjectListCommandDisposition disposition) => new()
            {
                ContractRevision = ProjectListContractPolicy.CurrentRevision,
                Project = new ProjectListCommandSummary
                {
                    OperationId = request.OperationId,
                    ProjectId = request.ProjectId,
                    Disposition = disposition,
                    SafeDetail = "Completed."
                }
            };
    }

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
