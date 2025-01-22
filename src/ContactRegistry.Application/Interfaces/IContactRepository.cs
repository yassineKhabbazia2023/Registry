// <copyright file="IContactRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;

namespace Application.Interfaces;

public interface IContactRepository
{
    Task AddContactsAsync(IEnumerable<RefContactCsv> contacts);

}
