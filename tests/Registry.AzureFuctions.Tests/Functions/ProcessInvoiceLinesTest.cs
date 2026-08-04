// <copyright file="ProcessInvoiceLinesTest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Exceptions;
using Application.Interfaces;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;
using Registry.AzureFuctions.Functions;

namespace Registry.AzureFunctions.Tests.Functions;

public class ProcessInvoiceLinesTests
{
    private readonly Mock<IInvoicePublicationService> _invoicePublicationServiceMock;
    private readonly Mock<ServiceBusMessageActions> _messageActionsMock;
    private readonly ProcessInvoiceLines _function;

    public ProcessInvoiceLinesTests()
    {
        _invoicePublicationServiceMock = new Mock<IInvoicePublicationService>();
        _messageActionsMock = new Mock<ServiceBusMessageActions>();

        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory
            .Setup(factory => factory.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger<ProcessInvoiceLines>>().Object);

        _function = new ProcessInvoiceLines(loggerFactory.Object, _invoicePublicationServiceMock.Object);
    }

    [Fact]
    public async Task Run_WithMessage_ProcessesPendingLinesAndCompletesMessage()
    {
        // Arrange
        var message = CreateMessage("Invoice_20260803_101530.csv");

        // Act
        await _function.Run(message, _messageActionsMock.Object);

        // Assert
        _invoicePublicationServiceMock.Verify(s => s.ProcessPendingLinesAsync(), Times.Once);
        _messageActionsMock.Verify(a => a.CompleteMessageAsync(message, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Run_WhenProcessingFails_DoesNotCompleteMessage()
    {
        // Arrange
        var message = CreateMessage("Invoice_20260803_101530.csv");
        _invoicePublicationServiceMock
            .Setup(s => s.ProcessPendingLinesAsync())
            .ThrowsAsync(new ServiceBusOperationException("topic unavailable"));

        // Act
        var act = () => _function.Run(message, _messageActionsMock.Object);

        // Assert
        await Assert.ThrowsAsync<ServiceBusOperationException>(act);
        _messageActionsMock.Verify(
            a => a.CompleteMessageAsync(It.IsAny<ServiceBusReceivedMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void Run_TriggerAttribute_ExplicitlyDisablesAutoCompletion()
    {
        // Only named arguments written on the attribute reach the generated metadata (host.json is
        // overridden on the isolated runtime), and a plain bool read defaults to false either way.
        var messageParameter = typeof(ProcessInvoiceLines).GetMethod(nameof(ProcessInvoiceLines.Run))!.GetParameters()[0];
        var trigger = messageParameter.GetCustomAttributesData()
            .Single(a => a.AttributeType == typeof(ServiceBusTriggerAttribute));

        var autoComplete = trigger.NamedArguments
            .SingleOrDefault(n => n.MemberName == nameof(ServiceBusTriggerAttribute.AutoCompleteMessages));

        Assert.NotNull(autoComplete.MemberInfo);
        Assert.Equal(false, autoComplete.TypedValue.Value);
    }

    private static ServiceBusReceivedMessage CreateMessage(string blobName)
    {
        return ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString(blobName));
    }
}
