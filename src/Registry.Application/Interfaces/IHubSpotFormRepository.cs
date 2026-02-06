// <copyright file="IHubSpotFormRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Pulse.Registry.Domain.Entities;

namespace Application.Interfaces;

public interface IHubSpotFormRepository
{
    Task<bool> HasSuccessfulSubmissionAsync(string accountNumber);

    Task AddSubmissionAsync(HubSpotFormEntity submission);
}
