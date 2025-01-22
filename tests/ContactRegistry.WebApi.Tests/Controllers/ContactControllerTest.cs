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

        var contactService = new Mock<IContactService>();

        contactService.Setup(s => s.ValidateContacts(It.IsAny<IEnumerable<RefContactCsv>>()))
            .Returns(new List<string>());

        contactService.Setup(s => s.InsertContactsAsync(It.IsAny<IEnumerable<RefContactCsv>>()))
            .Returns(Task.CompletedTask);

        var controller = new ContactController(contactService.Object, options.Object);

        // Act
        var csvData = csvContent.ToString();
        var response = await controller.UpdateAsync("toto", csvData) as OkObjectResult;

        // Assert
        contactService.Verify(s => s.ValidateContacts(It.IsAny<IEnumerable<RefContactCsv>>()), Times.Once);
        contactService.Verify(s => s.InsertContactsAsync(It.IsAny<IEnumerable<RefContactCsv>>()), Times.Once);

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
        response!.Value.Should().Be("Invalid data: The input data cannot be null or empty.");
    }
}
