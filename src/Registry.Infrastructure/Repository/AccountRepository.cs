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
    private bool DoesOperationExists(RefAccountEntity refAccount)
    {
        return refContext.RegOperationEntity.FirstOrDefault(x => x.EntityId == refAccount.EntityId) != null;
    }

    public async Task ValidateAccountOperation()
    {
        var refAccounts = refContext.RefAccountEntity
            .OrderBy(a => a.OperationDate);

        foreach (RefAccountEntity refAccount in refAccounts)
        {
            if (DoesOperationExists(refAccount))
            {
                continue;
            }

            var Id = refContext.AccountEntities
                .Where(x => x.AccountNumber == refAccount.AccountNumber)
                .Select(x => x.AccountGlobalUniqueId)
                .FirstOrDefault();

            if (Id == null)
            {
                var operationInsert = refContext.RegOperationEntity
                                            .Join(refContext.RefAccountEntity,
                                             operation => operation.EntityId,
                                             refAcc => refAcc.EntityId,
                                             (operation, refAcc) => new { operation, refAcc })
                                            .FirstOrDefault(x => x.refAcc.AccountNumber == refAccount.AccountNumber);
                if (refAccount.OperationType.ToLower() == OperationType.Update.ToString().ToLower()
                                        || refAccount.OperationType.ToLower() == OperationType.Delete.ToString().ToLower())
                {
                    
                    if (operationInsert != null)
                    {
                        InsertNewOperation(refAccount);
                    }
                    else
                    {
                        InsertNewAudit(refAccount, $"Operation of Type : {refAccount.OperationType} while Account Number {refAccount.AccountNumber} does not exists");
                    }
                }
                if (refAccount.OperationType.ToLower() == OperationType.Insert.ToString().ToLower())
                {
                    if (operationInsert != null)
                    {
                        InsertNewAudit(refAccount, $"Operation of Type : {refAccount.OperationType} with Account Number {refAccount.AccountNumber} already exists");
                    }
                    else
                    {
                        InsertNewOperation(refAccount);
                    }
                }
            }
            else
            {
                if (refAccount.OperationType.ToLower() == OperationType.Update.ToString().ToLower())
                {
                    InsertNewOperation(refAccount);
                }
                else if (refAccount.OperationType.ToLower() == OperationType.Delete.ToString().ToLower())
                {
                    var rolesToDelete = refContext.RoleEntities.Where(x => x.AccountGlobalUniqueId == Id).ToList();
                    foreach(RoleEntity role in rolesToDelete)
                    {
                        if(role.RoleDuplicatesCounter > 0)
                        {
                            role.RoleDuplicatesCounter = 0;
                            refContext.RoleEntities.Update(role);
                        }
                        await InsertNewOperation_DeleteRole(role);
                    }
                    await SaveChangesAsync();
                }
                else if (refAccount.OperationType.ToLower() == OperationType.Insert.ToString().ToLower())
                {
                    InsertNewAudit(refAccount, $"Operation of Type : {refAccount.OperationType} while this account {refAccount.AccountNumber} exists already");
                }
            }

            await SaveChangesAsync();
        }
    }

    private async Task InsertNewOperation_DeleteRole(RoleEntity role)
    {
        var operation = new RegOperationEntity
        {
            Operation = OperationType.Delete.ToString(),
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
            .AnyAsync(a => a.AccountNumber.ToLower() == accountNumber.ToLower());
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
                            where operation.Operation.ToLower() == operationType.ToLower()
                            && operation.ProcessStatus.ToLower() == processStatus.ToLower()
                            && account.AccountNumber.ToLower() == accountNumber.ToLower()
                            select 1
                      ).AnyAsync();
        return result;
    }

}

