// <copyright file="MapContacts.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using Application.Models.Contacts;
using Domain.Entities.Contacts;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using Pulse.ContactRegistry.Domain.Entities;
using System.Runtime.CompilerServices;

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
            FirstName = contact.FirstName,
            LastName = contact.LastName,
            CreationDate = contact.CreationDate,
            IsActive = contact.IsActive,
            LandPhone = contact.LandPhone,
            MobilePhone = contact.MobilePhone,
            Office = contact.Office,
            PersonaName = contact.PersonaName,
            Source = contact.Source,
            Status = contact.Status,
            Type = contact.Type,
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
            FirstName = contact.FirstName,
            LastName = contact.LastName,
            CreationDate = contact.CreationDate,
            IsActive = contact.IsActive,
            LandPhone = contact.LandPhone,
            MobilePhone = contact.MobilePhone,
            Office = contact.Office,
            PersonaName = contact.PersonaName,
            Source = contact.Source,
            Status = contact.Status,
            Type = contact.Type,
        };
    }

    public static Contact MapContactStateEventToModel(this ContactStateEventData contactEntity)
    {
        return new Contact()
        {
            ContactGlobalUniqueId = contactEntity.ContactGlobalUniqueId,
            ContactId = contactEntity.ContactId,
            Email = contactEntity.Email,
            FirstName = contactEntity.FirstName,
            LastName = contactEntity.LastName,
            CreationDate = contactEntity.CreationDate,
            IsActive = contactEntity.IsActive,
            LandPhone = contactEntity.LandPhone,
            MobilePhone = contactEntity.MobilePhone,
            Office = contactEntity.Office,
            PersonaName = contactEntity.PersonaName,
            Source = contactEntity.Source,
            Status = contactEntity.Status,
            Type = contactEntity.Type,
        };
    }


}
