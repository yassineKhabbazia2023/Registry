// <copyright file="RoleRegistry.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models;

public partial class RoleRegistry
{
    public int AccountId { get; set; }

    public int ContactId { get; set; }

    public bool? IsFavorite { get; set; }

    public bool? IsSignatory { get; set; }

    public bool? IsDelegation { get; set; }

    public DateTime CreationDate { get; set; }

    public DateTime? LastUpdateDate { get; set; }
}