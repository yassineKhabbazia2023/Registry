// <copyright file="AkuiteoAccountOperationResponse.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models.Results;

/// <summary>
/// Represents an Akuiteo account operation response containing metadata only.
/// </summary>
public class AkuiteoAccountOperationResponse
{
    /// <summary>
    /// Gets or sets the Akuiteo response metadata.
    /// </summary>
    public AkuiteoMetaResponse? Meta { get; set; }
}
