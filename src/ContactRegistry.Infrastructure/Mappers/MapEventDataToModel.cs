// <copyright file="MapEventDataToModel.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using Azure;
using Domain.Constants;
using Domain.Constants.Enums;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using Pulse.ContactRegistry.Domain.Constants;

namespace Infrastructure.Mappers;

public static class MapEventDataToModel
{
    public static RoleRegistry RoleEventCreatedDataToModel(this RoleCreatedEventData source)
    {
        if (source == null)
        {
            return null!;
        }

        return new RoleRegistry
        {
            ContactCode = source.ContactId.ToString(),
            ContactEmailOffice = source.ContactEmail,
            AccountNumber = source.AccountNumber,
            RoleFlagStatus = (int)FlagStatusEnum.ENABLED,
            RoleFunctionDescription = string.Empty,
            RoleSourceName = GlobalConstants.SOURCENAME,
        };
    }

    public static RoleRegistry RoleEventDeletedDataToModel(this RoleDeletedEventData source)
    {
        if (source == null)
        {
            return null!;
        }

        return new RoleRegistry
        {
            ContactCode = source.ContactId.ToString(),
            ContactEmailOffice = source.ContactEmail,
            AccountNumber = source.AccountNumber,
            RoleFlagStatus = (int)FlagStatusEnum.DISABLED,
            RoleFunctionDescription = string.Empty,
            RoleSourceName = GlobalConstants.SOURCENAME,
        };
    }

    public static DeploymentPlanningRegistry AccountEventDataToModel(this AccountStateEventData source)
    {
        if (source == null)
        {
            return null!;
        }

        return new DeploymentPlanningRegistry
        {
            AccountNumber = source.AccountNumber,
            DeploymentStatus = Enum.IsDefined(typeof(DeploymentStatusEnum), source.Status) ? source.Status : DeploymentStatusEnum.ToDeploy.ToString(),
            PlanningDeploymentDate = DateTime.UtcNow,
            DateDeployment = DateTime.UtcNow,
            HasVault = 0,
            DeploymentPlanningFlagStatus = (int)FlagStatusEnum.ENABLED
        };
    }
}