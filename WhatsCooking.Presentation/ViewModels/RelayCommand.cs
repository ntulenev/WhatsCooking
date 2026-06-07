using System.Windows.Input;

namespace WhatsCooking.ViewModels;

/// <summary>
/// Delegate-based WPF command implementation.
/// </summary>
internal sealed class RelayCommand : ICommand
{
    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="RelayCommand"/> class.
    /// </summary>
    /// <param name="execute">Command action.</param>
    /// <param name="canExecute">Predicate that controls whether the command can run.</param>
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute == null ? null : _ => canExecute())
    {
        ArgumentNullException.ThrowIfNull(execute, nameof(execute));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RelayCommand"/> class.
    /// </summary>
    /// <param name="execute">Command action with a command parameter.</param>
    /// <param name="canExecute">Predicate that controls whether the command can run.</param>
    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        ArgumentNullException.ThrowIfNull(execute, nameof(execute));
        _execute = execute;
        _canExecute = canExecute;
    }

    /// <inheritdoc />
    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    /// <inheritdoc />
    public void Execute(object? parameter) => _execute(parameter);

    /// <summary>
    /// Raises the <see cref="CanExecuteChanged"/> event.
    /// </summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    private readonly Action<object?> _execute;

    private readonly Predicate<object?>? _canExecute;
}
