using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using LYBT.Infrastructure.Data;

namespace LYBT.Tests.Server.Infrastructure;

/// <summary>
/// Shared test context that provides a singleton WebApplicationFactory instance.
/// All test collections share this WAF to reduce initialization overhead.
/// Uses a shared SQL Server database with transaction isolation per test.
/// </summary>
public static class SharedTestContext
{
    // Use LocalDB for shared test database
    private const string SharedConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=LYBT_Test_Shared;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

    private static readonly Lazy<WebApplicationFactory<Program>> _factory = new(() =>
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");

                // Configure shared connection string
                builder.UseSetting("ConnectionStrings:DefaultConnection", SharedConnectionString);

                builder.ConfigureServices(services =>
                {
                    // Remove all background services to avoid interference
                    RemoveHostedServices(services);
                });
            });

        // Ensure database is created and migrated
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Database.Migrate();

        return factory;
    });

    public static WebApplicationFactory<Program> Factory => _factory.Value;

    public static string ConnectionString => SharedConnectionString;

    private static void RemoveHostedServices(IServiceCollection services)
    {
        var hostedServices = services
            .Where(d => d.ServiceType == typeof(IHostedService))
            .ToList();
        foreach (var svc in hostedServices)
        {
            services.Remove(svc);
        }
    }
}
