// <copyright file="CreRoleEntity.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>


using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class CreRole
{
    public Guid Id { get; set; }

    public Guid ContactId { get; set; }

    public Guid AccountId { get; set; }

    public DateTime? Deleted { get; set; }

    public CreContact Contact { get; set; }

    public CreAccount Account { get; set; }
}