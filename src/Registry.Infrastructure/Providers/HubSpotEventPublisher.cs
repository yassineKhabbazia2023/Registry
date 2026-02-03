// <copyright file="HubSpotEventPublisher.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Requests;
using Microsoft.Extensions.Logging;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using Registry.Infrastructure.Managers;

namespace Infrastructure.Providers;

public class HubSpotEventPublisher(
    INotificationManager notificationManager,
    IAccountRepository accountRepository,
    IContactRepository contactRepository,
    TimeProvider timeProvider,
    ILogger<HubSpotEventPublisher> logger) : IInvoiceDematerializationNotifier
{
    private const string ActionCode = "FAC-MAT-CREA";
    private const string UserTypeCustomer = "Customer";

    public async Task NotifyDematerializationCreatedAsync(string? accountNumber, HubSpotSubmissionInputRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        logger.LogInformation(
            "Starting history event creation for dematerialization. AccountNumber: {AccountNumber}, RequesterEmail: {RequesterEmail}",
            accountNumber,
            request.RequesterEmail);

        AccountHistoryEventData? account = null;
        if (!string.IsNullOrWhiteSpace(accountNumber))
        {
            var accountEntity = await accountRepository.GetAccountByNumberOrIdAsync(accountNumber);
            if (accountEntity is not null)
            {
                account = new AccountHistoryEventData
                {
                    AccountId = accountEntity.AccountId,
                    AccountNumber = accountEntity.AccountNumber,
                    LegalName = accountEntity.LegalName
                };
                logger.LogInformation(
                    "Account found for history event. AccountId: {AccountId}, LegalName: {LegalName}",
                    accountEntity.AccountId,
                    accountEntity.LegalName);
            }
            else
            {
                logger.LogWarning("Account not found for AccountNumber: {AccountNumber}", accountNumber);
            }
        }

        var vaultContactDetails = request.VaultEmail;
        if (!string.IsNullOrWhiteSpace(request.VaultEmail))
        {
            var vaultContact = await contactRepository.GetContactByEmailOrIdAsync(email: request.VaultEmail);
            if (vaultContact is not null)
            {
                vaultContactDetails = $"{vaultContact?.FirstName} {vaultContact?.LastName} ({request.VaultEmail})";
            }
        }

        var eventData = new HistoryCreatedEventData
        {
            CreationDate = timeProvider.GetUtcNow().UtcDateTime,
            Action = new ActionHistoryEventData { Code = ActionCode },
            User = new UserHistoryEventData
            {
                Name = $"{request.LastName} {request.FirstName}",
                Email = request.RequesterEmail,
                UserType = UserTypeCustomer
            },
            TargetUser = new UserHistoryEventData
            {
                Name = string.Empty,
                Email = request.DematerializationEmail,
                UserType = UserTypeCustomer
            },
            Account = account,
            Details = $"et le contact choisi pour se connecter au coffre fort est {vaultContactDetails}."
        };

        var @event = new HistoryCreatedEvent(eventData);
        await notificationManager.PublishAsync(@event);

        logger.LogInformation(
            "History event published successfully for dematerialization. ActionCode: {ActionCode}, UserEmail: {UserEmail}, TargetEmail: {TargetEmail}",
            ActionCode,
            request.RequesterEmail,
            request.DematerializationEmail);
    }
}
