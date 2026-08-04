// <copyright file="InvoiceEventPublisherTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Exceptions;
using Application.Options;
using Azure.Messaging.ServiceBus;
using FluentAssertions;
using Infrastructure.Providers;
using Moq;
using Pulse.Back.Events.Abstractions;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using Registry.Infrastructure.Managers;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace Registry.Infrastructure.Tests.Providers;

public class InvoiceEventPublisherTests
{
    private readonly Mock<INotificationManager> _notificationManagerMock = new();
    private readonly Mock<IServiceBusMessageFactory> _messageFactoryMock = new();

    [Fact]
    public async Task SendInvoiceLinesBatchEvent_WithConfiguredQueue_SendsBlobNameToQueue()
    {
        // Arrange
        var publisher = CreatePublisher("registry-invoice-lines");

        // Act
        await publisher.SendInvoiceLinesBatchEvent("Invoice_20260730_101530.csv");

        // Assert
        _notificationManagerMock.Verify(
            n => n.SendMessageToQueueAsync("Invoice_20260730_101530.csv", "registry-invoice-lines"),
            Times.Once);
    }

    [Fact]
    public async Task SendInvoiceLinesBatchEvent_WithoutConfiguredQueue_ThrowsServiceBusOperationException()
    {
        // Arrange
        var publisher = CreatePublisher(queueName: null);

        // Act
        var act = () => publisher.SendInvoiceLinesBatchEvent("Invoice_20260730_101530.csv");

        // Assert
        await act.Should().ThrowAsync<ServiceBusOperationException>();
        _notificationManagerMock.Verify(
            n => n.SendMessageToQueueAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task SendInvoiceCreatedEventsAsync_WithEvents_BulkPublishesOneMessagePerEvent()
    {
        // Arrange
        var publisher = CreatePublisher("registry-invoice-lines");
        var wrapped = new List<RegistryInvoiceCreatedEventData>();
        _messageFactoryMock
            .Setup(f => f.CreateMessage(It.IsAny<RegistryInvoiceCreatedEvent>(), It.IsAny<string>()))
            .Callback<BaseEvent<RegistryInvoiceCreatedEventData>, string>((e, _) => wrapped.Add(e.Data))
            .Returns(new ServiceBusMessage());

        var events = new List<RegistryInvoiceCreatedEventData>
        {
            CreateEventData("FA-2024-0001"),
            CreateEventData("FA-2024-0002"),
        };

        // Act
        await publisher.SendInvoiceCreatedEventsAsync(events);

        // Assert
        wrapped.Select(e => e.InvoiceNumber).Should().BeEquivalentTo("FA-2024-0001", "FA-2024-0002");
        _notificationManagerMock.Verify(
            n => n.BulkPublishAsync(It.Is<List<ServiceBusMessage>>(m => m.Count == 2), null),
            Times.Once);
    }

    [Fact]
    public async Task SendInvoiceRemovedEventsAsync_WithEvents_BulkPublishesOneMessagePerEvent()
    {
        // Arrange
        var publisher = CreatePublisher("registry-invoice-lines");
        var wrapped = new List<RegistryInvoiceRemovedEventData>();
        _messageFactoryMock
            .Setup(f => f.CreateMessage(It.IsAny<RegistryInvoiceRemovedEvent>(), It.IsAny<string>()))
            .Callback<BaseEvent<RegistryInvoiceRemovedEventData>, string>((e, _) => wrapped.Add(e.Data))
            .Returns(new ServiceBusMessage());

        var events = new List<RegistryInvoiceRemovedEventData>
        {
            new() { InvoiceNumber = "FA-2024-0001" },
        };

        // Act
        await publisher.SendInvoiceRemovedEventsAsync(events);

        // Assert
        wrapped.Select(e => e.InvoiceNumber).Should().BeEquivalentTo("FA-2024-0001");
        _notificationManagerMock.Verify(
            n => n.BulkPublishAsync(It.Is<List<ServiceBusMessage>>(m => m.Count == 1), null),
            Times.Once);
    }

    private InvoiceEventPublisher CreatePublisher(string? queueName)
    {
        var options = MsOptions.Create(new InvoiceOptions { InvoiceLinesQueueName = queueName! });
        return new InvoiceEventPublisher(_notificationManagerMock.Object, _messageFactoryMock.Object, options);
    }

    private static RegistryInvoiceCreatedEventData CreateEventData(string invoiceNumber)
    {
        return new RegistryInvoiceCreatedEventData
        {
            InvoiceNumber = invoiceNumber,
            AccountNumber = "C000123",
            DocumentPath = "https://docs.pulse.fr/" + invoiceNumber + ".pdf",
            InvoiceDate = new DateTime(2024, 1, 15),
            DepositDate = new DateTime(2026, 8, 1),
            Type = "Facture RYDGE",
            Category = "ADMINISTRATIF",
        };
    }
}
