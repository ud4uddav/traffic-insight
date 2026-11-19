
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using TrafficInsight.Api.Models;

namespace TrafficInsight.Api.Tests;

public class TrafficIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TrafficIntegrationTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetTraffic_ReturnsAllMeasurements()
    {
        // Arrange
        // Create an HTTP GET request to the API

        // Act
        var response = await _client.GetAsync("/api/traffic");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var measurements =
            await response.Content.ReadFromJsonAsync<
                List<TrafficMeasurement>>();

        Assert.NotNull(measurements);
        Assert.Equal(5, measurements.Count);
    }


[Fact]
public async Task PostTraffic_CreatesNewMeasurement()
{
    // Arrange: Create a new traffic measurement
    var newMeasurement = new TrafficMeasurement
    {
        Location = "Vaasa Test Road",
        Vehicles = 500,
        Speed = 55,
        Status = "Normal"
    };

    // Act: Send POST request
    var response = await _client.PostAsJsonAsync(
        "/api/traffic",
        newMeasurement
    );

    // Assert: Check HTTP status
    Assert.Equal(
        HttpStatusCode.Created,
        response.StatusCode
    );

    // Read the created measurement
    var createdMeasurement =
        await response.Content.ReadFromJsonAsync<
            TrafficMeasurement>();

    Assert.NotNull(createdMeasurement);

    try
    {
        // Verify saved data
        Assert.True(createdMeasurement.Id > 0);

        Assert.Equal(
            "Vaasa Test Road",
            createdMeasurement.Location
        );

        Assert.Equal(500, createdMeasurement.Vehicles);
        Assert.Equal(55, createdMeasurement.Speed);
        Assert.Equal("Normal", createdMeasurement.Status);
    }
    finally
    {
        // Remove test data to keep the GET test isolated
        if (createdMeasurement.Id > 0)
        {
            await _client.DeleteAsync(
                $"/api/traffic/{createdMeasurement.Id}"
            );
        }
    }
}


[Fact]
public async Task PutTraffic_UpdatesExistingMeasurement()
{
    // Arrange: Create a measurement first
    var newMeasurement = new TrafficMeasurement
    {
        Location = "Helsinki Test Road",
        Vehicles = 500,
        Speed = 50,
        Status = "Normal"
    };

    var createResponse = await _client.PostAsJsonAsync(
        "/api/traffic",
        newMeasurement
    );

    var createdMeasurement =
        await createResponse.Content.ReadFromJsonAsync<
            TrafficMeasurement>();

    Assert.NotNull(createdMeasurement);

    // Prepare updated information
    var updatedMeasurement = new TrafficMeasurement
    {
        Location = "Helsinki Updated Road",
        Vehicles = 1500,
        Speed = 25,
        Status = "Heavy"
    };

    try
    {
        // Act: Send PUT request
        var response = await _client.PutAsJsonAsync(
            $"/api/traffic/{createdMeasurement.Id}",
            updatedMeasurement
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result =
            await response.Content.ReadFromJsonAsync<
                TrafficMeasurement>();

        Assert.NotNull(result);

        Assert.Equal(
            "Helsinki Updated Road",
            result.Location
        );

        Assert.Equal(1500, result.Vehicles);
        Assert.Equal(25, result.Speed);
        Assert.Equal("Heavy", result.Status);
    }
    finally
    {
        await _client.DeleteAsync(
            $"/api/traffic/{createdMeasurement.Id}"
        );
    }
}

[Fact]
public async Task DeleteTraffic_RemovesExistingMeasurement()
{
    // Arrange: Create a measurement
    var newMeasurement = new TrafficMeasurement
    {
        Location = "Oulu Test Road",
        Vehicles = 700,
        Speed = 45,
        Status = "Normal"
    };

    var createResponse = await _client.PostAsJsonAsync(
        "/api/traffic",
        newMeasurement
    );

    var createdMeasurement =
        await createResponse.Content.ReadFromJsonAsync<
            TrafficMeasurement>();

    Assert.NotNull(createdMeasurement);

    // Act: Delete the measurement
    var deleteResponse = await _client.DeleteAsync(
        $"/api/traffic/{createdMeasurement.Id}"
    );

    // Assert: Verify successful deletion
    Assert.Equal(
        HttpStatusCode.NoContent,
        deleteResponse.StatusCode
    );

    // Verify that the record no longer exists
    var getResponse = await _client.GetAsync(
        "/api/traffic"
    );

    var measurements =
        await getResponse.Content.ReadFromJsonAsync<
            List<TrafficMeasurement>>();

    Assert.NotNull(measurements);

    Assert.DoesNotContain(
        measurements,
        item => item.Id == createdMeasurement.Id
    );
}

[Fact]
public async Task PutTraffic_NonExistingId_ReturnsNotFound()
{
    // Arrange
    int nonExistingId = int.MaxValue;

    var updatedMeasurement = new TrafficMeasurement
    {
        Location = "Helsinki Test Road",
        Vehicles = 1000,
        Speed = 40,
        Status = "Normal"
    };

    // Act
    var response = await _client.PutAsJsonAsync(
        $"/api/traffic/{nonExistingId}",
        updatedMeasurement
    );

    // Assert
    Assert.Equal(
        HttpStatusCode.NotFound,
        response.StatusCode
    );
}


}
