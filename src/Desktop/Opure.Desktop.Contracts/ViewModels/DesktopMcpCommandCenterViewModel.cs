using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Opure.Desktop.Contracts;
using Opure.Runtime.Contracts.Mcp.V1;

namespace Opure.Desktop.Contracts;

public sealed class DesktopMcpCommandCenterViewModel : INotifyPropertyChanged
{
    private readonly IDesktopMcpCommandCenterSource source;
    private bool isRefreshing;
    private bool hasError;
    private string errorMessage = string.Empty;

    public DesktopMcpCommandCenterViewModel(IDesktopMcpCommandCenterSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        this.source = source;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsRefreshing
    {
        get => isRefreshing;
        set
        {
            if (isRefreshing != value)
            {
                isRefreshing = value;
                OnPropertyChanged();
            }
        }
    }

    public bool HasError
    {
        get => hasError;
        set
        {
            if (hasError != value)
            {
                hasError = value;
                OnPropertyChanged();
            }
        }
    }

    public string ErrorMessage
    {
        get => errorMessage;
        set
        {
            if (errorMessage != value)
            {
                errorMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public ObservableCollection<McpToolDefinition> Tools { get; } = new();

    public async Task RefreshAsync()
    {
        if (IsRefreshing)
        {
            return;
        }

        IsRefreshing = true;
        HasError = false;

        try
        {
            var response = await source.GetToolsAsync(CancellationToken.None).ConfigureAwait(true);
            
            Tools.Clear();
            foreach (var tool in response.Tools)
            {
                Tools.Add(tool);
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
