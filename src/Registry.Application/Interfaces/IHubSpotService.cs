// <copyright file="IHubSpotService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models.Results;
using Application.Requests;

namespace Application.Interfaces;

public interface IHubSpotService
{
    Task<HubSpotFormSubmissionResult> SubmitIntegrationAsync(int accountId, HubSpotSubmissionInputRequest request);

    Task<HubSpotSubmissionStateResult> GetSubmissionStateAsync(int accountId);

    /// <summary>
    /// Resets every HubSpot form submission recorded for the given account (QA reset usage).
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    Task<HubSpotSubmissionResetResult> ResetSubmissionsAsync(int accountId);
}
