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
        private const int BATCH_SIZE = 10000;

        public RoleService(IRoleRepository roleRepository, IProcessDeltaTriggerRepository processDeltaTriggerRepository)
        {
            this.roleRepository = roleRepository;
            this.processDeltaTriggerRepository = processDeltaTriggerRepository;
        }

        public async Task ProcessRoleAsync(IEnumerable<RoleCsv> roles)
        {
            var list = new List<AlxRole>();

            foreach (var role in roles)
            {
                var entity = new AlxRole
                {
                    RoleId = role.RoleId,
                    AccountId = role.AccountId,
                    ContactId = role.ContactId,
                    Onboarded = role.Onboarded,
                    IsFavorite = role.IsFavorite is null ? null : role.IsFavorite,
                    RoleSignatory = role.RoleSignatory is null ? null : role.RoleSignatory,
                    RoleDelegataireEmail = !string.IsNullOrEmpty(role.RoleDelegataireEmail) ? role.RoleDelegataireEmail : null,
                };
                list.Add(entity);
                if (list.Count == BATCH_SIZE)
                {
                    await this.roleRepository.AddRolesAsync(list);
                    list.Clear();
                }
            }
            if (list.Count > 0)
            {
                await this.roleRepository.AddRolesAsync(list);
            }


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
                    RoleDelegataireEmail = role.RoleDelegataireEmail,
                });
            }

            jsonWriter.WriteEndArray();
            await jsonWriter.FlushAsync();
        }
    }
}
