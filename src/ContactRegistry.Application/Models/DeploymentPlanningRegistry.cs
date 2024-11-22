// <copyright file="DeploymentPlanningRegistry.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Domain.Constants;

namespace Application.Models;

public partial class DeploymentPlanningRegistry
{
    public required string AccountNumber { get; set; }

    public string DeploymentStatus { get; set; } = DeploymentStatusEnum.ToDeploy.ToString();

    public DateTime? DeploymentDate { get; set; }  = DateTime.UtcNow;
}