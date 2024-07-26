// <copyright file="IContactService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using Domain.Entities;

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
}