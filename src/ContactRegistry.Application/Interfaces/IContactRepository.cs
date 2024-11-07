// <copyright file="IContactRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using Domain.Entities;
using CreContact = Domain.Entities.CreContact;

namespace Application.Interfaces;

public interface IContactRepository
{
    Task AddContactsAsync(IEnumerable<AlxContact> contacts);

    Task AddContactsAsync(IEnumerable<RefContactCsv> contacts);

    IAsyncEnumerable<CreContact> GetContactsAsync();

    Task<(int creContactActif, int alxContactActif)> GetCountContactActifAsync();

    Task ClearAlxAsync();
}
