// <copyright file="CreRole.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models
{
    public class CreRole
    {
        public Guid Id { get; set; }

        public Guid ContactId { get; set; }

        public Guid AccountId { get; set; }

        public DateTime? Deleted { get; set; }
    }
}
