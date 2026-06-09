using FluentAssertions;

using Moq;

using WhatsCooking.Services;

namespace WhatsCooking.Presentation.Tests;

public sealed class TimerDebouncerTests
{
    [Fact(DisplayName = "Constructor throws when time provider is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenTimeProviderIsNullThrowsArgumentNullException()
    {
        // Arrange
        TimeProvider timeProvider = null!;

        // Act
        Action act = () => _ = new TimerDebouncer(timeProvider);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Schedule throws when action is null")]
    [Trait("Category", "Unit")]
    public void ScheduleWhenActionIsNullThrowsArgumentNullException()
    {
        // Arrange
        var fixture = CreateFixture();
        using var debouncer = fixture.Debouncer;
        Action action = null!;

        // Act
        Action act = () => debouncer.Schedule(action, TimeSpan.Zero);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Schedule throws when delay is negative")]
    [Trait("Category", "Unit")]
    public void ScheduleWhenDelayIsNegativeThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var fixture = CreateFixture();
        using var debouncer = fixture.Debouncer;

        // Act
        Action act = () => debouncer.Schedule(() => { }, TimeSpan.FromMilliseconds(-1));

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "Schedule invokes only latest action when timer elapses")]
    [Trait("Category", "Unit")]
    public void ScheduleWhenCalledAgainInvokesOnlyLatestAction()
    {
        // Arrange
        var fixture = CreateFixture();
        using var debouncer = fixture.Debouncer;
        var firstCalls = 0;
        var secondCalls = 0;

        // Act
        debouncer.Schedule(() => firstCalls++, TimeSpan.FromMilliseconds(100));
        debouncer.Schedule(() => secondCalls++, TimeSpan.FromMilliseconds(200));
        fixture.Callback(null);
        fixture.Callback(null);

        // Assert
        firstCalls.Should().Be(0);
        secondCalls.Should().Be(1);
        fixture.TimerChanges.Should().Equal(
            (TimeSpan.FromMilliseconds(100), Timeout.InfiniteTimeSpan),
            (TimeSpan.FromMilliseconds(200), Timeout.InfiniteTimeSpan));
    }

    [Fact(DisplayName = "Schedule throws after debouncer is disposed")]
    [Trait("Category", "Unit")]
    public void ScheduleWhenDebouncerIsDisposedThrowsObjectDisposedException()
    {
        // Arrange
        var fixture = CreateFixture();
        fixture.Debouncer.Dispose();

        // Act
        Action act = () => fixture.Debouncer.Schedule(() => { }, TimeSpan.Zero);

        // Assert
        act.Should().Throw<ObjectDisposedException>();
        fixture.Timer.Verify(instance => instance.Dispose(), Times.Once);
    }

    private static TimerFixture CreateFixture()
    {
        TimerCallback? callback = null;
        var timerChanges = new List<(TimeSpan DueTime, TimeSpan Period)>();
        var timer = new Mock<ITimer>(MockBehavior.Strict);
        timer.Setup(instance => instance.Change(
                It.Is<TimeSpan>(dueTime => dueTime >= TimeSpan.Zero),
                Timeout.InfiniteTimeSpan))
            .Callback<TimeSpan, TimeSpan>((dueTime, period) => timerChanges.Add((dueTime, period)))
            .Returns(true);
        timer.Setup(instance => instance.Dispose());
        var timeProvider = new Mock<TimeProvider>(MockBehavior.Strict);
        timeProvider.Setup(instance => instance.CreateTimer(
                It.IsAny<TimerCallback>(),
                null,
                Timeout.InfiniteTimeSpan,
                Timeout.InfiniteTimeSpan))
            .Callback<TimerCallback, object?, TimeSpan, TimeSpan>((value, _, _, _) => callback = value)
            .Returns(timer.Object);

        var debouncer = new TimerDebouncer(timeProvider.Object);
        return new TimerFixture(debouncer, timer, timerChanges, state => callback!(state));
    }

    private sealed record TimerFixture(
        TimerDebouncer Debouncer,
        Mock<ITimer> Timer,
        List<(TimeSpan DueTime, TimeSpan Period)> TimerChanges,
        TimerCallback Callback);
}
