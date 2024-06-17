// <copyright file="AccountService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models;
using Domain.Entities;
using System.Text.Json;

namespace Application.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository accountRepository;
        private readonly IProcessDeltaTriggerRepository processDeltaTriggerRepository;

        public AccountService(IAccountRepository accountRepository, IProcessDeltaTriggerRepository processDeltaTriggerRepository)
        {
            this.accountRepository = accountRepository;
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
                    AccountTurnoverSlice = account.AccountTurnoverSlice,
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
            var accountsAlx = accounts
                .Select(
                a => new AlxAccount 
                        {
                    AccountGlobalUniqueIdentifier = a.AccountGlobalUniqueIdentifier,
                    AccountFlagEscActif = a.AccountFlagEscActif,
                    LegalName = a.LegalName,
                    AccountNumber = a.AccountNumber,
                    AccountCommercialName = a.AccountCommercialName,
                    AccountType = a.AccountType,
                    AccountEmail = a.AccountEmail,
                    AccountNafIdentifier = a.AccountNafIdentifier,
                    AccountSectorCode = a.AccountSectorCode,
                    AccountTaxeValeurAjoutee = a.AccountTaxeValeurAjoutee,
                    AccountDeliveryEmail = a.AccountDeliveryEmail,
                    AccountBillingEmail = a.AccountBillingEmail,
                    AccountTaxationSystem = a.AccountTaxationSystem,
                    AccountSourceName = a.AccountSourceName,
                    AccountISIN = a.AccountISIN,
                    AccountRegisterIdentification1 = a.AccountRegisterIdentification1,
                    AccountStaffSize = a.AccountStaffSize,
                    AccountDeliveryFax = a.AccountDeliveryFax,
                    AccountBillingFax = a.AccountBillingFax,
                    AccountTurnoverSlice = a.AccountTurnoverSlice,
                    AccountRegimeFiscal = a.AccountRegimeFiscal,
                    AccountTypeTenueComptable = a.AccountTypeTenueComptable,
                    AccountFormeJuridique = a.AccountFormeJuridique,
                    AccountStaffSizeSlice = a.AccountStaffSizeSlice,
                    AccountEscCategory = a.AccountEscCategory,
                    AccountCodeFormeJuridique = a.AccountCodeFormeJuridique,
                    AccountInsertedDate = !string.IsNullOrWhiteSpace(a.AccountInsertedDate) ? DateTime.Parse(a.AccountInsertedDate) : null,
                    AccountUpdatedDate = !string.IsNullOrWhiteSpace(a.AccountUpdatedDate) ?  DateTime.Parse(a.AccountUpdatedDate) : null,
                    CreatedBy = a.CreatedBy,
                    ModifiedBy = a.ModifiedBy,
                    DeliveryAddressLine1 = a.DeliveryAddressLine1,
                    DeliveryAddressLine2 = a.DeliveryAddressLine2,
                    DeliveryAddressLine3 = a.DeliveryAddressLine3,
                    DeliveryCity = a.DeliveryCity,
                    DeliveryZipCode = a.DeliveryZipCode,
                    DeliveryCountry = a.DeliveryCountry,
                    DeliveryState = a.DeliveryState,
                    BillingAddressLine1 = a.BillingAddressLine1,
                    BillingAddressLine2 = a.BillingAddressLine2,
                    BillingAddressLine3 = a.BillingAddressLine3,
                    BillingCity = a.BillingCity,
                    BillingZipCode = a.BillingZipCode,
                    BillingCountry = a.BillingCountry,
                    BillingState = a.BillingState,
                    DeploymentStatus = a.DeploymentStatus,
                    DeploymentDate = !string.IsNullOrWhiteSpace(a.DeploymentDate) ? DateTime.Parse(a.DeploymentDate) : null,
                }).ToList();
            await this.accountRepository.AddAccountsAsync(accountsAlx);
            await this.processDeltaTriggerRepository.UpdateAccountProcessAsync(true);
        }
    }
}
