// <copyright file="ServicesConfiguration.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Registry.AzureFuctions.Logging;
using Registry.AzureFuctions.Managers;
using Registry.AzureFuctions.Options;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Pulse.Back.Events;
using Pulse.Back.Events.Abstractions;
using Pulse.Back.Events.Configurations;
using System.Diagnostics.CodeAnalysis;

namespace Registry.AzureFuctions
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
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            ArgumentException.ThrowIfNullOrEmpty(configuration["ServiceBusQueueProcessName"]);
            ArgumentException.ThrowIfNullOrEmpty(configuration["ServiceBusTopicRegisteryName"]);
            ArgumentException.ThrowIfNullOrEmpty(configuration["ProcessEventPublishBatchSize"]);

            services.Configure<ProcessEventPublishOptions>(options =>
            {
                options.ProcessEventPublishBatchSize = int.Parse(configuration["ProcessEventPublishBatchSize"]!);
            });

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
                builder.AddClient<ServiceBusSender, ServiceBusClientOptions>((_, _, provider) =>
                    provider
                        !.GetService<ServiceBusClient>()
                        !.CreateSender(configuration["ServiceBusTopicRegisteryName"]))
                .WithName(configuration["ServiceBusTopicRegisteryName"]);
            });
            services.AddScoped<INotificationManager, NotificationsManager>();
            services.AddScoped<IReplaySafeLoggerAdapter, ReplaySafeLoggerAdapter>();
        }
    }
}
