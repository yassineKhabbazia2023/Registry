// <copyright file="HubSpotSubmissionInputRequest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using System.Text.Json.Serialization;

using System.ComponentModel.DataAnnotations;

namespace Application.Requests;

public class HubSpotSubmissionInputRequest
{
    [EmailAddress]
    public string? DematerializationEmail { get; set; }

    [Required(AllowEmptyStrings = false)]
    public string? FirstName { get; set; }

    [Required(AllowEmptyStrings = false)]
    public string? LastName { get; set; }

    [EmailAddress]
    public string? VaultEmail { get; set; }

    [EmailAddress]
    public string? RequesterEmail { get; set; }

    public DateTime? SubmittedAt { get; set; }
}
