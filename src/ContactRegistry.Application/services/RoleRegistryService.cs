// <copyright file="RoleRegistryService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models;
using Newtonsoft.Json;
using System.Text;
using System.Text.Json;

namespace Application.services;

public class RoleRegistryService : IRoleRegistryService
{
    private readonly HttpClient _httpClient;

    public RoleRegistryService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> DoesRoleExistAsync(RoleRegistry role)
    {
        var url = string.Concat($"");
        var response = await _httpClient.GetAsync(url);
        if (response.IsSuccessStatusCode)
        {
            var jsonString = await response.Content.ReadAsStringAsync();
            return true;
        }

        return false;
    }

    public async Task CreateRoleAsync(RoleRegistry role)
    {
        var url = string.Concat($"");
        var json = JsonConvert.SerializeObject(role);
        var content = new StringContent(json, encoding: Encoding.UTF8, mediaType: "application/json");

        await _httpClient.PostAsync(url, content);
    }
}
