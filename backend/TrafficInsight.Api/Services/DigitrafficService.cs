
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using TrafficInsight.Api.Models;

namespace TrafficInsight.Api.Services;

public class DigitrafficService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;

    public DigitrafficService(HttpClient httpClient, IMemoryCache cache)
    {
        _httpClient = httpClient;
        _cache = cache;
    }

    public async Task<List<ExternalTrafficMeasurement>>
        GetMeasurementsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            "api/tms/v1/stations/data",
            cancellationToken
        );

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(
            cancellationToken
        );

        var digitrafficData =
            await JsonSerializer.DeserializeAsync<DigitrafficResponse>(
                stream,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                },
                cancellationToken
            );

        if (digitrafficData?.Stations == null)
        {
            return new List<ExternalTrafficMeasurement>();
        }

        var stationNames = await GetStationNamesAsync(cancellationToken);

        var measurements = new List<ExternalTrafficMeasurement>();

        foreach (var station in digitrafficData.Stations)
        {
            var vehicleSensor = station.SensorValues.FirstOrDefault(
                sensor =>
                    sensor.Name == "OHITUKSET_5MIN_KIINTEA_SUUNTA1"
            );

            var speedSensor = station.SensorValues.FirstOrDefault(
                sensor =>
                    sensor.Name == "KESKINOPEUS_5MIN_LIUKUVA_SUUNTA1"
            );

            // Ignore stations without either measurement
            if (vehicleSensor?.Value == null &&
                speedSensor?.Value == null)
            {
                continue;
            }

            measurements.Add(new ExternalTrafficMeasurement
            {
                StationId = station.TmsNumber,
                StationName = stationNames.GetValueOrDefault(station.TmsNumber),

                VehiclesPerHour = vehicleSensor?.Value,

                AverageSpeedKmh = speedSensor?.Value,

                MeasuredTime = speedSensor?.MeasuredTime
                    ?? vehicleSensor?.MeasuredTime
                    ?? station.DataUpdatedTime
            });
        }

        return measurements;
    }

    private async Task<Dictionary<int, string>> GetStationNamesAsync(
        CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue("digitraffic-tms-station-names", out Dictionary<int, string>? cached)
            && cached is not null)
        {
            return cached;
        }

        try
        {
            using var response = await _httpClient.GetAsync(
                "api/tms/v1/stations",
                cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var names = new Dictionary<int, string>();

            if (document.RootElement.TryGetProperty("features", out var features))
            {
                foreach (var feature in features.EnumerateArray())
                {
                    if (!feature.TryGetProperty("properties", out var properties) ||
                        !properties.TryGetProperty("tmsNumber", out var number) ||
                        !number.TryGetInt32(out var stationId) ||
                        !properties.TryGetProperty("name", out var name) ||
                        string.IsNullOrWhiteSpace(name.GetString()))
                    {
                        continue;
                    }

                    names[stationId] = name.GetString()!;
                }
            }

            _cache.Set("digitraffic-tms-station-names", names, TimeSpan.FromHours(6));
            return names;
        }
        catch (HttpRequestException)
        {
            // Live measurements are still useful when metadata is temporarily unavailable.
            return new Dictionary<int, string>();
        }
        catch (JsonException)
        {
            // Fall back to station IDs if the metadata response is incomplete or changes shape.
            return new Dictionary<int, string>();
        }
    }
}
