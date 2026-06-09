using FluentAssertions;

namespace WhatsCooking.Tests;

public sealed class BindingProxyTests
{
    [Fact(DisplayName = "Data stores value through dependency property")]
    [Trait("Category", "Unit")]
    public void DataWhenAssignedStoresExactValue()
    {
        StaTest.Run(() =>
        {
            // Arrange
            var proxy = new BindingProxy();
            var value = new object();

            // Act
            proxy.Data = value;

            // Assert
            proxy.Data.Should().BeSameAs(value);
            proxy.GetValue(BindingProxy.DataProperty).Should().BeSameAs(value);
        });
    }

    [Fact(DisplayName = "Clone creates binding proxy with copied data")]
    [Trait("Category", "Unit")]
    public void CloneWhenCalledCreatesBindingProxyWithCopiedData()
    {
        StaTest.Run(() =>
        {
            // Arrange
            var value = new object();
            var proxy = new BindingProxy { Data = value };

            // Act
            var clone = proxy.Clone();

            // Assert
            clone.Should().BeOfType<BindingProxy>();
            ((BindingProxy)clone).Data.Should().BeSameAs(value);
        });
    }
}
