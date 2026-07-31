// <copyright file="ContactControllerTest.cs" company="Pulse">
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
using CsvHelper;
using Application.Exceptions;
using Application.Helpers.Extensions;

namespace Registry.WebApi.Tests.Controllers;

public class ContactControllerTest
{
    private readonly Mock<IContactRegistryProvider> providerMock;
    private readonly Mock<IOperationService> operationServiceMock;

    public ContactControllerTest()
    {
        providerMock = new Mock<IContactRegistryProvider>();
        operationServiceMock = new Mock<IOperationService>();
    }

    [Fact]
    public async Task UpdateAsync_WithValidData_ShouldProcess()
    {
        // Arrange
        var contact = new RefContactCsv
        {
            ContactFlagStatus = 1,
            Email = "john.doe@example.com",
            FirstName = "John",
            LastName = "Doe",
            IsCustomer = true,
            LandPhone = "1234567890",
            MobilePhone = "0987654321",
            JobDescription = "Developer",
            OfficeId = "La defense",
            Operation = "INSERT"
        };

        var csvContent = new StringBuilder();
        csvContent.AppendLine("ContactFlagStatus;Email;FirstName;LastName;IsCustomer;LandPhone;MobilePhone;JobDescription;OfficeId;Operation");
        csvContent.AppendLine($"" +
            $"{contact.ContactFlagStatus};" +
            $"{contact.Email};" +
            $"{contact.FirstName};" +
            $"{contact.LastName};" +
            $"{contact.IsCustomer};" +
            $"{contact.LandPhone};" +
            $"{contact.MobilePhone};" +
            $"{contact.JobDescription};" +
            $"{contact.OfficeId};" +
            $"{contact.Operation}");

        var options = new Mock<IOptions<TokenModel>>();
        options.Setup(x => x.Value).Returns(new TokenModel { Token = "toto" });

        var contactRepo = new Mock<IContactRepository>();
        var logger = new Mock<ILogger<ContactService>>();
        var contactService = new ContactService(logger.Object, contactRepo.Object, providerMock.Object, operationServiceMock.Object);

        var blobStorageManagerMock = new Mock<IBlobStorageManager>(MockBehavior.Strict);
        blobStorageManagerMock.Setup(x => x.SaveFileAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((endpoint, fileContent) =>
            {
                Assert.Equal("Contact", endpoint);
                Assert.Equal(csvContent.ToString(), fileContent);
            }).ReturnsAsync(true);

        var controller = new ContactController(contactService, options.Object,blobStorageManagerMock.Object);

        // Act
        var csvData = csvContent.ToString();
        var response = await controller.UpdateAsync("toto", csvData) as OkObjectResult;

        // Assert
        blobStorageManagerMock.VerifyAll();
        response.Should().NotBeNull();
        response!.StatusCode.Should().Be((int)HttpStatusCode.OK);
        response!.Value.Should().Be("Csv Contacts retreval process was completed");
    }

    [Fact]
    public async Task UpdateAsync_WithValidLeading0InOfficeCode_ShouldProcessAndRemoveLeading0()
    {
        // Arrange
        var contact = new RefContactCsv
        {
            ContactFlagStatus = 1,
            Email = "john.doe@example.com",
            FirstName = "John",
            LastName = "Doe",
            IsCustomer = true,
            LandPhone = "1234567890",
            MobilePhone = "0987654321",
            JobDescription = "Developer",
            OfficeId = "0025",
            Operation = "INSERT"
        };

        contact.OfficeId.Should().Be("25");
    }

    [Theory]
    [MemberData(nameof(TokenData))]
    public async Task UpdateAsync_WithWrongToken_ShouldReturnUnauthorizedResult(string token)
    {
        var options = new Mock<IOptions<TokenModel>>();
        options.Setup(x => x.Value).Returns(new TokenModel { Token = "toto" });

        var blobStorageManagerMock = new Mock<IBlobStorageManager>(MockBehavior.Strict);

        var controller = new ContactController(null!, options.Object, blobStorageManagerMock.Object);

        var response = await controller.UpdateAsync(token, null!) as UnauthorizedObjectResult;

        response.Should().NotBeNull();
        response!.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
        response.Value.Should().Be("Invalid token.");
        blobStorageManagerMock.Verify(x => x.SaveFileAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
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
    public async Task UpdateAsync_ReturnOnlyError_ShouldReturnBadRequest()
    {
        // Arrange
        var contact = new RefContactCsv
        {
            ContactFlagStatus = 1,
            Email = "john.doeexample.com",
            FirstName = "John",
            LastName = "Doe",
            IsCustomer = true,
            LandPhone = "1234567890",
            MobilePhone = "0987654321",
            JobDescription = "Developer",
            OfficeId = "La defense",
            Operation = "INSERT"
        };

        var csvContent = new StringBuilder();
        csvContent.AppendLine("ContactFlagStatus;Email;FirstName;LastName;IsCustomer;LandPhone;MobilePhone;JobDescription;OfficeId;Operation");
        csvContent.AppendLine($"" +
            $"{contact.ContactFlagStatus};" +
            $"{contact.Email};" +
            $"{contact.FirstName};" +
            $"{contact.LastName};" +
            $"{contact.IsCustomer};" +
            $"{contact.LandPhone};" +
            $"{contact.MobilePhone};" +
            $"{contact.JobDescription};" +
            $"{contact.OfficeId};" +
            $"{contact.Operation}");

        var options = new Mock<IOptions<TokenModel>>();
        options.Setup(x => x.Value).Returns(new TokenModel { Token = "toto" });

        var contactRepo = new Mock<IContactRepository>();
        var logger = new Mock<ILogger<ContactService>>();
        var validationHelper = new ValidationHelper<RefContactCsv>();
        var contactService = new ContactService(logger.Object, contactRepo.Object,providerMock.Object,operationServiceMock.Object);

        var blobStorageManagerMock = new Mock<IBlobStorageManager>(MockBehavior.Strict);
        blobStorageManagerMock.Setup(x => x.SaveFileAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

        var controller = new ContactController(contactService, options.Object, blobStorageManagerMock.Object);
        var resultValidation = validationHelper.Validate(new List<RefContactCsv> { contact });

        // Act
        var response = await controller.UpdateAsync("toto", csvContent.ToString()) as BadRequestObjectResult;

        // Assert
        response.Should().NotBeNull();
        response!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        response!.Value.Should().Be($"Csv Contacts retreval process unsuccessful with errors: {JsonConvert.SerializeObject(resultValidation.Errors)}");
    }

    [Fact]
    public async Task UpdateAsync_ReturnValidAndError_ShouldReturnBadRequest()
    {
        // Arrange
        var contact = new RefContactCsv
        {
            ContactFlagStatus = 1,
            Email = "john.doeexample.com",
            FirstName = "John",
            LastName = "Doe",
            IsCustomer = true,
            LandPhone = "1234567890",
            MobilePhone = "0987654321",
            JobDescription = "Developer",
            OfficeId = "La defense",
            Operation = "INSERT"
        };
        var contact2 = new RefContactCsv
        {
            ContactFlagStatus = 1,
            Email = "john.doe@example.com",
            FirstName = "John",
            LastName = "Doe",
            IsCustomer = true,
            LandPhone = "1234567890",
            MobilePhone = "0987654321",
            JobDescription = "Developer",
            OfficeId = "La defense",
            Operation = "INSERT"
        };

        var csvContent = new StringBuilder();
        csvContent.AppendLine("ContactFlagStatus;Email;FirstName;LastName;IsCustomer;LandPhone;MobilePhone;JobDescription;OfficeId;Operation");
        csvContent.AppendLine($"" +
            $"{contact.ContactFlagStatus};" +
            $"{contact.Email};" +
            $"{contact.FirstName};" +
            $"{contact.LastName};" +
            $"{contact.IsCustomer};" +
            $"{contact.LandPhone};" +
            $"{contact.MobilePhone};" +
            $"{contact.JobDescription};" +
            $"{contact.OfficeId};" +
            $"{contact.Operation}");
        csvContent.AppendLine($"" +
            $"{contact2.ContactFlagStatus};" +
            $"{contact2.Email};" +
            $"{contact2.FirstName};" +
            $"{contact2.LastName};" +
            $"{contact2.IsCustomer};" +
            $"{contact2.LandPhone};" +
            $"{contact2.MobilePhone};" +
            $"{contact2.JobDescription};" +
            $"{contact2.OfficeId};" +
            $"{contact2.Operation}");

        var options = new Mock<IOptions<TokenModel>>();
        options.Setup(x => x.Value).Returns(new TokenModel { Token = "toto" });

        var contactRepo = new Mock<IContactRepository>();
        var logger = new Mock<ILogger<ContactService>>();
        var validationHelper = new ValidationHelper<RefContactCsv>();
        var contactService = new ContactService(logger.Object, contactRepo.Object, providerMock.Object, operationServiceMock.Object);

        var blobStorageManagerMock = new Mock<IBlobStorageManager>(MockBehavior.Strict);
        blobStorageManagerMock.Setup(x => x.SaveFileAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

        var controller = new ContactController(contactService, options.Object, blobStorageManagerMock.Object);
        var resultValidation = validationHelper.Validate(new List<RefContactCsv> { contact });

        // Act
        var response = await controller.UpdateAsync("toto", csvContent.ToString()) as BadRequestObjectResult;

        // Assert
        response.Should().NotBeNull();
        response!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        response!.Value.Should().Be($"Csv Contacts retreval process success with errors: {JsonConvert.SerializeObject(resultValidation.Errors)}");
    }

    [Fact]
    public async Task UpdateAsync_When_Upload_Csv_Fails_ShouldReturnBadRequest()
    {
        // Arrange
        var contact = new RefContactCsv
        {
            ContactFlagStatus = 1,
            Email = "john.doeexample.com",
            FirstName = "John",
            LastName = "Doe",
            IsCustomer = true,
            LandPhone = "1234567890",
            MobilePhone = "0987654321",
            JobDescription = "Developer",
            OfficeId = "La defense",
            Operation = "INSERT"
        };
        var contact2 = new RefContactCsv
        {
            ContactFlagStatus = 1,
            Email = "john.doe@example.com",
            FirstName = "John",
            LastName = "Doe",
            IsCustomer = true,
            LandPhone = "1234567890",
            MobilePhone = "0987654321",
            JobDescription = "Developer",
            OfficeId = "La defense",
            Operation = "INSERT"
        };

        var csvContent = new StringBuilder();
        csvContent.AppendLine("ContactFlagStatus;Email;FirstName;LastName;IsCustomer;LandPhone;MobilePhone;JobDescription;OfficeId;Operation");
        csvContent.AppendLine($"" +
            $"{contact.ContactFlagStatus};" +
            $"{contact.Email};" +
            $"{contact.FirstName};" +
            $"{contact.LastName};" +
            $"{contact.IsCustomer};" +
            $"{contact.LandPhone};" +
            $"{contact.MobilePhone};" +
            $"{contact.JobDescription};" +
            $"{contact.OfficeId};" +
            $"{contact.Operation}");
        csvContent.AppendLine($"" +
            $"{contact2.ContactFlagStatus};" +
            $"{contact2.Email};" +
            $"{contact2.FirstName};" +
            $"{contact2.LastName};" +
            $"{contact2.IsCustomer};" +
            $"{contact2.LandPhone};" +
            $"{contact2.MobilePhone};" +
            $"{contact2.JobDescription};" +
            $"{contact2.OfficeId};" +
            $"{contact2.Operation}");

        var options = new Mock<IOptions<TokenModel>>();
        options.Setup(x => x.Value).Returns(new TokenModel { Token = "toto" });

        var contactRepo = new Mock<IContactRepository>();
        var logger = new Mock<ILogger<ContactService>>();
        var validationHelper = new ValidationHelper<RefContactCsv>();
        var contactService = new ContactService(logger.Object, contactRepo.Object, providerMock.Object, operationServiceMock.Object);

        var blobStorageManagerMock = new Mock<IBlobStorageManager>(MockBehavior.Strict);
        blobStorageManagerMock.Setup(x => x.SaveFileAsync(It.IsAny<string>(), It.IsAny<string>())).Throws(new BlobStorageOperationException("fail"));

        var controller = new ContactController(contactService, options.Object, blobStorageManagerMock.Object);
        var resultValidation = validationHelper.Validate(new List<RefContactCsv> { contact });

        // Act
        var response = await controller.UpdateAsync("toto", csvContent.ToString()) as BadRequestObjectResult;

        // Assert
        response.Should().NotBeNull();
        response!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        response!.Value.Should().Be("Something went wrong when saving received csv ");
    }

    [Fact]
    public async Task UpdateAsync_WithCollabEmail_ShouldSkip_Contact()
    {
        // Arrange
        var contact = new RefContactCsv
        {
            ContactFlagStatus = 1,
            Email = "john.doe@rydge.fr",
            FirstName = "John",
            LastName = "Doe",
            IsCustomer = true,
            LandPhone = "1234567890",
            MobilePhone = "0987654321",
            JobDescription = "Developer",
            OfficeId = "La defense",
            Operation = "INSERT"
        };

        var contact2 = new RefContactCsv
        {
            ContactFlagStatus = 1,
            Email = "john.doe@user.fr",
            FirstName = "John",
            LastName = "Doe",
            IsCustomer = true,
            LandPhone = "1234567890",
            MobilePhone = "0987654321",
            JobDescription = "Developer",
            OfficeId = "La defense",
            Operation = "INSERT"
        };

        var csvContent = new StringBuilder();
        csvContent.AppendLine("ContactFlagStatus;Email;FirstName;LastName;IsCustomer;LandPhone;MobilePhone;JobDescription;OfficeId;Operation");
        csvContent.AppendLine($"" +
            $"{contact.ContactFlagStatus};" +
            $"{contact.Email};" +
            $"{contact.FirstName};" +
            $"{contact.LastName};" +
            $"{contact.IsCustomer};" +
            $"{contact.LandPhone};" +
            $"{contact.MobilePhone};" +
            $"{contact.JobDescription};" +
            $"{contact.OfficeId};" +
            $"{contact.Operation}");
        csvContent.AppendLine($"" +
            $"{contact2.ContactFlagStatus};" +
            $"{contact2.Email};" +
            $"{contact2.FirstName};" +
            $"{contact2.LastName};" +
            $"{contact2.IsCustomer};" +
            $"{contact2.LandPhone};" +
            $"{contact2.MobilePhone};" +
            $"{contact2.JobDescription};" +
            $"{contact2.OfficeId};" +
            $"{contact2.Operation}");

        var options = new Mock<IOptions<TokenModel>>();
        options.Setup(x => x.Value).Returns(new TokenModel { Token = "toto" });

        var contactRepo = new Mock<IContactRepository>();
        var logger = new Mock<ILogger<ContactService>>();
        var contactService = new ContactService(logger.Object, contactRepo.Object, providerMock.Object, operationServiceMock.Object);
        var validationHelper = new ValidationHelper<RefContactCsv>();

        var blobStorageManagerMock = new Mock<IBlobStorageManager>(MockBehavior.Strict);
        blobStorageManagerMock.Setup(x => x.SaveFileAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((endpoint, fileContent) =>
            {
                Assert.Equal("Contact", endpoint);
                Assert.Equal(csvContent.ToString(), fileContent);
            }).ReturnsAsync(true);

        var controller = new ContactController(contactService, options.Object, blobStorageManagerMock.Object);
        var resultValidation = validationHelper.Validate(new List<RefContactCsv> { contact, contact2 }).ValidateCollabRules();

        // Act
        var csvData = csvContent.ToString();
        var response = await controller.UpdateAsync("toto", csvData) as BadRequestObjectResult;

        // Assert
        resultValidation.ValidateModels.Should().HaveCount(1);
        blobStorageManagerMock.VerifyAll();
        response.Should().NotBeNull();
        response!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        response!.Value.Should().Be("Csv Contacts retreval process success with errors: [{\"LineNumber\":0,\"Errors\":[\"Collaborators cannot be customers. Please check this list of addresses: [john.doe@rydge.fr]\"]}]");
    }
}
