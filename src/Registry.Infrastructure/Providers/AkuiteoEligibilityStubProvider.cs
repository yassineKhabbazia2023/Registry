// <copyright file="AkuiteoEligibilityStubProvider.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;

namespace Application.Providers;

/// <summary>
/// Stubbed Akuiteo provider used until the real eligibility integration is implemented.
/// </summary>
public class AkuiteoEligibilityStubProvider : IAccountOnboardingEligibilityProvider
{
    private static readonly HashSet<string> ExistingCustomerSirets =
    [
        "81519987200012",
        "82051385100015",
        "91772785100011"
    ];

    /// <inheritdoc/>
    public Task<bool> ExistsAsync(string siret)
    {
        return Task.FromResult(ExistingCustomerSirets.Contains(siret));
    }
}
