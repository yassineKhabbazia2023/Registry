// <copyright file="Program.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application;
using Application.Interfaces;
using Application.Options;
using Application.Providers;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pulse.Registry.Domain.Context;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;

namespace Registry.AzureFuctions;

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
                RegisterOpenTelemetry(services, config);
                services.AddInfrastructureServices(config);
                services.AddScoped<IAkuiteoContactService, RegistryApiAkuiteoContactService>();

                IConfigurationSection referentielSection = config.GetSection("Referential");
                services.Configure<ReferentialOptions>(referentielSection);

                services.AddHttpClient("RegistryApi", (serviceProvider, httpClient) =>
                {
                    var referentielOptions = serviceProvider.GetRequiredService<IOptions<ReferentialOptions>>().Value;
                    httpClient.BaseAddress = new Uri(config["RegistryApiUrl"]!);
                    httpClient.DefaultRequestHeaders.Add("X-Client-Id", referentielOptions.ClientId);
                    httpClient.DefaultRequestHeaders.Add("X-Client-Secret", referentielOptions.ClientSecret);
                    if (AuthenticationHeaderValue.TryParse(
                        referentielOptions.Authorization,
                        out var authorizationHeader))
                    {
                        httpClient.DefaultRequestHeaders.Authorization = authorizationHeader;
                    }
                });
            })
            .ConfigureLogging(logging =>
            {
                logging.Configure(options =>
                {
                    options.ActivityTrackingOptions =
                        Microsoft.Extensions.Logging.ActivityTrackingOptions.TraceId |
                        Microsoft.Extensions.Logging.ActivityTrackingOptions.SpanId;
                });
            })
            .Build();

        host.Run();
    }

    private static void RegisterOpenTelemetry(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
        if (string.IsNullOrEmpty(connectionString))
        {
            return;
        }

        services.AddOpenTelemetry()
            .UseAzureMonitor(options =>
            {
                options.ConnectionString = connectionString;
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource("Pulse.Back.Events");
            });
    }
}
