// <copyright file="RefMissionCsv.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Validations;
using CsvHelper.Configuration.Attributes;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Application.Models;

public class RefMissionCsv : IValidatableObject
{
    [Required(ErrorMessage = "AccountNumber is required")]
    public string? AccountNumber { get; set; }

    [Required(ErrorMessage = "EngagementCode is required")]
    public string? EngagementCode { get; set; }

    [Required(ErrorMessage = "OfferCode is required")]
    public string? OfferCode { get; set; }

    public string? ProductCode { get; set; }

    [Required(ErrorMessage = "StartDate is required")]
    [ValidateDateFormat(CsvDateFormat.Referential)]
    public string? StartDate { get; set; }

    [Required(ErrorMessage = "EndDate is required")]
    [ValidateDateFormat(CsvDateFormat.Referential)]
    public string? EndDate { get; set; }

    [Name("Operations")]
    [Required(ErrorMessage = "Operation is required")]
    [ValidateOperation("INSERT|DELETE")]
    public string? Operation { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DateTime.TryParseExact(StartDate, CsvDateFormat.Referential, CultureInfo.InvariantCulture, DateTimeStyles.None, out var start)
            && DateTime.TryParseExact(EndDate, CsvDateFormat.Referential, CultureInfo.InvariantCulture, DateTimeStyles.None, out var end)
            && end <= start)
        {
            yield return new ValidationResult("EndDate must be later than StartDate", [nameof(EndDate)]);
        }
    }
}
