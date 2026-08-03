// <copyright file="RefInvoiceCsv.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Validations;
using CsvHelper.Configuration.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Application.Models;

/// <summary>
/// Exploited columns of the Seres invoices csv (DS2I).
/// </summary>
public class RefInvoiceCsv
{
    [Name("client_code")]
    [Required(ErrorMessage = "AccountNumber is required")]
    public string? AccountNumber { get; set; }

    [Name("item_number")]
    [Required(ErrorMessage = "InvoiceNumber is required")]
    public string? InvoiceNumber { get; set; }

    [Name("item_date_issue")]
    [Required(ErrorMessage = "InvoiceDate is required")]
    [ValidateDateFormat("dd/MM/yyyy")]
    public string? InvoiceDate { get; set; }

    [Name("item_field_perso15")]
    [Required(ErrorMessage = "DocumentPath is required")]
    public string? DocumentPath { get; set; }

    [Name("operation")]
    [Required(ErrorMessage = "Operation is required")]
    [ValidateOperation("INSERT|DELETE")]
    public string? Operation { get; set; }
}
