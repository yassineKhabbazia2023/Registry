// <copyright file="RemoveRoleFromPulseTest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using System.Text;
using Azure.Messaging.ServiceBus;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using Pulse.Back.Events.IntegrationEvents;
using ContactRegistry.Infrastructure.Tests.Utils;
using ContactRegistry.AzureFuctions.Functions;
using Domain.Entities;
using Infrastructure.Context;

namespace ContactRegistry.AzureFuctions.Tests.Functions
{
    /// <summary>
    /// RemoveRoleFromPulseTest.
    /// </summary>
    public class RemoveRoleFromPulseTest
    {
        /// <summary>
        /// RunValidMessageRevokeContacttFromDatabase.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task RunValidMessageRevokeContacttFromDatabase()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<RemoveRoleFromPulse>>();

            var eventData = new RoleDeletedEventData
            {
                ContactId = 1,
                AccountId = 1,
                ContactGlobalUniqueId = Guid.NewGuid(),
                AccountGlobalUniqueId = Guid.NewGuid(),
            };
            var revokeAccountEvent = new RoleDeletedEvent(eventData);

            var messageBody = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(revokeAccountEvent));

            var receivedMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(new BinaryData(messageBody));

            var messageActionsMock = new Mock<ServiceBusMessageActions>();

            using var context = DbContextMockExtensions.CreateInMemoryDbContext();
            var roleCre = new CreRole()
            {
                AccountId = eventData.AccountGlobalUniqueId,
                ContactId = eventData.ContactGlobalUniqueId,
            };
            context.CreRoles.Add(roleCre);
            await context.SaveChangesAsync();

            var dbContextFactory = new Mock<IDbContextFactory<ApplicationDbContext>>();
            dbContextFactory.Setup(d => d.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(context);

            // Act
            var function = new RemoveRoleFromPulse(loggerMock.Object, dbContextFactory.Object);
            await function.Run(receivedMessage, messageActionsMock.Object);

            // Assert
            messageActionsMock.Verify(ma => ma.CompleteMessageAsync(receivedMessage, default), Times.Once);
        }

        /// <summary>
        /// RunUnValidMessageRevokeContactFromDatabase.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task RunUnValidMessageRevokeContactFromDatabase()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<RemoveContactFromPulse>>();

            var receivedMessage = ServiceBusModelFactory.ServiceBusReceivedMessage();

            var messageActionsMock = new Mock<ServiceBusMessageActions>();
            messageActionsMock.Setup(m =>
            m.DeadLetterMessageAsync(receivedMessage, It.IsAny<Dictionary<string, object>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            using var context = DbContextMockExtensions.CreateInMemoryDbContext();
            await context.SaveChangesAsync();

            var dbContextFactory = new Mock<IDbContextFactory<ApplicationDbContext>>();
            dbContextFactory.Setup(d => d.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(context);

            // Act
            var function = new RemoveContactFromPulse(loggerMock.Object, dbContextFactory.Object);
            await function.Run(receivedMessage, messageActionsMock.Object);

            // Assert
            messageActionsMock.Verify(m => m.DeadLetterMessageAsync(receivedMessage, It.IsAny<Dictionary<string, object>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            messageActionsMock.Verify(ma => ma.CompleteMessageAsync(receivedMessage, default), Times.Never);
        }

        /// <summary>
        /// Run_WhenAccountNotFound.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task Run_WhenContactNotFound()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<RemoveContactFromPulse>>();

            var eventData = new ContactRemovedEventData
            {
                ContactId = 1,
                ContactGlobalUniqueId = Guid.NewGuid(),
            };
            var revokeAccountEvent = new ContactRemovedEvent(eventData);

            var messageBody = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(revokeAccountEvent));

            var receivedMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(new BinaryData(messageBody));

            var messageActionsMock = new Mock<ServiceBusMessageActions>();

            using var context = DbContextMockExtensions.CreateInMemoryDbContext();
            await context.SaveChangesAsync();

            var dbContextFactory = new Mock<IDbContextFactory<ApplicationDbContext>>();
            dbContextFactory.Setup(d => d.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(context);

            // Act
            var function = new RemoveContactFromPulse(loggerMock.Object, dbContextFactory.Object);
            await function.Run(receivedMessage, messageActionsMock.Object);

            // Assert
            messageActionsMock.Verify(m => m.DeadLetterMessageAsync(receivedMessage, It.IsAny<Dictionary<string, object>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            messageActionsMock.Verify(ma => ma.CompleteMessageAsync(receivedMessage, default), Times.Never);
        }

        /// <summary>
        /// RunValidMessageRevokeContacttFromDatabase.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task Run_ArgumentNullException()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<RemoveContactFromPulse>>();

            var eventData = new RoleDeletedEventData
            {
                ContactId = 1,
                AccountId = 1,
            };
            var revokeAccountEvent = new RoleDeletedEvent(eventData);

            var messageBody = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(revokeAccountEvent));

            var receivedMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(new BinaryData(messageBody));

            var messageActionsMock = new Mock<ServiceBusMessageActions>();

            using var context = DbContextMockExtensions.CreateInMemoryDbContext();
            await context.SaveChangesAsync();

            var dbContextFactory = new Mock<IDbContextFactory<ApplicationDbContext>>();
            dbContextFactory.Setup(d => d.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(context);

            // Act
            var function = new RemoveContactFromPulse(loggerMock.Object, dbContextFactory.Object);
            Func<Task> act = async () => await function.Run(receivedMessage, null);

            // Assert
            await act.Should().ThrowExactlyAsync<ArgumentNullException>()
                                .WithMessage("Value cannot be null. (Parameter 'messageActions')");
        }
    }
}
