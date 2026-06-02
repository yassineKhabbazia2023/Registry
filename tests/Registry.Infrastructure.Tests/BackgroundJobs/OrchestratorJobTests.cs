using Application.Interfaces;
using Application.Options;
using Hangfire;
using Hangfire.MemoryStorage;
using Infrastructure.BackgroundJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Registry.Infrastructure.Tests.BackgroundJobs
{
    public class OrchestratorJobTests : IDisposable
    {
        private readonly Mock<IAccountOrchestrator> _mockAccountOrchestrator;
        private readonly Mock<IContactOrchestrator> _mockContactOrchestrator;
        private readonly Mock<IRoleOrchestrator> _mockRoleOrchestrator;
        private readonly Mock<IReviewService> _mockReviewService;
        private readonly Mock<ILogger<OrchestratorJob>> _mockLogger;
        private readonly OrchestratorJob _orchestratorJob;
        private readonly Mock<IOptions<BackGroundJobOptions>> _mockOptions;

        public OrchestratorJobTests()
        {
            _mockAccountOrchestrator = new Mock<IAccountOrchestrator>();
            _mockContactOrchestrator = new Mock<IContactOrchestrator>();
            _mockRoleOrchestrator = new Mock<IRoleOrchestrator>();
            _mockReviewService = new Mock<IReviewService>();
            _mockLogger = new Mock<ILogger<OrchestratorJob>>();
            _mockOptions = new Mock<IOptions<BackGroundJobOptions>>();

            _mockOptions.Setup(x => x.Value).Returns(new BackGroundJobOptions
            {
                Chunk = 1000,
                ShouldTriggerEvents = true,
                TimeToWaitBeforeEachStep = 4000
            });

            var serviceProvider = new ServiceCollection()
                .AddSingleton(_mockAccountOrchestrator.Object)
                .AddSingleton(_mockContactOrchestrator.Object)
                .AddSingleton(_mockRoleOrchestrator.Object)
                .AddSingleton(_mockReviewService.Object)
                .AddSingleton(_mockLogger.Object)
                .AddSingleton(_mockOptions.Object)
                .BuildServiceProvider();

            JobStorage.Current = new MemoryStorage();
            GlobalConfiguration.Configuration
                .UseActivator(new TestJobActivator(serviceProvider))
                .UseFilter(new TestJobFilter());

            _orchestratorJob = new OrchestratorJob(
                _mockAccountOrchestrator.Object,
                _mockContactOrchestrator.Object,
                _mockOptions.Object,
                _mockRoleOrchestrator.Object,
                _mockLogger.Object,
                _mockReviewService.Object
            );
        }

        [Fact]
        public async Task ProcessOrder_ShouldEnqueueJobsInCorrectOrder_WhenShouldTriggerEventsIsTrue()
        {
            TestJobFilter.CreatedJobs.Clear(); 

            await _orchestratorJob.ProcessOrder();

            var expectedJobs = new List<string>
            {
                nameof(IReviewService.ReviewChangeEmailAsync),
                nameof(IContactOrchestrator.ProcessContactPublishAsync), 
                nameof(IAccountOrchestrator.ProcessAccountPublishAsync),  
                nameof(OrchestratorJob.WaitFor),
                nameof(IRoleOrchestrator.ProcessRolePublishAsync),        
                nameof(IContactOrchestrator.ProcessContactPublishAsync),  
                nameof(IAccountOrchestrator.ProcessAccountPublishAsync),  
                nameof(IRoleOrchestrator.ProcessRolePublishAsync),        
                nameof(IRoleOrchestrator.ProcessRolePublishAsync),        
                nameof(IContactOrchestrator.ProcessContactPublishAsync),  
                nameof(IAccountOrchestrator.ProcessAccountPublishAsync)   
            };

            Assert.Equal(expectedJobs.Count, TestJobFilter.CreatedJobs.Count);
            Assert.Equal(expectedJobs, TestJobFilter.CreatedJobs);
        }

        [Fact]
        public async Task ProcessOrder_ShouldNotEnqueueDeleteJobs_WhenShouldTriggerEventsIsFalse()
        {
            // Arrange
            _mockOptions.Setup(x => x.Value).Returns(new BackGroundJobOptions
            {
                Chunk = 1000,
                ShouldTriggerEvents = false,
                TimeToWaitBeforeEachStep = 4000
            });

            var orchestratorJob = new OrchestratorJob(
                _mockAccountOrchestrator.Object,
                _mockContactOrchestrator.Object,
                _mockOptions.Object,
                _mockRoleOrchestrator.Object,
                _mockLogger.Object,
                _mockReviewService.Object
            );

            TestJobFilter.CreatedJobs.Clear();

            await orchestratorJob.ProcessOrder();

            var expectedJobs = new List<string>
            {
                nameof(IReviewService.ReviewChangeEmailAsync),
                nameof(IContactOrchestrator.ProcessContactPublishAsync),  
                nameof(IAccountOrchestrator.ProcessAccountPublishAsync),  
                nameof(OrchestratorJob.WaitFor),
                nameof(IRoleOrchestrator.ProcessRolePublishAsync),
                nameof(IContactOrchestrator.ProcessContactPublishAsync),  
                nameof(IAccountOrchestrator.ProcessAccountPublishAsync),
                nameof(IRoleOrchestrator.ProcessRolePublishAsync)
            };

            Assert.Equal(expectedJobs.Count, TestJobFilter.CreatedJobs.Count);
            Assert.Equal(expectedJobs, TestJobFilter.CreatedJobs);
        }

        [Fact]
        public async Task ProcessOrder_ShouldLogStartMessage()
        {
            await _orchestratorJob.ProcessOrder();

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v != null && 
                        v.ToString()!.Contains("ProcessOrder started")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ProcessOrder_ShouldLogFinishMessage()
        {
            await _orchestratorJob.ProcessOrder();

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v != null && 
                        v.ToString()!.Contains("finished")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }


        [Fact]
        public async Task WaitFor_ShouldUseProvidedTime_WhenMillisecondsIsGreaterThanZero()
        {
            var startTime = DateTime.UtcNow;
            var waitTime = 100; // 100ms for fast test

            await _orchestratorJob.WaitFor(waitTime);

            var endTime = DateTime.UtcNow;
            var elapsed = (endTime - startTime).TotalMilliseconds;

            Assert.True(elapsed >= waitTime);
            Assert.True(elapsed < waitTime + 100); // Allow some tolerance
        }

        [Fact]
        public async Task ProcessOrder_ShouldLogStartMessageWithCorrectFormat()
        {
            await _orchestratorJob.ProcessOrder();

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v!= null && 
                        v.ToString()!.Contains("ProcessOrder started at")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ProcessOrder_ShouldLogFinishMessageWithCorrectFormat()
        {
            await _orchestratorJob.ProcessOrder();

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v != null && 
                        v.ToString()!.Contains("ProcessOrder finished at")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ProcessOrder_ShouldLogBothStartAndFinishMessages()
        {
            await _orchestratorJob.ProcessOrder();

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v != null &&
                        v.ToString()!.Contains("ProcessOrder started at") &&
                        v.ToString()!.Contains("ProcessOrder")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once,
                "Start log message should be called once");

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v != null &&
                        v.ToString()!.Contains("ProcessOrder finished at") &&
                        v.ToString()!.Contains("ProcessOrder")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once,
                "Finish log message should be called once");

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Exactly(2),
                "Should log exactly 2 information messages");
        }

        [Fact]
        public async Task PublishApprovedRolesInstantlyAsync_ShouldDelegateToRoleOrchestrator()
        {
            // Arrange
            _mockRoleOrchestrator.Setup(r => r.PublishApprovedRoleInsertsAsync()).Returns(Task.CompletedTask);

            // Act
            await _orchestratorJob.PublishApprovedRolesInstantlyAsync();

            // Assert
            _mockRoleOrchestrator.Verify(r => r.PublishApprovedRoleInsertsAsync(), Times.Once);
        }

        public void Dispose()
        {
            TestJobFilter.CreatedJobs.Clear();
            JobStorage.Current = null;
            GC.SuppressFinalize(this);
        }
    }
}