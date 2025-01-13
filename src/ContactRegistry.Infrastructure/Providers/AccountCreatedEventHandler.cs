// <copyright file="AccountCreatedEventHandler.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Azure;
using Infrastructure.Mappers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Pulse.Back.Events.Abstractions;
using Pulse.Back.Events.IntegrationEvents;
using System.Net;
using System.Web.Http;

namespace Infrastructure.Providers;

public class AccountCreatedEventHandler : IEventHandler
{
    private readonly ILogger<AccountCreatedEventHandler> _logger;
    private readonly IAccountRegistryProvider _accountRegistryProvider;

    public AccountCreatedEventHandler(
        ILogger<AccountCreatedEventHandler> logger,
        IAccountRegistryProvider accountRegistryProvider)
    {
        _logger = logger;
        _accountRegistryProvider = accountRegistryProvider;
    }

    public async Task HandleAsync(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            _logger.LogError("[ERREUR] Impossible de traiter l'événement car le message est null - AccountCreatedEventHandler");
            return;
        }

        var accountEvent = JsonConvert.DeserializeObject<AccountCreatedEvent>(message);

        if (accountEvent?.Data == null || string.IsNullOrEmpty(accountEvent.Data.AccountNumber))
        {
            _logger.LogError("[ERREUR] Format de données invalide pour le deployment de l'account: {AccountNumber}", accountEvent?.Data.AccountNumber);
            return;
        }

        _logger.LogInformation("Consommation de l'event type: {EventType}, accountNumber: {AccountNumber}",
            accountEvent.EventType,
            accountEvent.Data.AccountNumber);

        var accountModel = accountEvent!.Data.AccountEventDataToModel();

        var responseMessage = await _accountRegistryProvider.CreateDeploymentAsync(accountModel);

        if (responseMessage.StatusCode != HttpStatusCode.OK)
        {
            var errorMessage = responseMessage.Content.ReadAsAsync<HttpError>().Result.Message;
            _logger.LogError("[ERREUR] Échec de la création du deploymentStatus. Cause : {ErrorMessage}. - AccountCreatedEventHandler", errorMessage);
            return;
        }

        _logger.LogInformation("Le deploymentStatus de l'account: {AccountNumber} vient d'être crée.", accountModel.AccountNumber);
    }
}
