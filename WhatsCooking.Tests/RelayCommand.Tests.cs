using FluentAssertions;

using WhatsCooking.ViewModels;

namespace WhatsCooking.Tests;

public sealed class RelayCommandTests
{
    [Fact(DisplayName = "Constructor throws when parameterless action is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenParameterlessActionIsNullThrowsArgumentNullException()
    {
        // Arrange
        Action execute = null!;

        // Act
        Action act = () => _ = new RelayCommand(execute);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when parameterized action is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenParameterizedActionIsNullThrowsArgumentNullException()
    {
        // Arrange
        Action<object?> execute = null!;

        // Act
        Action act = () => _ = new RelayCommand(execute);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Execute invokes action with exact parameter")]
    [Trait("Category", "Unit")]
    public void ExecuteWhenCalledInvokesActionWithExactParameter()
    {
        // Arrange
        var parameter = new object();
        object? receivedParameter = null;
        var command = new RelayCommand(value => receivedParameter = value);

        // Act
        command.Execute(parameter);

        // Assert
        receivedParameter.Should().BeSameAs(parameter);
    }

    [Fact(DisplayName = "CanExecute returns predicate result")]
    [Trait("Category", "Unit")]
    public void CanExecuteWhenPredicateExistsReturnsPredicateResult()
    {
        // Arrange
        var allowedParameter = new object();
        var command = new RelayCommand(_ => { }, value => ReferenceEquals(value, allowedParameter));

        // Act
        var allowed = command.CanExecute(allowedParameter);
        var denied = command.CanExecute(new object());

        // Assert
        allowed.Should().BeTrue();
        denied.Should().BeFalse();
    }

    [Fact(DisplayName = "RaiseCanExecuteChanged publishes event")]
    [Trait("Category", "Unit")]
    public void RaiseCanExecuteChangedWhenCalledPublishesEvent()
    {
        // Arrange
        var command = new RelayCommand(() => { });
        var calls = 0;
        command.CanExecuteChanged += (_, _) => calls++;

        // Act
        command.RaiseCanExecuteChanged();

        // Assert
        calls.Should().Be(1);
    }
}
