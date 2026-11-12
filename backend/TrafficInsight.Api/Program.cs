
using TrafficInsight.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// Allow requests from our React frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Enable CORS
app.UseCors("ReactFrontend");

// Temporary in-memory traffic data
var trafficData = new List<TrafficMeasurement>
{
    new TrafficMeasurement
    {
        Id = 1,
        Location = "Helsinki Central",
        Vehicles = 1250,
        Speed = 42,
        Status = "Normal"
    },
    new TrafficMeasurement
    {
        Id = 2,
        Location = "Tampere Junction",
        Vehicles = 2100,
        Speed = 24,
        Status = "Heavy"
    },
    new TrafficMeasurement
    {
        Id = 3,
        Location = "Turku Highway",
        Vehicles = 980,
        Speed = 65,
        Status = "Normal"
    },
    new TrafficMeasurement
    {
        Id = 4,
        Location = "Oulu North",
        Vehicles = 1450,
        Speed = 38,
        Status = "Normal"
    },
    new TrafficMeasurement
    {
        Id = 5,
        Location = "Espoo Centre",
        Vehicles = 1850,
        Speed = 18,
        Status = "Heavy"
    }
};

// GET: Return all traffic measurements
app.MapGet("/api/traffic", () =>
{
    return Results.Ok(trafficData);
});

// POST: Add a new traffic measurement
app.MapPost("/api/traffic", (TrafficMeasurement newMeasurement) =>
{
    // Validate the incoming data
    if (string.IsNullOrWhiteSpace(newMeasurement.Location))
    {
        return Results.BadRequest("Location is required.");
    }

    if (newMeasurement.Vehicles < 0 || newMeasurement.Speed < 0)
    {
        return Results.BadRequest("Vehicles and speed cannot be negative.");
    }

    if (newMeasurement.Status != "Normal" &&
        newMeasurement.Status != "Heavy")
    {
        return Results.BadRequest("Status must be Normal or Heavy.");
    }

    // Create a new measurement with an ID assigned by the backend
    var measurement = new TrafficMeasurement
    {
        Id = trafficData.Count > 0
            ? trafficData.Max(item => item.Id) + 1
            : 1,

        Location = newMeasurement.Location.Trim(),
        Vehicles = newMeasurement.Vehicles,
        Speed = newMeasurement.Speed,
        Status = newMeasurement.Status
    };

    // Add the measurement to our list
    trafficData.Add(measurement);

    // Return the created measurement
    return Results.Created(
        $"/api/traffic/{measurement.Id}",
        measurement
    );
});

app.Run();
