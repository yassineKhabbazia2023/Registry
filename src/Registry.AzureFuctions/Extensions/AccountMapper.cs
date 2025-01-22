// <copyright file="AccountMapper.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Domain.Constants;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using Pulse.ContactRegistry.Domain.Constants;
using Pulse.ContactRegistry.Infrastructure.Entities;

namespace ContactRegistry.AzureFuctions;

/// <summary>
/// Extensions.
/// </summary>
public static class AccountMapper
{

    /// <summary>
    /// ToRegAccountCreatedEventData.
    /// </summary>
    /// <param name="regAccount">regAccount.</param>
    /// <returns>A <see cref="RegistryAccountCreatedEventData"/> representing the registry account created message.</returns>
    public static RegistryAccountCreatedEventData ToRegAccountCreatedEventData(this RegAccountEntity regAccount)
    {
        ArgumentNullException.ThrowIfNull(regAccount);

        return new RegistryAccountCreatedEventData()
        {
            AccountGlobalUniqueIdentifier = regAccount.Id,
            AccountLegalName = regAccount.LegalName,
            AccountNumber = regAccount.AccountNumber,
            AccountFlagESCActif = regAccount.AccountFlagEscactif,
            DeliveryAddressLine1 = regAccount.DeliveryAddressLine1,
            DeliveryAddressLine2 = regAccount.DeliveryAddressLine2,
            DeliveryAddressLine3 = regAccount.DeliveryAddressLine3,
            DeliveryCity = regAccount.DeliveryCity,
            DeliveryCountry = regAccount.DeliveryCountry,
            DeliveryState = regAccount.DeliveryState,
            DeliveryZipCode = regAccount.DeliveryZipCode,
            DeploymentDate = regAccount.DeploymentDate,
            DeploymentStatus = string.IsNullOrEmpty(regAccount.DeploymentStatus) ? DeploymentStatusEnum.ToDeploy.ToString("d") : regAccount.DeploymentStatus,
            AccountDeliveryEmail = regAccount.AccountDeliveryEmail,
            AccountDeliveryFax = regAccount.AccountDeliveryFax,
            AccountBillingEmail = regAccount.AccountBillingEmail,
            AccountBillingFax = regAccount.AccountBillingFax,
            AccountCodeFormeJuridique = regAccount.AccountCodeFormeJuridique,
            AccountCommercialName = regAccount.AccountCommercialName,
            AccountEmail = regAccount.AccountEmail,
            AccountEscCategory = regAccount.AccountEscCategory,
            AccountFormeJuridique = regAccount.AccountFormeJuridique,
            AccountInsertedDate = regAccount.AccountInsertedDate,
            AccountISIN = regAccount.AccountIsin,
            AccountNafIdentifier = regAccount.AccountNafIdentifier,
            AccountRegimeFiscal = regAccount.AccountRegimeFiscal,
            AccountRegisterIdentification1 = regAccount.AccountRegisterIdentification1,
            AccountSectorCode = regAccount.AccountSectorCode,
            AccountSourceName = regAccount.AccountSourceName,
            AccountStaffSize = regAccount.AccountStaffSize,
            AccountStaffSizeSlice = regAccount.AccountStaffSizeSlice,
            AccountTaxationSystem = regAccount.AccountTaxationSystem,
            AccountTaxeValeurAjoutee = regAccount.AccountTaxeValeurAjoutee,
            Turnover = regAccount.AccountTurnover,
            AccountType = regAccount.AccountType,
            AccountTypeTenueComptable = regAccount.AccountTypeTenueComptable,
            AccountUpdatedDate = regAccount.AccountUpdatedDate,
            BillingAddressLine1 = regAccount.BillingAddressLine1,
            BillingAddressLine2 = regAccount.BillingAddressLine2,
            BillingAddressLine3 = regAccount.BillingAddressLine3,
            BillingCity = regAccount.BillingCity,
            BillingCountry = regAccount.BillingCountry,
            BillingState = regAccount.BillingState,
            BillingZipCode = regAccount.BillingZipCode,
            CreatedBy = string.IsNullOrEmpty(regAccount.CreatedBy) ? GlobalConstants.CREATEDBYREGISTRY : regAccount.CreatedBy,
            ModifiedBy = regAccount.ModifiedBy,
            BillingPhone = regAccount.BillingPhone,
            DeliveryPhone = regAccount.DeliveryPhone,
        };
    }

    /// <summary>
    /// ToRegAccountUpdatedEventData.
    /// </summary>
    /// <param name="regAccount">regAccount.</param>
    /// <returns>A <see cref="RegistryAccountUpdatedEventData"/> representing the registry account created message.</returns>
    public static RegistryAccountUpdatedEventData ToRegAccountUpdatedEventData(this RegAccountEntity regAccount)
    {
        ArgumentNullException.ThrowIfNull(regAccount);

        return new RegistryAccountUpdatedEventData()
        {
            AccountGlobalUniqueIdentifier = regAccount.Id,
            AccountLegalName = regAccount.LegalName,
            AccountNumber = regAccount.AccountNumber,
            AccountFlagESCActif = regAccount.AccountFlagEscactif,
            DeliveryAddressLine1 = regAccount.DeliveryAddressLine1,
            DeliveryAddressLine2 = regAccount.DeliveryAddressLine2,
            DeliveryAddressLine3 = regAccount.DeliveryAddressLine3,
            DeliveryCity = regAccount.DeliveryCity,
            DeliveryCountry = regAccount.DeliveryCountry,
            DeliveryState = regAccount.DeliveryState,
            DeliveryZipCode = regAccount.DeliveryZipCode,
            DeploymentDate = regAccount.DeploymentDate,
            DeploymentStatus = string.IsNullOrEmpty(regAccount.DeploymentStatus) ? DeploymentStatusEnum.ToDeploy.ToString("d") : regAccount.DeploymentStatus,
            AccountDeliveryEmail = regAccount.AccountDeliveryEmail,
            AccountDeliveryFax = regAccount.AccountDeliveryFax,
            AccountBillingEmail = regAccount.AccountBillingEmail,
            AccountBillingFax = regAccount.AccountBillingFax,
            AccountCodeFormeJuridique = regAccount.AccountCodeFormeJuridique,
            AccountCommercialName = regAccount.AccountCommercialName,
            AccountEmail = regAccount.AccountEmail,
            AccountEscCategory = regAccount.AccountEscCategory,
            AccountFormeJuridique = regAccount.AccountFormeJuridique,
            AccountInsertedDate = regAccount.AccountInsertedDate,
            AccountISIN = regAccount.AccountIsin,
            AccountNafIdentifier = regAccount.AccountNafIdentifier,
            AccountRegimeFiscal = regAccount.AccountRegimeFiscal,
            AccountRegisterIdentification1 = regAccount.AccountRegisterIdentification1,
            AccountSectorCode = regAccount.AccountSectorCode,
            AccountSourceName = regAccount.AccountSourceName,
            AccountStaffSize = regAccount.AccountStaffSize,
            AccountStaffSizeSlice = regAccount.AccountStaffSizeSlice,
            AccountTaxationSystem = regAccount.AccountTaxationSystem,
            AccountTaxeValeurAjoutee = regAccount.AccountTaxeValeurAjoutee,
            Turnover = regAccount.AccountTurnover,
            AccountType = regAccount.AccountType,
            AccountTypeTenueComptable = regAccount.AccountTypeTenueComptable,
            AccountUpdatedDate = regAccount.AccountUpdatedDate,
            BillingAddressLine1 = regAccount.BillingAddressLine1,
            BillingAddressLine2 = regAccount.BillingAddressLine2,
            BillingAddressLine3 = regAccount.BillingAddressLine3,
            BillingCity = regAccount.BillingCity,
            BillingCountry = regAccount.BillingCountry,
            BillingState = regAccount.BillingState,
            BillingZipCode = regAccount.BillingZipCode,
            CreatedBy = string.IsNullOrEmpty(regAccount.CreatedBy) ? GlobalConstants.CREATEDBYREGISTRY : regAccount.CreatedBy,
            ModifiedBy = regAccount.ModifiedBy,
            BillingPhone = regAccount.BillingPhone,
            DeliveryPhone = regAccount.DeliveryPhone,
        };
    }
}
