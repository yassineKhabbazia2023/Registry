// <copyright file="OperationService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Enums;
using Application.Interfaces;
using Application.Mappers;
using Application.Models;
using Application.Models.Commons;
using Application.Options;
using Application.Requests;
using Azure;
using Domain.Constants;
using Kpmg.ExceptionMiddleware.AdvancedException;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pulse.Registry.Domain.Entities;
using Registry.Application.Consts;

namespace Application.Services;


public class OperationService(IOperationRepository operationRepository, ILogger<OperationService> logger, IOptions<BackGroundJobOptions> backGroundJobOptions, IOptions<OperationOptions> operationOptions) : IOperationService
{
    private readonly BackGroundJobOptions _backGroundJobOptions = backGroundJobOptions.Value;
    private readonly OperationOptions _operationOptions = operationOptions.Value;

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

        await HandleRoleOperationsDuplicates(creOperation, email);

        creOperation.LastStatusUpdatedBy = email;
        return await operationRepository.UpdateOperationByIdAsync(operationId, creOperation);
    }

    public async Task<RegOperation?> GetOperationByIdAsync(int operationId)
    {
        return await operationRepository.GetOperationByIdAsync(operationId);
    }

    public async Task<bool> UpdateContactOperations(OperationSearchCriteria searchCriteria, string email)
    {
        var operationList = await operationRepository.FetchOperationsByCriteriaAsync(
                           searchCriteria,
                           OperationStrategyType.CONTACT,
                           email);

        if (!operationList.Any())
        {
            logger.LogWarning($"[Method]: {nameof(UpdateContactOperations)} operation List is empty!");
            return true;
        }
        var updateSucceed = await operationRepository.BulkUpdateOperationsStatusAsync(ProcessStatus.Succeeded, operationList);
        if (!updateSucceed)
        {
            logger.LogError($"[Method]: {nameof(UpdateContactOperations)}; [Error]: something went wrong while updating list of operation!");
        }
        return updateSucceed;
    }

    public async Task TryToProceedUntilTimeoutAsync(string entityType, string operationtType)
    {
        var startTime = DateTime.UtcNow;
        int TimeToWait = this._backGroundJobOptions.TimeToWaitBeforeEachStep;

        var sentAccountInsertOperations = await this.GetOperationsSentInLast24HoursAsync(entityType, operationtType);
        while (sentAccountInsertOperations.Count > 0 && (DateTime.UtcNow - startTime) < TimeSpan.FromMinutes(TimeToWait))
        {
            await Task.Delay(1000);
            sentAccountInsertOperations = await this.GetOperationsSentInLast24HoursAsync(entityType, operationtType);
        }

        if (sentAccountInsertOperations.Count > 0)
        {
            await operationRepository.BulkUpdateOperationsStatusAsync(ProcessStatus.Failed, sentAccountInsertOperations);
        }
    }

    public async Task UpdateOperationStatusListASync(string processStatus, List<RegOperationEntity> operation)
    {
        await operationRepository.BulkUpdateOperationsStatusAsync(processStatus, operation);
    }

    public List<AccountOperationRecord> GetAccountOperationRecordsBatch(string operationName, int chuckSize)
    {
        var query = operationRepository.GetAccountOperationDetails(operationName);

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
        return await operationRepository.GeRoleOperationDetailsAsync(operationName, chuckSize, fetchSystemCreatedOperations);
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
    public async Task<PagedResult<PendingRoleApprovals>> GetPendingRoleApprovalsAsync(int contactId, int page, int pageSize, string? search)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        int skip = (page - 1) * pageSize;

        ArgumentNullException.ThrowIfNull(_operationOptions);
        if (pageSize > _operationOptions.MaxAccountGettingOperations)
        {
            throw new TechnicalException($"Page size cannot be greater than {_operationOptions.MaxAccountGettingOperations}");
        }

        try
        {
            var result = await operationRepository.GetPendingRoleApprovalsAsync(contactId, skip, pageSize, search);

            var uniquePendingRoleApprovals = result.PendingRoleApprovals
                 .Select(pa => new PendingRoleApprovals
                 {
                     AccountNumber = pa.AccountNumber,
                     AccountName = pa.AccountName,
                     Operations = pa.Operations
                         .DistinctBy(op => op.Email)
                 });

            return new PagedResult<PendingRoleApprovals>
            {
                Items = uniquePendingRoleApprovals,
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = result.TotalItems
            };
        }
        catch (Exception ex)
        {
            throw new TechnicalException("An error occurred while fetching pending role approvals", ex);
        }
    }

    public bool ShouldTriggerInstantRolePublish(RegOperation? operation)
    {
        return operation != null
            && string.Equals(operation.Type, OperationCategory.ROLE, StringComparison.OrdinalIgnoreCase)
            && string.Equals(operation.Operation, OperationAction.Insert, StringComparison.OrdinalIgnoreCase)
            && string.Equals(operation.Status, ApprovalStatus.Approved, StringComparison.OrdinalIgnoreCase);
    }

    private async Task HandleRoleOperationsDuplicates(RegOperation sourceOperation, string email)
    {
        var duplicates = await operationRepository.GetInsertRoleOperationDuplicates(sourceOperation);
        if (duplicates.Any())
        {
            logger.LogInformation($"{nameof(OperationService)} Found duplicates for the operation {sourceOperation.Id}  treated by {email} to status : {sourceOperation.Status}, proceeding for mass approve of duplicates");

            foreach (var duplicate in duplicates)
            {
                var operation = duplicate.MapDbOperationEntityToOperationModel();
                if (operation == null)
                {
                    logger.LogWarning($"Mapping returned null for duplicate operation with ID {duplicate.Id}");
                    continue;
                }

                operation.LastStatusUpdatedBy = email;
                operation.Status = sourceOperation.Status ;
                await operationRepository.UpdateOperationByIdAsync(duplicate.Id, operation);
            }
        }
    }
}
