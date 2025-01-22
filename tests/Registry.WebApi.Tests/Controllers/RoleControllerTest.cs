// <copyright file="RoleControllerTest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models;
using ContactRegistry.WebApi.Controllers;
using FluentAssertions;
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
    public async Task UpdateAsync_WithValidData_ShouldProcess()
    {
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

        var roleService = new Mock<IRoleService>(MockBehavior.Strict);
        roleService.Setup(s => s.InsertRolesAsync(It.IsAny<IEnumerable<RefRoleCsv>>()))
            .Callback<IEnumerable<RefRoleCsv>>(data =>
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
        response!.Value.Should().Be("Invalid data: The input data cannot be null or empty.");
    }
}
