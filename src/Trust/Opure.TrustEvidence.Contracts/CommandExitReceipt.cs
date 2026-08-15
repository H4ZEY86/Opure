using System;

namespace Opure.TrustEvidence.Contracts;

public sealed class CommandStreamReceipt
{
    public CommandStreamReceipt(
        long totalBytesRead,
        bool truncated,
        bool redactionApplied,
        bool encodingFaultsDetected,
        string stagingBlobHash)
    {
        TotalBytesRead = totalBytesRead;
        Truncated = truncated;
        RedactionApplied = redactionApplied;
        EncodingFaultsDetected = encodingFaultsDetected;
        StagingBlobHash = stagingBlobHash ?? string.Empty;
    }

    public long TotalBytesRead { get; }
    public bool Truncated { get; }
    public bool RedactionApplied { get; }
    public bool EncodingFaultsDetected { get; }
    public string StagingBlobHash { get; }
}

public sealed class CommandExitReceipt
{
    public CommandExitReceipt(
        string id,
        string approvalId,
        DateTimeOffset startedAtUtc,
        DateTimeOffset finishedAtUtc,
        int exitCode,
        bool wasCancelled,
        bool wasTimeout,
        CommandStreamReceipt standardOutput,
        CommandStreamReceipt standardError)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(approvalId);
        ArgumentNullException.ThrowIfNull(standardOutput);
        ArgumentNullException.ThrowIfNull(standardError);

        if (finishedAtUtc < startedAtUtc)
        {
            throw new ArgumentException("A command cannot finish before it starts.", nameof(finishedAtUtc));
        }

        Id = id;
        ApprovalId = approvalId;
        StartedAtUtc = startedAtUtc;
        FinishedAtUtc = finishedAtUtc;
        ExitCode = exitCode;
        WasCancelled = wasCancelled;
        WasTimeout = wasTimeout;
        StandardOutput = standardOutput;
        StandardError = standardError;
    }

    public string Id { get; }
    public string ApprovalId { get; }
    public DateTimeOffset StartedAtUtc { get; }
    public DateTimeOffset FinishedAtUtc { get; }
    public int ExitCode { get; }
    public bool WasCancelled { get; }
    public bool WasTimeout { get; }
    public CommandStreamReceipt StandardOutput { get; }
    public CommandStreamReceipt StandardError { get; }
}
