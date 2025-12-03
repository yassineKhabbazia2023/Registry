using Infrastructure.Exceptions;

namespace Registry.Infrastructure.Tests.Exceptions;

public class ProcessContactOperationExceptionTests
{
    [Fact]
    public void DefaultConstructor_ShouldCreateException()
    {
        // Act
        var exception = new ProcessContactOperationException();

        // Assert
        Assert.NotNull(exception);
        Assert.IsType<ProcessContactOperationException>(exception);
    }

    [Fact]
    public void MessageConstructor_ShouldSetMessage()
    {
        // Arrange
        var expectedMessage = "Test error message";

        // Act
        var exception = new ProcessContactOperationException(expectedMessage);

        // Assert
        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public void MessageAndInnerExceptionConstructor_ShouldSetBoth()
    {
        // Arrange
        var expectedMessage = "Outer exception message";
        var innerException = new ArgumentException("Inner exception message");

        // Act
        var exception = new ProcessContactOperationException(expectedMessage, innerException);

        // Assert
        Assert.Equal(expectedMessage, exception.Message);
        Assert.Same(innerException, exception.InnerException);
    }

    [Fact]
    public void Exception_ShouldInheritFromException()
    {
        // Act
        var exception = new ProcessContactOperationException();

        // Assert
        Assert.IsAssignableFrom<Exception>(exception);
    }

    [Fact]
    public void InnerException_ShouldPreserveOriginalException()
    {
        // Arrange
        var innerMessage = "Original error";
        var outerMessage = "Wrapped error";
        var originalException = new InvalidOperationException(innerMessage);

        // Act
        var exception = new ProcessContactOperationException(outerMessage, originalException);

        // Assert
        Assert.NotNull(exception.InnerException);
        Assert.Equal(innerMessage, exception.InnerException.Message);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Simple message")]
    [InlineData("Message with special characters: !@#$%^&*()")]
    public void MessageConstructor_WithVariousMessages_ShouldStoreCorrectly(string message)
    {
        // Act
        var exception = new ProcessContactOperationException(message);

        // Assert
        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void ChainedExceptions_ShouldPreserveAllLevels()
    {
        // Arrange
        var level1 = new ArgumentException("Level 1");
        var level2 = new InvalidOperationException("Level 2", level1);
        var level3 = new ProcessContactOperationException("Level 3", level2);

        // Assert
        Assert.Equal("Level 3", level3.Message);
        Assert.Equal("Level 2", level3.InnerException?.Message);
        Assert.Equal("Level 1", level3.InnerException?.InnerException?.Message);
    }

    [Fact]
    public void ToString_ShouldIncludeExceptionDetails()
    {
        // Arrange
        var message = "Test exception details";
        var exception = new ProcessContactOperationException(message);

        // Act
        var result = exception.ToString();

        // Assert
        Assert.Contains("ProcessContactOperationException", result);
        Assert.Contains(message, result);
    }
}