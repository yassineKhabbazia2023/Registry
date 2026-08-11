// <copyright file="MapAccounts.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using Pulse.Registry.Domain.Entities;
using System.Globalization;

namespace Application.Mappers;

public static class MapAccounts
{
    public static IEnumerable<RefAccountEntity> MapAccountCsvsToAccountEntities(this IEnumerable<RefAccountCsv> source)
    {
        return source?.Select(s => s.MapAccountCsvToAccountEntity() !).ToList() ?? Enumerable.Empty<RefAccountEntity>();
    }

    public static RefAccountEntity? MapAccountCsvToAccountEntity(this RefAccountCsv source)
    {
        if (source == null)
        {
            return null!;
        }

        return new RefAccountEntity
        {
            EntityId = Guid.NewGuid(),
            AccountFlagStatus = source.AccountFlagStatus,
            LegalName = source.LegalName,
            AccountNumber = source.AccountNumber,
            AccountCommercialName = source.AccountCommercialName,
            AccountType = source.AccountType,
            AccountEmail = source.AccountEmail,
            AccountNafIdentifier = source.AccountNafIdentifier,
            AccountSectorCode = source.AccountSectorCode,
            AccountTaxeValeurAjoutee = source.AccountTaxeValeurAjoutee,
            AccountDeliveryEmail = source.AccountDeliveryEmail,
            AccountBillingEmail = source.AccountBillingEmail,
            AccountTaxationSystem = source.AccountTaxationSystem,
            AccountSourceName = source.AccountSourceName,
            AccountIsin = source.AccountISIN,
            AccountRegisterIdentification1 = source.AccountRegisterIdentification1,
            AccountStaffSize = source.AccountStaffSize,
            AccountDeliveryFax = source.AccountDeliveryFax,
            AccountBillingFax = source.AccountBillingFax,
            AccountTurnover = source.AccountTurnover,
            AccountRegimeFiscal = source.AccountRegimeFiscal,
            AccountTypeTenueComptable = source.AccountTypeTenueComptable,
            AccountFormeJuridique = source.AccountFormeJuridique,
            AccountStaffSizeSlice = source.AccountStaffSizeSlice,
            AccountEscCategory = source.AccountEscCategory,
            AccountCodeFormeJuridique = source.AccountCodeFormeJuridique,
            AccountRoutingCode = source.AccountRoutingCode,
            AccountRoutingLabel = source.AccountRoutingLabel,
            AccountLegalFormLabel = source.AccountLegalFormLabel,
            AccountElectronicAddressId = source.AccountElectronicAddressId,
            AccountInsertedDate = !string.IsNullOrWhiteSpace(source.AccountInsertedDate) ? DateTime.Parse(source.AccountInsertedDate, CultureInfo.InvariantCulture) : null,
            AccountUpdatedDate = !string.IsNullOrWhiteSpace(source.AccountUpdatedDate) ? DateTime.Parse(source.AccountUpdatedDate, CultureInfo.InvariantCulture) : null,
            DeliveryAddressLine1 = source.DeliveryAddressLine1,
            DeliveryAddressLine2 = source.DeliveryAddressLine2,
            DeliveryAddressLine3 = source.DeliveryAddressLine3,
            DeliveryCity = source.DeliveryCity,
            DeliveryZipCode = source.DeliveryZipCode,
            DeliveryCountry = source.DeliveryCountry,
            DeliveryState = source.DeliveryState,
            BillingAddressLine1 = source.BillingAddressLine1,
            BillingAddressLine2 = source.BillingAddressLine2,
            BillingAddressLine3 = source.BillingAddressLine3,
            BillingCity = source.BillingCity,
            BillingZipCode = source.BillingZipCode,
            BillingCountry = source.BillingCountry,
            BillingState = source.BillingState,
            AccountDeliveryPhone = source.AccountDeliveryPhone,
            AccountBillingPhone = source.AccountBillingPhone,
            OperationType = source.Operation,
            OperationDate = DateTime.UtcNow,
        };
    }
}
