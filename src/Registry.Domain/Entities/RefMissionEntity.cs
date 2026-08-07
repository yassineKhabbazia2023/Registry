// <copyright file="RefMissionEntity.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Pulse.Registry.Domain.Entities;

public partial class RefMissionEntity
{
    public Guid EntityId { get; set; }

    public string AccountNumber { get; set; } = null!;

    public string EngagementCode { get; set; } = null!;

    public string OfferCode { get; set; } = null!;

    public string? ProductCode { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string OperationType { get; set; } = null!;

    public DateTime OperationDate { get; set; }

    public DateTime? ValidationDate { get; set; }
}
