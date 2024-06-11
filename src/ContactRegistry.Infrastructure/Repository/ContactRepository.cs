// <copyright file="ContactRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository
{
    /// <summary>
    /// ContactsRepository.
    /// </summary>
    /// <param name="dbContext">dbContext.</param>
    public class ContactRepository(ApplicationDbContext dbContext)
        : IContactRepository
    {
        public async Task AddContactsAsync(IEnumerable<AlxContact> contacts)
        {
            await dbContext.AlxContacts.AddRangeAsync(contacts);
            await dbContext.SaveChangesAsync();
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
    }
}
