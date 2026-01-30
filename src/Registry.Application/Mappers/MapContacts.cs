// <copyright file="MapContacts.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using Application.Models.Contacts;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using Pulse.Registry.Domain.Entities;
using Pulse.Registry.Domain.Entities.Contacts;

namespace Application.Mappers;

public static class MapContacts
{
    public static IEnumerable<RefContactEntity> MapContactCsvsToContactEntities(this IEnumerable<RefContactCsv> source)
    {
        return source?.Select(s => s.MapContactCsvToContactEntity()!).ToList() ?? Enumerable.Empty<RefContactEntity>();
    }

    public static RefContactEntity? MapContactCsvToContactEntity(this RefContactCsv source)
    {
        if (source == null)
        {
            return null!;
        }

        return new RefContactEntity
        {
            ContactFlagStatus = source.ContactFlagStatus,
            Email = source.Email,
            FirstName = source.FirstName,
            LastName = source.LastName,
            IsCustomer = source.IsCustomer,
            LandPhone = source.LandPhone,
            MobilePhone = source.MobilePhone,
            JobDescription = source.JobDescription,
            OfficeCode = source.OfficeId,
            OperationType = source.Operation,
            OperationDate = DateTime.UtcNow,
        };
    }

    public static ContactEntity? MapContactModelToEntity(this Contact? contact)
    {
        if (contact == default(Contact))
        {
            return null;
        }
        return new ContactEntity()
        {
            ContactGlobalUniqueId = contact.ContactGlobalUniqueId,
            ContactId = contact.ContactId,
            Email = contact.Email,
            Type = contact.Type,
            FirstName = contact.FirstName,
            LastName = contact.LastName,
        };
    }

    public static Contact? MapContactEntityToModel(this ContactEntity? contact)
    {
        if (contact == default(ContactEntity))
        {
            return null;
        }
        return new Contact()
        {
            ContactGlobalUniqueId = contact.ContactGlobalUniqueId,
            ContactId = contact.ContactId,
            Email = contact.Email,
            Type = contact.Type,
            FirstName = contact.FirstName,
            LastName = contact.LastName
        };
    }

    public static Contact MapContactStateEventToModel(this ContactStateEventData contactEntity)
    {
        return new Contact()
        {
            ContactGlobalUniqueId = contactEntity.ContactGlobalUniqueId,
            ContactId = contactEntity.ContactId,
            Email = contactEntity.Email,
            Type = contactEntity.Type,
            FirstName = contactEntity.FirstName,
            LastName = contactEntity.LastName
        };
    }


}
