// <copyright file="AkuiteoCustomerCreationDataResponse.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models.Results;

/// <summary>
/// Represents the Akuiteo customer-creation response data.
/// </summary>
public class AkuiteoCustomerCreationDataResponse
{
    /// <summary>
    /// Gets or sets the created Akuiteo account number.
    /// </summary>
    public string? AccountNumber { get; set; }
}
