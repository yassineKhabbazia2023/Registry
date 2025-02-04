// <copyright file="IContactService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using Application.Models.Contacts;
using Application.Models.Results;
using Pulse.Back.Events.IntegrationEvents.EventsData;

namespace Application.Interfaces;

public interface IContactService
{

    /// <summary>
    /// Inserts all contacts and operations into [ref].[Contact] table.
    /// </summary>
    /// <param name="contacts">Contacts inserted.</param>
    /// <returns>A <see cref="Task"/> representing the async operation.</returns>
    Task InsertContactsAsync(IEnumerable<RefContactCsv> contacts);

    Task<ContactEventResult<Contact>> OnCreatedContactEventExecution(ContactStateEventData contactStateEventData);
    Task<ContactEventResult<Contact>> OnUpdatedContactEventExecution(ContactStateEventData contactStateEventData);
    Task<ContactEventResult<Contact>> OnRemovedContactEventExecution(ContactRemovedEventData contactRemovedData);
}