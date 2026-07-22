// <copyright file="AkuiteoBankingInformationRequest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Application.Requests;

/// <summary>
/// Represents one banking-information change sent to an Akuiteo account.
/// </summary>
public class AkuiteoBankingInformationRequest
{
    /// <summary>
    /// Gets or sets the SEPA banking information.
    /// </summary>
    public AkuiteoSepaRequest? Sepa { get; set; }

    /// <summary>
    /// Gets or sets the non-SEPA banking information as supplied by the caller.
    /// The downstream schema is pending confirmation, so the JSON structure is preserved without interpretation.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Include)]
    public JToken? NoneSepa { get; set; }

    /// <summary>
    /// Gets or sets the banking status-change date in the Akuiteo format.
    /// </summary>
    public string? StatusChangeDate { get; set; }

    /// <summary>
    /// Gets or sets the banking status-change details.
    /// </summary>
    public AkuiteoStatusChangeArgumentRequest? StatusChangeArgument { get; set; }

    /// <summary>
    /// Gets or sets the banking-information action.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    [RegularExpression("^(ADD|UPDATE|REMOVE)$")]
    public string? Action { get; set; }
}
