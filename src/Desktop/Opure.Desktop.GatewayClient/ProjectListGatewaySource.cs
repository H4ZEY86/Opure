using System.Globalization;
using Opure.Desktop.Contracts;
using Opure.Ipc.Abstractions;
using Opure.Ipc.NamedPipes.Windows;
using Opure.Project.Protocol;
using Opure.Project.Protocol.List.V1;

namespace Opure.Desktop.GatewayClient;

internal sealed class ProjectListGatewaySource(string releaseChannel) : IDesktopProjectListSource
{
    private readonly ProjectListReleaseChannel releaseChannel = Parse(releaseChannel);

    public async Task<DesktopProjectListProjection> RefreshAsync(CancellationToken cancellationToken)
    {
        await using NamedPipeProjectListClient client = CreateClient();
        ListProjectsResponse response = await client.ListAsync(
            new ListProjectsRequest
            {
                MinimumContractRevision = ProjectListContractPolicy.CurrentRevision,
                MaximumContractRevision = ProjectListContractPolicy.CurrentRevision,
                CorrelationId = CreateIdentity(),
                ReleaseChannel = releaseChannel
            },
            cancellationToken).ConfigureAwait(false);
        if (response.Error is not null)
        {
            throw new ProjectListTransportException(response.Error.Code, response.Error.SafeMessage, response.Error.Retryable);
        }

        IReadOnlyList<DesktopProjectListItem> projects = response.Projects
            .Select(ToDesktop)
            .ToArray();
        return new DesktopProjectListProjection(
            projects,
            DateTimeOffset.FromUnixTimeMilliseconds(response.GeneratedUnixTimeMilliseconds));
    }

    public Task<DesktopProjectCommandResult> OpenAsync(string projectId, CancellationToken cancellationToken) =>
        ExecuteAsync(projectId, remove: false, cancellationToken);

    public Task<DesktopProjectCommandResult> RemoveRegistrationAsync(string projectId, CancellationToken cancellationToken) =>
        ExecuteAsync(projectId, remove: true, cancellationToken);

    private async Task<DesktopProjectCommandResult> ExecuteAsync(
        string projectId,
        bool remove,
        CancellationToken cancellationToken)
    {
        await using NamedPipeProjectListClient client = CreateClient();
        ProjectListCommandRequest request = new()
        {
            MinimumContractRevision = ProjectListContractPolicy.CurrentRevision,
            MaximumContractRevision = ProjectListContractPolicy.CurrentRevision,
            OperationId = CreateIdentity(),
            CorrelationId = CreateIdentity(),
            ReleaseChannel = releaseChannel,
            ProjectId = projectId
        };
        ProjectListCommandResponse response = remove
            ? await client.RemoveAsync(request, cancellationToken).ConfigureAwait(false)
            : await client.OpenAsync(request, cancellationToken).ConfigureAwait(false);
        return response.OutcomeCase == ProjectListCommandResponse.OutcomeOneofCase.Project
            ? new DesktopProjectCommandResult(true, response.Project.SafeDetail)
            : new DesktopProjectCommandResult(false, response.Error.SafeMessage);
    }

    private static DesktopProjectListItem ToDesktop(ProjectListItem item)
    {
        DesktopProjectAvailability availability = item.Availability switch
        {
            ProjectAvailability.Available => DesktopProjectAvailability.Available,
            ProjectAvailability.Unavailable => DesktopProjectAvailability.Unavailable,
            ProjectAvailability.ReviewRequired => DesktopProjectAvailability.ReviewRequired,
            _ => throw new InvalidOperationException("Project Service returned an unsupported availability.")
        };
        return new DesktopProjectListItem(
            item.ProjectId,
            item.DisplayName,
            item.SafeLocationSummary,
            item.RepositoryClass,
            item.LastOpenedUnixTimeMilliseconds == 0
                ? "Never opened"
                : DateTimeOffset.FromUnixTimeMilliseconds(item.LastOpenedUnixTimeMilliseconds)
                    .ToLocalTime().ToString("g", CultureInfo.CurrentCulture),
            availability,
            availability switch
            {
                DesktopProjectAvailability.Available => "Available",
                DesktopProjectAvailability.Unavailable => "Unavailable",
                DesktopProjectAvailability.ReviewRequired => "Review required",
                _ => throw new InvalidOperationException("Unsupported project availability.")
            },
            item.AccessibilityLabel);
    }

    private static NamedPipeProjectListClient CreateClient()
    {
        RuntimeHealthEndpoint endpoint = RuntimeHealthEndpointEnvironment.ReadCurrent() ??
            throw new InvalidOperationException("The Runtime endpoint is unavailable.");
        RuntimeHealthSessionMaterial material = RuntimeHealthSessionEnvironment.ReadCurrent() ??
            throw new InvalidOperationException("The Runtime session is unavailable.");
        return new NamedPipeProjectListClient(endpoint, material);
    }

    private static string CreateIdentity() => Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

    private static ProjectListReleaseChannel Parse(string channel) => channel switch
    {
        "Development" => ProjectListReleaseChannel.Development,
        "Preview" => ProjectListReleaseChannel.Preview,
        "Stable" => ProjectListReleaseChannel.Stable,
        "Test" => ProjectListReleaseChannel.Test,
        _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, "The release channel is unsupported.")
    };
}
