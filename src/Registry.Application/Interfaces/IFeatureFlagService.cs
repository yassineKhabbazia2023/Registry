// <copyright file="IFeatureFlagService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Interfaces;

/// <summary>
/// Provides feature flag evaluations for Registry flows.
/// </summary>
public interface IFeatureFlagService
{
    /// <summary>
    /// Evaluates whether the specified feature flag is enabled.
    /// </summary>
    /// <param name="flagKey">The feature flag key to evaluate.</param>
    /// <returns><c>true</c> when the flag is enabled; otherwise, <c>false</c>.</returns>
    bool IsEnabled(string flagKey);
}
