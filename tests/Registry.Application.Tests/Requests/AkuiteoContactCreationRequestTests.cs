using System.ComponentModel.DataAnnotations;
using Application.Requests;

namespace Registry.Application.Tests.Requests;

/// <summary>
/// Tests for <see cref="AkuiteoContactCreationRequest"/>.
/// </summary>
public class AkuiteoContactCreationRequestTests
{
    #region Validation

    /// <summary>
    /// Ensures a fully populated request is valid.
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
    /// Ensures required fields are enforced.
    /// </summary>
    [Theory]
    [InlineData(nameof(AkuiteoContactCreationRequest.AccountNumber))]
    [InlineData(nameof(AkuiteoContactCreationRequest.Title))]
    [InlineData(nameof(AkuiteoContactCreationRequest.LastName))]
    [InlineData(nameof(AkuiteoContactCreationRequest.FirstName))]
    [InlineData(nameof(AkuiteoContactCreationRequest.JobTitle))]
    [InlineData(nameof(AkuiteoContactCreationRequest.ContactDepartment))]
    [InlineData(nameof(AkuiteoContactCreationRequest.CompanyRole))]
    [InlineData(nameof(AkuiteoContactCreationRequest.ContactTypes))]
    [InlineData(nameof(AkuiteoContactCreationRequest.Email))]
    [InlineData(nameof(AkuiteoContactCreationRequest.MobilePhone))]
    public void Validate_WhenRequiredFieldIsMissing_ShouldFail(string propertyName)
    {
        // Arrange
        var request = CreateValidRequest();
        typeof(AkuiteoContactCreationRequest).GetProperty(propertyName)!.SetValue(request, null);

        // Act
        var validationResults = ValidateWithResults(request);

        // Assert
        Assert.Contains(validationResults, result => result.MemberNames.Contains(propertyName));
    }

    /// <summary>
    /// Ensures the title is constrained to the Akuiteo contract values.
    /// </summary>
    [Theory]
    [InlineData("Mr")]
    [InlineData("mme")]
    [InlineData("M ")]
    [InlineData("Unknown")]
    public void Validate_WhenTitleIsInvalid_ShouldFail(string title)
    {
        // Arrange
        var request = CreateValidRequest();
        request.Title = title;

        // Act
        var validationResults = ValidateWithResults(request);

        // Assert
        Assert.Contains(validationResults, result => result.MemberNames.Contains(nameof(AkuiteoContactCreationRequest.Title)));
    }

    /// <summary>
    /// Ensures the allowed titles are accepted by validation.
    /// </summary>
    [Theory]
    [InlineData("M")]
    [InlineData("Mme")]
    [InlineData("Dr")]
    [InlineData("Pr")]
    public void Validate_WhenTitleIsValid_ShouldSucceed(string title)
    {
        // Arrange
        var request = CreateValidRequest();
        request.Title = title;

        // Act
        var result = Validate(request);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Ensures the email contract is enforced.
    /// </summary>
    [Theory]
    [InlineData("not-an-email")]
    [InlineData("contact@")]
    [InlineData("contact@@domain.com")]
    public void Validate_WhenEmailIsInvalid_ShouldFail(string email)
    {
        // Arrange
        var request = CreateValidRequest();
        request.Email = email;

        // Act
        var validationResults = ValidateWithResults(request);

        // Assert
        Assert.Contains(validationResults, result => result.MemberNames.Contains(nameof(AkuiteoContactCreationRequest.Email)));
    }

    #endregion

    /// <summary>
    /// Creates a valid request.
    /// </summary>
    /// <returns>A valid request instance.</returns>
    private static AkuiteoContactCreationRequest CreateValidRequest()
    {
        return new AkuiteoContactCreationRequest
        {
            AccountNumber = "9000120123",
            Title = "M",
            LastName = "Jean",
            FirstName = "Dupont",
            JobTitle = "Gérant",
            ContactDepartment = "Direction",
            CompanyRole = "Président",
            ContactTypes = new AkuiteoContactTypesRequest
            {
                IsDigitalVaultContact = true,
                IsDebtCollectionContact = false,
                IsMandateSignatory = true
            },
            Email = "o.dbira@boulangerie.fr",
            MobilePhone = "06 12 34 56 78"
        };
    }

    /// <summary>
    /// Validates the supplied request.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <returns><see langword="true"/> when validation succeeds; otherwise <see langword="false"/>.</returns>
    private static bool Validate(AkuiteoContactCreationRequest request)
    {
        return Validator.TryValidateObject(request, new ValidationContext(request), null, validateAllProperties: true);
    }

    /// <summary>
    /// Validates the supplied request and returns detailed validation errors.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <returns>The validation results.</returns>
    private static List<ValidationResult> ValidateWithResults(AkuiteoContactCreationRequest request)
    {
        var validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), validationResults, validateAllProperties: true);
        return validationResults;
    }
}
