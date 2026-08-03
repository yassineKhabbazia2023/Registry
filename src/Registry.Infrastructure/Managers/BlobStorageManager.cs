using Application.Exceptions;
using Application.Interfaces;
using Application.Options;
using Azure;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Managers
{
    public class BlobStorageManager(IAzureClientFactory<BlobServiceClient> clientFactory, IOptions<BlobStorageOptions> options, ILogger<BlobStorageManager> logger) : IBlobStorageManager
    {
        private static BlobContainerClient? blobContainer;
        private readonly BlobStorageOptions blobClientOptions = options.Value;
        private readonly BlobServiceClient blobServiceClient = clientFactory.CreateClient("Default");
        private readonly ILogger<BlobStorageManager> logger = logger;

        public async Task<string> SaveFileAsync(string endpoint, string fileContent)
        {
            try
            {
                this.GetBlobContainerClient();

                if (blobContainer == null)
                {
                    this.logger.LogError("Blob container client is not initialized.");
                    throw new BlobStorageOperationException("Blob container client is not initialized.");
                }

                string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                string fileName = $"{endpoint}_{timestamp}.csv";

                using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));

                var blobClient = blobContainer.GetBlobClient(fileName);

                await blobClient.UploadAsync(stream, overwrite: true);

                this.logger.LogInformation($"File {fileName} successfully uploaded to Blob Storage.");
                return fileName;
            }
            catch (RequestFailedException ex)
            {
                this.logger.LogError(ex, "Error while uploading file to Blob Storage.");
                throw new BlobStorageOperationException("An error occurred while saving the file.", ex);
            }
        }

        /// <summary>
        /// Method to get the BlobContainerClient for interacting with Azure Blob Storage.
        /// </summary>
        private void GetBlobContainerClient()
        {
            blobContainer ??= this.blobServiceClient!.GetBlobContainerClient(this.blobClientOptions.ContainerName);
        }
    }
}
