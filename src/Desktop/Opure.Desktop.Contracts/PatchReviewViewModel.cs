using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Opure.TrustEvidence.Contracts;

namespace Opure.Desktop.Contracts;

public sealed class PatchReviewViewModel : INotifyPropertyChanged
{
    private readonly TaskCompletionSource<PatchReviewResult> _tcs = new();
    private string? _feedback;

    public string FilePath { get; }
    public IReadOnlyList<DiffLineItem> DiffLines { get; }
    public int LinesAdded { get; }
    public int LinesDeleted { get; }
    
    public ICommand ApproveCommand { get; }
    public ICommand RejectCommand { get; }

    public Task<PatchReviewResult> ResultTask => _tcs.Task;

    public string? Feedback
    {
        get => _feedback;
        set
        {
            if (_feedback != value)
            {
                _feedback = value;
                OnPropertyChanged();
            }
        }
    }

    public PatchReviewViewModel(string filePath, IReadOnlyList<DiffLineItem> diffLines, int linesAdded, int linesDeleted)
    {
        FilePath = filePath;
        DiffLines = diffLines;
        LinesAdded = linesAdded;
        LinesDeleted = linesDeleted;

        ApproveCommand = new DelegateCommand(_ => 
        {
            _tcs.TrySetResult(new PatchReviewResult(true, ApproverIdentity.User("Developer"), null));
        });

        RejectCommand = new DelegateCommand(_ =>
        {
            _tcs.TrySetResult(new PatchReviewResult(false, null, Feedback));
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private sealed class DelegateCommand(Action<object?> execute) : ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => execute(parameter);
    }
}
