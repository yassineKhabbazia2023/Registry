// <copyright file="ProcessReferentialData.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using ContactRegistry.AzureFuctions.Logging;
using ContactRegistry.AzureFuctions.Managers;
using ContactRegistry.AzureFuctions.Message;
using Infrastructure.Context;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace ContactRegistry.AzureFuctions.Functions
{
    /// <summary>
    /// ProcessReferentialData.
    /// </summary>
    public class ProcessReferentialData
    {
        private readonly IDbContextFactory<ApplicationDbContext> dbContextFactory;
        private readonly INotificationManager notificationManager;
        private readonly IReplaySafeLoggerAdapter loggerFactory;
        private readonly IProcessDeltaTriggerRepository processDeltaTriggerRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessReferentialData"/> class.
        /// </summary>
        /// <param name="logger">logger.</param>
        /// <param name="contextFactory">contextFactory.</param>
        /// <param name="notificationManager">notificationManager.</param>
        /// <param name="loggerFactory">loggerFactory.</param>
        /// <param name="processDeltaTriggerRepository">processDeltaTriggerRepository.</param>
        public ProcessReferentialData(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            INotificationManager notificationManager,
            IReplaySafeLoggerAdapter loggerFactory,
            IProcessDeltaTriggerRepository processDeltaTriggerRepository)
        {
            this.dbContextFactory = contextFactory;
            this.notificationManager = notificationManager;
            this.loggerFactory = loggerFactory;
            this.processDeltaTriggerRepository = processDeltaTriggerRepository;
        }

        /// <summary>
        /// Orchestrator for processing referential data.
        /// </summary>
        /// <param name="context">instance of the <see cref="IDurableOrchestrationContext"/> class.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        [Function("ProcessReferentialData")]
        public async Task RunOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
        {
            ILogger logger = this.loggerFactory.CreateReplaySafeLogger(context, nameof(ProcessReferentialData));

            var parallelTasks = new List<Task>();

            Task accountTask = context.CallActivityAsync(nameof(this.ProcessAccountDataAsync), string.Empty);
            parallelTasks.Add(accountTask);
            Task contactTask = context.CallActivityAsync(nameof(this.ProcessContactDataAsync), string.Empty);
            parallelTasks.Add(contactTask);

            await Task.WhenAll(parallelTasks);

            await context.CallActivityAsync(nameof(this.ProcessRoleDataAsync), string.Empty);

            await context.CallActivityAsync(nameof(this.ProcessDeleteAlxDataAsync), string.Empty);
        }

        /// <summary>
        /// Activity of processing Account data.
        /// </summary>
        /// <param name="input">input.</param>
        /// <param name="executionContext">executionContext.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        [ExcludeFromCodeCoverage]
        [Function(nameof(ProcessAccountDataAsync))]
        public async Task ProcessAccountDataAsync([ActivityTrigger] string input, FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger(nameof(this.ProcessAccountDataAsync));

            var canProcess = await this.processDeltaTriggerRepository.GetProcessAsync();

            if (!canProcess.Account)
            {
                logger.LogInformation("ProcessAccountDataAsync Activity trigger function stop at: {date}", DateTime.UtcNow);
                return;
            }

            logger.LogInformation("ProcessAccountDataAsync Activity trigger function executed at: {date}", DateTime.UtcNow);
            using var applicationContext = await this.dbContextFactory.CreateDbContextAsync();
            await applicationContext.Database.ExecuteSqlRawAsync("EXEC [cre].[ManageAccountDelta]");

            var message = new RegistryEntityType { EntityType = OperationType.Account };
            await this.notificationManager.PublishToQueueAsync(message);
            await this.processDeltaTriggerRepository.UpdateAccountProcessAsync(false);
            logger.LogInformation("ProcessAccountDataAsync Activity trigger function succeed at: {date}", DateTime.UtcNow);
        }

        /// <summary>
        /// Activity of processing Contact data.
        /// </summary>
        /// <param name="input">input.</param>
        /// <param name="executionContext">executionContext.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        [ExcludeFromCodeCoverage]
        [Function(nameof(ProcessContactDataAsync))]
        public async Task ProcessContactDataAsync([ActivityTrigger] string input, FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger(nameof(this.ProcessContactDataAsync));

            var canProcess = await this.processDeltaTriggerRepository.GetProcessAsync();

            if (!canProcess.Contact)
            {
                logger.LogInformation("ProcessContactDataAsync Activity trigger function stop at: {date}", DateTime.UtcNow);
                return;
            }

            logger.LogInformation("ProcessContactDataAsync Activity trigger function executed at: {date}", DateTime.UtcNow);
            using var applicationContext = await this.dbContextFactory.CreateDbContextAsync();
            await applicationContext.Database.ExecuteSqlRawAsync("EXEC [cre].[ManageContactDelta]");

            var message = new RegistryEntityType { EntityType = OperationType.Contact };
            await this.notificationManager.PublishToQueueAsync(message);
            await this.processDeltaTriggerRepository.UpdateContactProcessAsync(false);
            logger.LogInformation("ProcessContactDataAsync Activity trigger function succeed at: {date}", DateTime.UtcNow);
        }

        /// <summary>
        /// Activity of processing Role data.
        /// </summary>
        /// <param name="input">input.</param>
        /// <param name="executionContext">executionContext.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        [ExcludeFromCodeCoverage]
        [Function(nameof(ProcessRoleDataAsync))]
        public async Task ProcessRoleDataAsync([ActivityTrigger] string input, FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger(nameof(this.ProcessRoleDataAsync));
            var canProcess = await this.processDeltaTriggerRepository.GetProcessAsync();

            if (!canProcess.Role)
            {
                logger.LogInformation("ProcessRoleDataAsync Activity trigger function stop at: {date}", DateTime.UtcNow);
                return;
            }

            logger.LogInformation("ProcessRoleDataAsync Activity trigger function executed at: {date}", DateTime.UtcNow);
            using var applicationContext = await this.dbContextFactory.CreateDbContextAsync();
            await applicationContext.Database.ExecuteSqlRawAsync("EXEC [cre].[ManageRoleDelta]");

            var message = new RegistryEntityType { EntityType = OperationType.Role };
            await this.notificationManager.PublishToQueueAsync(message);
            await this.processDeltaTriggerRepository.UpdateRoleProcessAsync(false);
            logger.LogInformation("ProcessRoleDataAsync Activity trigger function succeed at: {date}", DateTime.UtcNow);
        }

        /// <summary>
        /// Activity of processing deleting row data.
        /// </summary>
        /// <param name="input">input.</param>
        /// <param name="executionContext">executionContext.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        [ExcludeFromCodeCoverage]
        [Function(nameof(ProcessDeleteAlxDataAsync))]
        public async Task ProcessDeleteAlxDataAsync([ActivityTrigger] string input, FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger(nameof(this.ProcessDeleteAlxDataAsync));
            logger.LogInformation("ProcessDeleteAlxDataAsync Activity trigger function executed at: {date}", DateTime.UtcNow);
            using var applicationContext = await this.dbContextFactory.CreateDbContextAsync();
            await applicationContext.AlxRoles.ExecuteDeleteAsync();
            await applicationContext.AlxAccounts.ExecuteDeleteAsync();
            await applicationContext.AlxContacts.ExecuteDeleteAsync();
            logger.LogInformation("ProcessDeleteAlxDataAsync Activity trigger function succeed at: {date}", DateTime.UtcNow);
        }

        /// <summary>
        /// Triggered by a timer to start orchestration referential data process.
        /// </summary>
        /// <param name="myTimer">Timer information, including the schedule.</param>
        /// <param name="client">Durable orchestration client to start orchestrations.</param>
        /// <param name="executionContext">Logger instance for logging purpose.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [ExcludeFromCodeCoverage]
        [Function(nameof(Run))]
        public async Task Run(
         [TimerTrigger("%ProcessRefDataRunSchedule%")] TimerInfo myTimer,
         [DurableClient] DurableTaskClient client,
         FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("Run");
            logger.LogInformation("Started statuses monitoring hourly run : {myTimer}", myTimer);
            string instanceId = await client!.ScheduleNewOrchestrationInstanceAsync(
                "ProcessReferentialData",
                string.Empty);
            logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);
        }
    }
}
