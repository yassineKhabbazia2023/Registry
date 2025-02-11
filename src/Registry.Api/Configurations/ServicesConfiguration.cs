// <copyright file="ServicesConfiguration.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Pulse.Back.Events;
using System.Diagnostics.CodeAnalysis;
using WebApi.Configurations.Models;
using Pulse.Back.Events.Configurations;

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

    /// <summary>
    /// Register Service Bus Broker
    /// </summary>
    /// <param name="services">IServiceCollection.</param>
    /// <param name="configuration">IConfiguration.</param>
    public static void RegisterBroker(this IServiceCollection services, IConfiguration configuration)
    {
        var brokerSettings = configuration!.GetSection("BrokerSetting").Get<BrokerSetting>();

        ArgumentNullException.ThrowIfNull(brokerSettings);

        var options = new BrokerOptions
        {
            ServiceBusNamespace = brokerSettings.ServiceBusNamespace!,
            ManagedIdentityClientId = brokerSettings.ManagedIdentityClientId!,
        };

        if (brokerSettings.PullTopics?.Count != 0)
        {
            foreach (var topic in brokerSettings.PullTopics!)
            {
                if (!options.PullTopics.ContainsKey(topic.TopicName))
                {
                    options.AddPullTopicItem(topic.TopicName!, topic.Subscriptions!);
                }
            }
        }

        services.AddEventPullServices(options);
    }
}
