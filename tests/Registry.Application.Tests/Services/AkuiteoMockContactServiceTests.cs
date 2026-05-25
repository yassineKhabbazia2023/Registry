using Application.Options;
using Application.Requests;
using Application.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Registry.Application.Tests.Services;

/// <summary>
/// Tests for <see cref="AkuiteoMockContactService"/>.
/// </summary>
public class AkuiteoMockContactServiceTests
{
    #region CreateContactAsync

    /// <summary>
    /// Ensures the mocked service returns the configured mocked contact identifier.
    /// </summary>
    [Fact]
    public async Task CreateContactAsync_WhenMockContactIdIsConfigured_ShouldReturnConfiguredMockedContactId()
    {
        // Arrange
        var service = CreateService(new AkuiteoOptions
        {
            MockContactId = "MOCK-CONTACT-123"
        });

        // Act
        var result = await service.CreateContactAsync(CreateRequest());

        // Assert
        Assert.Equal("MOCK-CONTACT-123", result.ContactId);
    }

    /// <summary>
    /// Ensures the mocked service falls back to the default mocked contact identifier.
    /// </summary>
    [Fact]
    public async Task CreateContactAsync_WhenMockContactIdIsNotConfigured_ShouldReturnDefaultMockedContactId()
    {
        // Arrange
        var service = CreateService(new AkuiteoOptions
        {
            MockContactId = string.Empty
        });

        // Act
        var result = await service.CreateContactAsync(CreateRequest());

        // Assert
        Assert.Equal("500145940", result.ContactId);
    }

    #endregion

    /// <summary>
    /// Creates the service under test.
    /// </summary>
    /// <param name="akuiteoOptions">The Akuiteo options.</param>
    /// <returns>The configured service.</returns>
    private static AkuiteoMockContactService CreateService(AkuiteoOptions akuiteoOptions)
    {
        return new AkuiteoMockContactService(
            Microsoft.Extensions.Options.Options.Create(akuiteoOptions),
            Mock.Of<ILogger<AkuiteoMockContactService>>());
    }

    /// <summary>
    /// Creates a valid Registry request.
    /// </summary>
    /// <returns>A valid request instance.</returns>
    private static AkuiteoContactCreationRequest CreateRequest()
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
}
