using Application.Enums;
using Application.Interfaces;
using Application.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Pulse.Registry.Domain.Entities;
using Registry.AzureFuctions.Functions;

namespace Registry.AzureFunctions.Tests.Functions;

public class CreateOfferSubscriptionEventsTests
{
    private readonly Mock<IOfferEventPublisher> _offerEventPublisherMock;
    private readonly Mock<IOfferRepository> _offerRepositoryMock;

    public CreateOfferSubscriptionEventsTests()
    {
        _offerEventPublisherMock = new Mock<IOfferEventPublisher>(MockBehavior.Strict);
        _offerRepositoryMock = new Mock<IOfferRepository>(MockBehavior.Strict);
    }

    [Fact]
    public async Task Run_Should_Send_Events()
    {
        // Arrange
        Guid batchId = Guid.NewGuid();

        var offers = new List<RefOfferEntity>()
            {
                new RefOfferEntity(){
                    Id = 1,
                    BatchId = batchId,
                    Offer = "offer1",
                    AccountingExpertEmail = "expert@test.fr",
                    ClientEmail = "client@test.fr",
                    CollaboratorEmail = "collab@test.fr",
                    AccountNumber = "123456789",
                    MigrationStatus = OfferMigrationStatus.SUCCESS.ToString(),
                    MissionLeaderEmail = "lead@test.fr"
                },
                new RefOfferEntity()
                {
                    Id = 2,
                    BatchId = batchId,
                    Offer = "offer2",
                    AccountingExpertEmail = "expert2@test.fr",
                    ClientEmail = "client2@test.fr",
                    CollaboratorEmail = "collab2@test.fr",
                    AccountNumber = "523456789",
                    MigrationStatus = OfferMigrationStatus.SUCCESS.ToString(),
                    MissionLeaderEmail = "lead2@test.fr"
                }
            };

        _offerRepositoryMock.Setup(r => r.GetOffersByBatchIdAsync(batchId))
            .ReturnsAsync(offers);

        _offerEventPublisherMock.Setup(p => p.BulkPublishOfferEventAsync(It.IsAny<List<Offer>>()))
            .Callback((List<Offer> events) =>
            {
                Assert.Equal(2, events.Count);
                Assert.Equal("offer1", events[0].OfferName);
                Assert.Equal("offer2", events[1].OfferName);
            })
            .Returns(Task.CompletedTask);

        var mockLoggerFactory = new Mock<ILoggerFactory>();
        var mockLogger = new Mock<ILogger<CreateOfferSubscriptionEvents>>();
        mockLoggerFactory
            .Setup(factory => factory.CreateLogger(It.IsAny<string>()))
            .Returns(mockLogger.Object);

        // Act
        var function = new CreateOfferSubscriptionEvents(
            mockLoggerFactory.Object,
            _offerEventPublisherMock.Object,
            _offerRepositoryMock.Object
            );

        await function.Run(batchId.ToString());

        // Assert
        _offerRepositoryMock.Verify(r => r.GetOffersByBatchIdAsync(batchId), Times.Once);
        _offerEventPublisherMock.Verify(p => p.BulkPublishOfferEventAsync(It.IsAny<List<Offer>>()), Times.Once);
    }
}
