using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using WebApi.Configurations;
using Application;
using Infrastructure;
using System.Text.Json.Serialization;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
}); ;

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddServer(new Microsoft.OpenApi.Models.OpenApiServer()
    {
        Url = "/",
    });
    c.AddServer(new Microsoft.OpenApi.Models.OpenApiServer()
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

app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

/// <summary>
/// Program partial class.
/// </summary>
[ExcludeFromCodeCoverage]
public partial class Program
{
}