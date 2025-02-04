// <copyright file="AccountCsv.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Validations;
using Application.Validations.Common;
using Domain.Constants.Enums;
using System.ComponentModel.DataAnnotations;

namespace Application.Models;

public class RefAccountCsv
{
    [Required(ErrorMessage = "Account Flag Status is required")]
    [Range(0, 1, ErrorMessage = "AccountFlagStatus must be either 0 or 1.")]
    public required int AccountFlagStatus { get; set; }

    [Required(ErrorMessage = "Account LegalName is required")]
    public required string LegalName { get; set; }

    [Required(ErrorMessage = "AccountNumber is required")]
    [AlphanumericAttribute]
    public required string AccountNumber { get; set; }

    public string? AccountCommercialName { get; set; }
    [ValidateAccountType]
    public string? AccountType { get; set; }

    public string? AccountEmail { get; set; }

    public string? AccountNafIdentifier { get; set; }

    public string? AccountSectorCode { get; set; }

    public string? AccountTaxeValeurAjoutee { get; set; }

    public string? AccountDeliveryEmail { get; set; }

    public string? AccountBillingEmail { get; set; }

    public string? AccountTaxationSystem { get; set; }

    public string? AccountSourceName { get; set; }

    public string? AccountISIN { get; set; }

    public string? AccountRegisterIdentification1 { get; set; }

    public string? AccountStaffSize { get; set; }

    public string? AccountDeliveryFax { get; set; }

    public string? AccountBillingFax { get; set; }

    public string? AccountTurnover { get; set; }

    public string? AccountRegimeFiscal { get; set; }

    public string? AccountTypeTenueComptable { get; set; }

    public string? AccountFormeJuridique { get; set; }

    public string? AccountStaffSizeSlice { get; set; }

    public string? AccountEscCategory { get; set; }

    public string? AccountCodeFormeJuridique { get; set; }

    public string? AccountInsertedDate { get; set; }

    public string? AccountUpdatedDate { get; set; }

    public string? DeliveryAddressLine1 { get; set; }

    public string? DeliveryAddressLine2 { get; set; }

    public string? DeliveryAddressLine3 { get; set; }

    public string? DeliveryCity { get; set; }

    public string? DeliveryZipCode { get; set; }

    public string? DeliveryCountry { get; set; }

    public string? DeliveryState { get; set; }

    public string? BillingAddressLine1 { get; set; }

    public string? BillingAddressLine2 { get; set; }

    public string? BillingAddressLine3 { get; set; }

    public string? BillingCity { get; set; }

    public string? BillingZipCode { get; set; }

    public string? BillingCountry { get; set; }

    public string? BillingState { get; set; }

    public string? AccountDeliveryPhone { get; set; }

    public string? AccountBillingPhone { get; set; }

    [ValidateOperation("INSERT|UPDATE|DELETE")]
    public required string Operation { get; set; }
}
