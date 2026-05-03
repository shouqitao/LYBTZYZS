using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace LYBT.Tests.Desktop.LocalWebAPI;

/// <summary>
/// Integration tests for HealthController (GET /api/health/*).
/// All endpoints are [AllowAnonymous] -- no auth required.
/// </summary>
public class HealthControllerTests : LocalWebApiControllerTestBase
{
    [Fact]
    public async Task Ping_Returns_Ok()
    {
        var response = await Client.GetAsync("/api/health/ping");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("status").GetString().Should().Be("ok");
    }

    [Fact]
    public async Task GetHealth_Returns_Ok()
    {
        var response = await Client.GetAsync("/api/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("status").GetString().Should().Be("Healthy");
        json.GetProperty("database").GetString().Should().Be("Connected");
    }

    [Fact]
    public async Task GetDetails_Returns_User_Count()
    {
        var response = await Client.GetAsync("/api/health/details");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("status").GetString().Should().Be("Healthy");
        json.GetProperty("version").GetString().Should().Be("1.0.0-local");
        json.GetProperty("statistics").GetProperty("totalUsers").GetInt32().Should().BeGreaterThanOrEqualTo(1);
    }
}
