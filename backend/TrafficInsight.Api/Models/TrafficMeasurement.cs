
namespace TrafficInsight.Api.Models;

public class TrafficMeasurement
{
    public int Id { get; set; }

    public string Location { get; set; } = string.Empty;

    public int Vehicles { get; set; }

    public int Speed { get; set; }

    public string Status { get; set; } = string.Empty;
}
