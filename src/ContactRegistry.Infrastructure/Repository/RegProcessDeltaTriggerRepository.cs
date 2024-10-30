// <copyright file="RegProcessDeltaTriggerRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;
using Pulse.ContactRegistry.Infrastructure.Context;
using Pulse.ContactRegistry.Infrastructure.Entities;

namespace Infrastructure.Repository;

public class RegProcessDeltaTriggerRepository : IRegProcessDeltaTriggerRepository
{
    private readonly RefContext context;

    public RegProcessDeltaTriggerRepository(RefContext context)
    {
        this.context = context;
    }

    public async Task<RegProcessDeltaTrigger?> GetProcessAsync()
    {
        return (await this.context.RegProcessDeltaTriggerEntity.FirstAsync()).MapProcessDeltaTriggerEntityToModel();
    }

    private async Task<RegProcessDeltaTriggerEntity> GetProcessEntityAsync()
    {
        return await this.context.RegProcessDeltaTriggerEntity.FirstAsync();
    }

    public async Task UpdateAccountProcessAsync(bool state)
    {
        var stateLine = await this.GetProcessEntityAsync();
        stateLine!.Account = state;
        await context.SaveChangesAsync();
    }

    public async Task UpdateContactProcessAsync(bool state)
    {
        var stateLine = await this.GetProcessEntityAsync();
        stateLine!.Contact = state;
        await context.SaveChangesAsync();
    }

    public async Task UpdateRoleProcessAsync(bool state)
    {
        var stateLine = await this.GetProcessEntityAsync();
        stateLine!.Role = state;
        await context.SaveChangesAsync();
    }
}
