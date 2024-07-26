// <copyright file="AccountService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Application.Services
{
    public class AccountService : IAccountService
    {
        private const int BATCH_SIZE = 2000;
        private readonly IAccountRepository accountRepository;
        private readonly IProcessDeltaTriggerRepository processDeltaTriggerRepository;
        private ILogger<AccountService> logger;

        public AccountService(ILogger<AccountService> logger, IAccountRepository accountRepository, IProcessDeltaTriggerRepository processDeltaTriggerRepository)
        {
            this.accountRepository = accountRepository;
            this.logger = logger;
            this.processDeltaTriggerRepository = processDeltaTriggerRepository;
        }

        public async Task StreamAccountsJsonAsync(StreamWriter streamWriter)
        {
            await using var jsonWriter = new Utf8JsonWriter(streamWriter.BaseStream, new JsonWriterOptions { Indented = true });

            jsonWriter.WriteStartArray();

            await foreach (var account in accountRepository.GetAccountsAsync())
            {
                JsonSerializer.Serialize(jsonWriter, new Models.CreAccount
                {
                    AccountGlobalUniqueIdentifier = account.Id,
                    AccountFlagEscActif = account.AccountFlagEscActif,
                    LegalName = account.LegalName,
                    AccountNumber = account.AccountNumber,
                    AccountCommercialName = account.AccountCommercialName,
                    AccountType = account.AccountType,
                    AccountEmail = account.AccountEmail,
                    AccountNafIdentifier = account.AccountNafIdentifier,
                    AccountSectorCode = account.AccountSectorCode,
                    AccountTaxeValeurAjoutee = account.AccountTaxeValeurAjoutee,
                    AccountDeliveryEmail = account.AccountDeliveryEmail,
                    AccountBillingEmail = account.AccountBillingEmail,
                    AccountTaxationSystem = account.AccountTaxationSystem,
                    AccountSourceName = account.AccountSourceName,
                    AccountISIN = account.AccountISIN,
                    AccountRegisterIdentification1 = account.AccountRegisterIdentification1,
                    AccountStaffSize = account.AccountStaffSize,
                    AccountDeliveryFax = account.AccountDeliveryFax,
                    AccountBillingFax = account.AccountBillingFax,
                    AccountTurnoverSlice = account.AccountTurnover,
                    AccountRegimeFiscal = account.AccountRegimeFiscal,
                    AccountTypeTenueComptable = account.AccountTypeTenueComptable,
                    AccountFormeJuridique = account.AccountFormeJuridique,
                    AccountStaffSizeSlice = account.AccountStaffSizeSlice,
                    AccountEscCategory = account.AccountEscCategory,
                    AccountCodeFormeJuridique = account.AccountCodeFormeJuridique,
                    AccountInsertedDate = account.AccountInsertedDate,
                    AccountUpdatedDate = account.AccountUpdatedDate,
                    CreatedBy = account.CreatedBy,
                    ModifiedBy = account.ModifiedBy,
                    DeliveryAddressLine1 = account.DeliveryAddressLine1,
                    DeliveryAddressLine2 = account.DeliveryAddressLine2,
                    DeliveryAddressLine3 = account.DeliveryAddressLine3,
                    DeliveryCity = account.DeliveryCity,
                    DeliveryZipCode = account.DeliveryZipCode,
                    DeliveryCountry = account.DeliveryCountry,
                    DeliveryState = account.DeliveryState,
                    BillingAddressLine1 = account.BillingAddressLine1,
                    BillingAddressLine2 = account.BillingAddressLine2,
                    BillingAddressLine3 = account.BillingAddressLine3,
                    BillingCity = account.BillingCity,
                    BillingZipCode = account.BillingZipCode,
                    BillingCountry = account.BillingCountry,
                    BillingState = account.BillingState,
                    DeploymentStatus = account.DeploymentStatus,
                    DeploymentDate = account.DeploymentDate,
                });
            }

            jsonWriter.WriteEndArray();
            await jsonWriter.FlushAsync();
        }

        public async Task ProcessAccountAsync(IEnumerable<AccountCsv> accounts)
        {
            var list = new List<AlxAccount>();
            foreach(var a in accounts)
            {
                var account = new AlxAccount
                {
                    AccountGlobalUniqueIdentifier = a.AccountGlobalUniqueIdentifier,
                    AccountFlagEscActif = a.AccountFlagEscActif,
                    LegalName = a.LegalName,
                    AccountNumber = !string.IsNullOrEmpty(a.AccountNumber) ? a.AccountNumber : null,
                    AccountCommercialName = !string.IsNullOrEmpty(a.AccountCommercialName) ? a.AccountCommercialName : null,
                    AccountType = !string.IsNullOrEmpty(a.AccountType) ? a.AccountType : null,
                    AccountEmail = !string.IsNullOrEmpty(a.AccountEmail) ? a.AccountEmail : null,
                    AccountNafIdentifier = !string.IsNullOrEmpty(a.AccountNafIdentifier) ? a.AccountNafIdentifier : null,
                    AccountSectorCode = !string.IsNullOrEmpty(a.AccountSectorCode) ? a.AccountSectorCode : null,
                    AccountTaxeValeurAjoutee = !string.IsNullOrEmpty(a.AccountTaxeValeurAjoutee) ? a.AccountTaxeValeurAjoutee : null,
                    AccountDeliveryEmail = !string.IsNullOrEmpty(a.AccountDeliveryEmail) ? a.AccountDeliveryEmail : null,
                    AccountBillingEmail = !string.IsNullOrEmpty(a.AccountBillingEmail) ? a.AccountBillingEmail : null,
                    AccountTaxationSystem = !string.IsNullOrEmpty(a.AccountTaxationSystem) ? a.AccountTaxationSystem : null,
                    AccountSourceName = !string.IsNullOrEmpty(a.AccountSourceName) ? a.AccountSourceName : null,
                    AccountISIN = !string.IsNullOrEmpty(a.AccountISIN) ? a.AccountISIN : null,
                    AccountRegisterIdentification1 = !string.IsNullOrEmpty(a.AccountRegisterIdentification1) ? a.AccountRegisterIdentification1 : null,
                    AccountStaffSize = !string.IsNullOrEmpty(a.AccountStaffSize) ? a.AccountStaffSize : null,
                    AccountDeliveryFax = !string.IsNullOrEmpty(a.AccountDeliveryFax) ? a.AccountDeliveryFax : null,
                    AccountBillingFax = !string.IsNullOrEmpty(a.AccountBillingFax) ? a.AccountBillingFax : null,
                    AccountTurnover = !string.IsNullOrEmpty(a.AccountTurnover) ? a.AccountTurnover : null,
                    AccountRegimeFiscal = !string.IsNullOrEmpty(a.AccountRegimeFiscal) ? a.AccountRegimeFiscal : null,
                    AccountTypeTenueComptable = !string.IsNullOrEmpty(a.AccountTypeTenueComptable) ? a.AccountTypeTenueComptable : null,
                    AccountFormeJuridique = !string.IsNullOrEmpty(a.AccountFormeJuridique) ? a.AccountFormeJuridique : null,
                    AccountStaffSizeSlice = !string.IsNullOrEmpty(a.AccountStaffSizeSlice) ? a.AccountStaffSizeSlice : null,
                    AccountEscCategory = !string.IsNullOrEmpty(a.AccountEscCategory) ? a.AccountEscCategory : null,
                    AccountCodeFormeJuridique = !string.IsNullOrEmpty(a.AccountCodeFormeJuridique) ? a.AccountCodeFormeJuridique : null,
                    AccountInsertedDate = !string.IsNullOrWhiteSpace(a.AccountInsertedDate) ? DateTime.Parse(a.AccountInsertedDate) : null,
                    AccountUpdatedDate = !string.IsNullOrWhiteSpace(a.AccountUpdatedDate) ? DateTime.Parse(a.AccountUpdatedDate) : null,
                    CreatedBy = !string.IsNullOrEmpty(a.CreatedBy) ? a.CreatedBy : null,
                    ModifiedBy = !string.IsNullOrEmpty(a.ModifiedBy) ? a.ModifiedBy : null,
                    DeliveryAddressLine1 = !string.IsNullOrEmpty(a.DeliveryAddressLine1) ? a.DeliveryAddressLine1 : null,
                    DeliveryAddressLine2 = !string.IsNullOrEmpty(a.DeliveryAddressLine2) ? a.DeliveryAddressLine2 : null,
                    DeliveryAddressLine3 = !string.IsNullOrEmpty(a.DeliveryAddressLine3) ? a.DeliveryAddressLine3 : null,
                    DeliveryCity = !string.IsNullOrEmpty(a.DeliveryCity) ? a.DeliveryCity : null,
                    DeliveryZipCode = !string.IsNullOrEmpty(a.DeliveryZipCode) ? a.DeliveryZipCode : null,
                    DeliveryCountry = !string.IsNullOrEmpty(a.DeliveryCountry) ? a.DeliveryCountry : null,
                    DeliveryState = !string.IsNullOrEmpty(a.DeliveryState) ? a.DeliveryState : null,
                    BillingAddressLine1 = !string.IsNullOrEmpty(a.BillingAddressLine1) ? a.BillingAddressLine1 : null,
                    BillingAddressLine2 = !string.IsNullOrEmpty(a.BillingAddressLine2) ? a.BillingAddressLine2 : null,
                    BillingAddressLine3 = !string.IsNullOrEmpty(a.BillingAddressLine3) ? a.BillingAddressLine3 : null,
                    BillingCity = !string.IsNullOrEmpty(a.BillingCity) ? a.BillingCity : null,
                    BillingZipCode = !string.IsNullOrEmpty(a.BillingZipCode) ? a.BillingZipCode : null,
                    BillingCountry = !string.IsNullOrEmpty(a.BillingCountry) ? a.BillingCountry : null,
                    BillingState = !string.IsNullOrEmpty(a.BillingState) ? a.BillingState : null,
                    DeploymentStatus = !string.IsNullOrEmpty(a.DeploymentStatus) ? a.DeploymentStatus : null,
                    DeploymentDate = !string.IsNullOrWhiteSpace(a.DeploymentDate) ? DateTime.Parse(a.DeploymentDate) : null,
                    AccountDeliveryPhone = !string.IsNullOrWhiteSpace(a.AccountDeliveryPhone) ? a.AccountDeliveryPhone : null,
                    AccountBillingPhone = !string.IsNullOrEmpty(a.AccountBillingPhone) ? a.AccountBillingPhone : null
                };
                list.Add(account);
                if (list.Count == BATCH_SIZE)
                {
                    await this.accountRepository.AddAccountsAsync(list);
                    list.Clear();
                }
            }

            if(list.Count> 0)
            {
                await this.accountRepository.AddAccountsAsync(list);
            }
            await this.processDeltaTriggerRepository.UpdateAccountProcessAsync(true);
            var countResult = await this.accountRepository.GetCountAccountActifAsync();
            logger.LogInformation("CreAccountActif count:{countCRE} ,  AlxAccountActif count: {countAlx}", countResult.creAccountActif, countResult.alxAccountActif);
        }

        public async Task ClearAlxAsync()
        {
            await this.accountRepository.ClearAlxAsync();
        }
    }
}
