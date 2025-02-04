// <copyright file="AccountControllerTest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Helpers;
using Application.Interfaces;
using Application.Models;
using Application.Services;
using Registry.WebApi.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using WebApi.Configurations.Models;
using Application.Exceptions;

namespace Registry.WebApi.Tests.Controllers;

public class AccountControllerTest
{
    private readonly Mock<ILogger<AccountService>> _logger;

    public AccountControllerTest()
    {
        _logger = new Mock<ILogger<AccountService>>();
    }

    [Fact]
    public async Task UpdateAsync_WithValidData_ShouldProcess()
    {
        // Arrange
        var account = new RefAccountCsv
        {
            AccountNumber = "ABC12345",
            LegalName = "Pulse Corporation",
            AccountCommercialName = "Pulse Corp",
            AccountType = "CLIENT",
            AccountEmail = "info@pulse.com",
            AccountNafIdentifier = "NAF123456",
            AccountFlagStatus = 1,
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
            AccountBillingPhone = "phone",
            AccountDeliveryPhone = "deliveryPhone",
            Operation = "INSERT"
        };

        var csvContent = new StringBuilder();
        csvContent.AppendLine("AccountNumber;LegalName;AccountCommercialName;AccountType;AccountEmail;AccountNafIdentifier;AccountFlagStatus;AccountSectorCode;AccountTaxeValeurAjoutee;AccountDeliveryEmail;AccountBillingEmail;AccountTaxationSystem;AccountSourceName;AccountISIN;AccountRegisterIdentification1;AccountStaffSize;AccountDeliveryFax;AccountBillingFax;AccountTurnover;AccountRegimeFiscal;AccountTypeTenueComptable;AccountFormeJuridique;AccountStaffSizeSlice;AccountEscCategory;AccountCodeFormeJuridique;AccountInsertedDate;AccountUpdatedDate;DeliveryAddressLine1;DeliveryAddressLine2;DeliveryAddressLine3;DeliveryCity;DeliveryZipCode;DeliveryCountry;DeliveryState;BillingAddressLine1;BillingAddressLine2;BillingAddressLine3;BillingCity;BillingZipCode;BillingCountry;BillingState;AccountBillingPhone;AccountDeliveryPhone;Operation");
        csvContent.AppendLine(
            $"{account.AccountNumber};" +
            $"{account.LegalName};" +
            $"{account.AccountCommercialName};" +
            $"{account.AccountType};" +
            $"{account.AccountEmail};" +
            $"{account.AccountNafIdentifier};" +
            $"{account.AccountFlagStatus};" +
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
            $"{account.AccountBillingPhone};" +
            $"{account.AccountDeliveryPhone};" +
            $"{account.Operation}"
        );

        var options = new Mock<IOptions<TokenModel>>();
        options.Setup(x => x.Value).Returns(new TokenModel { Token = "toto" });
        var blobStorageManagerMock = new Mock<IBlobStorageManager>(MockBehavior.Strict);
        blobStorageManagerMock.Setup(x => x.SaveFileAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((endpoint, fileContent) =>
            {
                Assert.Equal("Account", endpoint);
                Assert.Equal(csvContent.ToString(), fileContent);
            }).ReturnsAsync(true);

        var accountRepo = new Mock<IAccountRepository>();
        var logger = new Mock<ILogger<AccountService>>();
        var operationRepositoryMock = new Mock<IOperationRepository>(MockBehavior.Strict);
        var accountService = new AccountService(accountRepo.Object, operationRepositoryMock.Object, _logger.Object);

        var controller = new AccountController(accountService, options.Object, blobStorageManagerMock.Object);

        // Act
        var response = await controller.UpdateAsync("toto", csvContent.ToString()) as OkObjectResult;

        // Assert
        blobStorageManagerMock.VerifyAll();
        response.Should().NotBeNull();
        response!.StatusCode.Should().Be((int)HttpStatusCode.OK);
        response!.Value.Should().Be("Csv Accounts retreval process was completed");
    }

    [Theory]
    [MemberData(nameof(TokenData))]
    public async Task UpdateAsync_WithWrongToken_ShouldReturnUnauthorizedResult(string token)
    {
        var options = new Mock<IOptions<TokenModel>>();
        options.Setup(x => x.Value).Returns(new TokenModel { Token = "toto" });

        var blobStorageManagerMock = new Mock<IBlobStorageManager>(MockBehavior.Strict);
        blobStorageManagerMock.Setup(x => x.SaveFileAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

        var controller = new AccountController(null!, options.Object, blobStorageManagerMock.Object);

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
    public async Task UpdateAsync_ReturnOnlyErrors_ShouldReturnBadRequest()
    {
        // Arrange
        var account = new RefAccountCsv
        {
            AccountNumber = "ABC1@2345",
            LegalName = "Pulse Corporation",
            AccountCommercialName = "Pulse Corp",
            AccountType = "CLIENT",
            AccountEmail = "info@pulse.com",
            AccountNafIdentifier = "NAF123456",
            AccountFlagStatus = 1,
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
            AccountBillingPhone = "phone",
            AccountDeliveryPhone = "deliveryPhone",
            Operation = "INSERT"
        };

        var csvContent = new StringBuilder();
        csvContent.AppendLine("AccountNumber;LegalName;AccountCommercialName;AccountType;AccountEmail;AccountNafIdentifier;AccountFlagStatus;AccountSectorCode;AccountTaxeValeurAjoutee;AccountDeliveryEmail;AccountBillingEmail;AccountTaxationSystem;AccountSourceName;AccountISIN;AccountRegisterIdentification1;AccountStaffSize;AccountDeliveryFax;AccountBillingFax;AccountTurnover;AccountRegimeFiscal;AccountTypeTenueComptable;AccountFormeJuridique;AccountStaffSizeSlice;AccountEscCategory;AccountCodeFormeJuridique;AccountInsertedDate;AccountUpdatedDate;DeliveryAddressLine1;DeliveryAddressLine2;DeliveryAddressLine3;DeliveryCity;DeliveryZipCode;DeliveryCountry;DeliveryState;BillingAddressLine1;BillingAddressLine2;BillingAddressLine3;BillingCity;BillingZipCode;BillingCountry;BillingState;AccountBillingPhone;AccountDeliveryPhone;Operation");
        csvContent.AppendLine(
            $"{account.AccountNumber};" +
            $"{account.LegalName};" +
            $"{account.AccountCommercialName};" +
            $"{account.AccountType};" +
            $"{account.AccountEmail};" +
            $"{account.AccountNafIdentifier};" +
            $"{account.AccountFlagStatus};" +
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
            $"{account.AccountBillingPhone};" +
            $"{account.AccountDeliveryPhone};" +
            $"{account.Operation}"
        );

        var options = new Mock<IOptions<TokenModel>>();
        options.Setup(x => x.Value).Returns(new TokenModel { Token = "toto" });

        var accountRepo = new Mock<IAccountRepository>();
        var operationRepositoryMock = new Mock<IOperationRepository>();
        var logger = new Mock<ILogger<AccountService>>();
        var validationHelper = new ValidationHelper<RefAccountCsv>();
        var accountService = new AccountService(accountRepo.Object, operationRepositoryMock.Object, _logger.Object) ;

        var blobStorageManagerMock = new Mock<IBlobStorageManager>(MockBehavior.Strict);
        blobStorageManagerMock.Setup(x => x.SaveFileAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

        var controller = new AccountController(accountService, options.Object, blobStorageManagerMock.Object);
        var resultValidation = validationHelper.Validate(new List<RefAccountCsv> { account });

        // Act
        var response = await controller.UpdateAsync("toto", csvContent.ToString()) as BadRequestObjectResult;

        // Assert
        response.Should().NotBeNull();
        response!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        response!.Value.Should().Be($"Csv Accounts retreval process unsuccessuf with errors: {JsonConvert.SerializeObject(resultValidation.Errors)}");
    }

    [Fact]
    public async Task UpdateAsync_ReturnErrorsAndVaid_ShouldReturnBadRequest()
    {
        // Arrange
        var account = new RefAccountCsv
        {
            AccountNumber = "ABC1@2345",
            LegalName = "Pulse Corporation",
            AccountCommercialName = "Pulse Corp",
            AccountType = "CLIENT",
            AccountEmail = "info@pulse.com",
            AccountNafIdentifier = "NAF123456",
            AccountFlagStatus = 1,
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
            AccountBillingPhone = "phone",
            AccountDeliveryPhone = "deliveryPhone",
            Operation = "INSERT"
        };
        var account2 = new RefAccountCsv
        {
            AccountNumber = "ABC12345",
            LegalName = "Pulse Corporation",
            AccountCommercialName = "Pulse Corp",
            AccountType = "CLIENT",
            AccountEmail = "info@pulse.com",
            AccountNafIdentifier = "NAF123456",
            AccountFlagStatus = 1,
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
            AccountBillingPhone = "phone",
            AccountDeliveryPhone = "deliveryPhone",
            Operation = "INSERT"
        };

        var csvContent = new StringBuilder();
        csvContent.AppendLine("AccountNumber;LegalName;AccountCommercialName;AccountType;AccountEmail;AccountNafIdentifier;AccountFlagStatus;AccountSectorCode;AccountTaxeValeurAjoutee;AccountDeliveryEmail;AccountBillingEmail;AccountTaxationSystem;AccountSourceName;AccountISIN;AccountRegisterIdentification1;AccountStaffSize;AccountDeliveryFax;AccountBillingFax;AccountTurnover;AccountRegimeFiscal;AccountTypeTenueComptable;AccountFormeJuridique;AccountStaffSizeSlice;AccountEscCategory;AccountCodeFormeJuridique;AccountInsertedDate;AccountUpdatedDate;DeliveryAddressLine1;DeliveryAddressLine2;DeliveryAddressLine3;DeliveryCity;DeliveryZipCode;DeliveryCountry;DeliveryState;BillingAddressLine1;BillingAddressLine2;BillingAddressLine3;BillingCity;BillingZipCode;BillingCountry;BillingState;AccountBillingPhone;AccountDeliveryPhone;Operation");
        csvContent.AppendLine(
            $"{account.AccountNumber};" +
            $"{account.LegalName};" +
            $"{account.AccountCommercialName};" +
            $"{account.AccountType};" +
            $"{account.AccountEmail};" +
            $"{account.AccountNafIdentifier};" +
            $"{account.AccountFlagStatus};" +
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
            $"{account.AccountBillingPhone};" +
            $"{account.AccountDeliveryPhone};" +
            $"{account.Operation}"
        );
        csvContent.AppendLine(
            $"{account2.AccountNumber};" +
            $"{account2.LegalName};" +
            $"{account2.AccountCommercialName};" +
            $"{account2.AccountType};" +
            $"{account2.AccountEmail};" +
            $"{account2.AccountNafIdentifier};" +
            $"{account2.AccountFlagStatus};" +
            $"{account2.AccountSectorCode};" +
            $"{account2.AccountTaxeValeurAjoutee};" +
            $"{account2.AccountDeliveryEmail};" +
            $"{account2.AccountBillingEmail};" +
            $"{account2.AccountTaxationSystem};" +
            $"{account2.AccountSourceName};" +
            $"{account2.AccountISIN};" +
            $"{account2.AccountRegisterIdentification1};" +
            $"{account2.AccountStaffSize};" +
            $"{account2.AccountDeliveryFax};" +
            $"{account2.AccountBillingFax};" +
            $"{account2.AccountTurnover};" +
            $"{account2.AccountRegimeFiscal};" +
            $"{account2.AccountTypeTenueComptable};" +
            $"{account2.AccountFormeJuridique};" +
            $"{account2.AccountStaffSizeSlice};" +
            $"{account2.AccountEscCategory};" +
            $"{account2.AccountCodeFormeJuridique};" +
            $"{account2.AccountInsertedDate.ToString()};" +
            $"{account2.AccountUpdatedDate.ToString()};" +
            $"{account2.DeliveryAddressLine1};" +
            $"{account2.DeliveryAddressLine2};" +
            $"{account2.DeliveryAddressLine3};" +
            $"{account2.DeliveryCity};" +
            $"{account2.DeliveryZipCode};" +
            $"{account2.DeliveryCountry};" +
            $"{account2.DeliveryState};" +
            $"{account2.BillingAddressLine1};" +
            $"{account2.BillingAddressLine2};" +
            $"{account2.BillingAddressLine3};" +
            $"{account2.BillingCity};" +
            $"{account2.BillingZipCode};" +
            $"{account2.BillingCountry};" +
            $"{account2.BillingState};" +
            $"{account2.AccountBillingPhone};" +
            $"{account2.AccountDeliveryPhone};" +
            $"{account2.Operation}"
        );

        var options = new Mock<IOptions<TokenModel>>();
        options.Setup(x => x.Value).Returns(new TokenModel { Token = "toto" });

        var accountRepo = new Mock<IAccountRepository>();
        var logger = new Mock<ILogger<AccountService>>();
        var validationHelper = new ValidationHelper<RefAccountCsv>();
        var operationRepositoryMock = new Mock<IOperationRepository>();

        var accountService = new AccountService(accountRepo.Object, operationRepositoryMock.Object, _logger.Object);

        var blobStorageManagerMock = new Mock<IBlobStorageManager>(MockBehavior.Strict);
        blobStorageManagerMock.Setup(x => x.SaveFileAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

        var controller = new AccountController(accountService, options.Object, blobStorageManagerMock.Object);
        var resultValidation = validationHelper.Validate(new List<RefAccountCsv> { account });

        // Act
        var response = await controller.UpdateAsync("toto", csvContent.ToString()) as BadRequestObjectResult;

        // Assert
        response.Should().NotBeNull();
        response!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        response!.Value.Should().Be($"Csv Accounts retreval process success with errors: {JsonConvert.SerializeObject(resultValidation.Errors)}");
    }

    [Fact]
    public async Task UpdateAsync_When_Upload_Csv_Fails_ShouldReturnBadRequest()
    {
        // Arrange
        var account = new RefAccountCsv
        {
            AccountNumber = "ABC1@2345",
            LegalName = "Pulse Corporation",
            AccountCommercialName = "Pulse Corp",
            AccountType = "Corporation",
            AccountEmail = "info@pulse.com",
            AccountNafIdentifier = "NAF123456",
            AccountFlagStatus = 1,
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
            AccountBillingPhone = "phone",
            AccountDeliveryPhone = "deliveryPhone",
            Operation = "INSERT"
        };
        var account2 = new RefAccountCsv
        {
            AccountNumber = "ABC12345",
            LegalName = "Pulse Corporation",
            AccountCommercialName = "Pulse Corp",
            AccountType = "Corporation",
            AccountEmail = "info@pulse.com",
            AccountNafIdentifier = "NAF123456",
            AccountFlagStatus = 1,
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
            AccountBillingPhone = "phone",
            AccountDeliveryPhone = "deliveryPhone",
            Operation = "INSERT"
        };

        var csvContent = new StringBuilder();
        csvContent.AppendLine("AccountNumber;LegalName;AccountCommercialName;AccountType;AccountEmail;AccountNafIdentifier;AccountFlagStatus;AccountSectorCode;AccountTaxeValeurAjoutee;AccountDeliveryEmail;AccountBillingEmail;AccountTaxationSystem;AccountSourceName;AccountISIN;AccountRegisterIdentification1;AccountStaffSize;AccountDeliveryFax;AccountBillingFax;AccountTurnover;AccountRegimeFiscal;AccountTypeTenueComptable;AccountFormeJuridique;AccountStaffSizeSlice;AccountEscCategory;AccountCodeFormeJuridique;AccountInsertedDate;AccountUpdatedDate;DeliveryAddressLine1;DeliveryAddressLine2;DeliveryAddressLine3;DeliveryCity;DeliveryZipCode;DeliveryCountry;DeliveryState;BillingAddressLine1;BillingAddressLine2;BillingAddressLine3;BillingCity;BillingZipCode;BillingCountry;BillingState;AccountBillingPhone;AccountDeliveryPhone;Operation");
        csvContent.AppendLine(
            $"{account.AccountNumber};" +
            $"{account.LegalName};" +
            $"{account.AccountCommercialName};" +
            $"{account.AccountType};" +
            $"{account.AccountEmail};" +
            $"{account.AccountNafIdentifier};" +
            $"{account.AccountFlagStatus};" +
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
            $"{account.AccountBillingPhone};" +
            $"{account.AccountDeliveryPhone};" +
            $"{account.Operation}"
        );
        csvContent.AppendLine(
            $"{account2.AccountNumber};" +
            $"{account2.LegalName};" +
            $"{account2.AccountCommercialName};" +
            $"{account2.AccountType};" +
            $"{account2.AccountEmail};" +
            $"{account2.AccountNafIdentifier};" +
            $"{account2.AccountFlagStatus};" +
            $"{account2.AccountSectorCode};" +
            $"{account2.AccountTaxeValeurAjoutee};" +
            $"{account2.AccountDeliveryEmail};" +
            $"{account2.AccountBillingEmail};" +
            $"{account2.AccountTaxationSystem};" +
            $"{account2.AccountSourceName};" +
            $"{account2.AccountISIN};" +
            $"{account2.AccountRegisterIdentification1};" +
            $"{account2.AccountStaffSize};" +
            $"{account2.AccountDeliveryFax};" +
            $"{account2.AccountBillingFax};" +
            $"{account2.AccountTurnover};" +
            $"{account2.AccountRegimeFiscal};" +
            $"{account2.AccountTypeTenueComptable};" +
            $"{account2.AccountFormeJuridique};" +
            $"{account2.AccountStaffSizeSlice};" +
            $"{account2.AccountEscCategory};" +
            $"{account2.AccountCodeFormeJuridique};" +
            $"{account2.AccountInsertedDate.ToString()};" +
            $"{account2.AccountUpdatedDate.ToString()};" +
            $"{account2.DeliveryAddressLine1};" +
            $"{account2.DeliveryAddressLine2};" +
            $"{account2.DeliveryAddressLine3};" +
            $"{account2.DeliveryCity};" +
            $"{account2.DeliveryZipCode};" +
            $"{account2.DeliveryCountry};" +
            $"{account2.DeliveryState};" +
            $"{account2.BillingAddressLine1};" +
            $"{account2.BillingAddressLine2};" +
            $"{account2.BillingAddressLine3};" +
            $"{account2.BillingCity};" +
            $"{account2.BillingZipCode};" +
            $"{account2.BillingCountry};" +
            $"{account2.BillingState};" +
            $"{account2.AccountBillingPhone};" +
            $"{account2.AccountDeliveryPhone};" +
            $"{account2.Operation}"
        );

        var options = new Mock<IOptions<TokenModel>>();
        options.Setup(x => x.Value).Returns(new TokenModel { Token = "toto" });

        var accountRepo = new Mock<IAccountRepository>();
        var logger = new Mock<ILogger<AccountService>>();
        var validationHelper = new ValidationHelper<RefAccountCsv>();
        var operationRepositoryMock = new Mock<IOperationRepository>();

        var accountService = new AccountService(accountRepo.Object, operationRepositoryMock.Object, _logger.Object);

        var blobStorageManagerMock = new Mock<IBlobStorageManager>(MockBehavior.Strict);
        blobStorageManagerMock.Setup(x => x.SaveFileAsync(It.IsAny<string>(), It.IsAny<string>())).Throws(new BlobStorageOperationException("fail"));

        var controller = new AccountController(accountService, options.Object, blobStorageManagerMock.Object);
        var resultValidation = validationHelper.Validate(new List<RefAccountCsv> { account });

        // Act
        var response = await controller.UpdateAsync("toto", csvContent.ToString()) as BadRequestObjectResult;

        // Assert
        response.Should().NotBeNull();
        response!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        response!.Value.Should().Be("Something went wrong when saving received csv ");
    }
}
