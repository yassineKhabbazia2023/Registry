// <copyright file="AkuiteoConfigurationExtensions.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Options;
using Application.Providers;
using Application.Interfaces;
using Application.Services;
using Azure.Identity;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;

namespace WebApi.Configurations;

/// <summary>
/// Provides Akuiteo configuration extensions.
/// </summary>
[ExcludeFromCodeCoverage]
public static class AkuiteoConfigurationExtensions
{
    /// <summary>
    /// Registers Akuiteo options validation and HTTP clients.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    public static void AddAkuiteoConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var akuiteoSection = configuration.GetSection("Akuiteo");
        services
            .AddOptions<AkuiteoOptions>()
            .Bind(akuiteoSection)
            .PostConfigure(options =>
            {
                var useMockMode = ResolveUseMockMode(configuration);
                if (useMockMode.HasValue)
                {
                    options.UseMockMode = useMockMode.Value;
                }
            })
            .Validate(
                options => options.UseMockMode
                    || (
                        !string.IsNullOrWhiteSpace(options.BaseUrl)
                        && !string.IsNullOrWhiteSpace(options.ClientIdRateLimiting)
                        && !string.IsNullOrWhiteSpace(options.ClientSecretRateLimiting)
                        && !string.IsNullOrWhiteSpace(options.Tenant)
                        && !string.IsNullOrWhiteSpace(options.TokenClientId)
                        && !string.IsNullOrWhiteSpace(options.TokenClientSecret)
                        && !string.IsNullOrWhiteSpace(options.TokenScope)),
                "Akuiteo real mode requires BaseUrl, ClientIdRateLimiting, ClientSecretRateLimiting, Tenant, TokenClientId, TokenClientSecret and TokenScope.")
            .ValidateOnStart();

        var akuiteoOptions = akuiteoSection.Get<AkuiteoOptions>() ?? new AkuiteoOptions();
        var useMockMode = ResolveUseMockMode(configuration);
        if (useMockMode.HasValue)
        {
            akuiteoOptions.UseMockMode = useMockMode.Value;
        }

        if (akuiteoOptions.UseMockMode)
        {
            services.AddScoped<IAkuiteoDocumentService, AkuiteoDocumentService>();
            services.AddScoped<IAkuiteoCustomerService, AkuiteoMockCustomerService>();
            services.AddScoped<IAkuiteoContactService, AkuiteoMockContactService>();
            services.AddScoped<IAccountOnboardingEligibilityProvider, AkuiteoEligibilityStubProvider>();

            ConfigureAkuiteoAuthentication(
                services.AddHttpClient<IAkuiteoDocumentProvider, AkuiteoDocumentProvider>(ConfigureAkuiteoHttpClient));
        }
        else
        {
            services.AddScoped<IAkuiteoCustomerService, AkuiteoCustomerService>();
            services.AddScoped<IAkuiteoContactService, AkuiteoContactService>();
            services.AddScoped<IAkuiteoDocumentService, AkuiteoDocumentService>();

            ConfigureAkuiteoAuthentication(
                services.AddHttpClient<IAkuiteoCustomerProvider, AkuiteoCustomerProvider>(ConfigureAkuiteoHttpClient));
            ConfigureAkuiteoAuthentication(
                services.AddHttpClient<IAkuiteoContactProvider, AkuiteoContactProvider>(ConfigureAkuiteoHttpClient));
            ConfigureAkuiteoAuthentication(
                services.AddHttpClient<IAkuiteoDocumentProvider, AkuiteoDocumentProvider>(ConfigureAkuiteoHttpClient));
            ConfigureAkuiteoAuthentication(
                services.AddHttpClient<IAccountOnboardingEligibilityProvider, AkuiteoEligibilityProvider>(ConfigureAkuiteoHttpClient));
        }
    }

    /// <summary>
    /// Configures the typed Akuiteo HTTP client.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="httpClient">The HTTP client.</param>
    private static void ConfigureAkuiteoHttpClient(IServiceProvider serviceProvider, HttpClient httpClient)
    {
        var akuiteoOptions = serviceProvider.GetRequiredService<IOptions<AkuiteoOptions>>().Value;

        if (!string.IsNullOrWhiteSpace(akuiteoOptions.BaseUrl))
        {
            httpClient.BaseAddress = akuiteoOptions.BuildApiBaseUrl();
        }

        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (!string.IsNullOrWhiteSpace(akuiteoOptions.ClientIdRateLimiting))
        {
            httpClient.DefaultRequestHeaders.Add("X-Client-Id", akuiteoOptions.ClientIdRateLimiting);
        }

        if (!string.IsNullOrWhiteSpace(akuiteoOptions.ClientSecretRateLimiting))
        {
            httpClient.DefaultRequestHeaders.Add("X-Client-Secret", akuiteoOptions.ClientSecretRateLimiting);
        }
    }

    /// <summary>
    /// Adds Azure AD authentication to an Akuiteo typed HTTP client.
    /// </summary>
    /// <param name="httpClientBuilder">The HTTP client builder.</param>
    private static void ConfigureAkuiteoAuthentication(IHttpClientBuilder httpClientBuilder)
    {
        httpClientBuilder.AddHttpMessageHandler(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<AkuiteoOptions>>().Value;
            var credential = new ClientSecretCredential(
                options.Tenant,
                options.TokenClientId,
                options.TokenClientSecret);
            var scopes = new[]
            {
                options.TokenScope ?? $"api://{options.TokenClientId}/.default"
            };

            return new AkuiteoBearerTokenHandler(
                credential,
                scopes,
                serviceProvider.GetRequiredService<ILogger<AkuiteoBearerTokenHandler>>());
        });
    }

    /// <summary>
    /// Resolves the root-level UseMockMode environment override.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The configured mock mode when the override is provided; otherwise <see langword="null"/>.</returns>
    private static bool? ResolveUseMockMode(IConfiguration configuration)
    {
        var configuredValue = configuration["UseMockMode"];
        return bool.TryParse(configuredValue, out var useMockMode) ? useMockMode : null;
    }
}
