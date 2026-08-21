using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Markup.Xaml;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Opure.Desktop;

public class EnumMatchConverter : IValueConverter
{
    public static readonly EnumMatchConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return false;
        return value.ToString() == parameter.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public partial class WorkspaceGraphCanvas : UserControl
{
    private double _zoom = 1.0;
    private Point _offset = new Point(0, 0);
    private bool _isPanning = false;
    private Point _lastMousePosition;
    private TransformGroup? _canvasTransform;

    public WorkspaceGraphCanvas()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        
        var canvasBorder = this.FindControl<Border>("CanvasBorder");
        if (canvasBorder != null)
        {
            canvasBorder.PointerWheelChanged += OnPointerWheelChanged;
            canvasBorder.PointerPressed += OnPointerPressed;
            canvasBorder.PointerMoved += OnPointerMoved;
            canvasBorder.PointerReleased += OnPointerReleased;
            
            if (canvasBorder.Child is Grid grid && grid.RenderTransform is TransformGroup tg)
            {
                _canvasTransform = tg;
            }
        }
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (_canvasTransform == null) return;
        
        var scaleTransform = _canvasTransform.Children[0] as ScaleTransform;
        if (scaleTransform == null) return;

        double zoomAmount = e.Delta.Y > 0 ? 1.1 : 0.9;
        
        if (_zoom * zoomAmount < 0.1 || _zoom * zoomAmount > 10.0) return;

        _zoom *= zoomAmount;
        scaleTransform.ScaleX = _zoom;
        scaleTransform.ScaleY = _zoom;
        
        e.Handled = true;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed || 
            e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _isPanning = true;
            _lastMousePosition = e.GetPosition(this);
            e.Handled = true;
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isPanning) return;

        var currentPosition = e.GetPosition(this);
        var delta = currentPosition - _lastMousePosition;
        _lastMousePosition = currentPosition;

        if (_canvasTransform == null) return;

        var translateTransform = _canvasTransform.Children[1] as TranslateTransform;
        if (translateTransform == null) return;

        _offset = new Point(_offset.X + delta.X, _offset.Y + delta.Y);
        translateTransform.X = _offset.X;
        translateTransform.Y = _offset.Y;

        e.Handled = true;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            e.Handled = true;
        }
    }
}
