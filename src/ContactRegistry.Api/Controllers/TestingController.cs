// <copyright file="TestingController.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>


namespace WebApi.Controllers;

using Application.Interfaces;
using Application.Models;
using Microsoft.AspNetCore.Mvc;
/// <summary>
/// THIS CONTROLLER ONLY FOR TESTING 
/// WILL BE DELETED ONCE WE GO TO PROD 
/// </summary>

[ApiController]
[Route("api/test")]
public class TestingController : ControllerBase
{

    private readonly IReferentialTokenProvider _tokenProvider;
    private readonly IContactRegistryProvider _contactRegistryProvider;

    public TestingController(IReferentialTokenProvider tokenProvider, IContactRegistryProvider contactRegistryProvider)
    {
        _tokenProvider = tokenProvider;
        _contactRegistryProvider = contactRegistryProvider;
    }


    [HttpGet("token")]
    public async Task<IActionResult> GenerateToken()
    {
        var token = await _tokenProvider.GenerateTokenAsync();
        return Ok(token);
    }


    [HttpPost("ref/contact/add")]
    public async Task<IActionResult> CreateContact(ContactRegistry contactRegistry)
    {
        return Ok(await _contactRegistryProvider.CreateContactAsync(contactRegistry));
    }

    [HttpPut("ref/contact/update")]
    public async Task<IActionResult> UpdateContact(ContactRegistry contactRegistry)
    {
        return Ok(await _contactRegistryProvider.UpdateContactAsync(contactRegistry));
    }


}

