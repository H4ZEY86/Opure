using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Opure.Desktop.Contracts;

public sealed class GraphEdgeViewModel : INotifyPropertyChanged, IDisposable
{
    private GraphNodeViewModel _source = null!;
    private GraphNodeViewModel _target = null!;

    public GraphNodeViewModel Source
    {
        get => _source;
        set
        {
            if (_source != value)
            {
                if (_source != null)
                {
                    _source.PropertyChanged -= OnSourcePropertyChanged;
                }
                _source = value;
                if (_source != null)
                {
                    _source.PropertyChanged += OnSourcePropertyChanged;
                }
                OnPropertyChanged();
                OnPropertyChanged(nameof(X1));
                OnPropertyChanged(nameof(Y1));
            }
        }
    }

    public GraphNodeViewModel Target
    {
        get => _target;
        set
        {
            if (_target != value)
            {
                if (_target != null)
                {
                    _target.PropertyChanged -= OnTargetPropertyChanged;
                }
                _target = value;
                if (_target != null)
                {
                    _target.PropertyChanged += OnTargetPropertyChanged;
                }
                OnPropertyChanged();
                OnPropertyChanged(nameof(X2));
                OnPropertyChanged(nameof(Y2));
            }
        }
    }

    public double X1 => _source.X;
    public double Y1 => _source.Y;
    public double X2 => _target.X;
    public double Y2 => _target.Y;
    
    public string StartPoint => $"{X1},{Y1}";
    public string EndPoint => $"{X2},{Y2}";

    public GraphEdgeViewModel(GraphNodeViewModel source, GraphNodeViewModel target)
    {
        _source = source;
        _target = target;
        
        if (_source != null)
        {
            _source.PropertyChanged += OnSourcePropertyChanged;
        }
        
        if (_target != null)
        {
            _target.PropertyChanged += OnTargetPropertyChanged;
        }
    }

    private void OnSourcePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GraphNodeViewModel.X))
        {
            OnPropertyChanged(nameof(X1));
            OnPropertyChanged(nameof(StartPoint));
        }
        else if (e.PropertyName == nameof(GraphNodeViewModel.Y))
        {
            OnPropertyChanged(nameof(Y1));
            OnPropertyChanged(nameof(StartPoint));
        }
    }

    private void OnTargetPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GraphNodeViewModel.X))
        {
            OnPropertyChanged(nameof(X2));
            OnPropertyChanged(nameof(EndPoint));
        }
        else if (e.PropertyName == nameof(GraphNodeViewModel.Y))
        {
            OnPropertyChanged(nameof(Y2));
            OnPropertyChanged(nameof(EndPoint));
        }
    }

    public void Dispose()
    {
        if (_source != null)
        {
            _source.PropertyChanged -= OnSourcePropertyChanged;
        }
        
        if (_target != null)
        {
            _target.PropertyChanged -= OnTargetPropertyChanged;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
