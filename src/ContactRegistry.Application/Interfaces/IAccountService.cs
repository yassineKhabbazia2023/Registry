// <copyright file="IAccountService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;

namespace Application.Interfaces;

/// <summary>
/// IAccountService.
/// </summary>
public interface IAccountService
{
    /// <summary>
    /// ProcessAccountAsync.
    /// </summary>
    /// <param name="accounts">Accounts being processed.</param>
    /// <returns>A <see cref="Task"/> representing the async operation.</returns>
    Task ProcessAccountAsync(IEnumerable<AccountCsv> accounts);

    /// <summary>
    /// StreamAccountsJsonAsync.
    /// </summary>
    /// <param name="streamWriter">streamWriter.</param>
    /// <returns>A <see cref="Task"/> representing the async operation.</returns>
    Task StreamAccountsJsonAsync(StreamWriter streamWriter);

    /// <summary>
    /// ClearAlxAsync.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the async operation.</returns>
    Task ClearAlxAsync();

    /// <summary>
    /// Inserts all accounts and operations into [ref].[Account] table.
    /// </summary>
    /// <param name="accounts">Accounts inserted.</param>
    /// <returns>A <see cref="Task"/> representing the async operation.</returns>
    Task InsertAccountsAsync(IEnumerable<RefAccountCsv> accounts);
}