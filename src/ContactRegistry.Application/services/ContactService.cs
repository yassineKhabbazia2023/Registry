// <copyright file="ContactService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Application.Services;

public class ContactService : IContactService
{
    private readonly IContactRepository contactRepository;
    private readonly IProcessDeltaTriggerRepository processDeltaTriggerRepository;
    private const int BATCH_SIZE = 2000;
    private readonly ILogger<ContactService> logger;

    public ContactService(ILogger<ContactService> logger, IContactRepository contactRepository, IProcessDeltaTriggerRepository processDeltaTriggerRepository)
    {
        contactRepository = contactRepository;
        this.processDeltaTriggerRepository = processDeltaTriggerRepository;
        this.logger = logger;
    }

    public async Task ProcessContactAsync(IEnumerable<ContactCsv> contacts)
    {
        var list = new List<AlxContact>();
        foreach (var contact in contacts)
        {
            var c = new AlxContact
            {
                Id = contact.Id,
                Email = contact.Email,
                FirstName = contact.FirstName,
                LastName = contact.LastName,
                IsActive = contact.IsActive,
                IsCustomer = contact.IsCustomer,
                MobilePhone = contact.MobilePhone,
                LandPhone = contact.LandPhone,
                JobDescription = contact.JobDescription,
                OfficeId = contact.OfficeId,
            };
            list.Add(c);
            if (list.Count == BATCH_SIZE)
            {
                await contactRepository.AddContactsAsync(list);
                list.Clear();
            }

        }
        if (list.Count > 0)
        {
            await contactRepository.AddContactsAsync(list);
        }

        await processDeltaTriggerRepository.UpdateContactProcessAsync(true);

        var countResult = await contactRepository.GetCountContactActifAsync();
        logger.LogInformation("CreContactActif count:{countCRE} ,  AlxContactActif count: {countAlx}", countResult.creContactActif, countResult.alxContactActif);
    }

    public async Task StreamContactsJsonAsync(StreamWriter streamWriter)
    {
        await using var jsonWriter = new Utf8JsonWriter(streamWriter.BaseStream, new JsonWriterOptions { Indented = true });

        jsonWriter.WriteStartArray();

        await foreach (var contact in contactRepository.GetContactsAsync())
        {
            JsonSerializer.Serialize(jsonWriter, new Models.CreContact
            {
                Id = contact.Id,
                FirstName = contact.FirstName,
                LastName = contact.LastName,
                IsActive = contact.IsActive,
                IsCustomer = contact.IsCustomer,
                MobilePhone = contact.MobilePhone,
                LandPhone = contact.LandPhone,
                JobDescription = contact.JobDescription,
                OfficeId = contact.OfficeId,
                Deleted = contact.Deleted,
                Email = contact.Email,
                Roles = contact.Roles?.Select(r => new Models.CreRole()
                {
                    AccountId = r.RoleId,
                    Deleted = r.Deleted,
                    RoleId = r.RoleId,
                }).ToList(),
                Source = contact.Source,
                Updated = contact.Updated,
            });
        }

        jsonWriter.WriteEndArray();
        await jsonWriter.FlushAsync();
    }

    public async Task ClearAlxAsync()
    {
        await contactRepository.ClearAlxAsync();
    }

    public async Task InsertContactsAsync(IEnumerable<RefContactCsv> contacts)
    {
        await contactRepository.AddContactsAsync(contacts);
    }
}
