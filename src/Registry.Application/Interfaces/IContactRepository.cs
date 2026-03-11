// <copyright file="IContactRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using Application.Models.Contacts;
using Pulse.Registry.Domain.Entities;

namespace Application.Interfaces;

public interface IContactRepository
{
    Task BulkAddContactsAsync(IEnumerable<RefContactEntity> contacts, string? source = null);

    Task<bool> AddContactAsync(Contact contact);

    Task<bool> UpdateContactAsync(Contact contact);

    Task<bool> DeleteContactByIdAsync(int? contactId);

    Task<Contact?> GetContactByEmailOrIdAsync(string? email = null, int? contactId = null);

    Task<bool> DoesContactExistByEmailOrIdAsync(string? email = null, int? contactId = null);

    Task<IList<RefContactEntity>?> GetContactsWithoutOperationsPagedAsync(int pageSize,
        Guid? lastEntityId = null);

    Task<bool> DoesContactExistByEmailAsync(string email);

    Task<bool> DoesContactGlobalUniqueIdExistByEmailAsync(string email);

    Task<RefContactEntity?> GetRefContactByEmailAsync(string email);
}
