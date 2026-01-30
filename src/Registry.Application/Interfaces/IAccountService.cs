// <copyright file="IAccountService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using Pulse.Back.Events.IntegrationEvents.EventsData;

namespace Application.Interfaces;

/// <summary>
/// IAccountService.
/// </summary>
public interface IAccountService
{

    /// <summary>
    /// Inserts all accounts and operations into [ref].[Account] table.
    /// </summary>
    /// <param name="accounts">Accounts inserted.</param>
    /// <returns>A <see cref="Task"/> representing the async operation.</returns>
    Task InsertAccountsAsync(IEnumerable<RefAccountCsv> accounts);

    Task UpdateAccountProcessStatusAsync(string accountNumber, string operationName);

    Task<bool> SyncAcountAsync(AccountStateEventData accountEvent, string syncType);

    Task<string> GetAccountNumberByIdAsync(int accountId);
}