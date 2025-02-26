using Application.Consts;
using Application.Interfaces;
using Application.Models;
using Application.Options;
using Azure.Messaging.ServiceBus;
using Domain.Constants;
using Infrastructure.Helper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pulse.Back.Events.Abstractions;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using Pulse.Registry.Domain.Constants;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities;
using Registry.Application.Consts;
using Registry.Infrastructure.Managers;

namespace Infrastructure.Orchestrators
{
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
            logger.LogInformation("Send Account event data started at: {Date} - ProcessAccountsOperationsAsync", DateTime.UtcNow);

            var nbOperation = 0;

            do
            {
                var operationBatch = await GeAccountsOperationDetailsAsync(operationName, this.options.Value.Chunk);

                nbOperation = operationBatch.Count;

                if (nbOperation == 0)
                {
                    break;
                }

                foreach (var row in operationBatch)
                {
                    CreateRegistryAccountEventAsync(row.Operation, row.Account);
                }

                await OrchestratorHelper.SendBatchMessageAsync<AccountOrchestrator>(messagesToSendInBatch, notificationManager, logger);
                var operations = operationBatch.Select(o => OrchestratorHelper.UpdateOperationsToPublisAt(o.Operation)).ToList();

                await this.operationService.UpdateOperationStatusListASync(ProcessStatus.Sent, operations);
            }
            while (nbOperation != 0);

            await this.operationService.TryToProceedUntilTimeoutAsync(OperationTypeConsts.ACCOUNT, operationName);

            logger.LogInformation("Send Account event data finished at: {Date} - ProcessAccountsOperationsAsync", DateTime.UtcNow);
        }

        private void CreateRegistryAccountEventAsync(RegOperationEntity operation, RefAccountEntity account)
        {
            ServiceBusMessage? serviceBusMessage = null;
            switch (operation.Operation)
            {
                case OperationName.Insert:
                    var accountCreatedEvent = CreateAccountCreatedEventData(account);
                    serviceBusMessage = serviceBusMessageFactory.CreateMessage(new RegistryAccountCreatedEvent(accountCreatedEvent));
                    messagesToSendInBatch.Add(serviceBusMessage);
                    break;

                case OperationName.Delete:
                    var accountEntity = refcontext.AccountEntities.FirstOrDefault(a => a.AccountNumber.Equals(account.AccountNumber));
                    if (accountEntity is null)
                    {
                        operation.ProcessStatus = ProcessStatus.Failed;
                        refcontext.Update(operation);
                        refcontext.SaveChanges();
                        break;
                    }

                    var accountRemovedEvent = new RegistryAccountRemovedEventData()
                    {
                        AccountGlobalUniqueIdentifier = accountEntity.AccountGlobalUniqueId.Value,
                        AccountNumber = account.AccountNumber!,
                    };

                    serviceBusMessage = serviceBusMessageFactory.CreateMessage(new RegistryAccountRemovedEvent(accountRemovedEvent));
                    messagesToSendInBatch.Add(serviceBusMessage);
                    break;

                case OperationName.Update:
                    var accountUpdatedEvent = CreateAccountUpdatedEventData(account);
                    serviceBusMessage = serviceBusMessageFactory.CreateMessage(new RegistryAccountUpdatedEvent(accountUpdatedEvent));
                    messagesToSendInBatch.Add(serviceBusMessage);
                    break;
            }
        }

        private async Task<List<AccountOperationDetail>> GeAccountsOperationDetailsAsync(string operationName, int chuckSize)
        {
            var query = from operation in this.refcontext.RegOperationEntity
                        join account in this.refcontext.RefAccountEntity
                        on operation.EntityId equals account.EntityId
                        where operation.Type == OperationTypeConsts.ACCOUNT && operation.Operation.Equals(operationName)
                        && operation.PublishedAt == null
                        && operation.ApprovalStatus == ApprovalStatus.Approved
                        select new AccountOperationDetail() { Operation = operation, Account = account };

            var operationBatch = await query
                .Take(chuckSize)
                .ToListAsync();

            return operationBatch;
        }

        #region Events models creators
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
            var existingAccount = this.refcontext.AccountEntities.First(x => x.AccountNumber == account.AccountNumber);

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
                DeploymentDate = existingAccount.DeploymentDate,
                DeploymentStatus = existingAccount.DeploymentStatus,
                AccountDeliveryEmail = account.AccountDeliveryEmail,
                AccountDeliveryFax = account.AccountDeliveryFax,
                AccountBillingEmail = account.AccountBillingEmail,
                AccountBillingFax = account.AccountBillingFax,
                AccountCodeFormeJuridique = account.AccountCodeFormeJuridique,
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
}
