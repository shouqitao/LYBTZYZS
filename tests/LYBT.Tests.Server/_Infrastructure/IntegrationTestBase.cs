using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using Xunit;

namespace LYBT.Tests.Server.Infrastructure;

/// <summary>
/// Base class for all server integration tests.
///
/// Provides:
/// - Per-test database reset via Respawn (InitializeAsync calls Fixture.ResetAsync)
/// - Convenience login helpers for common test roles
/// - Access to the anonymous HttpClient
/// - Shared JSON serialization options
/// - Common user ID lookup helpers
///
/// Usage:
///   [Collection("Server")]
///   public class MyTests : IntegrationTestBase
///   {
///       public MyTests(ServerFixture fixture) : base(fixture) { }
///   }
/// </summary>
[Collection("Server")]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected ServerFixture Fixture { get; }

    /// <summary>
    /// Standard JSON options for API response deserialization.
    /// All integration tests use the same configuration: case-insensitive property names
    /// and string-based enum serialization.
    /// </summary>
    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    protected IntegrationTestBase(ServerFixture fixture)
    {
        Fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await Fixture.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>Login as admin (Admin role) and return an authenticated HttpClient.</summary>
    protected Task<HttpClient> LoginAsAdminAsync() => Fixture.LoginAsAdminAsync();

    /// <summary>Login as doctor (Doctor role) and return an authenticated HttpClient.</summary>
    protected Task<HttpClient> LoginAsDoctorAsync() => Fixture.LoginAsDoctorAsync();

    /// <summary>Login as sysadmin (SuperAdmin role) and return an authenticated HttpClient.</summary>
    protected Task<HttpClient> LoginAsSysAdminAsync() => Fixture.LoginAsSysAdminAsync();

    /// <summary>Unauthenticated HttpClient for testing anonymous access.</summary>
    protected HttpClient AnonymousClient => Fixture.AnonymousClient;

    #region Shared User ID Helpers

    /// <summary>
    /// Look up the admin user ID by querying the users API.
    /// The fixture seeds a user with username "admin".
    /// </summary>
    protected async Task<Guid> GetAdminUserIdAsync(HttpClient adminClient)
    {
        var response = await adminClient.GetAsync("/api/v1/users?keyword=admin");
        response.EnsureSuccessStatusCode();
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<PagedResult<UserListDto>>>(JsonOptions);
        var adminUser = body!.Data!.Items.First(u => u.UserName == "admin");
        return adminUser.Id;
    }

    /// <summary>
    /// Look up the doctor user ID by querying the users API.
    /// The fixture seeds a user with username "doctor".
    /// </summary>
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
}
