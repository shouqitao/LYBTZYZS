using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Respawn;
using Xunit;

namespace LYBT.Tests.Server.Infrastructure;

/// <summary>
/// Core server integration test fixture.
///
/// Key design decisions:
/// - Creates a unique SQL Server database per test run (via LocalSqlServerProvider)
/// - Uses WebApplicationFactory with Test environment
/// - Runs EF Core migrations once during initialization
/// - Uses Respawn for fast per-test database reset
/// - Seeds base data (sysadmin, admin, doctor) through production code paths
/// - Authenticates via real login endpoint (POST /api/v1/auth/login)
/// </summary>
public sealed class ServerFixture : IAsyncLifetime
{
    private readonly LocalSqlServerProvider _dbProvider = new();
    private WebApplicationFactory<Program> _factory = null!;
    private Respawner _respawner = null!;

    // Fixed test user IDs for predictable seeding
    private static readonly Guid AdminUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid DoctorUserId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    // Test credentials
    private const string SysAdminPassword = "TestAdmin2025@";
    private const string AdminPassword = "TestAdmin2025@";
    private const string DoctorPassword = "TestDoctor2025@";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>Service provider from the WebApplicationFactory.</summary>
    public IServiceProvider Services => _factory.Services;

    /// <summary>Anonymous (unauthenticated) HttpClient.</summary>
    public HttpClient AnonymousClient { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // 1. Create unique test database
        await _dbProvider.InitializeAsync();

        // 2. Build WebApplicationFactory with dynamic connection string
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");

                // Inject dynamic connection string via configuration
                builder.UseSetting(
                    "ConnectionStrings:DefaultConnection",
                    _dbProvider.ConnectionString);

                builder.ConfigureServices(services =>
                {
                    // Remove all background services to avoid interference
                    RemoveHostedServices(services);
                });
            });

        // 3. Run migrations
        await MigrateAsync();

        // 4. Create Respawner
        await using var connection = new SqlConnection(_dbProvider.ConnectionString);
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,
            SchemasToInclude = ["dbo"],
            // EF __EFMigrationsHistory should not be reset
            TablesToIgnore = [new Respawn.Graph.Table("__EFMigrationsHistory")]
        });

        // 5. Seed base data
        await SeedBaseDataAsync();

        // 6. Create anonymous client
        AnonymousClient = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        AnonymousClient?.Dispose();
        await _factory.DisposeAsync();
        await _dbProvider.DisposeAsync();
    }

    /// <summary>
    /// Resets the database to a clean state and re-seeds base data.
    /// Called before each test via IntegrationTestBase.InitializeAsync.
    /// </summary>
    public async Task ResetAsync()
    {
        await using var connection = new SqlConnection(_dbProvider.ConnectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
        await SeedBaseDataAsync();
    }

    /// <summary>
    /// Login as sysadmin and return an authenticated HttpClient.
    /// </summary>
    public Task<HttpClient> LoginAsSysAdminAsync()
        => LoginAsAsync("sysadmin", SysAdminPassword);

    /// <summary>
    /// Login as admin and return an authenticated HttpClient.
    /// </summary>
    public Task<HttpClient> LoginAsAdminAsync()
        => LoginAsAsync("admin", AdminPassword);

    /// <summary>
    /// Login as doctor and return an authenticated HttpClient.
    /// </summary>
    public Task<HttpClient> LoginAsDoctorAsync()
        => LoginAsAsync("doctor", DoctorPassword);

    /// <summary>
    /// Login via POST /api/v1/auth/login and return an authenticated HttpClient.
    /// Uses the real authentication pipeline.
    /// </summary>
    public async Task<HttpClient> LoginAsAsync(string username, string password)
    {
        using var loginClient = _factory.CreateClient();

        var loginRequest = new LoginRequest
        {
            UserName = username,
            Password = password
        };

        var response = await loginClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<LoginResponse>>(content, JsonOptions);

        if (apiResponse?.Success != true || string.IsNullOrEmpty(apiResponse.Data?.Token))
        {
            throw new InvalidOperationException(
                $"Login failed for user '{username}'. Response: {content}");
        }

        var authenticatedClient = _factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiResponse.Data.Token);

        return authenticatedClient;
    }

    /// <summary>
    /// Provides direct access to AppDbContext via a new scope.
    /// Caller is responsible for disposing the scope.
    /// </summary>
    public async Task<T> WithDbContextAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await action(db);
    }

    /// <summary>
    /// Provides direct access to AppDbContext via a new scope (no return value).
    /// </summary>
    public async Task WithDbContextAsync(Func<AppDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await action(db);
    }

    /// <summary>
    /// Creates JSON content for HTTP requests.
    /// </summary>
    public static StringContent CreateJsonContent<T>(T obj)
    {
        var json = JsonSerializer.Serialize(obj);
        return new StringContent(json, System.Text.Encoding.UTF8, "application/json");
    }

    /// <summary>
    /// Deserializes HTTP response content.
    /// </summary>
    public static T? ParseResponse<T>(string content)
    {
        return JsonSerializer.Deserialize<T>(content, JsonOptions);
    }

    #region Private Methods

    private async Task MigrateAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    /// <summary>
    /// Seeds base test data:
    /// 1. sysadmin (SuperAdmin) - created via DatabaseInitializationService path
    /// 2. admin (Admin) - created directly in DB
    /// 3. doctor (Doctor) - created directly in DB
    ///
    /// We seed directly to the database (like the existing WebApiFixture) because:
    /// - DatabaseInitializationService.EnsureSystemAdminExistsAsync is private
    /// - Creating users via API requires authentication (chicken-and-egg problem)
    /// - Direct seeding is faster and more reliable for test setup
    /// </summary>
    private async Task SeedBaseDataAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Seed sysadmin (SuperAdmin) - matches production DatabaseInitializationService behavior
        await UpsertUserAsync(db,
            id: Guid.Empty, // sysadmin may have Guid.Empty or Guid.NewGuid(); use Empty for consistency
            userName: "sysadmin",
            realName: "系统管理员",
            role: UserRole.SuperAdmin,
            email: "admin@lybt.com",
            password: SysAdminPassword);

        // Seed admin
        await UpsertUserAsync(db,
            id: AdminUserId,
            userName: "admin",
            realName: "测试管理员",
            role: UserRole.Admin,
            email: "admin-test@lybt.com",
            password: AdminPassword);

        // Seed doctor
        await UpsertUserAsync(db,
            id: DoctorUserId,
            userName: "doctor",
            realName: "测试医生",
            role: UserRole.Doctor,
            email: "doctor-test@lybt.com",
            password: DoctorPassword);

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Upsert a user into the database.
    /// Uses IgnoreQueryFilters() to handle soft-deleted records (EF Core 8 global filter caveat).
    /// Uses PasswordHelper.HashPassword() for production-compatible password hashing.
    /// </summary>
    private static async Task UpsertUserAsync(
        AppDbContext db,
        Guid id,
        string userName,
        string realName,
        UserRole role,
        string email,
        string password)
    {
        var existing = await db.Set<User>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == id);

        if (existing != null)
        {
            existing.UserName = userName;
            existing.RealName = realName;
            existing.Role = role;
            existing.Email = email;
            existing.Status = CommonStatus.Enabled;
            existing.IsDeleted = false;
            existing.PasswordHash = PasswordHelper.HashPassword(password, role);
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            db.Set<User>().Add(new User
            {
                Id = id == Guid.Empty ? Guid.NewGuid() : id,
                UserName = userName,
                RealName = realName,
                Role = role,
                Email = email,
                Status = CommonStatus.Enabled,
                PasswordHash = PasswordHelper.HashPassword(password, role),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty,
                UpdatedBy = Guid.Empty,
                IsDeleted = false
            });
        }
    }

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

    #endregion
}
