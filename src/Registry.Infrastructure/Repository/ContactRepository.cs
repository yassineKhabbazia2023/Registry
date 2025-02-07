// <copyright file="ContactRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models;
using Application.Models.Contacts;
using Domain.Entities.Contacts;
using EFCore.BulkExtensions;
using Application.Mappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pulse.ContactRegistry.Domain.Context;

namespace Application.Repository;

/// <summary>
/// ContactsRepository.
/// </summary>
/// <param name="dbContext">dbContext.</param>
public class ContactRepository(RefContext refContext, ILogger<ContactRepository> logger) : IContactRepository
{
    public async Task AddContactsAsync(IEnumerable<RefContactCsv> contacts)
    {
        await refContext.BulkInsertAsync(contacts.MapContactCsvsToContactEntities());
    }
    public async Task<bool> AddContactAsync(Contact contact)
    {
        return await TryReposAction<Contact>(TryAddContactAsync, contact);
    }

    public async Task<bool> UpdateContactAsync(Contact contact)
    {
        return await TryReposAction<Contact>(TryUpdateContactAsync, contact);
    }

    public async Task<bool> DeleteContactAsync(int? contactId)
    {
        return await TryReposAction<int?>(TryDeleteContactAsync, contactId);
    }

    public async Task<bool> IsContactExisted(string? email = null, int? contactId = null)
    {
        if (string.IsNullOrEmpty(email) && (contactId == null || contactId == default(int)))
        {
            return false;
        }

        var query = refContext.ContactEntities.AsNoTracking();
        if (!string.IsNullOrEmpty(email))
        {
            return await query.AnyAsync(x => x.Email.Equals(email, StringComparison.InvariantCultureIgnoreCase));
        }

        if (contactId != null && contactId != default(int))
        {
            return await query.AnyAsync(c => c.ContactId == contactId);
        }
        return false;
    }

    public async Task<Contact?> GetContactAsync(string? email = null, int? contactId = null)
    {
        if (string.IsNullOrEmpty(email) && (contactId == null || contactId == default(int)))
        {
            return null;
        }
        var query = refContext.ContactEntities.AsNoTracking();

        if (!string.IsNullOrEmpty(email))
        {
            var contactEntity = await query.FirstOrDefaultAsync(c => c.Email.Equals(email, StringComparison.InvariantCultureIgnoreCase));
            return contactEntity.MapContactEntityToModel();
        }

        if (contactId != null && contactId != default(int))
        {
            var contactEntity = await query.FirstOrDefaultAsync(c => c.ContactId == contactId);
            return contactEntity.MapContactEntityToModel();
        }
        return null;
    }


    private async Task<bool> TryAddContactAsync(Contact contact)
    {
        bool contactExisted = await IsContactExisted(contactId: contact.ContactId);
        if (contactExisted)
        {
            logger.LogError($"[Method]: ${nameof(AddContactAsync)}; [Error]: Contact {contact.Email} already existed!");
            return false;
        }
        ContactEntity contactEntity = contact.MapContactModelToEntity();
        await refContext.ContactEntities.AddAsync(contactEntity);
        await refContext.SaveChangesAsync();
        return true;
    }

    private async Task<bool> TryUpdateContactAsync(Contact contact)
    {
        bool contactExisted = await IsContactExisted(contactId: contact.ContactId);
        if (contactExisted)
        {
            var trackedEntity = await refContext.ContactEntities.FirstOrDefaultAsync(c => c.ContactId == contact.ContactId);
            if (trackedEntity != null)
            {
                refContext.Entry(trackedEntity).State = EntityState.Detached;
            }

            ContactEntity contactEntity = contact.MapContactModelToEntity();
            refContext.ContactEntities.Update(contactEntity);
            await refContext.SaveChangesAsync();
            return true;
        }
        logger.LogError($"[Method]: ${nameof(TryUpdateContactAsync)}; [Error]: Contact with Email {contact.Email} Does not exists!");
        return false;
    }
    private async Task<bool> TryDeleteContactAsync(int? contactId)
    {
        if (contactId == null || contactId == default(int))
        {
            logger.LogError($"[Method]: ${nameof(DeleteContactAsync)}; [Error]: ContactId {contactId} Input is not valid");
            return false;
        }

        bool contactExisted = await IsContactExisted(contactId: contactId);
        if (contactExisted)
        {
            ContactEntity contactEntity = await refContext.ContactEntities
           .FirstAsync(c => c.ContactId == contactId);

            refContext.ContactEntities.Remove(contactEntity);
            await refContext.SaveChangesAsync();
            return true;

        }
        logger.LogError($"[Method]: ${nameof(DeleteContactAsync)}; [Error]: ContactId {contactId} Does not exists in contact.Contact");
        return false;
    }

    private async Task<bool> TryReposAction<T>(Func<T, Task<bool>> functionExecution, T t)
    {
        try
        {
            return await functionExecution(t);
        }
        catch (DbUpdateException dbEx)
        {
            logger.LogError($"[Method]: {nameof(functionExecution)}; [Error]: {dbEx.Message}");
            return false;
        }
        catch (InvalidOperationException ioEx)
        {
            logger.LogError($"[Method]: {nameof(functionExecution)}; [Error]: {ioEx.Message}");
            return false;
        }
    }
}