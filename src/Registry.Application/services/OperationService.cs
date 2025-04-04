// <copyright file="OperationService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Enums;
using Application.Interfaces;
using Application.Models;
using Application.Options;
using Application.Requests;
using Domain.Constants;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pulse.Registry.Domain.Entities;
using Registry.Application.Consts;

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
        var operations = await operationRepository.FetchOperationsByAccountNumberAsync(accountNumber, operationSearchCriteria, OperationSource.MainTables);

        // If no operations are found, fallback to using the alternative repository method
        if (!operations.Any())
        {
            operations = await operationRepository.FetchOperationsByAccountNumberAsync(accountNumber, operationSearchCriteria, OperationSource.AccountMainAndContactRefTables);
        }

        // Return distinct operations based on Email
        return operations.DistinctBy(x => x.Email);
    }

    public async Task<RegOperation?> UpdateOperationByIdAsync(int operationId, string email, RegOperation creOperation)
    {
        ArgumentNullException.ThrowIfNull(operationId);
        ArgumentNullException.ThrowIfNull(email);

        creOperation.LastStatusUpdatedBy = email;
        return await this.operationRepository.UpdateOperationByIdAsync(operationId, creOperation);
    }

    public async Task<RegOperation?> GetOperationByIdAsync(int operationId)
    {
        return await this.operationRepository.GetOperationByIdAsync(operationId);
    }

    public async Task<bool> UpdateContactOperations(OperationSearchCriteria searchCriteria, string email)
    {
        var operationList = await this.operationRepository.FetchOperationsByCriteriaAsync(
                           searchCriteria,
                           OperationStrategyType.CONTACT,
                           email);

        if (!operationList.Any())
        {
            logger.LogWarning($"[Method]: {nameof(UpdateContactOperations)} operation List is empty!");
            return true;
        }
        var updateSucceed = await this.operationRepository.BulkUpdateOperationsStatusAsync(ProcessStatus.Succeeded, operationList);
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

        var sentAccountInsertOperations = await this.GetOperationsSentInLast24HoursAsync(entityType, operationtType);
        while (sentAccountInsertOperations.Count > 0 && (DateTime.UtcNow - startTime) < TimeSpan.FromMinutes(TimeToWait))
        {
            await Task.Delay(1000);
            sentAccountInsertOperations = await this.GetOperationsSentInLast24HoursAsync(entityType, operationtType);
        }

        if (sentAccountInsertOperations.Count > 0)
        {
            await this.operationRepository.BulkUpdateOperationsStatusAsync(ProcessStatus.Failed, sentAccountInsertOperations);
        }
    }

    public async Task UpdateOperationStatusListASync(string processStatus, List<RegOperationEntity> operation)
    {
        await this.operationRepository.BulkUpdateOperationsStatusAsync(processStatus, operation);
    }

    public List<AccountOperationRecord> GetAccountOperationRecordsBatch(string operationName, int chuckSize)
    {
        var query = this.operationRepository.GetAccountOperationDetails(operationName);

        return query
        .Take(chuckSize)
        .ToList();

    }

    public IEnumerable<ContactOperationRecord> GeContactOperationRecords(string operationType)
    {
        return operationRepository.GeContactOperationDetails(operationType).ToList();
    }

    public async Task<List<RoleOperationRecord>> GetRoleOperationRecordsAsync(string operationName, int chuckSize, bool? fetchSystemCreatedOperations = false)
    {
        return await this.operationRepository.GeRoleOperationDetailsAsync(operationName, chuckSize, fetchSystemCreatedOperations);
    }

    private async Task<List<RegOperationEntity>> GetOperationsSentInLast24HoursAsync(string entityType, string operationType)
    {
        DateTime twentyFourHoursAgo = DateTime.UtcNow.AddHours(-24);

        var criteria = new OperationSearchCriteria
        {
            OperationName = operationType,
            PublishedAt = twentyFourHoursAgo,
            PublishedAtGreaterThan = true,
        };

        IEnumerable<RegOperationEntity> operations = new List<RegOperationEntity>();

        switch (entityType)
        {
            case OperationCategory.ACCOUNT:
                operations = await operationRepository.FetchOperationsByCriteriaAsync(
                   criteria,
                   OperationStrategyType.ACCOUNT,
                   string.Empty);
                break;

            case OperationCategory.CONTACT:
                operations = await operationRepository.FetchOperationsByCriteriaAsync(
                   criteria,
                   OperationStrategyType.CONTACT,
                   string.Empty);
                break;

            case OperationCategory.ROLE:
                operations = await operationRepository.FetchOperationsByCriteriaAsync(
                   criteria,
                   OperationStrategyType.ROLE,
                   string.Empty);
                break;
        }

        return operations.ToList();
    }
}
