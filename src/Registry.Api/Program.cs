// <copyright file="Program.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application;
using Application.Interfaces;
using Application.Options;
using Application.Providers;
using Hangfire;
using Kpmg.ExceptionMiddleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebApi.Configurations;
using Registry.WebApi.Logging;

namespace Registry.WebApi;

/// <summary>
/// Program partial class.
/// </summary>
[ExcludeFromCodeCoverage]
public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers(options =>
        {
            options.InputFormatters.Insert(0, new PlainTextInputFormatter());
        })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        })
        .AddControllersAsServices()
        .AddNewtonsoftJson(options =>
        {
            options.SerializerSettings.ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() };
            options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            options.SerializerSettings.DateParseHandling = DateParseHandling.None;
            options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        });

        builder.Services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                ProspectCreationValidationLogger.Log(context, "Pulse.Back.Registry");
                var problemDetailsFactory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
                var validationProblemDetails = problemDetailsFactory.CreateValidationProblemDetails(context.HttpContext, context.ModelState);
                return new BadRequestObjectResult(validationProblemDetails);
            };
        });

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.AddServer(new OpenApiServer()
            {
                Url = "/",
            });
            c.AddServer(new OpenApiServer()
            {
                Url = "/registry",
            });
            var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
            c.UseInlineDefinitionsForEnums();
            c.MapType<JObject>(() => new OpenApiSchema
            {
                Type = "object",
                AdditionalPropertiesAllowed = true,
                AdditionalProperties = new OpenApiSchema()
            });
        });

        builder.Services.AddInfrastructureServices(builder.Configuration);
        builder.Services.AddHealthChecks();
        builder.Services.AddProblemDetails();
        builder.Services.RegisterOpenTelemetry(builder.Configuration);

        builder.Logging.Configure(options =>
        {
            options.ActivityTrackingOptions =
                Microsoft.Extensions.Logging.ActivityTrackingOptions.TraceId |
                Microsoft.Extensions.Logging.ActivityTrackingOptions.SpanId;
        });
        builder.Services.GetToken(builder.Configuration);
        builder.Services.AddSingleton<IHeaderTokenValidator, HeaderTokenValidator>();

        IConfigurationSection referentielTokenSection = builder.Configuration.GetSection("ReferentialToken");
        builder.Services.Configure<ReferentialTokenOptions>(referentielTokenSection);

        builder.Services.AddHttpClient("ReferentialToken", (serviceProvider, httpClient) =>
        {
            var referentielTokenOptions = serviceProvider.GetRequiredService<IOptions<ReferentialTokenOptions>>().Value;
            httpClient.BaseAddress = new Uri(referentielTokenOptions.TokenUrl);
        })
            .AddHttpMessageHandler<ReferentialTokenContentHandler>();

        IConfigurationSection referentielSection = builder.Configuration.GetSection("Referential");
        builder.Services.Configure<ReferentialOptions>(referentielSection);

        builder.Services.AddHttpClient("RegistryApi",(serviceProvider, httpClient) =>
        {
            var referentielOptions = serviceProvider.GetRequiredService<IOptions<ReferentialOptions>>().Value;
            var referentialTokenService = serviceProvider.GetRequiredService<IReferentialTokenProvider>();

            httpClient.BaseAddress = new Uri(builder.Configuration["RegistryApiUrl"]!);
            httpClient.DefaultRequestHeaders.Add("X-Client-Id", referentielOptions.ClientId);
            httpClient.DefaultRequestHeaders.Add("X-Client-Secret", referentielOptions.ClientSecret);
            var authorization = referentialTokenService.GenerateTokenAsync().Result;

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",authorization?.AccessToken);
        });

        IConfigurationSection hubSpotSection = builder.Configuration.GetSection("HubSpot");
        builder.Services.Configure<HubSpotOptions>(hubSpotSection);

        builder.Services.AddHttpClient("HubSpot", (serviceProvider, httpClient) =>
        {
            var hubSpotOptions = serviceProvider.GetRequiredService<IOptions<HubSpotOptions>>().Value;

            if (string.IsNullOrWhiteSpace(hubSpotOptions.BaseUrl))
            {
                throw new ArgumentException("HubSpot:BaseUrl must be provided.");
            }

            if (string.IsNullOrWhiteSpace(hubSpotOptions.PortalId))
            {
                throw new ArgumentException("HubSpot:PortalId must be provided.");
            }

            if (string.IsNullOrWhiteSpace(hubSpotOptions.FormGuid))
            {
                throw new ArgumentException("HubSpot:FormGuid must be provided.");
            }

            httpClient.BaseAddress = new Uri(hubSpotOptions.BaseUrl, UriKind.Absolute);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        builder.Services.AddAkuiteoConfiguration(builder.Configuration);

        var app = builder.Build();
        app.UseExceptionMiddleware();

        // Configure the HTTP request pipeline.
        app.UseSwagger(option =>
        {
            option.RouteTemplate = "/registry/api/{documentName}/api.json";
        });
        var assemblyName = typeof(Program).Assembly.GetName().Name;
        app.UseSwaggerUI(c =>
        {
            c.EnableTryItOutByDefault();
            c.SwaggerEndpoint("/registry/api/v1/api.json", $"{assemblyName} v1");
            c.RoutePrefix = "api";
        });

        app.UseHttpsRedirection();

        app.MapControllers();
        app.MapHealthChecks("/health");


        app.UseStaticFiles();

        app.UseRouting();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });

        app.MapFallbackToFile("index.html");

        app.UseHangfireServer();
        app.UseHangfireDashboard();
        app.Run();
    }
}
