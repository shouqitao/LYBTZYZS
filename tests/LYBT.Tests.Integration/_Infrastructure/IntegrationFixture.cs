using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
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

namespace LYBT.Tests.Integration;

/// <summary>
/// Integration test fixture for Desktop+Server full-chain testing.
///
/// Key design decisions:
/// - Reuses ServerFixture pattern (WebApplicationFactory + SQL Server + Respawn)
/// - Creates unique SQL Server database per test run (via LocalSqlServerProvider pattern)
/// - Seeds base data (sysadmin, admin, doctor) through direct DB seeding
/// - Provides Refit API client creation for testing Desktop RemoteDataSource -> Server API chain
/// - Authenticates via real login endpoint (POST /api/v1/auth/login)
/// </summary>
public sealed class IntegrationFixture : IAsyncLifetime
{
    private static readonly SemaphoreSlim InitGate = new(1, 1);

    private WebApplicationFactory<Program> _factory = null!;
    private Respawner _respawner = null!;
    private string _connectionString = null!;
    private string _databaseName = null!;

    // Fixed test user IDs for predictable seeding (same as ServerFixture)
    public static readonly Guid AdminUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static readonly Guid DoctorUserId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    // Test credentials
    private const string SysAdminPassword = "TestAdmin2025@";
    private const string AdminUsername = "admin";
    private const string AdminPassword = "TestAdmin2025@";
    private const string DoctorUsername = "doctor";
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
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var guidPart = Guid.NewGuid().ToString("N")[..8];
        _databaseName = $"LYBT_Integration_{timestamp}_{guidPart}";
        _connectionString = GetFullConnectionString();

        await CreateDatabaseAsync();

        // 2. Build WebApplicationFactory — serialized to prevent "logger is already frozen" race
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
                        _connectionString);

                    builder.ConfigureServices(services =>
                    {
                        // Remove all background services to avoid interference
                        RemoveHostedServices(services);
                    });
                });

            // 3. Run migrations
            await MigrateAsync();
        }
        finally
        {
            InitGate.Release();
        }

        // 4. Create Respawner
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,
            SchemasToInclude = ["dbo"],
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
        await DropDatabaseAsync();
    }

    /// <summary>
    /// Resets the database to a clean state and re-seeds base data.
    /// Called before each test via IntegrationTestBase.InitializeAsync.
    /// </summary>
    public async Task ResetAsync()
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
        await SeedBaseDataAsync();
    }

    /// <summary>
    /// Login as admin and return an authenticated HttpClient.
    /// </summary>
    public Task<HttpClient> LoginAsAdminAsync()
        => LoginAsAsync(AdminUsername, AdminPassword);

    /// <summary>
    /// Login as doctor and return an authenticated HttpClient.
    /// </summary>
    public Task<HttpClient> LoginAsDoctorAsync()
        => LoginAsAsync(DoctorUsername, DoctorPassword);

    /// <summary>
    /// Login as sysadmin and return an authenticated HttpClient.
    /// </summary>
    public Task<HttpClient> LoginAsSysAdminAsync()
        => LoginAsAsync("sysadmin", SysAdminPassword);

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

        var json = JsonSerializer.Serialize(loginRequest);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await loginClient.PostAsync("/api/v1/auth/login", content);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<LoginResponse>>(body, JsonOptions);

        if (apiResponse?.Success != true || string.IsNullOrEmpty(apiResponse.Data?.Token))
        {
            throw new InvalidOperationException(
                $"Login failed for user '{username}'. Response: {body}");
        }

        var authenticatedClient = _factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiResponse.Data.Token);

        return authenticatedClient;
    }

    /// <summary>
    /// Create Refit API client from authenticated HttpClient.
    /// Usage: var api = fixture.CreateApi&lt;IPatientApi&gt;(authenticatedClient);
    /// </summary>
    public T CreateApi<T>(HttpClient client) where T : class
        => Refit.RestService.For<T>(client, new Refit.RefitSettings
        {
            ContentSerializer = new Refit.SystemTextJsonContentSerializer(
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() }
                })
        });

    /// <summary>
    /// Provides direct access to AppDbContext via a new scope.
    /// </summary>
    public async Task WithDbContextAsync(Func<AppDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await action(db);
    }

    /// <summary>
    /// Provides direct access to AppDbContext via a new scope (with return value).
    /// </summary>
    public async Task<T> WithDbContextAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await action(db);
    }

    #region Private Methods

    private string GetBaseConnectionString()
    {
        // Check for external SQL Server connection string from environment
        var envConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (!string.IsNullOrEmpty(envConnectionString))
        {
            // Parse and extract base connection, removing only Database parameter
            var builder = new SqlConnectionStringBuilder(envConnectionString);
            builder.Remove("Database"); // Remove any existing database
            return builder.ConnectionString;
        }

        // Fall back to default LocalDB
        return "Server=localhost;Database=master;Trusted_Connection=True;TrustServerCertificate=True";
    }

    private string GetFullConnectionString()
    {
        var baseConnectionString = GetBaseConnectionString();
        var builder = new SqlConnectionStringBuilder(baseConnectionString);

        // Remove existing Encrypt setting
        builder.Remove("Encrypt");

        // Enable SSL encryption with trusted certificate (SQL Server requires TLS)
        builder["Encrypt"] = true;
        builder["TrustServerCertificate"] = true;

        // Set database
        builder["Database"] = _databaseName;

        return builder.ConnectionString;
    }

    private async Task CreateDatabaseAsync()
    {
        var baseConnectionString = GetBaseConnectionString();

        await using var connection = new SqlConnection(baseConnectionString);
        await connection.OpenAsync();

        var sql = $"CREATE DATABASE [{_databaseName}]";
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task DropDatabaseAsync()
    {
        try
        {
            var baseConnectionString = GetBaseConnectionString();

            await using var connection = new SqlConnection(baseConnectionString);
            await connection.OpenAsync();

            var sql = $"""
                IF EXISTS (SELECT 1 FROM sys.databases WHERE name = '{_databaseName}')
                BEGIN
                    ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE [{_databaseName}];
                END
                """;
            await using var command = new SqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }
        catch (SqlException)
        {
            // Best-effort cleanup; don't fail test teardown
        }
    }

    private async Task MigrateAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    /// <summary>
    /// Seeds base test data (same pattern as ServerFixture):
    /// 1. sysadmin (SuperAdmin)
    /// 2. admin (Admin)
    /// 3. doctor (Doctor)
    /// </summary>
    private async Task SeedBaseDataAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await UpsertUserAsync(db,
            id: Guid.Empty,
            userName: "sysadmin",
            realName: "系统管理员",
            role: UserRole.SuperAdmin,
            email: "admin@lybt.com",
            password: SysAdminPassword);

        await UpsertUserAsync(db,
            id: AdminUserId,
            userName: AdminUsername,
            realName: "测试管理员",
            role: UserRole.Admin,
            email: "admin-test@lybt.com",
            password: AdminPassword);

        await UpsertUserAsync(db,
            id: DoctorUserId,
            userName: DoctorUsername,
            realName: "测试医生",
            role: UserRole.Doctor,
            email: "doctor-test@lybt.com",
            password: DoctorPassword);

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Upsert a user into the database.
    /// Uses IgnoreQueryFilters() to handle soft-deleted records.
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
