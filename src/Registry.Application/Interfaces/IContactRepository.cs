// <copyright file="IContactRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using Application.Models.Contacts;

namespace Application.Interfaces;

public interface IContactRepository
{
    Task AddContactsAsync(IEnumerable<RefContactCsv> contacts);
    Task<bool> AddContactAsync(Contact contact);

    Task<bool> UpdateContactAsync(Contact contact);

    Task<bool> DeleteContactAsync(int? contactId);

    Task<Contact?> GetContactAsync(string? email = null, int? contactId = null);

}
