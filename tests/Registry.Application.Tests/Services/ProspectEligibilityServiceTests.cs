using Application.Interfaces;
using Application.Services;
using Kpmg.ExceptionMiddleware.AdvancedException;
using Microsoft.Extensions.Logging;
using Moq;

namespace Registry.Application.Tests.Services;

public class ProspectEligibilityServiceTests
{
    private readonly Mock<IAccountOnboardingEligibilityProvider> accountOnboardingEligibilityProviderMock;
    private readonly ProspectEligibilityService service;

    public ProspectEligibilityServiceTests()
    {
        accountOnboardingEligibilityProviderMock = new Mock<IAccountOnboardingEligibilityProvider>();
        service = new ProspectEligibilityService(accountOnboardingEligibilityProviderMock.Object, Mock.Of<ILogger<ProspectEligibilityService>>());
    }

    #region CheckEligibilityAsync

    [Fact]
    public async Task CheckEligibilityAsync_WhenSiretAlreadyExists_ReturnsNotEligible()
    {
        // Arrange
        const string normalizedSiret = "91772785100011";
        accountOnboardingEligibilityProviderMock
            .Setup(provider => provider.ExistsAsync(normalizedSiret))
            .ReturnsAsync(true);

        // Act
        var result = await service.CheckEligibilityAsync($" {normalizedSiret} ");

        // Assert
        Assert.False(result);
        accountOnboardingEligibilityProviderMock.Verify(
            provider => provider.ExistsAsync(normalizedSiret),
            Times.Once);
    }

    [Fact]
    public async Task CheckEligibilityAsync_WhenSiretDoesNotExist_ReturnsEligible()
    {
        // Arrange
        const string siret = "73282932000074";
        accountOnboardingEligibilityProviderMock
            .Setup(provider => provider.ExistsAsync(siret))
            .ReturnsAsync(false);

        // Act
        var result = await service.CheckEligibilityAsync(siret);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("1234567890123A")]
    [InlineData("91772785100012")]
    public async Task CheckEligibilityAsync_WhenSiretIsInvalid_ThrowsBadRequestException(string? siret)
    {
        // Act
        var action = () => service.CheckEligibilityAsync(siret);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);
        accountOnboardingEligibilityProviderMock.Verify(
            provider => provider.ExistsAsync(It.IsAny<string>()),
            Times.Never);
    }

    #endregion
}
