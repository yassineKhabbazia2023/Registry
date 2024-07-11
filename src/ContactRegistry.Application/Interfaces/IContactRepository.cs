// <copyright file="IContactRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Interfaces
{
    using Domain.Entities;
    public interface IContactRepository
    {
        Task AddContactsAsync(IEnumerable<AlxContact> contacts);
        IAsyncEnumerable<CreContact> GetContactsAsync();
    }
}
