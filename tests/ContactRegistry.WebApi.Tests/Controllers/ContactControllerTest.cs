// <copyright file="ContactControllerTest.cs" company="Pulse">
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

public class ContactControllerTest
{
    [Fact]
    public async Task UploadAsync_InvalidFile_Retuns_BadRequest()
    {
        // Arrnge
        var fileName = "data.csv";
        var stream = new MemoryStream(new byte[0]);

        IFormFile file = new FormFile(stream, 0, stream.Length, "id_from_form", fileName);

        var contactService = new Mock<IContactService>();
        var options = new Mock<IOptions<TokenModel>>();

        // Act
        var contactController = new ContactController(contactService.Object, options.Object);
        var response = await contactController.UploadAsync(file) as ObjectResult;

        // Assert
        contactService.Verify(s => s.ProcessContactAsync(It.IsAny<IEnumerable<ContactCsv>>()), Times.Never());
        response!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        response.Value.Should().Be("Invalid file.");
    }

    [Fact]
    public async Task UploadAsync_ValidFile_Retuns_Ok()
    {
        var contact = new ContactCsv(
            Id: new Guid(),
            Email: "john.doe@example.com",
            FirstName: "John",
            LastName: "Doe",
            IsCustomer: true,
            IsActive: true,
            LandPhone: "1234567890",
            MobilePhone: "0987654321",
            JobDescription: "Developer",
            OfficeId: Guid.NewGuid()
        );

        var csvContent = new StringBuilder();
        csvContent.AppendLine("Id;Email;FirstName;LastName;IsCustomer;IsActive;LandPhone;MobilePhone;JobDescription;OfficeId");
        csvContent.AppendLine($"" +
            $"{contact.Id};" +
            $"{contact.Email};" +
            $"{contact.FirstName};" +
            $"{contact.LastName};" +
            $"{contact.IsCustomer};" +
            $"{contact.IsActive};" +
            $"{contact.LandPhone};" +
            $"{contact.MobilePhone};" +
            $"{contact.JobDescription};" +
            $"{contact.OfficeId}");

        var stream = new MemoryStream(Encoding.GetEncoding("ISO-8859-1").GetBytes(csvContent.ToString()));
        IFormFile file = new FormFile(stream, 0, stream.Length, "id_from_form", "contacts.csv");


        var contactService = new Mock<IContactService>(MockBehavior.Strict);
        contactService.Setup(s => s.ProcessContactAsync(It.IsAny<IEnumerable<ContactCsv>>()))
            .Callback<IEnumerable<ContactCsv>>(data =>
            {
                var firstData = data.First();
                firstData.Should().NotBeNull();
                firstData.Should().Be(contact);
            })
            .Returns(Task.CompletedTask);

        // Act
        var options = new Mock<IOptions<TokenModel>>();
        var contactController = new ContactController(contactService.Object, options.Object);
        var response = await contactController.UploadAsync(file) as ObjectResult;

        // Assert
        contactService.Verify(s => s.ProcessContactAsync(It.IsAny<IEnumerable<ContactCsv>>()), Times.Once());
        response!.StatusCode.Should().Be((int)HttpStatusCode.OK);
        response.Value.Should().Be("File processed successfully.");
    }

    [Fact]
    public async Task ExportDataAsync_Returns_ExpectedFileResult()
    {
        // Arrange
        var expectedData = "{'Name': 'John', 'Age': 30}"; // Example JSON data
        var expectedFileName = "CreContacts.json";

        var options = new Mock<IOptions<TokenModel>>();
        var contactServiceMock = new Mock<IContactService>();
        contactServiceMock.Setup(x => x.StreamContactsJsonAsync(It.IsAny<StreamWriter>()))
                            .Callback<StreamWriter>(async writer => await writer.WriteAsync(expectedData));

        var controller = new ContactController(contactServiceMock.Object, options.Object);

        // Act
        var result = await controller.ExportDataAsync();

        // Assert
        result.Should().BeOfType<FileStreamResult>();
        var fileContentResult = result as FileStreamResult;
        contactServiceMock.Verify(x => x.StreamContactsJsonAsync(It.IsAny<StreamWriter>()), Times.Once);
        fileContentResult!.FileDownloadName.Should().Be(expectedFileName, null!);
    }

    [Fact]
    public async Task UpdateAsync_WithValidData_ShouldProcess()
    {
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
            OfficeId = Guid.NewGuid(),
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

        var contactService = new Mock<IContactService>();
        contactService.Setup(s => s.InsertContactsAsync(It.IsAny<IEnumerable<RefContactCsv>>()))
            .Callback<IEnumerable<RefContactCsv>>(data =>
            {
                var firstData = data.First();
                firstData.Should().NotBeNull();
                firstData.Should().BeEquivalentTo(contact);
            })
            .Returns(Task.CompletedTask);

        var controller = new ContactController(contactService.Object, options.Object);

        var response = await controller.UpdateAsync("toto", csvContent.ToString()) as OkObjectResult;

        contactService.VerifyAll();
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

        var controller = new ContactController(null!, options.Object);

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

        var controller = new ContactController(null!, options.Object);

        var response = await controller.UpdateAsync("toto", null!) as BadRequestObjectResult;

        response.Should().NotBeNull();
        response!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        response!.Value.Should().Be("Invalid data: message cannot be null or empty.");
    }
}
