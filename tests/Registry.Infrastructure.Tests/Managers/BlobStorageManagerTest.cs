using Application.Exceptions;
using Application.Options;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FluentAssertions;
using Infrastructure.Managers;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Infrastructure.UnitTests.Managers
{
    public class BlobStorageManagerTest
    {
        private readonly BlobStorageManager _sut;
        private readonly Mock<IAzureClientFactory<BlobServiceClient>> clientFactory;
        private readonly Mock<ILogger<BlobStorageManager>> logger;
        private readonly Mock<BlobServiceClient> blobServiceClient;
        private readonly IOptions<BlobStorageOptions> options;

        public BlobStorageManagerTest()
        {
            clientFactory = new Mock<IAzureClientFactory<BlobServiceClient>>();
            logger = new Mock<ILogger<BlobStorageManager>>();
            blobServiceClient = new Mock<BlobServiceClient>();

            options = Options.Create(new BlobStorageOptions
            {
                ContainerName = "test-container",
            });

            clientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(blobServiceClient.Object);
            blobServiceClient.Setup(b => b.GetBlobContainerClient(options.Value.ContainerName))
                             .Returns(() => null);

            _sut = new BlobStorageManager(clientFactory.Object, options, logger.Object);
        }

        #region SaveFileAsync Tests

        [Fact]
        public async Task SaveFileAsync_ShouldThrowException_WhenRequestFails()
        {
            ResetBlobContainer();

            // Arrange
            string endpoint = "testEndpoint";
            string fileContent = "Test file content";

            var blobClientMock = new Mock<BlobClient>();
            blobClientMock
                 .Setup(b => b.UploadAsync(
                     It.IsAny<Stream>(),
                     true,
                     It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new RequestFailedException("Simulated upload failure"));
            BlobClient blobClient = blobClientMock.Object;

            var blobContainerClientMock = new Mock<BlobContainerClient>();
            blobContainerClientMock
                 .Setup(c => c.GetBlobClient(It.IsAny<string>()))
                 .Returns(blobClient)
                 .Verifiable();
            BlobContainerClient blobContainerClient = blobContainerClientMock.Object;

            blobServiceClient.Setup(b => b.GetBlobContainerClient(options.Value.ContainerName))
                             .Returns(blobContainerClient);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<BlobStorageOperationException>(() => _sut.SaveFileAsync(endpoint, fileContent));
            exception.Message.Should().Contain("An error occurred while saving the file.");
        }

        [Fact]
        public async Task SaveFileAsync_ShouldReturnTrue_WhenUploadSucceeds_AndFileNameMatchesPattern()
        {
            ResetBlobContainer();

            // Arrange
            string endpoint = "testEndpoint";
            string fileContent = "Test file content";

            BlobClient blobClient = CreateMockBlobClient_Success();

            var blobContainerClientMock = new Mock<BlobContainerClient>();
            blobContainerClientMock
                 .Setup(c => c.GetBlobClient(It.IsAny<string>()))
                 .Returns(blobClient)
                 .Verifiable();
            BlobContainerClient blobContainerClient = blobContainerClientMock.Object;

            blobServiceClient.Setup(b => b.GetBlobContainerClient(options.Value.ContainerName))
                             .Returns(blobContainerClient);

            // Act
            var result = await _sut.SaveFileAsync(endpoint, fileContent);

            // Assert
            result.Should().BeTrue();

            string expectedPattern = $"^{Regex.Escape(endpoint)}_\\d{{8}}_\\d{{6}}\\.csv$";
            blobContainerClientMock.Verify(c =>
                c.GetBlobClient(It.Is<string>(fileName => Regex.IsMatch(fileName, expectedPattern))),
                Times.Once,
                "The file name should match the pattern {endpoint}_yyyyMMdd_HHmmss.csv");
        }

        #endregion SaveFileAsync Tests

        #region Helper Methods

        private static BlobClient CreateMockBlobClient_Success()
        {
            var mockBlobClient = new Mock<BlobClient>();
            var responseMock = new Mock<Response>();

            var dummyBlobContentInfo = default(BlobContentInfo);
            var dummyResponse = Response.FromValue(dummyBlobContentInfo, responseMock.Object);

            mockBlobClient
                 .Setup(b => b.UploadAsync(
                     It.IsAny<Stream>(),
                     true,
                     It.IsAny<CancellationToken>()))
                 .ReturnsAsync(dummyResponse);

            return mockBlobClient.Object;
        }

        /// <summary>
        /// Resets the static blobContainer field in BlobStorageManager to null.
        /// </summary>
        private void ResetBlobContainer()
        {
            var field = typeof(BlobStorageManager).GetField("blobContainer", BindingFlags.NonPublic | BindingFlags.Static);
            field.SetValue(null, null);
        }

        #endregion Helper Methods
    }
}
