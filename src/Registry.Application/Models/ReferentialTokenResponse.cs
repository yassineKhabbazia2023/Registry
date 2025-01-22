// <copyright file="ReferentialTokenResponse.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models;

using Newtonsoft.Json;
public class ReferentialTokenResponse
{
    [JsonProperty("token_type")]
    public required string TokenType { get; set; }

    [JsonProperty("scope")]
    public required string Scope { get; set; }

    [JsonProperty("expires_in")]
    public required int ExpiresIn { get; set; }

    [JsonProperty("ext_expires_in")]
    public required string ExtExpiresIn { get; set; }

    [JsonProperty("access_token")]
    public required string AccessToken { get; set; }
}

