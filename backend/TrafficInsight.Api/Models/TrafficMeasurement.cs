using System.ComponentModel.DataAnnotations;

namespace TrafficInsight.Api.Models;

public class TrafficMeasurement
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Location is required.")]
    [StringLength(100, ErrorMessage = "Location cannot exceed 100 characters.")]
    public string Location { get; set; } = string.Empty;

    [Range(0, 100000, ErrorMessage = "Vehicles must be between 0 and 100000.")]
    public int Vehicles { get; set; }

    [Range(0, 300, ErrorMessage = "Speed must be between 0 and 300.")]
    public int Speed { get; set; }

    [Required(ErrorMessage = "Status is required.")]
    [RegularExpression("^(Normal|Heavy)$", ErrorMessage = "Status must be Normal or Heavy.")]
    public string Status { get; set; } = string.Empty;
}