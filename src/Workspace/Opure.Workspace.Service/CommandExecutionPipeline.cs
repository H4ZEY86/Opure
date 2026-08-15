using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Opure.TrustEvidence.Contracts;
using Opure.Workspace.Contracts;
using Opure.Workspace.Execution;

namespace Opure.Workspace.Service;

public sealed class CommandExecutionPipeline : ICommandExecutionPipeline
{
    private readonly IRestrictedCommandWorker _worker;
    private readonly TimeProvider _timeProvider;

    public CommandExecutionPipeline(
        IRestrictedCommandWorker worker,
        TimeProvider timeProvider)
    {
        _worker = worker;
        _timeProvider = timeProvider;
    }

    public async Task<CommandExitReceipt> ExecuteAsync(
        CommandApproval approval,
        ToolTemplate template,
        string stagingDirectory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(approval);
        ArgumentNullException.ThrowIfNull(template);
        ArgumentException.ThrowIfNullOrWhiteSpace(stagingDirectory);

        // Anti-drift validation
        string expectedIdInput = $"{approval.TemplateHash}:{approval.CanonicalArguments}:{approval.WorkspaceSnapshotId}";
        string expectedId = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(expectedIdInput)));

        if (!string.Equals(approval.Id, expectedId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Command approval identity drift detected. Execution aborted.");
        }

        if (!string.Equals(template.Id, approval.TemplateHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Provided ToolTemplate identity does not match CommandApproval.");
        }

        string receiptId = Guid.NewGuid().ToString("N");
        DateTimeOffset startedAtUtc = _timeProvider.GetUtcNow();

        CommandExecutionResult result;
        bool wasCancelled = false;
        bool wasTimeout = false;

        try
        {
            result = await _worker.ExecuteAsync(template, approval.TargetDirectory, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            wasCancelled = true;
            result = new CommandExecutionResult(
                -1,
                new CommandOutputBuffer(string.Empty, new CommandOutputMetadata(false, 0, false, false)),
                new CommandOutputBuffer(string.Empty, new CommandOutputMetadata(false, 0, false, false)));
        }
        catch (TimeoutException)
        {
            wasTimeout = true;
            result = new CommandExecutionResult(
                -2,
                new CommandOutputBuffer(string.Empty, new CommandOutputMetadata(false, 0, false, false)),
                new CommandOutputBuffer(string.Empty, new CommandOutputMetadata(false, 0, false, false)));
        }

        DateTimeOffset finishedAtUtc = _timeProvider.GetUtcNow();

        string outHash = await FlushStagingBlobAsync(stagingDirectory, result.StandardOutput.Content, cancellationToken).ConfigureAwait(false);
        string errHash = await FlushStagingBlobAsync(stagingDirectory, result.StandardError.Content, cancellationToken).ConfigureAwait(false);

        var outReceipt = new CommandStreamReceipt(
            result.StandardOutput.Metadata.TotalBytesRead,
            result.StandardOutput.Metadata.Truncated,
            result.StandardOutput.Metadata.RedactionApplied,
            result.StandardOutput.Metadata.EncodingFaultsDetected,
            outHash);

        var errReceipt = new CommandStreamReceipt(
            result.StandardError.Metadata.TotalBytesRead,
            result.StandardError.Metadata.Truncated,
            result.StandardError.Metadata.RedactionApplied,
            result.StandardError.Metadata.EncodingFaultsDetected,
            errHash);

        return new CommandExitReceipt(
            receiptId,
            approval.Id,
            startedAtUtc,
            finishedAtUtc,
            result.ExitCode,
            wasCancelled,
            wasTimeout,
            outReceipt,
            errReceipt);
    }

    private static async Task<string> FlushStagingBlobAsync(string stagingDirectory, string content, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(content))
        {
            return string.Empty;
        }

        byte[] bytes = Encoding.UTF8.GetBytes(content);
        string hash = Convert.ToHexStringLower(SHA256.HashData(bytes));
        string blobPath = Path.Combine(stagingDirectory, hash);

        if (!File.Exists(blobPath))
        {
            Directory.CreateDirectory(stagingDirectory);
            await File.WriteAllBytesAsync(blobPath, bytes, cancellationToken).ConfigureAwait(false);
        }

        return hash;
    }
}
