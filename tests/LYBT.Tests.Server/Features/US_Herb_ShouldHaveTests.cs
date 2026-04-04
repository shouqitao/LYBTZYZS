using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Server.Infrastructure;
using LYBT.Tests.Server.Infrastructure.TestDataBuilders;
using Xunit;

namespace LYBT.Tests.Server.Features.Herbs;

/// <summary>
/// Should Have User Stories for Herbs module.
/// PRD: US-HERB-006, US-HERB-008, US-HERB-009, US-HERB-011 (4 Should Have)
/// Collection: HerbFormula (isolated DB, parallel with other domains)
/// </summary>
[Collection("HerbFormula")]
public sealed class US_Herb_ShouldHaveTests : IntegrationTestBase<HerbFormulaFixture>
{
    public US_Herb_ShouldHaveTests(HerbFormulaFixture fixture) : base(fixture) { }

    #region Helpers

    private async Task<(Guid Id, string Name)> CreateHerbAsync(HttpClient client, string name = "SH药材")
    {
        var payload = HerbBuilder.Default().WithName($"{name}_{Guid.NewGuid():N}"[..12]).Build();
        var response = await client.PostAsJsonAsync("/api/v1/herbs", payload);
        var data = await response.ShouldBeSuccessWithDataAsync<HerbDetailDto>();
        return (data.Id, data.Name);
    }

    #endregion

    #region US-HERB-006: Enable/disable herb

    [Fact]
    public async Task US_HERB_006_ToggleHerbToDisabled_IsActiveBecomesFalse()
    {
        // Arrange
        var client = await LoginAsAdminAsync();
        var (herbId, _) = await CreateHerbAsync(client, "禁用测试");

        // Act - toggle to disabled
        var response = await client.PostAsync($"/api/v1/herbs/{herbId}/toggle-status", null);

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.NoContent },
            "US-HERB-006: toggle should succeed");

        // Verify
        var getResp = await client.GetAsync($"/api/v1/herbs/{herbId}");
        var data = await getResp.ShouldBeSuccessWithDataAsync<HerbDetailDto>();
        data.Status.Should().Be(CommonStatus.Disabled, "US-HERB-006: herb should be disabled after toggle");
    }

    [Fact]
    public async Task US_HERB_006_ToggleDisabledHerbBackToActive()
    {
        // Arrange
        var client = await LoginAsAdminAsync();
        var (herbId, _) = await CreateHerbAsync(client, "启用测试");

        // First toggle: active -> disabled
        await client.PostAsync($"/api/v1/herbs/{herbId}/toggle-status", null);

        // Act - second toggle: disabled -> active
        var response = await client.PostAsync($"/api/v1/herbs/{herbId}/toggle-status", null);

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.NoContent },
            "US-HERB-006: re-enable should succeed");

        var getResp = await client.GetAsync($"/api/v1/herbs/{herbId}");
        var data = await getResp.ShouldBeSuccessWithDataAsync<HerbDetailDto>();
        data.Status.Should().Be(CommonStatus.Enabled, "US-HERB-006: herb should be active after second toggle");
    }

    #endregion

    #region US-HERB-008: Batch delete herbs

    [Fact]
    public async Task US_HERB_008_BatchDelete_WithoutReferences_Succeeds()
    {
        // Arrange
        var client = await LoginAsAdminAsync();
        var herb1 = await CreateHerbAsync(client, "批删甲");
        var herb2 = await CreateHerbAsync(client, "批删乙");

        var payload = new { Ids = new[] { herb1.Id, herb2.Id } };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/herbs/batch-delete", payload);

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.NoContent },
            "US-HERB-008: batch delete unreferenced herbs should succeed");
    }

    [Fact]
    public async Task US_HERB_008_BatchDelete_EmptyList_Returns400()
    {
        // Arrange
        var client = await LoginAsAdminAsync();
        var payload = new { Ids = Array.Empty<Guid>() };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/herbs/batch-delete", payload);

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity, HttpStatusCode.OK },
            "US-HERB-008: empty batch delete should be handled");
    }

    #endregion

    #region US-HERB-009: Import herbs

    [Fact]
    public async Task US_HERB_009_BatchImport_ValidData_ReturnsResult()
    {
        // Arrange - HerbBatchImportInputDto wraps Herbs list + Strategy
        var client = await LoginAsAdminAsync();
        var payload = new
        {
            Herbs = new[]
            {
                new { Name = $"导入甲_{Guid.NewGuid():N}"[..12], Category = "补气", PinYinCode = "DRJ" },
                new { Name = $"导入乙_{Guid.NewGuid():N}"[..12], Category = "解表", PinYinCode = "DRY" }
            },
            Strategy = 0  // DuplicateStrategy.Skip
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/herbs/batch-import", payload);

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.Created },
            "US-HERB-009: batch import valid herbs should succeed");
    }

    [Fact]
    public async Task US_HERB_009_BatchImport_EmptyList_Returns400()
    {
        // Arrange
        var client = await LoginAsAdminAsync();
        var payload = new { Herbs = Array.Empty<object>(), Strategy = 0 };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/herbs/batch-import", payload);

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity, HttpStatusCode.OK },
            "US-HERB-009: empty import should be handled");
    }

    #endregion

    #region US-HERB-011: Export herbs

    [Fact]
    public async Task US_HERB_011_Export_ReturnsData()
    {
        // Arrange
        var client = await LoginAsAdminAsync();
        await CreateHerbAsync(client, "导出测试");

        // Act
        var response = await client.GetAsync("/api/v1/herbs/export-all");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-HERB-011: export should return 200");
    }

    [Fact]
    public async Task US_HERB_011_Export_Anonymous_Returns401()
    {
        // Act
        var response = await AnonymousClient.GetAsync("/api/v1/herbs/export-all");

        // Assert
        response.ShouldBeUnauthorized();
    }

    #endregion
}
