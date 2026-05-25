// <copyright file="AkuiteoContactCreationRequest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;

namespace Application.Requests;

/// <summary>
/// Represents the payload used to create a contact in Akuiteo from Registry.
/// </summary>
public class AkuiteoContactCreationRequest
{
    /// <summary>
    /// Gets or sets the account number of the target Akuiteo customer.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string? AccountNumber { get; set; }

    /// <summary>
    /// Gets or sets the contact title.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    [RegularExpression("^(M|Mme|Dr|Pr)$")]
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the contact last name.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string? LastName { get; set; }

    /// <summary>
    /// Gets or sets the contact first name.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets or sets the contact job title.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string? JobTitle { get; set; }

    /// <summary>
    /// Gets or sets the contact department code.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string? ContactDepartment { get; set; }

    /// <summary>
    /// Gets or sets the company role.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string? CompanyRole { get; set; }

    /// <summary>
    /// Gets or sets the contact type flags.
    /// </summary>
    [Required]
    public AkuiteoContactTypesRequest? ContactTypes { get; set; }

    /// <summary>
    /// Gets or sets the contact email address.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    [EmailAddress]
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the contact mobile phone number.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string? MobilePhone { get; set; }
}
