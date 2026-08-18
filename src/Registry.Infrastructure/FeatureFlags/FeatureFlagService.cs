// <copyright file="FeatureFlagService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Microsoft.Extensions.Logging;
using OpenFeature;
using OpenFeature.Constant;

namespace Infrastructure.FeatureFlags;

/// <summary>
/// Evaluates Registry feature flags through OpenFeature.
/// </summary>
public class FeatureFlagService(IFeatureClient featureClient, ILogger<FeatureFlagService> logger) : IFeatureFlagService
{
    /// <inheritdoc/>
    public bool IsEnabled(string flagKey)
    {
        var details = featureClient.GetBooleanDetailsAsync(flagKey, false).GetAwaiter().GetResult();

        if (details.ErrorType != ErrorType.None)
        {
            logger.LogWarning(
                "Feature flag {FlagKey} evaluation failed with {ErrorType} ({ErrorMessage}), falling back to default value {Value}",
                flagKey,
                details.ErrorType,
                details.ErrorMessage,
                details.Value);
        }

        return details.Value;
    }
}
