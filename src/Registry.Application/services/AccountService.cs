// <copyright file="AccountService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Application.Services;

public class AccountService : IAccountService
{
    private readonly IAccountRepository accountRepository;
    private ILogger<AccountService> logger;

    public AccountService(ILogger<AccountService> logger, IAccountRepository accountRepository)
    {
        this.accountRepository = accountRepository;
        this.logger = logger;
    }


    public async Task InsertAccountsAsync(IEnumerable<RefAccountCsv> accounts)
    {
        await accountRepository.AddAccountsAsync(accounts);
    }
}
