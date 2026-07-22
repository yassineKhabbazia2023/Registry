// <copyright file="AkuiteoBicRequest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Requests;

/// <summary>
/// Represents the BIC components sent to Akuiteo.
/// </summary>
public class AkuiteoBicRequest
{
    /// <summary>Gets or sets the BIC country code.</summary>
    public string? Country { get; set; }

    /// <summary>Gets or sets the BIC bank code.</summary>
    public string? Bank { get; set; }

    /// <summary>Gets or sets the BIC location code.</summary>
    public string? Location { get; set; }

    /// <summary>Gets or sets the BIC branch code.</summary>
    public string? Branch { get; set; }
}
