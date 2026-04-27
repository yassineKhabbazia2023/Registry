// <copyright file="ServicesConfiguration.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Azure.Monitor.OpenTelemetry.AspNetCore;
using Pulse.Back.Events;
using System.Diagnostics.CodeAnalysis;
using WebApi.Configurations.Models;
using Pulse.Back.Events.Configurations;
using Registry.Infrastructure.Options;

namespace WebApi.Configurations;

/// <summary>
/// ServiceConfiguration extension.
/// </summary>
[ExcludeFromCodeCoverage]
public static class ServicesConfiguration
{
    /// <summary>
    /// Extension to configure OpenTelemetry.
    /// </summary>
    /// <param name="services">IServiceCollection.</param>.
    /// <param name="configuration">IConfiguration.</param>.
    public static void RegisterOpenTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
        if (string.IsNullOrEmpty(connectionString))
        {
            return;
        }

        services.AddOpenTelemetry()
            .UseAzureMonitor(options =>
            {
                options.ConnectionString = connectionString;
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource("Pulse.Back.Events");
            });
    }

    /// <summary>
    /// Get authentication token.
    /// </summary>
    /// <param name="services">IServiceCollection.</param>
    /// <param name="configuration">IConfiguration.</param>
    public static void GetToken(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TokenModel>(configuration);
    }
}
