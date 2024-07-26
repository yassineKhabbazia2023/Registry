// <copyright file="AccountRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Domain.Entities;
using EFCore.BulkExtensions;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace Infrastructure.Repository
{

    /// <summary>
    /// ContactsRepository.
    /// </summary>
    /// <param name="dbContext">dbContext.</param>
    [ExcludeFromCodeCoverage]
    public class AccountRepository(ApplicationDbContext dbContext)
        : IAccountRepository
    {
        public async Task AddAccountsAsync(IEnumerable<AlxAccount> accounts)
        {
            await dbContext.BulkInsertAsync(accounts);
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
}
