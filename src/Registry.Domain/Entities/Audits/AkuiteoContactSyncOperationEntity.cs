// <copyright file="AkuiteoContactSyncOperationEntity.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Pulse.Registry.Domain.Entities.Audits;

public class AkuiteoContactSyncOperationEntity
{
    public long Id { get; set; }

    public Guid SourceEventId { get; set; }

    public int AccountId { get; set; }

    public required string AccountNumber { get; set; }

    public required string AccountType { get; set; }

    public int ContactId { get; set; }

    public required string ContactEmail { get; set; }

    public bool? ContactFlagPortailFactures { get; set; }

    public bool? IsSignatory { get; set; }

    public required string Reason { get; set; }

    public required string Status { get; set; }

    public string? LastError { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? SentAt { get; set; }

    public string? AkuiteoContactId { get; set; }
}
