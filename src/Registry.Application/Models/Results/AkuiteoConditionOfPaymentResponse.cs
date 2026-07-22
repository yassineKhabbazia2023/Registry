// <copyright file="AkuiteoConditionOfPaymentResponse.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Newtonsoft.Json;

namespace Application.Models.Results;

/// <summary>
/// Represents one payment condition returned by Akuiteo.
/// </summary>
public class AkuiteoConditionOfPaymentResponse
{
    /// <summary>Gets or sets the payment-condition display code.</summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Include)]
    public string? Code { get; set; }

    /// <summary>Gets or sets the payment deadline.</summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Include)]
    public string? DeadLine { get; set; }

    /// <summary>Gets or sets the payment term.</summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Include)]
    public string? Term { get; set; }

    /// <summary>Gets or sets the payment day.</summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Include)]
    public int? Day { get; set; }

    /// <summary>Gets or sets the reference shown on bank statements.</summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Include)]
    public string? ReferenceOnBankStatement { get; set; }
}
