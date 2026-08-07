// <copyright file="IMissionReceptionService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models.Results;

namespace Application.Interfaces;

public interface IMissionReceptionService
{
    Task<MissionCsvReceptionOutcome> ReceiveAsync(string data);
}
