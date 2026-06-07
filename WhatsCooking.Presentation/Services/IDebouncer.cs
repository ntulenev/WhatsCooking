namespace WhatsCooking.Services;

/// <summary>
/// Schedules the latest requested action after a quiet period.
/// </summary>
internal interface IDebouncer : IDisposable
{
    /// <summary>
    /// Replaces any pending action and schedules the new action.
    /// </summary>
    /// <param name="action">Action to invoke.</param>
    /// <param name="delay">Quiet period before invocation.</param>
    void Schedule(Action action, TimeSpan delay);
}
