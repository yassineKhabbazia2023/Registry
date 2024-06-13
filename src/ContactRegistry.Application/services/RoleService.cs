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
        private readonly IProcessDeltaTriggerRepository processDeltaTriggerRepository;

        public RoleService(IRoleRepository roleRepository, IProcessDeltaTriggerRepository processDeltaTriggerRepository)
        {
            this.roleRepository = roleRepository;
            this.processDeltaTriggerRepository = processDeltaTriggerRepository;
        }

        public async Task ProcessRoleAsync(IEnumerable<RoleCsv> roles)
        {

            var rolesAlx = roles
                .Select(
                a => new AlxRole
                {
                    RoleId =  a.RoleId,
                    AccountId = a.AccountId,
                    ContactId = a.ContactId,
                    Onboarded = a.Onboarded,
                    IsFavorite = a.IsFavorite,
                    RoleSignatory = a.RoleSignatory,
                    RoleDelegataireEmail = a.RoleDelegataireEmail,
                }).ToList();
            await this.roleRepository.AddRolesAsync(rolesAlx);

            await this.processDeltaTriggerRepository.UpdateRoleProcessAsync(true);
        }

        public async Task StreamRolesJsonAsync(StreamWriter streamWriter)
        {
            await using var jsonWriter = new Utf8JsonWriter(streamWriter.BaseStream, new JsonWriterOptions { Indented = true });

            jsonWriter.WriteStartArray();

            await foreach (var role in roleRepository.GetRolesAsync())
            {
                JsonSerializer.Serialize(jsonWriter, new Models.CreRole
                {
                    RoleId = role.RoleId,
                    AccountId = role.AccountId,
                    ContactId = role.ContactId,
                    Onboarded = role.Onboarded,
                    IsFavorite = role.IsFavorite,
                    RoleSignatory = role.RoleSignatory,
                    RoleDelegataireEmail =role.RoleDelegataireEmail,    
                });
            }

            jsonWriter.WriteEndArray();
            await jsonWriter.FlushAsync();
        }
    }
}
