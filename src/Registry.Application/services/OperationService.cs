// <copyright file="OperationService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Interfaces;
using Application.Models;
using Application.Options;
using Application.Requests;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pulse.Registry.Domain.Entities;

namespace Application.Services;


public class OperationService : IOperationService
{
    private readonly ILogger<OperationService> logger;
    private readonly BackGroundJobOptions options;
    private readonly IOperationRepository operationRepository;

    public OperationService(IOperationRepository roleRepository, ILogger<OperationService> logger, IOptions<BackGroundJobOptions> options)
    {
        this.operationRepository = roleRepository;
        this.logger = logger;
        this.options = options.Value;
    }

    public async Task<IEnumerable<RegOperationDetail?>> GetOperationsAsync(string accountNumber, OperationSearchCriteria operationSearchCriteria)
    {
        ArgumentNullException.ThrowIfNull(accountNumber);

        // Try getting operations via the concrete tables
        var operations = await operationRepository.GetOperationsByMainTablesAsync(accountNumber, operationSearchCriteria);

        // If no operations are found, fallback to using the alternative repository method
        if (!operations.Any())
        {
            operations = await operationRepository.GetOperationsByAccountMainAndContactRefTablesAsync(accountNumber, operationSearchCriteria);
        }

        // Return distinct operations based on Email
        return operations.DistinctBy(x => x.Email);
    }

    public async Task<RegOperation?> UpdateOperationAsync(int operationId, string email, RegOperation creOperation)
    {
        ArgumentNullException.ThrowIfNull(operationId);
        ArgumentNullException.ThrowIfNull(email);

        creOperation.LastStatusUpdatedBy = email;
        return await this.operationRepository.UpdateOperationAsync(operationId, creOperation);
    }

    public async Task<RegOperation?> GetOperationByIdAsync(int operationId)
    {
        return await this.operationRepository.GetOperationByIdAsync(operationId);
    }

    public async Task<bool> UpdateContactOperations(OperationSearchCriteria searchCriteria, string email)
    {
        IEnumerable<RegOperationEntity> operationList = await this.operationRepository.FindContactOperationAsync(searchCriteria, email);
        if (!operationList.Any())
        {
            logger.LogWarning($"[Method]: {nameof(UpdateContactOperations)} operation List is empty!");
            return true;
        }
        var updateSucceed = await this.operationRepository.UpdateOperationStatusListASync(ProcessStatus.Succeeded, operationList);
        if (!updateSucceed)
        {
            logger.LogError($"[Method]: {nameof(UpdateContactOperations)}; [Error]: something went wrong while updating list of operation!");
        }
        return updateSucceed;
    }

    public async Task TryToProceedUntilTimeoutAsync(string entityType, string operationtType)
    {
        var startTime = DateTime.UtcNow;
        int TimeToWait = this.options.TimeToWaitBeforeEachStep;
        var sentAccountInsertOperations = await this.operationRepository.FindSentOperationsAsync(entityType, operationtType);
        while (sentAccountInsertOperations.Count > 0 && (DateTime.UtcNow - startTime) < TimeSpan.FromMinutes(TimeToWait))
        {
            await Task.Delay(1000);
            sentAccountInsertOperations = await this.operationRepository.FindSentOperationsAsync(entityType, operationtType);
        }

        if (sentAccountInsertOperations.Count > 0)
        {
            await this.operationRepository.UpdateOperationStatusListASync(ProcessStatus.Failed, sentAccountInsertOperations);
        }
    }

    public async Task UpdateOperationStatusListASync(string processStatus, List<RegOperationEntity> operation)
    {
        await this.operationRepository.UpdateOperationStatusListASync(processStatus, operation);
    }

}
