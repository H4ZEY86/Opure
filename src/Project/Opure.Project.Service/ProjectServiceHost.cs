using System.Runtime.Versioning;
using Opure.Persistence.Sqlite;
using Opure.Project.Contracts;
using Opure.Project.Protocol;
using Opure.Project.Sqlite;
using Opure.TrustEvidence.Contracts;

namespace Opure.Project.Service;

[SupportedOSPlatform("windows")]
public sealed class ProjectServiceHost : IDisposable
{
    private readonly ProjectDatabase database;
    private readonly SqliteOutboxDispatcher trustReceiptDispatcher;
    private readonly ProjectTrustEvidencePublisher trustReceiptPublisher;
    private bool disposed;

    private ProjectServiceHost(
        ProjectDatabase database,
        ProjectOpenService openService,
        SqliteOutboxDispatcher trustReceiptDispatcher,
        ProjectTrustEvidencePublisher trustReceiptPublisher)
    {
        this.database = database;
        this.trustReceiptDispatcher = trustReceiptDispatcher;
        this.trustReceiptPublisher = trustReceiptPublisher;
        OpenHandler = new DispatchingProjectOpenHandler(
            openService,
            DispatchPendingTrustReceipts);
    }

    public IProjectOpenRequestHandler OpenHandler { get; }

    public static async Task<ProjectServiceHost> StartAsync(
        string channelDataRoot,
        string releaseChannel,
        ITrustEvidenceOwnerIngestionPort trustEvidenceIngestion,
        CancellationToken cancellationToken)
    {
        return await StartAsync(
            channelDataRoot,
            releaseChannel,
            trustEvidenceIngestion,
            new DeferredInitialWorkspaceSnapshotRequester(),
            timeProvider: null,
            cancellationToken).ConfigureAwait(false);
    }

    public static async Task<ProjectServiceHost> StartAsync(
        string channelDataRoot,
        string releaseChannel,
        ITrustEvidenceOwnerIngestionPort trustEvidenceIngestion,
        IInitialWorkspaceSnapshotRequester snapshotRequester,
        TimeProvider? timeProvider,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelDataRoot);
        ArgumentNullException.ThrowIfNull(trustEvidenceIngestion);
        ArgumentNullException.ThrowIfNull(snapshotRequester);
        ProjectReleaseChannel channel = ParseReleaseChannel(releaseChannel);
        ProjectDatabase database = ProjectDatabase.Open(
            channelDataRoot,
            cancellationToken);

        try
        {
            ProjectOpenService openService = new(
                database.CreateRepository(timeProvider),
                snapshotRequester);
            SqliteOutboxDispatcher dispatcher =
                database.CreateOutboxDispatcher(
                    new SqliteOutboxRetryPolicy(
                        maximumAttempts: 100,
                        initialDelay: TimeSpan.FromMilliseconds(250),
                        maximumDelay: TimeSpan.FromMinutes(1),
                        leaseDuration: TimeSpan.FromSeconds(5)),
                    timeProvider);
            ProjectTrustEvidencePublisher publisher = new(
                trustEvidenceIngestion);
            _ = await openService.ReconcileAsync(
                channel,
                cancellationToken).ConfigureAwait(false);
            ProjectServiceHost host = new(
                database,
                openService,
                dispatcher,
                publisher);
            _ = host.DispatchPendingTrustReceipts(
                cancellationToken: cancellationToken);
            return host;
        }
        catch
        {
            database.Dispose();
            throw;
        }
    }

    public ProjectTrustReceiptDispatchReport DispatchPendingTrustReceipts(
        int maximumMessages = 256,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (maximumMessages is < 1 or > 4096)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumMessages),
                maximumMessages,
                "A Trust receipt dispatch pass must be bounded between 1 and 4,096 messages.");
        }

        int delivered = 0;
        int retryScheduled = 0;
        int blocked = 0;
        bool limitReached = false;

        for (int index = 0; index < maximumMessages; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SqliteOutboxDispatchResult result;

            try
            {
                result = trustReceiptDispatcher.DispatchNextOfType(
                    EvidenceIngestionRequest.MessageType,
                    trustReceiptPublisher,
                    cancellationToken);
            }
            catch (SqlitePersistenceException exception) when (
                exception.ErrorCode ==
                    SqlitePersistenceErrorCodes.OutboxPublishFailed)
            {
                retryScheduled++;
                break;
            }

            switch (result.Outcome)
            {
                case SqliteOutboxDispatchOutcome.NoMessage:
                    return CreateDispatchReport(
                        delivered,
                        retryScheduled,
                        blocked,
                        limitReached: false,
                        cancellationToken);
                case SqliteOutboxDispatchOutcome.Delivered:
                    delivered++;
                    break;
                case SqliteOutboxDispatchOutcome.RetryScheduled:
                    retryScheduled++;
                    return CreateDispatchReport(
                        delivered,
                        retryScheduled,
                        blocked,
                        limitReached: false,
                        cancellationToken);
                case SqliteOutboxDispatchOutcome.Blocked:
                    blocked++;
                    return CreateDispatchReport(
                        delivered,
                        retryScheduled,
                        blocked,
                        limitReached: false,
                        cancellationToken);
                default:
                    throw new InvalidOperationException(
                        "The Project Trust receipt dispatcher returned an unsupported outcome.");
            }
        }

        limitReached = true;
        return CreateDispatchReport(
            delivered,
            retryScheduled,
            blocked,
            limitReached,
            cancellationToken);
    }

    public SqliteOutboxBacklogHealth ReadTrustReceiptBacklog(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return trustReceiptDispatcher.ReadBacklogHealthOfType(
            EvidenceIngestionRequest.MessageType,
            cancellationToken);
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

    private ProjectTrustReceiptDispatchReport CreateDispatchReport(
        int delivered,
        int retryScheduled,
        int blocked,
        bool limitReached,
        CancellationToken cancellationToken)
    {
        return new ProjectTrustReceiptDispatchReport(
            delivered,
            retryScheduled,
            blocked,
            limitReached,
            ReadTrustReceiptBacklog(cancellationToken));
    }

    private sealed class DispatchingProjectOpenHandler :
        IProjectOpenRequestHandler
    {
        private readonly IProjectOpenRequestHandler inner;
        private readonly Func<
            int,
            CancellationToken,
            ProjectTrustReceiptDispatchReport> dispatch;

        internal DispatchingProjectOpenHandler(
            IProjectOpenRequestHandler inner,
            Func<
                int,
                CancellationToken,
                ProjectTrustReceiptDispatchReport> dispatch)
        {
            this.inner = inner;
            this.dispatch = dispatch;
        }

        public async Task<Protocol.Open.V1.OpenProjectResponse> HandleAsync(
            Protocol.Open.V1.OpenProjectRequest request,
            CancellationToken cancellationToken)
        {
            Protocol.Open.V1.OpenProjectResponse response =
                await inner.HandleAsync(
                    request,
                    cancellationToken).ConfigureAwait(false);
            using CancellationTokenSource projectionDeadline =
                new(TimeSpan.FromSeconds(2));

            try
            {
                _ = dispatch(256, projectionDeadline.Token);
            }
            catch (OperationCanceledException) when (
                projectionDeadline.IsCancellationRequested)
            {
                // The authoritative Project transaction has committed. Its
                // receipt remains pending for a later bounded dispatch pass.
            }

            return response;
        }
    }
}
