using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Auth;
using Xunit;

namespace LYBT.Tests.Desktop;

/// <summary>
/// Integration tests for AuthController (POST /api/auth/*, GET /api/auth/validate).
/// </summary>
public class AuthControllerTests : LocalWebApiControllerTestBase
{
    [Fact]
    public async Task Login_With_Valid_Credentials_Returns_Token()
    {
        var request = new LoginRequest { UserName = "admin", Password = "Admin@123456" };

        var response = await Client.PostAsJsonAsync("/api/auth/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("token").GetString().Should().NotBeNullOrWhiteSpace();
        json.GetProperty("username").GetString().Should().Be("admin");
    }

    [Fact]
    public async Task Sysadmin_Can_Login_With_Default_Password()
    {
        var request = new LoginRequest { UserName = "sysadmin", Password = "SysAdmin@2026!" };

        var response = await Client.PostAsJsonAsync("/api/auth/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            $"sysadmin login failed: {await response.Content.ReadAsStringAsync()}");

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("token").GetString().Should().NotBeNullOrWhiteSpace();
        json.GetProperty("username").GetString().Should().Be("sysadmin");
    }

    [Fact]
    public async Task Login_With_Invalid_Password_Returns_Unauthorized()
    {
        var request = new LoginRequest { UserName = "admin", Password = "wrongpassword" };

        var response = await Client.PostAsJsonAsync("/api/auth/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_With_Nonexistent_User_Returns_Unauthorized()
    {
        var request = new LoginRequest { UserName = "nonexistent_user", Password = "admin123" };

        var response = await Client.PostAsJsonAsync("/api/auth/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_Returns_Ok()
    {
        var request = new LogoutRequest { UserName = "admin" };

        var response = await Client.PostAsJsonAsync("/api/auth/logout", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Validate_With_Valid_Token_Returns_Ok()
    {
        var token = await GetAdminTokenAsync();
        SetAuthHeader(token);

        var response = await Client.GetAsync("/api/auth/validate");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("isValid").GetBoolean().Should().BeTrue();
        json.GetProperty("username").GetString().Should().Be("admin");
    }

    [Fact]
    public async Task Validate_Without_Token_Returns_Ok_With_IsValid_False()
    {
        // No auth header set -- controller returns Ok with IsValid=false
        var response = await Client.GetAsync("/api/auth/validate");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("isValid").GetBoolean().Should().BeFalse();
    }
}
