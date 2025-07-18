// <copyright file="PennylaneUserCreatedEventHandler.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Enums;
using Application.Exceptions;
using Application.Interfaces;
using Application.Models;
using Application.services;
using Infrastructure.Orchestrators;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Pulse.Back.Events.Abstractions;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.Back.Events.IntegrationEvents.EventsData;

namespace Infrastructure.Handlers
{
    public class PennylaneUserCreatedEventHandler : IEventHandler
    {
        private readonly ILogger<PennylaneUserCreatedEventHandler> _logger;
        private readonly IContactsDeepValidationsService _contactsDeepValidationsService;
        private readonly IRoleOrchestrator _roleOrchestrator;
        private readonly IContactOrchestrator _contactOrchestrator;
        private readonly IContactService _contactService;
        private readonly IRoleService _roleService;

        public PennylaneUserCreatedEventHandler(
            IContactService contactService,
            IRoleService roleService,
            ILogger<PennylaneUserCreatedEventHandler> logger,
            IContactsDeepValidationsService contactsDeepValidationsService,
            IRoleOrchestrator roleOrchestrator,
            IContactOrchestrator contactOrchestrator)
        {
            _contactService = contactService ?? throw new ArgumentNullException(nameof(contactService));
            _roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _contactsDeepValidationsService = contactsDeepValidationsService;
            _roleOrchestrator = roleOrchestrator;
            _contactOrchestrator = contactOrchestrator;
        }

        public async Task HandleAsync(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                _logger.LogError("[{Handler}] Incoming message is empty or whitespace.", nameof(PennylaneUserCreatedEventHandler));
                return;
            }

            var data = DeserializeEvent(message);
            if (data == null)
            {
                return;
            }

            await ProcessContactAsync(data);
            await ProcessRolesAsync(data);

            await SynchronizeContactsAndRolesAsync();
        }

        private PennylaneUserCreatedEventData? DeserializeEvent(string message)
        {
            try
            {
                var integrationEvent = JsonConvert.DeserializeObject<PennylaneUserCreatedEvent>(message);
                if (integrationEvent?.Data == null)
                {
                    _logger.LogError(
                        "[{Handler}] Event data is null after deserialization.",
                        nameof(PennylaneUserCreatedEventHandler));
                    return null;
                }

                return integrationEvent.Data;
            }
            catch (JsonException ex)
            {
                _logger.LogError(
                    ex,
                    "[{Handler}] Failed to deserialize message into PennylaneUserCreatedEvent.",
                    nameof(PennylaneUserCreatedEventHandler));
                return null;
            }
        }

        private async Task ProcessContactAsync(PennylaneUserCreatedEventData data)
        {
            if (string.IsNullOrWhiteSpace(data.Email))
            {
                _logger.LogError(
                    "[{Handler}] Missing required field 'Email' in event data.",
                    nameof(PennylaneUserCreatedEventHandler));
                return;
            }

            var contact = new RefContactCsv
            {
                FirstName = data.FirstName,
                LastName = data.LastName,
                Email = data.Email,
                MobilePhone = data.PhoneNumber,
                IsCustomer = true,
                Operation = OperationAction.Insert
            };

            try
            {
                await _contactService.InsertContactsAsync(new List<RefContactCsv> { contact },DataSources.PENNYLANE.ToString());
                _logger.LogInformation(
                    "[{Handler}] Created contact for user {Email}.",
                    nameof(PennylaneUserCreatedEventHandler),
                    contact.Email);
            }
            catch (DbOperationException ex)
            {
                _logger.LogError(
                    ex,
                    "[{Handler}] Database error while inserting contact for {Email}.",
                    nameof(PennylaneUserCreatedEventHandler),
                    contact.Email);
            }
        }

        private async Task ProcessRolesAsync(PennylaneUserCreatedEventData data)
        {
            if (data.CompanyIds == null || !data.CompanyIds.Any())
            {
                _logger.LogWarning(
                    "[{Handler}] No CompanyIds provided for user {Email}; skipping role creation.",
                    nameof(PennylaneUserCreatedEventHandler),
                    data.Email);
                return;
            }

            var roles = data.CompanyIds.Select(accountNumber => new RefRoleCsv
            {
                AccountNumber = accountNumber,
                ContactEmail = data.Email,
                Description = data.Role,
                RoleFlagStatus = 1,
                Operation = OperationAction.Insert
            }).ToList();

            try
            {
                await _roleService.InsertRolesAsync(roles, DataSources.PENNYLANE.ToString());
                var accounts = string.Join(",", data.CompanyIds);
                _logger.LogInformation(
                    "[{Handler}] Created roles for user {Email} on accounts [{Accounts}].",
                    nameof(PennylaneUserCreatedEventHandler),
                    data.Email,
                    accounts);
            }
            catch (DbOperationException ex)
            {
                _logger.LogError(
                    ex,
                    "[{Handler}] Database error while inserting roles for {Email}.",
                    nameof(PennylaneUserCreatedEventHandler),
                    data.Email);
            }
        }

        private async Task SynchronizeContactsAndRolesAsync()
        {   
            // Create and validate operations
            await _contactsDeepValidationsService.CreateValidContactsOperationsAsync();

            await _roleService.CreateValidRolesOperationsAsync();

            // Orchestrate and trigger events
            await _contactOrchestrator.ProcessContactPublishAsync("INSERT");

            await _roleOrchestrator.ProcessRolePublishAsync("INSERT", false);
        }
    }
}
