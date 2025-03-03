// <copyright file="ContactRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Mappers;
using Application.Models;
using Application.Models.Contacts;
using Domain.Entities.Audits;
using Domain.Entities.Contacts;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities;

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
            return await query.AnyAsync(x => x.Email == email);
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
            var contactEntity = await query.FirstOrDefaultAsync(c => c.Email == email);
            return contactEntity.MapContactEntityToModel();
        }

        if (contactId != null && contactId != default(int))
        {
            var contactEntity = await query.FirstOrDefaultAsync(c => c.ContactId == contactId);
            return contactEntity.MapContactEntityToModel();
        }
        return null;
    }

    public async Task<RefContactEntity?> GetRefContactAsync(string email)
    {
        return await refContext.RefContactEntity
                .FirstOrDefaultAsync(x => (x.IsCustomer ?? false) && x.Email == email);
    }

    public async Task<IList<RefContactEntity>> GetContactsWithoutOperationsPagedAsync(
        int pageSize,
        Guid? lastEntityId = null)
    {

        var query = refContext.RefContactEntity
            .Where(refContact => refContact.ValidationDate == null)
            .OrderBy(refContact => refContact.OperationDate);

        if (lastEntityId.HasValue)
        {
            query = query.Where(refContact => refContact.EntityId.CompareTo(lastEntityId.Value) > 0)
                .OrderBy(refContact => refContact.EntityId); ;
        }
     
            return await query.Take(pageSize).ToListAsync();
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
            var roles = await refContext.RoleEntities.Where(r => r.ContactId == contactId).ToListAsync();
            if (roles.Any())
            {
                refContext.RoleEntities.RemoveRange(roles);
            }

            ContactEntity contactEntity = await refContext.ContactEntities
           .FirstAsync(c => c.ContactId == contactId);

            refContext.ContactEntities.Remove(contactEntity);
            await refContext.SaveChangesAsync();
            return true;

        }
        logger.LogError($"[Method]: ${nameof(DeleteContactAsync)}; [Error]: ContactId {contactId} Does not exists in contact.Contact");
        return false;
    }


    public async Task<bool> DoesContactExistInOperations(string email, string operationType, string processStatus)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(email);
        ArgumentNullException.ThrowIfNullOrEmpty(operationType);
        ArgumentNullException.ThrowIfNullOrEmpty(processStatus);

        var result = await (from operations in refContext.RegOperationEntity
                            join contacts in refContext.RefContactEntity on operations.EntityId equals contacts.EntityId
                            where operations.Operation == operationType
                            && operations.Type == "CONTACT"
                            && operations.ProcessStatus == processStatus
                            && contacts.Email == email select 1).AnyAsync();
        return result;
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

    public async Task<bool> DoesContactExistAsync(string email)
    {
        return refContext.ContactEntities.Any(contact =>
            contact.Email.Equals(email)
        );
    }

    public async Task<bool> DoesOperationContactExistAsync(string email, string operationName)
    {
        return await (from refContact in refContext.RefContactEntity
                      join opContact in refContext.RegOperationEntity on refContact.EntityId equals opContact.EntityId
                      where refContact.Email == email && opContact.Operation == operationName
                      select 1).AnyAsync();
    }

    public async Task InsertContactNewAudit(RefContactEntity refContact, string reason)
    {
        var audit = new DeepValidationEntity
        {
            Type = "CONTACT",
            EntityId = refContact.EntityId,
            Reason = reason,
            CreationDate = DateTime.UtcNow,
        };

        refContext.DeepValidationEntities.Add(audit);

        await refContext.SaveChangesAsync();
    }
}