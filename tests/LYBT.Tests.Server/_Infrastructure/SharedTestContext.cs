using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LYBT.Tests.Server.Infrastructure;

/// <summary>
/// Shared test context that provides a singleton WebApplicationFactory instance.
/// All test collections share this WAF to reduce initialization overhead.
/// </summary>
public static class SharedTestContext
{
    private static readonly Lazy<WebApplicationFactory<Program>> _factory = new(() =>
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");

                builder.ConfigureServices(services =>
                {
                    // Remove all background services to avoid interference
                    RemoveHostedServices(services);
                });
            });

        return factory;
    });

    public static WebApplicationFactory<Program> Factory => _factory.Value;

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
