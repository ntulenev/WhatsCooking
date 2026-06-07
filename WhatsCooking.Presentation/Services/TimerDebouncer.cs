using System.Diagnostics.CodeAnalysis;

namespace WhatsCooking.Services;

/// <summary>
/// Debounces actions with <see cref="TimeProvider"/> timers.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Service is created by dependency injection.")]
internal sealed class TimerDebouncer : IDebouncer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TimerDebouncer"/> class.
    /// </summary>
    /// <param name="timeProvider">Time provider used to create the timer.</param>
    public TimerDebouncer(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        _synchronizationContext = SynchronizationContext.Current;
        _timer = timeProvider.CreateTimer(OnTimerElapsed, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    /// <inheritdoc />
    public void Schedule(Action action, TimeSpan delay)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentOutOfRangeException.ThrowIfLessThan(delay, TimeSpan.Zero);

        lock (_syncRoot)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            _pendingAction = action;
            _ = _timer.Change(delay, Timeout.InfiniteTimeSpan);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_syncRoot)
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _pendingAction = null;
            _timer.Dispose();
        }
    }

    private void OnTimerElapsed(object? state)
    {
        Action? action;
        lock (_syncRoot)
        {
            if (_isDisposed)
            {
                return;
            }

            action = _pendingAction;
            _pendingAction = null;
        }

        if (action is null)
        {
            return;
        }

        if (_synchronizationContext is null)
        {
            action();
            return;
        }

        _synchronizationContext.Post(static callbackState => ((Action)callbackState!).Invoke(), action);
    }

    private readonly ITimer _timer;

    private readonly SynchronizationContext? _synchronizationContext;

    private readonly object _syncRoot = new();

    private Action? _pendingAction;

    private bool _isDisposed;
}
