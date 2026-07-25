using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using Scalar.AspNetCore;
using UltimaAPI.Configuration;
using UltimaAPI.Endpoints;
using UltimaAPI.Infrastructure;
using UltimaAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add modern .NET 10 native OpenAPI services
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

// Configure the JSON serializer to output readable formatted JSON for your AI
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.WriteIndented = true;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});
builder.Services.Configure<UltimaOptions>(builder.Configuration.GetSection(UltimaOptions.SectionName));
builder.Services.AddSingleton<UltimaSdkGateway>();
builder.Services.AddSingleton<TileDataService>();
builder.Services.AddSingleton<MapService>();
builder.Services.AddSingleton<VisualAssetService>();
builder.Services.AddSingleton<AudioAnimationService>();
builder.Services.AddSingleton<SemanticDataService>();

var app = builder.Build();

app.UseExceptionHandler();

// Enable the Native OpenAPI document generation and Scalar UI
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // Serves the JSON spec at /openapi/v1.json
    app.MapScalarApiReference(); // Serves the gorgeous interactive UI at /scalar
}

UltimaSdkGateway sdk = app.Services.GetRequiredService<UltimaSdkGateway>();
app.MapUltimaEndpoints();
app.MapMcp("/mcp");

app.Run();

partial class Program;
