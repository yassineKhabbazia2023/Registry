// <copyright file="AccountControllerTest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models;
using ContactRegistry.WebApi.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;
using System.Text;
using WebApi.Configurations.Models;

namespace ContactRegistry.WebApi.Tests.Controllers;

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
        var options = new Mock<IOptions<TokenModel>>();

        // Act
        var accountController = new AccountController(accountService.Object, options.Object);
        var response = await accountController.UploadAsync(file) as ObjectResult;

        // Assert
        accountService.Verify(s => s.ProcessAccountAsync(It.IsAny<IEnumerable<AccountCsv>>()), Times.Never());
        response!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        response.Value.Should().Be("Invalid file.");
    }

    [Fact]
    public async Task UploadAsync_ValidFile_Retuns_Ok()
    {
        var account = new AccountCsv()
        {
            AccountGlobalUniqueIdentifier = Guid.NewGuid(),
            AccountNumber = "ABC12345",
            DeploymentStatus = "Success",
            LegalName = "Pulse Corporation",
            AccountCommercialName = "Pulse Corp",
            AccountType = "Corporation",
            AccountEmail = "info@pulse.com",
            AccountNafIdentifier = "NAF123456",
            AccountFlagEscActif = true,
            AccountSectorCode = "Sector123",
            AccountTaxeValeurAjoutee = "TVA123456",
            AccountDeliveryEmail = "delivery@pulse.com",
            AccountBillingEmail = "billing@pulse.com",
            AccountTaxationSystem = "Standard",
            AccountSourceName = "SourceName",
            AccountISIN = "ISIN123456",
            AccountRegisterIdentification1 = "RegID123456",
            AccountStaffSize = "100",
            AccountDeliveryFax = "123-456-7890",
            AccountBillingFax = "098-765-4321",
            AccountTurnover = "1M-10M",
            AccountRegimeFiscal = "RegimeFiscal",
            AccountTypeTenueComptable = "TypeTenueComptable",
            AccountFormeJuridique = "FormeJuridique",
            AccountStaffSizeSlice = "50-100",
            AccountEscCategory = "Category",
            AccountCodeFormeJuridique = "CodeFormeJuridique",
            AccountInsertedDate = DateTime.Now.ToString(),
            AccountUpdatedDate = DateTime.Now.ToString(),
            CreatedBy = "System",
            ModifiedBy = "System",
            DeliveryAddressLine1 = "123 Delivery St",
            DeliveryAddressLine2 = "Suite 100",
            DeliveryAddressLine3 = string.Empty,
            DeliveryCity = "Delivery City",
            DeliveryZipCode = "12345",
            DeliveryCountry = "Country",
            DeliveryState = "State",
            BillingAddressLine1 = "456 Billing Ave",
            BillingAddressLine2 = "Suite 200",
            BillingAddressLine3 = "",
            BillingCity = "Billing City",
            BillingZipCode = "67890",
            BillingCountry = "Country",
            BillingState = "State",
            DeploymentDate = DateTime.Now.ToString(),
            AccountBillingPhone = "phone",
            AccountDeliveryPhone = "deliveryPhone"
        };

        var csvContent = new StringBuilder();
        csvContent.AppendLine("AccountGlobalUniqueIdentifier;AccountNumber;DeploymentStatus;LegalName;AccountCommercialName;AccountType;AccountEmail;AccountNafIdentifier;AccountFlagEscActif;AccountSectorCode;AccountTaxeValeurAjoutee;AccountDeliveryEmail;AccountBillingEmail;AccountTaxationSystem;AccountSourceName;AccountISIN;AccountRegisterIdentification1;AccountStaffSize;AccountDeliveryFax;AccountBillingFax;AccountTurnover;AccountRegimeFiscal;AccountTypeTenueComptable;AccountFormeJuridique;AccountStaffSizeSlice;AccountEscCategory;AccountCodeFormeJuridique;AccountInsertedDate;AccountUpdatedDate;CreatedBy;ModifiedBy;DeliveryAddressLine1;DeliveryAddressLine2;DeliveryAddressLine3;DeliveryCity;DeliveryZipCode;DeliveryCountry;DeliveryState;BillingAddressLine1;BillingAddressLine2;BillingAddressLine3;BillingCity;BillingZipCode;BillingCountry;BillingState;DeploymentDate;AccountBillingPhone;AccountDeliveryPhone");
        csvContent.AppendLine(
            $"{account.AccountGlobalUniqueIdentifier};" +
            $"{account.AccountNumber};" +
            $"{account.DeploymentStatus};" +
            $"{account.LegalName};" +
            $"{account.AccountCommercialName};" +
            $"{account.AccountType};" +
            $"{account.AccountEmail};" +
            $"{account.AccountNafIdentifier};" +
            $"{account.AccountFlagEscActif};" +
            $"{account.AccountSectorCode};" +
            $"{account.AccountTaxeValeurAjoutee};" +
            $"{account.AccountDeliveryEmail};" +
            $"{account.AccountBillingEmail};" +
            $"{account.AccountTaxationSystem};" +
            $"{account.AccountSourceName};" +
            $"{account.AccountISIN};" +
            $"{account.AccountRegisterIdentification1};" +
            $"{account.AccountStaffSize};" +
            $"{account.AccountDeliveryFax};" +
            $"{account.AccountBillingFax};" +
            $"{account.AccountTurnover};" +
            $"{account.AccountRegimeFiscal};" +
            $"{account.AccountTypeTenueComptable};" +
            $"{account.AccountFormeJuridique};" +
            $"{account.AccountStaffSizeSlice};" +
            $"{account.AccountEscCategory};" +
            $"{account.AccountCodeFormeJuridique};" +
            $"{account.AccountInsertedDate.ToString()};" +
            $"{account.AccountUpdatedDate.ToString()};" +
            $"{account.CreatedBy};" +
            $"{account.ModifiedBy};" +
            $"{account.DeliveryAddressLine1};" +
            $"{account.DeliveryAddressLine2};" +
            $"{account.DeliveryAddressLine3};" +
            $"{account.DeliveryCity};" +
            $"{account.DeliveryZipCode};" +
            $"{account.DeliveryCountry};" +
            $"{account.DeliveryState};" +
            $"{account.BillingAddressLine1};" +
            $"{account.BillingAddressLine2};" +
            $"{account.BillingAddressLine3};" +
            $"{account.BillingCity};" +
            $"{account.BillingZipCode};" +
            $"{account.BillingCountry};" +
            $"{account.BillingState};" +
            $"{account.DeploymentDate.ToString()};"+
            $"{account.AccountBillingPhone};"+
            $"{account.AccountDeliveryPhone}"
        );

        var stream = new MemoryStream(Encoding.GetEncoding("ISO-8859-1").GetBytes(csvContent.ToString()));
        IFormFile file = new FormFile(stream, 0, stream.Length, "id_from_form", "account.csv");

        var options = new Mock<IOptions<TokenModel>>();
        var accountService = new Mock<IAccountService>(MockBehavior.Strict);
        accountService.Setup(s => s.ProcessAccountAsync(It.IsAny<IEnumerable<AccountCsv>>()))
            .Callback<IEnumerable<AccountCsv>>(data =>
            {
                var firstData = data.First();
                firstData.Should().NotBeNull();
                firstData.Should().BeEquivalentTo(account);
            })
            .Returns(Task.CompletedTask);

        // Act
        var accounttController = new AccountController(accountService.Object, options.Object);
        var response = await accounttController.UploadAsync(file) as ObjectResult;

        // Assert
        accountService.Verify(s => s.ProcessAccountAsync(It.IsAny<IEnumerable<AccountCsv>>()), Times.Once());
        response!.StatusCode.Should().Be((int)HttpStatusCode.OK);
        response.Value.Should().Be("File processed successfully.");
    }

    [Fact]
    public async Task ExportDataAsync_Returns_ExpectedFileResult()
    {
        // Arrange
        var expectedData = "{'Name': 'John', 'Age': 30}"; // Example JSON data
        var expectedFileName = "CreAccounts.json";

        var options = new Mock<IOptions<TokenModel>>();
        var accountServiceMock = new Mock<IAccountService>();
        accountServiceMock.Setup(x => x.StreamAccountsJsonAsync(It.IsAny<StreamWriter>()))
                            .Callback<StreamWriter>(async writer => await writer.WriteAsync(expectedData));

        var controller = new AccountController(accountServiceMock.Object, options.Object);

        // Act
        var result = await controller.ExportDataAsync();

        // Assert
        result.Should().BeOfType<FileStreamResult>();
        var fileContentResult = result as FileStreamResult;
        accountServiceMock.Verify(x => x.StreamAccountsJsonAsync(It.IsAny<StreamWriter>()), Times.Once);
        fileContentResult!.FileDownloadName.Should().Be(expectedFileName);
    }

    [Fact]
    public async Task UpdateAsync_WithValidData_ShouldProcess()
    {
        var account = new AccountCsv()
        {
            AccountGlobalUniqueIdentifier = Guid.NewGuid(),
            AccountNumber = "ABC12345",
            DeploymentStatus = "Success",
            LegalName = "Pulse Corporation",
            AccountCommercialName = "Pulse Corp",
            AccountType = "Corporation",
            AccountEmail = "info@pulse.com",
            AccountNafIdentifier = "NAF123456",
            AccountFlagEscActif = true,
            AccountSectorCode = "Sector123",
            AccountTaxeValeurAjoutee = "TVA123456",
            AccountDeliveryEmail = "delivery@pulse.com",
            AccountBillingEmail = "billing@pulse.com",
            AccountTaxationSystem = "Standard",
            AccountSourceName = "SourceName",
            AccountISIN = "ISIN123456",
            AccountRegisterIdentification1 = "RegID123456",
            AccountStaffSize = "100",
            AccountDeliveryFax = "123-456-7890",
            AccountBillingFax = "098-765-4321",
            AccountTurnover = "1M-10M",
            AccountRegimeFiscal = "RegimeFiscal",
            AccountTypeTenueComptable = "TypeTenueComptable",
            AccountFormeJuridique = "FormeJuridique",
            AccountStaffSizeSlice = "50-100",
            AccountEscCategory = "Category",
            AccountCodeFormeJuridique = "CodeFormeJuridique",
            AccountInsertedDate = DateTime.Now.ToString(),
            AccountUpdatedDate = DateTime.Now.ToString(),
            CreatedBy = "System",
            ModifiedBy = "System",
            DeliveryAddressLine1 = "123 Delivery St",
            DeliveryAddressLine2 = "Suite 100",
            DeliveryAddressLine3 = string.Empty,
            DeliveryCity = "Delivery City",
            DeliveryZipCode = "12345",
            DeliveryCountry = "Country",
            DeliveryState = "State",
            BillingAddressLine1 = "456 Billing Ave",
            BillingAddressLine2 = "Suite 200",
            BillingAddressLine3 = "",
            BillingCity = "Billing City",
            BillingZipCode = "67890",
            BillingCountry = "Country",
            BillingState = "State",
            DeploymentDate = DateTime.Now.ToString(),
            AccountBillingPhone = "phone",
            AccountDeliveryPhone = "deliveryPhone"
        };

        var csvContent = new StringBuilder();
        csvContent.AppendLine("AccountGlobalUniqueIdentifier;AccountNumber;DeploymentStatus;LegalName;AccountCommercialName;AccountType;AccountEmail;AccountNafIdentifier;AccountFlagEscActif;AccountSectorCode;AccountTaxeValeurAjoutee;AccountDeliveryEmail;AccountBillingEmail;AccountTaxationSystem;AccountSourceName;AccountISIN;AccountRegisterIdentification1;AccountStaffSize;AccountDeliveryFax;AccountBillingFax;AccountTurnover;AccountRegimeFiscal;AccountTypeTenueComptable;AccountFormeJuridique;AccountStaffSizeSlice;AccountEscCategory;AccountCodeFormeJuridique;AccountInsertedDate;AccountUpdatedDate;CreatedBy;ModifiedBy;DeliveryAddressLine1;DeliveryAddressLine2;DeliveryAddressLine3;DeliveryCity;DeliveryZipCode;DeliveryCountry;DeliveryState;BillingAddressLine1;BillingAddressLine2;BillingAddressLine3;BillingCity;BillingZipCode;BillingCountry;BillingState;DeploymentDate;AccountBillingPhone;AccountDeliveryPhone");
        csvContent.AppendLine(
            $"{account.AccountGlobalUniqueIdentifier};" +
            $"{account.AccountNumber};" +
            $"{account.DeploymentStatus};" +
            $"{account.LegalName};" +
            $"{account.AccountCommercialName};" +
            $"{account.AccountType};" +
            $"{account.AccountEmail};" +
            $"{account.AccountNafIdentifier};" +
            $"{account.AccountFlagEscActif};" +
            $"{account.AccountSectorCode};" +
            $"{account.AccountTaxeValeurAjoutee};" +
            $"{account.AccountDeliveryEmail};" +
            $"{account.AccountBillingEmail};" +
            $"{account.AccountTaxationSystem};" +
            $"{account.AccountSourceName};" +
            $"{account.AccountISIN};" +
            $"{account.AccountRegisterIdentification1};" +
            $"{account.AccountStaffSize};" +
            $"{account.AccountDeliveryFax};" +
            $"{account.AccountBillingFax};" +
            $"{account.AccountTurnover};" +
            $"{account.AccountRegimeFiscal};" +
            $"{account.AccountTypeTenueComptable};" +
            $"{account.AccountFormeJuridique};" +
            $"{account.AccountStaffSizeSlice};" +
            $"{account.AccountEscCategory};" +
            $"{account.AccountCodeFormeJuridique};" +
            $"{account.AccountInsertedDate.ToString()};" +
            $"{account.AccountUpdatedDate.ToString()};" +
            $"{account.CreatedBy};" +
            $"{account.ModifiedBy};" +
            $"{account.DeliveryAddressLine1};" +
            $"{account.DeliveryAddressLine2};" +
            $"{account.DeliveryAddressLine3};" +
            $"{account.DeliveryCity};" +
            $"{account.DeliveryZipCode};" +
            $"{account.DeliveryCountry};" +
            $"{account.DeliveryState};" +
            $"{account.BillingAddressLine1};" +
            $"{account.BillingAddressLine2};" +
            $"{account.BillingAddressLine3};" +
            $"{account.BillingCity};" +
            $"{account.BillingZipCode};" +
            $"{account.BillingCountry};" +
            $"{account.BillingState};" +
            $"{account.DeploymentDate.ToString()};" +
            $"{account.AccountBillingPhone};" +
            $"{account.AccountDeliveryPhone}"
        );

        var options = new Mock<IOptions<TokenModel>>();
        options.Setup(x => x.Value).Returns(new TokenModel { Token = "toto" });

        var accountService = new Mock<IAccountService>(MockBehavior.Strict);
        accountService.Setup(s => s.ProcessAccountAsync(It.IsAny<IEnumerable<AccountCsv>>()))
            .Callback<IEnumerable<AccountCsv>>(data =>
            {
                var firstData = data.First();
                firstData.Should().NotBeNull();
                firstData.Should().BeEquivalentTo(account);
            })
            .Returns(Task.CompletedTask);

        var controller = new AccountController(accountService.Object, options.Object);

        var response = await controller.UpdateAsync("toto", csvContent.ToString()) as OkObjectResult;

        accountService.VerifyAll();
        response.Should().NotBeNull();
        response!.StatusCode.Should().Be((int)HttpStatusCode.OK);
        response!.Value.Should().Be("Execution processed successfully.");
    }

    [Theory]
    [MemberData(nameof(TokenData))]
    public async Task UpdateAsync_WithWrongToken_ShouldReturnUnauthorizedResult(string token)
    {
        var options = new Mock<IOptions<TokenModel>>();
        options.Setup(x => x.Value).Returns(new TokenModel { Token = "toto" });

        var controller = new AccountController(null!, options.Object);

        var response = await controller.UpdateAsync(token, null!) as UnauthorizedObjectResult;

        response.Should().NotBeNull();
        response!.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
        response.Value.Should().Be("Invalid token.");
    }

    public static TheoryData<string> TokenData =>
        new()
        {
            null!,
            string.Empty,
            "                        ",
            "titi",
        };

    [Fact]
    public async Task UpdateAsync_WithInvalidData_ShouldReturnBadRequest()
    {
        var options = new Mock<IOptions<TokenModel>>();
        options.Setup(x => x.Value).Returns(new TokenModel { Token = "toto" });

        var controller = new AccountController(null!, options.Object);

        var response = await controller.UpdateAsync("toto", null!) as BadRequestObjectResult;

        response.Should().NotBeNull();
        response!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        response!.Value.Should().Be("Invalid data: message cannot be null or empty.");
    }
}
