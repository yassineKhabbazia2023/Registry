// <copyright file="RoleCsv.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models
{
    /// <summary>
    /// RoleCsv.
    /// </summary>
    /// <param name="Id">AccountGlobalUniqueIdentifier.</param>
    /// <param name="ContactId">ContactId.</param>
    /// <param name="AccountId">AccountId.</param>
    /// <param name="Onboarded">Onboarded.</param>
    public class RoleCsv
    {
        public Guid RoleId { get; set; }

        public Guid ContactId { get; set; }

        public Guid AccountId { get; set; }

        public bool Onboarded { get; set; }

        public string RoleDelegataireEmail { get; set; }

        public bool RoleSignatory { get; set; }

        public bool IsFavorite { get; set; }
    }
}
