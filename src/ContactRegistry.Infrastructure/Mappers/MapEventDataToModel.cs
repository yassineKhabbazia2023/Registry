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

    public static ContactRegistry ContactStateEventDataToModel(this ContactStateEventData contactStateEvent)
    {

        ArgumentNullException.ThrowIfNull(contactStateEvent, nameof(contactStateEvent));
        ArgumentException.ThrowIfNullOrEmpty(contactStateEvent.Email, nameof(contactStateEvent.Email));
        ArgumentException.ThrowIfNullOrEmpty(contactStateEvent.Type, nameof(contactStateEvent.Type));   
        ArgumentException.ThrowIfNullOrEmpty(contactStateEvent.Source,nameof(contactStateEvent.Source));

        return new ContactRegistry
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

            // comment faire pour le mapping de ces proprietés?

            //ContactAddress1= null,
            //ContactAddress2 = null,
            //ContactAddress3 = null,
            //ContactCity = null,
            //ContactCountry = null,
            //ContactCode = null,
            //ContactDepartment = null,
            //ContactPostalCode = null,
            //ContactTitle = null
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
            DateDeployment = DateTime.UtcNow
        };
    }
}