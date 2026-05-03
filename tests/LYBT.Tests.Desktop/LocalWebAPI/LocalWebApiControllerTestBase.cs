using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using LYBT.LocalWebAPI;
using LYBT.LocalWebAPI.Auth;
using LYBT.LocalWebAPI.Data;
using LYBT.Shared.Models.Contracts.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Tests.Desktop.LocalWebAPI;

/// <summary>
/// Shared base class for LocalWebAPI controller integration tests.
/// Each subclass gets its own SQL Server LocalDB database, a seeded WebApplication,
/// and a pre-configured HttpClient.
/// </summary>
public abstract class LocalWebApiControllerTestBase : IAsyncLifetime
{
    private readonly string _dbName = $"LYBTZYZS_CtrlTests_{Guid.NewGuid():N}";
    private string _connectionString = null!;
    private WebApplication? _app;

    protected HttpClient Client { get; private set; } = null!;

    /// <summary>
    /// JSON serializer options shared across all controller tests.
    /// PropertyNameCaseInsensitive = true to match API responses regardless of casing.
    /// PropertyNamingPolicy = null to use exact property names (no camelCase conversion).
    /// </summary>
    protected static JsonSerializerOptions Json { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = null
    };

    public async Task InitializeAsync()
    {
        _connectionString = $@"Server=(localdb)\MSSQLLocalDB;Database={_dbName};Trusted_Connection=True;TrustServerCertificate=True";

        // Set environment to prevent Kestrel from loading launchSettings.json URLs
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", "http://127.0.0.1:0");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");

        var builder = LocalWebApiProgram.CreateBuilder([]);

        // Override the connection string to use our test-specific database
        builder.Configuration["ConnectionStrings:DefaultConnection"] = _connectionString;

        // Register services (same as LocalWebApiProgram.CreateApplication)
        builder.Services.AddDbContext<LocalWebApiDbContext>(options =>
            options.UseSqlServer(_connectionString));

        // Add controllers with explicit assembly so CreateSlimBuilder discovers them
        builder.Services.AddControllers()
            .AddApplicationPart(typeof(LYBT.LocalWebAPI.Controllers.HealthController).Assembly)
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            });

        // Suppress implicit required attribute for non-nullable reference type parameters
        builder.Services.Configure<Microsoft.AspNetCore.Mvc.MvcOptions>(options =>
        {
            options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
        });

        LocalJwtConfig.ConfigureServices(builder.Services);

        _app = builder.Build();

        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.MapControllers();

        // Use port 0 to let the OS assign a random available port
        _app.Urls.Add("http://127.0.0.1:0");

        await _app.StartAsync();

        // Build a client pointing at the running application
        var port = new Uri(_app.Urls.First()).Port;
        Client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}") };

        // Ensure database is created and seeded
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LocalWebApiDbContext>();
        await db.Database.EnsureCreatedAsync();
        await LocalWebApiSeedData.SeedAsync(db);
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();

        if (_app != null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }

        // Clean up environment variables
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", null);
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);

        // Delete the test database
        var options = new DbContextOptionsBuilder<LocalWebApiDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
        await using var context = new LocalWebApiDbContext(options);
        await context.Database.EnsureDeletedAsync();
    }

    /// <summary>
    /// Obtains a JWT token by logging in as the seeded admin user (admin/admin123).
    /// </summary>
    protected async Task<string> GetAdminTokenAsync()
    {
        var request = new LoginRequest
        {
            UserName = "admin",
            Password = "admin123"
        };

        var response = await Client.PostAsJsonAsync("/api/auth/login", request);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("token").GetString()!;
    }

    /// <summary>
    /// Sets the Authorization header on <see cref="Client"/> to use a Bearer token.
    /// </summary>
    protected void SetAuthHeader(string token)
    {
        Client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }
}
