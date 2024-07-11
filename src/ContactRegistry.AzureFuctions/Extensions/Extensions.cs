// <copyright file="Extensions.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Domain.Entities;
using Pulse.Back.Events.IntegrationEvents.EventsData;

namespace ContactRegistry.AzureFuctions
{
    /// <summary>
    /// Extensions.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// ToRegistryAccountCreatedEventData.
        /// </summary>
        /// <param name="creAccount">creAccount.</param>
        /// <returns>A <see cref="RegistryAccountCreatedEventData"/> representing the registry account created message.</returns>
        public static RegistryAccountCreatedEventData ToRegistryAccountCreatedEventData(this CreAccount creAccount)
        {
            ArgumentNullException.ThrowIfNull(creAccount);

            return new RegistryAccountCreatedEventData()
            {
                AccountGlobalUniqueIdentifier = creAccount.Id,
                AccountLegalName = creAccount.LegalName,
                AccountNumber = creAccount.AccountNumber,
                AccountFlagESCActif = creAccount.AccountFlagEscActif,
                DeliveryAddressLine1 = creAccount.DeliveryAddressLine1,
                DeliveryAddressLine2 = creAccount.DeliveryAddressLine2,
                DeliveryAddressLine3 = creAccount.DeliveryAddressLine3,
                DeliveryCity = creAccount.DeliveryCity,
                DeliveryCountry = creAccount.DeliveryCountry,
                DeliveryState = creAccount.DeliveryState,
                DeliveryZipCode = creAccount.DeliveryZipCode,
                DeploymentDate = creAccount.DeploymentDate,
                DeploymentStatus = creAccount.DeploymentStatus,
                AccountDeliveryEmail = creAccount.AccountDeliveryEmail,
                AccountDeliveryFax = creAccount.AccountDeliveryFax,
                AccountBillingEmail = creAccount.AccountBillingEmail,
                AccountBillingFax = creAccount.AccountBillingFax,
                AccountCodeFormeJuridique = creAccount.AccountCodeFormeJuridique,
                AccountCommercialName = creAccount.AccountCommercialName,
                AccountEmail = creAccount.AccountEmail,
                AccountEscCategory = creAccount.AccountEscCategory,
                AccountFormeJuridique = creAccount.AccountFormeJuridique,
                AccountInsertedDate = creAccount.AccountInsertedDate,
                AccountISIN = creAccount.AccountISIN,
                AccountNafIdentifier = creAccount.AccountNafIdentifier,
                AccountRegimeFiscal = creAccount.AccountRegimeFiscal,
                AccountRegisterIdentification1 = creAccount.AccountRegisterIdentification1,
                AccountSectorCode = creAccount.AccountSectorCode,
                AccountSourceName = creAccount.AccountSourceName,
                AccountStaffSize = creAccount.AccountStaffSize,
                AccountStaffSizeSlice = creAccount.AccountStaffSizeSlice,
                AccountTaxationSystem = creAccount.AccountTaxationSystem,
                AccountTaxeValeurAjoutee = creAccount.AccountTaxeValeurAjoutee,
                Turnover = creAccount.AccountTurnover,
                AccountType = creAccount.AccountType,
                AccountTypeTenueComptable = creAccount.AccountTypeTenueComptable,
                AccountUpdatedDate = creAccount.AccountUpdatedDate,
                BillingAddressLine1 = creAccount.BillingAddressLine1,
                BillingAddressLine2 = creAccount.BillingAddressLine2,
                BillingAddressLine3 = creAccount.BillingAddressLine3,
                BillingCity = creAccount.BillingCity,
                BillingCountry = creAccount.BillingCountry,
                BillingState = creAccount.BillingState,
                BillingZipCode = creAccount.BillingZipCode,
                CreatedBy = creAccount.CreatedBy,
                ModifiedBy = creAccount.ModifiedBy,
                BillingPhone = creAccount.BillingPhone,
                DeliveryPhone = creAccount.DeliveryPhone,
            };
        }

        /// <summary>
        /// ToRegistryAccountCreatedEventData.
        /// </summary>
        /// <param name="creAccount">creAccount.</param>
        /// <returns>A <see cref="RegistryAccountUpdatedEventData"/> representing the registry account created message.</returns>
        public static RegistryAccountUpdatedEventData ToRegistryAccountUpdatedEventData(this CreAccount creAccount)
        {
            ArgumentNullException.ThrowIfNull(creAccount);

            return new RegistryAccountUpdatedEventData()
            {
                AccountGlobalUniqueIdentifier = creAccount.Id,
                AccountLegalName = creAccount.LegalName,
                AccountNumber = creAccount.AccountNumber,
                AccountFlagESCActif = creAccount.AccountFlagEscActif,
                DeliveryAddressLine1 = creAccount.DeliveryAddressLine1,
                DeliveryAddressLine2 = creAccount.DeliveryAddressLine2,
                DeliveryAddressLine3 = creAccount.DeliveryAddressLine3,
                DeliveryCity = creAccount.DeliveryCity,
                DeliveryCountry = creAccount.DeliveryCountry,
                DeliveryState = creAccount.DeliveryState,
                DeliveryZipCode = creAccount.DeliveryZipCode,
                DeploymentDate = creAccount.DeploymentDate,
                DeploymentStatus = creAccount.DeploymentStatus,
                AccountDeliveryEmail = creAccount.AccountDeliveryEmail,
                AccountDeliveryFax = creAccount.AccountDeliveryFax,
                AccountBillingEmail = creAccount.AccountBillingEmail,
                AccountBillingFax = creAccount.AccountBillingFax,
                AccountCodeFormeJuridique = creAccount.AccountCodeFormeJuridique,
                AccountCommercialName = creAccount.AccountCommercialName,
                AccountEmail = creAccount.AccountEmail,
                AccountEscCategory = creAccount.AccountEscCategory,
                AccountFormeJuridique = creAccount.AccountFormeJuridique,
                AccountInsertedDate = creAccount.AccountInsertedDate,
                AccountISIN = creAccount.AccountISIN,
                AccountNafIdentifier = creAccount.AccountNafIdentifier,
                AccountRegimeFiscal = creAccount.AccountRegimeFiscal,
                AccountRegisterIdentification1 = creAccount.AccountRegisterIdentification1,
                AccountSectorCode = creAccount.AccountSectorCode,
                AccountSourceName = creAccount.AccountSourceName,
                AccountStaffSize = creAccount.AccountStaffSize,
                AccountStaffSizeSlice = creAccount.AccountStaffSizeSlice,
                AccountTaxationSystem = creAccount.AccountTaxationSystem,
                AccountTaxeValeurAjoutee = creAccount.AccountTaxeValeurAjoutee,
                Turnover = creAccount.AccountTurnover,
                AccountType = creAccount.AccountType,
                AccountTypeTenueComptable = creAccount.AccountTypeTenueComptable,
                AccountUpdatedDate = creAccount.AccountUpdatedDate,
                BillingAddressLine1 = creAccount.BillingAddressLine1,
                BillingAddressLine2 = creAccount.BillingAddressLine2,
                BillingAddressLine3 = creAccount.BillingAddressLine3,
                BillingCity = creAccount.BillingCity,
                BillingCountry = creAccount.BillingCountry,
                BillingState = creAccount.BillingState,
                BillingZipCode = creAccount.BillingZipCode,
                CreatedBy = creAccount.CreatedBy,
                ModifiedBy = creAccount.ModifiedBy,
                BillingPhone = creAccount.BillingPhone,
                DeliveryPhone = creAccount.DeliveryPhone,
            };
        }
    }
}
