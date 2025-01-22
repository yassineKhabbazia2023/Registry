// <copyright file="IAccountRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;

namespace Application.Interfaces;

public interface IAccountRepository
{
    Task AddAccountsAsync(IEnumerable<RefAccountCsv> accounts);
}
