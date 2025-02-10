// <copyright file="AccountService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Exceptions;
using Application.Interfaces;
using Application.Models;
using Domain.Entities.Accounts;
using Microsoft.Extensions.Logging;
using Pulse.Back.Events.IntegrationEvents.EventsData;

namespace Application.Services;

public class AccountService : IAccountService
{
    private readonly IAccountRepository accountRepository;
    private IOperationRepository operationRepository;
    private readonly ILogger<AccountService> logger;

    public AccountService(IAccountRepository accountRepository, IOperationRepository operationRepository, ILogger<AccountService> logger)
    {
        this.accountRepository = accountRepository;
        this.operationRepository = operationRepository;
        this.logger = logger;
    }

    public async Task<string?> GetAccountNumberByIdAsync(int accountId)
    {
        var account = await accountRepository.GetAccountByNumberOrIdAsync(accountId.ToString());
        return account?.AccountNumber;
    }

    public async Task InsertAccountsAsync(IEnumerable<RefAccountCsv> accounts)
    {
        await accountRepository.AddAccountsAsync(accounts);
    }

    public async Task<bool> SyncAcountAsync(AccountStateEventData accountEvent, string syncType)
    {
        var account = await this.accountRepository.GetAccountByNumberOrIdAsync(accountEvent.AccountNumber);

        string[] syncTypesThatNeedsAnExistingAccount = { OperationName.Update, OperationName.Delete };
        if (account is null && syncTypesThatNeedsAnExistingAccount.Contains(syncType))
        {
            this.logger.LogWarning("{Instance} Unable to trigger Sync operation of type {SyncType}, the account {AccountNumber} does not exists", nameof(AccountService), syncType, accountEvent.AccountNumber);
            return false;
        }

        if (account is not null && syncType.Equals(OperationName.Insert))
        {
            this.logger.LogWarning("{Instance} Unable to trigger Sync operation of type {SyncType}, the account {AccountNumber} already exists", nameof(AccountService), syncType, accountEvent.AccountNumber);
            return false;
        }

        switch (syncType)
        {
            case OperationName.Insert:
                return await CreateNewAccountAsync(accountEvent);
            case OperationName.Update:
                return await UpdateAccountAsync(accountEvent);
            case OperationName.Delete:
                return await RemoveAccountAsync(account);
            default:
                return false;
        }
    }

    public async Task UpdateAccountProcessStatusAsync(string accountNumber, string operationName)
    {
        var operations = await this.operationRepository.FindAccountOperationAsync(new Application.Requests.OperationSearchCriteria()
        {
            OperationName = operationName
        }, accountNumber);

        await this.operationRepository.UpdateOperationStatusListASync(ProcessStatus.Succeeded.ToString(), operations);
    }

    private async Task<bool> CreateNewAccountAsync(AccountStateEventData accountStateEventData)
    {
        var accountEntity = new AccountEntity()
        {
            AccountId = accountStateEventData!.AccountId,
            AccountNumber = accountStateEventData.AccountNumber,
            AccountGlobalUniqueId = accountStateEventData.AccountGlobalUniqueId,
        };
        try
        {
            await this.accountRepository.AddAccountAsync(accountEntity);
            return true;
        }
        catch (DbOperationException ex)
        {
            this.logger.LogError("{Instance} faced issues while adding account {AccountNumber} in the AccountEntity table,{Details}", nameof(AccountService), accountEntity.AccountNumber, ex.InnerException);
            return false;
        }
    }

    private async Task<bool> UpdateAccountAsync(AccountStateEventData accountStateEventData)
    {
        var accountEntity = new AccountEntity()
        {
            AccountId = accountStateEventData!.AccountId,
            AccountNumber = accountStateEventData.AccountNumber,
            AccountGlobalUniqueId = accountStateEventData.AccountGlobalUniqueId,
        };
        try
        {
            await this.accountRepository.UpdateAccountAsync(accountEntity);
            return true;
        }
        catch (DbOperationException ex)
        {
            this.logger.LogError("{Instance} faced issues while updating account {AccountNumber} in the AccountEntity table,{Details}", nameof(AccountService), accountEntity.AccountNumber, ex.InnerException);
            return false;
        }
    }

    private async Task<bool> RemoveAccountAsync(AccountEntity account)
    {
        try
        {
            if(account is null)
            {
                return false;
            }

            await this.accountRepository.RemoveAccountAsync(account);
            return true;
        }
        catch (DbOperationException ex)
        {
            this.logger.LogError("{Instance} faced issues while removing account {AccountNumber} in the AccountEntity table,{Details}", nameof(AccountService), account.AccountNumber, ex.InnerException);
            return false;
        }
    }
}
