// <copyright file="DeploymentStatusEnum.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>


namespace Domain.Constants;

public enum DeploymentStatusEnum
{
    ToDeploy = 1,
    InProgress = 2,
    Connected = 3,
    Revoked = 4,
}
