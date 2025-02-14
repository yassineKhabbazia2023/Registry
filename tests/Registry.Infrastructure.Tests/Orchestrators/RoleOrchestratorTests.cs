// <copyright file="RoleOrchestratorTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Options;
using Infrastructure.Orchestrators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Pulse.Back.Events.Abstractions;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities;
using Registry.Infrastructure.Managers;

public class RoleOrchestratorTests
{
    private readonly Mock<ILogger<RoleOrchestrator>> _loggerMock;
    private readonly Mock<INotificationManager> _notificationManagerMock;
    private readonly Mock<IServiceBusMessageFactory> _serviceBusMessageFactoryMock;
    private readonly RefContext _context;
    private readonly RoleOrchestrator _orchestrator;
    private readonly IOperationService _operationService;

    public RoleOrchestratorTests()
    {
        _loggerMock = new Mock<ILogger<RoleOrchestrator>>();
        _notificationManagerMock = new Mock<INotificationManager>();
        _serviceBusMessageFactoryMock = new Mock<IServiceBusMessageFactory>();

        var options = new DbContextOptionsBuilder<RefContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new RefContext(options);
        _operationService = Mock.Of<IOperationService>();

        _orchestrator = new RoleOrchestrator(
            _loggerMock.Object,
            _context,
            _notificationManagerMock.Object,
            Options.Create(new BackGroundJobOptions { Chunk = 5 }),
            _serviceBusMessageFactoryMock.Object
            ,_operationService
            );
    }

    [Fact]
    public async Task ProcessRolePublishAsync_ProcessesRolesCorrectly()
    {
        // Arrange
        var role = new RefRoleEntity { EntityId = Guid.NewGuid(), AccountNumber = "12345", ContactEmail = "test@example.com", OperationType = "INSERT" };
        var operation = new RegOperationEntity { EntityId = role.EntityId, Type = "ROLE", Operation = "insert", ApprovalStatus = "Approved" };

        _context.RegOperationEntity.Add(operation);
        _context.RefRoleEntity.Add(role);
        await _context.SaveChangesAsync();

        // Act
        await _orchestrator.ProcessRolePublishAsync("insert");

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Send Role event data executed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ), Times.AtLeastOnce);
    }
}
