// <copyright file="RegProcessDeltaTriggerRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository;

public class RegProcessDeltaTriggerRepository : IRegProcessDeltaTriggerRepository
{
    private readonly ApplicationDbContext context;

    public RegProcessDeltaTriggerRepository(ApplicationDbContext context)
    {
        this.context = context;
    }

    public async Task<RegProcessDeltaTrigger> GetProcessAsync()
    {
        return await this.context.RegProcessDeltaTriggers.FirstAsync();
    }

    public async Task UpdateAccountProcessAsync(bool state)
    {
        var stateLine = await this.GetProcessAsync();
        stateLine.Account = state;
        await context.SaveChangesAsync();
    }

    public async Task UpdateContactProcessAsync(bool state)
    {
        var stateLine = await this.GetProcessAsync();
        stateLine.Contact = state;
        await context.SaveChangesAsync();
    }

    public async Task UpdateRoleProcessAsync(bool state)
    {
        var stateLine = await this.GetProcessAsync();
        stateLine.Role = state;
        await context.SaveChangesAsync();
    }
}
