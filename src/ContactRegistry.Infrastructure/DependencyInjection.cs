// <copyright file="DependencyInjection.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>
using Application.Interfaces;
using Infrastructure.Context;
using Infrastructure.Providers;
using Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pulse.ContactRegistry.Infrastructure.Context;
using System.Diagnostics.CodeAnalysis;

namespace Infrastructure;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentException.ThrowIfNullOrEmpty(configuration["DatabaseConnectionString"]);
        services.AddDbContext<ApplicationDbContext>(

             options => {

                 options.UseSqlServer(

                     configuration["DatabaseConnectionString"], sqlServerOptionsAction: sqlOptions => {

                         sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);

                         sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);

                         sqlOptions.CommandTimeout(120);
                     });

             },

             ServiceLifetime.Scoped);
        services.AddDbContext<RefContext>(

             options => {

                 options.UseSqlServer(

                     configuration["DatabaseConnectionString"], sqlServerOptionsAction: sqlOptions => {

                         sqlOptions.MigrationsAssembly(typeof(RefContext).Assembly.FullName);

                         sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);

                         sqlOptions.CommandTimeout(120);
                     });

             },

             ServiceLifetime.Scoped);
        services.AddScoped<IContactRepository, ContactRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IOperationRepository, OperationRepository>();
        services.AddScoped<IProcessDeltaTriggerRepository, ProcessDeltaTriggerRepository>();
        services.AddScoped<IRoleRegistryProvider, RoleRegistryProvider>();
    }
}