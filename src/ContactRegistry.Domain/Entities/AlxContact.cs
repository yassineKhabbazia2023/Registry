// <copyright file="CreContactEntity.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>


namespace Domain.Entities;

public class AlxContact
{
    public Guid Id { get; set; }

    public Guid? OfficeId { get; set; }

    public bool IsCustomer { get; set; }

    public bool IsActive { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string Email { get; set; }

    public string LandPhone { get; set; }

    public string MobilePhone { get; set; }

    public string JobDescription { get; set; }

    public ICollection<AlxRole> Roles { get; set; }
}