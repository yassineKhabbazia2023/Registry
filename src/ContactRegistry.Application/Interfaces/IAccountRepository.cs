// <copyright file="IAccountRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using Domain.Entities;
using CreAccount = Domain.Entities.CreAccount;

namespace Application.Interfaces;

public interface IAccountRepository
{
    Task AddAccountsAsync(IEnumerable<AlxAccount> accounts);

    IAsyncEnumerable<CreAccount> GetAccountsAsync();

    Task<(int creAccountActif, int alxAccountActif)> GetCountAccountActifAsync();

    Task ClearAlxAsync();

    Task AddAccountsAsync(IEnumerable<RefAccountCsv> accounts);
}
