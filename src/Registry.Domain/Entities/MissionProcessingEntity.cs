// <copyright file="MissionProcessingEntity.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Pulse.Registry.Domain.Entities;

public partial class MissionProcessingEntity
{
    public int RegistryMissionId { get; set; }

    public string Status { get; set; } = null!;

    public string? Reason { get; set; }

    public DateTime? PublishedOn { get; set; }

    public DateTime? ProcessedOn { get; set; }

    public virtual MissionEntity? Mission { get; set; }
}
