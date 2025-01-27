// <copyright file="ContactService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Helpers;
using Application.Interfaces;
using Application.Models;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;

namespace Application.Services;

public class ContactService : IContactService
{
    private readonly IContactRepository contactRepository;
    private readonly ILogger<ContactService> logger;
    private readonly IValidationHelper<RefContactCsv> validationHelper;

    public ContactService(ILogger<ContactService> logger, IContactRepository contactRepository, IValidationHelper<RefContactCsv> validationHelper)
    {
        this.contactRepository = contactRepository;
        this.logger = logger;
        this.validationHelper = validationHelper;
    }

    public async Task InsertContactsAsync(IEnumerable<RefContactCsv> contacts)
    {
        await contactRepository.AddContactsAsync(contacts);
    }

    public IEnumerable<LightValidationResult> ValidateContacts(IEnumerable<RefContactCsv> contacts)
    {
       return validationHelper.ValidateInstanceList(contacts);
    }

}
