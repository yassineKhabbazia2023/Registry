// <copyright file="Program.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application;
using Application.Interfaces;
using ContactRegistry.AzureFuctions;
using Infrastructure;
using Infrastructure.Context;
using Infrastructure.Repository;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

var config = new ConfigurationBuilder()
      .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
      .AddEnvironmentVariables()
      .Build();

ArgumentException.ThrowIfNullOrEmpty(config["DatabaseConnectionString"]);
var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddApplicationServices();
        services.AddInfrastructureServices(config);
        services.AddServiceBusConfiguration(config);
        services.AddDbContextFactory<ApplicationDbContext>(
            options =>
            options.UseSqlServer(config["DatabaseConnectionString"]),
            ServiceLifetime.Scoped);
    })
    .ConfigureLogging(logging =>
    {
        logging.Services.Configure<LoggerFilterOptions>(options =>
        {
            LoggerFilterRule? defaultRule = options.Rules.FirstOrDefault(rule => rule.ProviderName == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");
            if (defaultRule is not null)
            {
                options.Rules.Remove(defaultRule);
            }
        });
    })
    .Build();

host.Run();

/// <summary>
/// Program partial class.
/// </summary>
[ExcludeFromCodeCoverage]
public partial class Program
{
}
