// <copyright file="IContactService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;

namespace Application.Interfaces;

public interface IContactService
{

    /// <summary>
    /// Inserts all contacts and operations into [ref].[Contact] table.
    /// </summary>
    /// <param name="contacts">Contacts inserted.</param>
    /// <returns>A <see cref="Task"/> representing the async operation.</returns>
    Task InsertContactsAsync(IEnumerable<RefContactCsv> contacts);

    /// <summary>
    /// Validate contacts data
    /// </summary>
    /// <param name="contacts"></param>
    /// <returns></returns>
    IEnumerable<LightValidationResult> ValidateContacts(IEnumerable<RefContactCsv> contacts);
}