using Azure.Messaging.ServiceBus;
using Application.Options;
using Microsoft.Extensions.Options;
using Moq;
using Pulse.Back.Events.Abstractions;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.Offer.Infrastructure.Providers;
using Registry.Infrastructure.Managers;

namespace Pulse.Offer.Infrastructure.Tests.Providers
{
    public class OfferEventPublisherTests
    {
        private readonly Mock<INotificationManager> _notificationMock;
        private readonly Mock<IServiceBusMessageFactory> _messageFactoryMock;
        private readonly IOptions<OffersMigrationOptions> _options;
        private readonly OfferEventPublisher _publisher;

        public OfferEventPublisherTests()
        {
            _notificationMock = new Mock<INotificationManager>(MockBehavior.Strict);
            _messageFactoryMock = new Mock<IServiceBusMessageFactory>(MockBehavior.Strict);

            // Injection de l'option nécessaire au nouveau constructeur
            var opts = new OffersMigrationOptions { RegistryOfferBatchQueueName = "test-queue" };
            _options = Options.Create(opts);

            _publisher = new OfferEventPublisher(
                _notificationMock.Object,
                _messageFactoryMock.Object,
                _options);
        }

        [Fact]
        public async Task PublishOfferBatchEventAsync_CreatesAndPublishesRegistryOfferBatchEvent()
        {
            var batchId = Guid.NewGuid();
            _notificationMock
                .Setup(nm => nm.PublishAsync(
                    It.Is<RegistryOfferBatchEvent>(e => e.Data.BatchId == batchId),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            await _publisher.PublishOfferBatchEventAsync(batchId);

            _notificationMock.Verify();
        }

        [Fact]
        public async Task SendOfferRegistryBatchEvent_Calls_SendMessageToQueueAsync_With_Correct_Parameters()
        {
            var batchId = Guid.NewGuid();
            _notificationMock
                .Setup(nm => nm.SendMessageToQueueAsync(
                    batchId.ToString(),
                    _options.Value.RegistryOfferBatchQueueName))
                .Returns(Task.CompletedTask)
                .Verifiable();

            await _publisher.SendOfferRegistryBatchEvent(batchId);

            _notificationMock.Verify();
        }

        [Fact]
        public async Task PublishOfferEventAsync_MapsModelAndPublishesRegistryOfferEvent()
        {
            var model = new Application.Models.Offer
            {
                AccountNumber = "ACC123",
                ClientEmail = "client@example.com",
                CollaboratorEmail = "collab@example.com",
                MissionLeaderEmail = "leader@example.com",
                AccountingExpertEmail = "expert@example.com",
                OfferName = "SpecialOffer",
                MigrationStatus = "NEW"
            };

            _notificationMock
                .Setup(nm => nm.PublishAsync(
                    It.Is<RegistryOfferEvent>(e =>
                        e.Data.AccountNumber == model.AccountNumber &&
                        e.Data.ClientEmail == model.ClientEmail &&
                        e.Data.CollaboratorEmail == model.CollaboratorEmail &&
                        e.Data.MissionLeaderEmail == model.MissionLeaderEmail &&
                        e.Data.AccountingExpertEmail == model.AccountingExpertEmail &&
                        e.Data.Offer == model.OfferName),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            await _publisher.PublishOfferEventAsync(model);

            _notificationMock.Verify();
        }

        [Fact]
        public async Task BulkPublishOfferEventAsync_CreatesMessagesAndBulkPublishes()
        {
            var offers = new List<Application.Models.Offer>
            {
                new Application.Models.Offer
                {
                    AccountNumber          = "A1",
                    ClientEmail            = "c1@e",
                    CollaboratorEmail      = "col1@e",
                    MissionLeaderEmail     = "m1@e",
                    AccountingExpertEmail  = "acc1@e",
                    OfferName              = "O1",
                    MigrationStatus        = "NEW"
                },
                new Application.Models.Offer
                {
                    AccountNumber          = "A2",
                    ClientEmail            = "c2@e",
                    CollaboratorEmail      = "col2@e",
                    MissionLeaderEmail     = "m2@e",
                    AccountingExpertEmail  = "acc2@e",
                    OfferName              = "O2",
                    MigrationStatus        = "NEW"
                }
            };

            var dummyMsg1 = new ServiceBusMessage("m1");
            var dummyMsg2 = new ServiceBusMessage("m2");

            // Le factory doit créer les bons messages
            _messageFactoryMock
                .Setup(mf => mf.CreateMessage(
                    It.Is<RegistryOfferEvent>(e => e.Data.Offer == "O1" && e.Data.AccountNumber == "A1"), It.IsAny<string>()))
                .Returns(dummyMsg1)
                .Verifiable();
            _messageFactoryMock
                .Setup(mf => mf.CreateMessage(
                    It.Is<RegistryOfferEvent>(e => e.Data.Offer == "O2" && e.Data.AccountNumber == "A2"), It.IsAny<string>()))
                .Returns(dummyMsg2)
                .Verifiable();

            // Vérifie qu'on envoie les deux messages
            _notificationMock
                .Setup(nm => nm.BulkPublishAsync(
                    It.Is<List<ServiceBusMessage>>(list =>
                        list.Count == 2 &&
                        list[0] == dummyMsg1 &&
                        list[1] == dummyMsg2),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            await _publisher.BulkPublishOfferEventAsync(offers);

            _messageFactoryMock.Verify();
            _notificationMock.Verify();
        }
    }
}
