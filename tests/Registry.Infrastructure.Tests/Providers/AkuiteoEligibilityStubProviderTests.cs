using Application.Providers;

namespace Registry.Infrastructure.Tests.Providers;

public class AkuiteoEligibilityStubProviderTests
{
    #region ExistsAsync

    [Fact]
    public async Task ExistsAsync_WhenSiretIsInStubDataset_ReturnsTrue()
    {
        // Arrange
        var provider = new AkuiteoEligibilityStubProvider();

        // Act
        var result = await provider.ExistsAsync("91772785100011");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_WhenSiretIsNotInStubDataset_ReturnsFalse()
    {
        // Arrange
        var provider = new AkuiteoEligibilityStubProvider();

        // Act
        var result = await provider.ExistsAsync("73282932000074");

        // Assert
        Assert.False(result);
    }

    #endregion
}
