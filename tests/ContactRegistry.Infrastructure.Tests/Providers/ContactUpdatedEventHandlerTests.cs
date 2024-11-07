// <copyright file="ContactUpdatedEventHandlerTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>


using Application.Interfaces;
using Infrastructure.Providers;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using Pulse.Back.Events.IntegrationEvents;

namespace ContactRegistry.Infrastructure.Tests.Providers
{
    public class ContactUpdatedEventHandlerTests
    {
        private readonly Mock<IContactRegistryProvider> _mockProvider;
        public ContactUpdatedEventHandlerTests()
        {
            _mockProvider = new Mock<IContactRegistryProvider>(MockBehavior.Strict);
        }

        [Fact]
        public async Task ContactUpdatedEvent_ShouldSendRequestToReferentiel()
        {
            ContactStateEventData contactStateEventData = new ContactStateEventData
            {
                Email = "test@kpmg.fr",
                ContactId = 1,
                Type = "Collaborateur",
                LastName = "Test",
                FirstName = "exam",
                IsActive = true,
                CreationDate = DateTime.Now,
            };


            var logger = new Mock<ILogger<ContactUpdatedEventHandler>>();

            var contactEvent = new ContactUpdatedEvent(contactStateEventData);

            var contentMessage = JsonConvert.SerializeObject(contactEvent);

            var eventHandler = new ContactUpdatedEventHandler(logger.Object, _mockProvider.Object);

            _mockProvider.Setup(x => x.UpdateContactAsync(It.IsAny<Application.Models.ContactRegistry>()));

            await eventHandler.HandleAsync(contentMessage);

            _mockProvider.Verify(x => x.UpdateContactAsync(It.IsAny<Application.Models.ContactRegistry>()), Times.Once);

        }
    }
}
