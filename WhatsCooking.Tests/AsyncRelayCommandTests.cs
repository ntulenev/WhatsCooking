using WhatsCooking.ViewModels;

using Xunit;

namespace WhatsCooking.Tests;

public sealed class AsyncRelayCommandTests
{
    [Fact]
    public async Task ExecuteAsyncDisablesConcurrentExecution()
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var command = new AsyncRelayCommand(_ => completion.Task);

        var execution = command.ExecuteAsync();

        Assert.True(command.IsRunning);
        Assert.False(command.CanExecute(null));
        Assert.Same(execution, command.ExecutionTask);

        completion.SetResult();
        await execution;

        Assert.False(command.IsRunning);
        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public async Task CancelPropagatesCancellationToken()
    {
        var cancellationObserved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var command = new AsyncRelayCommand(async cancellationToken =>
        {
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                cancellationObserved.SetResult();
            }
        });

        var execution = command.ExecuteAsync();
        command.Cancel();

        await cancellationObserved.Task;
        await execution;

        Assert.False(command.IsRunning);
    }
}
