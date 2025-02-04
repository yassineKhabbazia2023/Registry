// <copyright file="AccountController.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Helpers;
using Application.Interfaces;
using Application.Models;
using Application.Services;
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
    /// Allow updating account informations for Pulse.
    /// </summary>
    /// <param name="token">Security token ensuring the caller is legit.</param>
    /// <param name="data">Data containing the account information in CSV format.</param>
    /// <returns></returns>
    [HttpPost("update")]
    [Consumes("application/csv")]
    public async Task<IActionResult> UpdateAsync([FromQuery] string token, [FromBody] string data)
    {
        List<RefAccountCsv> accounts = [];

        if (string.IsNullOrWhiteSpace(token) || !_tokenModel.Token.Equals(token))
        {
            return new UnauthorizedObjectResult("Invalid token.");
        }

        if (!CsvConfig.IsValidCsvFormat(data, typeof(RefAccountCsv), out var messageError))
        {
            return BadRequest("Invalid data: " + messageError);
        }

        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(data)))
        {
            accounts = CsvFileReader.ReadStreamAsync<RefAccountCsv>(stream).ToList();
        }

        var result = new ValidationHelper<RefAccountCsv>().Validate(accounts);

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
