// <copyright file="IContactRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using Application.Models.Contacts;
using Pulse.Registry.Domain.Entities;

namespace Application.Interfaces;

public interface IContactRepository
{
    Task AddContactsAsync(IEnumerable<RefContactCsv> contacts);
    Task<bool> AddContactAsync(Contact contact);

    Task<bool> UpdateContactAsync(Contact contact);

    Task<bool> DeleteContactAsync(int? contactId);

    Task<Contact?> GetContactAsync(string? email = null, int? contactId = null);

    Task<bool> IsContactExisted(string? email = null, int? contactId = null);

    Task<bool> DoesOperationContactExistAsync(string email);

    Task<bool> DoesContactExistInOperations(string email, string operationType, string processStatus);

    Task<IList<RefContactEntity>?> GetContactsWithoutOperationsPagedAsync(int pageSize,
        Guid? lastEntityId = null);

    Task InsertContactNewAudit(RefContactEntity refContact, string reason);

    Task<bool> DoesContactExistAsync(string email);
}
