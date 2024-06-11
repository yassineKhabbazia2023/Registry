// <copyright file="IRoleRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Domain.Entities;

namespace Application.Interfaces
{
    public interface IRoleRepository
    {
        Task AddRolesAsync(IEnumerable<AlxRole> roles);
        IAsyncEnumerable<CreRole> GetRolesAsync();
    }
}
