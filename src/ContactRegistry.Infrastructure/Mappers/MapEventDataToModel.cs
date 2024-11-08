// <copyright file="MapEventDataToModel.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
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
            RoleFlagStatus = true,
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
            RoleFlagStatus = false,
            RoleFunctionDescription = string.Empty,
            RoleSourceName = GlobalConstants.SOURCENAME,
        };
    }


    public static ContactRegistry ContactStateEventDataToModel(this ContactStateEventData contactStateEvent)
    {
        if (contactStateEvent == null)
        {
            throw new ArgumentNullException(nameof(contactStateEvent));
        }
        if (string.IsNullOrEmpty(contactStateEvent.Email))
        {
            throw new ArgumentNullException(nameof(contactStateEvent.Email));
        }
        if (string.IsNullOrEmpty(contactStateEvent.Type)) 
        {
            throw new ArgumentNullException(nameof(contactStateEvent.Type));
        }
        if (string.IsNullOrEmpty(nameof(contactStateEvent.Source)))
        {
            throw new ArgumentNullException(nameof(contactStateEvent.Source));
        }

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
}
