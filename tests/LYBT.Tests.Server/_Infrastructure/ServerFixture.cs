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
public class ServerFixture : IAsyncLifetime
{
    /// <summary>
    /// Serialize concurrent WebApplicationFactory creation across all fixture instances.
    /// Prevents "The logger is already frozen" race condition when multiple
    /// DomainFixture collections initialize in parallel.
    /// </summary>
    private static readonly SemaphoreSlim InitGate = new(1, 1);

    private readonly LocalSqlServerProvider _dbProvider = new();
    protected WebApplicationFactory<Program> Factory => _factory;
    private WebApplicationFactory<Program> _factory = null!;
    private Respawner _respawner = null!;

    // Fixed test user IDs for predictable seeding
    private static readonly Guid AdminUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid DoctorUserId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid ReceptionistUserId = Guid.Parse("00000000-0000-0000-0000-000000000003");

    // Test credentials
    private const string SysAdminPassword = "TestAdmin2025@";
    private const string AdminPassword = "TestAdmin2025@";
    private const string DoctorPassword = "TestDoctor2025@";
    private const string ReceptionistPassword = "TestReceptionist2025@";

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

        // 2. Serialize WebApplicationFactory creation to prevent
        //    "The logger is already frozen" race condition
        await InitGate.WaitAsync();
        try
        {
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

            // 3. Run migrations (inside gate to avoid concurrent DB ops during startup)
            await MigrateAsync();
        }
        finally
        {
            InitGate.Release();
        }

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

        // 5. Reset any data seeded by app startup (e.g., DatabaseInitializationService),
        //    then seed our own test data with known credentials
        await _respawner.ResetAsync(connection);
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
    /// <summary>
    /// Semaphore to serialize database reset operations within the same fixture.
    /// Prevents concurrent reset conflicts when tests run in parallel.
    /// </summary>
    private readonly SemaphoreSlim _resetGate = new(1, 1);

    /// <summary>
    /// Resets the database to a clean state and re-seeds base data.
    /// Thread-safe for parallel execution.
    /// Called before each test via IntegrationTestBase.InitializeAsync.
    /// </summary>
    public async Task ResetAsync()
    {
        await _resetGate.WaitAsync();
        try
        {
            await using var connection = new SqlConnection(_dbProvider.ConnectionString);
            await connection.OpenAsync();
            await _respawner.ResetAsync(connection);
            await SeedBaseDataAsync();
        }
        finally
        {
            _resetGate.Release();
        }
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
    /// Login as receptionist and return an authenticated HttpClient.
    /// </summary>
    public Task<HttpClient> LoginAsReceptionistAsync()
        => LoginAsAsync("receptionist", ReceptionistPassword);

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
    /// Seeds base test data after Respawn reset.
    /// Post-Respawn the DB is empty -- use direct Add (no Upsert needed).
    /// </summary>
    private async Task SeedBaseDataAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Set<User>().AddRange(
            CreateUser(Guid.NewGuid(), "sysadmin", "系统管理员",
                UserRole.SuperAdmin, "admin@lybt.com", SysAdminPassword),
            CreateUser(AdminUserId, "admin", "测试管理员",
                UserRole.Admin, "admin-test@lybt.com", AdminPassword),
            CreateUser(DoctorUserId, "doctor", "测试医生",
                UserRole.Doctor, "doctor-test@lybt.com", DoctorPassword),
            CreateUser(ReceptionistUserId, "receptionist", "测试前台",
                UserRole.Receptionist, "receptionist-test@lybt.com", ReceptionistPassword)
        );

        await db.SaveChangesAsync();
    }

    private static User CreateUser(
        Guid id, string userName, string realName,
        UserRole role, string email, string password)
    {
        var now = DateTime.UtcNow;
        return new User
        {
            Id = id,
            UserName = userName,
            RealName = realName,
            Role = role,
            Email = email,
            Status = CommonStatus.Enabled,
            PasswordHash = PasswordHelper.HashPassword(password, role),
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty,
            IsDeleted = false
        };
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
