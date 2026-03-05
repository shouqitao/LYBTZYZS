using System.Text.Json;
using System.Text.Json.Serialization;

namespace LYBT.Tests.Integration;

/// <summary>
/// Base class for all Desktop+Server integration tests.
///
/// Provides:
/// - Per-test database reset via Respawn (InitializeAsync calls Fixture.ResetAsync)
/// - Convenience login helpers that return both HttpClient and Refit API client
/// - Access to the anonymous HttpClient
/// - Shared JSON serialization options
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
}
