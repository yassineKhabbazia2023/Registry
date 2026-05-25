// <copyright file="AkuiteoApiResponse.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models.Results;

/// <summary>
/// Represents a standard Akuiteo API response.
/// </summary>
/// <typeparam name="TData">The Akuiteo response data type.</typeparam>
public class AkuiteoApiResponse<TData>
{
    /// <summary>
    /// Gets or sets the response metadata.
    /// </summary>
    public AkuiteoMetaResponse? Meta { get; set; }

    /// <summary>
    /// Gets or sets the response data.
    /// </summary>
    public TData? Data { get; set; }
}
