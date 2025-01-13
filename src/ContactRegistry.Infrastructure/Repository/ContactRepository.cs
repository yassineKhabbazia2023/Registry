// <copyright file="ContactRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models;
using Domain.Entities;
using EFCore.BulkExtensions;
using Infrastructure.Context;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;
using Pulse.ContactRegistry.Infrastructure.Context;
using CreContact = Domain.Entities.CreContact;

namespace Infrastructure.Repository;

/// <summary>
/// ContactsRepository.
/// </summary>
/// <param name="dbContext">dbContext.</param>
public class ContactRepository(ApplicationDbContext dbContext, RefContext refContext) : IContactRepository
{
    public async Task AddContactsAsync(IEnumerable<AlxContact> contacts)
    {
        await dbContext.BulkInsertAsync(contacts);
    }

    public async Task AddContactsAsync(IEnumerable<RefContactCsv> contacts)
    {
        await refContext.BulkInsertAsync(contacts.MapContactCsvsToContactEntities());
    }
    public async IAsyncEnumerable<CreContact> GetContactsAsync()
    {
        if(dbContext.CreContacts.Any())
        {
            await foreach (var contact in dbContext.CreContacts.Include(r => r.Roles).AsAsyncEnumerable())
            {
                yield return contact;
            }
        }
    }

    public async Task<(int creContactActif, int alxContactActif)> GetCountContactActifAsync()
    {
        var countCreAct = await dbContext.CreContacts.CountAsync(c => c.IsActive);
        var countAlxAct = await dbContext.AlxContacts.CountAsync(c => c.IsActive);
        return (countCreAct, countCreAct);
    }

    public async Task ClearAlxAsync()
    {
        await dbContext.AlxContacts.ExecuteDeleteAsync();
    }
}
