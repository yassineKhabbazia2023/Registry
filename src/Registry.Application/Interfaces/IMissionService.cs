// <copyright file="IMissionService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;

namespace Application.Interfaces;

public interface IMissionService
{
    Task SaveMissionsAsync(IEnumerable<MissionCsv> missions);
}

