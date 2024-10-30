// <copyright file="IContactService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;

namespace Application.Interfaces;

public interface IContactService
{
    Task ProcessContactAsync(IEnumerable<ContactCsv> contacts);

    Task StreamContactsJsonAsync(StreamWriter streamWriter);

    /// <summary>
    /// ClearAlxAsync.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the async operation.</returns>
    Task ClearAlxAsync();

    /// <summary>
    /// Inserts all contacts and operations into [ref].[Contact] table.
    /// </summary>
    /// <param name="contacts">Contacts inserted.</param>
    /// <returns>A <see cref="Task"/> representing the async operation.</returns>
    Task InsertContactsAsync(IEnumerable<RefContactCsv> contacts);
}