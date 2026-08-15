using Opure.Persistence.Sqlite;
using Opure.TrustEvidence.Contracts;
using Opure.Patch.Sqlite;

namespace Opure.Patch.Service;

public sealed class PatchTrustReceiptDispatchService
{
    private readonly SqliteOutboxDispatcher dispatcher;
    private readonly PatchTrustEvidencePublisher publisher;

    public PatchTrustReceiptDispatchService(
        PatchDatabase database,
        ITrustEvidenceOwnerIngestionPort ingestion,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(database);
        dispatcher = database.CreateOutboxDispatcher(
            new SqliteOutboxRetryPolicy(
                maximumAttempts: 100,
                initialDelay: TimeSpan.FromMilliseconds(250),
                maximumDelay: TimeSpan.FromMinutes(1),
                leaseDuration: TimeSpan.FromSeconds(5)),
            timeProvider);
        publisher = new PatchTrustEvidencePublisher(ingestion);
    }

    public PatchTrustReceiptDispatchReport DispatchPending(
        int maximumMessages = 256,
        CancellationToken cancellationToken = default)
    {
        if (maximumMessages is < 1 or > 4096)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumMessages),
                "A Patch Trust receipt dispatch pass must remain bounded.");
        }

        int delivered = 0;
        int retryScheduled = 0;
        int blocked = 0;
        for (int index = 0; index < maximumMessages; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SqliteOutboxDispatchResult result;
            try
            {
                result = dispatcher.DispatchNextOfType(
                    EvidenceIngestionRequest.MessageType,
                    publisher,
                    cancellationToken);
            }
            catch (SqlitePersistenceException exception) when (
                exception.ErrorCode == SqlitePersistenceErrorCodes.OutboxPublishFailed)
            {
                retryScheduled++;
                break;
            }

            switch (result.Outcome)
            {
                case SqliteOutboxDispatchOutcome.NoMessage:
                    return CreateReport(
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
                    return CreateReport(
                        delivered,
                        retryScheduled,
                        blocked,
                        limitReached: false,
                        cancellationToken);
                case SqliteOutboxDispatchOutcome.Blocked:
                    blocked++;
                    return CreateReport(
                        delivered,
                        retryScheduled,
                        blocked,
                        limitReached: false,
                        cancellationToken);
                default:
                    throw new InvalidOperationException(
                        "The Patch Trust receipt dispatcher returned an unsupported outcome.");
            }
        }

        return CreateReport(
            delivered,
            retryScheduled,
            blocked,
            limitReached: true,
            cancellationToken);
    }

    public SqliteOutboxBacklogHealth ReadBacklog(
        CancellationToken cancellationToken = default) =>
        dispatcher.ReadBacklogHealthOfType(
            EvidenceIngestionRequest.MessageType,
            cancellationToken);

    private PatchTrustReceiptDispatchReport CreateReport(
        int delivered,
        int retryScheduled,
        int blocked,
        bool limitReached,
        CancellationToken cancellationToken) =>
        new(
            delivered,
            retryScheduled,
            blocked,
            limitReached,
            ReadBacklog(cancellationToken));
}

public sealed record PatchTrustReceiptDispatchReport(
    int Delivered,
    int RetryScheduled,
    int Blocked,
    bool LimitReached,
    SqliteOutboxBacklogHealth Backlog);
