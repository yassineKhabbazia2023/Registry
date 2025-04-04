// <copyright file="AccountRemovedEventHandler.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Pulse.Back.Events.Abstractions;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.Back.Events.IntegrationEvents.EventsData;

namespace Infrastructure.Providers;

public class AccountRemovedEventHandler : IEventHandler
{
    private readonly ILogger<AccountRemovedEventHandler> _logger;
    private readonly IAccountService _accountService;

    public AccountRemovedEventHandler(
        ILogger<AccountRemovedEventHandler> logger,
        IAccountService accountService)
    {
        _logger = logger;
        _accountService = accountService;
    }

    public async Task HandleAsync(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            _logger.LogError("[ERREUR] Impossible de traiter l'événement car le message est null - AccountCreatedEventHandler");
            return;
        }

        var accountEvent = JsonConvert.DeserializeObject<AccountRemovedEvent>(message);

        if (accountEvent?.Data == null || accountEvent.Data.AccountId == 0)
        {
            _logger.LogError("[ERREUR] Format de données invalide pour le deployment de l'account: {AccountId}", accountEvent?.Data.AccountId);
            return;
        }

        _logger.LogInformation("Consommation de l'event type: {EventType}, AccountId: {AccountId}",
            accountEvent.EventType,
            accountEvent.Data.AccountId);

        await TriggerSyncAndUpdateProcessStatusStep(new AccountStateEventData() {  AccountId = accountEvent.Data.AccountId }, accountEvent.EventType) ;
    }

    private async Task TriggerSyncAndUpdateProcessStatusStep(AccountStateEventData accountStateEventData, string eventType)
    {
        var accountNumber = await _accountService.GetAccountNumberByIdAsync(accountStateEventData.AccountId);
        accountStateEventData.AccountNumber = accountNumber;

        var syncStatus = await _accountService.SyncAcountAsync(accountStateEventData, OperationAction.Delete);
        if (syncStatus)
        {
            _logger.LogInformation("Completed syncing event {EventType} related to the account {AccountNumber}",
               eventType,
               accountNumber);
        }

        await _accountService.UpdateAccountProcessStatusAsync(accountNumber, OperationAction.Delete);
        _logger.LogInformation("Update process status executed for the account {AccountNumber} upon the event {EventType}",
           eventType,
           accountNumber);
    }
}
