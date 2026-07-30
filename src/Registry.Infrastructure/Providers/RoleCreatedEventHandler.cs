// <copyright file="RoleCreatedEventHandler.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Enums;
using Application.Interfaces;
using Application.Mappers;
using Application.Requests;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Pulse.Back.Events.Abstractions;
using Pulse.Back.Events.IntegrationEvents;

namespace Application.Providers;

public class RoleCreatedEventHandler : IEventHandler
{
    private readonly ILogger<RoleCreatedEventHandler> _logger;
    private readonly IRoleRegistryProvider _roleRegistryProvider;
    private readonly IRoleRepository _roleRepository;
    private readonly IOperationRepository _operationRepository;
    private readonly IFeatureFlagService _featureFlagService;
    private readonly IContactAkuiteoSynchronizer _contactAkuiteoSynchronizer;

    public RoleCreatedEventHandler(
        ILogger<RoleCreatedEventHandler> logger,
        IRoleRegistryProvider roleRegistryProvider,
        IRoleRepository roleRepository,
        IOperationRepository operationRepository,
        IFeatureFlagService featureFlagService,
        IContactAkuiteoSynchronizer contactAkuiteoSynchronizer)
    {
        _logger = logger;
        _roleRegistryProvider = roleRegistryProvider;
        _roleRepository = roleRepository;
        _operationRepository = operationRepository;
        _featureFlagService = featureFlagService;
        _contactAkuiteoSynchronizer = contactAkuiteoSynchronizer;
    }

    public async Task HandleAsync(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            _logger.LogError("[ERREUR] Impossible de traiter l'événement car le message est null - RoleCreatedEventHandler");
            return;
        }

        var roleEvent = JsonConvert.DeserializeObject<RoleCreatedEvent>(message);

        if (roleEvent?.Data == null || roleEvent.Data.ContactId <= 0 || roleEvent.Data.AccountId < -1)
        {
            _logger.LogError("[ERREUR] Format de données invalide pour le role du contact: {ContactId} sur l'account: {AccountId}", roleEvent?.Data.ContactId, roleEvent?.Data.AccountId);
            return;
        }

        _logger.LogInformation("Consommation de l'event type: {EventType}, contactId: {ContactId}, accountId: {AccountId}",
            roleEvent.EventType,
            roleEvent.Data.ContactId,
            roleEvent.Data.AccountId);


        // Map Role to model Pulse for persist in DB
        var rolePulse = roleEvent!.Data.MapToRoleEntity();

        var existingRole = await _roleRepository.GetPulseRole(rolePulse.ContactEmail!, rolePulse.AccountNumber!);
        if (existingRole != null && (existingRole.ContactFlagPortailFactures != rolePulse.ContactFlagPortailFactures
            || existingRole.ContactFlagMainContact != rolePulse.ContactFlagMainContact))
        {
            // Synchronisation pour aligner la valeur de ContactFlagPortailFactures et ContactFlagMainContact entre Registry et Pulse.
            existingRole.ContactFlagPortailFactures = rolePulse.ContactFlagPortailFactures;
            existingRole.ContactFlagMainContact = rolePulse.ContactFlagMainContact;
            await _roleRepository.UpdatePulseRole(existingRole);
        }
        else
        {
            await _roleRepository.AddRoleAsync(rolePulse!);
        }

        // Update status operation
        await UpdateOperationProcessStatusAsync(rolePulse.ContactEmail!, rolePulse.AccountNumber!);
        await SynchronizeContactWithAkuiteoAsync(roleEvent);

        #region Flux sortant
        //var roleEntity = roleEvent!.Data.RoleEventCreatedDataToModel();
        //var responseMessage = await _roleRegistryProvider.CreateRoleAsync(roleEntity);

        //if (responseMessage.StatusCode != HttpStatusCode.OK)
        //{
        //    var errorMessage = responseMessage.Content.ReadAsAsync<HttpError>()?.Result?.Message;
        //    _logger.LogError("[ERREUR] Échec de la création de role. Cause : {ErrorMessage}. - RoleCreatedEventHandler", errorMessage);
        //    return;
        //}

        //_logger.LogInformation("Le role du contact: {ContactId} sur l'account: {AccountId} vient d'être crée.", roleEntity.ContactEmailOffice, roleEntity.AccountNumber);
        #endregion
    }

    /// <summary>
    /// Synchronizes an eligible role contact with Akuiteo when the feature is enabled.
    /// </summary>
    /// <param name="roleEvent">The role event.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task SynchronizeContactWithAkuiteoAsync(RoleCreatedEvent roleEvent)
    {
        if (!_featureFlagService.IsEnabled(FeatureFlagKeys.IsContactAkuiteoSynchronizationEnabled))
        {
            _logger.LogInformation(
                "Skipping Akuiteo contact synchronization because the feature flag is disabled. EventId: {EventId}, ContactId: {ContactId}, AccountId: {AccountId}",
                roleEvent.EventId,
                roleEvent.Data.ContactId,
                roleEvent.Data.AccountId);
            return;
        }

        _logger.LogInformation(
            "Starting Akuiteo contact synchronization from role event. EventId: {EventId}, AccountType: {AccountType}, ContactId: {ContactId}, AccountId: {AccountId}, AccountNumber: {AccountNumber}, Email: {Email}",
            roleEvent.EventId,
            roleEvent.AccountType,
            roleEvent.Data.ContactId,
            roleEvent.Data.AccountId,
            roleEvent.Data.AccountNumber,
            roleEvent.Data.ContactEmail);

        await _contactAkuiteoSynchronizer.SynchronizeAsync(roleEvent.Data);
    }

    private async Task UpdateOperationProcessStatusAsync(string email, string accountNumber)
    {
        var criteria = new OperationSearchCriteria()
        {
            OperationName = "INSERT"
        };

        var operation = await _operationRepository.FetchOperationsByCriteriaAsync(criteria, OperationStrategyType.ROLE, email, secondaryFilter: accountNumber);

        if (operation.Any())
        {
            await _operationRepository.BulkUpdateOperationsStatusAsync(ProcessStatus.Succeeded.ToString(), operation);
        }
    }
}
