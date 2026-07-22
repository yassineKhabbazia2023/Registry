using System.ComponentModel.DataAnnotations;
using Application.Requests;

namespace Registry.Application.Tests.Requests;

/// <summary>
/// Tests for <see cref="AkuiteoBankingInformationRequest"/> validation.
/// </summary>
public class AkuiteoBankingInformationRequestTests
{
    /// <summary>
    /// Ensures supported Akuiteo banking actions pass model validation.
    /// </summary>
    /// <param name="action">The supported action.</param>
    [Theory]
    [InlineData("ADD")]
    [InlineData("UPDATE")]
    [InlineData("REMOVE")]
    public void Validate_WithSupportedAction_ShouldBeValid(string action)
    {
        // Arrange
        var request = new AkuiteoBankingInformationRequest { Action = action };

        // Act
        var results = Validate(request);

        // Assert
        Assert.Empty(results);
    }

    /// <summary>
    /// Ensures missing or unsupported actions follow the established DataAnnotations validation behavior.
    /// </summary>
    /// <param name="action">The invalid action.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("UPSERT")]
    [InlineData("add")]
    public void Validate_WithUnsupportedAction_ShouldBeInvalid(string? action)
    {
        // Arrange
        var request = new AkuiteoBankingInformationRequest { Action = action };

        // Act
        var results = Validate(request);

        // Assert
        Assert.NotEmpty(results);
    }

    /// <summary>
    /// Validates a banking-information request with DataAnnotations.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <returns>The validation failures.</returns>
    private static IReadOnlyCollection<ValidationResult> Validate(AkuiteoBankingInformationRequest request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, true);
        return results;
    }
}
