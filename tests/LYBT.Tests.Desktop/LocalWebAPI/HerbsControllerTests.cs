using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LYBT.Entities.Herbs;
using LYBT.Shared.Models.Enums;
using Xunit;

namespace LYBT.Tests.Desktop.LocalWebAPI;

/// <summary>
/// Integration tests for HerbsController (GET/POST/PUT/DELETE /api/herbs/*).
/// All endpoints require [Authorize].
/// </summary>
public class HerbsControllerTests : LocalWebApiControllerTestBase
{
    private async Task AuthenticateAsync()
    {
        var token = await GetAdminTokenAsync();
        SetAuthHeader(token);
    }

    private async Task<JsonElement> CreateTestHerbAsync(string? name = null, string? category = null)
    {
        var herb = new Herb
        {
            Name = name ?? $"TestHerb_{Guid.NewGuid():N}",
            Category = category ?? "TestCategory",
            Unit = "g",
            Price = 5.50m,
            Effect = "Test effect",
            Status = CommonStatus.Enabled
        };

        var response = await Client.PostAsJsonAsync("/api/herbs", herb);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>(Json);
    }

    [Fact]
    public async Task GetHerbs_Returns_Ok()
    {
        await AuthenticateAsync();

        var response = await Client.GetAsync("/api/herbs?keyword=%20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var herbs = await response.Content.ReadFromJsonAsync<List<JsonElement>>(Json);
        herbs.Should().NotBeNull();
        herbs!.Should().HaveCountGreaterThanOrEqualTo(1); // seed data includes Ginseng
    }

    [Fact]
    public async Task CreateHerb_And_GetById_Works()
    {
        await AuthenticateAsync();

        var created = await CreateTestHerbAsync("Dang Gui");
        var herbId = created.GetProperty("id").GetGuid();

        var response = await Client.GetAsync($"/api/herbs/{herbId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("name").GetString().Should().Be("Dang Gui");
    }

    [Fact]
    public async Task DeleteHerb_Soft_Deletes()
    {
        await AuthenticateAsync();

        var created = await CreateTestHerbAsync();
        var herbId = created.GetProperty("id").GetGuid();

        // Delete
        var deleteResponse = await Client.DeleteAsync($"/api/herbs/{herbId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // GET should return NotFound after soft delete
        var getResponse = await Client.GetAsync($"/api/herbs/{herbId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RestoreHerb_Works_After_Soft_Delete()
    {
        await AuthenticateAsync();

        var created = await CreateTestHerbAsync();
        var herbId = created.GetProperty("id").GetGuid();

        // Delete
        await Client.DeleteAsync($"/api/herbs/{herbId}");

        // Restore
        var restoreResponse = await Client.PostAsJsonAsync($"/api/herbs/{herbId}/restore", (object?)null);
        restoreResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // GET should succeed after restore
        var getResponse = await Client.GetAsync($"/api/herbs/{herbId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ToggleStatus_Toggles_Herb()
    {
        await AuthenticateAsync();

        var created = await CreateTestHerbAsync();
        var herbId = created.GetProperty("id").GetGuid();

        // Toggle (Enabled -> Disabled)
        var toggleResponse = await Client.PostAsJsonAsync($"/api/herbs/{herbId}/toggle-status", (object?)null);
        toggleResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await toggleResponse.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("status").GetInt32().Should().Be((int)CommonStatus.Disabled);

        // Toggle back
        var toggleResponse2 = await Client.PostAsJsonAsync($"/api/herbs/{herbId}/toggle-status", (object?)null);
        toggleResponse2.StatusCode.Should().Be(HttpStatusCode.OK);

        var json2 = await toggleResponse2.Content.ReadFromJsonAsync<JsonElement>(Json);
        json2.GetProperty("status").GetInt32().Should().Be((int)CommonStatus.Enabled);
    }

    [Fact]
    public async Task GetCategories_Returns_Distinct()
    {
        await AuthenticateAsync();

        // Create herbs with specific categories
        await CreateTestHerbAsync($"CatTest1_{Guid.NewGuid():N}", "QiTonics");
        await CreateTestHerbAsync($"CatTest2_{Guid.NewGuid():N}", "BloodTonics");

        var response = await Client.GetAsync("/api/herbs/categories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var categories = await response.Content.ReadFromJsonAsync<List<string>>(Json);
        categories.Should().NotBeNull();
        categories!.Should().Contain("QiTonics");
        categories.Should().Contain("BloodTonics");
    }
}
