// <copyright file="ContactUpdatedEventHandlerTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>


using Application.Interfaces;
using Application.Providers;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using Pulse.Back.Events.IntegrationEvents;
using Infrastructure.Providers;
using Application.Models.Results;
using Application.Models.Contacts;

namespace Registry.Infrastructure.Tests.Providers
{
    public class ContactUpdatedEventHandlerTests
    {
        private readonly Mock<IContactService> _contactServiceMock;
        public ContactUpdatedEventHandlerTests()
        {
            _contactServiceMock = new Mock<IContactService>();
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
                Source = "Pulse"
            };


            var logger = new Mock<ILogger<ContactUpdatedEventHandler>>();

            var contactEvent = new ContactUpdatedEvent(contactStateEventData);

            var contentMessage = JsonConvert.SerializeObject(contactEvent);

            var eventHandler = new ContactUpdatedEventHandler(logger.Object, _contactServiceMock.Object);

            var contactEventResult = new ContactEventResult<Contact>
            {
                EventName = "ContactUpdatedEventHandler",
                IsRegisteredInDb = true,
                IsSentToAkuiteo= true,
                IsOpeationProcessUpdated = true
            };
            _contactServiceMock.Setup(x => x.OnUpdatedContactEventExecution(It.IsAny<ContactStateEventData>())).ReturnsAsync(contactEventResult);
            await eventHandler.HandleAsync(contentMessage);

            _contactServiceMock.Verify(x => x.OnUpdatedContactEventExecution(It.IsAny<ContactStateEventData>()), Times.Once);

        }
    }
}
