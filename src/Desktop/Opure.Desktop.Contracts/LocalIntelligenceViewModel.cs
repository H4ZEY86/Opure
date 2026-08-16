using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Opure.Desktop.Contracts;

public sealed class LocalIntelligenceViewModel : INotifyPropertyChanged
{
    private readonly ILocalIntelligenceSource _source;
    private readonly SynchronizationContext? _syncContext;
    private string _generatedText = string.Empty;
    private bool _isGenerating;
    private CancellationTokenSource? _cancellationTokenSource;

    public LocalIntelligenceViewModel(ILocalIntelligenceSource source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _syncContext = SynchronizationContext.Current;

        GenerateCommand = new DelegateCommand(async _ => await GenerateAsync(), _ => !IsGenerating);
        StopCommand = new DelegateCommand(_ => StopGeneration(), _ => IsGenerating);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string GeneratedText
    {
        get => _generatedText;
        private set
        {
            if (_generatedText != value)
            {
                _generatedText = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsGenerating
    {
        get => _isGenerating;
        private set
        {
            if (_isGenerating != value)
            {
                _isGenerating = value;
                OnPropertyChanged();
                ((DelegateCommand)GenerateCommand).RaiseCanExecuteChanged();
                ((DelegateCommand)StopCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand GenerateCommand { get; }
    public ICommand StopCommand { get; }

    public string Prompt { get; set; } = string.Empty;

    private async Task GenerateAsync()
    {
        if (IsGenerating || string.IsNullOrWhiteSpace(Prompt)) return;

        IsGenerating = true;
        GeneratedText = string.Empty;
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            await foreach (var token in _source.GenerateStreamAsync(Prompt, _cancellationTokenSource.Token).ConfigureAwait(false))
            {
                AppendToken(token);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on stop
            AppendToken("\n[Generation Stopped]");
        }
        catch (Exception ex)
        {
            AppendToken($"\n[Error: {ex.Message}]");
        }
        finally
        {
            IsGenerating = false;
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private void AppendToken(string token)
    {
        if (_syncContext != null)
        {
            _syncContext.Post(_ =>
            {
                GeneratedText += token;
            }, null);
        }
        else
        {
            GeneratedText += token;
        }
    }

    private void StopGeneration()
    {
        if (IsGenerating && _cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private sealed class DelegateCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?> _canExecute;

        public DelegateCommand(Action<object?> execute, Predicate<object?> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => _canExecute(parameter);

        public void Execute(object? parameter) => _execute(parameter);

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
