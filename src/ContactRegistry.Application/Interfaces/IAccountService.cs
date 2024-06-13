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
    /// <param name="contacts">contacts.</param>
    /// <returns>A <see cref="Task"/> representing the async operation.</returns>
    Task ProcessAccountAsync(IEnumerable<AccountCsv> contacts);

    /// <summary>
    /// StreamAccountsJsonAsync.
    /// </summary>
    /// <param name="streamWriter">streamWriter.</param>
    /// <returns>A <see cref="Task"/> representing the async operation.</returns>
    Task StreamAccountsJsonAsync(StreamWriter streamWriter);
}