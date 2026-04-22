using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
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

    // Static counter for unique value generation (combines with thread prefix for isolation)
    private static int _globalSequence = 0;

    /// <summary>
    /// Generates a unique name with thread-specific prefix for parallel test isolation.
    /// </summary>
    protected static string UniqueName(string baseName)
    {
        var seq = Interlocked.Increment(ref _globalSequence);
        return $"{_testPrefix.Value!}_{baseName}_{seq}";
    }

    /// <summary>
    /// Generates a unique phone number for parallel test isolation.
    /// </summary>
    protected static string UniquePhone()
    {
        // Generate unique 11-digit phone: 138 + 8 digits
        var prefix = _testPrefix.Value!;
        var seq = Interlocked.Increment(ref _globalSequence);
        var hashPart = Math.Abs(prefix.GetHashCode()) % 1000; // 3 digits
        var seqPart = seq % 100000; // 5 digits
        return $"138{hashPart:D3}{seqPart:D5}";
    }

    /// <summary>
    /// Generates a unique ID number for parallel test isolation.
    /// </summary>
    protected static string UniqueIdNumber()
    {
        var prefix = _testPrefix.Value!;
        var seq = Interlocked.Increment(ref _globalSequence);
        var random = new Random(prefix.GetHashCode() + seq);
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
        var seq = Interlocked.Increment(ref _globalSequence);
        return $"{baseName.ToLower()}_{_testPrefix.Value!}_{seq}@test.com";
    }

    /// <summary>
    /// Generates a unique username for parallel test isolation.
    /// </summary>
    protected static string UniqueUsername(string baseName)
    {
        var seq = Interlocked.Increment(ref _globalSequence);
        return $"{baseName.ToLower()}_{_testPrefix.Value!}_{seq}";
    }

    #endregion

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
}
