// <copyright file="PennylaneUserRemovedEventHandler.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Enums;
using Application.Exceptions;
using Application.Interfaces;
using Application.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Pulse.Back.Events.Abstractions;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.Back.Events.IntegrationEvents.EventsData;

namespace Infrastructure.Handlers
{
    /// <summary>
    /// Handles Pennylane user removal events by deleting corresponding roles.
    /// </summary>
    public class PennylaneUserRemovedEventHandler : IEventHandler
    {
        private readonly ILogger<PennylaneUserRemovedEventHandler> _logger;
        private readonly IRoleService _roleService;

        public PennylaneUserRemovedEventHandler(
            IRoleService roleService,
            ILogger<PennylaneUserRemovedEventHandler> logger)
        {
            _roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task HandleAsync(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                _logger.LogError("[{Handler}] Empty or whitespace message received.", nameof(PennylaneUserRemovedEventHandler));
                return;
            }

            var data = DeserializeEvent(message);
            if (data == null)
            {
                return;
            }

            await ProcessRolesAsync(data);
        }

        private PennylaneUserRemovedEventData? DeserializeEvent(string message)
        {
            try
            {
                var integrationEvent = JsonConvert.DeserializeObject<PennylaneUserRemovedEvent>(message);
                if (integrationEvent?.Data == null)
                {
                    _logger.LogError(
                        "[{Handler}] Deserialized event data is null.",
                        nameof(PennylaneUserRemovedEventHandler));
                    return null;
                }

                return integrationEvent.Data;
            }
            catch (JsonException ex)
            {
                _logger.LogError(
                    ex,
                    "[{Handler}] JSON deserialization failed for PennylaneUserRemovedEvent.",
                    nameof(PennylaneUserRemovedEventHandler));
                return null;
            }
        }

        private async Task ProcessRolesAsync(PennylaneUserRemovedEventData data)
        {
            var companies = data.CompanyIds ?? Enumerable.Empty<string>();
            if (!companies.Any())
            {
                _logger.LogWarning(
                    "[{Handler}] No companies provided for user {Email}; role deletions skipped.",
                    nameof(PennylaneUserRemovedEventHandler),
                    data.Email);
                return;
            }

            var roles = companies.Select(accountNumber => new RefRoleCsv
            {
                AccountNumber = accountNumber,
                ContactEmail = data.Email,
                Description = string.Empty,
                RoleFlagStatus = 0,
                Operation = OperationAction.Delete,
               
            }).ToList();

            try
            {
                await _roleService.InsertRolesAsync(roles, DataSources.PENNYLANE.ToString());
                var accountList = string.Join(",", companies);
                _logger.LogInformation(
                    "[{Handler}] Deleted roles for user {Email} on accounts [{Accounts}].",
                    nameof(PennylaneUserRemovedEventHandler),
                    data.Email,
                    accountList);
            }
            catch (DbOperationException dbEx)
            {
                _logger.LogError(
                    dbEx,
                    "[{Handler}] Database error during role deletion for {Email}.",
                    nameof(PennylaneUserRemovedEventHandler),
                    data.Email);
            }
        }
    }
}
