// <copyright file="ContactController.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Helpers;
using Application.Interfaces;
using Application.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text;
using WebApi.Configurations.Models;

namespace ContactRegistry.WebApi.Controllers;

/// <summary>
/// ContactController.
/// </summary>
[ApiController]
[Route("api/contact")]
public class ContactController : ControllerBase
{
    private readonly IContactService _contactService;
    private readonly TokenModel _tokenModel;

    /// <summary>
    /// ContactController.
    /// </summary>
    /// <param name="contactService">contactService.</param>
    /// <param name="tokenModel"></param>
    public ContactController(IContactService contactService, IOptions<TokenModel> tokenModel)
    {
        _contactService = contactService;
        _tokenModel = tokenModel!.Value;
    }

    /// <summary>
    /// UploadAsync.
    /// </summary>
    /// <param name="file">csv file</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [HttpPost]
    public async Task<IActionResult> UploadAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("Invalid file.");
        }

        var stream = file.OpenReadStream();

        var contacts = CsvFileReader.ReadStreamAsync<ContactCsv>(stream);

        await _contactService.ProcessContactAsync(contacts);

        return Ok("File processed successfully.");
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
        if (string.IsNullOrWhiteSpace(token) || !_tokenModel.Token.Equals(token))
        {
            return new UnauthorizedObjectResult("Invalid token.");
        }

        if (!CsvConfig.IsValidCsvFormat(data, out var messageError))
        {
            return BadRequest("Invalid data" + messageError);
        }

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(data));

        var contacts = CsvFileReader.ReadStreamAsync<RefContactCsv>(stream);
        await _contactService.InsertContactsAsync(contacts);

        return Ok("Execution processed successfully.");
    }

    /// <summary>
    /// ExportDataAsync.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [HttpGet("export")]
    public async Task<IActionResult> ExportDataAsync()
    {
        var accountsStream = await JsonStreamHelper.CreateJsonStreamAsync(_contactService.StreamContactsJsonAsync);
        return File(accountsStream, "application/json", "CreContacts.json");
    }
}
