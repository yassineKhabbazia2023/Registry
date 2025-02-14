// <copyright file="DependencyInjection.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>
using Application.Interfaces;
using Application.Options;
using Application.Providers;
using Application.Repository;
using Azure.Identity;
using Hangfire;
using Hangfire.MemoryStorage;
using Infrastructure.Adapters;
using Infrastructure.BackgroundJobs;
using Infrastructure.Managers;
using Infrastructure.Orchestrators;
using Infrastructure.Providers;
using Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pulse.Back.Events;
using Pulse.Back.Events.Abstractions;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.Registry.Domain.Context;
using Registry.AzureFuctions;
using Registry.Infrastructure.Managers;
using Registry.Infrastructure.Options;
using System.Diagnostics.CodeAnalysis;

namespace Application;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplicationServices();

        services.AddServiceBusConfiguration(configuration);
        ArgumentException.ThrowIfNullOrEmpty(configuration["DatabaseConnectionString"]);

        services.AddDbContext<RefContext>(

             options =>
             {

                 options.UseSqlServer(

                     configuration["DatabaseConnectionString"], sqlServerOptionsAction: sqlOptions =>
                     {

                         sqlOptions.MigrationsAssembly(typeof(RefContext).Assembly.FullName);

                         sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);

                         sqlOptions.CommandTimeout(120);
                     });

             },

             ServiceLifetime.Scoped);
        services.AddTransient<ReferentialTokenContentHandler>();
        services.AddSingleton<IReferentialTokenProvider, ReferentialTokenProvider>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IContactRepository,ContactRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IDeepValidationRepository, DeepValidationRepository>();
        services.AddScoped<IOperationRepository, OperationRepository>();
        services.AddScoped<IRoleRegistryProvider, RoleRegistryProvider>();
        services.AddScoped<IAccountRegistryProvider, AccountRegistryProvider>();
        services.AddScoped<IContactRegistryProvider, ContactRegistryProvider>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddKeyedScoped<IEventHandler, AccountCreatedEventHandler>(nameof(AccountCreatedEvent));
        services.AddKeyedScoped<IEventHandler, AccountUpdatedEventHandler>(nameof(AccountUpdatedEvent));
        services.AddKeyedScoped<IEventHandler, AccountRemovedEventHandler>(nameof(AccountRemovedEvent));
        services.AddKeyedScoped<IEventHandler, RoleCreatedEventHandler>(nameof(RoleCreatedEvent));
        services.AddKeyedScoped<IEventHandler, RoleDeletedEventHandler>(nameof(RoleDeletedEvent));
        services.AddKeyedScoped<IEventHandler, ContactCreatedEventHandler>(nameof(ContactCreatedEvent));
        services.AddKeyedScoped<IEventHandler, ContactUpdatedEventHandler>(nameof(ContactUpdatedEvent));
        services.AddKeyedScoped<IEventHandler,ContactRemovedEventHandler>(nameof(ContactRemovedEvent));

        services.Configure<BlobStorageOptions>(opt =>
        {
            if(configuration is not null)
            {
                opt.ContainerName = configuration["BlobStorageContainerName"] ?? throw new ArgumentException("BlobStorageContainerName parameters should been provided"); 
                opt.BlobUri = configuration["BlobStorageUri"] ?? throw new ArgumentException("BlobStorageUri parameters should been provided");
            }
        });

        var backGroundJobSettings = configuration!.GetSection("BackGroundJob").Get<BackGroundJobOptions>() ?? throw new ArgumentException("BackGroundJob section should be provided"); ;

        services.Configure<BackGroundJobOptions>(opt =>
        {
            if (configuration is not null)
            {
                opt.Chunk = backGroundJobSettings.Chunk;
                opt.TimeToWaitBeforeEachStep = backGroundJobSettings.TimeToWaitBeforeEachStep;
                opt.ShouldTriggerEvents = backGroundJobSettings.ShouldTriggerEvents;
            }
        });

        var brokerSettings = configuration!.GetSection("BrokerSetting").Get<BrokerSetting>();

        ArgumentException.ThrowIfNullOrEmpty(brokerSettings?.ManagedIdentityClientId);

        services.AddAzureClients(delegate (AzureClientFactoryBuilder builder)
        {
            bool useManagedIdentity = configuration["ConnectToBlobViaManagedIdentity"].Equals("true",StringComparison.InvariantCultureIgnoreCase);
            if (useManagedIdentity)
            {
                services.AddAzureClients(delegate (AzureClientFactoryBuilder builder)
                {
                    builder.AddBlobServiceClient(configuration["BlobStorageUri"])
                            .WithCredential(new DefaultAzureCredential(new DefaultAzureCredentialOptions
                            {
                                ManagedIdentityClientId = brokerSettings.ManagedIdentityClientId,
                            }));
                });
            }
            else
            {
                builder.AddBlobServiceClient(configuration["BlobStoragePrimaryConnectionString"]);
            }
        });

        services.AddScoped<IBlobStorageManager, BlobStorageManager>();
        services.AddScoped<IContactOrchestrator,ContactOrchetrator>();
        services.AddScoped<IAccountOrchestrator, AccountOrchestrator>();
        services.AddScoped<IRoleOrchestrator,RoleOrchestrator>();
        services.AddScoped<OrchestratorJob>();
        services.AddScoped<INotificationManager, NotificationsManager>();
        services.AddScoped<IEventPublisher, EventPublisher>();
        services.AddScoped<IServiceBusMessageFactory, ServiceBusMessageFactory>();


        services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseMemoryStorage()
        );
        services.AddHangfireServer();
        services.AddScoped<IBackgroundJobEnqueuer, BackgroundJobEnqueuer>();
    }
}