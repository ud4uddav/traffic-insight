
namespace TrafficInsight.Api.Models;

public class DigitrafficResponse
{
    public DateTimeOffset? DataUpdatedTime { get; set; }

    public List<DigitrafficStation> Stations { get; set; } = new();
}

public class DigitrafficStation
{
    public int Id { get; set; }

    public int TmsNumber { get; set; }

    public DateTimeOffset? DataUpdatedTime { get; set; }

    public List<DigitrafficSensorValue> SensorValues { get; set; } = new();
}

public class DigitrafficSensorValue
{
    public string Name { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;

    public double? Value { get; set; }

    public DateTimeOffset? MeasuredTime { get; set; }
}

// Simplified response for our React frontend
public class ExternalTrafficMeasurement
{
    public int StationId { get; set; }

    public string? StationName { get; set; }

    public double? VehiclesPerHour { get; set; }

    public double? AverageSpeedKmh { get; set; }

    public DateTimeOffset? MeasuredTime { get; set; }
}
