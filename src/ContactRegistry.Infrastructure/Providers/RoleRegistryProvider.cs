// <copyright file="RoleRegistryProvider.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using Pulse.ContactRegistry.Domain.Constants;
using System.Text;

namespace Infrastructure.Providers;

public class RoleRegistryProvider : IRoleRegistryProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AsyncRetryPolicy _retryPolicy;

    public RoleRegistryProvider(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        _retryPolicy = Policy
                .Handle<SqlException>()
                .WaitAndRetryAsync(
                    retryCount: 2,
                    sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(GlobalConstants.RETRYTIMESPAN));
    }

    public async Task<HttpResponseMessage> CreateRoleAsync(RoleRegistry role)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var url = "roles";
            var json = JsonConvert.SerializeObject(role);
            var httpClient = _httpClientFactory.CreateClient("RegistryApi");
            var content = new StringContent(json, encoding: Encoding.UTF8, mediaType: "application/json");

            return await httpClient.PostAsync(url, content);
        });
    }

    public async Task<HttpResponseMessage> UpdateRoleAsync(RoleRegistry role)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var url = "roles/pulse";
            var json = JsonConvert.SerializeObject(role);
            var httpClient = _httpClientFactory.CreateClient("RegistryApi");
            var content = new StringContent(json, encoding: Encoding.UTF8, mediaType: "application/json");

            return await httpClient.PutAsync(url, content);
        });
    }
}
