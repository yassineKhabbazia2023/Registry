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
    /// Allow updating contact informations for Pulse.
    /// </summary>
    /// <param name="token">Security token ensuring the caller is legit.</param>
    /// <param name="data">Data containing the contact information in CSV format.</param>
    /// <returns></returns>
    [HttpPost("update")]
    [Consumes("application/csv")]
    public async Task<IActionResult> UpdateAsync([FromQuery] string token, [FromBody] string data)
    {
        List<RefContactCsv> contacts = new List<RefContactCsv>();

        if (string.IsNullOrWhiteSpace(token) || !_tokenModel.Token.Equals(token))
        {
            return new UnauthorizedObjectResult("Invalid token.");
        }

        if (!CsvConfig.IsValidCsvFormat(data, typeof(RefContactCsv), out var messageError))
        {
            return BadRequest("Invalid data: " + messageError);
        }

        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(data)))
        {
            contacts = CsvFileReader.ReadStreamAsync<RefContactCsv>(stream).ToList();
        }

        // Validation des contacts
        var validationErrors = _contactService.ValidateContacts(contacts);
        if (validationErrors.Any())
        {
            return BadRequest(new
            {
                Message = "Validation failed.",
                Errors = validationErrors
            });
        }

        await _contactService.InsertContactsAsync(contacts);

        return Ok("Execution processed successfully.");
    }
}
