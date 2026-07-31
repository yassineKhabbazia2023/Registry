// <copyright file="AccountController.cs" company="Pulse">
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
/// AccountController.
/// </summary>
[ApiController]
[Route("api/account")]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly TokenModel _tokenModel;
    private readonly IBlobStorageManager _blobStorageManager;
    private readonly IValidationHelper<RefAccountCsv> validationHelper;
    private readonly IAccountDeepValidationService _accountDeepValidationService;

    /// <summary>
    /// AccountController.
    /// </summary>
    /// <param name="accountService">The account service.</param>
    /// <param name="tokenModel">The token options.</param>
    /// <param name="blobStorageManager">The blob storage manager.</param>
    /// <param name="validationHelper">The CSV validation helper.</param>
    public AccountController(
        IAccountService accountService,
        IOptions<TokenModel> tokenModel,
        IBlobStorageManager blobStorageManager,
        IValidationHelper<RefAccountCsv> validationHelper
    )
    {
        _accountService = accountService;
        _tokenModel = tokenModel!.Value;
        _blobStorageManager = blobStorageManager;
        this.validationHelper = validationHelper;
    }

    /// <summary>
    /// Allow updating account informations for Pulse.
    /// </summary>
    /// <param name="token">Security token ensuring the caller is legit.</param>
    /// <param name="data">Data containing the account information in CSV format.</param>
    /// <returns></returns>
    [HttpPost("update")]
    [Consumes("application/csv")]
    public async Task<IActionResult> UpdateAsync([FromQuery] string token, [FromBody] string data)
    {
        if (string.IsNullOrWhiteSpace(token) || !_tokenModel.Token.Equals(token))
        {
            return new UnauthorizedObjectResult("Invalid token.");
        }

        try
        {
            await _blobStorageManager.SaveFileAsync("Account", data);
        }
        catch (BlobStorageOperationException ex)
        {
            return BadRequest($"Something went wrong when saving received csv {ex.InnerException}");
        }

        List<(RefAccountCsv, int, string[])> csvDatas = [];

        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(data)))
        {
            try
            {
                csvDatas = CsvFileReader.ReadStreamAsync<RefAccountCsv>(stream).ToList();
            }
            catch (HeaderValidationException)
            {
                return BadRequest("Invalid data: Missing columns in header");
            }

            if (!CsvConfig.IsValidCsvFormat(csvDatas, typeof(RefAccountCsv), out var messageError))
            {
                return BadRequest("Invalid data: " + messageError);
            }
        }

        var accounts = csvDatas.Select(d => d.Item1);
        var result = validationHelper.Validate(accounts);

        if (result.ValidateModels.Count == 0)
        {
            return BadRequest($"Csv Accounts retreval process unsuccessuf with errors: {JsonConvert.SerializeObject(result.Errors)}");
        }
        else
        {
            await _accountService.InsertAccountsAsync(result.ValidateModels);

            return result.Errors.Count != 0
                ? BadRequest($"Csv Accounts retreval process success with errors: {JsonConvert.SerializeObject(result.Errors)}")
                : Ok($"Csv Accounts retreval process was completed");
        }
    }
}
