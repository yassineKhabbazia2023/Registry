// <copyright file="AkuiteoContactCreationResponse.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models.Results;

/// <summary>
/// Represents the Registry response returned after a contact creation in Akuiteo.
/// </summary>
public class AkuiteoContactCreationResponse
{
    /// <summary>
    /// Gets or sets the Akuiteo contact identifier.
    /// </summary>
    public required string ContactId { get; set; }
}
