// <copyright file="AkuiteoStatusChangeArgumentResponse.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Newtonsoft.Json;

namespace Application.Models.Results;

/// <summary>
/// Represents banking status-change details returned by Akuiteo.
/// </summary>
public class AkuiteoStatusChangeArgumentResponse
{
    /// <summary>Gets or sets the status-change comment.</summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Include)]
    public string? Comment { get; set; }

    /// <summary>Gets or sets the banking-information status.</summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Include)]
    public string? Status { get; set; }
}
