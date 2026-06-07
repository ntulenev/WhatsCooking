namespace WhatsCooking.ViewModels;

/// <summary>
/// Provides an exception raised by an asynchronous command.
/// </summary>
internal sealed class AsyncCommandFailedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncCommandFailedEventArgs"/> class.
    /// </summary>
    /// <param name="exception">Command execution exception.</param>
    public AsyncCommandFailedEventArgs(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        Exception = exception;
    }

    /// <summary>
    /// Command execution exception.
    /// </summary>
    public Exception Exception { get; }
}
