// <copyright file="ContactRemovedEventHandlerTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models.Contacts;
using Application.Models.Results;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using Pulse.Back.Events.IntegrationEvents;
using Infrastructure.Providers;

namespace Registry.Infrastructure.Tests.Providers
{
    public class ContactRemovedEventHandlerTests
    {
        private readonly Mock<IContactService> _contactServiceMock;
        public ContactRemovedEventHandlerTests()
        {
            _contactServiceMock = new Mock<IContactService>();
        }

        [Fact]
        public async Task ContactCreatedEvent_ShouldSendRequestToReferentiel()
        {
            ContactRemovedEventData contactStateEventData = new ContactRemovedEventData
            {
                ContactId = 1,
            };


            var logger = new Mock<ILogger<ContactRemovedEventHandler>>();

            var contactEvent = new ContactRemovedEvent(contactStateEventData);

            var contentMessage = JsonConvert.SerializeObject(contactEvent);

            var eventHandler = new ContactRemovedEventHandler(logger.Object, _contactServiceMock.Object);

            var contactEventResult = new ContactEventResult<Contact>
            {
                EventName = "ContactRemovedEventHandler",
                IsRegisteredInDb = true,
                IsSentToAkuiteo = false,
                IsOpeationProcessUpdated = true
            };

            _contactServiceMock.Setup(x => x.OnRemovedContactEventExecution(It.IsAny<ContactRemovedEventData>())).ReturnsAsync(contactEventResult);

            await eventHandler.HandleAsync(contentMessage);

            _contactServiceMock.Verify(x => x.OnRemovedContactEventExecution(It.IsAny<ContactRemovedEventData>()), Times.Once);

        }
    }
}
