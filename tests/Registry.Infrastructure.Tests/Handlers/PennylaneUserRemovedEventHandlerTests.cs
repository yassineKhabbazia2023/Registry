// <copyright file="PennylaneUserRemovedEventHandlerTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using AutoFixture;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using Application.Interfaces;
using Application.Models;
using Application.Consts;
using Application.Exceptions;
using Application.Enums;

namespace Infrastructure.Handlers.Tests
{
    public class PennylaneUserRemovedEventHandlerTests
    {
        private readonly IFixture _fixture;

        public PennylaneUserRemovedEventHandlerTests()
        {
            _fixture = new Fixture();
            _fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
                .ToList()
                .ForEach(b => _fixture.Behaviors.Remove(b));
            _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        }

        private PennylaneUserRemovedEventHandler CreateHandler(
            out Mock<IRoleService> roleService,
            out Mock<ILogger<PennylaneUserRemovedEventHandler>> logger)
        {
            roleService = new Mock<IRoleService>(MockBehavior.Strict);
            logger = new Mock<ILogger<PennylaneUserRemovedEventHandler>>(MockBehavior.Loose);
            return new PennylaneUserRemovedEventHandler(roleService.Object, logger.Object);
        }

        [Fact]
        public async Task HandleAsync_ShouldDeleteRoles_WhenMessageIsValid()
        {
            // Arrange
            var handler = CreateHandler(out var roleService, out _);
            var data = _fixture.Build<PennylaneUserRemovedEventData>()
                .With(d => d.CompanyIds, new List<string> { "X1", "Y2" })
                .With(d => d.Email, _fixture.Create<string>())
                .Create();
            var integrationEvent = new PennylaneUserRemovedEvent(data);
            var message = JsonConvert.SerializeObject(integrationEvent);

            roleService
                .Setup(s => s.InsertRolesAsync(
                    It.Is<IEnumerable<RefRoleCsv>>(list =>
                        list.Count() == data.CompanyIds.Count &&
                        list.All(r => r.ContactEmail == data.Email &&
                                      r.Operation == OperationAction.Delete)),DataSources.PENNYLANE.ToString())
                )
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            await handler.HandleAsync(message);

            // Assert
            roleService.VerifyAll();
        }

        [Fact]
        public async Task HandleAsync_ShouldNotCallService_WhenMessageIsNullOrWhitespace()
        {
            // Arrange
            var handler = CreateHandler(out var roleService, out _);
            var message = "   ";

            // Act
            await handler.HandleAsync(message);

            // Assert
            roleService.Verify(
                s => s.InsertRolesAsync(It.IsAny<IEnumerable<RefRoleCsv>>(), null),
                Times.Never);
        }

        [Fact]
        public async Task HandleAsync_ShouldNotCallService_WhenDeserializationFails()
        {
            // Arrange
            var handler = CreateHandler(out var roleService, out _);
            var message = "not a json";

            // Act
            await handler.HandleAsync(message);

            // Assert
            roleService.Verify(
                s => s.InsertRolesAsync(It.IsAny<IEnumerable<RefRoleCsv>>(), null),
                Times.Never);
        }

        [Fact]
        public async Task HandleAsync_ShouldNotCallService_WhenNoCompaniesProvided()
        {
            // Arrange
            var handler = CreateHandler(out var roleService, out _);
            var data = _fixture.Build<PennylaneUserRemovedEventData>()
                .With(d => d.CompanyIds, (List<string>?)null)
                .With(d => d.Email, _fixture.Create<string>())
                .Create();
            var message = JsonConvert.SerializeObject(new PennylaneUserRemovedEvent(data));

            // Act
            await handler.HandleAsync(message);

            // Assert
            roleService.Verify(
                s => s.InsertRolesAsync(It.IsAny<IEnumerable<RefRoleCsv>>(), null),
                Times.Never);
        }

        [Fact]
        public async Task HandleAsync_ShouldHandleDbOperationException_Silently()
        {
            // Arrange
            var handler = CreateHandler(out var roleService, out _);
            var data = _fixture.Build<PennylaneUserRemovedEventData>()
                .With(d => d.CompanyIds, new List<string> { "Z3" })
                .With(d => d.Email, _fixture.Create<string>())
                .Create();
            var message = JsonConvert.SerializeObject(new PennylaneUserRemovedEvent(data));
            var dbEx = new DbOperationException("Failure");

            roleService
                .Setup(s => s.InsertRolesAsync(It.IsAny<IEnumerable<RefRoleCsv>>(), DataSources.PENNYLANE.ToString()))
                .ThrowsAsync(dbEx)
                .Verifiable();

            // Act
            Func<Task> act = async () => await handler.HandleAsync(message);
            await act();

            // Assert
            roleService.VerifyAll();
        }
    }
}
