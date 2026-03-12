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
}
