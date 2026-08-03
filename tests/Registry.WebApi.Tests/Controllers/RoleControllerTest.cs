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
using Application.Exceptions;
using Application.Options;

namespace Registry.WebApi.Tests.Controllers;

public class RoleControllerTest
{
    private readonly IOptions<BackGroundJobOptions> backGroundJobOptions;
    public RoleControllerTest()
    {
        backGroundJobOptions = Mock.Of<IOptions<BackGroundJobOptions>>();
    }

    [Fact]
    public async Task UpdateAsync_WithValidData_ShouldProcess()
    {
        // Arrange
        var role = new RefRoleCsv()
        {
            RoleFlagStatus = 1,
            ContactFlagPortailFactures = false,
            ContactEmail = "jp@hotmail.com",
            AccountNumber = "19870000442",
            Description = "Description",
            Operation = "INSERT"
        };

        var csvContent = new StringBuilder();
        csvContent.AppendLine("RoleFlagStatus;ContactFlagPortailFactures;ContactFlagMainContact;ContactEmail;AccountNumber;Description;Operation");
        csvContent.AppendLine($"" +
            $"{role.RoleFlagStatus};" +
            $"{role.ContactFlagPortailFactures};" +
            $"{role.ContactFlagMainContact};" +
            $"{role.ContactEmail};" +
            $"{role.AccountNumber};" +
            $"{role.Description};" +
            $"{role.Operation}");

        var options = new Mock<IOptions<TokenModel>>();
        options.Setup(x => x.Value).Returns(new TokenModel { Token = "toto" });

        var roleRepo = new Mock<IRoleRepository>();
        var logger = new Mock<ILogger<RoleService>>();
        var factory = Mock.Of<IRoleDeepValidatorFactory>();
        var roleService = new RoleService(logger.Object, roleRepo.Object, factory, backGroundJobOptions);

        var blobStorageManagerMock = new Mock<IBlobStorageManager>(MockBehavior.Strict);
        blobStorageManagerMock.Setup(x => x.SaveFileAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((endpoint, fileContent) =>
            {
                Assert.Equal("Role", endpoint);
                Assert.Equal(csvContent.ToString(), fileContent);
            }).ReturnsAsync("Role_20260101_000000.csv");

        var controller = new RoleController(roleService, options.Object, blobStorageManagerMock.Object);

        // Act
        var response = await controller.UpdateAsync("toto", csvContent.ToString()) as OkObjectResult;


        // Assert
        blobStorageManagerMock.VerifyAll();
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

        var blobStorageManagerMock = new Mock<IBlobStorageManager>(MockBehavior.Strict);

        var controller = new RoleController(null!, options.Object, blobStorageManagerMock.Object);

        var response = await controller.UpdateAsync(token, "test test"!) as UnauthorizedObjectResult;

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
    public async Task UpdateAsync_WithoutFileCsv_ShouldReturnBadRequest()
    {
        // Arrange
        var options = new Mock<IOptions<TokenModel>>();
        options.Setup(x => x.Value).Returns(new TokenModel { Token = "toto" });
        var roleRepo = new Mock<IRoleRepository>();
        var logger = new Mock<ILogger<RoleService>>();
        var factory = Mock.Of<IRoleDeepValidatorFactory>();
        var roleService = new RoleService(logger.Object, roleRepo.Object, factory, backGroundJobOptions);

        var blobStorageManagerMock = new Mock<IBlobStorageManager>(MockBehavior.Strict);
        blobStorageManagerMock.Setup(x => x.SaveFileAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("Role_20260101_000000.csv");

        var controller = new RoleController(roleService, options.Object, blobStorageManagerMock.Object);

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
            ContactFlagPortailFactures = false,
            ContactEmail = "jphotmail.com",
            AccountNumber = "19870000442",
            Description = "Description",
            Operation = "INSERT"
        };

        var csvContent = new StringBuilder();
        csvContent.AppendLine("RoleFlagStatus;ContactFlagPortailFactures;ContactFlagMainContact;ContactEmail;AccountNumber;Description;Operation");
        csvContent.AppendLine($"" +
            $"{role.RoleFlagStatus};" +
            $"{role.ContactFlagPortailFactures};" +
            $"{role.ContactFlagMainContact};" +
            $"{role.ContactEmail};" +
            $"{role.AccountNumber};" +
            $"{role.Description};" +
            $"{role.Operation}");

        var options = new Mock<IOptions<TokenModel>>();
        options.Setup(x => x.Value).Returns(new TokenModel { Token = "toto" });
        var roleRepo = new Mock<IRoleRepository>();
        var logger = new Mock<ILogger<RoleService>>();
        var validationHelper = new ValidationHelper<RefRoleCsv>();
        var factory = Mock.Of<IRoleDeepValidatorFactory>();
        var roleService = new RoleService(logger.Object, roleRepo.Object, factory, backGroundJobOptions);

        var blobStorageManagerMock = new Mock<IBlobStorageManager>(MockBehavior.Strict);
        blobStorageManagerMock.Setup(x => x.SaveFileAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("Role_20260101_000000.csv");

        var controller = new RoleController(roleService, options.Object, blobStorageManagerMock.Object);
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
            ContactFlagPortailFactures = false,
            ContactEmail = "jphotmail.com",
            AccountNumber = "19870000442",
            Description = "Description",
            Operation = "INSERT"
        };
        var role2 = new RefRoleCsv()
        {
            RoleFlagStatus = 1,
            ContactFlagPortailFactures = false,
            ContactEmail = "jphot@mail.com",
            AccountNumber = "19870000442",
            Description = "Description",
            Operation = "INSERT"
        };

        var csvContent = new StringBuilder();
        csvContent.AppendLine("RoleFlagStatus;ContactFlagPortailFactures;ContactFlagMainContact;ContactEmail;AccountNumber;Description;Operation");
        csvContent.AppendLine($"" +
            $"{role.RoleFlagStatus};" +
            $"{role.ContactFlagPortailFactures};" +
            $"{role.ContactFlagMainContact};" +
            $"{role.ContactEmail};" +
            $"{role.AccountNumber};" +
            $"{role.Description};" +
            $"{role.Operation}");
        csvContent.AppendLine($"" +
           $"{role2.RoleFlagStatus};" +
           $"{role2.ContactFlagPortailFactures};" +
           $"{role2.ContactFlagMainContact};" +
           $"{role2.ContactEmail};" +
           $"{role2.AccountNumber};" +
           $"{role2.Description};" +
           $"{role2.Operation}");

        var options = new Mock<IOptions<TokenModel>>();
        options.Setup(x => x.Value).Returns(new TokenModel { Token = "toto" });
        var roleRepo = new Mock<IRoleRepository>();
        var logger = new Mock<ILogger<RoleService>>();
        var validationHelper = new ValidationHelper<RefRoleCsv>();
        var factory = Mock.Of<IRoleDeepValidatorFactory>();
        var roleService = new RoleService(logger.Object, roleRepo.Object, factory, backGroundJobOptions);

        var blobStorageManagerMock = new Mock<IBlobStorageManager>(MockBehavior.Strict);
        blobStorageManagerMock.Setup(x => x.SaveFileAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("Role_20260101_000000.csv");

        var controller = new RoleController(roleService, options.Object, blobStorageManagerMock.Object);
        var resultValidation = validationHelper.Validate(new List<RefRoleCsv> { role });

        // Act
        var response = await controller.UpdateAsync("toto", csvContent.ToString()) as BadRequestObjectResult;


        // Assert
        response.Should().NotBeNull();
        response!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        response!.Value.Should().Be($"Csv Roles retreval process success with errors: {JsonConvert.SerializeObject(resultValidation.Errors)}");
    }

    [Fact]
    public async Task UpdateAsync_When_Upload_Csv_Fails_ShouldReturnBadRequest()
    {
        // Arrange
        var role = new RefRoleCsv()
        {
            RoleFlagStatus = 1,
            ContactFlagPortailFactures = false,
            ContactEmail = "jphotmail.com",
            AccountNumber = "19870000442",
            Description = "Description",
            Operation = "INSERT"
        };
        var role2 = new RefRoleCsv()
        {
            RoleFlagStatus = 1,
            ContactFlagPortailFactures = false,
            ContactEmail = "jphot@mail.com",
            AccountNumber = "19870000442",
            Description = "Description",
            Operation = "INSERT"
        };

        var csvContent = new StringBuilder();
        csvContent.AppendLine("RoleFlagStatus;ContactFlagPortailFactures;ContactFlagMainContact;ContactEmail;AccountNumber;Description;Operation");
        csvContent.AppendLine($"" +
            $"{role.RoleFlagStatus};" +
            $"{role.ContactFlagPortailFactures};" +
            $"{role.ContactFlagMainContact};" +
            $"{role.ContactEmail};" +
            $"{role.AccountNumber};" +
            $"{role.Description};" +
            $"{role.Operation}");
        csvContent.AppendLine($"" +
           $"{role2.RoleFlagStatus};" +
           $"{role2.ContactFlagPortailFactures};" +
           $"{role2.ContactFlagMainContact};" +
           $"{role2.ContactEmail};" +
           $"{role2.AccountNumber};" +
           $"{role2.Description};" +
           $"{role2.Operation}");

        var options = new Mock<IOptions<TokenModel>>();
        options.Setup(x => x.Value).Returns(new TokenModel { Token = "toto" });
        var roleRepo = new Mock<IRoleRepository>();
        var logger = new Mock<ILogger<RoleService>>();
        var validationHelper = new ValidationHelper<RefRoleCsv>();
        var factory = Mock.Of<IRoleDeepValidatorFactory>();
        var roleService = new RoleService(logger.Object, roleRepo.Object, factory, backGroundJobOptions);

        var blobStorageManagerMock = new Mock<IBlobStorageManager>(MockBehavior.Strict);
        blobStorageManagerMock.Setup(x => x.SaveFileAsync(It.IsAny<string>(), It.IsAny<string>())).Throws(new BlobStorageOperationException("fail"));

        var controller = new RoleController(roleService, options.Object, blobStorageManagerMock.Object);
        var resultValidation = validationHelper.Validate(new List<RefRoleCsv> { role });

        // Act
        var response = await controller.UpdateAsync("toto", csvContent.ToString()) as BadRequestObjectResult;


        // Assert
        response.Should().NotBeNull();
        response!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        response!.Value.Should().Be("Something went wrong when saving received csv ");
    }
}
