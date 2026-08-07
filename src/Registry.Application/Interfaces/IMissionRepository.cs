// <copyright file="IMissionRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Pulse.Registry.Domain.Entities;

namespace Application.Interfaces;

public interface IMissionRepository
{
    Task AddMissionsAsync(IEnumerable<RefMissionEntity> missions);
}
