using Infrastructure.Exceptions;
using Xunit;

namespace Registry.Infrastructure.Tests.Exceptions;

public class ContactPublishExceptionTests
{
    [Fact]
    public void Constructor_Default_Should_CreateException()
    {
        // Act
        var exception = new ContactPublishException();

        // Assert
        Assert.NotNull(exception);
        Assert.IsType<ContactPublishException>(exception);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void Constructor_WithMessage_Should_CreateExceptionWithMessage()
    {
        // Arrange
        var expectedMessage = "Failed to process contact publish operation";

        // Act
        var exception = new ContactPublishException(expectedMessage);

        // Assert
        Assert.NotNull(exception);
        Assert.IsType<ContactPublishException>(exception);
        Assert.Equal(expectedMessage, exception.Message);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_Should_CreateExceptionWithBoth()
    {
        // Arrange
        var expectedMessage = "Failed to process contact publish operation";
        var innerException = new InvalidOperationException("Database connection failed");

        // Act
        var exception = new ContactPublishException(expectedMessage, innerException);

        // Assert
        Assert.NotNull(exception);
        Assert.IsType<ContactPublishException>(exception);
        Assert.Equal(expectedMessage, exception.Message);
        Assert.NotNull(exception.InnerException);
        Assert.Equal(innerException, exception.InnerException);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Equal("Database connection failed", exception.InnerException!.Message);
    }

    [Fact]
    public void Constructor_WithEmptyMessage_Should_CreateExceptionWithEmptyMessage()
    {
        // Arrange
        var expectedMessage = string.Empty;

        // Act
        var exception = new ContactPublishException(expectedMessage);

        // Assert
        Assert.NotNull(exception);
        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public void Constructor_WithMessageAndNullInnerException_Should_CreateExceptionWithNullInnerException()
    {
        // Arrange
        var expectedMessage = "Failed to process contact publish operation";
        Exception? innerException = null;

        // Act
        var exception = new ContactPublishException(expectedMessage, innerException!);

        // Assert
        Assert.NotNull(exception);
        Assert.Equal(expectedMessage, exception.Message);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void Exception_Should_InheritFromException()
    {
        // Act
        var exception = new ContactPublishException();

        // Assert
        Assert.IsAssignableFrom<Exception>(exception);
    }

    [Fact]
    public void Exception_Should_BeThrowable()
    {
        // Arrange
        var expectedMessage = "Test exception";

        // Act
        Action act = () => throw new ContactPublishException(expectedMessage);

        // Assert
        var exception = Assert.Throws<ContactPublishException>(act);
        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public void Exception_WithInnerException_Should_BeThrowableAndPreserveInnerException()
    {
        // Arrange
        var expectedMessage = "Outer exception";
        var innerException = new InvalidOperationException("Inner exception");

        // Act
        Action act = () => throw new ContactPublishException(expectedMessage, innerException);

        // Assert
        var exception = Assert.Throws<ContactPublishException>(act);
        Assert.Equal(expectedMessage, exception.Message);
        Assert.NotNull(exception.InnerException);
        Assert.Equal(innerException, exception.InnerException);
        Assert.Equal("Inner exception", exception.InnerException!.Message);
    }

    [Fact]
    public void Exception_Should_PreserveStackTrace_WhenThrown()
    {
        // Arrange & Act
        ContactPublishException? caughtException = null;

        try
        {
            ThrowException();
        }
        catch (ContactPublishException ex)
        {
            caughtException = ex;
        }

        // Assert
        Assert.NotNull(caughtException);
        Assert.NotNull(caughtException.StackTrace);
        Assert.Contains(nameof(ThrowException), caughtException.StackTrace);
    }

    private static void ThrowException()
    {
        throw new ContactPublishException("Test exception with stack trace");
    }

    [Fact]
    public void Exception_Should_SupportNestedExceptions()
    {
        // Arrange
        var level3Exception = new ArgumentNullException("param");
        var level2Exception = new InvalidOperationException("Level 2", level3Exception);
        var level1Exception = new ContactPublishException("Level 1", level2Exception);

        // Act & Assert
        Assert.Equal("Level 1", level1Exception.Message);
        Assert.NotNull(level1Exception.InnerException);
        Assert.IsType<InvalidOperationException>(level1Exception.InnerException);
        Assert.Equal("Level 2", level1Exception.InnerException!.Message);
        Assert.NotNull(level1Exception.InnerException.InnerException);
        Assert.IsType<ArgumentNullException>(level1Exception.InnerException.InnerException);
    }
}