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
        Task<(int creContactActif, int alxContactActif)> GetCountContactActifAsync();

        /// <summary>
        /// ClearAlxAsync.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the async operation.</returns>
        Task ClearAlxAsync();

    }
}
