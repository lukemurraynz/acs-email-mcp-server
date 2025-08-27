using Azure.Communication.Email;
using Azure.Core;
using Azure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.AspNetCore;
using AcsEmailMcp.Tools;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

var port = Environment.GetEnvironmentVariable("FUNCTIONS_CUSTOMHANDLER_PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddMcpServer()
    .WithHttpTransport((options) =>
    {
        options.Stateless = true;
    })
    .WithTools<EmailTools>();

builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Add request logging
builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;
    logging.RequestBodyLogLimit = 4096;
    logging.ResponseBodyLogLimit = 4096;
});

builder.Services.AddSingleton<EmailClient>(serviceProvider =>
{
    var logger = serviceProvider.GetRequiredService<ILogger<EmailClient>>();
    
    // Try to get ACS endpoint from environment variables
    var acsEndpoint = Environment.GetEnvironmentVariable("ACS_ENDPOINT");
    
    if (!string.IsNullOrEmpty(acsEndpoint))
    {
        logger.LogInformation("Configuring EmailClient with ACS endpoint: {Endpoint}", acsEndpoint);
        
        // Check for user-assigned managed identity client ID
        var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
        
        TokenCredential credential;
        if (!string.IsNullOrEmpty(clientId))
        {
            logger.LogInformation("Using user-assigned managed identity with client ID: {ClientId}", clientId);
        }
        else
        {
            logger.LogInformation("Using default Azure credential (system-assigned managed identity or local development)");
        }
        credential = new DefaultAzureCredential();
        
        return new EmailClient(new Uri(acsEndpoint), credential);
    }
    else
    {
        logger.LogWarning("ACS_ENDPOINT environment variable not found. EmailClient will not be functional.");
        
        // Return a placeholder client - in production this should throw an exception
        // For now, we'll create a minimal client that will fail gracefully
        return new EmailClient("endpoint=https://placeholder.communication.azure.com/;accesskey=placeholder");
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseSerilogRequestLogging();

if (builder.Environment.IsDevelopment())
{
    app.UseHttpLogging();
}

// Add health check endpoint with detailed response
app.MapGet("/api/healthz", (ILogger<Program> logger) => 
{
    logger.LogInformation("Health check requested");
    return Results.Ok(new { 
        Status = "Healthy", 
        Timestamp = DateTime.UtcNow,
        Version = "1.0.0",
        Environment = builder.Environment.EnvironmentName
    });
});

// Add readiness check
app.MapGet("/api/ready", (EmailClient emailClient, ILogger<Program> logger) => 
{
    try
    {
        // Basic readiness check - ensure EmailClient is configured
        var isReady = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ACS_ENDPOINT"));
        logger.LogInformation("Readiness check requested. Ready: {IsReady}", isReady);
        
        return isReady 
            ? Results.Ok(new { Status = "Ready", Timestamp = DateTime.UtcNow })
            : Results.Problem(
                detail: "ACS_ENDPOINT not configured", 
                statusCode: 503, 
                title: "Service Unavailable");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Readiness check failed");
        return Results.Problem(
            detail: ex.Message,
            statusCode: 503,
            title: "Service Unavailable");
    }
});

// Map MCP endpoints
app.MapMcp(pattern: "/mcp");

try
{
    Log.Information("Starting ACS Email MCP server");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class accessible for testing
public partial class Program { }
