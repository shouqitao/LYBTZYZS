using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Tests.Integration;

/// <summary>
/// Base class for all Desktop+Server integration tests.
///
/// Provides:
/// - Per-test database reset via Respawn (InitializeAsync calls Fixture.ResetAsync)
/// - Convenience login helpers that return both HttpClient and Refit API client
/// - Access to the anonymous HttpClient
/// - Shared JSON serialization options
/// - Parallel-safe unique value generators
/// </summary>
[Collection("Integration")]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected IntegrationFixture Fixture { get; }

    /// <summary>
    /// Standard JSON options for API response deserialization.
    /// Case-insensitive property names and string-based enum serialization.
    /// </summary>
    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    protected IntegrationTestBase(IntegrationFixture fixture)
    {
        Fixture = fixture;
    }

    public async Task InitializeAsync() => await Fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>Unauthenticated HttpClient for testing anonymous access.</summary>
    protected HttpClient AnonymousClient => Fixture.AnonymousClient;

    /// <summary>Login as admin and return both HttpClient and Refit API client.</summary>
    protected async Task<(HttpClient Client, T Api)> LoginAsAdminWithApiAsync<T>() where T : class
    {
        var client = await Fixture.LoginAsAdminAsync();
        var api = Fixture.CreateApi<T>(client);
        return (client, api);
    }

    /// <summary>Login as doctor and return both HttpClient and Refit API client.</summary>
    protected async Task<(HttpClient Client, T Api)> LoginAsDoctorWithApiAsync<T>() where T : class
    {
        var client = await Fixture.LoginAsDoctorAsync();
        var api = Fixture.CreateApi<T>(client);
        return (client, api);
    }

    /// <summary>Login as sysadmin and return both HttpClient and Refit API client.</summary>
    protected async Task<(HttpClient Client, T Api)> LoginAsSysAdminWithApiAsync<T>() where T : class
    {
        var client = await Fixture.LoginAsSysAdminAsync();
        var api = Fixture.CreateApi<T>(client);
        return (client, api);
    }

    /// <summary>Login as admin and return an authenticated HttpClient.</summary>
    protected Task<HttpClient> LoginAsAdminAsync() => Fixture.LoginAsAdminAsync();

    /// <summary>Login as doctor and return an authenticated HttpClient.</summary>
    protected Task<HttpClient> LoginAsDoctorAsync() => Fixture.LoginAsDoctorAsync();

    /// <summary>Login as sysadmin and return an authenticated HttpClient.</summary>
    protected Task<HttpClient> LoginAsSysAdminAsync() => Fixture.LoginAsSysAdminAsync();

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
}
