using Application.Consts;
using Application.Interfaces;
using Application.Helpers.Extensions;
using Application.Options;
using Azure.Messaging.ServiceBus;
using Infrastructure.Helper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pulse.Back.Events.Abstractions;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using Pulse.Registry.Domain.Constants;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities;
using Registry.Infrastructure.Managers;

namespace Infrastructure.Orchestrators;

public class AccountOrchestrator : IAccountOrchestrator
{
    private readonly ILogger<AccountOrchestrator> logger;
    private readonly RefContext refcontext;
    private readonly INotificationManager notificationManager;
    private readonly IOptions<BackGroundJobOptions> options;
    private readonly IServiceBusMessageFactory serviceBusMessageFactory;
    private readonly IOperationService operationService;
    private readonly List<ServiceBusMessage> messagesToSendInBatch = [];

    public AccountOrchestrator(ILogger<AccountOrchestrator> logger,
        RefContext refcontext,
        INotificationManager notificationManager,
        IOptions<BackGroundJobOptions> options,
        IServiceBusMessageFactory serviceBusMessageFactory,
        IOperationService operationService)
    {
        this.logger = logger;
        this.refcontext = refcontext;
        this.notificationManager = notificationManager;
        this.options = options;
        this.serviceBusMessageFactory = serviceBusMessageFactory;
        this.operationService = operationService;
    }

    public async Task ProcessAccountPublishAsync(string operationType)
    {
        await ProcessAccountsOperationsAsync(operationType);
    }

    private async Task ProcessAccountsOperationsAsync(string operationName)
    {
        try
        {
            logger.LogInformation("Start {OperationName} process accounts operations at: {Date} - ProcessAccountsOperationsAsync", operationName, DateTime.UtcNow);

            var nbOperation = 0;

            do
            {
                var operationBatch = this.operationService.GetAccountOperationRecordsBatch(operationName, this.options.Value.Chunk);
                nbOperation = operationBatch.Count;

                if (nbOperation == 0)
                {
                    break;
                }

                foreach (var row in operationBatch)
                {
                    var result = CreateRegistryAccountEventAsync(row.Operation, row.Account);
                    if (!result)
                    {
                        return;
                    }
                }

                await OrchestratorHelper.SendBatchMessageAsync<AccountOrchestrator>(messagesToSendInBatch, notificationManager, logger);
                var operations = operationBatch.Select(o => OrchestratorHelper.UpdateOperationsToPublisAt(o.Operation)).ToList();

                await this.operationService.UpdateOperationStatusListASync(ProcessStatus.Sent, operations);
            }
            while (nbOperation != 0);

            await this.operationService.TryToProceedUntilTimeoutAsync(OperationCategory.ACCOUNT, operationName);
            logger.LogInformation("Finished {OperationName} process accounts operations at: {Date} - ProcessAccountsOperationsAsync", operationName, DateTime.UtcNow);

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send Contact event data - ProcessAccountsOperationsAsync");
            throw new InvalidOperationException($"Failed to process accounts operations for {operationName}", ex);
        }
    }

    private bool CreateRegistryAccountEventAsync(RegOperationEntity operation, RefAccountEntity account)
    {
        try
        {
            ServiceBusMessage? serviceBusMessage = null;
            switch (operation.Operation)
            {
                case OperationAction.Insert:
                    var accountCreatedEvent = CreateAccountCreatedEventData(account);
                    serviceBusMessage = serviceBusMessageFactory.CreateMessage(new RegistryAccountCreatedEvent(accountCreatedEvent));
                    LogProspectAccountPublish(OperationAction.Insert, account);
                    messagesToSendInBatch.Add(serviceBusMessage);
                    break;

                case OperationAction.Delete:
                    var accountEntity = refcontext.AccountEntity.FirstOrDefault(a => a.AccountNumber.Equals(account.AccountNumber));
                    if (accountEntity is null)
                    {
                        operation.ProcessStatus = ProcessStatus.Failed;
                        refcontext.Update(operation);
                        refcontext.SaveChanges();
                    }

                    var accountRemovedEvent = new RegistryAccountRemovedEventData()
                    {
                        AccountGlobalUniqueIdentifier = accountEntity.AccountGlobalUniqueId.Value,
                        AccountNumber = account.AccountNumber!,
                    };

                    serviceBusMessage = serviceBusMessageFactory.CreateMessage(new RegistryAccountRemovedEvent(accountRemovedEvent));
                    LogProspectAccountPublish(OperationAction.Delete, account);
                    messagesToSendInBatch.Add(serviceBusMessage);
                    break;

                case OperationAction.Update:
                    var accountUpdatedEvent = CreateAccountUpdatedEventData(account);
                    serviceBusMessage = serviceBusMessageFactory.CreateMessage(new RegistryAccountUpdatedEvent(accountUpdatedEvent));
                    LogProspectAccountPublish(OperationAction.Update, account);
                    messagesToSendInBatch.Add(serviceBusMessage);
                    break;
            }
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process operation {Id} - ProcessAccountsOperationsAsync", operation.Id);
            return false;
        }
    }

    #region Events models creators
    private void LogProspectAccountPublish(string operationType, RefAccountEntity account)
    {
        if (account.AccountType.IsProspectAccount())
        {
            logger.LogInformation("Publishing {OperationType} event for prospect account {AccountNumber}", operationType, account.AccountNumber);
        }
    }

    private RegistryAccountCreatedEventData CreateAccountCreatedEventData(RefAccountEntity account)
    {
        ArgumentNullException.ThrowIfNull(account);

        return new RegistryAccountCreatedEventData()
        {
            AccountLegalName = account.LegalName,
            AccountNumber = account.AccountNumber,
            AccountFlagESCActif = account.AccountFlagStatus == 1,
            DeliveryAddressLine1 = account.DeliveryAddressLine1,
            DeliveryAddressLine2 = account.DeliveryAddressLine2,
            DeliveryAddressLine3 = account.DeliveryAddressLine3,
            DeliveryCity = account.DeliveryCity,
            DeliveryCountry = account.DeliveryCountry,
            DeliveryState = account.DeliveryState,
            DeliveryZipCode = account.DeliveryZipCode,
            AccountDeliveryEmail = account.AccountDeliveryEmail,
            AccountDeliveryFax = account.AccountDeliveryFax,
            AccountBillingEmail = account.AccountBillingEmail,
            AccountBillingFax = account.AccountBillingFax,
            AccountCodeFormeJuridique = account.AccountCodeFormeJuridique,
            AccountRoutingCode = account.AccountRoutingCode,
            AccountRoutingLabel = account.AccountRoutingLabel,
            AccountLegalFormLabel = account.AccountLegalFormLabel,
            AccountElectronicAddressId = account.AccountElectronicAddressId,
            AccountCommercialName = account.AccountCommercialName,
            AccountEmail = account.AccountEmail,
            AccountEscCategory = account.AccountEscCategory,
            AccountFormeJuridique = account.AccountFormeJuridique,
            AccountInsertedDate = account.AccountInsertedDate,
            AccountISIN = account.AccountIsin,
            AccountNafIdentifier = account.AccountNafIdentifier,
            AccountRegimeFiscal = account.AccountRegimeFiscal,
            AccountRegisterIdentification1 = account.AccountRegisterIdentification1,
            AccountSectorCode = account.AccountSectorCode,
            AccountSourceName = account.AccountSourceName,
            AccountStaffSize = account.AccountStaffSize,
            AccountStaffSizeSlice = account.AccountStaffSizeSlice,
            AccountTaxationSystem = account.AccountTaxationSystem,
            AccountTaxeValeurAjoutee = account.AccountTaxeValeurAjoutee,
            Turnover = account.AccountTurnover,
            AccountType = account.AccountType,
            AccountTypeTenueComptable = account.AccountTypeTenueComptable,
            AccountUpdatedDate = account.AccountUpdatedDate,
            BillingAddressLine1 = account.BillingAddressLine1,
            BillingAddressLine2 = account.BillingAddressLine2,
            BillingAddressLine3 = account.BillingAddressLine3,
            BillingCity = account.BillingCity,
            BillingCountry = account.BillingCountry,
            BillingState = account.BillingState,
            BillingZipCode = account.BillingZipCode,
            CreatedBy = GlobalConstants.CREATEDBYREGISTRY,
            BillingPhone = account.AccountBillingPhone,
            DeliveryPhone = account.AccountDeliveryPhone,
            AccountGlobalUniqueIdentifier = Guid.NewGuid()
        };
    }

    public RegistryAccountUpdatedEventData CreateAccountUpdatedEventData(RefAccountEntity account)
    {
        ArgumentNullException.ThrowIfNull(account);
        var existingAccount = refcontext.AccountEntity.First(x => x.AccountNumber == account.AccountNumber);

        return new RegistryAccountUpdatedEventData()
        {
            AccountGlobalUniqueIdentifier = existingAccount.AccountGlobalUniqueId!.Value,
            AccountLegalName = account.LegalName,
            AccountNumber = account.AccountNumber,
            AccountFlagESCActif = account.AccountFlagStatus == 1,
            DeliveryAddressLine1 = account.DeliveryAddressLine1,
            DeliveryAddressLine2 = account.DeliveryAddressLine2,
            DeliveryAddressLine3 = account.DeliveryAddressLine3,
            DeliveryCity = account.DeliveryCity,
            DeliveryCountry = account.DeliveryCountry,
            DeliveryState = account.DeliveryState,
            DeliveryZipCode = account.DeliveryZipCode,
            AccountDeliveryEmail = account.AccountDeliveryEmail,
            AccountDeliveryFax = account.AccountDeliveryFax,
            AccountBillingEmail = account.AccountBillingEmail,
            AccountBillingFax = account.AccountBillingFax,
            AccountCodeFormeJuridique = account.AccountCodeFormeJuridique,
            AccountRoutingCode = account.AccountRoutingCode,
            AccountRoutingLabel = account.AccountRoutingLabel,
            AccountLegalFormLabel = account.AccountLegalFormLabel,
            AccountElectronicAddressId = account.AccountElectronicAddressId,
            AccountCommercialName = account.AccountCommercialName,
            AccountEmail = account.AccountEmail,
            AccountEscCategory = account.AccountEscCategory,
            AccountFormeJuridique = account.AccountFormeJuridique,
            AccountInsertedDate = account.AccountInsertedDate,
            AccountISIN = account.AccountIsin,
            AccountNafIdentifier = account.AccountNafIdentifier,
            AccountRegimeFiscal = account.AccountRegimeFiscal,
            AccountRegisterIdentification1 = account.AccountRegisterIdentification1,
            AccountSectorCode = account.AccountSectorCode,
            AccountSourceName = account.AccountSourceName,
            AccountStaffSize = account.AccountStaffSize,
            AccountStaffSizeSlice = account.AccountStaffSizeSlice,
            AccountTaxationSystem = account.AccountTaxationSystem,
            AccountTaxeValeurAjoutee = account.AccountTaxeValeurAjoutee,
            Turnover = account.AccountTurnover,
            AccountType = account.AccountType,
            AccountTypeTenueComptable = account.AccountTypeTenueComptable,
            AccountUpdatedDate = account.AccountUpdatedDate,
            BillingAddressLine1 = account.BillingAddressLine1,
            BillingAddressLine2 = account.BillingAddressLine2,
            BillingAddressLine3 = account.BillingAddressLine3,
            BillingCity = account.BillingCity,
            BillingCountry = account.BillingCountry,
            BillingState = account.BillingState,
            BillingZipCode = account.BillingZipCode,
            CreatedBy = existingAccount.CreatedBy,
            ModifiedBy = GlobalConstants.CREATEDBYREGISTRY,
            BillingPhone = account.AccountBillingPhone,
            DeliveryPhone = account.AccountDeliveryPhone,
        };
    }

    #endregion
}
