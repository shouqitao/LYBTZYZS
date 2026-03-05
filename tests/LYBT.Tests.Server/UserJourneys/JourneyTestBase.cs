using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Tests.Server.Infrastructure;
using Xunit;

namespace LYBT.Tests.Server.UserJourneys;

/// <summary>
/// Base class for UserJourney tests.
/// Each journey is a single [Fact] method containing all steps sequentially.
/// Resets DB once at the start, then steps build on prior state via local variables.
/// </summary>
[Collection("Server")]
public abstract class JourneyTestBase
{
    protected ServerFixture Fixture { get; }
    protected HttpClient AnonymousClient => Fixture.AnonymousClient;

    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    protected JourneyTestBase(ServerFixture fixture)
    {
        Fixture = fixture;
    }

    protected async Task ResetForJourneyAsync()
    {
        await Fixture.ResetAsync();
    }

    protected Task<HttpClient> LoginAsAdminAsync() => Fixture.LoginAsAdminAsync();
    protected Task<HttpClient> LoginAsDoctorAsync() => Fixture.LoginAsDoctorAsync();
    protected Task<HttpClient> LoginAsSysAdminAsync() => Fixture.LoginAsSysAdminAsync();
    protected Task<HttpClient> LoginAsAsync(string username, string password)
        => Fixture.LoginAsAsync(username, password);

    protected async Task<(HttpResponseMessage Response, T? Data)> PostAsync<T>(
        HttpClient client, string url, object payload) where T : class
    {
        var response = await client.PostAsJsonAsync(url, payload);
        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions);
            return (response, body?.Data);
        }
        return (response, default);
    }

    protected async Task<(HttpResponseMessage Response, T? Data)> PutAsync<T>(
        HttpClient client, string url, object payload) where T : class
    {
        var response = await client.PutAsJsonAsync(url, payload);
        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions);
            return (response, body?.Data);
        }
        return (response, default);
    }

    protected async Task<(HttpResponseMessage Response, T? Data)> GetAsync<T>(
        HttpClient client, string url) where T : class
    {
        var response = await client.GetAsync(url);
        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions);
            return (response, body?.Data);
        }
        return (response, default);
    }

    protected static string UniqueName(string prefix)
        => $"{prefix}_{Guid.NewGuid():N}"[..20];

    protected static string UniquePhone()
        => $"138{Random.Shared.Next(10000000, 99999999)}";
}
