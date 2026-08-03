using Opure.Persistence.Sqlite;
using Opure.TrustEvidence.Contracts;
using Opure.Workspace.Sqlite;

namespace Opure.Workspace.Service;

public sealed class WorkspaceTrustEvidencePublisher : ISqliteOutboxPublisher
{
    private readonly ITrustEvidenceOwnerIngestionPort ingestion;

    public WorkspaceTrustEvidencePublisher(
        ITrustEvidenceOwnerIngestionPort ingestion)
    {
        this.ingestion = ingestion ??
            throw new ArgumentNullException(nameof(ingestion));
        if (!string.Equals(
                ingestion.BoundOwnerServiceId,
                WorkspaceDatabase.OwnerServiceId,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The Trust ingestion port is not bound to the Workspace Service owner.",
                nameof(ingestion));
        }
    }

    public SqliteOutboxPublishResult Publish(
        SqliteOutboxMessage message,
        CancellationToken cancellationToken)
    {
        EvidenceIngestionRequest request;
        try
        {
            request = WorkspaceTrustEvidenceOutbox.CreateIngestionRequest(message);
        }
        catch (Exception exception) when (
            exception is ArgumentException or
                InvalidOperationException or
                OverflowException)
        {
            return SqliteOutboxPublishResult.PermanentFailure(
                "workspace-trust-receipt-invalid");
        }

        EvidenceIngestionReceipt receipt = ingestion.Ingest(
            request,
            cancellationToken);
        return receipt.Disposition is
            EvidenceIngestionDisposition.Applied or
            EvidenceIngestionDisposition.Duplicate
                ? SqliteOutboxPublishResult.Delivered(receipt.ReceiptId)
                : SqliteOutboxPublishResult.PermanentFailure(
                    "workspace-trust-receipt-rejected");
    }
}

public sealed class WorkspaceTrustReceiptDispatchService
{
    private readonly SqliteOutboxDispatcher dispatcher;
    private readonly WorkspaceTrustEvidencePublisher publisher;

    public WorkspaceTrustReceiptDispatchService(
        WorkspaceDatabase database,
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
        publisher = new WorkspaceTrustEvidencePublisher(ingestion);
    }

    public WorkspaceTrustReceiptDispatchReport DispatchPending(
        int maximumMessages = 256,
        CancellationToken cancellationToken = default)
    {
        if (maximumMessages is < 1 or > 4096)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumMessages),
                "A Workspace Trust receipt dispatch pass must remain bounded.");
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
                        "The Workspace Trust receipt dispatcher returned an unsupported outcome.");
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

    private WorkspaceTrustReceiptDispatchReport CreateReport(
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

public sealed record WorkspaceTrustReceiptDispatchReport(
    int Delivered,
    int RetryScheduled,
    int Blocked,
    bool LimitReached,
    SqliteOutboxBacklogHealth Backlog);
