using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Server.Infrastructure;
using LYBT.Tests.Server.Infrastructure.TestDataBuilders;
using Xunit;

namespace LYBT.Tests.Server.Features.Herbs;

/// <summary>
/// Must Have User Stories for Herbs module.
/// PRD: US-HERB-001 ~ US-HERB-005 (5 Must Have)
/// Collection: HerbFormula (isolated DB, parallel with other domains)
/// </summary>
[Collection("HerbFormula")]
public sealed class US_Herb_MustHaveTests : IntegrationTestBase<HerbFormulaFixture>
{
    public US_Herb_MustHaveTests(HerbFormulaFixture fixture) : base(fixture) { }

    #region US-HERB-001: Create herb

    [Fact]
    public async Task US_HERB_001_CreateHerb_WithValidData_ReturnsCreatedHerb()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var payload = HerbBuilder.Default()
            .WithName("黄芪")
            .WithCategory("补气药")
            .WithPrice(15.0m)
            .WithUnit("克")
            .Build();

        // Act
        var response = await doctorClient.PostAsJsonAsync("/api/v1/herbs", payload);

        // Assert
        var data = await response.ShouldBeSuccessWithDataAsync<HerbDetailDto>(
            "US-HERB-001: doctor should create herb successfully");
        data.Name.Should().Be("黄芪");
        data.Category.Should().Be("补气药");
        data.Price.Should().Be(15.0m);
        data.Unit.Should().Be("克");
        data.Id.Should().NotBeEmpty();
        data.Status.Should().Be(CommonStatus.Enabled, "new herb should be enabled by default");
    }

    [Fact]
    public async Task US_HERB_001_CreateHerb_WithPinYinCode_AutoOrManual()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var payload = HerbBuilder.Default()
            .WithName("当归")
            .WithPinYinCode("DG")
            .Build();

        // Act
        var response = await doctorClient.PostAsJsonAsync("/api/v1/herbs", payload);

        // Assert
        var data = await response.ShouldBeSuccessWithDataAsync<HerbDetailDto>(
            "US-HERB-001: herb with pinyin code should succeed");
        data.Name.Should().Be("当归");
        data.PinYinCode.Should().NotBeNullOrWhiteSpace("pinyin code should be set");
    }

    [Fact]
    public async Task US_HERB_001_CreateHerb_WithoutName_Returns400()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var payload = new { Name = "", Unit = "克", Price = 10.0m };

        // Act
        var response = await doctorClient.PostAsJsonAsync("/api/v1/herbs", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "US-HERB-001: empty name should fail validation");
    }

    #endregion

    #region US-HERB-002: Update herb

    [Fact]
    public async Task US_HERB_002_UpdateHerb_ModifiesFields()
    {
        // Arrange - create herb first
        var doctorClient = await LoginAsDoctorAsync();
        var createPayload = HerbBuilder.Default().WithName("待更新药材").Build();
        var createResp = await doctorClient.PostAsJsonAsync("/api/v1/herbs", createPayload);
        var created = await createResp.ShouldBeSuccessWithDataAsync<HerbDetailDto>();

        // Act - update
        var updatePayload = HerbBuilder.Default()
            .WithName("已更新药材")
            .WithCategory("活血化瘀")
            .WithPrice(20.0m)
            .WithEffect("活血祛瘀，通经止痛")
            .Build();
        var response = await doctorClient.PutAsJsonAsync($"/api/v1/herbs/{created.Id}", updatePayload);

        // Assert
        var data = await response.ShouldBeSuccessWithDataAsync<HerbDetailDto>(
            "US-HERB-002: update should return modified herb");
        data.Name.Should().Be("已更新药材");
        data.Category.Should().Be("活血化瘀");
        data.Price.Should().Be(20.0m);
        data.Effect.Should().Be("活血祛瘀，通经止痛");
        data.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task US_HERB_002_UpdateHerb_NonexistentId_Returns404()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var fakeId = Guid.NewGuid();
        var payload = HerbBuilder.Default().Build();

        // Act
        var response = await doctorClient.PutAsJsonAsync($"/api/v1/herbs/{fakeId}", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "US-HERB-002: update non-existent herb should return 404");
    }

    #endregion

    #region US-HERB-003: Search herbs

    [Fact]
    public async Task US_HERB_003_SearchByKeyword_ReturnsMatchingHerbs()
    {
        // Arrange - create herbs with unique prefix
        var doctorClient = await LoginAsDoctorAsync();
        var uniquePrefix = $"搜索_{Guid.NewGuid():N}"[..8];
        var payload1 = HerbBuilder.Default().WithName($"{uniquePrefix}甲").Build();
        var payload2 = HerbBuilder.Default().WithName($"{uniquePrefix}乙").Build();

        await doctorClient.PostAsJsonAsync("/api/v1/herbs", payload1);
        await doctorClient.PostAsJsonAsync("/api/v1/herbs", payload2);

        // Act
        var response = await doctorClient.GetAsync(
            $"/api/v1/herbs?keyword={uniquePrefix}&page=1&pageSize=10");

        // Assert
        var paged = await response.ShouldBePagedResultAsync<HerbListDto>(
            expectedMinCount: 2,
            because: "US-HERB-003: keyword search should match created herbs");
    }

    [Fact]
    public async Task US_HERB_003_SearchByCategory_ReturnsFiltered()
    {
        // Arrange - create herb with specific category
        var doctorClient = await LoginAsDoctorAsync();
        var uniqueCategory = $"测试分类_{Guid.NewGuid():N}"[..10];
        var payload = HerbBuilder.Default().WithCategory(uniqueCategory).Build();
        await doctorClient.PostAsJsonAsync("/api/v1/herbs", payload);

        // Act
        var response = await doctorClient.GetAsync(
            $"/api/v1/herbs?category={uniqueCategory}&page=1&pageSize=10");

        // Assert
        var paged = await response.ShouldBePagedResultAsync<HerbListDto>(
            expectedMinCount: 1,
            because: "US-HERB-003: category filter should return matching herbs");
    }

    [Fact]
    public async Task US_HERB_003_Pagination_RespectsPageSize()
    {
        // Arrange - create multiple herbs
        var doctorClient = await LoginAsDoctorAsync();
        for (var i = 0; i < 3; i++)
        {
            var payload = HerbBuilder.Default().Build();
            await doctorClient.PostAsJsonAsync("/api/v1/herbs", payload);
        }

        // Act
        var response = await doctorClient.GetAsync("/api/v1/herbs?page=1&pageSize=2");

        // Assert
        var paged = await response.ShouldBePagedResultAsync<HerbListDto>(
            because: "US-HERB-003: pagination should work");
        paged.Items.Should().HaveCountLessThanOrEqualTo(2, "page size should be respected");
    }

    #endregion

    #region US-HERB-004: Delete herb (with reference check)

    [Fact]
    public async Task US_HERB_004_DeleteHerb_WithoutReferences_Succeeds()
    {
        // Arrange - create herb with no prescription references
        var doctorClient = await LoginAsDoctorAsync();
        var payload = HerbBuilder.Default().WithName("待删除无引用药材").Build();
        var createResp = await doctorClient.PostAsJsonAsync("/api/v1/herbs", payload);
        var created = await createResp.ShouldBeSuccessWithDataAsync<HerbDetailDto>();

        // Act
        var response = await doctorClient.DeleteAsync($"/api/v1/herbs/{created.Id}");

        // Assert
        await response.ShouldBeSuccessAsync(
            "US-HERB-004: delete unreferenced herb should succeed");

        // Verify - herb should not be found
        var getResp = await doctorClient.GetAsync($"/api/v1/herbs/{created.Id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task US_HERB_004_CheckReference_ReturnsReferenceStatus()
    {
        // Arrange - create a herb
        var doctorClient = await LoginAsDoctorAsync();
        var payload = HerbBuilder.Default().WithName("引用检查药材").Build();
        var createResp = await doctorClient.PostAsJsonAsync("/api/v1/herbs", payload);
        var created = await createResp.ShouldBeSuccessWithDataAsync<HerbDetailDto>();

        // Act - check references (should have none)
        var response = await doctorClient.GetAsync($"/api/v1/herbs/{created.Id}/check-reference");

        // Assert
        var data = await response.ShouldBeSuccessWithDataAsync<HerbReferenceCheckDto>(
            "US-HERB-004: reference check should return status");
        data.HerbId.Should().Be(created.Id);
        data.HasReferences.Should().BeFalse("new herb should have no references");
        data.ReferenceCount.Should().Be(0);
    }

    [Fact]
    public async Task US_HERB_004_DeleteHerb_NonexistentId_Returns404()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var fakeId = Guid.NewGuid();

        // Act
        var response = await doctorClient.DeleteAsync($"/api/v1/herbs/{fakeId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "US-HERB-004: deleting non-existent herb should return 404");
    }

    #endregion

    #region US-HERB-005: Import herbs (batch)

    [Fact]
    public async Task US_HERB_005_BatchImport_WithValidData_ReturnsImportResult()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var herbs = new[]
        {
            HerbBuilder.Default().WithName($"导入药材甲_{Guid.NewGuid():N}"[..12]).Build(),
            HerbBuilder.Default().WithName($"导入药材乙_{Guid.NewGuid():N}"[..12]).Build(),
            HerbBuilder.Default().WithName($"导入药材丙_{Guid.NewGuid():N}"[..12]).Build()
        };
        var payload = new { Herbs = herbs, Strategy = "Skip" };

        // Act
        var response = await doctorClient.PostAsJsonAsync("/api/v1/herbs/batch-import", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-HERB-005: batch import should succeed");
    }

    [Fact]
    public async Task US_HERB_005_BatchImport_EmptyList_Returns400()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var payload = new { Herbs = Array.Empty<object>(), Strategy = "Skip" };

        // Act
        var response = await doctorClient.PostAsJsonAsync("/api/v1/herbs/batch-import", payload);

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity },
            "US-HERB-005: empty import should be rejected");
    }

    #endregion
}
