// <copyright file="ProspectEligibilityService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Helpers;
using Application.Interfaces;
using Kpmg.ExceptionMiddleware.AdvancedException;
using Microsoft.Extensions.Logging;

namespace Application.Services;

/// <summary>
/// Handles SIRET-based prospect eligibility checks.
/// </summary>
public class ProspectEligibilityService : IProspectEligibilityService
{
    private readonly ILogger<ProspectEligibilityService> logger;
    private readonly IAccountOnboardingEligibilityProvider accountOnboardingEligibilityProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProspectEligibilityService"/> class.
    /// </summary>
    /// <param name="accountOnboardingEligibilityProvider">The account onboarding eligibility provider.</param>
    /// <param name="logger">The logger.</param>
    public ProspectEligibilityService(
        IAccountOnboardingEligibilityProvider accountOnboardingEligibilityProvider,
        ILogger<ProspectEligibilityService> logger)
    {
        this.accountOnboardingEligibilityProvider = accountOnboardingEligibilityProvider;
        this.logger = logger;
    }

    /// <inheritdoc/>
    public async Task<bool> CheckEligibilityAsync(string? siret)
    {
        var normalizedSiret = SiretValidationHelper.Normalize(siret);
        try
        {
            SiretValidationHelper.ValidateRequiredSiret(normalizedSiret);
        }
        catch (BadRequestException exception)
        {
            logger.LogWarning(
                exception,
                "Prospect eligibility check rejected an invalid SIRET. ProspectCreationStep: {ProspectCreationStep}, ServiceName: {ServiceName}, OperationName: {OperationName}, Siret: {Siret}",
                "CheckSiretNotInAkuiteoAsync",
                "Pulse.Back.Registry",
                nameof(CheckEligibilityAsync),
                normalizedSiret);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Prospect eligibility check rejected an invalid SIRET. ProspectCreationStep: {ProspectCreationStep}, ServiceName: {ServiceName}, OperationName: {OperationName}, Siret: {Siret}",
                "CheckSiretNotInAkuiteoAsync",
                "Pulse.Back.Registry",
                nameof(CheckEligibilityAsync),
                normalizedSiret);
            throw;
        }

        var alreadyExists = await accountOnboardingEligibilityProvider.ExistsAsync(normalizedSiret!);
        return !alreadyExists;
    }
}
