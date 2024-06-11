// <copyright file="ContactService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models;
using Domain.Entities;
using System.Text.Json;

namespace Application.Services
{
    public class ContactService : IContactService
    {
        private readonly IContactRepository contactRepository;

        public ContactService(IContactRepository contactRepository)
        {
            this.contactRepository = contactRepository;
        }

        public async Task ProcessContactAsync(IEnumerable<ContactCsv> contacts)
        {
            var contactsAlx = contacts
                .Select(
                    a => new AlxContact
                    {
                        Id = a.Id,
                        Email = a.Email,
                        FirstName = a.FirstName,
                        LastName = a.LastName,
                        IsActive = a.IsActive,
                        IsCustomer = a.IsCustomer,
                        MobilePhone = a.MobilePhone,
                        LandPhone = a.LandPhone,
                        JobDescription = a.JobDescription,
                        OfficeId = a.OfficeId,
                    }).ToList();
            await this.contactRepository.AddContactsAsync(contactsAlx);
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
                        AccountId = r.Id,
                        Deleted = r.Deleted,
                        Id = r.Id,
                    }).ToList(),
                    Source = contact.Source,
                    Updated = contact.Updated,
                });
            }

            jsonWriter.WriteEndArray();
            await jsonWriter.FlushAsync();
        }
    }
}
