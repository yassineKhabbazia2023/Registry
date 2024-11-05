// <copyright file="MapAccountsTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using AutoFixture;
using Infrastructure.Mappers;

namespace ContactRegistry.Infrastructure.Tests.Mappers;

public class MapAccountsTests
{
    private readonly Fixture _fixture;

    public MapAccountsTests()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public void MapAccountCsvToAccountEntity_ShouldMapsCorrectly()
    {
        var account = _fixture.Build<RefAccountCsv>()
            .With(r => r.AccountInsertedDate, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"))
            .With(r => r.AccountUpdatedDate, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"))
            .Create();

        var result = account.MapAccountCsvToAccountEntity();

        Assert.NotNull(result);
        Assert.Equal(account.AccountFlagStatus, result.AccountFlagStatus);
        Assert.Equal(account.LegalName, result.LegalName);
        Assert.Equal(account.AccountNumber, result.AccountNumber);
        Assert.Equal(account.AccountCommercialName, result.AccountCommercialName);
        Assert.Equal(account.AccountType, result.AccountType);
        Assert.Equal(account.AccountEmail, result.AccountEmail);
        Assert.Equal(account.AccountNafIdentifier, result.AccountNafIdentifier);
        Assert.Equal(account.AccountSectorCode, result.AccountSectorCode);
        Assert.Equal(account.AccountTaxeValeurAjoutee, result.AccountTaxeValeurAjoutee);
        Assert.Equal(account.AccountDeliveryEmail, result.AccountDeliveryEmail);
        Assert.Equal(account.AccountBillingEmail, result.AccountBillingEmail);
        Assert.Equal(account.AccountTaxationSystem, result.AccountTaxationSystem);
        Assert.Equal(account.AccountSourceName, result.AccountSourceName);
        Assert.Equal(account.AccountISIN, result.AccountIsin);
        Assert.Equal(account.AccountRegisterIdentification1, result.AccountRegisterIdentification1);
        Assert.Equal(account.AccountStaffSize, result.AccountStaffSize);
        Assert.Equal(account.AccountDeliveryFax, result.AccountDeliveryFax);
        Assert.Equal(account.AccountBillingFax, result.AccountBillingFax);
        Assert.Equal(account.AccountTurnover, result.AccountTurnover);
        Assert.Equal(account.AccountRegimeFiscal, result.AccountRegimeFiscal);
        Assert.Equal(account.AccountTypeTenueComptable, result.AccountTypeTenueComptable);
        Assert.Equal(account.AccountFormeJuridique, result.AccountFormeJuridique);
        Assert.Equal(account.AccountStaffSizeSlice, result.AccountStaffSizeSlice);
        Assert.Equal(account.AccountEscCategory, result.AccountEscCategory);
        Assert.Equal(account.AccountCodeFormeJuridique, result.AccountCodeFormeJuridique);
        Assert.Equal(DateTime.Parse(account.AccountInsertedDate!), result.AccountInsertedDate);
        Assert.Equal(DateTime.Parse(account.AccountUpdatedDate!), result.AccountUpdatedDate);
        Assert.Equal(account.DeliveryAddressLine1, result.DeliveryAddressLine1);
        Assert.Equal(account.DeliveryAddressLine2, result.DeliveryAddressLine2);
        Assert.Equal(account.DeliveryAddressLine3, result.DeliveryAddressLine3);
        Assert.Equal(account.DeliveryCity, result.DeliveryCity);
        Assert.Equal(account.DeliveryZipCode, result.DeliveryZipCode);
        Assert.Equal(account.DeliveryCountry, result.DeliveryCountry);
        Assert.Equal(account.DeliveryState, result.DeliveryState);
        Assert.Equal(account.BillingAddressLine1, result.BillingAddressLine1);
        Assert.Equal(account.BillingAddressLine2, result.BillingAddressLine2);
        Assert.Equal(account.BillingAddressLine3, result.BillingAddressLine3);
        Assert.Equal(account.BillingCity, result.BillingCity);
        Assert.Equal(account.BillingZipCode, result.BillingZipCode);
        Assert.Equal(account.BillingCountry, result.BillingCountry);
        Assert.Equal(account.BillingState, result.BillingState);
        Assert.Equal(account.Operation, result.OperationType);
    }

    [Fact]
    public void MapAccountCsvToAccountEntity_WithNullSource_ShouldReturnNull()
    {
        var result = MapAccounts.MapAccountCsvToAccountEntity(null!);

        Assert.Null(result);
    }

    [Fact]
    public void MapAccountCsvsToAccountEntities_ShouldMapsCorrectly()
    {
        var accounts = _fixture.Build<RefAccountCsv>()
            .With(r => r.AccountInsertedDate, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"))
            .With(r => r.AccountUpdatedDate, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"))
            .CreateMany(2);

        var result = accounts.MapAccountCsvsToAccountEntities();

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Equal(accounts.Count(), result.Count());
    }

    [Fact]
    public void MapAccountCsvsToAccountEntities_WithNullSource_ShouldReturnEmptyList()
    {
        var result = MapAccounts.MapAccountCsvsToAccountEntities(null!);

        Assert.Empty(result);
    }
}
