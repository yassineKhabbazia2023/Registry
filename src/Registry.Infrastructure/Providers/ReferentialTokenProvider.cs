// <copyright file="ReferentialTokenProvider.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models;
using Newtonsoft.Json;

namespace Application.Providers;
public class ReferentialTokenProvider : IReferentialTokenProvider
{
    private readonly IHttpClientFactory httpClientFactory;

    public ReferentialTokenProvider(IHttpClientFactory httpClientFactory)
    {
        this.httpClientFactory = httpClientFactory;
    }

    public async Task<ReferentialTokenResponse?> GenerateTokenAsync()
    {
        ReferentialTokenResponse? referentialTokenResponse = null;
        string url = string.Empty;
        var request = this.httpClientFactory.CreateClient("ReferentialToken");
        var response = await request.PostAsync(url, null);

        if (response.IsSuccessStatusCode)
        {
            referentialTokenResponse = JsonConvert.DeserializeObject<ReferentialTokenResponse>(await response.Content.ReadAsStringAsync());
        }

        return referentialTokenResponse;
    }
}


