using Application.Interfaces;
using Application.Options;
using Application.Providers;
using Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using WebApi.Configurations;

namespace Registry.WebApi.Tests.Configurations;

/// <summary>
/// Tests for <see cref="AkuiteoConfigurationExtensions"/> registrations.
/// </summary>
public class AkuiteoConfigurationExtensionsTests
{
    /// <summary>
    /// Ensures all Akuiteo workflows use their production implementations.
    /// </summary>
    [Fact]
    public void AddAkuiteoConfiguration_ShouldRegisterProductionImplementations()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(CreateConfiguration())
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Mock.Of<IAccountService>());
        services.AddSingleton(Mock.Of<IContactRepository>());

        // Act
        services.AddAkuiteoConfiguration(configuration);
        using var serviceProvider = services.BuildServiceProvider();

        // Assert
        Assert.IsType<AkuiteoCustomerService>(serviceProvider.GetRequiredService<IAkuiteoCustomerService>());
        Assert.IsType<AkuiteoContactService>(serviceProvider.GetRequiredService<IAkuiteoContactService>());
        Assert.IsType<AkuiteoDocumentService>(serviceProvider.GetRequiredService<IAkuiteoDocumentService>());
        Assert.IsType<AkuiteoCustomerProvider>(serviceProvider.GetRequiredService<IAkuiteoCustomerProvider>());
        Assert.IsType<AkuiteoContactProvider>(serviceProvider.GetRequiredService<IAkuiteoContactProvider>());
        Assert.IsType<AkuiteoDocumentProvider>(serviceProvider.GetRequiredService<IAkuiteoDocumentProvider>());
        Assert.IsType<AkuiteoEligibilityProvider>(serviceProvider.GetRequiredService<IAccountOnboardingEligibilityProvider>());
    }

    /// <summary>
    /// Ensures incomplete production settings are rejected.
    /// </summary>
    [Fact]
    public void AddAkuiteoConfiguration_WhenRequiredSettingsAreMissing_ShouldRejectOptions()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Akuiteo:BaseUrl"] = "https://api.akuiteo.local/"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Mock.Of<IContactRepository>());

        // Act
        services.AddAkuiteoConfiguration(configuration);
        using var serviceProvider = services.BuildServiceProvider();

        // Assert
        Assert.Throws<OptionsValidationException>(
            () => serviceProvider.GetRequiredService<IOptions<AkuiteoOptions>>().Value);
    }

    /// <summary>
    /// Creates a complete non-sensitive Akuiteo test configuration.
    /// </summary>
    /// <returns>The configuration values.</returns>
    private static Dictionary<string, string?> CreateConfiguration()
    {
        return new Dictionary<string, string?>
        {
            ["Akuiteo:BaseUrl"] = "https://api.akuiteo.local/",
            ["Akuiteo:ClientIdRateLimiting"] = "test-rate-client-id",
            ["Akuiteo:ClientSecretRateLimiting"] = "test-rate-client-secret",
            ["Akuiteo:Tenant"] = "test-tenant",
            ["Akuiteo:TokenClientId"] = "test-token-client-id",
            ["Akuiteo:TokenClientSecret"] = "test-token-client-secret",
            ["Akuiteo:TokenScope"] = "api://test/.default"
        };
    }
}
