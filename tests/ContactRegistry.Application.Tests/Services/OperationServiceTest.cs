// <copyright file="OperationServiceTest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models;
using Application.Services;
using AutoFixture;
using FluentAssertions;
using Moq;

namespace ContactRegistry.Application.Tests.Services;

public class OperationServiceTest
{
    private readonly Fixture _fixture;

    public OperationServiceTest()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task GetOperationsAsync_WithValidParam_ShouldReturnOperationList()
    {
        // Arrange
        var creOperationMock = _fixture.CreateMany<CreOperation>(3);

        var operationRepository = new Mock<IOperationRepository>(MockBehavior.Strict);
        operationRepository.Setup(r => r.GetOperationsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(creOperationMock);

        // Act
        var operationService = new OperationService(operationRepository.Object);
        var operations = await operationService.GetOperationsAsync("Name", "Pending", "12128179");

        // Assert
        operationRepository.VerifyAll();
        operations.Should().BeEquivalentTo(creOperationMock);
    }

    [Fact]
    public async Task GetOperationsAsync_WithInvalidParam_ShouldThrowArgumentNullException()
    {
        // Arrange
        var creOperationMock = _fixture.CreateMany<CreOperation>(3);

        var operationRepository = new Mock<IOperationRepository>(MockBehavior.Strict);
        operationRepository.Setup(r => r.GetOperationsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(creOperationMock);

        // Act
        var operationService = new OperationService(operationRepository.Object);
        Task operation() => operationService.GetOperationsAsync("Name", "Pending", null!);

        await Assert.ThrowsAsync<ArgumentNullException>(operation);
    }
}
