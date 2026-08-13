using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Opure.Desktop.Contracts;
using Xunit;

namespace Opure.Desktop.Tests;

public sealed class RecoveryPointViewHeadlessTests
{
    [AvaloniaFact]
    public void RecoveryPointControlsAreKeyboardReachableAndDescribeSameDeviceScope()
    {
        RecoveryPointView view = new()
        {
            DataContext = new FixedRecoveryPointViewModel()
        };
        Window window = new() { Content = view };

        try
        {
            window.Show();
            Button? create = view.FindControl<Button>("CreateRecoveryPointButton");
            Button? refresh = view.FindControl<Button>("RefreshRecoveryPointsButton");
            TextBlock? warning = view.FindControl<TextBlock>("RecoveryPointScopeWarning");
            ItemsControl? list = view.FindControl<ItemsControl>("RecoveryPointList");
            Assert.NotNull(create);
            Assert.NotNull(refresh);
            Assert.NotNull(warning);
            Assert.NotNull(list);
            Assert.True(create.IsTabStop);
            Assert.True(refresh.IsTabStop);
            Assert.Contains(
                "Create local recovery point",
                AutomationProperties.GetName(create),
                StringComparison.Ordinal);
            Assert.Contains(
                "Same-device recovery only",
                AutomationProperties.GetName(warning),
                StringComparison.Ordinal);
            DesktopRecoveryPoint item = Assert.IsType<DesktopRecoveryPoint>(
                Assert.Single(list.Items));
            Assert.Contains("verification Structural", item.AccessibilityLabel, StringComparison.Ordinal);
            Assert.Contains("Same-device recovery only", item.AccessibilityLabel, StringComparison.Ordinal);
        }
        finally
        {
            window.Close();
        }
    }

    private sealed class FixedRecoveryPointViewModel : DesktopRecoveryPointViewModel
    {
        public override ObservableCollection<DesktopRecoveryPoint> RecoveryPoints { get; } =
        [
            new DesktopRecoveryPoint(
                Guid.Parse("00000000-0000-0000-0000-000000000060"),
                new DateTimeOffset(2026, 8, 13, 10, 0, 0, TimeSpan.Zero),
                "Structural",
                "same-device",
                "1.0.0-test",
                2,
                [1],
                [
                    new DesktopRecoveryPointReceipt(
                        "backup.recovery-point-created",
                        new DateTimeOffset(2026, 8, 13, 10, 0, 0, TimeSpan.Zero),
                        "Created.")
                ])
        ];

        public override ICommand CreateRecoveryPointCommand { get; } = new NoOpCommand();

        public override ICommand VerifyRecoveryPointCommand { get; } = new NoOpCommand();

        public override ICommand RefreshRecoveryPointsCommand { get; } = new NoOpCommand();

        public override string StatusTitle => "One local recovery point";

        public override string StatusDetail => "Projected by Runtime.";

        public override bool IsBusy => false;

        public override bool HasRecoveryPoints => true;

        public override Task RefreshAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    private sealed class NoOpCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
        }
    }
}
