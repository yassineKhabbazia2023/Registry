// <copyright file="ProcessRegistryData.cs" company="Pulse">
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
using Pulse.ContactRegistry.Infrastructure.Context;
using System.Diagnostics.CodeAnalysis;

namespace ContactRegistry.AzureFuctions.Functions.Registry;

/// <summary>
/// ProcessRegistryData.
/// </summary>
[ExcludeFromCodeCoverage]
public class ProcessRegistryData
{
    private readonly IDbContextFactory<RefContext> dbContextFactory;
    private readonly INotificationManager notificationManager;
    private readonly IReplaySafeLoggerAdapter loggerFactory;
    private readonly IRegProcessDeltaTriggerRepository processDeltaTriggerRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessRegistryData"/> class.
    /// </summary>
    /// <param name="logger">logger.</param>
    /// <param name="contextFactory">contextFactory.</param>
    /// <param name="notificationManager">notificationManager.</param>
    /// <param name="loggerFactory">loggerFactory.</param>
    /// <param name="processDeltaTriggerRepository">processDeltaTriggerRepository.</param>
    public ProcessRegistryData(
        IDbContextFactory<RefContext> contextFactory,
        INotificationManager notificationManager,
        IReplaySafeLoggerAdapter loggerFactory,
        IRegProcessDeltaTriggerRepository processDeltaTriggerRepository)
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
    [Function("ProcessRegistryData")]
    public async Task RunOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        ILogger logger = this.loggerFactory.CreateReplaySafeLogger(context, nameof(ProcessRegistryData));

        var parallelTasks = new List<Task>();

        Task accountTask = context.CallActivityAsync(nameof(this.ProcessRefAccountDataAsync), string.Empty);
        parallelTasks.Add(accountTask);
        Task contactTask = context.CallActivityAsync(nameof(this.ProcessRefContactDataAsync), string.Empty);
        parallelTasks.Add(contactTask);

        await Task.WhenAll(parallelTasks);

        await context.CallActivityAsync(nameof(this.ProcessRefRoleDataAsync), string.Empty);
        await context.CallActivityAsync(nameof(this.ProcessDeleteRefDataAsync), string.Empty);
    }

    /// <summary>
    /// Activity of processing Account data.
    /// </summary>
    /// <param name="input">input.</param>
    /// <param name="executionContext">executionContext.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    [Function(nameof(ProcessRefAccountDataAsync))]
    public async Task ProcessRefAccountDataAsync([ActivityTrigger] string input, FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger(nameof(this.ProcessRefAccountDataAsync));

        logger.LogInformation("ProcessRefAccountDataAsync Activity trigger function executed at: {date}", DateTime.UtcNow);
        using var applicationContext = await this.dbContextFactory.CreateDbContextAsync();
        await applicationContext.Database.ExecuteSqlRawAsync("EXEC [reg].[ManageAccount]");

        var message = new RegistryEntityType { EntityType = OperationType.Account };
        await this.notificationManager.PublishToQueueAsync(message);
        await this.processDeltaTriggerRepository.UpdateAccountProcessAsync(true);
        logger.LogInformation("ProcessRefAccountDataAsync Activity trigger function succeed at: {date}", DateTime.UtcNow);
    }

    /// <summary>
    /// Activity of processing Contact data.
    /// </summary>
    /// <param name="input">input.</param>
    /// <param name="executionContext">executionContext.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    [Function(nameof(ProcessRefContactDataAsync))]
    public async Task ProcessRefContactDataAsync([ActivityTrigger] string input, FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger(nameof(this.ProcessRefContactDataAsync));

        logger.LogInformation("ProcessRefContactDataAsync Activity trigger function executed at: {date}", DateTime.UtcNow);
        using var applicationContext = await this.dbContextFactory.CreateDbContextAsync();
        await applicationContext.Database.ExecuteSqlRawAsync("EXEC [reg].[ManageContact]");

        var message = new RegistryEntityType { EntityType = OperationType.Contact };
        await this.notificationManager.PublishToQueueAsync(message);
        await this.processDeltaTriggerRepository.UpdateContactProcessAsync(true);
        logger.LogInformation("ProcessRefContactDataAsync Activity trigger function succeed at: {date}", DateTime.UtcNow);
    }

    /// <summary>
    /// Activity of processing Role data.
    /// </summary>
    /// <param name="input">input.</param>
    /// <param name="executionContext">executionContext.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    [Function(nameof(ProcessRefRoleDataAsync))]
    public async Task ProcessRefRoleDataAsync([ActivityTrigger] string input, FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger(nameof(this.ProcessRefRoleDataAsync));

        logger.LogInformation("ProcessRefRoleDataAsync Activity trigger function executed at: {date}", DateTime.UtcNow);
        using var applicationContext = await this.dbContextFactory.CreateDbContextAsync();
        await applicationContext.Database.ExecuteSqlRawAsync("EXEC [reg].[ManageRole]");

        var message = new RegistryEntityType { EntityType = OperationType.Role };
        await this.notificationManager.PublishToQueueAsync(message);
        await this.processDeltaTriggerRepository.UpdateRoleProcessAsync(true);
        logger.LogInformation("ProcessRefRoleDataAsync Activity trigger function succeed at: {date}", DateTime.UtcNow);
    }

    /// <summary>
    /// Activity of processing deleting row data.
    /// </summary>
    /// <param name="input">input.</param>
    /// <param name="executionContext">executionContext.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    [Function(nameof(ProcessDeleteRefDataAsync))]
    public async Task ProcessDeleteRefDataAsync([ActivityTrigger] string input, FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger(nameof(this.ProcessDeleteRefDataAsync));
        logger.LogInformation("ProcessDeleteRefDataAsync Activity trigger function executed at: {date}", DateTime.UtcNow);
        using var applicationContext = await this.dbContextFactory.CreateDbContextAsync();
        await applicationContext.RefContactEntity.ExecuteDeleteAsync();
        await applicationContext.RefAccountEntity.ExecuteDeleteAsync();
        await applicationContext.RefRoleEntity.ExecuteDeleteAsync();
        logger.LogInformation("ProcessDeleteRefDataAsync Activity trigger function succeed at: {date}", DateTime.UtcNow);
    }

    /// <summary>
    /// Triggered by a timer to start orchestration referential data process.
    /// </summary>
    /// <param name="myTimer">Timer information, including the schedule.</param>
    /// <param name="client">Durable orchestration client to start orchestrations.</param>
    /// <param name="executionContext">Logger instance for logging purpose.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Function(nameof(Run))]
    public async Task Run(
     [TimerTrigger("%ProcessRefDataRunSchedule%")] TimerInfo myTimer,
     [DurableClient] DurableTaskClient client,
     FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger("Run");
        logger.LogInformation("Started statuses monitoring hourly run : {myTimer}", myTimer);
        string instanceId = await client!.ScheduleNewOrchestrationInstanceAsync(
            "ProcessRegistryData",
            string.Empty);
        logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);
    }
}
