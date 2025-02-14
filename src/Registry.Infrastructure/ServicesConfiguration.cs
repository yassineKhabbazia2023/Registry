// <copyright file="ServicesConfiguration.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Pulse.Back.Events;
using Pulse.Back.Events.Abstractions;
using Pulse.Back.Events.Configurations;
using System.Diagnostics.CodeAnalysis;
using Application.Configurations;
using Registry.Infrastructure;
using Registry.Infrastructure.Options;

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

            var brokerSettings = configuration!.GetSection("BrokerSetting").Get<BrokerSetting>();
            ArgumentException.ThrowIfNullOrEmpty(brokerSettings!.FullyQualifiedNamespace);
            if (brokerSettings!.PushTopicName?.Count == 0)
            {
                throw new ArgumentException("PushTopicNames parameter should at least have one value");
            }

            var options = new BrokerOptions
            {
                ServiceBusNamespace = brokerSettings!.FullyQualifiedNamespace!,
                ManagedIdentityClientId = brokerSettings!.ManagedIdentityClientId!,
                PushTopicNames = brokerSettings!.PushTopicName ?? new List<string>(),
            };

            services.AddEventPushServices(options);

            services.Configure<ServiceBusOptions>(opt =>
            {
                opt.ServiceBusContactQueueName = configuration["ServiceBusQueueProcessName"]!;
                opt.ServiceBusRegistryTopicName = configuration["ServiceBusTopicRegisteryName"]!;
            });


            services.AddAzureClients(builder =>
            {
                builder.AddServiceBusClientWithNamespace(brokerSettings.FullyQualifiedNamespace)
                  .WithCredential(new DefaultAzureCredential(new DefaultAzureCredentialOptions
                   {
                       ManagedIdentityClientId = configuration[brokerSettings?.ManagedIdentityClientId],
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


        }
    }
}
