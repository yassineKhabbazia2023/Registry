// <copyright file="ContactCsv.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models;

public class RefContactCsv
{
    public required int ContactFlagStatus { get; set; }

    public required string Email { get; set; }

    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public bool IsCustomer { get; set; } = true;

    public string? LandPhone { get; set; }

    public string? MobilePhone { get; set; }

    public string? JobDescription { get; set; }

    public Guid? OfficeId { get; set; }

    public required string Operation { get; set; }
}
