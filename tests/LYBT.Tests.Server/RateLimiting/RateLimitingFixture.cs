using LYBT.Entities.Users;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Security;
using LYBT.Tests.Server.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Xunit;

namespace LYBT.Tests.Server.RateLimiting;

/// <summary>
/// Dedicated fixture for rate limiting tests.
///
/// Key differences from ServerFixture:
/// - Enables rate limiting (Security:RateLimiting:Enabled = true)
/// - Uses its own LocalSqlServerProvider for database isolation
/// - Handles Serilog freeze workaround (Log.CloseAndFlush + replace ILoggerFactory)
/// - Only exposes AnonymousClient (rate limiting tests don't need authenticated clients)
///
/// Technical notes:
/// - UseSetting overrides appsettings.Test.json (Security:RateLimiting:Enabled=false)
/// - Serilog's UseSerilog() freezes the global ReloadableLogger; a second WAF instance
///   would trigger "logger is already frozen" exception. Resolved by CloseAndFlush +
///   replacing ILoggerFactory in DI with built-in LoggerFactory.
/// </summary>
public sealed class RateLimitingFixture : IAsyncLifetime
{
    private readonly LocalSqlServerProvider _dbProvider = new();
    private WebApplicationFactory<Program> _factory = null!;

    /// <summary>Unauthenticated HttpClient (rate limiting tests only need anonymous access).</summary>
    public HttpClient AnonymousClient { get; private set; } = null!;

    /// <summary>WebApplicationFactory service container.</summary>
    public IServiceProvider Services => _factory.Services;

    /// <summary>
    /// Rate limit configuration: matches production FixedWindowLimiter (5 requests / 60 seconds).
    /// </summary>
    public const int PermitLimit = 5;

    /// <summary>Seed user password (matches ServerFixture).</summary>
    public const string SeedPassword = "TestAdmin2025@";

    // Fixed test user ID for predictable seeding
    private static readonly Guid AdminUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public async Task InitializeAsync()
    {
        // 1. Create unique test database
        await _dbProvider.InitializeAsync();

        // 2. Reset Serilog global Logger to avoid conflict with ServerFixture's WAF instance.
        //    When ServerFixture's WAF is created first, UseSerilog() freezes the global ReloadableLogger.
        //    A second WAF would call UseSerilog() again, and DI resolution of ILoggerFactory would
        //    attempt Freeze() a second time, throwing "logger is already frozen".
        //    CloseAndFlush resets the global logger + we replace ILoggerFactory in DI below.
        Log.CloseAndFlush();
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Warning()
            .WriteTo.Console()
            .CreateLogger();

        // 3. Build WebApplicationFactory with rate limiting enabled
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");

                // Override configuration: enable rate limiting
                // UseSetting takes precedence over appsettings.Test.json
                builder.UseSetting("Security:RateLimiting:Enabled", "true");
                builder.UseSetting("ConnectionStrings:DefaultConnection", _dbProvider.ConnectionString);

                // Replace Serilog log providers with built-in console logging
                builder.ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Warning);
                });

                builder.ConfigureServices(services =>
                {
                    // Remove background services to avoid test interference
                    RemoveHostedServices(services);

                    // Replace Serilog's ILoggerFactory to avoid ReloadableLogger.Freeze() exception
                    ReplaceSerilogLoggerFactory(services);
                });
            });

        // 4. Run EF Core migrations + seed base user
        await InitializeDatabase();

        // 5. Create anonymous HttpClient (rate limiting tests don't need authentication)
        AnonymousClient = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        AnonymousClient?.Dispose();
        await _factory.DisposeAsync();
        await _dbProvider.DisposeAsync();
    }

    #region Private Setup

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

    /// <summary>
    /// Replaces Serilog's ILoggerFactory registration to avoid ReloadableLogger.Freeze() exception.
    ///
    /// Serilog's UseSerilog() registers ILoggerFactory via ImplementationFactory (lambda) that
    /// calls ReloadableLogger.Freeze() on first DI resolution. When a second WAF exists in the
    /// same process, the global ReloadableLogger may already be frozen, causing
    /// InvalidOperationException on the second Freeze() call.
    ///
    /// Solution: Remove all ILoggerFactory registrations and replace with built-in LoggerFactory.
    /// Rate limiting tests don't need structured logging.
    /// </summary>
    private static void ReplaceSerilogLoggerFactory(IServiceCollection services)
    {
        var factoryDescriptors = services
            .Where(d => d.ServiceType == typeof(ILoggerFactory))
            .ToList();
        foreach (var descriptor in factoryDescriptors)
        {
            services.Remove(descriptor);
        }

        services.AddSingleton<ILoggerFactory>(new LoggerFactory());
    }

    private async Task InitializeDatabase()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await SeedDefaultUser(db);
    }

    /// <summary>
    /// Seeds a single admin user. Rate limiting tests only need one loginable user.
    /// Uses PasswordHelper.HashPassword for production-compatible hashing.
    /// Uses IgnoreQueryFilters() to handle soft-deleted records (EF Core 8 global filter caveat).
    /// </summary>
    private static async Task SeedDefaultUser(AppDbContext db)
    {
        var existing = await db.Set<User>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == AdminUserId);

        if (existing != null)
        {
            existing.UserName = "admin";
            existing.RealName = "系统管理员";
            existing.Role = UserRole.Admin;
            existing.Status = CommonStatus.Enabled;
            existing.IsDeleted = false;
            existing.PasswordHash = PasswordHelper.HashPassword(SeedPassword, UserRole.Admin);
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            db.Set<User>().Add(new User
            {
                Id = AdminUserId,
                UserName = "admin",
                RealName = "系统管理员",
                Role = UserRole.Admin,
                Status = CommonStatus.Enabled,
                PasswordHash = PasswordHelper.HashPassword(SeedPassword, UserRole.Admin),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty,
                UpdatedBy = Guid.Empty,
                IsDeleted = false
            });
        }

        await db.SaveChangesAsync();
    }

    #endregion
}
