// <copyright file="ServicesConfiguration.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

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
    /// Extension to configure applicationInsight.
    /// </summary>
    /// <param name="services">IServiceCollection.</param>.
    /// <param name="configuration">IConfiguration.</param>.
    public static void RegisterApplicationInsights(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"], "APPLICATIONINSIGHTS_CONNECTION_STRING");
        var applicationInsightsConexionString = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];

        services.AddApplicationInsightsTelemetry(options =>
        {
            options.ConnectionString = applicationInsightsConexionString;
        })
        .AddLogging(logging =>
        {
            logging.AddApplicationInsights();
            if (Enum.TryParse<Microsoft.Extensions.Logging.LogLevel>(configuration["LogLevel"], out var logLevel))
            {
                logging.AddFilter<Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider>(string.Empty, logLevel);
                logging.AddFilter<Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider>("Microsoft.AspNetCore", LogLevel.Warning);
            }
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
