using System.Collections.ObjectModel;
using System.Windows.Input;
using Opure.Desktop.Contracts;
using Opure.Ipc.Abstractions;
using Opure.Ipc.NamedPipes.Windows;
using Opure.Recovery.Protocol;
using Opure.Recovery.Protocol.Point.V1;

namespace Opure.Desktop.GatewayClient;

public sealed class RecoveryPointGatewayClient : DesktopRecoveryPointViewModel
{
    private readonly string releaseChannel;
    private string statusTitle = "Recovery points unavailable";
    private string statusDetail = "The local Runtime has not provided a recovery point projection.";
    private bool isBusy;

    public RecoveryPointGatewayClient(string releaseChannel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(releaseChannel);
        this.releaseChannel = releaseChannel;
        CreateRecoveryPointCommand = new AsyncRelayCommand(CreateAsync, _ => !IsBusy);
        VerifyRecoveryPointCommand = new AsyncRelayCommand(VerifyAsync, parameter => !IsBusy && parameter is DesktopRecoveryPoint);
        RefreshRecoveryPointsCommand = new AsyncRelayCommand(
            _ => RefreshAsync(CancellationToken.None),
            _ => !IsBusy);
    }

    public override ObservableCollection<DesktopRecoveryPoint> RecoveryPoints { get; } = [];

    public override ICommand CreateRecoveryPointCommand { get; }

    public override ICommand VerifyRecoveryPointCommand { get; }

    public override ICommand RefreshRecoveryPointsCommand { get; }

    public override string StatusTitle => statusTitle;

    public override string StatusDetail => statusDetail;

    public override bool IsBusy => isBusy;

    public override bool HasRecoveryPoints => RecoveryPoints.Count > 0;

    public override async Task RefreshAsync(CancellationToken cancellationToken)
    {
        SetBusy(true);
        try
        {
            await using NamedPipeRecoveryPointClient client = CreateClient();
            ListRecoveryPointsResponseMessage response = await client.ListRecoveryPointsAsync(
                new ListRecoveryPointsRequestMessage
                {
                    ContractRevision = RecoveryPointContractPolicy.CurrentRevision,
                    ReleaseChannel = releaseChannel
                },
                cancellationToken).ConfigureAwait(true);

            RecoveryPoints.Clear();
            foreach (RecoveryPointSummaryMessage point in response.Points)
            {
                if (!Guid.TryParse(point.RecoveryPointId, out Guid recoveryPointId))
                {
                    continue;
                }

                RecoveryPoints.Add(new DesktopRecoveryPoint(
                    recoveryPointId,
                    DateTimeOffset.FromUnixTimeMilliseconds(
                        point.CreatedAtUnixTimeMilliseconds).ToLocalTime(),
                    point.VerificationState,
                    point.ScopeClass,
                    point.ProductVersion,
                    point.OwnerCount,
                    point.SupportedSchemaVersions.ToArray(),
                    point.Receipts.Select(receipt => new DesktopRecoveryPointReceipt(
                        receipt.EventType,
                        DateTimeOffset.FromUnixTimeMilliseconds(
                            receipt.TimestampUnixTimeMilliseconds).ToLocalTime(),
                        receipt.StatusMessage)).ToArray()));
            }

            statusTitle = RecoveryPoints.Count == 0
                ? "No local recovery points"
                : $"{RecoveryPoints.Count} local recovery point{(RecoveryPoints.Count == 1 ? string.Empty : "s")}";
            statusDetail = RecoveryPoints.Count == 0
                ? "Create a same-device recovery point explicitly when you need a local rollback checkpoint."
                : "Recovery points are projected by the Runtime. They do not protect against device loss.";
            NotifyProjectionChanged();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            statusTitle = "Recovery points unavailable";
            statusDetail = "The Runtime could not provide the recovery point list. No fallback data was used.";
            NotifyProjectionChanged();
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task CreateAsync(object? parameter)
    {
        _ = parameter;
        SetBusy(true);
        try
        {
            await using NamedPipeRecoveryPointClient client = CreateClient();
            CreateRecoveryPointResponseMessage response = await client.CreateRecoveryPointAsync(
                new CreateRecoveryPointRequestMessage
                {
                    ContractRevision = RecoveryPointContractPolicy.CurrentRevision,
                    ReleaseChannel = releaseChannel
                },
                CancellationToken.None).ConfigureAwait(true);

            if (!response.IsSuccess)
            {
                statusTitle = "Recovery point creation failed";
                statusDetail = string.IsNullOrWhiteSpace(response.ErrorMessage)
                    ? "The Runtime rejected the recovery point request. Existing points were not changed."
                    : response.ErrorMessage;
                NotifyProjectionChanged();
                return;
            }
        }
        catch (Exception)
        {
            statusTitle = "Recovery point creation unavailable";
            statusDetail = "The Runtime could not be reached. Existing recovery points were not changed.";
            NotifyProjectionChanged();
            return;
        }
        finally
        {
            SetBusy(false);
        }

        await RefreshAsync(CancellationToken.None).ConfigureAwait(true);
    }

    private async Task VerifyAsync(object? parameter)
    {
        if (parameter is not DesktopRecoveryPoint recoveryPoint)
        {
            return;
        }

        SetBusy(true);
        try
        {
            await using NamedPipeRecoveryPointClient client = CreateClient();
            VerifyRecoveryPointResponseMessage response = await client.VerifyRecoveryPointAsync(
                new VerifyRecoveryPointRequestMessage
                {
                    ContractRevision = RecoveryPointContractPolicy.CurrentRevision,
                    ReleaseChannel = releaseChannel,
                    RecoveryPointId = recoveryPoint.RecoveryPointId.ToString("N")
                },
                CancellationToken.None).ConfigureAwait(true);

            statusTitle = response.IsSuccess
                ? "Structural verification completed"
                : "Structural verification failed";
            statusDetail = response.IsSuccess
                ? $"Recovery point {recoveryPoint.RecoveryPointId:D} passed verification in a disposable staging root."
                : response.ErrorMessage;
            NotifyProjectionChanged();
        }
        catch (Exception)
        {
            statusTitle = "Verification unavailable";
            statusDetail = "The Runtime could not verify the selected recovery point.";
            NotifyProjectionChanged();
        }
        finally
        {
            SetBusy(false);
        }
    }

    private static NamedPipeRecoveryPointClient CreateClient()
    {
        RuntimeHealthEndpoint endpoint = RuntimeHealthEndpointEnvironment.ReadCurrent()
            ?? throw new InvalidOperationException("The Runtime endpoint is unavailable.");
        RuntimeHealthSessionMaterial sessionMaterial = RuntimeHealthSessionEnvironment.ReadCurrent()
            ?? throw new InvalidOperationException("The Runtime session is unavailable.");
        return new NamedPipeRecoveryPointClient(endpoint, sessionMaterial);
    }

    private void SetBusy(bool value)
    {
        if (isBusy == value)
        {
            return;
        }

        isBusy = value;
        OnPropertyChanged(nameof(IsBusy));
        ((AsyncRelayCommand)CreateRecoveryPointCommand).NotifyCanExecuteChanged();
        ((AsyncRelayCommand)VerifyRecoveryPointCommand).NotifyCanExecuteChanged();
        ((AsyncRelayCommand)RefreshRecoveryPointsCommand).NotifyCanExecuteChanged();
    }

    private void NotifyProjectionChanged()
    {
        OnPropertyChanged(nameof(StatusTitle));
        OnPropertyChanged(nameof(StatusDetail));
        OnPropertyChanged(nameof(HasRecoveryPoints));
    }

    private sealed class AsyncRelayCommand(
        Func<object?, Task> execute,
        Predicate<object?> canExecute) : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => canExecute(parameter);

        public async void Execute(object? parameter)
        {
            await execute(parameter).ConfigureAwait(true);
        }

        internal void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
