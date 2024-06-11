// <copyright file="IAccountService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;

namespace Application.Interfaces;

public interface IAccountService
{
    Task ProcessAccountAsync(IEnumerable<AccountCsv> contacts);
    Task StreamAccountsJsonAsync(StreamWriter streamWriter);
}