// <copyright file="AddContactFromPulseTest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using System.Text;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using FluentAssertions;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using Pulse.Back.Events.IntegrationEvents;
using ContactRegistry.Infrastructure.Tests.Utils;
using ContactRegistry.AzureFuctions.Functions;
using Infrastructure.Context;

namespace ContactRegistry.AzureFuctions.Tests.Functions
{
    /// <summary>
    /// AddContactFromPulseTest.
    /// </summary>
    public class AddContactFromPulseTest
    {
        /// <summary>
        /// RunValidMessageAddsContactsToDatabase.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task RunValidMessageAddsContactsToDatabase()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<AddContactFromPulse>>();

            var eventData = new ContactStateEventData
            {
                ContactId = 1,
                FirstName = "FirstName",
                LastName = "LastName",
                Email = "testemail",
                Type = "typeName",
                ContactGlobalUniqueId = Guid.NewGuid(),
                MobilePhone = "1123",
                LandPhone = "112345"
            };
            var addAccountEvent = new ContactCreatedEvent(eventData);

            var messageBody = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(addAccountEvent));

            var receivedMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(new BinaryData(messageBody));

            var messageActionsMock = new Mock<ServiceBusMessageActions>();

            using var context = DbContextMockExtensions.CreateInMemoryDbContext();

            var dbContextFactory = new Mock<IDbContextFactory<ApplicationDbContext>>();
            dbContextFactory.Setup(d => d.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(context);

            var function = new AddContactFromPulse(loggerMock.Object, dbContextFactory.Object);

            // Act
            await function.Run(receivedMessage, messageActionsMock.Object);

            // Assert
            messageActionsMock.Verify(ma => ma.CompleteMessageAsync(receivedMessage, default), Times.Once);
        }

        /// <summary>
        /// RunUnValidMessageSendsToDeadLetter.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task RunUnValidMessageSendsToDeadLetter()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<AddContactFromPulse>>();

            var receivedMessage = ServiceBusModelFactory.ServiceBusReceivedMessage();

            var messageActionsMock = new Mock<ServiceBusMessageActions>();
            messageActionsMock.Setup(m =>
            m.DeadLetterMessageAsync(receivedMessage, It.IsAny<Dictionary<string, object>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            using var context = DbContextMockExtensions.CreateInMemoryDbContext();

            var dbContextFactory = new Mock<IDbContextFactory<ApplicationDbContext>>();
            dbContextFactory.Setup(d => d.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(context);

            var function = new AddContactFromPulse(loggerMock.Object, dbContextFactory.Object);

            // Act
            await function.Run(receivedMessage, messageActionsMock.Object);

            // Assert
            messageActionsMock.Verify(m => m.DeadLetterMessageAsync(receivedMessage, It.IsAny<Dictionary<string, object>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            messageActionsMock.Verify(ma => ma.CompleteMessageAsync(receivedMessage, default), Times.Never);
        }

        /// <summary>
        /// Run_ArgumentNullException.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task Run_ArgumentNullException()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<AddContactFromPulse>>();

            var receivedMessage = ServiceBusModelFactory.ServiceBusReceivedMessage();

            using var context = DbContextMockExtensions.CreateInMemoryDbContext();

            var dbContextFactory = new Mock<IDbContextFactory<ApplicationDbContext>>();
            dbContextFactory.Setup(d => d.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(context);

            var function = new AddContactFromPulse(loggerMock.Object, dbContextFactory.Object);

            // Act
            Func<Task> act = async () => await function.Run(receivedMessage, null);

            // Assert
            await act.Should().ThrowExactlyAsync<ArgumentNullException>()
                                .WithMessage("Value cannot be null. (Parameter 'messageActions')");
        }
    }
}
