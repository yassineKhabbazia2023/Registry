// <copyright file="RefOfferEntity.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Domain.Entities
{
    public class RefOfferEntity
    {
        public int Id { get; set; }
        public required string AccountNumber { get; set; }

        public string? ClientEmail { get; set; }

        public string? CollaboratorEmail { get; set; }

        public string? MissionLeaderEmail { get; set; }

        public string? AccountingExpertEmail { get; set; }

        public required string Offer { get; set; }

        public required string MigrationStatus { get; set; }

        public Guid BatchId { get; set; }
    }
}
