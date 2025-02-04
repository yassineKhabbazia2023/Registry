// <copyright file="IAccountRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using Domain.Entities.Accounts;
using Pulse.ContactRegistry.Domain.Entities;

namespace Application.Interfaces;

public interface IAccountRepository
{
    Task AddAccountsAsync(IEnumerable<RefAccountCsv> accounts);

    Task AddAccountAsync(AccountEntity account);

    Task<AccountEntity?> GetAccountByNumberOrIdAsync(string accountNumber);

    Task RemoveAccountAsync(AccountEntity account);

    Task UpdateAccountAsync(AccountEntity account);
}
