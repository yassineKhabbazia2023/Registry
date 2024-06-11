// <copyright file="AccountRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository
{
    /// <summary>
    /// ContactsRepository.
    /// </summary>
    /// <param name="dbContext">dbContext.</param>
    public class AccountRepository(ApplicationDbContext dbContext)
        : IAccountRepository
    {
        public async Task AddAccountsAsync(IEnumerable<AlxAccount> accounts)
        {
            await dbContext.AlxAccounts.AddRangeAsync(accounts);
            await dbContext.SaveChangesAsync();
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
    }
}
