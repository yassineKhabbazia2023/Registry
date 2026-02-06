// <copyright file="RoleCsv.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Validations;
using Application.Validations.Common;
using CsvHelper.Configuration.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Application.Models;

public class RefRoleCsv
{
    [Required(ErrorMessage = "ContactEmail is required")]
    [ValidateEmail(ErrorMessage = "Format email invalid")]
    public required string ContactEmail { get; set; }

    [Required(ErrorMessage = "AccountNumber is required")]
    [Alphanumeric]
    public required string AccountNumber { get; set; }

    [Required(ErrorMessage = "RoleFlagStatus cannot be null")]
    [Range(0, 1, ErrorMessage = "RoleFlagStatus must be either 0 or 1.")]
    public int? RoleFlagStatus { get; set; }

    public bool? ContactFlagPortailFactures { get; set; }

    public string? Description { get; set; }

    [ValidateOperation("INSERT|DELETE")]
    public required string Operation { get; set; }

    [Ignore]
    public string? SubRole { get; set; }
}
