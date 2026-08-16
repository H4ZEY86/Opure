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

    public DesktopPatchApprovalGate(IPatchReviewDialogService dialogService)
    {
        ArgumentNullException.ThrowIfNull(dialogService);
        _dialogService = dialogService;
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

        var resultTask = Dispatcher.UIThread.InvokeAsync(
            () => _dialogService.ShowReviewAsync(viewModel, cancellationToken),
            DispatcherPriority.Normal);

        var innerResult = await resultTask.ConfigureAwait(false);

        if (innerResult is null || !innerResult.IsApproved)
        {
            throw new OperationCanceledException(innerResult?.Feedback ?? "Patch rejected by developer.");
        }

        return new ExecutePatchCommand
        {
            PatchId = command.PatchId,
            WorkspaceRootPath = command.WorkspaceRootPath,
            Proposals = command.Proposals,
            ApproverIdentity = innerResult.ApproverIdentity ?? ApproverIdentity.User("Developer")
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
