using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Opure.Desktop.Contracts;
using Opure.Ipc.Abstractions;
using Opure.Ipc.NamedPipes.Windows;
using Opure.Recovery.Protocol;
using Opure.Recovery.Protocol.Point.V1;

namespace Opure.Desktop.GatewayClient;

public sealed class RecoveryPointGatewayClient : DesktopRecoveryPointViewModel
{
    private readonly string _releaseChannel;

    public override ObservableCollection<DesktopRecoveryPoint> RecoveryPoints { get; } = new();

    public override ICommand CreateRecoveryPointCommand { get; }
    public override ICommand VerifyRecoveryPointCommand { get; }

    public RecoveryPointGatewayClient(string releaseChannel)
    {
        _releaseChannel = releaseChannel;
        CreateRecoveryPointCommand = new RelayCommand(async () => await CreateAsync());
        VerifyRecoveryPointCommand = new RelayCommand(async () => await VerifyAsync());

        // Fire and forget list population
        _ = RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        RuntimeHealthEndpoint? endpoint = RuntimeHealthEndpointEnvironment.ReadCurrent();
        RuntimeHealthSessionMaterial? sessionMaterial = RuntimeHealthSessionEnvironment.ReadCurrent();

        if (endpoint is null || sessionMaterial is null)
        {
            return;
        }

        await using NamedPipeRecoveryPointClient client = new(endpoint, sessionMaterial);

        try
        {
            var response = await client.ListRecoveryPointsAsync(new ListRecoveryPointsRequestMessage
            {
                ContractRevision = RecoveryPointContractPolicy.CurrentRevision,
                ReleaseChannel = _releaseChannel
            }, CancellationToken.None).ConfigureAwait(true);

            RecoveryPoints.Clear();

            foreach (var point in response.Points)
            {
                RecoveryPoints.Add(new DesktopRecoveryPoint(
                    Guid.Parse(point.RecoveryPointId),
                    DateTimeOffset.FromUnixTimeMilliseconds(point.CreatedAtUnixTimeMilliseconds).ToLocalTime(),
                    point.VerificationState));
            }
        }
        catch
        {
            // Ignore transport errors for now
        }
    }

    private async Task CreateAsync()
    {
        RuntimeHealthEndpoint? endpoint = RuntimeHealthEndpointEnvironment.ReadCurrent();
        RuntimeHealthSessionMaterial? sessionMaterial = RuntimeHealthSessionEnvironment.ReadCurrent();

        if (endpoint is null || sessionMaterial is null)
        {
            return;
        }

        await using NamedPipeRecoveryPointClient client = new(endpoint, sessionMaterial);

        try
        {
            var response = await client.CreateRecoveryPointAsync(new CreateRecoveryPointRequestMessage
            {
                ContractRevision = RecoveryPointContractPolicy.CurrentRevision,
                ReleaseChannel = _releaseChannel
            }, CancellationToken.None).ConfigureAwait(true);

            if (response.IsSuccess)
            {
                await RefreshAsync();
            }
        }
        catch
        {
            // Ignore for now
        }
    }

    private static Task VerifyAsync() => Task.CompletedTask;

    private class RelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        public RelayCommand(Func<Task> execute) => _execute = execute;
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public async void Execute(object? parameter) => await _execute();
    }
}
