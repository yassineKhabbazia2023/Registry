// <copyright file="RefAccountLegacyCsvTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using System.Text;
using Application.Helpers;
using Application.Models;
using CsvHelper;
using FluentAssertions;

namespace Registry.WebApi.Tests.Utils;

/// <summary>
/// Verifies backward compatibility at the actual account CSV parsing boundary.
/// </summary>
public class RefAccountLegacyCsvTests
{
    private const string HistoricalHeaders = "AccountFlagStatus;LegalName;AccountNumber;AccountCommercialName;AccountType;AccountEmail;AccountNafIdentifier;AccountSectorCode;AccountTaxeValeurAjoutee;AccountDeliveryEmail;AccountBillingEmail;AccountTaxationSystem;AccountSourceName;AccountISIN;AccountRegisterIdentification1;AccountStaffSize;AccountDeliveryFax;AccountBillingFax;AccountTurnover;AccountRegimeFiscal;AccountTypeTenueComptable;AccountFormeJuridique;AccountStaffSizeSlice;AccountEscCategory;AccountCodeFormeJuridique;AccountInsertedDate;AccountUpdatedDate;DeliveryAddressLine1;DeliveryAddressLine2;DeliveryAddressLine3;DeliveryCity;DeliveryZipCode;DeliveryCountry;DeliveryState;BillingAddressLine1;BillingAddressLine2;BillingAddressLine3;BillingCity;BillingZipCode;BillingCountry;BillingState;AccountDeliveryPhone;AccountBillingPhone;Operation";

    #region Header compatibility

    /// <summary>
    /// Reads historical and enriched files with either, both or neither legacy header.
    /// </summary>
    [Theory]
    [InlineData(true, true, false, "Legacy")]
    [InlineData(true, true, true, "Legacy")]
    [InlineData(false, true, true, "Legacy")]
    [InlineData(true, false, true, "Legacy")]
    [InlineData(false, false, true, "Legacy")]
    [InlineData(false, false, false, "Legacy")]
    [InlineData(true, true, true, "")]
    public void ReadStreamAsync_WithLegacyHeaderVariants_ShouldPreserveColumnAlignment(
        bool includeLabel, bool includeCode, bool enriched, string legacyValue)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(CreateCsv(includeLabel, includeCode, enriched, legacyValue)));

        var records = CsvFileReader.ReadStreamAsync<RefAccountCsv>(stream).ToList();

        CsvConfig.IsValidCsvFormat(records, typeof(RefAccountCsv), out var error).Should().BeTrue();
        error.Should().BeEmpty();
        var record = records.Single().Item1;
        record.AccountFormeJuridique.Should().Be(includeLabel ? legacyValue : null);
        record.AccountCodeFormeJuridique.Should().Be(includeCode ? legacyValue : null);
        record.AccountStaffSizeSlice.Should().Be("10-50");
        record.AccountNumber.Should().Be("64931");
        record.Operation.Should().Be("UPDATE");
        record.AccountRoutingCode.Should().Be(enriched ? "0-B2B" : null);
        record.AccountRoutingLabel.Should().Be(enriched ? "B2B" : null);
        record.AccountLegalFormLabel.Should().Be(enriched ? "INSEE label" : null);
        record.AccountElectronicAddressId.Should().Be(enriched ? "Electronic address" : null);
    }

    /// <summary>
    /// Keeps required-header validation and row-width validation active.
    /// </summary>
    [Fact]
    public void ReadStreamAsync_WithMalformedCsv_ShouldKeepValidation()
    {
        var csv = CreateCsv(false, false, true, "Legacy");
        var missingHeader = csv.Replace("LegalName;", string.Empty).Replace("CSV company;", string.Empty);
        using var missingHeaderStream = new MemoryStream(Encoding.UTF8.GetBytes(missingHeader));
        Assert.Throws<HeaderValidationException>(() => CsvFileReader.ReadStreamAsync<RefAccountCsv>(missingHeaderStream).ToList());

        using var shortRowStream = new MemoryStream(Encoding.UTF8.GetBytes(csv.Replace(";UPDATE", string.Empty)));
        var records = CsvFileReader.ReadStreamAsync<RefAccountCsv>(shortRowStream).ToList();
        CsvConfig.IsValidCsvFormat(records, typeof(RefAccountCsv), out var error).Should().BeFalse();
        error.Should().Contain("Column count mismatch");
    }

    /// <summary>
    /// Builds explicit CSV headers and matching cells without deriving the contract from model reflection.
    /// </summary>
    private static string CreateCsv(bool includeLabel, bool includeCode, bool enriched, string legacyValue)
    {
        var headers = HistoricalHeaders.Split(';').Where(header =>
            (includeLabel || header != "AccountFormeJuridique") &&
            (includeCode || header != "AccountCodeFormeJuridique")).ToList();
        if (enriched)
        {
            headers.AddRange(["AccountRoutingCode", "AccountRoutingLabel", "AccountLegalFormLabel", "AccountElectronicAddressId"]);
        }
        var values = headers.Select(header => header switch
        {
            "AccountFlagStatus" => "1",
            "LegalName" => "CSV company",
            "AccountNumber" => "64931",
            "AccountType" => "CLIENT",
            "AccountFormeJuridique" or "AccountCodeFormeJuridique" => legacyValue,
            "AccountStaffSizeSlice" => "10-50",
            "Operation" => "UPDATE",
            "AccountRoutingCode" => "0-B2B",
            "AccountRoutingLabel" => "B2B",
            "AccountLegalFormLabel" => "INSEE label",
            "AccountElectronicAddressId" => "Electronic address",
            _ => string.Empty
        });
        return string.Join(';', headers) + Environment.NewLine + string.Join(';', values);
    }

    #endregion
}
