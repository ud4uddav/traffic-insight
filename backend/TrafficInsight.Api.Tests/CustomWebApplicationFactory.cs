
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TrafficInsight.Api.Data;

namespace TrafficInsight.Api.Tests;

public class CustomWebApplicationFactory
    : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the original PostgreSQL registrations
            services.RemoveAll<DbContextOptions<TrafficDbContext>>();
            services.RemoveAll<
                IDbContextOptionsConfiguration<TrafficDbContext>>();

            services.RemoveAll<TrafficDbContext>();

            // Create SQLite in-memory connection
            _connection = new SqliteConnection(
                "Data Source=:memory:"
            );

            _connection.Open();

            // Register SQLite for integration testing
            services.AddDbContext<TrafficDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            // Build the test service provider
var serviceProvider = services.BuildServiceProvider();

// Create the database and its tables
using (var scope = serviceProvider.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<TrafficDbContext>();

    db.Database.EnsureCreated();
}
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection?.Dispose();
        }
    }
}
