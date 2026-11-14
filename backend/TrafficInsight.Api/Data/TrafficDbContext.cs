
using Microsoft.EntityFrameworkCore;
using TrafficInsight.Api.Models;

namespace TrafficInsight.Api.Data;

public class TrafficDbContext : DbContext
{
    public TrafficDbContext(
        DbContextOptions<TrafficDbContext> options
    ) : base(options)
    {
    }

    public DbSet<TrafficMeasurement> TrafficMeasurements
        => Set<TrafficMeasurement>();
}
