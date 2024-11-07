// <copyright file="RoleRegistryProvider.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models;
using Newtonsoft.Json;
using System.Text;

namespace Infrastructure.Providers;

public class RoleRegistryProvider : IRoleRegistryProvider
{
    private readonly IHttpClientFactory _httpClientFactory;

    public RoleRegistryProvider(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task CreateRoleAsync(RoleRegistry role)
    {
        var url = string.Concat($"");
        var json = JsonConvert.SerializeObject(role);
        var httpClient = _httpClientFactory.CreateClient("RegistryApi");
        var content = new StringContent(json, encoding: Encoding.UTF8, mediaType: "application/json");

        await httpClient.PostAsync(url, content);
    }

    public async Task UpdateRoleAsync(RoleRegistry role)
    {
        var url = string.Concat($"");
        var json = JsonConvert.SerializeObject(role);
        var httpClient = _httpClientFactory.CreateClient("RegistryApi");
        var content = new StringContent(json, encoding: Encoding.UTF8, mediaType: "application/json");

        await httpClient.PutAsync(url, content);
    }
}
