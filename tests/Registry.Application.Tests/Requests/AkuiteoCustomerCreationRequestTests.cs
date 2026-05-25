using System.ComponentModel.DataAnnotations;
using Application.Requests;

namespace Registry.Application.Tests.Requests;

/// <summary>
/// Tests for <see cref="AkuiteoCustomerCreationRequest"/>.
/// </summary>
public class AkuiteoCustomerCreationRequestTests
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
    [InlineData(nameof(AkuiteoCustomerCreationRequest.LegalName))]
    [InlineData(nameof(AkuiteoCustomerCreationRequest.Siret))]
    [InlineData(nameof(AkuiteoCustomerCreationRequest.Siren))]
    [InlineData(nameof(AkuiteoCustomerCreationRequest.LegalStructure))]
    [InlineData(nameof(AkuiteoCustomerCreationRequest.LegalForm))]
    [InlineData(nameof(AkuiteoCustomerCreationRequest.NafCode))]
    [InlineData(nameof(AkuiteoCustomerCreationRequest.Address))]
    [InlineData(nameof(AkuiteoCustomerCreationRequest.ZipCode))]
    [InlineData(nameof(AkuiteoCustomerCreationRequest.City))]
    [InlineData(nameof(AkuiteoCustomerCreationRequest.DepartmentCode))]
    [InlineData(nameof(AkuiteoCustomerCreationRequest.RegionCode))]
    [InlineData(nameof(AkuiteoCustomerCreationRequest.CountryCode))]
    [InlineData(nameof(AkuiteoCustomerCreationRequest.CaseManagerContactId))]
    [InlineData(nameof(AkuiteoCustomerCreationRequest.AccountManagerContactId))]
    public void Validate_WhenRequiredFieldIsMissing_ShouldFail(string propertyName)
    {
        // Arrange
        var request = CreateValidRequest();
        typeof(AkuiteoCustomerCreationRequest).GetProperty(propertyName)!.SetValue(request, null);

        // Act
        var validationResults = ValidateWithResults(request);

        // Assert
        Assert.Contains(validationResults, result => result.MemberNames.Contains(propertyName));
    }

    /// <summary>
    /// Ensures manager contact identifiers must be positive.
    /// </summary>
    [Fact]
    public void Validate_WhenManagerContactIdsAreInvalid_ShouldFail()
    {
        // Arrange
        var request = CreateValidRequest();
        request.CaseManagerContactId = 0;
        request.AccountManagerContactId = 0;

        // Act
        var validationResults = ValidateWithResults(request);

        // Assert
        Assert.Contains(validationResults, result => result.MemberNames.Contains(nameof(AkuiteoCustomerCreationRequest.CaseManagerContactId)));
        Assert.Contains(validationResults, result => result.MemberNames.Contains(nameof(AkuiteoCustomerCreationRequest.AccountManagerContactId)));
    }

    #endregion

    /// <summary>
    /// Creates a valid request.
    /// </summary>
    /// <returns>A valid request instance.</returns>
    private static AkuiteoCustomerCreationRequest CreateValidRequest()
    {
        return new AkuiteoCustomerCreationRequest
        {
            LegalName = "Boulangerie du coin",
            Siret = "72200393604516",
            Siren = "722003936",
            LegalStructure = "PersonneMorale",
            LegalForm = "SAS",
            NafCode = "10.71C",
            Address = "221B Baker Street",
            ZipCode = "69002",
            City = "Lyon",
            DepartmentCode = "69",
            RegionCode = "84",
            CountryCode = "FR",
            CaseManagerContactId = 10,
            AccountManagerContactId = 20
        };
    }

    /// <summary>
    /// Validates the supplied request.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <returns><see langword="true"/> when validation succeeds; otherwise <see langword="false"/>.</returns>
    private static bool Validate(AkuiteoCustomerCreationRequest request)
    {
        return Validator.TryValidateObject(request, new ValidationContext(request), null, validateAllProperties: true);
    }

    /// <summary>
    /// Validates the supplied request and returns detailed validation errors.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <returns>The validation results.</returns>
    private static List<ValidationResult> ValidateWithResults(AkuiteoCustomerCreationRequest request)
    {
        var validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), validationResults, validateAllProperties: true);
        return validationResults;
    }
}
