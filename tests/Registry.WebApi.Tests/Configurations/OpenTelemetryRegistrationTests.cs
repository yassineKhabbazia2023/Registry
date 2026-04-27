using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebApi.Configurations;

namespace Registry.WebApi.Tests.Configurations;

public class OpenTelemetryRegistrationTests
{
    [Fact]
    public void RegisterOpenTelemetry_WithValidConnectionString_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["APPLICATIONINSIGHTS_CONNECTION_STRING"] = "InstrumentationKey=test-key;IngestionEndpoint=https://test.in.applicationinsights.azure.com/"
            })
            .Build();

        var initialCount = services.Count;

        // Act
        services.RegisterOpenTelemetry(configuration);

        // Assert
        services.Count.Should().BeGreaterThan(initialCount);
    }

    [Fact]
    public void RegisterOpenTelemetry_WithMissingConnectionString_ShouldNotRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var initialCount = services.Count;

        // Act
        services.RegisterOpenTelemetry(configuration);

        // Assert
        services.Count.Should().Be(initialCount);
    }
}
