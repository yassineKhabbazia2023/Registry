// <copyright file="AkuiteoBankingInformationResponse.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Requests;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Application.Models.Results;

/// <summary>
/// Represents banking information returned by Akuiteo for a customer account.
/// </summary>
public class AkuiteoBankingInformationResponse
{
    /// <summary>Gets or sets the Akuiteo banking-information identifier.</summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Include)]
    public string? Id { get; set; }

    /// <summary>Gets or sets the SEPA banking information.</summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Include)]
    public AkuiteoSepaRequest? Sepa { get; set; }

    /// <summary>Gets or sets the non-SEPA banking information in its downstream JSON shape.</summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Include)]
    public JToken? NoneSepa { get; set; }

    /// <summary>Gets or sets the status-change date.</summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Include)]
    public string? StatusChangeDate { get; set; }

    /// <summary>Gets or sets the status-change details.</summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Include)]
    public AkuiteoStatusChangeArgumentResponse? StatusChangeArgument { get; set; }

    /// <summary>Gets or sets the validator in its downstream JSON shape.</summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Include)]
    public JToken? Validator { get; set; }

    /// <summary>Gets or sets the validator identifier.</summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Include)]
    public string? ValidatorId { get; set; }

    /// <summary>Gets or sets the banking-information action when returned.</summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Include)]
    public string? Action { get; set; }

    /// <summary>Gets or sets the banking-information type.</summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Include)]
    public string? Type { get; set; }
}
