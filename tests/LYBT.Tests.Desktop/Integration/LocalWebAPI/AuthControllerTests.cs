using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Auth;
using Xunit;

namespace LYBT.Tests.Desktop;

/// <summary>
/// LocalWebAPI Auth 端点集成测试 — 以 sysadmin 为出发点
/// </summary>
public class AuthControllerTests : LocalWebApiControllerTestBase
{
    #region sysadmin 登录

    [Fact]
    public async Task Sysadmin_Can_Login_With_Default_Password()
    {
        var response = await LoginAsync("sysadmin", "SysAdmin@2026!");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            $"sysadmin login failed: {await response.Content.ReadAsStringAsync()}");

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("data").GetProperty("token").GetString().Should().NotBeNullOrWhiteSpace();
        json.GetProperty("data").GetProperty("user").GetProperty("username").GetString().Should().Be("sysadmin");
    }

    [Fact]
    public async Task Sysadmin_Token_Contains_IsSysAdmin_Claim()
    {
        var response = await LoginAsync("sysadmin", "SysAdmin@2026!");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        var token = json.GetProperty("data").GetProperty("token").GetString()!;

        // Decode JWT payload
        var parts = token.Split('.');
        var payload = parts[1];
        var padded = payload.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4) { case 2: padded += "=="; break; case 3: padded += "="; break; }
        var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(padded));
        var claims = JsonSerializer.Deserialize<JsonElement>(decoded);

        claims.TryGetProperty("IsSysAdmin", out var isSysAdmin).Should().BeTrue("JWT should contain IsSysAdmin claim");
        isSysAdmin.GetString().Should().Be("true");
    }

    #endregion

    #region sysadmin 受保护端点

    [Fact]
    public async Task Sysadmin_Can_Access_Validated_With_Token()
    {
        var token = await GetSysadminTokenAsync();
        SetAuthHeader(token);

        var response = await Client.GetAsync("/api/v1/auth/validate");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("data").GetProperty("isValid").GetBoolean().Should().BeTrue();
        json.GetProperty("data").GetProperty("username").GetString().Should().Be("sysadmin");
    }

    [Fact]
    public async Task Validate_Without_Token_Returns_IsValid_False()
    {
        var response = await Client.GetAsync("/api/v1/auth/validate");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("success").GetBoolean().Should().BeFalse();
    }

    #endregion

    #region admin 登录

    [Fact]
    public async Task Admin_Can_Login_With_Default_Password()
    {
        var response = await LoginAsync("admin", "Admin@123456");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("data").GetProperty("token").GetString().Should().NotBeNullOrWhiteSpace();
        json.GetProperty("data").GetProperty("user").GetProperty("username").GetString().Should().Be("admin");
    }

    #endregion

    #region 错误场景

    [Fact]
    public async Task Login_With_Wrong_Password_Returns_Unauthorized()
    {
        var response = await LoginAsync("admin", "wrongpassword");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_With_Nonexistent_User_Returns_Unauthorized()
    {
        var response = await LoginAsync("nonexistent_user", "Admin@123456");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region 登出

    [Fact]
    public async Task Logout_Returns_Ok()
    {
        var request = new LogoutRequest { UserName = "admin" };
        var response = await Client.PostAsJsonAsync("/api/v1/auth/logout", request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region 辅助方法

    private async Task<HttpResponseMessage> LoginAsync(string username, string password)
    {
        var request = new LoginRequest { UserName = username, Password = password };
        return await Client.PostAsJsonAsync("/api/v1/auth/login", request);
    }

    private async Task<string> GetSysadminTokenAsync()
    {
        var response = await LoginAsync("sysadmin", "SysAdmin@2026!");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        return json.GetProperty("data").GetProperty("token").GetString()!;
    }

    #endregion
}
