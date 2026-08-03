// <copyright file="InvoiceEventPublisherTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Exceptions;
using Application.Options;
using FluentAssertions;
using Infrastructure.Providers;
using Moq;
using Registry.Infrastructure.Managers;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace Registry.Infrastructure.Tests.Providers;

public class InvoiceEventPublisherTests
{
    private readonly Mock<INotificationManager> _notificationManagerMock = new();

    [Fact]
    public async Task SendInvoiceLinesBatchEvent_WithConfiguredQueue_SendsBlobNameToQueue()
    {
        // Arrange
        var options = MsOptions.Create(new InvoiceOptions { InvoiceLinesQueueName = "registry-invoice-lines" });
        var publisher = new InvoiceEventPublisher(_notificationManagerMock.Object, options);

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
        var options = MsOptions.Create(new InvoiceOptions());
        var publisher = new InvoiceEventPublisher(_notificationManagerMock.Object, options);

        // Act
        var act = () => publisher.SendInvoiceLinesBatchEvent("Invoice_20260730_101530.csv");

        // Assert
        await act.Should().ThrowAsync<ServiceBusOperationException>();
        _notificationManagerMock.Verify(
            n => n.SendMessageToQueueAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }
}
