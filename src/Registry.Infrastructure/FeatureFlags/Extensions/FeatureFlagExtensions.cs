// <copyright file="FeatureFlagExtensions.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using ConfigCat.Client;
using Application.Interfaces;
using OpenFeature;
using OpenFeature.Contrib.ConfigCat;
using OpenFeature.Providers.Memory;
using Infrastructure.FeatureFlags;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.FeatureFlags.Extensions;

/// <summary>
/// Registers OpenFeature-backed feature flag services for Registry.
/// </summary>
public static class FeatureFlagExtensions
{
    /// <summary>
    /// Adds Registry feature flag services using ConfigCat when configured, otherwise in-memory defaults.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddFeatureFlags(this IServiceCollection services, IConfiguration configuration)
    {
        var sdkKey = configuration.GetValue<string>("ConfigCat:SdkKey");

        if (!string.IsNullOrWhiteSpace(sdkKey))
        {
            var provider = new ConfigCatProvider(sdkKey, options =>
            {
                options.PollingMode = PollingModes.AutoPoll(pollInterval: TimeSpan.FromSeconds(60));
                options.DataGovernance = DataGovernance.EuOnly;
            });

            Api.Instance.SetProviderAsync(provider).GetAwaiter().GetResult();
        }
        else
        {
            var flags = BuildInMemoryFlags(configuration);
            var provider = new InMemoryProvider(flags);
            Api.Instance.SetProviderAsync(provider).GetAwaiter().GetResult();
        }

        var featureClient = Api.Instance.GetClient();
        services.AddSingleton<IFeatureClient>(featureClient);
        services.AddSingleton<IFeatureFlagService, FeatureFlagService>();

        return services;
    }

    /// <summary>
    /// Builds the in-memory default flags from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The in-memory flag dictionary.</returns>
    private static IDictionary<string, Flag> BuildInMemoryFlags(IConfiguration configuration)
    {
        var flags = new Dictionary<string, Flag>();
        var section = configuration.GetSection("FeatureFlags:Defaults");

        foreach (var child in section.GetChildren())
        {
            if (bool.TryParse(child.Value, out var boolValue))
            {
                flags[child.Key] = new Flag<bool>(
                    new Dictionary<string, bool> { { "on", true }, { "off", false } },
                    boolValue ? "on" : "off");
            }
        }

        return flags;
    }
}
