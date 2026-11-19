
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using TrafficInsight.Api.Data;
using TrafficInsight.Api.Models;

bool ValidateModel(TrafficMeasurement measurement, out List<string> errors)
{
    var validationResults = new List<ValidationResult>();

    var validationContext = new ValidationContext(measurement);

    bool isValid = Validator.TryValidateObject(
        measurement,
        validationContext,
        validationResults,
        validateAllProperties: true
    );

    errors = validationResults
        .Select(result => result.ErrorMessage ?? "Invalid value")
        .ToList();

    return isValid;
}



var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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
    TrafficMeasurement measurement,
    TrafficDbContext db) =>
{
    if (!ValidateModel(measurement, out var errors))
    {
        return Results.BadRequest(new
        {
            Errors = errors
        });
    }

    db.TrafficMeasurements.Add(measurement);

    await db.SaveChangesAsync();

    return Results.Created(
        $"/api/traffic/{measurement.Id}",
        measurement
    );
});

// PUT: Update an existing traffic measurement
app.MapPut("/api/traffic/{id:int}", async (
    int id,
    TrafficMeasurement updatedMeasurement,
    TrafficDbContext db) =>
{
    var measurement = await db.TrafficMeasurements.FindAsync(id);

    if (measurement is null)
    {
        return Results.NotFound();
    }

    if (!ValidateModel(updatedMeasurement, out var errors))
    {
        return Results.BadRequest(new
        {
            Errors = errors
        });
    }

    measurement.Location = updatedMeasurement.Location;
    measurement.Vehicles = updatedMeasurement.Vehicles;
    measurement.Speed = updatedMeasurement.Speed;
    measurement.Status = updatedMeasurement.Status;

    await db.SaveChangesAsync();

    return Results.Ok(measurement);
});

// DELETE: Remove a traffic measurement
app.MapDelete("/api/traffic/{id:int}", async (
    int id,
    TrafficDbContext db) =>
{
    var measurement = await db.TrafficMeasurements
        .FindAsync(id);

    if (measurement == null)
    {
        return Results.NotFound(
            $"Traffic measurement with ID {id} was not found."
        );
    }

    db.TrafficMeasurements.Remove(measurement);

    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.Run();

public partial class Program { }
