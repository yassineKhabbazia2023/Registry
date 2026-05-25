// <copyright file="IAccountOnboardingEligibilityProvider.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Interfaces;

/// <summary>
/// Provides account-onboarding eligibility lookups for a company identified by its SIRET.
/// </summary>
public interface IAccountOnboardingEligibilityProvider
{
    /// <summary>
    /// Determines whether the specified SIRET already exists in the external account system.
    /// </summary>
    /// <param name="siret">The normalized SIRET to look up.</param>
    /// <returns><see langword="true"/> when the company already exists in the external account system; otherwise <see langword="false"/>.</returns>
    Task<bool> ExistsAsync(string siret);
}
