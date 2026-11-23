using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.RateLimiting;
using TrafficInsight.Api.Services;

var builder = WebApplication.CreateBuilder(args);
var rateLimitPerMinute = builder.Configuration.GetValue("Traffic:RateLimitPerMinute", 120);
var upstreamTimeoutSeconds = builder.Configuration.GetValue("Digitraffic:RequestTimeoutSeconds", 15);

if (rateLimitPerMinute < 1 || upstreamTimeoutSeconds is < 1 or > 60)
{
    throw new InvalidOperationException("Traffic rate limits and Digitraffic timeout are outside their allowed range.");
}

builder.Services.AddMemoryCache();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("traffic-read", limiter =>
    {
        limiter.PermitLimit = rateLimitPerMinute;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
        limiter.AutoReplenishment = true;
    });
});

if (!builder.Environment.IsDevelopment())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddJsonConsole();
}

var digitrafficBaseUrl = builder.Configuration["Digitraffic:BaseUrl"]
    ?? "https://tie.digitraffic.fi/";

if (!Uri.TryCreate(digitrafficBaseUrl, UriKind.Absolute, out var digitrafficUri) ||
    digitrafficUri.Scheme != Uri.UriSchemeHttps)
{
    throw new InvalidOperationException("Digitraffic:BaseUrl must be an HTTPS URL.");
}

builder.Services
    .AddHttpClient<DigitrafficService>(client =>
    {
        client.BaseAddress = digitrafficUri;
        client.Timeout = TimeSpan.FromSeconds(upstreamTimeoutSeconds);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("TrafficInsight/1.0");
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AutomaticDecompression =
            DecompressionMethods.GZip |
            DecompressionMethods.Deflate |
            DecompressionMethods.Brotli
    });

var allowedOrigins = builder.Configuration
    .GetSection("AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:5173"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactFrontend", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("ReactFrontend");
app.UseRateLimiter();

app.MapGet("/", () => Results.Ok(new
{
    name = "Traffic Insight API",
    status = "healthy"
}));

app.MapHealthChecks("/health");

app.MapGet("/api/traffic/external", async (
    DigitrafficService digitrafficService,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    try
    {
        var measurements = await digitrafficService.GetMeasurementsAsync(cancellationToken);
        return Results.Ok(measurements);
    }
    catch (HttpRequestException exception)
    {
        logger.LogWarning(exception, "The Digitraffic request failed.");
        return Results.Problem(
            title: "Digitraffic API unavailable",
            detail: "Unable to retrieve live traffic data.",
            statusCode: StatusCodes.Status502BadGateway);
    }
    catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
    {
        logger.LogWarning(exception, "The Digitraffic request timed out.");
        return Results.Problem(
            title: "Digitraffic API timed out",
            detail: "The live traffic service did not respond in time.",
            statusCode: StatusCodes.Status504GatewayTimeout);
    }
    catch (JsonException exception)
    {
        logger.LogWarning(exception, "Digitraffic returned an unexpected response format.");
        return Results.Problem(
            title: "Unexpected Digitraffic response",
            detail: "The live traffic service returned data in an unexpected format.",
            statusCode: StatusCodes.Status502BadGateway);
    }
})
.RequireRateLimiting("traffic-read");

app.Run();

public partial class Program { }
