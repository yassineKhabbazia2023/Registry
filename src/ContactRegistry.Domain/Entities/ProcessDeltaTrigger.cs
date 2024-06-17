// <copyright file="CreContactEntity.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Domain.Entities
{
    /// <summary>
    /// ProcessDeltaTrigger
    /// </summary>
    public class ProcessDeltaTrigger
    {
        /// <summary>
        /// Get or set Id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Get or set Account.
        /// </summary>
        public bool Account { get; set; }

        /// <summary>
        /// Get or set Role.
        /// </summary>
        public bool Role { get; set; }

        /// <summary>
        /// Get or set Contact.
        /// </summary>
        public bool Contact { get; set; }
    }
}
