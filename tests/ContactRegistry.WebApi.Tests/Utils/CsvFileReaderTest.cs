using Application.Models;
using Application.Utils;
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
            IFormFile file = new FormFile(stream, 0, stream.Length, "id_from_form", "roles.csv");

            // Act
            var records = await CsvFileReader.ReadCsvAsync<RoleCsv>(file);

            // Assert
            records.Should().NotBeNull();
            records.Should().HaveCount(1);
            records.Should().ContainEquivalentOf(role);
        }

        [Fact]
        public async Task ReadCsvAsync_InvalidFile_ThrowsException()
        {
            // Arrange
            var csvContent = new StringBuilder();
            csvContent.AppendLine("RoleId;ContactId;AccountId;Onboarded;IsFavorite;RoleDelegataireEmail;RoleSignatory");
            csvContent.AppendLine("invalid_guid;invalid_guid;invalid_guid;invalid_bool;invalid_bool;invalid_string;invalid_bool");

            var stream = new MemoryStream(Encoding.GetEncoding("ISO-8859-1").GetBytes(csvContent.ToString()));
            IFormFile file = new FormFile(stream, 0, stream.Length, "id_from_form", "invalid_roles.csv");

            // Act
            Func<Task> act = async () => await CsvFileReader.ReadCsvAsync<RoleCsv>(file);

            // Assert
            await act.Should().ThrowAsync<CsvHelperException>();
        }
    }
}
