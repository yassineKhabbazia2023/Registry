// <copyright file="OperationControllerTest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models;
using Application.Requests;
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


        var operationSearchCriteria = new OperationSearchCriteria()
        {
            OperationName = "INSERT",
            Status = "Pending"
        };

        operationServiceMock.Setup(x => x.GetOperationsAsync(It.IsAny<string>(), It.IsAny<OperationSearchCriteria>())).ReturnsAsync(operationList);

        var controller = new OperationController(operationServiceMock.Object);

        var response = await controller.GetOperationsAsync("12128179", operationSearchCriteria) as ObjectResult;

        response.Should().NotBeNull();
        response!.StatusCode.Should().Be((int)HttpStatusCode.OK);
        response!.Value.Should().BeEquivalentTo(operationList);
    }

    [Fact]
    public async Task GetOperationsAsync_WithInvalidParam_ShouldThrowArgumentNullException()
    {
        var operationServiceMock = new Mock<IOperationService>();

        var operationSearchCriteria = new OperationSearchCriteria()
        {
            OperationName = "INSERT",
            Status = "Pending"
        };
        operationServiceMock.Setup(x => x.GetOperationsAsync(It.IsAny<string>(), It.IsAny<OperationSearchCriteria>())).ThrowsAsync(new ArgumentNullException());

        var controller = new OperationController(operationServiceMock.Object);

        Task operation() => controller.GetOperationsAsync(null!, operationSearchCriteria);

        await Assert.ThrowsAsync<ArgumentNullException>(operation);
    }
}
