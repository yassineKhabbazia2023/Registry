// <copyright file="AccountRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models;
using EFCore.BulkExtensions;
using Infrastructure.Mappers;
// <copyright file="ContactRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Infrastructure.Repository;
using Pulse.ContactRegistry.Domain.Context;

/// <summary>
/// ContactsRepository.
/// </summary>
/// <param name="dbContext">dbContext.</param>
public class AccountRepository(RefContext refContext) : IAccountRepository
{
    public async Task AddAccountsAsync(IEnumerable<RefAccountCsv> accounts)
    {
        await refContext.BulkInsertAsync(accounts.MapAccountCsvsToAccountEntities());
    }

}
