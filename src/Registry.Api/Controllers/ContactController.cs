// <copyright file="ContactController.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

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

namespace Registry.WebApi.Controllers;

/// <summary>
/// ContactController.
/// </summary>
[ApiController]
[Route("api/contact")]
public class ContactController : ControllerBase
{
    private readonly IContactService _contactService;
    private readonly TokenModel _tokenModel;
    private readonly IBlobStorageManager _blobStorageManager;

    /// <summary>
    /// ContactController.
    /// </summary>
    /// <param name="contactService">contactService.</param>
    /// <param name="tokenModel"></param>
    public ContactController(IContactService contactService, IOptions<TokenModel> tokenModel, IBlobStorageManager blobStorageManager)
    {
        _contactService = contactService;
        _tokenModel = tokenModel!.Value;
        _blobStorageManager = blobStorageManager;
    }

    /// <summary>
    /// Allow updating contact informations for Pulse.
    /// </summary>
    /// <param name="token">Security token ensuring the caller is legit.</param>
    /// <param name="data">Data containing the contact information in CSV format.</param>
    /// <returns></returns>
    [HttpPost("update")]
    [Consumes("application/csv")]
    public async Task<IActionResult> UpdateAsync([FromQuery] string token, [FromBody] string data)
    {
        try
        {
            await _blobStorageManager.SaveFileAsync("Contact", data);
        }
        catch (BlobStorageOperationException ex)
        {
            return BadRequest($"Something went wrong when saving received csv {ex.InnerException}");
        }

        List<(RefContactCsv, int, string[])> csvDatas = [];

        if (string.IsNullOrWhiteSpace(token) || !_tokenModel.Token.Equals(token))
        {
            return new UnauthorizedObjectResult("Invalid token.");
        }


        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(data)))
        {

            try
            {
                csvDatas = CsvFileReader.ReadStreamAsync<RefContactCsv>(stream).ToList();
            }
            catch (HeaderValidationException)
            {
                return BadRequest("Invalid data: Missing columns in header");
            }

            if (!CsvConfig.IsValidCsvFormat(csvDatas, typeof(RefContactCsv), out var messageError))
            {
                return BadRequest("Invalid data: " + messageError);
            }

        }

        // Validation des contacts
        var contacts = csvDatas.Select(d => d.Item1);
        var result = new ValidationHelper<RefContactCsv>().Validate(contacts);

        if (result.ValidateModels.Count == 0)
        {
            return BadRequest($"Csv Contacts retreval process unsuccessuf with errors: {JsonConvert.SerializeObject(result.Errors)}");
        }
        else
        {
            await _contactService.InsertContactsAsync(result.ValidateModels);

            return result.Errors.Count != 0
                ? BadRequest($"Csv Contacts retreval process success with errors: {JsonConvert.SerializeObject(result.Errors)}")
                : Ok($"Csv Contacts retreval process was completed");
        }
    }
}
