using System.Net;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using TrafficInsight.Api.Services;

namespace TrafficInsight.Api.Tests;

public class DigitrafficServiceTests
{
    [Fact]
    public async Task GetMeasurements_MapsSpeedAndStationName()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            var json = request.RequestUri!.AbsolutePath.EndsWith("/stations/data")
                ? """{"stations":[{"tmsNumber":20002,"dataUpdatedTime":"2026-11-22T12:00:00Z","sensorValues":[{"name":"OHITUKSET_5MIN_KIINTEA_SUUNTA1","value":276,"measuredTime":"2026-11-22T12:00:00Z"},{"name":"KESKINOPEUS_5MIN_LIUKUVA_SUUNTA1","value":95,"measuredTime":"2026-11-22T12:00:00Z"}]}]}"""
                : """{"features":[{"properties":{"tmsNumber":20002,"name":"vt1_Espoo_Hirvisuo"}}]}""";

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://tie.digitraffic.fi/")
        };
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new DigitrafficService(client, cache);

        var result = await service.GetMeasurementsAsync();

        var measurement = Assert.Single(result);
        Assert.Equal(20002, measurement.StationId);
        Assert.Equal("vt1_Espoo_Hirvisuo", measurement.StationName);
        Assert.Equal(276, measurement.VehiclesPerHour);
        Assert.Equal(95, measurement.AverageSpeedKmh);
    }

    [Fact]
    public async Task GetMeasurements_LeavesMissingSpeedNull()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            var json = request.RequestUri!.AbsolutePath.EndsWith("/stations/data")
                ? """{"stations":[{"tmsNumber":101,"sensorValues":[{"name":"OHITUKSET_5MIN_KIINTEA_SUUNTA1","value":0}]}]}"""
                : """{"features":[]}""";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://tie.digitraffic.fi/")
        };
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new DigitrafficService(client, cache);

        var measurement = Assert.Single(await service.GetMeasurementsAsync());

        Assert.Null(measurement.AverageSpeedKmh);
        Assert.Null(measurement.StationName);
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory(request));
    }
}
