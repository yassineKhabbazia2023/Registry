// <copyright file="CreRole.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models
{
    public class CreRole
    {
        public Guid RoleId { get; set; }

        public Guid ContactId { get; set; }

        public Guid AccountId { get; set; }

        public bool Onboarded { get; set; }

        public DateTime? Deleted { get; set; }

        public string? RoleDelegataireEmail { get; set; }

        public bool? RoleSignatory { get; set; }

        public bool? IsFavorite { get; set; }
    }
}
