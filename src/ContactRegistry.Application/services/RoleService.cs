// <copyright file="RoleService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models;
using Domain.Entities;
using System.Text.Json;

namespace Application.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository roleRepository;

        public RoleService(IRoleRepository roleRepository)
        {
            this.roleRepository = roleRepository;
        }

        public async Task ProcessRoleAsync(IEnumerable<RoleCsv> roles)
        {
            var rolesAlx = roles
                .Select(
                a => new AlxRole
                {
                    RoleId =  a.Id,
                    AccountId = a.AccountId,
                    ContactId = a.ContactId,
                    Onboarded = a.Onboarded
                }).ToList();
            await this.roleRepository.AddRolesAsync(rolesAlx);
        }

        public async Task StreamRolesJsonAsync(StreamWriter streamWriter)
        {
            await using var jsonWriter = new Utf8JsonWriter(streamWriter.BaseStream, new JsonWriterOptions { Indented = true });

            jsonWriter.WriteStartArray();

            await foreach (var role in roleRepository.GetRolesAsync())
            {
                JsonSerializer.Serialize(jsonWriter, new Models.CreRole
                {
                    AccountId = role.Id,
                    ContactId = role.ContactId,
                    Deleted = role.Deleted
                });
            }

            jsonWriter.WriteEndArray();
            await jsonWriter.FlushAsync();
        }
    }
}
