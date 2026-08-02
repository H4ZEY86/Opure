using Opure.Persistence.Sqlite;
using Opure.Project.Sqlite;
using Opure.TrustEvidence.Contracts;

namespace Opure.Project.Service;

public sealed class ProjectTrustEvidencePublisher : ISqliteOutboxPublisher
{
    private readonly ITrustEvidenceOwnerIngestionPort ingestion;

    public ProjectTrustEvidencePublisher(
        ITrustEvidenceOwnerIngestionPort ingestion)
    {
        this.ingestion = ingestion ??
            throw new ArgumentNullException(nameof(ingestion));

        if (!string.Equals(
                ingestion.BoundOwnerServiceId,
                ProjectDatabase.OwnerServiceId,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The Trust ingestion port is not bound to the Project Service owner.",
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
            request = ProjectTrustEvidenceOutbox.CreateIngestionRequest(
                message);
        }
        catch (Exception exception) when (
            exception is ArgumentException or
                InvalidOperationException or
                OverflowException)
        {
            return SqliteOutboxPublishResult.PermanentFailure(
                "project-trust-receipt-invalid");
        }

        EvidenceIngestionReceipt receipt = ingestion.Ingest(
            request,
            cancellationToken);

        return receipt.Disposition is
            EvidenceIngestionDisposition.Applied or
            EvidenceIngestionDisposition.Duplicate
                ? SqliteOutboxPublishResult.Delivered(receipt.ReceiptId)
                : SqliteOutboxPublishResult.PermanentFailure(
                    "project-trust-receipt-rejected");
    }
}

public sealed record ProjectTrustReceiptDispatchReport(
    int Delivered,
    int RetryScheduled,
    int Blocked,
    bool LimitReached,
    SqliteOutboxBacklogHealth Backlog);
