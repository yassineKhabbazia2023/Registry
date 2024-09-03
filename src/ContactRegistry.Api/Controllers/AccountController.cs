// <copyright file="AccountController.cs" company="Pulse">
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
/// AccountController.
/// </summary>
[ApiController]
[Route("api/account")]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly TokenModel _tokenModel;

    /// <summary>
    /// AccountController.
    /// </summary>
    /// <param name="accountService"></param>
    /// <param name="tokenModel"></param>
    public AccountController(IAccountService accountService, IOptions<TokenModel> tokenModel)
    {
        _accountService = accountService;
        _tokenModel = tokenModel!.Value;
    }

    /// <summary>
    /// UploadAsync.
    /// </summary>
    /// <param name="file">csv file.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("Invalid file.");
        }
        var stream = file.OpenReadStream();
        var accounts =  CsvFileReader.ReadStreamAsync<AccountCsv>(stream);

        await _accountService.ProcessAccountAsync(accounts);

        return Ok("File processed successfully.");
    }

    /// <summary>
    /// Allow updating account informations for Pulse.
    /// </summary>
    /// <param name="token">Security token ensuring the caller is legit.</param>
    /// <param name="data">Data containing the account information in CSV format.</param>
    /// <returns></returns>
    [HttpPost("update")]
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

        var accounts = CsvFileReader.ReadStreamAsync<AccountCsv>(stream);
        await _accountService.ProcessAccountAsync(accounts);

        return Ok("Execution processed successfully.");
    }

    /// <summary>
    /// ExportDataAsync.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [HttpGet("export")]
    public async Task<IActionResult> ExportDataAsync()
    {
        var accountsStream = await JsonStreamHelper.CreateJsonStreamAsync(_accountService.StreamAccountsJsonAsync);
        return File(accountsStream, "application/json", "CreAccounts.json");
    }
}
