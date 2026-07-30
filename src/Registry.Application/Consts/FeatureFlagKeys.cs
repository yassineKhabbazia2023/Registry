// <copyright file="FeatureFlagKeys.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Consts;

/// <summary>
/// Defines the feature flag keys used by Registry.
/// </summary>
public static class FeatureFlagKeys
{
    /// <summary>
    /// The flag controlling contact synchronization with Akuiteo after a role is created.
    /// </summary>
    public const string IsContactAkuiteoSynchronizationEnabled = "isContactAkuiteoSynchronizationEnabled";

    /// <summary>
    /// The flag controlling Prospect account consumption in Registry.
    /// </summary>
    public const string IsProspectConsumptionEnabled = "isProspectConsumptionEnabled";
}
