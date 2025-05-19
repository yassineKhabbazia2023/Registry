using Application.Exceptions;
using Application.Helpers;
using Application.Interfaces;
using Application.Models;
using CsvHelper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Text;
using WebApi.Configurations.Models;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/offer")]
    public class OfferController : ControllerBase
    {
        private TokenModel tokenModel;
        private IBlobStorageManager blobStorageManager;
        private IOfferService offerService;
        private IOfferEventPublisher eventPublisher;

        public OfferController(IOptions<TokenModel> tokenModel, IBlobStorageManager blobStorageManager, IOfferService offerService, IOfferEventPublisher eventPublisher)
        {
            this.tokenModel = tokenModel.Value;
            this.blobStorageManager = blobStorageManager;
            this.offerService = offerService;
            this.eventPublisher = eventPublisher;
        }

        /// <summary>
        /// Allow updating offer informations for Pulse.
        /// </summary>
        /// <param name="token">Security token ensuring the caller is legit.</param>
        /// <param name="data">Data containing the offer information in CSV format.</param>
        /// <returns></returns>
        [HttpPost("update")]
        [Consumes("application/csv")]
        public async Task<IActionResult> UpdateAsync([FromQuery] string token, [FromBody] string data)
        {
            try
            {
                await this.blobStorageManager.SaveFileAsync("Offer", data);
            }
            catch (BlobStorageOperationException ex)
            {
                return BadRequest($"Something went wrong when saving received csv {ex.InnerException}");
            }

            List<(Offer, int, string[])> csvDatas = [];

            if (string.IsNullOrWhiteSpace(token) || !this.tokenModel.Token.Equals(token))
            {
                return new UnauthorizedObjectResult("Invalid token.");
            }


            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(data)))
            {
                try
                {
                    csvDatas = CsvFileReader.ReadStreamAsync<Offer>(stream).ToList();
                }
                catch (HeaderValidationException)
                {
                    return BadRequest("Invalid data: Missing columns in header");
                }

            }

            var offers = csvDatas.Select(d => d.Item1);
            var result = new ValidationHelper<Offer>().Validate(offers);

            if (result.ValidateModels.Count == 0)
            {
                return BadRequest($"Csv Offers retreval process unsuccessful with errors: {JsonConvert.SerializeObject(result.Errors)}");
            }
            else
            {
                var batchId = Guid.NewGuid();
                await this.offerService.SaveOffersAsync(result.ValidateModels, batchId);
                try
                {
                    await this.eventPublisher.SendOfferRegistryBatchEvent(batchId);
                }
                catch (ServiceBusOperationException ex)
                {
                    var baseMessage = "CSV offers retrieval process";
                    var errorDetails = result.Errors.Count != 0
                        ? $"{baseMessage} succeeded with errors: {JsonConvert.SerializeObject(result.Errors)}"
                        : $"{baseMessage} completed successfully";

                    return BadRequest($"{errorDetails}, but failed to send the completion message to the queue: {ex.Message}");
                }

                return result.Errors.Count != 0
                    ? BadRequest($"CSV offers retrieval process succeeded with errors: {JsonConvert.SerializeObject(result.Errors)}")
                    : Ok("CSV offers retrieval process completed successfully");
            }
        }
    }
}
