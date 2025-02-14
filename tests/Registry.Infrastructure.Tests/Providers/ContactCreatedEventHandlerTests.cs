// <copyright file="ContactCreatedEventHandlerTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models.Contacts;
using Application.Models.Results;
using Application.Providers;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.Back.Events.IntegrationEvents.EventsData;

namespace Registry.Infrastructure.Tests.Providers
{
    public class ContactCreatedEventHandlerTests
    {
        private readonly Mock<IContactService> _contactServiceMock;
        public ContactCreatedEventHandlerTests()
        {
            _contactServiceMock = new Mock<IContactService>();
        }

        [Fact]
        public async Task ContactCreatedEvent_ShouldSendRequestToReferentiel()
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
                Source="PULSE"
            };


            var logger = new Mock<ILogger<ContactCreatedEventHandler>>();

            var contactEvent = new ContactCreatedEvent(contactStateEventData);

            var contentMessage = JsonConvert.SerializeObject(contactEvent);
            
            var eventHandler = new ContactCreatedEventHandler(logger.Object, _contactServiceMock.Object);

            var contactEventResult = new ContactEventResult<Contact>
            {
                EventName = "ContactCreatedEventHandler",
                IsRegisteredInDb = true,
                IsSentToAkuiteo = true,
                IsOpeationProcessUpdated = true
            };

            _contactServiceMock.Setup(x => x.OnCreatedContactEventExecution(It.IsAny<ContactStateEventData>())).ReturnsAsync(contactEventResult);

            await eventHandler.HandleAsync(contentMessage);

            _contactServiceMock.Verify(x => x.OnCreatedContactEventExecution(It.IsAny<ContactStateEventData>()), Times.Once);

        }
    }
}
