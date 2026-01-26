// <copyright file="IHubSpotService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models.Results;
using Application.Requests;

namespace Application.Interfaces;

public interface IHubSpotService
{
    Task<HubSpotSubmissionResult> SubmitIntegrationAsync(string? accountNumber, HubSpotSubmissionInputRequest request);
}
