// <copyright file="AccountRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Enums;
using Application.Exceptions;
using Application.Helpers.Extensions;
using Application.Interfaces;
using Application.Mappers;
using Application.Models;
using Application.Requests;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities;
using Pulse.Registry.Domain.Entities.Accounts;
using Pulse.Registry.Domain.Entities.Audits;
using Registry.Application.Consts;

namespace Infrastructure.Repository;
/// <summary>
/// ContactsRepository.
/// </summary>
/// <param name="dbContext">dbContext.</param>
public class AccountRepository(RefContext refContext, 
    IOperationRepository operationRepository, 
    IDeepValidationRepository deepValidationRepository, 
    ILogger<AccountRepository> logger,
    IFeatureFlagService featureFlagService) : IAccountRepository
{
    public async Task AddAccountAsync(AccountEntity account)
    {
        refContext.Add(account);

        await SaveChangesAsync();
    }

    public async Task AddAccountsAsync(IEnumerable<RefAccountCsv> accounts)
    {
        await refContext.BulkInsertAsync(accounts.MapAccountCsvsToAccountEntities());
    }

    public async Task<AccountEntity?> GetAccountByNumberOrIdAsync(string accountNumberOrId)
    {
        int accountId = 0;
        int.TryParse(accountNumberOrId, out accountId);

        return await refContext.AccountEntity.AsNoTracking().FirstOrDefaultAsync(a => a.AccountId.Equals(accountId) || a.AccountNumber.Equals(accountNumberOrId));
    }

    public async Task RemoveAccountAsync(AccountEntity account)
    {
        refContext.AccountEntity.Remove(account);

        await SaveChangesAsync();
    }

    private async Task SaveChangesAsync()
    {
        try
        {
            await refContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            throw new DbOperationException("Something went wrong while saving data", ex.InnerException);
        }
    }

    #region Deep Validation

    public async Task ValidateAccountOperation()
    {
        // get ligne qui sont pas traité
        var refAccounts = refContext.RefAccountEntity
            .Where(x => x.ValidationDate == null)
            .OrderBy(a => a.OperationDate);

        foreach (RefAccountEntity refAccount in refAccounts)
        {
            // Update validation date to prevent iterating on the same lines the next day
            UpdateValidationDate(refAccount);

            if (refAccount.AccountType.IsProspectAccount())
            {
                if (!IsProspectConsumptionEnabled())
                {
                    logger.LogWarning("{RepositoryName} Prospect processing disabled by feature flag for account {AccountNumber}", nameof(AccountRepository), refAccount.AccountNumber);
                    await InsertNewAudit(refAccount, $"Prospect account {refAccount.AccountNumber} was rejected because the prospect feature flag is disabled");
                    continue;
                }

                LogProspectAccountInformation(refAccount, "{RepositoryName} Prospect account {AccountNumber} accepted for operation {OperationType}", nameof(AccountRepository), refAccount.AccountNumber, refAccount.OperationType);
            }

            var retreivedAcountId = await refContext.AccountEntity
                .Where(x => x.AccountNumber == refAccount.AccountNumber)
                .Select(x => x.AccountGlobalUniqueId)
                .FirstOrDefaultAsync();

            var doesCreateOperationExist = await DoesInsertOperationExist(refAccount);
            if (retreivedAcountId != null || doesCreateOperationExist)
            {
                logger.LogInformation("{RepositoryName} Transforming the account operation {OperationType} to UPDATE for the account {AccountNumber}", nameof(AccountRepository), refAccount.OperationType, refAccount.AccountNumber);

                refAccount.OperationType = OperationAction.Update;
                await UpdateRefAccountAsync(refAccount);
            }

            switch (refAccount.OperationType)
            {
                case OperationAction.Insert:
                    await CreateInsertAccountOperationAsync(refAccount, retreivedAcountId);
                    break;
                case OperationAction.Update:
                    await CreateUpdateAccountOperationAsync(refAccount, retreivedAcountId);
                    break;
                case OperationAction.Delete:
                    await CreateDeleteAccountOperationAsync(refAccount, retreivedAcountId);
                    break;
            }
        }
    }

    private static void UpdateValidationDate(RefAccountEntity refAccount)
    {
        refAccount.ValidationDate = DateTime.UtcNow;
    }

    private async Task InsertNewAudit(RefAccountEntity refAccount, string reason)
    {
        await deepValidationRepository.AddDeepValidationAsync(new DeepValidationEntity
        {
            Type = "Account",
            EntityId = refAccount.EntityId,
            Reason = reason
        });
    }

    #endregion

    public async Task<bool> DoesAccountExist(string accountNumber)
    {
        return await refContext.AccountEntity.AsNoTracking()
            .AnyAsync(a => a.AccountNumber == accountNumber);
    }

    public async Task<List<string>> GetExistingAccountNumbersAsync(IEnumerable<string> accountNumbers)
    {
        var result = new List<string>();
        foreach (var batch in accountNumbers.Distinct().Chunk(QueryBatching.BatchSize))
        {
            result.AddRange(await refContext.AccountEntity.AsNoTracking()
                .Where(a => batch.Contains(a.AccountNumber))
                .Select(a => a.AccountNumber)
                .ToListAsync());
        }

        return result;
    }

    public async Task UpdateAccountAsync(AccountEntity account)
    {
        refContext.AccountEntity.Update(account);
        await SaveChangesAsync();
    }

    private async Task UpdateRefAccountAsync(RefAccountEntity refAccount)
    {
        refContext.RefAccountEntity.Update(refAccount);
        await SaveChangesAsync();
    }

    private async Task CreateInsertAccountOperationAsync(RefAccountEntity refAccount, Guid? retreivedAcountId)
    {
        LogProspectAccountInformation(refAccount, "{RepositoryName} Creating INSERT operation for prospect account {AccountNumber}", nameof(AccountRepository), refAccount.AccountNumber);

        await operationRepository.CreateOperationAsync(new RegOperationEntity
        {
            Operation = refAccount.OperationType,
            Type = OperationCategory.ACCOUNT,
            EntityId = refAccount.EntityId,
            ApprovalStatus = ApprovalStatus.Approved,
            CreationDate = DateTime.UtcNow,
            ProcessStatus = ProcessStatus.Ready
        });
    }
    
    private async Task CreateUpdateAccountOperationAsync(RefAccountEntity refAccount, Guid? retreivedAcountId)
    {
        var criteria = new OperationSearchCriteria
        {
            OperationName = OperationAction.Insert,
            OperationProcessStatus = new[] { ProcessStatus.Ready, ProcessStatus.Sent, ProcessStatus.Succeeded }
        };

        var operations = await operationRepository.FetchOperationsByCriteriaAsync(
            criteria,
            OperationStrategyType.ACCOUNT,
            refAccount.AccountNumber);

        var doesInsertOperationExists = operations.Any();

        if (retreivedAcountId is null && !doesInsertOperationExists)
        {
            if (refAccount.AccountType.IsProspectAccount())
            {
                LogProspectAccountInformation(refAccount, "{RepositoryName} Treating UPDATE as INSERT for prospect account {AccountNumber}", nameof(AccountRepository), refAccount.AccountNumber);
                refAccount.OperationType = OperationAction.Insert;
                await UpdateRefAccountAsync(refAccount);
                await CreateInsertAccountOperationAsync(refAccount, retreivedAcountId);
                return;
            }

            await InsertNewAudit(refAccount, $"Operation of Type : {refAccount.OperationType} with this Account Number {refAccount.AccountNumber} account does not exists");
        }
        else
        {
            LogProspectAccountInformation(refAccount, "{RepositoryName} Creating UPDATE operation for prospect account {AccountNumber}", nameof(AccountRepository), refAccount.AccountNumber);

            var operation = new RegOperationEntity
            {
                Operation = refAccount.OperationType,
                Type = OperationCategory.ACCOUNT,
                EntityId = refAccount.EntityId,
                ApprovalStatus = ApprovalStatus.Approved,
                CreationDate = DateTime.UtcNow,
                ProcessStatus = ProcessStatus.Ready
            };
            await operationRepository.CreateOperationAsync(operation);
        }

    }

    private async Task CreateDeleteAccountOperationAsync(RefAccountEntity refAccount, Guid? retreivedAcountId)
    {
        var criteria = new OperationSearchCriteria
        {
            OperationName = OperationAction.Insert,
            OperationProcessStatus = new[] { ProcessStatus.Ready, ProcessStatus.Sent, ProcessStatus.Succeeded }
        };

        var operations = await operationRepository.FetchOperationsByCriteriaAsync(
            criteria,
            OperationStrategyType.ACCOUNT,
            refAccount.AccountNumber);

        var doesInsertOperationExists = operations.Any();

        if (retreivedAcountId is null && !doesInsertOperationExists)
        {
            await InsertNewAudit(refAccount, $"Operation of Type : {refAccount.OperationType} with this Account Number {refAccount.AccountNumber} account does not exists");
        }
        else
        {
            LogProspectAccountInformation(refAccount, "{RepositoryName} Creating DELETE operation for prospect account {AccountNumber}", nameof(AccountRepository), refAccount.AccountNumber);

            // Create delete roles operations
            var rolesToDelete = await refContext.RoleEntity.Where(x => x.AccountGlobalUniqueId == retreivedAcountId).ToListAsync();
            foreach (RoleEntity role in rolesToDelete)
            {
                if (role.RoleDuplicatesCounter > 0)
                {
                    role.RoleDuplicatesCounter = 0;
                    refContext.RoleEntity.Update(role);
                }

                await operationRepository.CreateOperationAsync(new RegOperationEntity
                {
                    Operation = OperationAction.Delete,
                    ApprovalStatus = ApprovalStatus.Approved,
                    EntityId = (Guid) role.AccountGlobalUniqueId,
                    Type = OperationCategory.ROLE,
                    ProcessStatus = ProcessStatus.Ready,
                    CreationDate = DateTime.UtcNow,
                    CreatedBySystem = true
                });
            }

            // Create delete account operation
            await operationRepository.CreateOperationAsync(new RegOperationEntity
            {
                Operation = refAccount.OperationType,
                Type = OperationCategory.ACCOUNT,
                EntityId = refAccount.EntityId,
                ApprovalStatus = ApprovalStatus.Approved,
                CreationDate = DateTime.UtcNow,
                ProcessStatus = ProcessStatus.Ready
            });
        }
    }

    private async Task<bool> DoesInsertOperationExist(RefAccountEntity refAccount)
    {
        var criteria = new OperationSearchCriteria
        {
            OperationName = OperationAction.Insert
        };

        var operations = await operationRepository.FetchOperationsByCriteriaAsync(
            criteria,
            OperationStrategyType.ACCOUNT,
            refAccount.AccountNumber);

        return operations.Any();
    }

    /// <summary>
    /// Logs an information message only when the current account is a prospect account.
    /// </summary>
    /// <param name="refAccount">The account being processed.</param>
    /// <param name="messageTemplate">The message template to log.</param>
    /// <param name="args">The message template arguments.</param>
    private void LogProspectAccountInformation(RefAccountEntity refAccount, string messageTemplate, params object[] args)
    {
        if (refAccount.AccountType.IsProspectAccount())
        {
            logger.LogInformation(messageTemplate, args);
        }
    }

    private bool IsProspectConsumptionEnabled()
        => featureFlagService.IsEnabled(FeatureFlagKeys.IsProspectConsumptionEnabled);
}

