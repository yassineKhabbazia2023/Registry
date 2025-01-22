// <copyright file="TestingController.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>


namespace WebApi.Controllers;

using Application.Interfaces;
using Application.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// THIS CONTROLLER ONLY FOR TESTING 
/// WILL BE DELETED ONCE WE GO TO PROD 
/// </summary>

[ApiController]
[Route("api/test")]
[ExcludeFromCodeCoverage]
public class TestingController : ControllerBase
{

    private readonly IReferentialTokenProvider _tokenProvider;
    private readonly IContactRegistryProvider _contactRegistryProvider;
    private readonly IRoleRegistryProvider _roleRegistryProvider;
    private readonly IAccountRegistryProvider _accountRegistryProvider;

    public TestingController(IReferentialTokenProvider tokenProvider,
        IContactRegistryProvider contactRegistryProvider,
        IRoleRegistryProvider roleRegistryProvider,
        IAccountRegistryProvider accountRegistryProvider)
    {
        _tokenProvider = tokenProvider;
        _contactRegistryProvider = contactRegistryProvider;
        _roleRegistryProvider = roleRegistryProvider;
        _accountRegistryProvider = accountRegistryProvider;
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

    [HttpPost("ref/role/add")]
    public async Task<IActionResult> CreateRole(RoleRegistry roleRegistry)
    {
        return Ok(await _roleRegistryProvider.CreateRoleAsync(roleRegistry));
    }

    [HttpPut("ref/role/update")]
    public async Task<IActionResult> UpdateRole(RoleRegistry roleRegistry)
    {
        return Ok(await _roleRegistryProvider.UpdateRoleAsync(roleRegistry));
    }


    [HttpPost("ref/deploy/add")]
    public async Task<IActionResult> AddDeployment(DeploymentPlanningRegistry deploymentPlanningRegistry)
    {
        return Ok(await _accountRegistryProvider.CreateDeploymentAsync(deploymentPlanningRegistry));
    }

    [HttpPut("ref/deploy/update")]
    public async Task<IActionResult> UpdateDeployment(DeploymentPlanningRegistry deploymentPlanningRegistry)
    {
        return Ok(await _accountRegistryProvider.UpdateDeploymentAsync(deploymentPlanningRegistry));
    }



}

