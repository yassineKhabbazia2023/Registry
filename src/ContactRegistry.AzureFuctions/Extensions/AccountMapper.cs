// <copyright file="AccountMapper.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Domain.Constants;
using Domain.Entities;
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
            DeploymentDate = creAccount.DeploymentDate == null ? DateTime.UtcNow : creAccount.DeploymentDate,
            DeploymentStatus = string.IsNullOrEmpty(creAccount.DeploymentStatus) ? DeploymentStatusEnum.ToDeploy.ToString("d") : creAccount.DeploymentStatus,
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
            CreatedBy = string.IsNullOrEmpty(creAccount.CreatedBy) ? GlobalConstants.CREATEDBY : creAccount.CreatedBy,
            ModifiedBy = creAccount.ModifiedBy,
            BillingPhone = creAccount.BillingPhone,
            DeliveryPhone = creAccount.DeliveryPhone,
        };
    }

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
            DeploymentDate = regAccount.DeploymentDate == null ? DateTime.UtcNow : regAccount.DeploymentDate,
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
            CreatedBy = string.IsNullOrEmpty(regAccount.CreatedBy) ? GlobalConstants.CREATEDBY : regAccount.CreatedBy,
            ModifiedBy = regAccount.ModifiedBy,
            BillingPhone = regAccount.BillingPhone,
            DeliveryPhone = regAccount.DeliveryPhone,
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
            DeploymentDate = creAccount.DeploymentDate == null ? DateTime.UtcNow : creAccount.DeploymentDate,
            DeploymentStatus = string.IsNullOrEmpty(creAccount.DeploymentStatus) ? DeploymentStatusEnum.ToDeploy.ToString("d") : creAccount.DeploymentStatus,
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
            CreatedBy = string.IsNullOrEmpty(creAccount.CreatedBy) ? GlobalConstants.CREATEDBY : creAccount.CreatedBy,
            ModifiedBy = creAccount.ModifiedBy,
            BillingPhone = creAccount.BillingPhone,
            DeliveryPhone = creAccount.DeliveryPhone,
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
            DeploymentDate = regAccount.DeploymentDate == null ? DateTime.UtcNow : regAccount.DeploymentDate,
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
            CreatedBy = string.IsNullOrEmpty(regAccount.CreatedBy) ? GlobalConstants.CREATEDBY : regAccount.CreatedBy,
            ModifiedBy = regAccount.ModifiedBy,
            BillingPhone = regAccount.BillingPhone,
            DeliveryPhone = regAccount.DeliveryPhone,
        };
    }
}
