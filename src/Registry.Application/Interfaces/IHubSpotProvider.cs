// <copyright file="IHubSpotProvider.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models.Results;
using Application.Requests;

namespace Application.Interfaces;

public interface IHubSpotProvider
{
    Task<HubSpotSubmissionResult> SubmitIntegrationAsync(string portalId, string formGuid, HubSpotSubmissionRequest request);
}
