// <copyright file="AccountService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Helpers;
using Application.Interfaces;
using Application.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Application.Services;

public class AccountService : IAccountService
{
    private readonly IAccountRepository accountRepository;
    private ILogger<AccountService> logger;
    private IValidationHelper<RefAccountCsv> validationHelper;

    public AccountService(ILogger<AccountService> logger, IAccountRepository accountRepository, IValidationHelper<RefAccountCsv> validationHelper)
    {
        this.accountRepository = accountRepository;
        this.logger = logger;
        this.validationHelper = validationHelper;
    }


    public async Task InsertAccountsAsync(IEnumerable<RefAccountCsv> accounts)
    {
        await accountRepository.AddAccountsAsync(accounts);
    }

    public IEnumerable<LightValidationResult> ValidateAccounts(IEnumerable<RefAccountCsv> accounts)
    {
        return this.validationHelper.ValidateInstanceList(accounts);
    }
}
