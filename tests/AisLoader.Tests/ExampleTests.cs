using Shouldly;
using Xunit;

namespace AisLoader.Tests;

public class ExampleTests
{
    [Fact]
    public void Example_SimpleTest()
    {
        // Arrange
        var expected = 42;
        var actual = 42;

        // Act & Assert
        actual.ShouldBe(expected);
    }

    [Theory]
    [InlineData(2, 4)]
    [InlineData(3, 9)]
    [InlineData(5, 25)]
    public void Example_Theory_WithMultipleInputs(int input, int expected)
    {
        // Act
        var result = input * input;

        // Assert
        result.ShouldBe(expected);
    }
}