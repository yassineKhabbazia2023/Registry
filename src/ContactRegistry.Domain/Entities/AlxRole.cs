// <copyright file="CreRoleEntity.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Domain.Entities;

public class AlxRole
{
    public Guid RoleId { get; set; }

    public Guid ContactId { get; set; }

    public Guid AccountId { get; set; }

    public bool Onboarded { get; set; }

    public AlxContact? Contact { get; set; }

    public AlxAccount? Account { get; set; }
}