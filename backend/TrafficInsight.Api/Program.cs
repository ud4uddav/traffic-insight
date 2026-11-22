using System.Net;
using System.Text.Json;
using TrafficInsight.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services
    .AddHttpClient<DigitrafficService>(client =>
    {
        client.BaseAddress = new Uri("https://tie.digitraffic.fi/");
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

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapGet("/api/traffic/external", async (
    DigitrafficService digitrafficService,
    CancellationToken cancellationToken) =>
{
    try
    {
        var measurements = await digitrafficService.GetMeasurementsAsync(cancellationToken);
        return Results.Ok(measurements);
    }
    catch (HttpRequestException)
    {
        return Results.Problem(
            title: "Digitraffic API unavailable",
            detail: "Unable to retrieve live traffic data.",
            statusCode: StatusCodes.Status502BadGateway);
    }
    catch (JsonException)
    {
        return Results.Problem(
            title: "Unexpected Digitraffic response",
            detail: "The live traffic service returned data in an unexpected format.",
            statusCode: StatusCodes.Status502BadGateway);
    }
});

app.Run();

public partial class Program { }
