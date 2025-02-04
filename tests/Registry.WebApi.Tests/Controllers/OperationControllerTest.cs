// <copyright file="OperationControllerTest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Exceptions;
using Application.Interfaces;
using Application.Models;
using Application.Requests;
using AutoFixture;
using Registry.WebApi.Controllers;
using FluentAssertions;
using Kpmg.ExceptionMiddleware.AdvancedException;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Net;

namespace Registry.WebApi.Tests.Controllers;

public class OperationControllerTest
{
    private readonly Fixture _fixture;

    public OperationControllerTest()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task GetOperationsAsync_WithValidParam_ShouldReturnOperationList()
    {
        var operationServiceMock = new Mock<IOperationService>();

        var operationList = new List<RegOperationDetail>()
        {
            new RegOperationDetail()
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

    [Fact]
    public async Task UpdateOperationAsync_ReturnsOkResultAsync()
    {
        // Arrange
        var creOperationModelMock = _fixture.Create<RegOperation>();
        var expectedOperation = creOperationModelMock;
        expectedOperation.Status = "APPROVED";

        var jsonPatch = new JsonPatchDocument<RegOperation>();
        jsonPatch.Replace(a => a.Status, "APPROVED");

        var operationServiceMock = new Mock<IOperationService>(MockBehavior.Strict);
        operationServiceMock.Setup(x => x.UpdateOperationAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<RegOperation>()))
                            .ReturnsAsync(expectedOperation);
        operationServiceMock.Setup(x => x.GetOperationByIdAsync(It.IsAny<int>()))
                            .ReturnsAsync(creOperationModelMock);

        var operationController = new OperationController(operationServiceMock.Object);
        // Act
        var result = await operationController.UpdateOperationAsync(creOperationModelMock.Id, "test@email.fr", jsonPatch) as OkObjectResult;

        // Assert
        Assert.Equal(200, result!.StatusCode);
        Assert.Equal(expectedOperation.Status, result.Value.As<RegOperation>().Status);
        Assert.Equal(expectedOperation.LastStatusUpdatedBy, result.Value.As<RegOperation>().LastStatusUpdatedBy);
    }

    [Fact]
    public async Task UpdateOperationAsyncAsync_WithAccountPatchNull_ShouldThrowBadRequestException()
    {
        // Arrange
        var operationServiceMock = new Mock<IOperationService>();

        var operationController = new OperationController(operationServiceMock.Object);

        // Act
        var result = await Assert.ThrowsAsync<BadRequestException>(async () => await operationController.UpdateOperationAsync(It.IsAny<int>(), It.IsAny<string>(), null!));

        // Assert
        Assert.Equal(Errors.BadRequestOperationPatchCode, result.Code);
        Assert.Equal(Errors.BadRequestOperationPatchMessage, result.Message);

    }
}
