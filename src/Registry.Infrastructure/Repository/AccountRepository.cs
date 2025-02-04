// <copyright file="AccountRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models;
using EFCore.BulkExtensions;
using Domain.Entities.Accounts;
using Microsoft.EntityFrameworkCore;
using Pulse.ContactRegistry.Domain.Context;
using Application.Mappers;
using Application.Exceptions;

namespace Infrastructure.Repository;
/// <summary>
/// ContactsRepository.
/// </summary>
/// <param name="dbContext">dbContext.</param>
public class AccountRepository(RefContext refContext) : IAccountRepository
{
    public async Task AddAccountAsync(AccountEntity account)
    {
        account.CreationDate = DateTime.UtcNow;
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

        return await refContext.AccountEntities.AsNoTracking().FirstOrDefaultAsync(a => (a.AccountId.Equals(accountId) || a.AccountNumber.Equals(accountNumberOrId)) && a.IsActive == true);
    }

    public async Task RemoveAccountAsync(AccountEntity account)
    {
        account.IsActive = false;
        refContext.AccountEntities.Update(account);

        await SaveChangesAsync();
    }

    public async Task UpdateAccountAsync(AccountEntity account)
    {
        account.UpdatedDate = DateTime.UtcNow;
        refContext.AccountEntities.Update(account);

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
}
