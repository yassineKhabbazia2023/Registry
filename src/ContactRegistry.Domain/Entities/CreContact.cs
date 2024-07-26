// <copyright file="CreContactEntity.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>


namespace Domain.Entities;

public class CreContact
{
    public Guid Id { get; set; }

    public Guid? OfficeId { get; set; }

    public bool IsCustomer { get; set; }

    public bool IsActive { get; set; }

    public DateTime? Updated { get; set; }

    public DateTime? Deleted { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string Email { get; set; }

    public string? LandPhone { get; set; }

    public string? MobilePhone { get; set; }

    public string? JobDescription { get; set; }

    public string? Source { get; set; }

    public ICollection<CreRole> Roles { get; set; }
}