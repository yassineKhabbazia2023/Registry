// <copyright file="ContactCsv.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Validations;
using System.ComponentModel.DataAnnotations;

namespace Application.Models;

public class RefContactCsv 
{
    [Required(ErrorMessage ="Contact Flag Status is required")]
    [Range(0,1,ErrorMessage ="Contact Flag Status should be 0 or 1")]
    public int ContactFlagStatus { get; set; }

    [Required(ErrorMessage = "Email is required")]
    [StringLength(255, ErrorMessage = "Email should not exceed 255 characters")]
    [ValidateEmail]
    public required string Email { get; set; }

    [StringLength(255, ErrorMessage = "First Name should not exceed 255 characters")]
    public string? FirstName { get; set; }

    [StringLength(255, ErrorMessage = "Last Name should not exceed 255 characters")]
    public string? LastName { get; set; }

    public bool? IsCustomer { get; set; }

    [DataType(DataType.PhoneNumber, ErrorMessage = "This is not a valid phone number!")]
    [StringLength(255, ErrorMessage = "Land Phone should not exceed 255 characters")]
    public string? LandPhone { get; set; }

    [StringLength(255, ErrorMessage = "Mobile Phone should not exceed 255 characters")]
    public string? MobilePhone { get; set; }

    [DataType(DataType.PhoneNumber, ErrorMessage = "This is not a valid phone number!")]
    public string? JobDescription { get; set; }

    [ValidateOfficeCode]
    [StringLength(50,ErrorMessage = "Office Code should not exceed 255 characters")]
    public string? OfficeCode { get; set; }

    [ValidateOperation]
    public required string Operation { get; set; }

}
