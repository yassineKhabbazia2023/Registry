// <copyright file="AccountRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models;
using Domain.Entities;
using EFCore.BulkExtensions;
using Infrastructure.Context;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;
using Pulse.ContactRegistry.Infrastructure.Context;
using CreAccount = Domain.Entities.CreAccount;

namespace Infrastructure.Repository;

/// <summary>
/// ContactsRepository.
/// </summary>
/// <param name="dbContext">dbContext.</param>
public class AccountRepository(ApplicationDbContext dbContext, RefContext refContext) : IAccountRepository
{
    public async Task AddAccountsAsync(IEnumerable<AlxAccount> accounts)
    {
        await dbContext.BulkInsertAsync(accounts);
    }

    public async Task AddAccountsAsync(IEnumerable<RefAccountCsv> accounts)
    {
        await refContext.BulkInsertAsync(accounts.MapAccountCsvsToAccountEntities());
    }

    public async IAsyncEnumerable<CreAccount> GetAccountsAsync()
    {
        if (dbContext.CreAccounts.Any())
        {
            await foreach (var account in dbContext.CreAccounts.AsAsyncEnumerable())
            {
                yield return account;
            }
        }
    }

    public async Task<(int creAccountActif, int alxAccountActif)> GetCountAccountActifAsync()
    {
        var countCreAct = await dbContext.CreAccounts.CountAsync(c => c.AccountFlagEscActif);
        var countAlxAct = await dbContext.AlxAccounts.CountAsync(c => c.AccountFlagEscActif);
        return (countCreAct, countAlxAct);
    }

    public async Task ClearAlxAsync()
    {
        await dbContext.AlxAccounts.ExecuteDeleteAsync();
    }
}
