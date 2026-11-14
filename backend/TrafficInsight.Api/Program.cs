
using Microsoft.EntityFrameworkCore;
using TrafficInsight.Api.Data;
using TrafficInsight.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// Register PostgreSQL database connection
builder.Services.AddDbContext<TrafficDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("TrafficDb")
    )
);

// Configure CORS for React
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

app.UseCors("ReactFrontend");

// Add initial sample data if the database is empty
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<TrafficDbContext>();

    if (!await db.TrafficMeasurements.AnyAsync())
    {
        db.TrafficMeasurements.AddRange(
            new TrafficMeasurement
            {
                Location = "Helsinki Central",
                Vehicles = 1250,
                Speed = 42,
                Status = "Normal"
            },
            new TrafficMeasurement
            {
                Location = "Tampere Junction",
                Vehicles = 2100,
                Speed = 24,
                Status = "Heavy"
            },
            new TrafficMeasurement
            {
                Location = "Turku Highway",
                Vehicles = 980,
                Speed = 65,
                Status = "Normal"
            },
            new TrafficMeasurement
            {
                Location = "Oulu North",
                Vehicles = 1450,
                Speed = 38,
                Status = "Normal"
            },
            new TrafficMeasurement
            {
                Location = "Espoo Centre",
                Vehicles = 1850,
                Speed = 18,
                Status = "Heavy"
            }
        );

        await db.SaveChangesAsync();
    }
}

// GET: Retrieve all traffic measurements
app.MapGet("/api/traffic", async (TrafficDbContext db) =>
{
    var measurements = await db.TrafficMeasurements
        .OrderBy(item => item.Id)
        .ToListAsync();

    return Results.Ok(measurements);
});

// POST: Add a new traffic measurement
app.MapPost("/api/traffic", async (
    TrafficMeasurement newMeasurement,
    TrafficDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(newMeasurement.Location))
        return Results.BadRequest("Location is required.");

    if (newMeasurement.Vehicles < 0 ||
        newMeasurement.Speed < 0)
        return Results.BadRequest(
            "Vehicles and speed cannot be negative."
        );

    if (newMeasurement.Status != "Normal" &&
        newMeasurement.Status != "Heavy")
        return Results.BadRequest(
            "Status must be Normal or Heavy."
        );

    var measurement = new TrafficMeasurement
    {
        Location = newMeasurement.Location.Trim(),
        Vehicles = newMeasurement.Vehicles,
        Speed = newMeasurement.Speed,
        Status = newMeasurement.Status
    };

    db.TrafficMeasurements.Add(measurement);

    await db.SaveChangesAsync();

    return Results.Created(
        $"/api/traffic/{measurement.Id}",
        measurement
    );
});

app.Run();
