
using System.ComponentModel.DataAnnotations;
using TrafficInsight.Api.Models;

namespace TrafficInsight.Api.Tests;

public class TrafficMeasurementTests
{
    [Fact]
    public void ValidMeasurement_ShouldPassValidation()
    {
        // Arrange
        var measurement = new TrafficMeasurement
        {
            Location = "Helsinki",
            Vehicles = 120,
            Speed = 60,
            Status = "Normal"
        };

        // Act
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            measurement,
            new ValidationContext(measurement),
            results,
            validateAllProperties: true
        );

        // Assert
        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Fact]
    public void InvalidMeasurement_ShouldFailValidation()
    {
        // Arrange
        var measurement = new TrafficMeasurement
        {
            Location = "",
            Vehicles = -10,
            Speed = -5,
            Status = "Unknown"
        };

        // Act
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            measurement,
            new ValidationContext(measurement),
            results,
            validateAllProperties: true
        );

        // Assert
        Assert.False(isValid);
        Assert.NotEmpty(results);
    }

    [Theory]
    [InlineData("", 120, 60, "Normal")]
    [InlineData("Helsinki", -10, 60, "Normal")]
    [InlineData("Helsinki", 120, -5, "Normal")]
    [InlineData("Helsinki", 120, 60, "Unknown")]
    public void InvalidProperty_ShouldFailValidation(
        string location,
        int vehicles,
        int speed,
        string status)
    {
        // Arrange
        var measurement = new TrafficMeasurement
        {
            Location = location,
            Vehicles = vehicles,
            Speed = speed,
            Status = status
        };

        // Act
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            measurement,
            new ValidationContext(measurement),
            results,
            validateAllProperties: true
        );

        // Assert
        Assert.False(isValid);
        Assert.NotEmpty(results);
    }
}
