namespace Opure.Desktop.Contracts;

public sealed record TrustReceiptItem(
    string ReceiptId,
    string Timestamp,
    string Approver,
    string TargetFileOrCommand,
    string MutationSummary,
    string VerificationStatus
);
