//// <copyright file="OperationServiceTest.cs" company="Pulse">
//// Copyright (c) Pulse. All rights reserved.
//// </copyright>

//using Application.Interfaces;
//using Application.Models;
//using Application.Requests;
//using Application.Services;
//using AutoFixture;
//using Castle.Core.Logging;
//using FluentAssertions;
//using Microsoft.Extensions.Logging;
//using Moq;

//namespace ContactRegistry.Application.Tests.Services;

//public class OperationServiceTest
//{
//    private readonly Fixture _fixture;

//    public OperationServiceTest()
//    {
//        _fixture = new Fixture();
//        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(b => _fixture.Behaviors.Remove(b));
//        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
//    }

//    [Fact]
//    public async Task GetOperationsAsync_WithValidParam_ShouldReturnOperationList()
//    {
//        // Arrange
//        var creOperationMock = _fixture.CreateMany<RegOperationDetail>(3);
//        var operationSearchCriteria = new OperationSearchCriteria()
//        {
//            OperationName = "INSERT",
//            Status = "Pending"
//        };

//        var operationRepository = new Mock<IOperationRepository>(MockBehavior.Strict);
//        operationRepository.Setup(r => r.GetOperationsAsync(It.IsAny<string>(), It.IsAny<OperationSearchCriteria>()))
//            .ReturnsAsync(creOperationMock);
//        // Act
//        var operationService = new OperationService(operationRepository.Object,logger);
//        var operations = await operationService.GetOperationsAsync("12128179", operationSearchCriteria);

//        // Assert
//        operationRepository.VerifyAll();
//        operations.Should().BeEquivalentTo(creOperationMock);
//    }

//    [Fact]
//    public async Task GetOperationsAsync_WithInvalidParam_ShouldThrowArgumentNullException()
//    {
//        // Arrange
//        var creOperationMock = _fixture.CreateMany<RegOperationDetail>(3);
//        var operationSearchCriteria = new OperationSearchCriteria()
//        {
//            OperationName = "INSERT",
//            Status = "Pending"
//        };

//        var operationRepository = new Mock<IOperationRepository>(MockBehavior.Strict);
//        operationRepository.Setup(r => r.GetOperationsAsync(It.IsAny<string>(), It.IsAny<OperationSearchCriteria>()))
//            .ReturnsAsync(creOperationMock);

//        // Act
//        var operationService = new OperationService(operationRepository.Object);
//        Task operation() => operationService.GetOperationsAsync(null!, operationSearchCriteria);

//        await Assert.ThrowsAsync<ArgumentNullException>(operation);
//    }

//    [Fact]
//    public async Task UpdateAccount_Should_ReturnsOkResultAsync()
//    {
//        // Arrange
//        var operationMocked = _fixture.Build<RegOperation>()
//            .With(o => o.Id, 1)
//            .With(o => o.Status, "PENDING")
//            .Create();

//        var operationRepository = new Mock<IOperationRepository>(MockBehavior.Strict);
//        operationRepository.Setup(r => r.UpdateOperationAsync(It.IsAny<int>(), It.IsAny<RegOperation>()))
//            .ReturnsAsync(operationMocked);

//        var operationService = new OperationService(operationRepository.Object);

//        // Act
//        var updatedOperation = await operationService.UpdateOperationAsync(1, "email@test.fr", operationMocked);

//        // Assert
//        operationRepository.Verify(repository => repository.UpdateOperationAsync(1, operationMocked));
//        updatedOperation!.LastStatusUpdatedBy.Should().BeSameAs("email@test.fr");
//        updatedOperation!.Status.Should().BeSameAs("PENDING");
//    }
//}
