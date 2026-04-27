// <copyright file="AccountUpdatedEventHandler.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Pulse.Back.Events.Abstractions;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using System.Net;
using Application.Mappers;

namespace Infrastructure.Providers;

public class AccountUpdatedEventHandler : IEventHandler
{
    private readonly ILogger<AccountUpdatedEventHandler> _logger;
    private readonly IAccountRegistryProvider _accountRegistryProvider;
    private readonly IAccountService _accountService;

    public AccountUpdatedEventHandler(
    ILogger<AccountUpdatedEventHandler> logger,
    IAccountRegistryProvider accountRegistryProvider,
    IAccountService accountService)
    {
        _logger = logger;
        _accountRegistryProvider = accountRegistryProvider;
        _accountService = accountService;
    }

    public async Task HandleAsync(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            _logger.LogError("[ERREUR] Impossible de traiter l'événement car le message est null - AccountUpdatedEventHandler");
            return;
        }

        var accountEvent = JsonConvert.DeserializeObject<AccountUpdatedEvent>(message);

        if (accountEvent?.Data == null || string.IsNullOrEmpty(accountEvent.Data.AccountNumber))
        {
            _logger.LogError("[ERREUR] Format de données invalide pour le deployment de l'account: {AccountNumber}", accountEvent?.Data.AccountNumber);
            return;
        }

        _logger.LogInformation("Consommation de l'event type: {EventType}, accountNumber: {AccountNumber}",
            accountEvent.EventType,
            accountEvent.Data.AccountNumber);

        await TriggerSyncAndUpdateProcessStatusStep(accountEvent.Data, accountEvent.EventType);

        #region Flux vers Akuiteo lecagy
        //var accountModel = accountEvent!.Data.AccountEventDataToModel();

        //var responseMessage = await _accountRegistryProvider.UpdateDeploymentAsync(accountModel!);

        //if (responseMessage.StatusCode != HttpStatusCode.OK)
        //{
        //    var errorMessage = responseMessage.Content.ReadAsAsync<HttpError>().Result.Message;
        //    _logger.LogError("[ERREUR] Échec de la suppression de deploymentStatus. Cause : {ErrorMessage}. - AccountUpdatedEventHandler", errorMessage);
        //    return;
        //}
        //_logger.LogInformation("Le deploymentStatus de l'account: {AccountNumber} vient d'être modifié.", accountModel.AccountNumber);
        #endregion

    }

    private async Task TriggerSyncAndUpdateProcessStatusStep(AccountStateEventData accountStateEventData, string eventType)
    {
        var syncStatus = await _accountService.SyncAcountAsync(accountStateEventData, OperationAction.Update);
        if (syncStatus)
        {
            _logger.LogInformation("Completed syncing event {EventType} related to the account {AccountNumber}",
               eventType,
               accountStateEventData.AccountNumber);
        }

        await _accountService.UpdateAccountProcessStatusAsync(accountStateEventData.AccountNumber, OperationAction.Update);
        _logger.LogInformation("Update process status executed for the account {AccountNumber} upon the event {EventType}",
           eventType,
           accountStateEventData.AccountNumber);
    }
}
