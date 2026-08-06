// <copyright file="AccountRegistryProvider.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Polly;
using Polly.Retry;
using Pulse.Registry.Domain.Constants;
using System.Text;

namespace Application.Providers;

// TODO: Legacy provider currently has no active call path. Document its historical
// RegistryApi dependency, then confirm whether it can be removed safely.
public class AccountRegistryProvider : IAccountRegistryProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AsyncRetryPolicy _retryPolicy;

    public AccountRegistryProvider(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        _retryPolicy = Policy
                .Handle<SqlException>()
                .WaitAndRetryAsync(
                    retryCount: 2,
                    sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(GlobalConstants.RETRYTIMESPAN));
    }

    public async Task<HttpResponseMessage> CreateDeploymentAsync(DeploymentPlanningRegistry deployment)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var json = JsonConvert.SerializeObject(deployment, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
            var httpClient = _httpClientFactory.CreateClient("RegistryApi");
            var content = new StringContent(json, encoding: Encoding.UTF8, mediaType: "application/json");

            return await httpClient.PostAsync(GlobalConstants.DEPLOYMENTPLANNINGACTION, content);
        });
    }

    public async Task<HttpResponseMessage> UpdateDeploymentAsync(DeploymentPlanningRegistry deployment)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var json = JsonConvert.SerializeObject(deployment, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
            var httpClient = _httpClientFactory.CreateClient("RegistryApi");
            var content = new StringContent(json, encoding: Encoding.UTF8, mediaType: "application/json");

            return await httpClient.PutAsync(GlobalConstants.DEPLOYMENTPLANNINGACTION, content);
        });
    }
}
