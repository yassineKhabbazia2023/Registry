// <copyright file="CsvFileReaderTest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Helpers;
using Application.Models;
using CsvHelper;
using FluentAssertions;
using System.Text;

namespace Registry.WebApi.Tests.Utils
{
    public class CsvFileReaderTest
    {
        [Fact]
        public async Task ReadCsvAsync_ValidFile_ReturnsRecords()
        {
            // Arrange
            var role = new RoleCsv()
            {
                RoleId = Guid.NewGuid(),
                ContactId = Guid.NewGuid(),
                AccountId = Guid.NewGuid(),
                Onboarded = true,
                IsFavorite = true,
                RoleDelegataireEmail = "delegataire@email.fr",
                RoleSignatory = true
            };

            var csvContent = new StringBuilder();
            csvContent.AppendLine("RoleId;ContactId;AccountId;Onboarded;IsFavorite;RoleDelegataireEmail;RoleSignatory");
            csvContent.AppendLine($"" +
                $"{role.RoleId};" +
                $"{role.ContactId};" +
                $"{role.AccountId};" +
                $"{role.Onboarded};" +
                $"{role.IsFavorite};" +
                $"{role.RoleDelegataireEmail};" +
                $"{role.RoleSignatory}");

            var stream = new MemoryStream(Encoding.GetEncoding("ISO-8859-1").GetBytes(csvContent.ToString()));

            // Act
            var records = CsvFileReader.ReadStreamAsync<RoleCsv>(stream).ToList();

            // Assert
            records.Should().NotBeNull();
            records.Should().HaveCount(1);
            records.Select(d => d.Item1).Should().ContainEquivalentOf(role);
        }

        [Fact]
        public async Task ReadCsvAsync_ValidFileWithLIneFeeds_ReturnsRecords()
        {
            // Arrange
            var role = new RoleCsv()
            {
                RoleId = Guid.NewGuid(),
                ContactId = Guid.NewGuid(),
                AccountId = Guid.NewGuid(),
                Onboarded = true,
                IsFavorite = true,
                RoleDelegataireEmail = "delegataire@email.fr\r\nwithLineFeed",
                RoleSignatory = true
            };

            var csvContent = new StringBuilder();
            csvContent.AppendLine("RoleId;ContactId;AccountId;Onboarded;IsFavorite;RoleDelegataireEmail;RoleSignatory");
            csvContent.AppendLine($"" +
                $"{role.RoleId};" +
                $"{role.ContactId};" +
                $"{role.AccountId};" +
                $"{role.Onboarded};" +
                $"{role.IsFavorite};" +
                $"\"{role.RoleDelegataireEmail}\";" +
                $"{role.RoleSignatory}");

            var stream = new MemoryStream(Encoding.GetEncoding("ISO-8859-1").GetBytes(csvContent.ToString()));

            // Act
            var records = CsvFileReader.ReadStreamAsync<RoleCsv>(stream).ToList();

            // Assert
            records.Should().NotBeNull();
            records.Should().HaveCount(1);
            records.Select(d => d.Item1).Should().ContainEquivalentOf(role);
        }

        [Fact]
        public async Task ReadCsvAsync_ValidAccountFileWithLIneFeeds_ReturnsRecords()
        {
            // Arrange
            var csvContent = new StringBuilder();
            csvContent.AppendLine("AccountFlagStatus;LegalName;AccountNumber;AccountCommercialName;AccountType;AccountEmail;AccountNafIdentifier;AccountSectorCode;AccountTaxeValeurAjoutee;AccountDeliveryEmail;AccountBillingEmail;AccountTaxationSystem;AccountSourceName;AccountISIN;AccountRegisterIdentification1;AccountStaffSize;AccountDeliveryFax;AccountBillingFax;AccountTurnover;AccountRegimeFiscal;AccountTypeTenueComptable;AccountFormeJuridique;AccountStaffSizeSlice;AccountEscCategory;AccountCodeFormeJuridique;AccountInsertedDate;AccountUpdatedDate;DeliveryAddressLine1;DeliveryAddressLine2;DeliveryAddressLine3;DeliveryCity;DeliveryZipCode;DeliveryCountry;DeliveryState;BillingAddressLine1;BillingAddressLine2;BillingAddressLine3;BillingCity;BillingZipCode;BillingCountry;BillingState;AccountDeliveryPhone;AccountBillingPhone;Operation\r\n");
            csvContent.AppendLine("1;SCI DU BONHEUR;1001071085;NULL;CLIENT;toto@orange.fr;6820B;;FR70917727851;toto@orange.fr;toto@orange.fr;BIC;Akuiteo;NULL;91772785100011;NULL;NULL;NULL;NULL;NULL;NULL;NULL;z- NonRenseigné;Personne morale;NULL;2025-01-30 13:59:14.317;2025-02-12 00:38:59.487;\"ZONE ARTISANALE  ");
            csvContent.AppendLine("3 RUE GEORGES NOEL\";NULL;NULL;ESTISSAC;10190;NULL;;\"ZONE ARTISANALE  ");
            csvContent.AppendLine("3 RUE GEORGES NOEL\";NULL;NULL;ESTISSAC;10190;NULL;NULL;NULL;NULL;UPDATE");

            var stream = new MemoryStream(Encoding.GetEncoding("ISO-8859-1").GetBytes(csvContent.ToString()));

            // Act
            var records = CsvFileReader.ReadStreamAsync<RefAccountCsv>(stream).ToList();

            // Assert
            records.Should().NotBeNull();
            records.Should().HaveCount(1);
        }

        /// <summary>
        /// Verifies that a partially enriched Account CSV remains valid when the other new optional headers are absent.
        /// </summary>
        [Fact]
        public void ReadCsvAsync_AccountFileWithPartialOptionalHeaders_ReturnsRecord()
        {
            // Arrange
            string[] optionalHeaders =
            [
                nameof(RefAccountCsv.AccountRoutingCode),
                nameof(RefAccountCsv.AccountRoutingLabel),
                nameof(RefAccountCsv.AccountLegalFormLabel),
                nameof(RefAccountCsv.AccountElectronicAddressId)
            ];
            var headers = typeof(RefAccountCsv).GetProperties()
                .Select(property => property.Name)
                .Where(name => !optionalHeaders.Contains(name) || name == nameof(RefAccountCsv.AccountRoutingCode))
                .ToArray();
            var values = headers.Select(header => header switch
            {
                nameof(RefAccountCsv.AccountFlagStatus) => "1",
                nameof(RefAccountCsv.LegalName) => "Pulse Corp",
                nameof(RefAccountCsv.AccountNumber) => "ACC001",
                nameof(RefAccountCsv.AccountType) => "CLIENT",
                nameof(RefAccountCsv.AccountRoutingCode) => "0-B2G",
                nameof(RefAccountCsv.Operation) => "INSERT",
                _ => string.Empty
            });
            var csvContent = string.Join(';', headers) + Environment.NewLine + string.Join(';', values);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

            // Act
            var record = CsvFileReader.ReadStreamAsync<RefAccountCsv>(stream).Single().Item1;

            // Assert
            record.AccountRoutingCode.Should().Be("0-B2G");
            record.AccountRoutingLabel.Should().BeNull();
            record.AccountLegalFormLabel.Should().BeNull();
            record.AccountElectronicAddressId.Should().BeNull();
        }
    }
}
