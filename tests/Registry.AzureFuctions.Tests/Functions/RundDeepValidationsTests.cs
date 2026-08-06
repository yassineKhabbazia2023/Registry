// <copyright file="RundDeepValidationsTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Interfaces;
using Infrastructure.Adapters;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using Moq;
using Registry.AzureFuctions.Functions;

namespace Registry.AzureFunctions.Tests;

public class RunDeepValidationsDurableTests
{
    [Fact]
    public async Task RunOrchestrator_ShouldProcessAkuiteoOperationsBeforeLegacyActivities()
    {
        var context = new Mock<TaskOrchestrationContext>(MockBehavior.Strict);
        var sequence = new MockSequence();
        SetupActivity(
            context,
            sequence,
            nameof(RunDeepValidationsDurable.ProcessAkuiteoContactSyncOperations));
        SetupActivity(context, sequence, nameof(RunDeepValidationsDurable.RunContactsDeepValidations));
        SetupActivity(context, sequence, nameof(RunDeepValidationsDurable.RunAccountsDeepValidations));
        SetupActivity(context, sequence, nameof(RunDeepValidationsDurable.RunRolesDeepValidations));
        SetupActivity(context, sequence, nameof(RunDeepValidationsDurable.TriggerOrchestrationProcess));

        await CreateFunction().RunOrchestrator(context.Object);

        context.VerifyAll();
    }

    [Fact]
    public async Task RunOrchestrator_WhenAkuiteoActivityFails_ShouldContinueLegacyActivities()
    {
        var context = new Mock<TaskOrchestrationContext>(MockBehavior.Loose);
        context
            .Setup(orchestrationContext => orchestrationContext.CallActivityAsync<Task>(
                nameof(RunDeepValidationsDurable.ProcessAkuiteoContactSyncOperations),
                string.Empty,
                null))
            .ThrowsAsync(new TaskFailedException(
                nameof(RunDeepValidationsDurable.ProcessAkuiteoContactSyncOperations),
                1,
                new InvalidOperationException("Simulated Akuiteo activity failure.")));
        SetupSuccessfulActivity(context, nameof(RunDeepValidationsDurable.RunContactsDeepValidations));
        SetupSuccessfulActivity(context, nameof(RunDeepValidationsDurable.RunAccountsDeepValidations));
        SetupSuccessfulActivity(context, nameof(RunDeepValidationsDurable.RunRolesDeepValidations));
        SetupSuccessfulActivity(context, nameof(RunDeepValidationsDurable.TriggerOrchestrationProcess));

        await CreateFunction().RunOrchestrator(context.Object);

        context.Verify(orchestrationContext => orchestrationContext.CallActivityAsync<Task>(
            nameof(RunDeepValidationsDurable.RunContactsDeepValidations),
            string.Empty,
            null), Times.Once);
        context.Verify(orchestrationContext => orchestrationContext.CallActivityAsync<Task>(
            nameof(RunDeepValidationsDurable.RunAccountsDeepValidations),
            string.Empty,
            null), Times.Once);
        context.Verify(orchestrationContext => orchestrationContext.CallActivityAsync<Task>(
            nameof(RunDeepValidationsDurable.RunRolesDeepValidations),
            string.Empty,
            null), Times.Once);
        context.Verify(orchestrationContext => orchestrationContext.CallActivityAsync<Task>(
            nameof(RunDeepValidationsDurable.TriggerOrchestrationProcess),
            string.Empty,
            null), Times.Once);
    }

    [Fact]
    public async Task ProcessAkuiteoContactSyncOperations_ShouldProcessPendingOperations()
    {
        var syncOperationServiceMock = new Mock<IAkuiteoContactSyncOperationService>();
        syncOperationServiceMock
            .Setup(service => service.ProcessPendingOperationsAsync())
            .Returns(Task.CompletedTask);
        var function = CreateFunction(syncOperationServiceMock);

        await function.ProcessAkuiteoContactSyncOperations(string.Empty);

        syncOperationServiceMock.Verify(
            service => service.ProcessPendingOperationsAsync(),
            Times.Once);
    }

    [Fact]
    public async Task ProcessAkuiteoContactSyncOperations_WhenFeatureIsDisabled_ShouldNotProcessPendingOperations()
    {
        var syncOperationServiceMock = new Mock<IAkuiteoContactSyncOperationService>();
        var featureFlagServiceMock = new Mock<IFeatureFlagService>();
        featureFlagServiceMock
            .Setup(service => service.IsEnabled(FeatureFlagKeys.IsContactAkuiteoSynchronizationEnabled))
            .Returns(false);
        var function = CreateFunction(syncOperationServiceMock, featureFlagServiceMock);

        await function.ProcessAkuiteoContactSyncOperations(string.Empty);

        syncOperationServiceMock.Verify(
            service => service.ProcessPendingOperationsAsync(),
            Times.Never);
    }

    [Fact]
    public async Task RunOrchestrator_WhenContactsActivityFails_ShouldNotRunFollowingLegacyActivities()
    {
        var context = new Mock<TaskOrchestrationContext>(MockBehavior.Loose);
        SetupSuccessfulActivity(
            context,
            nameof(RunDeepValidationsDurable.ProcessAkuiteoContactSyncOperations));
        context
            .Setup(orchestrationContext => orchestrationContext.CallActivityAsync<Task>(
                nameof(RunDeepValidationsDurable.RunContactsDeepValidations),
                string.Empty,
                null))
            .ThrowsAsync(new InvalidOperationException("Simulated contacts validation failure."));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateFunction().RunOrchestrator(context.Object));

        context.Verify(orchestrationContext => orchestrationContext.CallActivityAsync<Task>(
            nameof(RunDeepValidationsDurable.RunAccountsDeepValidations),
            It.IsAny<string>(),
            null), Times.Never);
        context.Verify(orchestrationContext => orchestrationContext.CallActivityAsync<Task>(
            nameof(RunDeepValidationsDurable.RunRolesDeepValidations),
            It.IsAny<string>(),
            null), Times.Never);
    }

    private static RunDeepValidationsDurable CreateFunction(
        Mock<IAkuiteoContactSyncOperationService>? syncOperationServiceMock = null,
        Mock<IFeatureFlagService>? featureFlagServiceMock = null)
    {
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock
            .Setup(factory => factory.CreateLogger(It.IsAny<string>()))
            .Returns(Mock.Of<ILogger<RunDeepValidationsDurable>>());
        if (featureFlagServiceMock is null)
        {
            featureFlagServiceMock = new Mock<IFeatureFlagService>();
            featureFlagServiceMock
                .Setup(service => service.IsEnabled(FeatureFlagKeys.IsContactAkuiteoSynchronizationEnabled))
                .Returns(true);
        }

        return new RunDeepValidationsDurable(
            loggerFactoryMock.Object,
            Mock.Of<IAccountDeepValidationService>(),
            Mock.Of<IContactsDeepValidationsService>(),
            Mock.Of<IRoleService>(),
            Mock.Of<IBackgroundJobEnqueuer>(),
            (syncOperationServiceMock ?? new Mock<IAkuiteoContactSyncOperationService>()).Object,
            featureFlagServiceMock.Object);
    }

    private static void SetupActivity(
        Mock<TaskOrchestrationContext> context,
        MockSequence sequence,
        string activityName)
    {
        context
            .InSequence(sequence)
            .Setup(orchestrationContext => orchestrationContext.CallActivityAsync<Task>(
                activityName,
                string.Empty,
                null))
            .ReturnsAsync(Task.CompletedTask);
    }

    private static void SetupSuccessfulActivity(
        Mock<TaskOrchestrationContext> context,
        string activityName)
    {
        context
            .Setup(orchestrationContext => orchestrationContext.CallActivityAsync<Task>(
                activityName,
                string.Empty,
                null))
            .ReturnsAsync(Task.CompletedTask);
    }
}
