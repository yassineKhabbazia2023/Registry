// <copyright file="OrchestratorJobTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Options;
using Hangfire;
using Hangfire.MemoryStorage;
using Infrastructure.BackgroundJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace Registry.Infrastructure.Tests.BackgroundJobs
{
    public class OrchestratorJobTests
    {
        private readonly Mock<IAccountOrchestrator> _mockAccountOrchestrator;
        private readonly Mock<IContactOrchestrator> _mockContactOrchestrator;
        private readonly Mock<IRoleOrchestrator> _mockRoleOrchestrator;
        private readonly Mock<IReviewService> _mockReviewService;
        private readonly OrchestratorJob _orchestratorJob;
        private readonly Mock<IOptions<BackGroundJobOptions>> _options;

        public OrchestratorJobTests()
        {
            _mockAccountOrchestrator = new Mock<IAccountOrchestrator>();
            _mockContactOrchestrator = new Mock<IContactOrchestrator>();
            _mockRoleOrchestrator = new Mock<IRoleOrchestrator>();
            _mockReviewService = new Mock<IReviewService>();
            _options = new Mock<IOptions<BackGroundJobOptions>>();

            var serviceProvider = new ServiceCollection()
                .AddSingleton(_mockAccountOrchestrator.Object)
                .AddSingleton(_mockContactOrchestrator.Object)
                .AddSingleton(_mockRoleOrchestrator.Object)
                .AddSingleton(_mockReviewService.Object)
                .BuildServiceProvider();

            JobStorage.Current = new MemoryStorage();
            GlobalConfiguration.Configuration
                .UseActivator(new TestJobActivator(serviceProvider))
                .UseFilter(new TestJobFilter());

            _options.Setup(x => x.Value).Returns(new BackGroundJobOptions { Chunk = 1000, ShouldTriggerEvents = true, TimeToWaitBeforeEachStep = 4000 });

            _orchestratorJob = new OrchestratorJob(
                _mockRoleOrchestrator.Object,
                _mockAccountOrchestrator.Object,
                _mockContactOrchestrator.Object,
                _mockReviewService.Object,
                _options.Object
            );
        }

        [Fact]
        public async Task ProcessOrder_ShouldEnqueueJobsInCorrectOrder()
        {
           
            // Act
            await _orchestratorJob.ProcessOrder();

            // Assert
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

            Assert.Equal(expectedJobs, TestJobFilter.CreatedJobs);
        }
    }
}
