using Application.Interfaces;
using Application.Models;
using ContactRegistry.WebApi.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Net;
using System.Text;

namespace ContactRegistry.WebApi.Tests.Controllers
{
    public class AccountControllerTest
    {
        [Fact]
        public async Task UploadAsync_InvalidFile_Retuns_BadRequest()
        {
            // Arrnge
            var fileName = "data.csv";
            var stream = new MemoryStream(new byte[0]);

            IFormFile file = new FormFile(stream, 0, stream.Length, "id_from_form", fileName);

            var accountService = new Mock<IAccountService>();

            // Act
            var accountController = new AccountController(accountService.Object);
            var response = await accountController.UploadAsync(file) as ObjectResult;

            // Assert
            accountService.Verify(s => s.ProcessAccountAsync(It.IsAny<IEnumerable<AccountCsv>>()), Times.Never());
            response!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UploadAsync_ValidFile_Retuns_Ok()
        {
            var account = new AccountCsv(
                Id: Guid.NewGuid(),
                AccountNumber: "123456789",
                LegalName: "Example Corp",
                AccountFlagESCActif: true
            );

            var csvContent = new StringBuilder();
            csvContent.AppendLine("Id;AccountNumber;LegalName;AccountFlagESCActif");
            csvContent.AppendLine($"" +
                $"{account.Id};" +
                $"{account.AccountNumber};" +
                $"{account.LegalName};" +
                $"{account.AccountFlagESCActif}");

            var stream = new MemoryStream(Encoding.GetEncoding("ISO-8859-1").GetBytes(csvContent.ToString()));
            IFormFile file = new FormFile(stream, 0, stream.Length, "id_from_form", "account.csv");


            var accountService = new Mock<IAccountService>(MockBehavior.Strict);
            accountService.Setup(s => s.ProcessAccountAsync(It.IsAny<IEnumerable<AccountCsv>>()))
                .Callback<IEnumerable<AccountCsv>>(data =>
                {
                    data.First().Should().NotBeNull();
                    data.First().Should().Be(account);
                })
                .Returns(Task.CompletedTask);

            // Act
            var accounttController = new AccountController(accountService.Object);
            var response = await accounttController.UploadAsync(file) as ObjectResult;

            // Assert
            accountService.Verify(s => s.ProcessAccountAsync(It.IsAny<IEnumerable<AccountCsv>>()), Times.Once());
            response!.StatusCode.Should().Be((int)HttpStatusCode.OK);
        }

        [Fact]
        public async Task ExportDataAsync_Returns_ExpectedFileResult()
        {
            // Arrange
            var expectedData = "{'Name': 'John', 'Age': 30}"; // Example JSON data
            var expectedFileName = "CreAccounts.json";

            var accountServiceMock = new Mock<IAccountService>();
            accountServiceMock.Setup(x => x.StreamAccountsJsonAsync(It.IsAny<StreamWriter>()))
                              .Callback<StreamWriter>(async writer => await writer.WriteAsync(expectedData));

            var controller = new AccountController(accountServiceMock.Object);

            // Act
            var result = await controller.ExportDataAsync();

            // Assert
            result.Should().BeOfType<FileStreamResult>();
            var fileContentResult = result as FileStreamResult;
            accountServiceMock.Verify(x => x.StreamAccountsJsonAsync(It.IsAny<StreamWriter>()), Times.Once);
            fileContentResult!.FileDownloadName.Should().Be(expectedFileName);
        }
    }
}
