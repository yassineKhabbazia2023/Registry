// <copyright file="ContactRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Domain.Entities;
using EFCore.BulkExtensions;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace Infrastructure.Repository
{
    /// <summary>
    /// ContactsRepository.
    /// </summary>
    /// <param name="dbContext">dbContext.</param>
    [ExcludeFromCodeCoverage]
    public class ContactRepository(ApplicationDbContext dbContext)
        : IContactRepository
    {
        public async Task AddContactsAsync(IEnumerable<AlxContact> contacts)
        {
            await dbContext.BulkInsertAsync(contacts);
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
}
