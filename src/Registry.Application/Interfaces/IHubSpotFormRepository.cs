// <copyright file="IHubSpotFormRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Pulse.Registry.Domain.Entities;

namespace Application.Interfaces;

public interface IHubSpotFormRepository
{
    Task<bool> HasSuccessfulSubmissionAsync(string accountNumber);

    Task AddSubmissionAsync(HubSpotFormEntity submission);

    /// <summary>
    /// Deletes every HubSpot form submission row recorded for the given account (QA reset usage).
    /// </summary>
    /// <param name="accountNumber">The Akuiteo account number.</param>
    Task DeleteSubmissionsAsync(string accountNumber);
}
