using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Exceptions;
using Application.Interfaces;
using Application.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using WebApi.Configurations.Models;
using WebApi.Controllers;
using Xunit;

namespace Registry.WebApi.Tests.Controllers
{
    class FakeTokenOptions : IOptions<TokenModel>
    {
        public TokenModel Value { get; }
        public FakeTokenOptions(string token) => Value = new TokenModel { Token = token };
    }

    public class OfferControllerTests
    {
        private const string ValidToken = "secret";
        private readonly Mock<IBlobStorageManager> _blobMock;
        private readonly Mock<IOfferService> _offerSvcMock;
        private readonly Mock<IOfferEventPublisher> _eventPublisherMock;
        private readonly IOptions<TokenModel> _tokenOpts;
        private readonly OfferController _controller;

        public OfferControllerTests()
        {
            _blobMock = new Mock<IBlobStorageManager>(MockBehavior.Strict);
            _offerSvcMock = new Mock<IOfferService>(MockBehavior.Strict);
            _eventPublisherMock = new Mock<IOfferEventPublisher>(MockBehavior.Strict);
            _tokenOpts = new FakeTokenOptions(ValidToken);
            _controller = new OfferController(_tokenOpts, _blobMock.Object, _offerSvcMock.Object, _eventPublisherMock.Object);
        }

        [Fact]
        public async Task UpdateAsync_WhenBlobStorageFails_ReturnsBadRequest()
        {
            const string csv = "whatever";
            _blobMock
                .Setup(b => b.SaveFileAsync("Offer", csv))
                .ThrowsAsync(new BlobStorageOperationException("x", new Exception("oops")));

            var result = await _controller.UpdateAsync(ValidToken, csv);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            bad.Value!.ToString()!.Should().StartWith("Something went wrong when saving received csv");
            _blobMock.Verify(b => b.SaveFileAsync("Offer", csv), Times.Once);
            _offerSvcMock.VerifyNoOtherCalls();
            _eventPublisherMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("wrong", "some data")]
        public async Task UpdateAsync_InvalidOrEmptyToken_ReturnsUnauthorized(string token, string data)
        {
            var result = await _controller.UpdateAsync(token, data);

            Assert.IsType<UnauthorizedObjectResult>(result);
            _blobMock.Verify(b => b.SaveFileAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _offerSvcMock.VerifyNoOtherCalls();
            _eventPublisherMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task UpdateAsync_MissingCsvHeader_ReturnsBadRequest()
        {
            _blobMock
                .Setup(b => b.SaveFileAsync("Offer", It.IsAny<string>()))
                .ReturnsAsync(true);

            var badCsv = "foo;bar\n1;2";   // invalid for Offer

            var result = await _controller.UpdateAsync(ValidToken, badCsv);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            bad.Value.Should().Be("Invalid data: Missing columns in header");
            _blobMock.Verify(b => b.SaveFileAsync("Offer", badCsv), Times.Once);
            _offerSvcMock.VerifyNoOtherCalls();
            _eventPublisherMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task UpdateAsync_NoValidModels_ReturnsBadRequestUnsuccessful()
        {
            // Only header → no valid models
            var sb = new StringBuilder();
            sb.AppendLine("AccountNumber;ClientEmail;CollaboratorEmail;MissionLeaderEmail;AccountingExpertEmail;OfferName;MigrationStatus");
            var headerOnlyCsv = sb.ToString();

            _blobMock
                .Setup(b => b.SaveFileAsync("Offer", headerOnlyCsv))
                .ReturnsAsync(true);

            var result = await _controller.UpdateAsync(ValidToken, headerOnlyCsv);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            bad.Value.Should().Be($"Csv Offers retreval process unsuccessful with errors: []");

            _blobMock.Verify(b => b.SaveFileAsync("Offer", headerOnlyCsv), Times.Once);
            _offerSvcMock.VerifyNoOtherCalls();
            _eventPublisherMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task UpdateAsync_ValidAndInvalidModels_ReturnsBadRequestWithErrors()
        {
            // Arrange: one valid line, one invalid line
            var sb = new StringBuilder();
            sb.AppendLine("AccountNumber;ClientEmail;CollaboratorEmail;MissionLeaderEmail;AccountingExpertEmail;OfferName;MigrationStatus");
            sb.AppendLine("ACC1;client@ex.com;collab@ex.com;lead@ex.com;expert@ex.com;MyOffer;NEW");
            sb.AppendLine(";;;;;;;");
            var mixedCsv = sb.ToString();

            _blobMock
                .Setup(b => b.SaveFileAsync("Offer", mixedCsv))
                .ReturnsAsync(true);

            List<Offer>? saved = null;
            Guid? usedBatch = null;
            _offerSvcMock
                .Setup(s => s.SaveOffersAsync(It.IsAny<IEnumerable<Offer>>(), It.IsAny<Guid>()))
                .Callback<IEnumerable<Offer>, Guid>((o, id) =>
                {
                    saved = o.ToList();
                    usedBatch = id;
                })
                .Returns(Task.CompletedTask)
                .Verifiable();

            _eventPublisherMock
                .Setup(e => e.SendOfferRegistryBatchEvent(It.IsAny<Guid>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            var result = await _controller.UpdateAsync(ValidToken, mixedCsv);

            // Assert
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            bad.Value!
               .ToString()!
               .Should().StartWith("CSV offers retrieval process succeeded with errors:");

            _blobMock.Verify(b => b.SaveFileAsync("Offer", mixedCsv), Times.Once);
            _offerSvcMock.Verify();
            _eventPublisherMock.Verify();

            saved!
                .Should().ContainSingle()
                .Which.AccountNumber.Should().Be("ACC1");
            usedBatch.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateAsync_ValidCsvAndToken_CallsSaveAndReturnsOk()
        {
            // Arrange: one fully valid line
            var sb = new StringBuilder();
            sb.AppendLine("AccountNumber;ClientEmail;CollaboratorEmail;MissionLeaderEmail;AccountingExpertEmail;OfferName;MigrationStatus");
            sb.AppendLine("ACC1;client@ex.com;collab@ex.com;lead@ex.com;expert@ex.com;MyOffer;NEW");
            var goodCsv = sb.ToString();

            _blobMock
                .Setup(b => b.SaveFileAsync("Offer", goodCsv))
                .ReturnsAsync(true);

            List<Offer>? saved = null;
            Guid? usedBatch = null;
            _offerSvcMock
                .Setup(s => s.SaveOffersAsync(It.IsAny<IEnumerable<Offer>>(), It.IsAny<Guid>()))
                .Callback<IEnumerable<Offer>, Guid>((o, id) =>
                {
                    saved = o.ToList();
                    usedBatch = id;
                })
                .Returns(Task.CompletedTask)
                .Verifiable();

            _eventPublisherMock
                .Setup(e => e.SendOfferRegistryBatchEvent(It.IsAny<Guid>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            var result = await _controller.UpdateAsync(ValidToken, goodCsv);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            ok.Value.Should().Be("CSV offers retrieval process completed successfully");

            _blobMock.Verify(b => b.SaveFileAsync("Offer", goodCsv), Times.Once);
            _offerSvcMock.Verify();
            _eventPublisherMock.Verify();

            saved!
                .Should().ContainSingle()
                .Which.AccountNumber.Should().Be("ACC1");
            usedBatch.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateAsync_ServiceBusError_ReturnsBadRequestWithQueueError()
        {
            // Arrange: valid CSV but queue send fails
            var sb = new StringBuilder();
            sb.AppendLine("AccountNumber;ClientEmail;CollaboratorEmail;MissionLeaderEmail;AccountingExpertEmail;OfferName;MigrationStatus");
            sb.AppendLine("ACC1;client@ex.com;collab@ex.com;lead@ex.com;expert@ex.com;MyOffer;NEW");
            var goodCsv = sb.ToString();

            _blobMock
                .Setup(b => b.SaveFileAsync("Offer", goodCsv))
                .ReturnsAsync(true);

            _offerSvcMock
                .Setup(s => s.SaveOffersAsync(It.IsAny<IEnumerable<Offer>>(), It.IsAny<Guid>()))
                .Returns(Task.CompletedTask);

            _eventPublisherMock
                .Setup(e => e.SendOfferRegistryBatchEvent(It.IsAny<Guid>()))
                .ThrowsAsync(new ServiceBusOperationException("Queue failure"));

            // Act
            var result = await _controller.UpdateAsync(ValidToken, goodCsv);

            // Assert
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            bad.Value!
               .Should().Be("CSV offers retrieval process completed successfully, but failed to send the completion message to the queue: Queue failure");

            _blobMock.Verify(b => b.SaveFileAsync("Offer", goodCsv), Times.Once);
            _offerSvcMock.Verify(s => s.SaveOffersAsync(It.IsAny<IEnumerable<Offer>>(), It.IsAny<Guid>()), Times.Once);
            _eventPublisherMock.Verify(e => e.SendOfferRegistryBatchEvent(It.IsAny<Guid>()), Times.Once);
        }
    }
}
