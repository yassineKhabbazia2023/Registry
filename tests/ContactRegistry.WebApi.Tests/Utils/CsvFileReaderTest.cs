using Application.Helpers;
using Application.Models;
using CsvHelper;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using System.Text;

namespace ContactRegistry.WebApi.Tests.Utils
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
            var records =  CsvFileReader.ReadStreamAsync<RoleCsv>(stream).ToList();

            // Assert
            records.Should().NotBeNull();
            records.Should().HaveCount(1);
            records.Should().ContainEquivalentOf(role);
        }
    }
}
