using System.Data.Common;
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
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LYBT.Tests.Server.Infrastructure;

/// <summary>
/// Transactional base class for integration tests using database transactions instead of Respawn.
///
/// Performance benefit: ~140ms per test (transaction rollback vs Respawn reset)
/// For 462 tests: saves ~65 seconds
/// </summary>
public abstract class TransactionalIntegrationTestBase : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private IServiceScope? _scope;
    private DbTransaction? _transaction;

    protected AppDbContext DbContext { get; private set; } = null!;
    protected HttpClient AnonymousClient { get; private set; } = null!;

    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    protected TransactionalIntegrationTestBase()
    {
        _factory = SharedTestContext.Factory;
    }

    public async Task InitializeAsync()
    {
        // Create a new scope for this test
        _scope = _factory.Services.CreateScope();
        DbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Begin transaction
        var connection = DbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }
        _transaction = await connection.BeginTransactionAsync();

        // Seed base data within transaction
        await SeedBaseDataAsync();

        // Create anonymous client
        AnonymousClient = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        AnonymousClient?.Dispose();

        // Rollback transaction to clean up test data
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
        }

        _scope?.Dispose();
    }

    /// <summary>
    /// Creates an authenticated HttpClient by logging in with the specified credentials.
    /// </summary>
    protected async Task<HttpClient> LoginAsAsync(string username, string password)
    {
        var loginClient = _factory.CreateClient();

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

    protected Task<HttpClient> LoginAsAdminAsync() => LoginAsAsync("admin", "TestAdmin2025@");
    protected Task<HttpClient> LoginAsDoctorAsync() => LoginAsAsync("doctor", "TestDoctor2025@");
    protected Task<HttpClient> LoginAsSysAdminAsync() => LoginAsAsync("sysadmin", "TestAdmin2025@");

    #region Shared User ID Helpers

    protected async Task<Guid> GetAdminUserIdAsync(HttpClient adminClient)
    {
        var response = await adminClient.GetAsync("/api/v1/users?keyword=admin");
        response.EnsureSuccessStatusCode();
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<PagedResult<UserListDto>>>(JsonOptions);
        var adminUser = body!.Data!.Items.First(u => u.UserName == "admin");
        return adminUser.Id;
    }

    protected async Task<Guid> GetDoctorUserIdAsync(HttpClient adminClient)
    {
        var response = await adminClient.GetAsync("/api/v1/users?keyword=doctor");
        response.EnsureSuccessStatusCode();
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<PagedResult<UserListDto>>>(JsonOptions);
        var doctorUser = body!.Data!.Items.First(u => u.UserName == "doctor");
        return doctorUser.Id;
    }

    #endregion

    #region Parallel-Safe Unique Generators

    // Thread-local storage for test-specific prefix
    private static readonly ThreadLocal<string> _testPrefix = new(() => Guid.NewGuid().ToString("N")[..8]);

    /// <summary>
    /// Generates a unique name with thread-specific prefix for parallel test isolation.
    /// </summary>
    protected static string UniqueName(string baseName)
    {
        return $"{_testPrefix.Value!}_{baseName}";
    }

    /// <summary>
    /// Generates a unique phone number for parallel test isolation.
    /// </summary>
    protected static string UniquePhone()
    {
        // Generate unique 11-digit phone: 138 + 8 random digits based on thread prefix
        var prefix = _testPrefix.Value!;
        var randomPart = prefix.GetHashCode() % 100000000;
        return $"138{Math.Abs(randomPart):D8}";
    }

    /// <summary>
    /// Generates a unique ID number for parallel test isolation.
    /// </summary>
    protected static string UniqueIdNumber()
    {
        var prefix = _testPrefix.Value!;
        var random = new Random(prefix.GetHashCode());
        var year = random.Next(1960, 2000);
        var month = random.Next(1, 13);
        var day = random.Next(1, 29);
        var suffix = random.Next(1000, 9999);
        return $"320101{year}{month:D2}{day:D2}{suffix}";
    }

    /// <summary>
    /// Generates a unique email for parallel test isolation.
    /// </summary>
    protected static string UniqueEmail(string baseName)
    {
        return $"{baseName.ToLower()}_{_testPrefix.Value!}@test.com";
    }

    /// <summary>
    /// Generates a unique username for parallel test isolation.
    /// </summary>
    protected static string UniqueUsername(string baseName)
    {
        return $"{baseName.ToLower()}_{_testPrefix.Value!}";
    }

    #endregion

    #region Private Methods

    private async Task SeedBaseDataAsync()
    {
        var roleManager = _factory.Services.CreateScope().ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = _factory.Services.CreateScope().ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        string[] roles = { "Receptionist", "Doctor", "Admin", "SuperAdmin" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        await CreateIdentityUserAsync(userManager, "sysadmin", "系统管理员",
            "SuperAdmin", "TestAdmin2025@", "admin@lybt.com");
        await CreateIdentityUserAsync(userManager, "admin", "测试管理员",
            "Admin", "TestAdmin2025@", "admin-test@lybt.com");
        await CreateIdentityUserAsync(userManager, "doctor", "测试医生",
            "Doctor", "TestDoctor2025@", "doctor-test@lybt.com");
    }

    private static async Task CreateIdentityUserAsync(
        UserManager<ApplicationUser> userManager,
        string userName, string realName,
        string role, string password, string email)
    {
        var existing = await userManager.FindByNameAsync(userName);
        if (existing != null) return;

        var user = new ApplicationUser
        {
            UserName = userName,
            RealName = realName,
            Email = email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, role);
        }
    }

    #endregion
}
