// <copyright file="ServicesConfiguration.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Azure.Identity;
using Azure.Messaging.ServiceBus;
using ContactRegistry.AzureFuctions.Logging;
using ContactRegistry.AzureFuctions.Managers;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pulse.Back.Events;
using Pulse.Back.Events.Configurations;
using System.Diagnostics.CodeAnalysis;

namespace ContactRegistry.AzureFuctions
{
    /// <summary>
    /// ServiceConfiguration extension.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class ServicesConfiguration
    {
        /// <summary>
        /// Extension to configure applicationInsight.
        /// </summary>
        /// <param name="services">IServiceCollection./param>.
        /// <param name="configuration">configuration.</param>.
        public static void AddServiceBusConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            ArgumentException.ThrowIfNullOrEmpty(configuration["ServiceBusQueueProcessName"]);
            ArgumentException.ThrowIfNullOrEmpty(configuration["ServiceBusTopicRegisteryName"]);

            var brokerSettings = configuration!.GetSection("hubServiceBus").Get<BrokerSetting>();
            ArgumentException.ThrowIfNullOrEmpty(brokerSettings!.FullyQualifiedNamespace);
            if (brokerSettings!.PushTopicNames.Count == 0)
            {
                throw new ArgumentException("PushTopicNames parameter should at least have one value");
            }

            var options = new BrokerOptions
            {
                ServiceBusNamespace = brokerSettings!.FullyQualifiedNamespace!,
                ManagedIdentityClientId = brokerSettings!.ClientId!,
                PushTopicNames = brokerSettings!.PushTopicNames,
            };

            services.AddEventPushServices(options);

            services.Configure<ServiceBusOptions>(opt =>
            {
                opt.ServiceBusContactQueueName = configuration["ServiceBusQueueProcessName"]!;
                opt.ServiceBusRegistryTopicName = configuration["ServiceBusTopicRegisteryName"]!;
            });

            services.AddAzureClients(builder =>
            {
                builder.AddServiceBusClientWithNamespace(configuration["hubServiceBus:fullyQualifiedNamespace"])
                  .WithCredential(new DefaultAzureCredential(new DefaultAzureCredentialOptions
                  {
                      ManagedIdentityClientId = configuration["hubServiceBus:clientId"],
                  }));
                builder.AddClient<ServiceBusSender, ServiceBusClientOptions>((_, _, provider) =>
                    provider
                        !.GetService<ServiceBusClient>()
                        !.CreateSender(configuration["ServiceBusQueueProcessName"]))
                .WithName(configuration["ServiceBusQueueProcessName"]);
            });
            services.AddScoped<INotificationManager, NotificationsManager>();
            services.AddScoped<IReplaySafeLoggerAdapter, ReplaySafeLoggerAdapter>();
        }
    }
}
