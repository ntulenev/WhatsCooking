using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace WhatsCooking.ViewModels;

/// <summary>
/// Task-based command with execution state and cancellation support.
/// </summary>
internal sealed class AsyncRelayCommand : ICommand, INotifyPropertyChanged, IDisposable
{
    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncRelayCommand"/> class.
    /// </summary>
    /// <param name="execute">Asynchronous operation to execute.</param>
    /// <param name="canExecute">Predicate that controls whether the command can run.</param>
    public AsyncRelayCommand(Func<CancellationToken, Task> execute, Func<bool>? canExecute = null)
    {
        ArgumentNullException.ThrowIfNull(execute);

        _execute = execute;
        _canExecute = canExecute;
    }

    /// <summary>
    /// Gets the currently executing task, if any.
    /// </summary>
    public Task? ExecutionTask {
        get;
        private set
        {
            if (ReferenceEquals(field, value))
            {
                return;
            }

            field = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets a value indicating whether the command is currently executing.
    /// </summary>
    public bool IsRunning {
        get;
        private set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanBeCanceled));
            RaiseCanExecuteChanged();
        }
    }

    /// <summary>
    /// Gets a value indicating whether the current execution can be canceled.
    /// </summary>
    public bool CanBeCanceled => IsRunning && _executionCancellation is { IsCancellationRequested: false };

    /// <inheritdoc />
    public bool CanExecute(object? parameter) => !IsRunning && (_canExecute?.Invoke() ?? true);

    /// <inheritdoc />
    public async void Execute(object? parameter)
    {
        await ExecuteAsync().ConfigureAwait(true);
    }

    /// <summary>
    /// Executes the command and returns a task representing the operation.
    /// </summary>
    /// <returns>Task representing the current command execution.</returns>
    public Task ExecuteAsync()
    {
        if (!CanExecute(null))
        {
            return Task.CompletedTask;
        }

        _executionCancellation?.Dispose();
        _executionCancellation = new CancellationTokenSource();
        IsRunning = true;
        OnPropertyChanged(nameof(CanBeCanceled));

        ExecutionTask = ExecuteCoreAsync(_executionCancellation.Token);
        return ExecutionTask;
    }

    /// <summary>
    /// Requests cancellation of the current execution.
    /// </summary>
    public void Cancel()
    {
        if (!CanBeCanceled)
        {
            return;
        }

        _executionCancellation?.Cancel();
        OnPropertyChanged(nameof(CanBeCanceled));
    }

    /// <summary>
    /// Raises the <see cref="CanExecuteChanged"/> event.
    /// </summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    /// <inheritdoc />
    public void Dispose()
    {
        _executionCancellation?.Dispose();
    }

    private async Task ExecuteCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _execute(cancellationToken).ConfigureAwait(true);
        }
        finally
        {
            IsRunning = false;
            OnPropertyChanged(nameof(CanBeCanceled));
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private readonly Func<CancellationToken, Task> _execute;

    private readonly Func<bool>? _canExecute;

    private CancellationTokenSource? _executionCancellation;
}
