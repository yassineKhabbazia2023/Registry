// <copyright file="PennylaneUserRemovedEventHandlerTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using AutoFixture;
using Application.Consts;
using Application.Exceptions;
using Application.Interfaces;
using Application.Models;
using Application.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Infrastructure.Handlers.Tests
{
    public class PennylaneUserRemovedEventHandlerTests
    {
        private readonly IFixture _fixture;

        public PennylaneUserRemovedEventHandlerTests()
        {
            _fixture = new Fixture();
            _fixture.Behaviors
                .OfType<ThrowingRecursionBehavior>()
                .ToList()
                .ForEach(b => _fixture.Behaviors.Remove(b));
            _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        }

        private PennylaneUserRemovedEventHandler CreateHandler(
            out Mock<IRoleService> roleService,
            out Mock<ILogger<PennylaneUserRemovedEventHandler>> logger,
            out Mock<IRoleOrchestrator> roleOrchestrator)
        {
            roleService = new Mock<IRoleService>(MockBehavior.Strict);
            logger = new Mock<ILogger<PennylaneUserRemovedEventHandler>>(MockBehavior.Loose);
            roleOrchestrator = new Mock<IRoleOrchestrator>(MockBehavior.Strict);

            // default synchronisation setups
            roleService
                .Setup(s => s.CreateValidRolesOperationsAsync())
                .ReturnsAsync((IList<bool>)new List<bool>())
                .Verifiable();
            roleOrchestrator
                .Setup(o => o.ProcessRolePublishAsync(It.IsAny<string>(), It.IsAny<bool?>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            return new PennylaneUserRemovedEventHandler(
                roleService.Object,
                logger.Object,
                roleOrchestrator.Object);
        }

        [Fact]
        public async Task HandleAsync_ShouldDeleteRolesAndSynchronize_WhenMessageIsValid()
        {
            // Arrange
            var handler = CreateHandler(
                out var roleService,
                out var _logger,
                out var roleOrchestrator);

            var data = _fixture.Build<PennylaneUserRemovedEventData>()
                .With(d => d.CompanyIds, new List<string> { "X1", "Y2" })
                .With(d => d.Email, _fixture.Create<string>())
                .Create();

            var integrationEvent = new PennylaneUserRemovedEvent(data);
            var message = JsonConvert.SerializeObject(integrationEvent);

            // deletion call
            roleService
                .Setup(s => s.InsertRolesAsync(
                    It.Is<IEnumerable<RefRoleCsv>>(list =>
                        list.Count() == data.CompanyIds.Count &&
                        list.All(r => r.ContactEmail == data.Email &&
                                      r.Operation == OperationAction.Delete)),
                    DataSources.PENNYLANE.ToString()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            await handler.HandleAsync(message);

            // Assert
            // deletion
            roleService.Verify(s => s.InsertRolesAsync(
                It.IsAny<IEnumerable<RefRoleCsv>>(),
                DataSources.PENNYLANE.ToString()), Times.Once);

            // synchronization
            roleService.Verify(s => s.CreateValidRolesOperationsAsync(), Times.Once);
            roleOrchestrator.Verify(o => o.ProcessRolePublishAsync(
                It.Is<string>(op => op == OperationAction.Delete),
                It.Is<bool?>(b => b == true)), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_ShouldNotCallService_WhenMessageIsNullOrWhitespace()
        {
            // Arrange
            var handler = CreateHandler(
                out var roleService,
                out var _logger,
                out var _ro);

            // Act
            await handler.HandleAsync("   ");

            // Assert
            roleService.Verify(
                s => s.InsertRolesAsync(It.IsAny<IEnumerable<RefRoleCsv>>(), It.IsAny<string>()),
                Times.Never);
            roleService.Verify(
                s => s.CreateValidRolesOperationsAsync(),
                Times.Never);
        }

        [Fact]
        public async Task HandleAsync_ShouldNotCallService_WhenDeserializationFails()
        {
            // Arrange
            var handler = CreateHandler(
                out var roleService,
                out var _logger,
                out var _ro);

            // Act
            await handler.HandleAsync("not a json");

            // Assert
            roleService.Verify(
                s => s.InsertRolesAsync(It.IsAny<IEnumerable<RefRoleCsv>>(), It.IsAny<string>()),
                Times.Never);
            roleService.Verify(
                s => s.CreateValidRolesOperationsAsync(),
                Times.Never);
        }

        [Fact]
        public async Task HandleAsync_ShouldNotCallService_WhenNoCompaniesProvided()
        {
            // Arrange
            var handler = CreateHandler(
                out var roleService,
                out var _logger,
                out var _ro);

            var data = _fixture.Build<PennylaneUserRemovedEventData>()
                .With(d => d.CompanyIds, (List<string>?)null)
                .With(d => d.Email, _fixture.Create<string>())
                .Create();
            var evt = new PennylaneUserRemovedEvent(data);
            var message = JsonConvert.SerializeObject(evt);

            // Act
            await handler.HandleAsync(message);

            // Assert
            roleService.Verify(
                s => s.InsertRolesAsync(It.IsAny<IEnumerable<RefRoleCsv>>(), It.IsAny<string>()),
                Times.Never);
            roleService.Verify(
                s => s.CreateValidRolesOperationsAsync(),
                Times.Never);
        }

        [Fact]
        public async Task HandleAsync_ShouldHandleDbOperationException_Silently()
        {
            // Arrange
            var handler = CreateHandler(
                out var roleService,
                out var _logger,
                out var _ro);

            var data = _fixture.Build<PennylaneUserRemovedEventData>()
                .With(d => d.CompanyIds, new List<string> { "Z3" })
                .With(d => d.Email, _fixture.Create<string>())
                .Create();
            var evt = new PennylaneUserRemovedEvent(data);
            var message = JsonConvert.SerializeObject(evt);

            roleService
                .Setup(s => s.InsertRolesAsync(
                    It.IsAny<IEnumerable<RefRoleCsv>>(),
                    DataSources.PENNYLANE.ToString()))
                .ThrowsAsync(new DbOperationException("Failure"))
                .Verifiable();

            // Act
            var ex = await Record.ExceptionAsync(() => handler.HandleAsync(message));

            // Assert
            Assert.Null(ex);

            // We still attempted the delete call:
            roleService.Verify(s =>
                s.InsertRolesAsync(
                    It.IsAny<IEnumerable<RefRoleCsv>>(),
                    DataSources.PENNYLANE.ToString()),
                Times.Once);

            // But on failure we do NOT synchronize:
            roleService.Verify(s =>
                s.CreateValidRolesOperationsAsync(),
                Times.Never);

            _ro.Verify(o =>
                o.ProcessRolePublishAsync(
                    It.Is<string>(op => op == OperationAction.Delete),
                    It.Is<bool?>(flag => flag == true)),
                Times.Never);
        }

    }
}
