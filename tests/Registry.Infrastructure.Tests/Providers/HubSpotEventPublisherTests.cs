// <copyright file="HubSpotEventPublisherTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models.Contacts;
using Application.Requests;
using Infrastructure.Providers;
using Microsoft.Extensions.Logging;
using Moq;
using Pulse.Back.Events.Abstractions;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using Pulse.Registry.Domain.Entities.Accounts;
using Registry.Infrastructure.Managers;

namespace Registry.Infrastructure.Tests.Providers;

public class HubSpotEventPublisherTests
{
    private readonly Mock<INotificationManager> _notificationManagerMock;
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly Mock<IContactRepository> _contactRepositoryMock;
    private readonly Mock<TimeProvider> _timeProviderMock;
    private readonly Mock<ILogger<HubSpotEventPublisher>> _loggerMock;
    private readonly HubSpotEventPublisher _publisher;
    private static readonly DateTimeOffset FixedUtcNow = new(2025, 1, 15, 10, 30, 0, TimeSpan.Zero);

    public HubSpotEventPublisherTests()
    {
        _notificationManagerMock = new Mock<INotificationManager>();
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _contactRepositoryMock = new Mock<IContactRepository>();
        _timeProviderMock = new Mock<TimeProvider>();
        _loggerMock = new Mock<ILogger<HubSpotEventPublisher>>();
        _timeProviderMock.Setup(tp => tp.GetUtcNow()).Returns(FixedUtcNow);
        _publisher = new HubSpotEventPublisher(
            _notificationManagerMock.Object,
            _accountRepositoryMock.Object,
            _contactRepositoryMock.Object,
            _timeProviderMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task NotifyDematerializationCreatedAsync_Should_PublishHistoryCreatedEvent_WithCorrectData()
    {
        // Arrange
        var accountNumber = "ACC-2025-001847";
        var vaultEmail = "jean.dupont@gmail.com";
        var request = new HubSpotSubmissionInputRequest
        {
            DematerializationEmail = "facturation@test.fr",
            FirstName = "Sarah",
            LastName = "TATA",
            VaultEmail = vaultEmail,
            RequesterEmail = "sarah.tata@gmail.com"
        };

        var accountEntity = new AccountEntity
        {
            AccountId = 123,
            AccountNumber = accountNumber,
            LegalName = "Ma Société"
        };

        var vaultContact = new Contact
        {
            ContactId = 456,
            Email = vaultEmail,
            FirstName = "Jean",
            LastName = "Dupont"
        };

        _accountRepositoryMock
            .Setup(r => r.GetAccountByNumberOrIdAsync(accountNumber))
            .ReturnsAsync(accountEntity);

        _contactRepositoryMock
            .Setup(r => r.GetContactByEmailOrIdAsync(vaultEmail, null))
            .ReturnsAsync(vaultContact);

        HistoryCreatedEvent? capturedEvent = null;
        _notificationManagerMock
            .Setup(n => n.PublishAsync(It.IsAny<BaseEvent<HistoryCreatedEventData>>(), It.IsAny<string>()))
            .Callback<BaseEvent<HistoryCreatedEventData>, string>((e, _) => capturedEvent = e as HistoryCreatedEvent)
            .Returns(Task.CompletedTask);

        // Act
        await _publisher.NotifyDematerializationCreatedAsync(accountNumber, request);

        // Assert
        _notificationManagerMock.Verify(n => n.PublishAsync(It.IsAny<BaseEvent<HistoryCreatedEventData>>(), It.IsAny<string>()), Times.Once);

        Assert.NotNull(capturedEvent);
        Assert.Equal(FixedUtcNow.UtcDateTime, capturedEvent!.Data.CreationDate);
        Assert.Equal("FAC-MAT-CREA", capturedEvent.Data.Action!.Code);
        Assert.Equal("TATA Sarah", capturedEvent.Data.User!.Name);
        Assert.Equal("sarah.tata@gmail.com", capturedEvent.Data.User.Email);
        Assert.Equal("Customer", capturedEvent.Data.User.UserType);
        Assert.Equal(string.Empty, capturedEvent.Data.TargetUser!.Name);
        Assert.Equal("facturation@test.fr", capturedEvent.Data.TargetUser.Email);
        Assert.Equal("Customer", capturedEvent.Data.TargetUser.UserType);
        Assert.Equal(123, capturedEvent.Data.Account!.AccountId);
        Assert.Equal(accountNumber, capturedEvent.Data.Account.AccountNumber);
        Assert.Equal("Ma Société", capturedEvent.Data.Account.LegalName);
        Assert.Contains("Jean Dupont", capturedEvent.Data.Details);
        Assert.Contains(vaultEmail, capturedEvent.Data.Details);
    }

    [Fact]
    public async Task NotifyDematerializationCreatedAsync_Should_HandleNullAccountNumber()
    {
        // Arrange
        var request = new HubSpotSubmissionInputRequest
        {
            DematerializationEmail = "facturation@test.fr",
            FirstName = "Sarah",
            LastName = "TATA",
            VaultEmail = "vault@test.fr",
            RequesterEmail = "sarah.tata@gmail.com"
        };

        HistoryCreatedEvent? capturedEvent = null;
        _notificationManagerMock
            .Setup(n => n.PublishAsync(It.IsAny<BaseEvent<HistoryCreatedEventData>>(), It.IsAny<string>()))
            .Callback<BaseEvent<HistoryCreatedEventData>, string>((e, _) => capturedEvent = e as HistoryCreatedEvent)
            .Returns(Task.CompletedTask);

        // Act
        await _publisher.NotifyDematerializationCreatedAsync(null, request);

        // Assert
        _notificationManagerMock.Verify(n => n.PublishAsync(It.IsAny<BaseEvent<HistoryCreatedEventData>>(), It.IsAny<string>()), Times.Once);
        Assert.NotNull(capturedEvent);
        Assert.Null(capturedEvent!.Data.Account);
        _accountRepositoryMock.Verify(r => r.GetAccountByNumberOrIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task NotifyDematerializationCreatedAsync_Should_HandleAccountNotFound()
    {
        // Arrange
        var accountNumber = "ACC-NOT-FOUND";
        var request = new HubSpotSubmissionInputRequest
        {
            DematerializationEmail = "facturation@test.fr",
            FirstName = "Sarah",
            LastName = "TATA",
            VaultEmail = "vault@test.fr",
            RequesterEmail = "sarah.tata@gmail.com"
        };

        _accountRepositoryMock
            .Setup(r => r.GetAccountByNumberOrIdAsync(accountNumber))
            .ReturnsAsync((AccountEntity?)null);

        HistoryCreatedEvent? capturedEvent = null;
        _notificationManagerMock
            .Setup(n => n.PublishAsync(It.IsAny<BaseEvent<HistoryCreatedEventData>>(), It.IsAny<string>()))
            .Callback<BaseEvent<HistoryCreatedEventData>, string>((e, _) => capturedEvent = e as HistoryCreatedEvent)
            .Returns(Task.CompletedTask);

        // Act
        await _publisher.NotifyDematerializationCreatedAsync(accountNumber, request);

        // Assert
        _notificationManagerMock.Verify(n => n.PublishAsync(It.IsAny<BaseEvent<HistoryCreatedEventData>>(), It.IsAny<string>()), Times.Once);
        Assert.NotNull(capturedEvent);
        Assert.Null(capturedEvent!.Data.Account);
    }

    [Fact]
    public async Task NotifyDematerializationCreatedAsync_Should_ThrowOnNullRequest()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _publisher.NotifyDematerializationCreatedAsync("ACC-001", null!));
    }
}
