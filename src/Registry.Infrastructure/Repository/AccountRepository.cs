// <copyright file="AccountRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Enums;
using Application.Exceptions;
using Application.Interfaces;
using Application.Mappers;
using Application.Models;
using Application.Requests;
using Domain.Entities.Accounts;
using Domain.Entities.Audits;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities;
using Registry.Application.Consts;
using System.Threading.Tasks;
using OperationType = EFCore.BulkExtensions.OperationType;

namespace Infrastructure.Repository;
/// <summary>
/// ContactsRepository.
/// </summary>
/// <param name="dbContext">dbContext.</param>
public class AccountRepository(RefContext refContext, IOperationRepository operationRepository,IDeepValidationRepository deepValidationRepository) : IAccountRepository
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

        return await refContext.AccountEntities.AsNoTracking().FirstOrDefaultAsync(a => a.AccountId.Equals(accountId) || a.AccountNumber.Equals(accountNumberOrId));
    }

    public async Task RemoveAccountAsync(AccountEntity account)
    {
        refContext.AccountEntities.Remove(account);

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

            var retreivedAcountId = refContext.AccountEntities
                .Where(x => x.AccountNumber == refAccount.AccountNumber)
                .Select(x => x.AccountGlobalUniqueId)
                .FirstOrDefault();

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

    private async Task UpdateValidationDate(RefAccountEntity refAccount)
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
        return await refContext.AccountEntities.AsNoTracking()
            .AnyAsync(a => a.AccountNumber == accountNumber);
    }
    public async Task UpdateAccountAsync(AccountEntity account)
    {
        refContext.AccountEntities.Update(account);

        await SaveChangesAsync();
    }

    private async Task CreateInsertAccountOperationAsync(RefAccountEntity refAccount, Guid? retreivedAcountId)
    {
        var criteria = new OperationSearchCriteria
        {
            OperationName = refAccount.OperationType,
        };

        var operations = await operationRepository.FetchOperationsByCriteriaAsync(
            criteria,
            OperationStrategyType.ACCOUNT,
            refAccount.AccountNumber);

        var doesOperationExists = operations.Any();

        if (retreivedAcountId != null || doesOperationExists)
        {
           await  InsertNewAudit(refAccount, $"Operation of Type : {refAccount.OperationType} with this Account Number {refAccount.AccountNumber} already exists");
        }
        else
        {
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
            await InsertNewAudit(refAccount, $"Operation of Type : {refAccount.OperationType} with this Account Number {refAccount.AccountNumber} account does not exists");
        }
        else
        {
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
            // Create delete roles operations
            var rolesToDelete = refContext.RoleEntities.Where(x => x.AccountGlobalUniqueId == retreivedAcountId).ToList();
            foreach (RoleEntity role in rolesToDelete)
            {
                if (role.RoleDuplicatesCounter > 0)
                {
                    role.RoleDuplicatesCounter = 0;
                    refContext.RoleEntities.Update(role);
                }

                await operationRepository.CreateOperationAsync(new RegOperationEntity
                {
                    Operation = OperationAction.Delete,
                    ApprovalStatus = ApprovalStatus.Approved,
                    EntityId = role.AccountGlobalUniqueId,
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
}

