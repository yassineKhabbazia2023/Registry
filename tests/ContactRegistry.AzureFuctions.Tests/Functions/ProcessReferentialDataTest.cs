//using ContactRegistry.AzureFuctions.Functions;
//using ContactRegistry.AzureFuctions.Logging;
//using ContactRegistry.AzureFuctions.Managers;
//using ContactRegistry.Infrastructure.Tests.Utils;
//using Infrastructure.Context;
//using Microsoft.DurableTask;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Logging;
//using Moq;

//namespace ContactRegistry.AzureFuctions.Tests.Functions
//{
//    public class ProcessReferentialDataTest
//    {
//        private ApplicationDbContext dbContext;

//        public ProcessReferentialDataTest()
//        {
//            dbContext = DbContextMockExtensions.CreateInMemoryDbContext();
//        }

//        [Fact]
//        public async Task ProcessReferentialData_Orchestrator()
//        {
//            // Arrange
//            var dbContextFactory = new Mock<IDbContextFactory<ApplicationDbContext>>(MockBehavior.Strict);
//            dbContextFactory.Setup(d => d.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(dbContext);

//            var notificationManager = new Mock<INotificationManager>(MockBehavior.Strict);

//            var logger = new Mock<ILogger<ProcessReferentialData>>();
//            var loggerFactory = new Mock<IReplaySafeLoggerAdapter>();
//            loggerFactory.Setup(l => l.CreateReplaySafeLogger(It.IsAny<TaskOrchestrationContext>(), It.IsAny<string>())).Returns(logger.Object);

//            var context = new Mock<TaskOrchestrationContext>(MockBehavior.Loose);

//            var seq = new MockSequence();

//            context.InSequence(seq).Setup(x => x.CallActivityAsync(nameof(ProcessReferentialData.ProcessAccountDataAsync), It.IsAny<string>(), null))
//                 .Returns(Task.CompletedTask)
//                 .Verifiable();

//            context.InSequence(seq).Setup(x => x.CallActivityAsync(
//                 nameof(ProcessReferentialData.ProcessContactDataAsync),
//                 It.IsAny<string>(),
//                 null))
//                 .Returns(Task.CompletedTask)
//                 .Verifiable();

//            context.InSequence(seq).Setup(x => x.CallActivityAsync(
//                 nameof(ProcessReferentialData.ProcessRoleDataAsync),
//                 It.IsAny<string>(),
//                 null))
//                 .Returns(Task.CompletedTask)
//                 .Verifiable();

//            context.InSequence(seq).Setup(x => x.CallActivityAsync(
//                 nameof(ProcessReferentialData.ProcessDeleteAlxDataAsync),
//                 It.IsAny<string>(),
//                 null))
//                 .Returns(Task.CompletedTask)
//                 .Verifiable();

//            // Act
//            var processReferentialData = new ProcessReferentialData(dbContextFactory.Object, notificationManager.Object, loggerFactory.Object);
//            await processReferentialData.RunOrchestrator(context.Object);

//            // Assert
//            context.Verify(x => x.CallActivityAsync(nameof(ProcessReferentialData.ProcessAccountDataAsync), It.IsAny<string>(), null), Times.Once);
//            context.Verify(x => x.CallActivityAsync(nameof(ProcessReferentialData.ProcessContactDataAsync), It.IsAny<string>(), null), Times.Once);
//            context.Verify(x => x.CallActivityAsync(nameof(ProcessReferentialData.ProcessRoleDataAsync), It.IsAny<string>(), null), Times.Once);
//            context.Verify(x => x.CallActivityAsync(nameof(ProcessReferentialData.ProcessDeleteAlxDataAsync), It.IsAny<string>(), null), Times.Once);
//        }
//    }
//}
