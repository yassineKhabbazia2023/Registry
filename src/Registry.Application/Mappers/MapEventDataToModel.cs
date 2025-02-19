// <copyright file="MapEventDataToModel.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using Application.Models.Contacts;
using Azure;
using Domain.Constants;
using Domain.Constants.Enums;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using Pulse.Registry.Domain.Constants;
using System.Runtime.CompilerServices;

namespace Application.Mappers;

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

    public static Application.Models.ContactRegistry ContactStateEventDataToModel(this ContactStateEventData contactStateEvent)
    {

        ArgumentNullException.ThrowIfNull(contactStateEvent, nameof(contactStateEvent));
        ArgumentException.ThrowIfNullOrEmpty(contactStateEvent.Email, nameof(contactStateEvent.Email));
        ArgumentException.ThrowIfNullOrEmpty(contactStateEvent.Type, nameof(contactStateEvent.Type));   
        ArgumentException.ThrowIfNullOrEmpty(contactStateEvent.Source,nameof(contactStateEvent.Source));

        return new Application.Models.ContactRegistry
        {
            ContactEmailOffice = contactStateEvent.Email,
            ContactFullName = $"{contactStateEvent.FirstName} {contactStateEvent.LastName}",
            ContactFirstName = contactStateEvent.FirstName,
            ContactLastName = contactStateEvent.LastName,
            ContactFlagStatus = contactStateEvent.IsActive ? 1 : 0,
            ContactSourceName = contactStateEvent.Source,
            ContactPhoneMobileOffice = contactStateEvent.MobilePhone,
            ContactPhoneLandLine = contactStateEvent.LandPhone,
            ContactFunctionDescription = contactStateEvent.Type,
        };
    }

    public static Contact ContactStateEventDataToContactModel(this ContactStateEventData contactStateEventData)
    {
        return new Contact()
        {
            ContactGlobalUniqueId = contactStateEventData.ContactGlobalUniqueId,
            ContactId = contactStateEventData.ContactId,
            Email = contactStateEventData.Email,
            Type = contactStateEventData.Type,
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
            DeploymentDate = DateTime.UtcNow
        };
    }
}