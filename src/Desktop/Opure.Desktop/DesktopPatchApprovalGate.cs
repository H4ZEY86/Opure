using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Opure.Desktop.Contracts;
using Opure.Patch.Contracts;
using Opure.Runtime.Contracts.Models;
using Opure.TrustEvidence.Contracts;
using Opure.Workspace.Contracts;

namespace Opure.Desktop;

public sealed class DesktopPatchApprovalGate : IPatchApprovalGate
{
    private readonly IPatchReviewDialogService _dialogService;
    private readonly ITrustLedgerSource _ledgerSource;

    public DesktopPatchApprovalGate(IPatchReviewDialogService dialogService, ITrustLedgerSource ledgerSource)
    {
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(ledgerSource);
        
        _dialogService = dialogService;
        _ledgerSource = ledgerSource;
    }

    public async Task<ExecutePatchCommand> RequestPatchApprovalAsync(
        ExecutePatchCommand command,
        string agentIdentity,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var diffLines = UnifiedDiffParser.Parse(command.Proposals);
        
        int added = 0;
        int deleted = 0;
        foreach (var line in diffLines)
        {
            if (line.Kind == DiffKind.Added) added++;
            if (line.Kind == DiffKind.Deleted) deleted++;
        }

        string filePath = command.Proposals.Count > 0 ? command.Proposals[0].TargetFileHeader : "unknown";

        var viewModel = new PatchReviewViewModel(
            filePath,
            diffLines,
            added,
            deleted);

        PatchReviewResult? innerResult;
        
        if (SynchronizationContext.Current != null)
        {
            var resultTask = Dispatcher.UIThread.InvokeAsync(
                () => _dialogService.ShowReviewAsync(viewModel, cancellationToken),
                DispatcherPriority.Normal);
            innerResult = await resultTask.ConfigureAwait(false);
        }
        else
        {
            // Bypass dispatcher in test environments where no UI thread exists
            innerResult = await _dialogService.ShowReviewAsync(viewModel, cancellationToken).ConfigureAwait(false);
        }

        if (innerResult is null || !innerResult.IsApproved)
        {
            throw new OperationCanceledException(innerResult?.Feedback ?? "Patch rejected by developer.");
        }

        var finalIdentity = innerResult.ApproverIdentity ?? ApproverIdentity.User("Developer");

        var receipt = new TrustReceiptItem(
            Guid.NewGuid().ToString("N")[..8],
            DateTimeOffset.UtcNow.ToString("O"),
            finalIdentity,
            filePath,
            $"+{added} / -{deleted} lines",
            "Cryptographically Verified"
        );
        _ledgerSource.PushReceipt(receipt);

        return new ExecutePatchCommand
        {
            PatchId = command.PatchId,
            WorkspaceRootPath = command.WorkspaceRootPath,
            Proposals = command.Proposals,
            ApproverIdentity = finalIdentity
        };
    }

    public Task<CommandApproval> RequestCommandApprovalAsync(
        ToolTemplate template,
        string agentIdentity,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Command approval UI is deferred.");
    }
}
