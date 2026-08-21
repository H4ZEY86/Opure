using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Opure.Runtime.Contracts.Models;

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

    public ObservableCollection<ToolActivityItem> ToolActivity { get; } = new();

    public ICommand GenerateCommand { get; }
    public ICommand StopCommand { get; }

    public string Prompt { get; set; } = string.Empty;

    private async Task GenerateAsync()
    {
        if (IsGenerating || string.IsNullOrWhiteSpace(Prompt)) return;

        IsGenerating = true;
        GeneratedText = string.Empty;
        
        if (_syncContext != null)
        {
            _syncContext.Post(_ => ToolActivity.Clear(), null);
        }
        else
        {
            ToolActivity.Clear();
        }

        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            await foreach (var payload in _source.GenerateStreamAsync(Prompt, _cancellationTokenSource.Token).ConfigureAwait(false))
            {
                if (payload.IsToolCall)
                {
                    AddOrUpdateToolActivity(payload.Content);
                }
                else
                {
                    AppendToken(payload.Content);
                }
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
            DeactivateAllTools();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private void AddOrUpdateToolActivity(string content)
    {
        var statusText = FormatToolActivity(content);
        
        Action updateAction = () =>
        {
            // Deactivate any currently active tools before adding a new one
            foreach (var item in ToolActivity)
            {
                item.IsActive = false;
            }

            ToolActivity.Add(new ToolActivityItem
            {
                Id = Guid.NewGuid().ToString(),
                StatusText = statusText,
                IsActive = true
            });
        };

        if (_syncContext != null)
        {
            _syncContext.Post(_ => updateAction(), null);
        }
        else
        {
            updateAction();
        }
    }

    private static string FormatToolActivity(string content)
    {
        try
        {
            var toolRequest = JsonSerializer.Deserialize(content, ModelContractsJsonContext.Default.ToolRequest);
            if (toolRequest == null) return "Executing tool...";

            return toolRequest.ToolName switch
            {
                "read_file_range" => $"Reading {GetArg(toolRequest, "path")} (lines {GetArg(toolRequest, "start")}-{GetArg(toolRequest, "end")})...",
                "list_directory" => $"Listing directory {GetArg(toolRequest, "path")}...",
                "inspect_diff" => "Inspecting workspace diff...",
                "apply_patch" => $"Staging workspace patch for {GetArg(toolRequest, "path")}...",
                "run_command" => $"Executing sandboxed command: {GetArg(toolRequest, "command")}...",
                "search_workspace" => $"Searching codebase for '{GetArg(toolRequest, "query")}'...",
                _ => $"Executing {toolRequest.ToolName}..."
            };
        }
        catch
        {
            return "Executing tool...";
        }
    }

    private static string GetArg(ToolRequest req, string key)
    {
        if (req.Arguments != null && req.Arguments.TryGetValue(key, out var val))
        {
            return val?.ToString() ?? string.Empty;
        }
        return string.Empty;
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

    private void DeactivateAllTools()
    {
        Action deactivateAction = () =>
        {
            foreach (var item in ToolActivity)
            {
                item.IsActive = false;
            }
        };

        if (_syncContext != null)
        {
            _syncContext.Post(_ => deactivateAction(), null);
        }
        else
        {
            deactivateAction();
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
