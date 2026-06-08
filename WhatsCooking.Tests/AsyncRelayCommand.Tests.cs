using FluentAssertions;

using WhatsCooking.ViewModels;

namespace WhatsCooking.Tests;

public sealed class AsyncRelayCommandTests
{
    [Fact(DisplayName = "ExecuteAsync disables concurrent execution")]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsyncDisablesConcurrentExecution()
    {
        // Arrange
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var command = new AsyncRelayCommand(_ => completion.Task);

        // Act
        var execution = command.ExecuteAsync();

        // Assert
        command.IsRunning.Should().BeTrue();
        command.CanExecute(null).Should().BeFalse();
        command.ExecutionTask.Should().BeSameAs(execution);

        completion.SetResult();
        await execution;

        command.IsRunning.Should().BeFalse();
        command.CanExecute(null).Should().BeTrue();
    }

    [Fact(DisplayName = "Cancel propagates cancellation token")]
    [Trait("Category", "Unit")]
    public async Task CancelPropagatesCancellationToken()
    {
        // Arrange
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

        // Act
        var execution = command.ExecuteAsync();
        command.Cancel();

        await cancellationObserved.Task;
        await execution;

        // Assert
        command.IsRunning.Should().BeFalse();
    }

    [Fact(DisplayName = "Execute publishes unexpected exception")]
    [Trait("Category", "Unit")]
    public async Task ExecutePublishesUnexpectedException()
    {
        // Arrange
        using var command = new AsyncRelayCommand(_ => throw new InvalidOperationException("Unexpected failure"));
        var failure = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        command.ExecutionFailed += (_, args) => failure.SetResult(args.Exception);

        // Act
        command.Execute(null);
        var exception = await failure.Task;

        // Assert
        exception.Should().BeOfType<InvalidOperationException>()
            .Which.Message.Should().Be("Unexpected failure");
        command.IsRunning.Should().BeFalse();
    }
}
