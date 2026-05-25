using System.ComponentModel.DataAnnotations;
using Application.Requests;

namespace Registry.Application.Tests.Requests;

/// <summary>
/// Tests for <see cref="AkuiteoContactTypesRequest"/>.
/// </summary>
public class AkuiteoContactTypesRequestTests
{
    #region Validation

    /// <summary>
    /// Ensures a fully populated contact-types object is valid.
    /// </summary>
    [Fact]
    public void Validate_WhenRequestIsComplete_ShouldSucceed()
    {
        // Arrange
        var request = CreateValidRequest();

        // Act
        var result = Validate(request);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Ensures required flags are enforced.
    /// </summary>
    [Theory]
    [InlineData(nameof(AkuiteoContactTypesRequest.IsDigitalVaultContact))]
    [InlineData(nameof(AkuiteoContactTypesRequest.IsDebtCollectionContact))]
    [InlineData(nameof(AkuiteoContactTypesRequest.IsMandateSignatory))]
    public void Validate_WhenRequiredFlagIsMissing_ShouldFail(string propertyName)
    {
        // Arrange
        var request = CreateValidRequest();
        typeof(AkuiteoContactTypesRequest).GetProperty(propertyName)!.SetValue(request, null);

        // Act
        var validationResults = ValidateWithResults(request);

        // Assert
        Assert.Contains(validationResults, result => result.MemberNames.Contains(propertyName));
    }

    #endregion

    /// <summary>
    /// Creates a valid request.
    /// </summary>
    /// <returns>A valid request instance.</returns>
    private static AkuiteoContactTypesRequest CreateValidRequest()
    {
        return new AkuiteoContactTypesRequest
        {
            IsDigitalVaultContact = true,
            IsDebtCollectionContact = false,
            IsMandateSignatory = true
        };
    }

    /// <summary>
    /// Validates the supplied request.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <returns><see langword="true"/> when validation succeeds; otherwise <see langword="false"/>.</returns>
    private static bool Validate(AkuiteoContactTypesRequest request)
    {
        return Validator.TryValidateObject(request, new ValidationContext(request), null, validateAllProperties: true);
    }

    /// <summary>
    /// Validates the supplied request and returns detailed validation errors.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <returns>The validation results.</returns>
    private static List<ValidationResult> ValidateWithResults(AkuiteoContactTypesRequest request)
    {
        var validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), validationResults, validateAllProperties: true);
        return validationResults;
    }
}
