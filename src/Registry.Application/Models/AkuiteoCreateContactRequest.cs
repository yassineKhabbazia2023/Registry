// <copyright file="AkuiteoCreateContactRequest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models;

/// <summary>
/// Represents the payload expected by the Akuiteo contact-creation API.
/// </summary>
public class AkuiteoCreateContactRequest
{
    /// <summary>
    /// Gets or sets the target account number.
    /// </summary>
    public required string AccountNumber { get; set; }

    /// <summary>
    /// Gets or sets the contact title.
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Gets or sets the contact last name.
    /// </summary>
    public required string LastName { get; set; }

    /// <summary>
    /// Gets or sets the contact first name.
    /// </summary>
    public required string FirstName { get; set; }

    /// <summary>
    /// Gets or sets the contact job title.
    /// </summary>
    public required string JobTitle { get; set; }

    /// <summary>
    /// Gets or sets the contact department code.
    /// </summary>
    public required string ContactDepartment { get; set; }

    /// <summary>
    /// Gets or sets the company role.
    /// </summary>
    public required string CompanyRole { get; set; }

    /// <summary>
    /// Gets or sets the contact type flags.
    /// </summary>
    public required AkuiteoCreateContactTypesRequest ContactTypes { get; set; }

    /// <summary>
    /// Gets or sets the contact email address.
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// Gets or sets the contact mobile phone number.
    /// </summary>
    public required string MobilePhone { get; set; }
}
