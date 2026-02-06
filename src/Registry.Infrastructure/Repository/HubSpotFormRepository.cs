// <copyright file="HubSpotFormRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Exceptions;
using Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities;

namespace Infrastructure.Repository;

public class HubSpotFormRepository(RefContext refContext) : IHubSpotFormRepository
{
    public async Task<bool> HasSuccessfulSubmissionAsync(string accountNumber)
    {
        return await refContext.HubSpotFormEntity
            .AsNoTracking()
            .AnyAsync(form => form.AccountNumber == accountNumber && form.HubSpotDispatchState);
    }

    public async Task AddSubmissionAsync(HubSpotFormEntity submission)
    {
        ArgumentNullException.ThrowIfNull(submission);

        refContext.HubSpotFormEntity.Add(submission);
        await refContext.SaveChangesAsync();
    }
}
