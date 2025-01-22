// <copyright file="ContactService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Application.Services;

public class ContactService : IContactService
{
    private readonly IContactRepository contactRepository;
    private readonly ILogger<ContactService> logger;

    public ContactService(ILogger<ContactService> logger, IContactRepository contactRepository)
    {
        this.contactRepository = contactRepository;
        this.logger = logger;
    }

    public async Task InsertContactsAsync(IEnumerable<RefContactCsv> contacts)
    {
        await contactRepository.AddContactsAsync(contacts);
    }

    public List<string> ValidateContacts(IEnumerable<RefContactCsv> contacts)
    {
        var errorMessages = new List<string>();
        var allowedOperations = new HashSet<string> { "INSERT", "UPDATE", "DELETE" };
        int lineNumber = 1;

        foreach (var contact in contacts)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(contact.Email))
                errors.Add("Email is required");

            if (string.IsNullOrWhiteSpace(contact.Operation))
                errors.Add("Operation is required");
            else if (!allowedOperations.Contains(contact.Operation.ToUpper()))
                errors.Add($"Invalid Operation '{contact.Operation}' (Allowed: INSERT, UPDATE, DELETE)");

            if (errors.Any())
                errorMessages.Add($"Line {lineNumber}: {string.Join(", ", errors)}");

            lineNumber++;
        }

        return errorMessages;
    }
}
