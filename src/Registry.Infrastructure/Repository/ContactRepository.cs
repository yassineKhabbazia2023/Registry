// <copyright file="ContactRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models;
using EFCore.BulkExtensions;
using Infrastructure.Mappers;
using Pulse.ContactRegistry.Infrastructure.Context;

namespace Infrastructure.Repository;

/// <summary>
/// ContactsRepository.
/// </summary>
/// <param name="dbContext">dbContext.</param>
public class ContactRepository(RefContext refContext) : IContactRepository
{


    public async Task AddContactsAsync(IEnumerable<RefContactCsv> contacts)
    {
        await refContext.BulkInsertAsync(contacts.MapContactCsvsToContactEntities());
    }
 }