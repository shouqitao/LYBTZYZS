using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LYBT.Shared.Models.Enums;
using Xunit;

using static LYBT.LocalWebAPI.Controllers.UsersController;

namespace LYBT.Tests.Desktop.LocalWebAPI;

/// <summary>
/// Integration tests for UsersController (GET/POST/PUT/DELETE /api/users/*).
/// All endpoints require [Authorize].
/// </summary>
public class UsersControllerTests : LocalWebApiControllerTestBase
{
    private async Task AuthenticateAsync()
    {
        var token = await GetAdminTokenAsync();
        SetAuthHeader(token);
    }

    [Fact]
    public async Task GetAll_Returns_Admin_User()
    {
        await AuthenticateAsync();

        var response = await Client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var users = await response.Content.ReadFromJsonAsync<List<JsonElement>>(Json);
        users.Should().NotBeNull();
        users!.Should().HaveCountGreaterThanOrEqualTo(1);
        users.Should().Contain(u => u.GetProperty("username").GetString() == "admin");
    }

    [Fact]
    public async Task GetById_Returns_Admin()
    {
        await AuthenticateAsync();

        // Get the admin user's ID from the list
        var listResponse = await Client.GetAsync("/api/users");
        var users = await listResponse.Content.ReadFromJsonAsync<List<JsonElement>>(Json);
        var admin = users!.First(u => u.GetProperty("username").GetString() == "admin");
        var adminId = admin.GetProperty("id").GetGuid();

        var response = await Client.GetAsync($"/api/users/{adminId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("username").GetString().Should().Be("admin");
        json.GetProperty("role").GetInt32().Should().Be((int)UserRole.Admin);
    }

    [Fact]
    public async Task GetById_Returns_NotFound_For_Invalid_Id()
    {
        await AuthenticateAsync();

        var response = await Client.GetAsync($"/api/users/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_User_Succeeds()
    {
        await AuthenticateAsync();

        var dto = new UserCreateDto
        {
            Username = $"testuser_{Guid.NewGuid():N}",
            Password = "TestPass123",
            Role = UserRole.Doctor,
            RealName = "Test Doctor"
        };

        var response = await Client.PostAsJsonAsync("/api/users", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("username").GetString().Should().Be(dto.Username);
        json.GetProperty("role").GetInt32().Should().Be((int)UserRole.Doctor);
    }

    [Fact]
    public async Task Create_Duplicate_User_Returns_Conflict()
    {
        await AuthenticateAsync();

        var dto = new UserCreateDto
        {
            Username = "admin",
            Password = "AdminPass123",
            Role = UserRole.Admin,
            RealName = "Duplicate Admin"
        };

        var response = await Client.PostAsJsonAsync("/api/users", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ToggleStatus_Toggles_User_Status()
    {
        await AuthenticateAsync();

        // Create a user first
        var username = $"toggleuser_{Guid.NewGuid():N}";
        var dto = new UserCreateDto
        {
            Username = username,
            Password = "TogglePass123",
            Role = UserRole.Doctor,
            RealName = "Toggle User"
        };
        var createResponse = await Client.PostAsJsonAsync("/api/users", dto);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>(Json);
        var userId = created.GetProperty("id").GetGuid();

        // Toggle status (Enabled -> Disabled)
        var toggleResponse = await Client.PostAsJsonAsync($"/api/users/{userId}/toggle-status", (object?)null);

        toggleResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await toggleResponse.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("status").GetInt32().Should().Be((int)CommonStatus.Disabled);

        // Toggle again (Disabled -> Enabled)
        var toggleResponse2 = await Client.PostAsJsonAsync($"/api/users/{userId}/toggle-status", (object?)null);

        toggleResponse2.StatusCode.Should().Be(HttpStatusCode.OK);

        var json2 = await toggleResponse2.Content.ReadFromJsonAsync<JsonElement>(Json);
        json2.GetProperty("status").GetInt32().Should().Be((int)CommonStatus.Enabled);
    }
}
