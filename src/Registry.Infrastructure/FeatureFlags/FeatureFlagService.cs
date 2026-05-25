// <copyright file="FeatureFlagService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using OpenFeature;

namespace Infrastructure.FeatureFlags;

/// <summary>
/// Evaluates Registry feature flags through OpenFeature.
/// </summary>
public class FeatureFlagService(IFeatureClient featureClient) : IFeatureFlagService
{
    /// <inheritdoc/>
    public bool IsEnabled(string flagKey)
        => featureClient.GetBooleanValueAsync(flagKey, false).GetAwaiter().GetResult();
}
