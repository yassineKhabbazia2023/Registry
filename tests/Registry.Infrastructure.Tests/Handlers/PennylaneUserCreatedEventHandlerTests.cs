// <copyright file="PennylaneUserCreatedEventHandlerTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Exceptions;
using Application.Interfaces;
using Application.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Pulse.Back.Events.Abstractions;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using AutoFixture;
using Moq;
using Application.Enums;

namespace Infrastructure.Handlers.Tests
{
    public class PennylaneUserCreatedEventHandlerTests
    {
        private readonly IFixture _fixture;

        public PennylaneUserCreatedEventHandlerTests()
        {
            _fixture = new Fixture();
            _fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
                .ToList()
                .ForEach(b => _fixture.Behaviors.Remove(b));
            _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        }

        private PennylaneUserCreatedEventHandler CreateHandler(
            out Mock<IContactService> contactService,
            out Mock<IRoleService> roleService,
            out Mock<ILogger<PennylaneUserCreatedEventHandler>> logger)
        {
            contactService = new Mock<IContactService>(MockBehavior.Strict);
            roleService = new Mock<IRoleService>(MockBehavior.Strict);
            logger = new Mock<ILogger<PennylaneUserCreatedEventHandler>>(MockBehavior.Loose);
            return new PennylaneUserCreatedEventHandler(contactService.Object, roleService.Object, logger.Object);
        }

        [Fact]
        public async Task HandleAsync_ShouldInsertContactAndRoles_WhenMessageIsValid()
        {
            // Arrange
            var handler = CreateHandler(out var contactService, out var roleService, out _);
            var data = _fixture.Build<PennylaneUserCreatedEventData>()
                .With(d => d.CompanyIds, new List<string> { "A1", "B2" })
                .With(d => d.Email, _fixture.Create<string>())
                .Create();
            var integrationEvent = new PennylaneUserCreatedEvent(data);
            var message = JsonConvert.SerializeObject(integrationEvent);

            contactService
                .Setup(s => s.InsertContactsAsync(
                    It.Is<IEnumerable<RefContactCsv>>(list =>
                        list.Single().Email == data.Email), DataSources.PENNYLANE.ToString()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            roleService
                .Setup(s => s.InsertRolesAsync(
                    It.Is<IEnumerable<RefRoleCsv>>(list =>
                        list.Count() == data.CompanyIds.Count), DataSources.PENNYLANE.ToString()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            await handler.HandleAsync(message);

            // Assert
            contactService.VerifyAll();
            roleService.VerifyAll();
        }

        [Fact]
        public async Task HandleAsync_ShouldNotCallServices_WhenMessageIsNullOrWhiteSpace()
        {
            // Arrange
            var handler = CreateHandler(out var contactService, out var roleService, out _);
            var msg = "   ";

            // Act
            await handler.HandleAsync(msg);

            // Assert
            contactService.Verify(s => s.InsertContactsAsync(It.IsAny<IEnumerable<RefContactCsv>>(),DataSources.PENNYLANE.ToString()), Times.Never);
            roleService.Verify(s => s.InsertRolesAsync(It.IsAny<IEnumerable<RefRoleCsv>>(), DataSources.PENNYLANE.ToString()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_ShouldNotCallServices_WhenDeserializationFails()
        {
            // Arrange
            var handler = CreateHandler(out var contactService, out var roleService, out _);
            var invalidMessage = "invalid json";

            // Act
            await handler.HandleAsync(invalidMessage);

            // Assert
            contactService.Verify(s => s.InsertContactsAsync(It.IsAny<IEnumerable<RefContactCsv>>(), DataSources.PENNYLANE.ToString()), Times.Never);
            roleService.Verify(s => s.InsertRolesAsync(It.IsAny<IEnumerable<RefRoleCsv>>(), DataSources.PENNYLANE.ToString()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_ShouldNotCallServices_WhenDataIsNullAfterDeserialization()
        {
            // Arrange
            var handler = CreateHandler(out var contactService, out var roleService, out _);
            // serialize an event whose Data property is null
            var evt = new PennylaneUserCreatedEvent((PennylaneUserCreatedEventData?)null);
            var message = JsonConvert.SerializeObject(evt);

            // Act
            await handler.HandleAsync(message);

            // Assert
            contactService.Verify(s =>
                s.InsertContactsAsync(It.IsAny<IEnumerable<RefContactCsv>>(), It.IsAny<string>()),
                Times.Never);
            roleService.Verify(s =>
                s.InsertRolesAsync(It.IsAny<IEnumerable<RefRoleCsv>>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task HandleAsync_ShouldSkipContact_AndPublishRoles_WhenEmailIsWhitespace()
        {
            // Arrange
            var handler = CreateHandler(out var contactService, out var roleService, out _);
            var data = _fixture.Build<PennylaneUserCreatedEventData>()
                .With(d => d.Email, "   ")
                .With(d => d.CompanyIds, new List<string> { "X1" })
                .Create();
            var evt = new PennylaneUserCreatedEvent(data);
            var message = JsonConvert.SerializeObject(evt);

            // since email missing, contactService must not be called
            // but roles still processed:
            roleService
                .Setup(s => s.InsertRolesAsync(
                    It.Is<IEnumerable<RefRoleCsv>>(r => r.Count() == 1),
                    DataSources.PENNYLANE.ToString()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            await handler.HandleAsync(message);

            // Assert
            contactService.Verify(s =>
                s.InsertContactsAsync(It.IsAny<IEnumerable<RefContactCsv>>(), It.IsAny<string>()),
                Times.Never);
            roleService.VerifyAll();
        }

        [Fact]
        public async Task HandleAsync_ShouldCatchAndLog_WhenContactServiceThrows()
        {
            // Arrange
            var handler = CreateHandler(out var contactService, out var roleService, out var logger);
            var data = _fixture.Build<PennylaneUserCreatedEventData>()
                .With(d => d.Email, _fixture.Create<string>())
                .With(d => d.CompanyIds, new List<string> { "C1", "C2" })
                .Create();
            var evt = new PennylaneUserCreatedEvent(data);
            var message = JsonConvert.SerializeObject(evt);

            contactService
                .Setup(s => s.InsertContactsAsync(
                    It.IsAny<IEnumerable<RefContactCsv>>(),
                    DataSources.PENNYLANE.ToString()))
                .ThrowsAsync(new DbOperationException("boom"))
                .Verifiable();

            roleService
                .Setup(s => s.InsertRolesAsync(
                    It.IsAny<IEnumerable<RefRoleCsv>>(),
                    DataSources.PENNYLANE.ToString()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            var ex = await Record.ExceptionAsync(() => handler.HandleAsync(message));

            // Assert
            Assert.Null(ex);
            contactService.VerifyAll();
            roleService.VerifyAll();
        }

        [Fact]
        public async Task HandleAsync_ShouldCatchAndLog_WhenRoleServiceThrows()
        {
            // Arrange
            var handler = CreateHandler(out var contactService, out var roleService, out var logger);
            var data = _fixture.Build<PennylaneUserCreatedEventData>()
                .With(d => d.Email, _fixture.Create<string>())
                .With(d => d.CompanyIds, new List<string> { "R1", "R2" })
                .Create();
            var evt = new PennylaneUserCreatedEvent(data);
            var message = JsonConvert.SerializeObject(evt);

            contactService
                .Setup(s => s.InsertContactsAsync(
                    It.IsAny<IEnumerable<RefContactCsv>>(),
                    DataSources.PENNYLANE.ToString()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            roleService
                .Setup(s => s.InsertRolesAsync(
                    It.IsAny<IEnumerable<RefRoleCsv>>(),
                    DataSources.PENNYLANE.ToString()))
                .ThrowsAsync(new DbOperationException("boom"))
                .Verifiable();

            // Act
            var ex = await Record.ExceptionAsync(() => handler.HandleAsync(message));

            // Assert
            Assert.Null(ex);
            contactService.VerifyAll();
            roleService.VerifyAll();
        }

        [Fact]
        public async Task HandleAsync_ShouldSkipRoles_WhenCompanyIdsIsNullOrEmpty()
        {
            // Arrange
            var handler = CreateHandler(out var contactService, out var roleService, out _);
            var data = _fixture.Build<PennylaneUserCreatedEventData>()
                .With(d => d.Email, _fixture.Create<string>())
                .With(d => d.CompanyIds, new List<string>())
                .Create();
            var evt = new PennylaneUserCreatedEvent(data);
            var message = JsonConvert.SerializeObject(evt);

            contactService
                .Setup(s => s.InsertContactsAsync(
                    It.IsAny<IEnumerable<RefContactCsv>>(),
                    DataSources.PENNYLANE.ToString()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            await handler.HandleAsync(message);

            // Assert
            contactService.VerifyAll();
            roleService.Verify(s =>
                s.InsertRolesAsync(It.IsAny<IEnumerable<RefRoleCsv>>(), It.IsAny<string>()),
                Times.Never);
        }
    }
}


