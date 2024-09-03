// <copyright file="RoleControllerTest.cs" company="Pulse">
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

public class RoleControllerTest
{
    [Fact]
    public async Task UploadAsync_InvalidFile_Returns_BadRequest()
    {
        // Arrange
        var fileName = "data.csv";
        var stream = new MemoryStream(new byte[0]);
        IFormFile file = new FormFile(stream, 0, stream.Length, "id_from_form", fileName);

        var roleService = new Mock<IRoleService>();
        var options = new Mock<IOptions<TokenModel>>();

        // Act
        var roleController = new RoleController(roleService.Object, options.Object);
        var response = await roleController.UploadAsync(file) as ObjectResult;

        // Assert
        roleService.Verify(s => s.ProcessRoleAsync(It.IsAny<IEnumerable<RoleCsv>>()), Times.Never());
        response!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        response.Value.Should().Be("Invalid file.");
    }

    [Fact]
    public async Task UploadAsync_ValidFile_Returns_Ok()
    {
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

        var options = new Mock<IOptions<TokenModel>>();
        var roleService = new Mock<IRoleService>(MockBehavior.Strict);
        roleService.Setup(s => s.ProcessRoleAsync(It.IsAny<IEnumerable<RoleCsv>>()))
            .Callback<IEnumerable<RoleCsv>>(data =>
            {
                var firstData = data.First();
                firstData.Should().NotBeNull();
                firstData.Should().BeEquivalentTo(role);
            })
            .Returns(Task.CompletedTask);

        // Act
        var roleController = new RoleController(roleService.Object, options.Object);
        var response = await roleController.UploadAsync(file) as ObjectResult;

        // Assert
        roleService.Verify(s => s.ProcessRoleAsync(It.IsAny<IEnumerable<RoleCsv>>()), Times.Once());
        response!.StatusCode.Should().Be((int)HttpStatusCode.OK);
        response.Value.Should().Be("File processed successfully.");
    }

    [Fact]
    public async Task ExportDataAsync_Returns_ExpectedFileResult()
    {
        // Arrange
        var expectedData = "{'Name': 'John', 'Age': 30}"; // Example JSON data
        var expectedFileName = "CreRoles.json";

        var options = new Mock<IOptions<TokenModel>>();
        var roleServiceMock = new Mock<IRoleService>();
        roleServiceMock.Setup(x => x.StreamRolesJsonAsync(It.IsAny<StreamWriter>()))
                            .Callback<StreamWriter>(async writer => await writer.WriteAsync(expectedData));

        var controller = new RoleController(roleServiceMock.Object, options.Object);

        // Act
        var result = await controller.ExportDataAsync();

        // Assert
        result.Should().BeOfType<FileStreamResult>();
        var fileContentResult = result as FileStreamResult;
        roleServiceMock.Verify(x => x.StreamRolesJsonAsync(It.IsAny<StreamWriter>()), Times.Once);
        fileContentResult!.FileDownloadName.Should().Be(expectedFileName);
    }

    [Fact]
    public async Task UpdateAsync_WithValidData_ShouldProcess()
    {
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

        var options = new Mock<IOptions<TokenModel>>();
        options.Setup(x => x.Value).Returns(new TokenModel { Token = "toto" });

        var roleService = new Mock<IRoleService>(MockBehavior.Strict);
        roleService.Setup(s => s.ProcessRoleAsync(It.IsAny<IEnumerable<RoleCsv>>()))
            .Callback<IEnumerable<RoleCsv>>(data =>
            {
                var firstData = data.First();
                firstData.Should().NotBeNull();
                firstData.Should().BeEquivalentTo(role);
            })
            .Returns(Task.CompletedTask);

        var controller = new RoleController(roleService.Object, options.Object);

        var response = await controller.UpdateAsync("toto", csvContent.ToString()) as OkObjectResult;

        roleService.VerifyAll();
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

        var controller = new RoleController(null!, options.Object);

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

        var controller = new RoleController(null!, options.Object);

        var response = await controller.UpdateAsync("toto", null!) as BadRequestObjectResult;

        response.Should().NotBeNull();
        response!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        response!.Value.Should().Be("Invalid data: message cannot be null or empty.");
    }
}
