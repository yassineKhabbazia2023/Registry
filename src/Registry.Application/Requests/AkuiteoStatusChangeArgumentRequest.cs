// <copyright file="AkuiteoStatusChangeArgumentRequest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Requests;

/// <summary>
/// Represents the status-change details sent with Akuiteo banking information.
/// </summary>
public class AkuiteoStatusChangeArgumentRequest
{
    /// <summary>Gets or sets the status-change comment.</summary>
    public string? Comment { get; set; }

    /// <summary>Gets or sets the banking-information status.</summary>
    public string? Status { get; set; }
}
