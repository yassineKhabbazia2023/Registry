// <copyright file="AccountMapperTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using AutoFixture;
using Domain.Entities;

namespace ContactRegistry.AzureFuctions.Tests.Extensions;

public class AccountMapperTests
{
    private readonly Fixture _fixture;

    public AccountMapperTests()
    {
        _fixture = new Fixture();
    }

    [Fact]
    public void ToRegistryAccountCreatedEventData_ShouldMapCreAccountToRegistryAccountCreatedEventData()
    {
        var account = _fixture.Create<CreAccount>();

        var result = account.ToRegistryAccountCreatedEventData();

        Assert.NotNull(result);
        Assert.Equal(account.Id, result.AccountGlobalUniqueIdentifier);
        Assert.Equal(account.AccountNumber, result.AccountNumber);
        Assert.Equal(account.AccountFlagEscActif, result.AccountFlagESCActif);
        Assert.Equal(account.DeliveryAddressLine1, result.DeliveryAddressLine1);
        Assert.Equal(account.DeliveryAddressLine2, result.DeliveryAddressLine2);
        Assert.Equal(account.DeliveryAddressLine3, result.DeliveryAddressLine3);
        Assert.Equal(account.DeliveryCity, result.DeliveryCity);
        Assert.Equal(account.DeliveryCountry, result.DeliveryCountry);
        Assert.Equal(account.DeliveryState, result.DeliveryState);
        Assert.Equal(account.DeliveryZipCode, result.DeliveryZipCode);
        Assert.Equal(account.DeploymentDate, result.DeploymentDate);
        Assert.Equal(account.DeploymentStatus, result.DeploymentStatus);
        Assert.Equal(account.AccountDeliveryEmail, result.AccountDeliveryEmail);
        Assert.Equal(account.AccountDeliveryFax, result.AccountDeliveryFax);
        Assert.Equal(account.AccountBillingEmail, result.AccountBillingEmail);
        Assert.Equal(account.AccountBillingFax, result.AccountBillingFax);
        Assert.Equal(account.AccountCodeFormeJuridique, result.AccountCodeFormeJuridique);
        Assert.Equal(account.AccountCommercialName, result.AccountCommercialName);
        Assert.Equal(account.AccountEmail, result.AccountEmail);
        Assert.Equal(account.AccountEscCategory, result.AccountEscCategory);
        Assert.Equal(account.AccountFormeJuridique, result.AccountFormeJuridique);
        Assert.Equal(account.AccountInsertedDate, result.AccountInsertedDate);
        Assert.Equal(account.AccountISIN, result.AccountISIN);
        Assert.Equal(account.AccountNafIdentifier, result.AccountNafIdentifier);
        Assert.Equal(account.AccountRegimeFiscal, result.AccountRegimeFiscal);
        Assert.Equal(account.AccountRegisterIdentification1, result.AccountRegisterIdentification1);
        Assert.Equal(account.AccountSectorCode, result.AccountSectorCode);
        Assert.Equal(account.AccountSourceName, result.AccountSourceName);
        Assert.Equal(account.AccountStaffSize, result.AccountStaffSize);
        Assert.Equal(account.AccountStaffSizeSlice, result.AccountStaffSizeSlice);
        Assert.Equal(account.AccountTaxationSystem, result.AccountTaxationSystem);
        Assert.Equal(account.AccountTaxeValeurAjoutee, result.AccountTaxeValeurAjoutee);
        Assert.Equal(account.AccountTurnover, result.Turnover);
        Assert.Equal(account.AccountType, result.AccountType);
        Assert.Equal(account.AccountTypeTenueComptable, result.AccountTypeTenueComptable);
        Assert.Equal(account.AccountUpdatedDate, result.AccountUpdatedDate);
        Assert.Equal(account.BillingAddressLine1, result.BillingAddressLine1);
        Assert.Equal(account.BillingAddressLine2, result.BillingAddressLine2);
        Assert.Equal(account.BillingAddressLine3, result.BillingAddressLine3);
        Assert.Equal(account.BillingCity, result.BillingCity);
        Assert.Equal(account.BillingCountry, result.BillingCountry);
        Assert.Equal(account.BillingState, result.BillingState);
        Assert.Equal(account.BillingZipCode, result.BillingZipCode);
        Assert.Equal(account.CreatedBy, result.CreatedBy);
        Assert.Equal(account.ModifiedBy, result.ModifiedBy);
        Assert.Equal(account.BillingPhone, result.BillingPhone);
        Assert.Equal(account.DeliveryPhone, result.DeliveryPhone);
    }

    [Fact]
    public void ToRegistryAccountCreatedEventData_WithNullSource_ShouldThrowArgumentNullException()
    {
        var result = Assert.Throws<ArgumentNullException>(() => AccountMapper.ToRegistryAccountCreatedEventData(null!));

        Assert.NotNull(result);
        Assert.IsType<ArgumentNullException>(result);
    }

    [Fact]
    public void ToRegistryAccountUpdatedEventData_ShouldMapCreAccountToRegistryAccountUpdatedEventData()
    {
        var account = _fixture.Create<CreAccount>();

        var result = account.ToRegistryAccountUpdatedEventData();

        Assert.NotNull(result);
        Assert.Equal(account.Id, result.AccountGlobalUniqueIdentifier);
        Assert.Equal(account.AccountNumber, result.AccountNumber);
        Assert.Equal(account.AccountFlagEscActif, result.AccountFlagESCActif);
        Assert.Equal(account.DeliveryAddressLine1, result.DeliveryAddressLine1);
        Assert.Equal(account.DeliveryAddressLine2, result.DeliveryAddressLine2);
        Assert.Equal(account.DeliveryAddressLine3, result.DeliveryAddressLine3);
        Assert.Equal(account.DeliveryCity, result.DeliveryCity);
        Assert.Equal(account.DeliveryCountry, result.DeliveryCountry);
        Assert.Equal(account.DeliveryState, result.DeliveryState);
        Assert.Equal(account.DeliveryZipCode, result.DeliveryZipCode);
        Assert.Equal(account.DeploymentDate, result.DeploymentDate);
        Assert.Equal(account.DeploymentStatus, result.DeploymentStatus);
        Assert.Equal(account.AccountDeliveryEmail, result.AccountDeliveryEmail);
        Assert.Equal(account.AccountDeliveryFax, result.AccountDeliveryFax);
        Assert.Equal(account.AccountBillingEmail, result.AccountBillingEmail);
        Assert.Equal(account.AccountBillingFax, result.AccountBillingFax);
        Assert.Equal(account.AccountCodeFormeJuridique, result.AccountCodeFormeJuridique);
        Assert.Equal(account.AccountCommercialName, result.AccountCommercialName);
        Assert.Equal(account.AccountEmail, result.AccountEmail);
        Assert.Equal(account.AccountEscCategory, result.AccountEscCategory);
        Assert.Equal(account.AccountFormeJuridique, result.AccountFormeJuridique);
        Assert.Equal(account.AccountInsertedDate, result.AccountInsertedDate);
        Assert.Equal(account.AccountISIN, result.AccountISIN);
        Assert.Equal(account.AccountNafIdentifier, result.AccountNafIdentifier);
        Assert.Equal(account.AccountRegimeFiscal, result.AccountRegimeFiscal);
        Assert.Equal(account.AccountRegisterIdentification1, result.AccountRegisterIdentification1);
        Assert.Equal(account.AccountSectorCode, result.AccountSectorCode);
        Assert.Equal(account.AccountSourceName, result.AccountSourceName);
        Assert.Equal(account.AccountStaffSize, result.AccountStaffSize);
        Assert.Equal(account.AccountStaffSizeSlice, result.AccountStaffSizeSlice);
        Assert.Equal(account.AccountTaxationSystem, result.AccountTaxationSystem);
        Assert.Equal(account.AccountTaxeValeurAjoutee, result.AccountTaxeValeurAjoutee);
        Assert.Equal(account.AccountTurnover, result.Turnover);
        Assert.Equal(account.AccountType, result.AccountType);
        Assert.Equal(account.AccountTypeTenueComptable, result.AccountTypeTenueComptable);
        Assert.Equal(account.AccountUpdatedDate, result.AccountUpdatedDate);
        Assert.Equal(account.BillingAddressLine1, result.BillingAddressLine1);
        Assert.Equal(account.BillingAddressLine2, result.BillingAddressLine2);
        Assert.Equal(account.BillingAddressLine3, result.BillingAddressLine3);
        Assert.Equal(account.BillingCity, result.BillingCity);
        Assert.Equal(account.BillingCountry, result.BillingCountry);
        Assert.Equal(account.BillingState, result.BillingState);
        Assert.Equal(account.BillingZipCode, result.BillingZipCode);
        Assert.Equal(account.CreatedBy, result.CreatedBy);
        Assert.Equal(account.ModifiedBy, result.ModifiedBy);
        Assert.Equal(account.BillingPhone, result.BillingPhone);
        Assert.Equal(account.DeliveryPhone, result.DeliveryPhone);
    }

    [Fact]
    public void ToRegistryAccountUpdatedEventData_WithNullSource_ShouldThrowAgrumentNullException()
    {
        var result = Assert.Throws<ArgumentNullException>(() => AccountMapper.ToRegistryAccountUpdatedEventData(null!));

        Assert.IsType<ArgumentNullException>(result);
    }
}
