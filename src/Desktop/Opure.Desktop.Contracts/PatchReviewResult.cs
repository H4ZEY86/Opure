namespace Opure.Desktop.Contracts;

public sealed record PatchReviewResult(
    bool IsApproved,
    string? ApproverIdentity,
    string? Feedback
);
