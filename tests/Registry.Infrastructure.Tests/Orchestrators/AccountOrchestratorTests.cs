// <copyright file="AccountOrchestratorTests.cs" company="Pulse">
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

public class AccountOrchestratorTests
{
    private readonly Mock<ILogger<AccountOrchestrator>> _loggerMock;
    private readonly Mock<INotificationManager> _notificationManagerMock;
    private readonly Mock<IServiceBusMessageFactory> _serviceBusMessageFactoryMock;
    private readonly IOperationService _operationService;
    private readonly IOptions<BackGroundJobOptions> _options;
    private readonly RefContext _context;
    private readonly AccountOrchestrator _orchestrator;

    public AccountOrchestratorTests()
    {
        _loggerMock = new Mock<ILogger<AccountOrchestrator>>();
        _notificationManagerMock = new Mock<INotificationManager>();
        _serviceBusMessageFactoryMock = new Mock<IServiceBusMessageFactory>();
        _options = Options.Create(new BackGroundJobOptions { Chunk = 5 });
        _operationService = Mock.Of<IOperationService>();

        var options = new DbContextOptionsBuilder<RefContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new RefContext(options);

        _orchestrator = new AccountOrchestrator(
            _loggerMock.Object,
            _context,
            _notificationManagerMock.Object,
            _options,
            _serviceBusMessageFactoryMock.Object
            , _operationService
            );
    }

    [Fact]
    public async Task ProcessAccountPublishAsync_ProcessesAccountsCorrectly()
    {
        // Arrange
        var operation = new RegOperationEntity { EntityId = Guid.NewGuid(), Type = "ACCOUNT", Operation = "INSERT", ApprovalStatus = "Approved" };
        var account = new RefAccountEntity { EntityId = Guid.NewGuid(), AccountNumber = "12345", OperationType = "INSERT" };
        _context.RegOperationEntity.Add(operation);
        _context.RefAccountEntity.Add(account);
        await _context.SaveChangesAsync();

        // Act
        await _orchestrator.ProcessAccountPublishAsync("INSERT");

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Send Account event data executed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ), Times.AtLeastOnce);
    }
}
