// <copyright file="IRegProcessDeltaTriggerRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using Domain.Entities;

namespace Application.Interfaces;

public interface IRegProcessDeltaTriggerRepository
{
    Task<RegProcessDeltaTrigger?> GetProcessAsync();

    Task UpdateContactProcessAsync(bool state);

    Task UpdateAccountProcessAsync(bool state);

    Task UpdateRoleProcessAsync(bool state);
}
