// <copyright file="IAccountRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using Pulse.Registry.Domain.Entities;
using Pulse.Registry.Domain.Entities.Accounts;

namespace Application.Interfaces;

public interface IAccountRepository
{
    Task AddAccountsAsync(IEnumerable<RefAccountCsv> accounts);

    Task AddAccountAsync(AccountEntity account);

    Task<AccountEntity?> GetAccountByNumberOrIdAsync(string accountNumber);

    Task RemoveAccountAsync(AccountEntity account);

    Task UpdateAccountAsync(AccountEntity account);

    #region Deep Validation
    Task ValidateAccountOperation();
    #endregion
    Task<bool> DoesAccountExist(string accountNumber);

    Task<List<string>> GetExistingAccountNumbersAsync(IEnumerable<string> accountNumbers);
}
