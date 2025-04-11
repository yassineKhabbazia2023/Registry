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
using Application.Models.Commons;
using Application.Services;

namespace Registry.WebApi.Tests.Controllers;

public class OperationControllerTest
{
    private readonly Fixture _fixture;
    private Mock<IOperationService> _operationService;
    private readonly OperationController _sut;

    public OperationControllerTest()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        _operationService = new Mock<IOperationService>();
        _sut = new OperationController(_operationService.Object);
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
            OperationApprovalStatus = "Pending"
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
            OperationApprovalStatus = "Pending"
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
        operationServiceMock.Setup(x => x.UpdateOperationByIdAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<RegOperation>()))
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

    #region GetPendingRoleApprovalsAsync

    [Fact]
    public async Task GivenExpectedParams_WhenGetPendingRoleApprovalsAsyncInvoked_ThenReturnExpectedResult()
    {
        // Arrange
        int pageSize = 10;
        int pageNumber = 1;
        string? search = null;
        var contactId = this._fixture.Create<int>();
        var expectedResponse = this._fixture.Create<PagedResult<PendingRoleApprovals>>();
        _operationService.Setup(Mock => Mock.GetPendingRoleApprovalsAsync(contactId, pageNumber, pageSize, search)).ReturnsAsync(expectedResponse);

        // Act
        var result = await this._sut.GetPendingRoleApprovalsAsync(contactId, pageNumber, pageSize, search) as OkObjectResult;

        // Assert
        Assert.NotNull(result);

        var response = result.Value as PagedResult<PendingRoleApprovals>;
        Assert.NotNull(response);
        Assert.Equal((int)HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(expectedResponse.TotalItems, response.TotalItems);
        Assert.Equal(expectedResponse.Items, response.Items);
    }


    [Fact]
    public async Task GivenInvalidContactId_WhenGetPendingRoleApprovalsAsyncInvoked_ThenThrowInternalServerError()
    {
        // Arrange
        int pageSize = 10;
        int pageNumber = 1;
        string? search = null;
        var contactId = this._fixture.Create<int>();
        _operationService.Setup(Mock => Mock.GetPendingRoleApprovalsAsync(contactId, pageNumber, pageSize, search)).Throws(new TechnicalException());

        // Act
        var result = await this._sut.GetPendingRoleApprovalsAsync(contactId, pageNumber, pageSize, search) as ObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal((int)HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [Fact]
    public async Task GivenInvalidContactId_WhenGetPendingRoleApprovalsAsyncInvoked_Return_BadRequest()
    {
        // Arrange
        int pageSize = 10;
        int pageNumber = 1;
        string? search = null;
        _operationService.Setup(Mock => Mock.GetPendingRoleApprovalsAsync(-1, pageNumber, pageSize, search)).ReturnsAsync(new PagedResult<PendingRoleApprovals>());

        // Act
        var result = await this._sut.GetPendingRoleApprovalsAsync(-1, pageNumber, pageSize, search) as ObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal((int)HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task GivenSearchParam_WhenGetPendingRoleApprovalsAsyncInvoked_ThenReturnFilteredResults()
    {
        // Arrange
        int pageSize = 10;
        int pageNumber = 1;
        string search = "ACC1";
        var contactId = this._fixture.Create<int>();
        var expectedResponse = this._fixture.Create<PagedResult<PendingRoleApprovals>>();
        _operationService.Setup(Mock => Mock.GetPendingRoleApprovalsAsync(contactId, pageNumber, pageSize, search)).ReturnsAsync(expectedResponse);

        // Act
        var result = await this._sut.GetPendingRoleApprovalsAsync(contactId, pageNumber, pageSize, search) as OkObjectResult;

        // Assert
        Assert.NotNull(result);

        var response = result.Value as PagedResult<PendingRoleApprovals>;
        Assert.NotNull(response);
        Assert.Equal((int)HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(expectedResponse.TotalItems, response.TotalItems);
        Assert.Equal(expectedResponse.Items, response.Items);
    }

    #endregion GetPendingRoleApprovalsAsync
}
