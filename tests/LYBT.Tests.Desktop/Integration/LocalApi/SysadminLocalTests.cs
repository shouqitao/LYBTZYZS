using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Xunit;

namespace LYBT.Tests.Desktop.Integration.LocalApi;

[Trait("Category", "LocalApi")]
[Collection("LocalApi")]
public class SysadminLocalTests : LocalWebApiTestBase
{
    [Fact]
    [Trait("US", "US-AUTH-001")]
    public async Task Sysadmin_Login_ReturnsToken()
    {
        var token = await GetSysadminTokenAsync();
        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    [Trait("US", "US-AUTH-002")]
    public async Task Sysadmin_Token_Contains_IsSysAdmin_Claim()
    {
        var token = await GetSysadminTokenAsync();
        var parts = token.Split('.');
        var payload = parts[1].Replace('-', '+').Replace('_', '/');
        switch (payload.Length % 4) { case 2: payload += "=="; break; case 3: payload += "="; break; }
        var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
        var claims = JsonSerializer.Deserialize<JsonElement>(decoded);
        claims.TryGetProperty("IsSysAdmin", out var isSysAdmin).Should().BeTrue();
        isSysAdmin.GetString().Should().Be("true");
    }

    [Fact]
    [Trait("US", "US-SHELL-003")]
    public async Task Sysadmin_GetConfiguration_ReturnsData()
    {
        var token = await GetSysadminTokenAsync();
        SetAuthHeader(token);

        var response = await Client.GetAsync("/api/v1/configuration");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    [Trait("US", "US-USER-001")]
    public async Task Sysadmin_GetUsers_ReturnsPaged()
    {
        var token = await GetSysadminTokenAsync();
        SetAuthHeader(token);

        var response = await Client.GetAsync("/api/v1/users");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    [Trait("US", "US-USER-003")]
    public async Task Sysadmin_CreateUser_Succeeds()
    {
        var token = await GetSysadminTokenAsync();
        SetAuthHeader(token);

        var dto = new UserInputDto
        {
            UserName = UniqueUsername(),
            Password = "Test1234!",
            ConfirmPassword = "Test1234!",
            RealName = $"E2E_{Guid.NewGuid():N}"[..8],
            Role = UserRole.Admin
        };

        var response = await Client.PostAsJsonAsync("/api/v1/users", dto);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    [Trait("US", "US-SYS-001")]
    public async Task Sysadmin_GetDiagnostics_ReturnsData()
    {
        var token = await GetSysadminTokenAsync();
        SetAuthHeader(token);

        var response = await Client.GetAsync("/api/v1/diagnostics/db-info");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
    }
}
