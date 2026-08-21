using System.ComponentModel;
using System.Runtime.CompilerServices;
using Opure.Workspace.Contracts.Models;

namespace Opure.Desktop.Contracts;

public sealed class GraphNodeViewModel : INotifyPropertyChanged
{
    private double _x;
    private double _y;
    private bool _isActive;

    public string Id { get; }
    public string Label { get; }
    public NodeKind Kind { get; }

    public double X
    {
        get => _x;
        set
        {
            if (_x != value)
            {
                _x = value;
                OnPropertyChanged();
            }
        }
    }

    public double Y
    {
        get => _y;
        set
        {
            if (_y != value)
            {
                _y = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive != value)
            {
                _isActive = value;
                OnPropertyChanged();
            }
        }
    }

    public GraphNodeViewModel(string id, string label, NodeKind kind)
    {
        Id = id;
        Label = label;
        Kind = kind;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
