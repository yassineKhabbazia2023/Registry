// <copyright file="Program.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application;
using Infrastructure;
using Infrastructure.Context;
using Infrastructure.Options;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;

namespace ContactRegistry.AzureFuctions;

/// <summary>
/// Program partial class.
/// </summary>
[ExcludeFromCodeCoverage]
public partial class Program
{
    public static void Main(string[] args)
    {
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

                IConfigurationSection referentielSection = config.GetSection("Referential");
                services.Configure<ReferentialOptions>(referentielSection);

                services.AddHttpClient("RegistryApi", (serviceProvider, httpClient) =>
                {
                    var referentielOptions = serviceProvider.GetRequiredService<IOptions<ReferentialOptions>>().Value;
                    httpClient.BaseAddress = new Uri(config["RegistryApiUrl"]!);
                    httpClient.DefaultRequestHeaders.Add("X-Correlation-Id", referentielOptions.CorrelationId);
                    httpClient.DefaultRequestHeaders.Add("X-Client-Id", referentielOptions.ClientId);
                    httpClient.DefaultRequestHeaders.Add("X-Client-Secret", referentielOptions.ClientSecret);
                    httpClient.DefaultRequestHeaders.Add("Authorization", referentielOptions.Authorization);
                });

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
    }
}
