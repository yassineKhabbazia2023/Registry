using Application.Options;
using Application.Requests;
using Application.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Registry.Application.Tests.Services;

/// <summary>
/// Tests for <see cref="AkuiteoMockCustomerService"/>.
/// </summary>
public class AkuiteoMockCustomerServiceTests
{
    #region CreateCustomerAsync

    /// <summary>
    /// Ensures the mocked service returns a random numeric account number.
    /// </summary>
    [Fact]
    public async Task CreateCustomerAsync_WhenCalled_ShouldReturnRandomNumericAccountNumber()
    {
        // Arrange
        var service = CreateService(new AkuiteoOptions
        {
            MockAccountNumber = "MOCK-123"
        });

        // Act
        var result = await service.CreateCustomerAsync(CreateRequest());

        // Assert
        Assert.NotNull(result.AccountNumber);
        Assert.Equal(10, result.AccountNumber.Length);
        Assert.All(result.AccountNumber, character => Assert.True(char.IsDigit(character)));
    }

    /// <summary>
    /// Ensures the mocked service returns a new random account number on each call.
    /// </summary>
    [Fact]
    public async Task CreateCustomerAsync_WhenCalledTwice_ShouldReturnDifferentAccountNumbers()
    {
        // Arrange
        var service = CreateService(new AkuiteoOptions
        {
            MockAccountNumber = string.Empty
        });

        // Act
        var firstResult = await service.CreateCustomerAsync(CreateRequest());
        var secondResult = await service.CreateCustomerAsync(CreateRequest());

        // Assert
        Assert.NotEqual(firstResult.AccountNumber, secondResult.AccountNumber);
    }

    #endregion

    /// <summary>
    /// Creates the service under test.
    /// </summary>
    /// <param name="akuiteoOptions">The Akuiteo options.</param>
    /// <returns>The configured service.</returns>
    private static AkuiteoMockCustomerService CreateService(AkuiteoOptions akuiteoOptions)
    {
        return new AkuiteoMockCustomerService(
            Microsoft.Extensions.Options.Options.Create(akuiteoOptions),
            Mock.Of<ILogger<AkuiteoMockCustomerService>>());
    }

    /// <summary>
    /// Creates a valid Registry request.
    /// </summary>
    /// <returns>A valid request instance.</returns>
    private static AkuiteoCustomerCreationRequest CreateRequest()
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
}
