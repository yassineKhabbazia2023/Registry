// <copyright file="HubSpotSubmissionRequest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using System.Text.Json.Serialization;

namespace Application.Requests;

public class HubSpotSubmissionRequest
{
    [JsonPropertyName("fields")]
    public required IReadOnlyCollection<HubSpotFieldRequest> Fields { get; set; }
}
