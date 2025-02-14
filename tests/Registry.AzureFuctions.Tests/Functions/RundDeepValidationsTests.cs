using System.Globalization;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using Moq;
using Registry.AzureFuctions.Functions;
using Application.Interfaces;
using Infrastructure.BackgroundJobs;
using Infrastructure.Adapters;
using System.Linq.Expressions;
using Hangfire;

namespace Registry.AzureFunctions.Tests
{
    public class RunDeepValidationsDurableTests
    {
        [Fact]
        public async Task RunOrchestrator()
        {
            // Arrange
            var culture = new CultureInfo("fr-FR", true);
            var date = System.DateTime.Today.AddDays(-1).ToString("d MMMM yyyy", culture);

            var sequenceActivities = new MockSequence();

            var context = new Mock<TaskOrchestrationContext>(MockBehavior.Loose);

            context.InSequence(sequenceActivities)
                .Setup(acc => acc.CallActivityAsync<Task>(
                    nameof(RunDeepValidationsDurable.RunContactsDeepValidations),
                    string.Empty,
                    null))
                .ReturnsAsync(Task.CompletedTask);

            context.InSequence(sequenceActivities)
                .Setup(acc => acc.CallActivityAsync<Task>(
                    nameof(RunDeepValidationsDurable.RunAccountsDeepValidations),
                    string.Empty,
                    null))
                .ReturnsAsync(Task.CompletedTask);

            context.InSequence(sequenceActivities)
                .Setup(acc => acc.CallActivityAsync<Task>(
                    nameof(RunDeepValidationsDurable.RunRolesDeepValidations),
                    string.Empty,
                    null))
                .ReturnsAsync(Task.CompletedTask);
            context.InSequence(sequenceActivities)
                 .Setup(acc => acc.CallActivityAsync<Task>(
                     nameof(RunDeepValidationsDurable.TriggerOrchestrationProcess),
                     string.Empty,
                     null)).ReturnsAsync(Task.CompletedTask);

            var contactsDeepValidationsServiceMock = new Mock<IContactsDeepValidationsService>(MockBehavior.Strict);
            contactsDeepValidationsServiceMock.Setup(s => s.CreateValidContactsOperationsAsync())
                .Returns(Task.CompletedTask);

            var accountDeepValidationServiceMock = new Mock<IAccountDeepValidationService>(MockBehavior.Strict);
            accountDeepValidationServiceMock.Setup(s => s.CreateValidAccountsOperationsAsync())
                .Returns(Task.CompletedTask);

            var roleServiceMock = new Mock<IRoleService>(MockBehavior.Strict);
            roleServiceMock.Setup(s => s.CreateValidRolesOperationsAsync())
                .ReturnsAsync(new List<bool>());

            var backgroundJobEnqueuerMock = new Mock<IBackgroundJobEnqueuer>(MockBehavior.Strict);
            backgroundJobEnqueuerMock
                .Setup(enqueuer => enqueuer.Enqueue<OrchestratorJob>(It.Is<Expression<Action<OrchestratorJob>>>(expr => expr.ToString().Contains("ProcessOrder"))))
                .Returns("job-id");

            var mockLoggerFactory = new Mock<ILoggerFactory>();
            var mockLogger = new Mock<ILogger<RunDeepValidationsDurable>>();
            mockLoggerFactory
                .Setup(factory => factory.CreateLogger(It.IsAny<string>()))
                .Returns(mockLogger.Object);

            // Act
            var orchestrator = new RunDeepValidationsDurable(
                mockLoggerFactory.Object,
                accountDeepValidationServiceMock.Object,
                contactsDeepValidationsServiceMock.Object,
                roleServiceMock.Object,
                backgroundJobEnqueuerMock.Object);
            await orchestrator.RunOrchestrator(context.Object);

            // Assert
            context.Verify(acc => acc.CallActivityAsync<Task>(
                nameof(RunDeepValidationsDurable.RunContactsDeepValidations),
                string.Empty,
                null),
                Times.Exactly(1));

            context.Verify(acc => acc.CallActivityAsync<Task>(
                nameof(RunDeepValidationsDurable.RunAccountsDeepValidations),
                string.Empty,
                null),
                Times.Exactly(1));

            context.Verify(acc => acc.CallActivityAsync<Task>(
                nameof(RunDeepValidationsDurable.RunRolesDeepValidations),
                string.Empty,
                null),
                Times.Exactly(1));

            backgroundJobEnqueuerMock
            .Setup(enqueuer => enqueuer.Enqueue(It.IsAny<Expression<Action<OrchestratorJob>>>()))
            .Returns("job-id");
        }

        [Fact]
        public async Task RunOrchestrator_WhenFirstActivityFails_DoesNotTriggerSubsequentActivities()
        {
            // Arrange
            var context = new Mock<TaskOrchestrationContext>(MockBehavior.Loose);

            context
                .Setup(acc => acc.CallActivityAsync<Task>(
                    nameof(RunDeepValidationsDurable.RunContactsDeepValidations),
                    string.Empty,
                    null))
                .ThrowsAsync(new Exception("Simulated failure in contacts validation"));


            var contactsDeepValidationsServiceMock = new Mock<IContactsDeepValidationsService>(MockBehavior.Strict);
            var accountDeepValidationServiceMock = new Mock<IAccountDeepValidationService>(MockBehavior.Strict);
            var roleServiceMock = new Mock<IRoleService>(MockBehavior.Strict);

            var mockLoggerFactory = new Mock<ILoggerFactory>();
            var mockLogger = new Mock<ILogger<RunDeepValidationsDurable>>();
            mockLoggerFactory
                .Setup(factory => factory.CreateLogger(It.IsAny<string>()))
                .Returns(mockLogger.Object);

            var backgroundJobEnqueuerMock = new Mock<IBackgroundJobEnqueuer>(MockBehavior.Strict);

            var orchestrator = new RunDeepValidationsDurable(
                mockLoggerFactory.Object,
                accountDeepValidationServiceMock.Object,
                contactsDeepValidationsServiceMock.Object,
                roleServiceMock.Object,
                backgroundJobEnqueuerMock.Object);

            // Assert
            await Assert.ThrowsAsync<Exception>(() => orchestrator.RunOrchestrator(context.Object));


            context.Verify(acc => acc.CallActivityAsync<Task>(
                nameof(RunDeepValidationsDurable.RunContactsDeepValidations),
                string.Empty,
                null),
                Times.Once);

            context.Verify(acc => acc.CallActivityAsync<Task>(
                nameof(RunDeepValidationsDurable.RunAccountsDeepValidations),
                It.IsAny<string>(),
                null),
                Times.Never);

            context.Verify(acc => acc.CallActivityAsync<Task>(
                nameof(RunDeepValidationsDurable.RunRolesDeepValidations),
                It.IsAny<string>(),
                null),
                Times.Never);
        }

        [Fact]
        public async Task RunOrchestrator_WhenSecondActivityFails_DoesNotTriggerRoleActivity()
        {
            // Arrange
            var context = new Mock<TaskOrchestrationContext>(MockBehavior.Loose);

            context
                .Setup(acc => acc.CallActivityAsync<Task>(
                    nameof(RunDeepValidationsDurable.RunContactsDeepValidations),
                    string.Empty,
                    null)).ReturnsAsync(Task.CompletedTask);

            context
                 .Setup(acc => acc.CallActivityAsync<Task>(
                     nameof(RunDeepValidationsDurable.RunAccountsDeepValidations),
                     string.Empty,
                     null)).ThrowsAsync(new Exception("Simulated failure in contacts validation"));


            var contactsDeepValidationsServiceMock = new Mock<IContactsDeepValidationsService>(MockBehavior.Strict);
            var accountDeepValidationServiceMock = new Mock<IAccountDeepValidationService>(MockBehavior.Strict);
            var roleServiceMock = new Mock<IRoleService>(MockBehavior.Strict);

            var mockLoggerFactory = new Mock<ILoggerFactory>();
            var mockLogger = new Mock<ILogger<RunDeepValidationsDurable>>();
            mockLoggerFactory
                .Setup(factory => factory.CreateLogger(It.IsAny<string>()))
                .Returns(mockLogger.Object);

            var backgroundJobEnqueuerMock = new Mock<IBackgroundJobEnqueuer>(MockBehavior.Strict);

            var orchestrator = new RunDeepValidationsDurable(
                mockLoggerFactory.Object,
                accountDeepValidationServiceMock.Object,
                contactsDeepValidationsServiceMock.Object,
                roleServiceMock.Object,
                backgroundJobEnqueuerMock.Object);

            // Assert
            await Assert.ThrowsAsync<Exception>(() => orchestrator.RunOrchestrator(context.Object));


            context.Verify(acc => acc.CallActivityAsync<Task>(
                nameof(RunDeepValidationsDurable.RunContactsDeepValidations),
                string.Empty,
                null),
                Times.Once);

            context.Verify(acc => acc.CallActivityAsync<Task>(
                nameof(RunDeepValidationsDurable.RunAccountsDeepValidations),
                It.IsAny<string>(),
                null),
                Times.Once);

            context.Verify(acc => acc.CallActivityAsync<Task>(
                nameof(RunDeepValidationsDurable.RunRolesDeepValidations),
                It.IsAny<string>(),
                null),
                Times.Never);
        }
    }
}
