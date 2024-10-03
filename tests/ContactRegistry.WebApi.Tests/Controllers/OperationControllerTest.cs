// <copyright file="OperationControllerTest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models;
using ContactRegistry.WebApi.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Net;

namespace ContactRegistry.WebApi.Tests.Controllers;

public class OperationControllerTest
{
    [Fact]
    public async Task GetOperationsAsync_WithValidParam_ShouldReturnOperationList()
    {
        var operationServiceMock = new Mock<IOperationService>();

        var operationList = new List<CreOperation>()
        {
            new CreOperation()
            {
                OperationId = 1,
                RoleId = Guid.NewGuid(),
                OperationName = "Name",
                OperationType = "TYPE",
                CreationDate = DateTime.UtcNow,
                Status = "Pending",
                Email = "email@test.fr",
                FirstName = "firstName",
                LastName = "lastName",
                AccountNumber = "12128179"
            }
        };

        operationServiceMock.Setup(x => x.GetOperationsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(operationList);

        var controller = new OperationController(operationServiceMock.Object);

        var response = await controller.GetOperationsAsync("Name", "Pending", "12128179") as ObjectResult;

        response.Should().NotBeNull();
        response!.StatusCode.Should().Be((int)HttpStatusCode.OK);
        response!.Value.Should().BeEquivalentTo(operationList);
    }

    [Fact]
    public async Task GetOperationsAsync_WithInvalidParam_ShouldThrowArgumentNullException()
    {
        var operationServiceMock = new Mock<IOperationService>();
        operationServiceMock.Setup(x => x.GetOperationsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ThrowsAsync(new ArgumentNullException());

        var controller = new OperationController(operationServiceMock.Object);

        Task operation() => controller.GetOperationsAsync("Name", "Pending", null!);

        await Assert.ThrowsAsync<ArgumentNullException>(operation);
    }
}
