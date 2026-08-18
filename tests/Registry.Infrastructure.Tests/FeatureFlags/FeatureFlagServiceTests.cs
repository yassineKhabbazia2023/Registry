using Infrastructure.FeatureFlags;
using Microsoft.Extensions.Logging;
using Moq;
using OpenFeature;
using OpenFeature.Constant;
using OpenFeature.Model;

namespace Registry.Infrastructure.Tests.FeatureFlags;

public class FeatureFlagServiceTests
{
    [Fact]
    public void IsEnabled_ReturnsValue_WhenNoError()
    {
        var featureClientMock = new Mock<IFeatureClient>();
        featureClientMock
            .Setup(c => c.GetBooleanDetailsAsync("myFlag", false, null, null, default))
            .ReturnsAsync(new FlagEvaluationDetails<bool>("myFlag", true, ErrorType.None, "TARGETING_MATCH", null, null, null));

        var loggerMock = new Mock<ILogger<FeatureFlagService>>();
        var service = new FeatureFlagService(featureClientMock.Object, loggerMock.Object);

        var result = service.IsEnabled("myFlag");

        Assert.True(result);
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public void IsEnabled_ReturnsDefaultAndLogsWarning_WhenEvaluationFails()
    {
        var featureClientMock = new Mock<IFeatureClient>();
        featureClientMock
            .Setup(c => c.GetBooleanDetailsAsync("myFlag", false, null, null, default))
            .ReturnsAsync(new FlagEvaluationDetails<bool>("myFlag", false, ErrorType.General, "ERROR", null, "network unreachable", null));

        var loggerMock = new Mock<ILogger<FeatureFlagService>>();
        var service = new FeatureFlagService(featureClientMock.Object, loggerMock.Object);

        var result = service.IsEnabled("myFlag");

        Assert.False(result);
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("myFlag") && v.ToString()!.Contains("General")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }
}
