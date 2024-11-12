// <copyright file="Program.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using WebApi.Configurations;
using Application;
using Infrastructure;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Infrastructure.Options;
using System.Net.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using Infrastructure.Providers;
using Application.Interfaces;

namespace ContactRegistry.WebApi;

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

        builder.Services.AddControllers()
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
        });




        builder.Services.AddApplicationServices();
        builder.Services.AddInfrastructureServices(builder.Configuration);
        builder.Services.AddHealthChecks();
        builder.Services.AddProblemDetails();
        builder.Services.RegisterApplicationInsights(builder.Configuration);
        builder.Services.GetToken(builder.Configuration);

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

        builder.Services.AddHttpClient("RegistryApi",async (serviceProvider, httpClient) =>
        {
            var referentielOptions = serviceProvider.GetRequiredService<IOptions<ReferentialOptions>>().Value;
            var referentialTokenService = serviceProvider.GetRequiredService<IReferentialTokenProvider>();

            httpClient.BaseAddress = new Uri(builder.Configuration["RegistryApiUrl"]!);
            httpClient.DefaultRequestHeaders.Add("X-Correlation-Id", Guid.NewGuid().ToString());
            httpClient.DefaultRequestHeaders.Add("X-Client-Id", referentielOptions.ClientId);
            httpClient.DefaultRequestHeaders.Add("X-Client-Secret", referentielOptions.ClientSecret);

            var authorization = await referentialTokenService.GenerateTokenAsync();

            httpClient.DefaultRequestHeaders.Add("Authorization", $"{authorization.TokenType} {authorization.AccessToken}");
        });

        

        var app = builder.Build();

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

        app.Run();
    }
}