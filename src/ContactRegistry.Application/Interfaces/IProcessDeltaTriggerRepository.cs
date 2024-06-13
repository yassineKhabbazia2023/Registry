// <copyright file="IProcessDeltaTriggerRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Domain.Entities;

namespace Application.Interfaces
{
    public interface IProcessDeltaTriggerRepository
    {
        Task<ProcessDeltaTrigger> GetProcessAsync();

        Task UpdateContactProcessAsync(bool state);

        Task UpdateAccountProcessAsync(bool state);

        Task UpdateRoleProcessAsync(bool state);
    }
}
