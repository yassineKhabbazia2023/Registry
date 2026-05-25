// <copyright file="AkuiteoMetaResponse.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models.Results;

/// <summary>
/// Represents Akuiteo response metadata.
/// </summary>
public class AkuiteoMetaResponse
{
    /// <summary>
    /// Gets or sets the Akuiteo operation status.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Gets or sets the Akuiteo operation messages.
    /// </summary>
    public IEnumerable<AkuiteoMessageResponse>? Messages { get; set; }
}
