using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using Xunit;

namespace LYBT.Tests.Server.Infrastructure;

/// <summary>
/// Generic base class for all server integration tests.
///
/// Provides:
/// - Per-test database reset via Respawn
/// - Convenience login helpers for common test roles
/// - Access to the anonymous HttpClient
/// - Shared JSON serialization options
///
/// Usage:
///   [Collection("ClinicalData")]
///   public class MyTests : IntegrationTestBase&lt;ClinicalDataFixture&gt;
///   {
///       public MyTests(ClinicalDataFixture fixture) : base(fixture) { }
///   }
/// </summary>
public abstract class IntegrationTestBase<TFixture> : IAsyncLifetime
    where TFixture : ServerFixture
{
    protected TFixture Fixture { get; }

    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    protected IntegrationTestBase(TFixture fixture)
    {
        Fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await Fixture.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    protected Task<HttpClient> LoginAsAdminAsync() => Fixture.LoginAsAdminAsync();
    protected Task<HttpClient> LoginAsDoctorAsync() => Fixture.LoginAsDoctorAsync();
    protected Task<HttpClient> LoginAsReceptionistAsync() => Fixture.LoginAsReceptionistAsync();
    protected Task<HttpClient> LoginAsSysAdminAsync() => Fixture.LoginAsSysAdminAsync();
    protected HttpClient AnonymousClient => Fixture.AnonymousClient;

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
        return $"{_testPrefix.Value}_{baseName}";
    }

    /// <summary>
    /// Generates a unique phone number for parallel test isolation.
    /// </summary>
    protected static string UniquePhone()
    {
        // Generate unique 11-digit phone: 138 + 8 random digits based on thread prefix
        var prefix = _testPrefix.Value;
        var randomPart = prefix.GetHashCode() % 100000000;
        return $"138{Math.Abs(randomPart):D8}";
    }

    /// <summary>
    /// Generates a unique ID number for parallel test isolation.
    /// </summary>
    protected static string UniqueIdNumber()
    {
        var prefix = _testPrefix.Value;
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
        return $"{baseName.ToLower()}_{_testPrefix.Value}@test.com";
    }

    /// <summary>
    /// Generates a unique username for parallel test isolation.
    /// </summary>
    protected static string UniqueUsername(string baseName)
    {
        return $"{baseName.ToLower()}_{_testPrefix.Value}";
    }

    #endregion
}
