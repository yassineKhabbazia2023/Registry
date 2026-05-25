// <copyright file="AkuiteoContactCreationDataResponse.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models.Results;

/// <summary>
/// Represents the Akuiteo contact-creation response data.
/// </summary>
public class AkuiteoContactCreationDataResponse
{
    /// <summary>
    /// Gets or sets the created contact identifier.
    /// </summary>
    public string? ContactId { get; set; }
}
