// <copyright file="MissionEntity.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Pulse.Registry.Domain.Entities;

public partial class MissionEntity
{
    /// <summary>
    /// Surrogate key of the engagement line, private to Registry: it keys mission.Missions
    /// to mission.Processing and never leaves the service. Offer keys its own offer.Mission
    /// with an independent MissionId, which is why this one is named for its owner.
    /// </summary>
    public int RegistryMissionId { get; set; }

    public string AccountNumber { get; set; } = null!;

    public string EngagementCode { get; set; } = null!;

    public string OfferCode { get; set; } = null!;

    public string? ProductCode { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    /// <summary>
    /// Operation carried by the CSV line: INSERT or DELETE. One row per operation, so the same
    /// engagement code can come back as a DELETE months after its INSERT.
    /// </summary>
    public string Operation { get; set; } = null!;

    public DateTime CreatedOn { get; set; }
}
