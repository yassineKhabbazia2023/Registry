// <copyright file="Offer.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;

namespace Application.Models
{
    public class Offer
    {
        [Required(ErrorMessage = "AccountNumber is required")]
        public required string AccountNumber { get; set; }

        public string? ClientEmail { get; set; }

        public string? CollaboratorEmail { get; set; }

        public string? MissionLeaderEmail { get; set; }

        public string? AccountingExpertEmail { get; set; }

        [Required(ErrorMessage = "OfferName is required")]
        public required string OfferName { get; set; }

        [Required(ErrorMessage = "MigrationStatus is required")]
        public required string MigrationStatus { get; set; }
    }
}
