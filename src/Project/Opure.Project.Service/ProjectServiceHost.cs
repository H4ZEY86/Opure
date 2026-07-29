using System.Runtime.Versioning;
using Opure.Project.Contracts;
using Opure.Project.Protocol;
using Opure.Project.Sqlite;

namespace Opure.Project.Service;

[SupportedOSPlatform("windows")]
public sealed class ProjectServiceHost : IDisposable
{
    private readonly ProjectDatabase database;
    private bool disposed;

    private ProjectServiceHost(
        ProjectDatabase database,
        ProjectOpenService openService)
    {
        this.database = database;
        OpenHandler = openService;
    }

    public IProjectOpenRequestHandler OpenHandler { get; }

    public static async Task<ProjectServiceHost> StartAsync(
        string channelDataRoot,
        string releaseChannel,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelDataRoot);
        ProjectReleaseChannel channel = ParseReleaseChannel(releaseChannel);
        ProjectDatabase database = ProjectDatabase.Open(
            channelDataRoot,
            cancellationToken);

        try
        {
            ProjectOpenService openService = new(
                database.CreateRepository(),
                new DeferredInitialWorkspaceSnapshotRequester());
            _ = await openService.ReconcileAsync(
                channel,
                cancellationToken).ConfigureAwait(false);
            return new ProjectServiceHost(database, openService);
        }
        catch
        {
            database.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        database.Dispose();
    }

    private static ProjectReleaseChannel ParseReleaseChannel(string value)
    {
        return value switch
        {
            "Development" => ProjectReleaseChannel.Development,
            "Preview" => ProjectReleaseChannel.Preview,
            "Stable" => ProjectReleaseChannel.Stable,
            _ => throw new ArgumentException(
                "The Project Service release channel is unsupported.",
                nameof(value))
        };
    }
}
