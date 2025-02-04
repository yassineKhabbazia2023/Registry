// <copyright file="RoleControllerTest.cs" company="Pulse">
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

namespace Registry.WebApi.Tests.Controllers;

public class RoleControllerTest
{
    [Fact]
    public async Task UpdateAsync_WithValidData_ShouldProcess()
    {
        // Arrange
        var role = new RefRoleCsv()
        {
            RoleFlagStatus = 1,
            ContactEmail = "jp@hotmail.com",
            AccountNumber = "19870000442",
            Description = "Description",
            Operation = "INSERT"
        };

        var csvContent = new StringBuilder();
        csvContent.AppendLine("RoleFlagStatus;ContactEmail;AccountNumber;Description;Operation");
        csvContent.AppendLine($"" +
            $"{role.RoleFlagStatus};" +
            $"{role.ContactEmail};" +
            $"{role.AccountNumber};" +
            $"{role.Description};" +
            $"{role.Operation}");

        var options = new Mock<IOptions<TokenModel>>();
        options.Setup(x => x.Value).Returns(new TokenModel { Token = "toto" });

        var roleRepo = new Mock<IRoleRepository>();
        var logger = new Mock<ILogger<RoleService>>();
        var roleService = new RoleService(logger.Object, roleRepo.Object);

        var controller = new RoleController(roleService, options.Object);

        // Act
        var response = await controller.UpdateAsync("toto", csvContent.ToString()) as OkObjectResult;


        // Assert
        response.Should().NotBeNull();
        response!.StatusCode.Should().Be((int)HttpStatusCode.OK);
        response!.Value.Should().Be("Csv Roles retreval process was completed");
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
    public async Task UpdateAsync_WithoutFileCsv_ShouldReturnBadRequest()
    {
        // Arrange
        var options = new Mock<IOptions<TokenModel>>();
        options.Setup(x => x.Value).Returns(new TokenModel { Token = "toto" });
        var roleRepo = new Mock<IRoleRepository>();
        var logger = new Mock<ILogger<RoleService>>();
        var roleService = new RoleService(logger.Object, roleRepo.Object);
        var controller = new RoleController(roleService, options.Object);

        // Act
        var response = await controller.UpdateAsync("toto", null!) as BadRequestObjectResult;


        // Assert
        response.Should().NotBeNull();
        response!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        response!.Value.Should().Be("Invalid data: The input data cannot be null or empty.");
    }

    [Fact]
    public async Task UpdateAsync_ReturnOnlyError_ShouldReturnBadRequest()
    {
        // Arrange
        var role = new RefRoleCsv()
        {
            RoleFlagStatus = 1,
            ContactEmail = "jphotmail.com",
            AccountNumber = "19870000442",
            Description = "Description",
            Operation = "INSERT"
        };

        var csvContent = new StringBuilder();
        csvContent.AppendLine("RoleFlagStatus;ContactEmail;AccountNumber;Description;Operation");
        csvContent.AppendLine($"" +
            $"{role.RoleFlagStatus};" +
            $"{role.ContactEmail};" +
            $"{role.AccountNumber};" +
            $"{role.Description};" +
            $"{role.Operation}");

        var options = new Mock<IOptions<TokenModel>>();
        options.Setup(x => x.Value).Returns(new TokenModel { Token = "toto" });
        var roleRepo = new Mock<IRoleRepository>();
        var logger = new Mock<ILogger<RoleService>>();
        var validationHelper = new ValidationHelper<RefRoleCsv>();
        var roleService = new RoleService(logger.Object, roleRepo.Object);
        var controller = new RoleController(roleService, options.Object);
        var resultValidation = validationHelper.Validate(new List<RefRoleCsv> { role });

        // Act
        var response = await controller.UpdateAsync("toto", csvContent.ToString()) as BadRequestObjectResult;


        // Assert
        response.Should().NotBeNull();
        response!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        response!.Value.Should().Be($"Csv Roles retreval process unsuccessuf with errors: {JsonConvert.SerializeObject(resultValidation.Errors)}");
    }

    [Fact]
    public async Task UpdateAsync_ReturnErrorAndValid_ShouldReturnBadRequest()
    {
        // Arrange
        var role = new RefRoleCsv()
        {
            RoleFlagStatus = 1,
            ContactEmail = "jphotmail.com",
            AccountNumber = "19870000442",
            Description = "Description",
            Operation = "INSERT"
        };
        var role2 = new RefRoleCsv()
        {
            RoleFlagStatus = 1,
            ContactEmail = "jphot@mail.com",
            AccountNumber = "19870000442",
            Description = "Description",
            Operation = "INSERT"
        };

        var csvContent = new StringBuilder();
        csvContent.AppendLine("RoleFlagStatus;ContactEmail;AccountNumber;Description;Operation");
        csvContent.AppendLine($"" +
            $"{role.RoleFlagStatus};" +
            $"{role.ContactEmail};" +
            $"{role.AccountNumber};" +
            $"{role.Description};" +
            $"{role.Operation}");
        csvContent.AppendLine($"" +
           $"{role2.RoleFlagStatus};" +
           $"{role2.ContactEmail};" +
           $"{role2.AccountNumber};" +
           $"{role2.Description};" +
           $"{role2.Operation}");

        var options = new Mock<IOptions<TokenModel>>();
        options.Setup(x => x.Value).Returns(new TokenModel { Token = "toto" });
        var roleRepo = new Mock<IRoleRepository>();
        var logger = new Mock<ILogger<RoleService>>();
        var validationHelper = new ValidationHelper<RefRoleCsv>();
        var roleService = new RoleService(logger.Object, roleRepo.Object);
        var controller = new RoleController(roleService, options.Object);
        var resultValidation = validationHelper.Validate(new List<RefRoleCsv> { role });

        // Act
        var response = await controller.UpdateAsync("toto", csvContent.ToString()) as BadRequestObjectResult;


        // Assert
        response.Should().NotBeNull();
        response!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        response!.Value.Should().Be($"Csv Roles retreval process success with errors: {JsonConvert.SerializeObject(resultValidation.Errors)}");
    }
}
