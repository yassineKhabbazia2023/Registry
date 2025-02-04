// <copyright file="DependencyInjection.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>
using Application.Configurations;
using Application.Interfaces;
using Application.Options;
using Application.Providers;
using Application.Repository;
using Azure.Identity;
using Infrastructure.Managers;
using Infrastructure.Providers;
using Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pulse.Back.Events.Abstractions;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.ContactRegistry.Domain.Context;
using System.Diagnostics.CodeAnalysis;

namespace Application;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
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
        services.AddScoped<IOperationRepository, OperationRepository>();
        services.AddScoped<IRoleRegistryProvider, RoleRegistryProvider>();
        services.AddScoped<IAccountRegistryProvider, AccountRegistryProvider>();
        services.AddScoped<IContactRegistryProvider, ContactRegistryProvider>();
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

        var blobStorage = configuration!.GetSection("BlobStorage").Get<BlobStorageOptions>() ?? throw new ArgumentException("BlobStorage parameters should been provided");
        var brokerSettings = configuration!.GetSection("BrokerSetting").Get<BrokerSetting>();

        ArgumentException.ThrowIfNullOrEmpty(brokerSettings?.ManagedIdentityClientId);
        ArgumentException.ThrowIfNullOrEmpty(blobStorage!.BlobUri);

        services.AddAzureClients(delegate (AzureClientFactoryBuilder builder)
        {
            builder.AddBlobServiceClient(blobStorage.BlobUri)
                    .WithCredential(new DefaultAzureCredential(new DefaultAzureCredentialOptions
                    {
                        ManagedIdentityClientId = brokerSettings.ManagedIdentityClientId,
                    }));
        });

        services.AddScoped<IBlobStorageManager, BlobStorageManager>();
    }
}