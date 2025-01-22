// <copyright file="IAccountRegistryProvider.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;

namespace Application.Interfaces;

public interface IAccountRegistryProvider
{
    Task<HttpResponseMessage> CreateDeploymentAsync(DeploymentPlanningRegistry deployment);

    Task<HttpResponseMessage> UpdateDeploymentAsync(DeploymentPlanningRegistry deployment);
}
