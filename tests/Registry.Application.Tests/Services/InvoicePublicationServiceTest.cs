// <copyright file="InvoicePublicationServiceTest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Exceptions;
using Application.Interfaces;
using Application.Options;
using Application.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using Pulse.Registry.Domain.Entities;

namespace Registry.Application.Tests.Services;

public class InvoicePublicationServiceTest
{
    private const string KnownAccount = "C000123";

    private readonly Mock<IInvoiceRepository> _invoiceRepositoryMock;
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly Mock<IInvoiceEventPublisher> _invoiceEventPublisherMock;
    private readonly BackGroundJobOptions _options;
    private readonly InvoicePublicationService _sut;

    public InvoicePublicationServiceTest()
    {
        _invoiceRepositoryMock = new Mock<IInvoiceRepository>(MockBehavior.Strict);
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _invoiceEventPublisherMock = new Mock<IInvoiceEventPublisher>();
        _options = new BackGroundJobOptions { Chunk = 1000 };
        var logger = new Mock<ILogger<InvoicePublicationService>>();

        _sut = new InvoicePublicationService(
            _invoiceRepositoryMock.Object,
            _accountRepositoryMock.Object,
            _invoiceEventPublisherMock.Object,
            Options.Create(_options),
            logger.Object);
    }

    [Fact]
    public async Task ProcessPendingLinesAsync_WithoutPendingLines_PublishesNothing()
    {
        // Arrange
        SetupPendingLines();

        // Act
        await _sut.ProcessPendingLinesAsync();

        // Assert
        _invoiceEventPublisherMock.Verify(x => x.SendInvoiceCreatedEventsAsync(It.IsAny<List<RegistryInvoiceCreatedEventData>>()), Times.Never);
        _invoiceEventPublisherMock.Verify(x => x.SendInvoiceRemovedEventsAsync(It.IsAny<List<RegistryInvoiceRemovedEventData>>()), Times.Never);
        _invoiceRepositoryMock.Verify(x => x.UpdateStatusAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPendingLinesAsync_WithValidInsertLine_PublishesCreatedEventAndMarksProcessed()
    {
        // Arrange
        var line = CreateLine(1, OperationAction.Insert);
        SetupPendingLines(line);
        SetupExistingAccounts(KnownAccount);
        SetupStatusUpdate();

        List<RegistryInvoiceCreatedEventData>? published = null;
        _invoiceEventPublisherMock
            .Setup(x => x.SendInvoiceCreatedEventsAsync(It.IsAny<List<RegistryInvoiceCreatedEventData>>()))
            .Callback<List<RegistryInvoiceCreatedEventData>>(e => published = e)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.ProcessPendingLinesAsync();

        // Assert
        published.Should().NotBeNull().And.HaveCount(1);
        var eventData = published![0];
        eventData.InvoiceNumber.Should().Be(line.InvoiceNumber);
        eventData.AccountNumber.Should().Be(line.AccountNumber);
        eventData.DocumentPath.Should().Be(line.DocumentPath);
        eventData.InvoiceDate.Should().Be(line.InvoiceDate);
        eventData.DepositDate.Should().Be(line.CreatedOn);
        eventData.Type.Should().Be(line.Type);
        eventData.Category.Should().Be("ADMINISTRATIF");
        _invoiceRepositoryMock.Verify(
            x => x.UpdateStatusAsync(It.Is<IEnumerable<int>>(ids => ids.SequenceEqual(new[] { 1 })), InvoiceStatus.Processed),
            Times.Once);
    }

    [Fact]
    public async Task ProcessPendingLinesAsync_WithValidDeleteLine_PublishesRemovedEventAndMarksProcessed()
    {
        // Arrange
        var line = CreateLine(4, OperationAction.Delete);
        SetupPendingLines(line);
        SetupExistingAccounts(KnownAccount);
        SetupStatusUpdate();

        List<RegistryInvoiceRemovedEventData>? published = null;
        _invoiceEventPublisherMock
            .Setup(x => x.SendInvoiceRemovedEventsAsync(It.IsAny<List<RegistryInvoiceRemovedEventData>>()))
            .Callback<List<RegistryInvoiceRemovedEventData>>(e => published = e)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.ProcessPendingLinesAsync();

        // Assert
        published.Should().NotBeNull().And.HaveCount(1);
        published![0].InvoiceNumber.Should().Be(line.InvoiceNumber);
        _invoiceEventPublisherMock.Verify(x => x.SendInvoiceCreatedEventsAsync(It.IsAny<List<RegistryInvoiceCreatedEventData>>()), Times.Never);
        _invoiceRepositoryMock.Verify(
            x => x.UpdateStatusAsync(It.Is<IEnumerable<int>>(ids => ids.SequenceEqual(new[] { 4 })), InvoiceStatus.Processed),
            Times.Once);
    }

    [Fact]
    public async Task ProcessPendingLinesAsync_WithUnknownAccount_RejectsLineWithoutEvent()
    {
        // Arrange
        var line = CreateLine(7, OperationAction.Insert, accountNumber: "C999999");
        SetupPendingLines(line);
        SetupExistingAccounts(KnownAccount);
        SetupStatusUpdate();

        // Act
        await _sut.ProcessPendingLinesAsync();

        // Assert
        _invoiceEventPublisherMock.Verify(x => x.SendInvoiceCreatedEventsAsync(It.IsAny<List<RegistryInvoiceCreatedEventData>>()), Times.Never);
        _invoiceEventPublisherMock.Verify(x => x.SendInvoiceRemovedEventsAsync(It.IsAny<List<RegistryInvoiceRemovedEventData>>()), Times.Never);
        _invoiceRepositoryMock.Verify(
            x => x.UpdateStatusAsync(It.Is<IEnumerable<int>>(ids => ids.SequenceEqual(new[] { 7 })), InvoiceStatus.Rejected),
            Times.Once);
    }

    [Fact]
    public async Task ProcessPendingLinesAsync_WithAccountCasingDifference_StillMatches()
    {
        // Arrange
        var line = CreateLine(2, OperationAction.Insert, accountNumber: "c000123");
        SetupPendingLines(line);
        SetupExistingAccounts(KnownAccount);
        SetupStatusUpdate();
        SetupPublishers();

        // Act
        await _sut.ProcessPendingLinesAsync();

        // Assert
        _invoiceRepositoryMock.Verify(
            x => x.UpdateStatusAsync(It.Is<IEnumerable<int>>(ids => ids.SequenceEqual(new[] { 2 })), InvoiceStatus.Processed),
            Times.Once);
    }

    [Fact]
    public async Task ProcessPendingLinesAsync_WithMixedLines_SplitsStatusesAndEvents()
    {
        // Arrange
        var insertLine = CreateLine(1, OperationAction.Insert);
        var deleteLine = CreateLine(2, OperationAction.Delete);
        var unknownAccountLine = CreateLine(3, OperationAction.Insert, accountNumber: "C999999");
        SetupPendingLines(insertLine, deleteLine, unknownAccountLine);
        SetupExistingAccounts(KnownAccount);
        SetupStatusUpdate();
        SetupPublishers();

        // Act
        await _sut.ProcessPendingLinesAsync();

        // Assert
        _invoiceEventPublisherMock.Verify(
            x => x.SendInvoiceCreatedEventsAsync(It.Is<List<RegistryInvoiceCreatedEventData>>(e => e.Count == 1)),
            Times.Once);
        _invoiceEventPublisherMock.Verify(
            x => x.SendInvoiceRemovedEventsAsync(It.Is<List<RegistryInvoiceRemovedEventData>>(e => e.Count == 1)),
            Times.Once);
        _invoiceRepositoryMock.Verify(
            x => x.UpdateStatusAsync(It.Is<IEnumerable<int>>(ids => ids.SequenceEqual(new[] { 1, 2 })), InvoiceStatus.Processed),
            Times.Once);
        _invoiceRepositoryMock.Verify(
            x => x.UpdateStatusAsync(It.Is<IEnumerable<int>>(ids => ids.SequenceEqual(new[] { 3 })), InvoiceStatus.Rejected),
            Times.Once);
    }

    [Fact]
    public async Task ProcessPendingLinesAsync_WithFullChunk_ProcessesNextChunk()
    {
        // Arrange
        _options.Chunk = 2;
        var firstChunk = new List<InvoiceEntity> { CreateLine(1, OperationAction.Insert), CreateLine(2, OperationAction.Insert) };
        var secondChunk = new List<InvoiceEntity> { CreateLine(3, OperationAction.Insert) };
        _invoiceRepositoryMock
            .SetupSequence(x => x.GetByStatusAsync(InvoiceStatus.Pending, 2))
            .ReturnsAsync(firstChunk)
            .ReturnsAsync(secondChunk)
            .ReturnsAsync([]);
        SetupExistingAccounts(KnownAccount);
        SetupStatusUpdate();
        SetupPublishers();

        // Act
        await _sut.ProcessPendingLinesAsync();

        // Assert
        _invoiceRepositoryMock.Verify(x => x.GetByStatusAsync(InvoiceStatus.Pending, 2), Times.Exactly(3));
        _invoiceEventPublisherMock.Verify(x => x.SendInvoiceCreatedEventsAsync(It.IsAny<List<RegistryInvoiceCreatedEventData>>()), Times.Exactly(2));
        _invoiceRepositoryMock.Verify(x => x.UpdateStatusAsync(It.IsAny<IEnumerable<int>>(), InvoiceStatus.Processed), Times.Exactly(2));
    }

    [Fact]
    public async Task ProcessPendingLinesAsync_WhenPublishFails_ThrowsWithoutStatusUpdate()
    {
        // Arrange
        SetupPendingLines(CreateLine(1, OperationAction.Insert));
        SetupExistingAccounts(KnownAccount);
        _invoiceEventPublisherMock
            .Setup(x => x.SendInvoiceCreatedEventsAsync(It.IsAny<List<RegistryInvoiceCreatedEventData>>()))
            .ThrowsAsync(new ServiceBusOperationException("queue unavailable"));

        // Act
        var act = () => _sut.ProcessPendingLinesAsync();

        // Assert
        await act.Should().ThrowAsync<ServiceBusOperationException>();
        _invoiceRepositoryMock.Verify(x => x.UpdateStatusAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<string>()), Times.Never);
    }

    private void SetupPendingLines(params InvoiceEntity[] lines)
    {
        _invoiceRepositoryMock
            .SetupSequence(x => x.GetByStatusAsync(InvoiceStatus.Pending, _options.Chunk))
            .ReturnsAsync(lines.ToList())
            .ReturnsAsync([]);
    }

    private void SetupExistingAccounts(params string[] accountNumbers)
    {
        _accountRepositoryMock
            .Setup(x => x.GetExistingAccountNumbersAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(accountNumbers.ToList());
    }

    private void SetupStatusUpdate()
    {
        _invoiceRepositoryMock
            .Setup(x => x.UpdateStatusAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
    }

    private void SetupPublishers()
    {
        _invoiceEventPublisherMock
            .Setup(x => x.SendInvoiceCreatedEventsAsync(It.IsAny<List<RegistryInvoiceCreatedEventData>>()))
            .Returns(Task.CompletedTask);
        _invoiceEventPublisherMock
            .Setup(x => x.SendInvoiceRemovedEventsAsync(It.IsAny<List<RegistryInvoiceRemovedEventData>>()))
            .Returns(Task.CompletedTask);
    }

    private static InvoiceEntity CreateLine(int invoiceId, string operation, string accountNumber = KnownAccount)
    {
        return new InvoiceEntity
        {
            InvoiceId = invoiceId,
            AccountNumber = accountNumber,
            InvoiceNumber = $"FA-2024-{invoiceId:D4}",
            InvoiceDate = new DateTime(2024, 1, 15),
            DocumentPath = $"https://docs.pulse.fr/FA-2024-{invoiceId:D4}.pdf",
            Type = "Facture RYDGE",
            Operation = operation,
            Status = InvoiceStatus.Pending,
            CreatedOn = new DateTime(2026, 8, 1, 10, 30, 0, DateTimeKind.Utc),
        };
    }
}
