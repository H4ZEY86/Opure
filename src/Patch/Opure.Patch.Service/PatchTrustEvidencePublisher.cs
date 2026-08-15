using Opure.Persistence.Sqlite;
using Opure.TrustEvidence.Contracts;
using Opure.Patch.Sqlite;

namespace Opure.Patch.Service;

public sealed class PatchTrustEvidencePublisher : ISqliteOutboxPublisher
{
    private readonly ITrustEvidenceOwnerIngestionPort ingestion;



    public PatchTrustEvidencePublisher(
        ITrustEvidenceOwnerIngestionPort ingestion)
    {
        this.ingestion = ingestion ??
            throw new ArgumentNullException(nameof(ingestion));
        if (!string.Equals(
                ingestion.BoundOwnerServiceId,
                PatchDatabase.OwnerServiceId,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The Trust ingestion port is not bound to the Patch Service owner.",
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
            request = PatchTrustEvidenceOutbox.CreateIngestionRequest(message);
        }
        catch (Exception exception) when (
            exception is ArgumentException or
                InvalidOperationException or
                OverflowException)
        {
            return SqliteOutboxPublishResult.PermanentFailure(
                "patch-trust-receipt-invalid");
        }

        EvidenceIngestionReceipt receipt = ingestion.Ingest(
            request,
            cancellationToken);
        return receipt.Disposition is
            EvidenceIngestionDisposition.Applied or
            EvidenceIngestionDisposition.Duplicate
                ? SqliteOutboxPublishResult.Delivered(receipt.ReceiptId)
                : SqliteOutboxPublishResult.PermanentFailure(
                    "patch-trust-receipt-rejected");
    }
}
