using Application.Interfaces;
using Application.Models;
using Application.services;
using Moq;
using Pulse.Registry.Domain.Entities;

namespace Registry.Application.Tests.Services;

public class OfferServiceTest
{
    private readonly Mock<IOfferRepository> _offerRepositoryMock;
    private readonly OfferService _service;

    public OfferServiceTest()
    {
        _offerRepositoryMock = new Mock<IOfferRepository>(MockBehavior.Strict);
        _service = new OfferService(_offerRepositoryMock.Object);
    }

    [Fact]
    public async Task SaveOffersAsync_WithOffers_MapsAndSavesAllEntities()
    {
        // Arrange
        var offers = new List<Offer>
            {
                new Offer
                {
                    AccountNumber = "ACC1",
                    ClientEmail = "client1@ex.com",
                    CollaboratorEmail = "collab1@ex.com",
                    MissionLeaderEmail = "leader1@ex.com",
                    AccountingExpertEmail = "expert1@ex.com",
                    OfferName = "OfferOne",
                    MigrationStatus = "NEW"
                },
                new Offer
                {
                    AccountNumber = "ACC2",
                    ClientEmail = "client2@ex.com",
                    CollaboratorEmail = "collab2@ex.com",
                    MissionLeaderEmail = "leader2@ex.com",
                    AccountingExpertEmail = "expert2@ex.com",
                    OfferName = "OfferTwo",
                    MigrationStatus = "PENDING"
                }
            };

        var InputbatchId = Guid.NewGuid();

        List<RefOfferEntity>? captured = null;
        _offerRepositoryMock
            .Setup(r => r.SaveOffersAsync(It.IsAny<IEnumerable<RefOfferEntity>>(), It.IsAny<Guid>()))
            .Callback<IEnumerable<RefOfferEntity>, Guid>((entities, batchId) =>
            {
                captured = entities.ToList();
                Assert.Equal(batchId, InputbatchId);
            })
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        await _service.SaveOffersAsync(offers, InputbatchId);

        // Assert
        _offerRepositoryMock.Verify(r => r.SaveOffersAsync(It.IsAny<IEnumerable<RefOfferEntity>>(), It.IsAny<Guid>()), Times.Once);
        Assert.NotNull(captured);
        Assert.Equal(2, captured.Count);

        foreach (var (src, dest) in offers.Zip(captured, (o, e) => (o, e)))
        {
            Assert.Equal(src.AccountNumber, dest.AccountNumber);
            Assert.Equal(src.ClientEmail, dest.ClientEmail);
            Assert.Equal(src.CollaboratorEmail, dest.CollaboratorEmail);
            Assert.Equal(src.MissionLeaderEmail, dest.MissionLeaderEmail);
            Assert.Equal(src.AccountingExpertEmail, dest.AccountingExpertEmail);
            Assert.Equal(src.OfferName, dest.Offer);
            Assert.Equal(src.MigrationStatus, dest.MigrationStatus);
        }
    }

    [Fact]
    public async Task SaveOffersAsync_WithEmptyList_CallsRepositoryWithEmptyCollection()
    {
        // Arrange
        var offers = Enumerable.Empty<Offer>();
        List<RefOfferEntity>? captured = null;
        var InputbatchId = Guid.NewGuid();

        _offerRepositoryMock
            .Setup(r => r.SaveOffersAsync(It.IsAny<IEnumerable<RefOfferEntity>>(), It.IsAny<Guid>()))
            .Callback<IEnumerable<RefOfferEntity>, Guid>((entities, batchId) =>
            {
                captured = entities.ToList();
                Assert.Equal(batchId, InputbatchId);
            })
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        await _service.SaveOffersAsync(offers, InputbatchId);

        // Assert
        _offerRepositoryMock.Verify(r => r.SaveOffersAsync(It.IsAny<IEnumerable<RefOfferEntity>>(), InputbatchId), Times.Once);
        Assert.NotNull(captured);
        Assert.Empty(captured);
    }
}
