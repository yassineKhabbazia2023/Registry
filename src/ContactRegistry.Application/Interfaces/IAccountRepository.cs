// <copyright file="IAccountRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>



using Domain.Entities;

namespace Application.Interfaces
{
    public interface IAccountRepository
    {
        Task AddAccountsAsync(IEnumerable<AlxAccount> accounts);

        IAsyncEnumerable<CreAccount> GetAccountsAsync();

        Task<(int creAccountActif, int alxAccountActif)> GetCountAccountActifAsync();

        Task ClearAlxAsync();
    }
}
