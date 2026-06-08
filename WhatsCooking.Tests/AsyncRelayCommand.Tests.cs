using FluentAssertions;

using WhatsCooking.ViewModels;

namespace WhatsCooking.Tests;

public sealed class AsyncRelayCommandTests
{
    [Fact(DisplayName = "Constructor throws when execute delegate is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenExecuteIsNullThrowsArgumentNullException()
    {
        // Arrange
        Func<CancellationToken, Task> execute = null!;

        // Act
        Action act = () => _ = new AsyncRelayCommand(execute);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "CanExecute returns predicate result")]
    [Trait("Category", "Unit")]
    public void CanExecuteWhenPredicateExistsReturnsPredicateResult()
    {
        // Arrange
        var canExecute = false;
        using var command = new AsyncRelayCommand(_ => Task.CompletedTask, () => canExecute);

        // Act
        var denied = command.CanExecute(null);
        canExecute = true;
        var allowed = command.CanExecute(null);

        // Assert
        denied.Should().BeFalse();
        allowed.Should().BeTrue();
    }

    [Fact(DisplayName = "RaiseCanExecuteChanged publishes event")]
    [Trait("Category", "Unit")]
    public void RaiseCanExecuteChangedWhenCalledPublishesEvent()
    {
        // Arrange
        using var command = new AsyncRelayCommand(_ => Task.CompletedTask);
        var calls = 0;
        command.CanExecuteChanged += (_, _) => calls++;

        // Act
        command.RaiseCanExecuteChanged();

        // Assert
        calls.Should().Be(1);
    }

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
