// <copyright file="AkuiteoCustomerCreationResponse.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models.Results;

/// <summary>
/// Represents the Registry response returned after a customer creation in Akuiteo.
/// </summary>
public class AkuiteoCustomerCreationResponse
{
    /// <summary>
    /// Gets or sets the Akuiteo account number.
    /// </summary>
    public required string AccountNumber { get; set; }
}
