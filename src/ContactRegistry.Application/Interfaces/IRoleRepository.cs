// <copyright file="IRoleRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;

namespace Application.Interfaces;

public interface IRoleRepository
{ 

    Task AddRolesAsync(IEnumerable<RefRoleCsv> roles);

}
