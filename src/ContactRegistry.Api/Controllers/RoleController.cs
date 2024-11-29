// <copyright file="RoleController.cs" company="Pulse">
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
/// RoleController.
/// </summary>
[ApiController]
[Route("api/role")]
public class RoleController : ControllerBase
{
    private readonly IRoleService _roleService;
    private readonly TokenModel _tokenModel;

    /// <summary>
    /// RoleController.
    /// </summary>
    /// <param name="roleService">roleService.</param>
    /// <param name="tokenModel"></param>
    public RoleController(IRoleService roleService, IOptions<TokenModel> tokenModel)
    {
        _roleService = roleService;
        _tokenModel = tokenModel!.Value;
    }

    /// <summary>
    /// UploadAsync.
    /// </summary>
    /// <param name="file">csv file.</param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> UploadAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("Invalid file.");
        }

        var stream = file.OpenReadStream();

        var roles = CsvFileReader.ReadStreamAsync<RoleCsv>(stream);

        await _roleService.ProcessRoleAsync(roles);

        return Ok("File processed successfully.");
    }

    /// <summary>
    /// Allow updating role informations for Pulse.
    /// </summary>
    /// <param name="token">Security token ensuring the caller is legit.</param>
    /// <param name="data">Data containing the role information in CSV format.</param>
    /// <returns></returns>
    [HttpPost("update")]
    [Consumes("application/csv")]
    public async Task<IActionResult> UpdateAsync([FromQuery] string token, [FromBody] string data)
    {
        if (string.IsNullOrWhiteSpace(token) || !_tokenModel.Token.Equals(token))
        {
            return new UnauthorizedObjectResult("Invalid token.");
        }

        if (!CsvConfig.IsValidCsvFormat(data, typeof(RefRoleCsv), out var messageError))
        {
            return BadRequest("Invalid data: " + messageError);
        }

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(data));

        var roles = CsvFileReader.ReadStreamAsync<RefRoleCsv>(stream);
        await _roleService.InsertRolesAsync(roles);

        return Ok("Execution processed successfully.");
    }

    /// <summary>
    /// ExportDataAsync.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [HttpGet("export")]
    public async Task<IActionResult> ExportDataAsync()
    {
        var accountsStream = await JsonStreamHelper.CreateJsonStreamAsync(_roleService.StreamRolesJsonAsync);
        return File(accountsStream, "application/json", "CreRoles.json");
    }
}
