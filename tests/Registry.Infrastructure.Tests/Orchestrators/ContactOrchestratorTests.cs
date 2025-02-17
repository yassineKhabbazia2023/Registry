// <copyright file="ContactOrchestratorTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Infrastructure.Orchestrators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Pulse.Back.Events.Abstractions;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities;
using Registry.Infrastructure.Managers;

public class ContactOrchestratorTests
{
    private readonly Mock<ILogger<ContactOrchetrator>> _loggerMock;
    private readonly Mock<INotificationManager> _notificationManagerMock;
    private readonly Mock<IServiceBusMessageFactory> _serviceBusMessageFactoryMock;
    private readonly RefContext _context;
    private readonly IOperationService _operationService;
    private readonly ContactOrchetrator _orchestrator;

    public ContactOrchestratorTests()
    {
        _loggerMock = new Mock<ILogger<ContactOrchetrator>>();
        _notificationManagerMock = new Mock<INotificationManager>();
        _serviceBusMessageFactoryMock = new Mock<IServiceBusMessageFactory>();

        var options = new DbContextOptionsBuilder<RefContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new RefContext(options);

        _operationService = Mock.Of<IOperationService>();
        _orchestrator = new ContactOrchetrator(
            _loggerMock.Object,
            _context,
            _notificationManagerMock.Object,
            _serviceBusMessageFactoryMock.Object,
            _operationService
            );
    }

    [Fact]
    public async Task ProcessContactPublishAsync_ProcessesContactsCorrectly()
    {

        // Arrange

        var contact = new RefContactEntity { EntityId = Guid.NewGuid(), FirstName = "John", LastName = "Doe", Email = "john.doe@example.com", OperationType = "INSERT" };
        var operation = new RegOperationEntity { EntityId = contact.EntityId, Type = "CONTACT", Operation = "INSERT", ApprovalStatus = "APPROVED" };
        _context.RegOperationEntity.Add(operation);
        _context.RefContactEntity.Add(contact);
        await _context.SaveChangesAsync();

        // Act
        await _orchestrator.ProcessContactPublishAsync("insert");

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Send Contact event data started")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ), Times.AtLeastOnce);
    }
}
