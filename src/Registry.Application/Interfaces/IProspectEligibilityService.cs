// <copyright file="IProspectEligibilityService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Interfaces;

/// <summary>
/// Exposes prospect SIRET eligibility checks.
/// </summary>
public interface IProspectEligibilityService
{
    /// <summary>
    /// Checks whether a prospect is eligible according to its SIRET.
    /// </summary>
    /// <param name="siret">The SIRET to validate and evaluate.</param>
    /// <returns><see langword="true"/> when the prospect is eligible; otherwise <see langword="false"/>.</returns>
    Task<bool> CheckEligibilityAsync(string? siret);
}
