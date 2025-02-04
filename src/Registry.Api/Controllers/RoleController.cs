// <copyright file="RoleController.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Exceptions;
using Application.Helpers;
using Application.Interfaces;
using Application.Models;
using Infrastructure.Managers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Text;
using WebApi.Configurations.Models;

namespace Registry.WebApi.Controllers;

/// <summary>
/// RoleController.
/// </summary>
[ApiController]
[Route("api/role")]
public class RoleController : ControllerBase
{
    private readonly IRoleService _roleService;
    private readonly TokenModel _tokenModel;
    private readonly IBlobStorageManager _blobStorageManager;

    /// <summary>
    /// RoleController.
    /// </summary>
    /// <param name="roleService">roleService.</param>
    /// <param name="tokenModel"></param>
    public RoleController(IRoleService roleService, IOptions<TokenModel> tokenModel, IBlobStorageManager blobStorageManager)
    {
        _roleService = roleService;
        _tokenModel = tokenModel!.Value;
        _blobStorageManager = blobStorageManager; 
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
        try
        {
            await _blobStorageManager.SaveFileAsync("Role", data);
        }
        catch (BlobStorageOperationException ex)
        {
            return BadRequest($"Something went wrong when saving received csv {ex.InnerException}");
        }

        List<RefRoleCsv> roles = [];

        if (string.IsNullOrWhiteSpace(token) || !_tokenModel.Token.Equals(token))
        {
            return new UnauthorizedObjectResult("Invalid token.");
        }

        if (!CsvConfig.IsValidCsvFormat(data, typeof(RefRoleCsv), out var messageError))
        {
            return BadRequest("Invalid data: " + messageError);
        }

        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(data)))
        {
            roles = CsvFileReader.ReadStreamAsync<RefRoleCsv>(stream).ToList();
        }

        // Validation Roles
        var result = new ValidationHelper<RefRoleCsv>().Validate(roles);

        if (result.ValidateModels.Count == 0)
        {
            return BadRequest($"Csv Roles retreval process unsuccessuf with errors: {JsonConvert.SerializeObject(result.Errors)}");
        }
        else
        {
            await _roleService.InsertRolesAsync(result.ValidateModels);
            
            return result.Errors.Count != 0  
                ? BadRequest($"Csv Roles retreval process success with errors: {JsonConvert.SerializeObject(result.Errors)}")
                : Ok($"Csv Roles retreval process was completed");
        }
    }
}
