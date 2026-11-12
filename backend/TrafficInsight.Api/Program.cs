
using TrafficInsight.Api.Models;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/api/traffic", () =>
{
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

    return Results.Ok(trafficData);
});

app.Run();
