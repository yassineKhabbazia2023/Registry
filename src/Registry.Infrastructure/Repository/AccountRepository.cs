// <copyright file="AccountRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Exceptions;
using Application.Interfaces;
using Application.Mappers;
using Application.Models;
using Domain.Entities.Accounts;
using Domain.Entities.Audits;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities;
using Registry.Application.Consts;
using OperationType = EFCore.BulkExtensions.OperationType;

namespace Infrastructure.Repository;
/// <summary>
/// ContactsRepository.
/// </summary>
/// <param name="dbContext">dbContext.</param>
public class AccountRepository(RefContext refContext) : IAccountRepository
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
    private bool DoesOperationInsertOrDeleteExists(RefAccountEntity refAccount)
    {
        if (refAccount.OperationType == OperationName.Update) return false;
        return (from refAcc in refContext.RefAccountEntity
                join opAcc in refContext.RegOperationEntity on refAcc.EntityId equals opAcc.EntityId
                where refAcc.AccountNumber == refAccount.AccountNumber
                && refAcc.OperationType == refAccount.OperationType
                select 1).Any();
    }

    private bool DoesOperationExists(RefAccountEntity refAccount)
    {
        return (from refAcc in refContext.RefAccountEntity
                join opAcc in refContext.RegOperationEntity on refAcc.EntityId equals opAcc.EntityId
                where refAcc.AccountNumber == refAccount.AccountNumber
                && refAcc.OperationType == refAccount.OperationType
                select 1).Any();
    }

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
                case OperationName.Insert:
                    await CreateInsertAccountOperationAsync(refAccount, retreivedAcountId);
                    break;
                case OperationName.Update:
                    await CreateUpdateAccountOperationAsync(refAccount, retreivedAcountId);
                    break;
                case OperationName.Delete:
                    await CreateDeleteAccountOperationAsync(refAccount, retreivedAcountId);
                    break;
            }
        }
    }

    private async Task UpdateValidationDate(RefAccountEntity refAccount)
    {
        refAccount.ValidationDate = DateTime.UtcNow;
    }

    private async Task InsertNewOperation_DeleteRole(RoleEntity role)
    {
        var operation = new RegOperationEntity
        {
            Operation = OperationName.Delete,
            ApprovalStatus = ApprovalStatus.Approved,
            EntityId = role.AccountGlobalUniqueId,
            Type = "ROLE",
            ProcessStatus = ProcessStatus.Ready
        };

        refContext.RegOperationEntity.Add(operation);
        await refContext.SaveChangesAsync();
    }

    private void InsertNewOperation(RefAccountEntity refAccount)
    {
        var operation = new RegOperationEntity
        {
            Operation = refAccount.OperationType,
            Type = "ACCOUNT",
            EntityId = refAccount.EntityId,
            ApprovalStatus = ApprovalStatus.Approved,
            CreationDate = DateTime.UtcNow,
            ProcessStatus = ProcessStatus.Ready
        };
        refContext.RegOperationEntity.Add(operation);
    }

    private void InsertNewAudit(RefAccountEntity refAccount, string reason)
    {
        var audit = new DeepValidationEntity
        {
            Type = "Account",
            EntityId = refAccount.EntityId,
            Reason = reason
        };
        refContext.DeepValidationEntities.Add(audit);
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

    public async Task<bool> DoesAccountExistInOperations(string accountNumber, string operationType, string processStatus)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(accountNumber);
        ArgumentNullException.ThrowIfNullOrEmpty(operationType);
        ArgumentNullException.ThrowIfNullOrEmpty(processStatus);

        var result = await (from operation in refContext.RegOperationEntity
                            join account in refContext.RefAccountEntity on operation.EntityId equals account.EntityId
                            where operation.Operation == operationType
                            && operation.ProcessStatus == processStatus
                            && account.AccountNumber == accountNumber
                            select 1
                      ).AnyAsync();
        return result;
    }

    public async Task<bool> AccountOperationExistsAsync(string accountNumber, string operationName, List<string> processStatusRange)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(accountNumber);

        var result = await (from operation in refContext.RegOperationEntity
                            join account in refContext.RefAccountEntity on operation.EntityId equals account.EntityId
                            where operation.Operation == operationName
                            && processStatusRange.Contains(operation.ProcessStatus)
                            && account.AccountNumber == accountNumber
                            select 1
                      ).AnyAsync();
        return result;
    }

    private async Task CreateInsertAccountOperationAsync(RefAccountEntity refAccount, Guid? retreivedAcountId)
    {
        var doesOperationExists = DoesOperationExists(refAccount);

        if (retreivedAcountId != null || doesOperationExists)
        {
            InsertNewAudit(refAccount, $"Operation of Type : {refAccount.OperationType} with this Account Number {refAccount.AccountNumber} already exists");
        }
        else
        {
            InsertNewOperation(refAccount);
        }

        await SaveChangesAsync();
    }

    private async Task CreateUpdateAccountOperationAsync(RefAccountEntity refAccount, Guid? retreivedAcountId)
    {
        var doesInsertOperationExists = await this.AccountOperationExistsAsync(
            refAccount.AccountNumber,
            OperationName.Insert,
            new List<string>() { ProcessStatus.Ready, ProcessStatus.Sent, ProcessStatus.Succeeded });

        if (retreivedAcountId is null && !doesInsertOperationExists)
        {
            InsertNewAudit(refAccount, $"Operation of Type : {refAccount.OperationType} with this Account Number {refAccount.AccountNumber} account does not exists");
        }
        else
        {
            InsertNewOperation(refAccount);
        }

        await SaveChangesAsync();
    }
    
    private async Task CreateDeleteAccountOperationAsync(RefAccountEntity refAccount, Guid? retreivedAcountId)
    {
        var doesInsertOperationExists = await this.AccountOperationExistsAsync(
            refAccount.AccountNumber,
            OperationName.Insert,
            new List<string>() { ProcessStatus.Ready, ProcessStatus.Sent});

        if (retreivedAcountId is null && !doesInsertOperationExists)
        {
            InsertNewAudit(refAccount, $"Operation of Type : {refAccount.OperationType} with this Account Number {refAccount.AccountNumber} account does not exists");
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
                await InsertNewOperation_DeleteRole(role);
            }

            // Create delete account operation
            InsertNewOperation(refAccount);
        }

        await SaveChangesAsync();
    }
}

