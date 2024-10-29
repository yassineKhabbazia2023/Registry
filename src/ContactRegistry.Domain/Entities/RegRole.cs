// <copyright file="RegRole.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Domain.Entities;

public class RegRole
{
    public Guid RoleId { get; set; }

    public required string ContactEmail { get; set; }

    public required string AccountNumber { get; set; }

    public bool? Onboarded { get; set; }

    public DateTime? Deleted { get; set; }

    public string? RoleDelegataireEmail { get; set; }

    public bool? RoleSignatory { get; set; }

    public bool? IsFavorite { get; set; }

    public bool RoleFlagStatus { get; set; }

    public string? Description { get; set; }

    public CreContact Contact { get; set; }

    public CreAccount Account { get; set; }
}