using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Tests.Server.Infrastructure;
using Xunit;

namespace LYBT.Tests.Server.UserJourneys;

/// <summary>
/// Generic base class for UserJourney tests.
/// Each journey is a single [Fact] containing all steps sequentially.
///
/// Usage:
///   [Collection("ClinicalData")]
///   public class MyJourney : JourneyTestBase&lt;ClinicalDataFixture&gt;
///   {
///       public MyJourney(ClinicalDataFixture fixture) : base(fixture) { }
///   }
/// </summary>
public abstract class JourneyTestBase<TFixture>
    where TFixture : ServerFixture
{
    protected TFixture Fixture { get; }
    protected HttpClient AnonymousClient => Fixture.AnonymousClient;

    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    protected JourneyTestBase(TFixture fixture)
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

    protected async Task<(string Message, int StatusCode)> ReadErrorAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        try
        {
            var body = JsonSerializer.Deserialize<ApiResponse<object>>(content, JsonOptions);
            return (body?.Message ?? content, (int)response.StatusCode);
        }
        catch
        {
            return (content, (int)response.StatusCode);
        }
    }

    protected static string UniqueName(string prefix)
        => $"{prefix}_{Guid.NewGuid():N}"[..20];

    protected static string UniquePhone()
        => $"138{Random.Shared.Next(10000000, 99999999)}";
}
