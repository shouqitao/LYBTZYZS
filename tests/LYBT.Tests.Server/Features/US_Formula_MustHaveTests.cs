using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Server.Infrastructure;
using LYBT.Tests.Server.Infrastructure.TestDataBuilders;
using Xunit;

namespace LYBT.Tests.Server.Features.Formulas;

/// <summary>
/// Must Have User Stories for Formulas module.
/// PRD: US-FORM-001 ~ US-FORM-006 (6 Must Have)
/// Collection: HerbFormula (isolated DB, parallel with other domains)
/// </summary>
[Collection("HerbFormula")]
public sealed class US_Formula_MustHaveTests : IntegrationTestBase<HerbFormulaFixture>
{
    public US_Formula_MustHaveTests(HerbFormulaFixture fixture) : base(fixture) { }

    #region Helpers

    /// <summary>Create a herb and return its ID and name.</summary>
    private async Task<(Guid Id, string Name)> CreateHerbAsync(HttpClient client, string name = "测试药材")
    {
        var payload = HerbBuilder.Default().WithName($"{name}_{Guid.NewGuid():N}"[..12]).Build();
        var response = await client.PostAsJsonAsync("/api/v1/herbs", payload);
        var data = await response.ShouldBeSuccessWithDataAsync<HerbDetailDto>();
        return (data.Id, data.Name);
    }

    /// <summary>Create a formula with herbs using typed DTOs (matching Journey test pattern).</summary>
    private async Task<FormulaDetailDto> CreateFormulaWithHerbsAsync(
        HttpClient client, string name, params (Guid Id, string Name, int Dosage)[] herbs)
    {
        var input = new FormulaInputDto
        {
            Name = name,
            Effect = "测试功效",
            Usage = "水煎服",
            Herbs = herbs.Select(h => new FormulaHerbItemInputDto
            {
                HerbId = h.Id,
                HerbName = h.Name,
                Dosage = h.Dosage,
                Unit = "克"
            }).ToList()
        };
        var response = await client.PostAsJsonAsync("/api/v1/formulas", input);
        return await response.ShouldBeSuccessWithDataAsync<FormulaDetailDto>();
    }

    #endregion

    #region US-FORM-001: Create formula with herb items

    [Fact]
    public async Task US_FORM_001_CreateFormula_WithHerbs_ReturnsCreatedFormula()
    {
        // Arrange - create herbs first
        var doctorClient = await LoginAsDoctorAsync();
        var herb1 = await CreateHerbAsync(doctorClient, "黄芪");
        var herb2 = await CreateHerbAsync(doctorClient, "当归");

        // Act - create formula with typed DTO (matching Journey test pattern)
        var input = new FormulaInputDto
        {
            Name = $"补气养血方_{Guid.NewGuid():N}"[..12],
            Effect = "补气养血",
            Usage = "水煎服",
            Herbs = new List<FormulaHerbItemInputDto>
            {
                new() { HerbId = herb1.Id, HerbName = herb1.Name, Dosage = 15, Unit = "克" },
                new() { HerbId = herb2.Id, HerbName = herb2.Name, Dosage = 10, Unit = "克" }
            }
        };
        var response = await doctorClient.PostAsJsonAsync("/api/v1/formulas", input);

        // Assert - formula created
        var data = await response.ShouldBeSuccessWithDataAsync<FormulaDetailDto>(
            "US-FORM-001: doctor should create formula with herbs");
        data.Id.Should().NotBeEmpty();
        data.Status.Should().Be(CommonStatus.Enabled);

        // Verify herbs via separate GET
        var getResp = await doctorClient.GetAsync($"/api/v1/formulas/{data.Id}");
        var fetched = await getResp.ShouldBeSuccessWithDataAsync<FormulaDetailDto>();
        fetched.Id.Should().Be(data.Id);
        fetched.HerbCount.Should().Be(2,
            "US-FORM-001: formula should have 2 herb items");
    }

    [Fact]
    public async Task US_FORM_001_CreateFormula_WithoutName_Returns400()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var input = new FormulaInputDto { Name = "", Effect = "测试" };

        // Act
        var response = await doctorClient.PostAsJsonAsync("/api/v1/formulas", input);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "US-FORM-001: empty name should fail validation");
    }

    #endregion

    #region US-FORM-002: Update formula

    [Fact]
    public async Task US_FORM_002_UpdateFormula_ModifiesFields()
    {
        // Arrange - create formula first
        var doctorClient = await LoginAsDoctorAsync();
        var herb = await CreateHerbAsync(doctorClient, "丹参");
        var created = await CreateFormulaWithHerbsAsync(
            doctorClient, $"待更新验方_{Guid.NewGuid():N}"[..12],
            (herb.Id, herb.Name, 10));

        // Act - update with new data
        var herb2 = await CreateHerbAsync(doctorClient, "川芎");
        var updateInput = new FormulaInputDto
        {
            Name = $"已更新验方_{Guid.NewGuid():N}"[..12],
            Effect = "活血化瘀",
            Usage = "日一剂，水煎服",
            Herbs = new List<FormulaHerbItemInputDto>
            {
                new() { HerbId = herb.Id, HerbName = herb.Name, Dosage = 15, Unit = "克" },
                new() { HerbId = herb2.Id, HerbName = herb2.Name, Dosage = 10, Unit = "克" }
            }
        };
        var response = await doctorClient.PutAsJsonAsync($"/api/v1/formulas/{created.Id}", updateInput);

        // Assert
        var data = await response.ShouldBeSuccessWithDataAsync<FormulaDetailDto>(
            "US-FORM-002: update should return modified formula");
        data.Effect.Should().Be("活血化瘀");
        data.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task US_FORM_002_UpdateFormula_NonexistentId_ReturnsError()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var fakeId = Guid.NewGuid();
        var input = new FormulaInputDto
        {
            Name = "不存在的验方",
            Effect = "测试",
            Usage = "水煎服"
        };

        // Act
        var response = await doctorClient.PutAsJsonAsync($"/api/v1/formulas/{fakeId}", input);

        // Assert - may return 400 (validation) or 404 (not found)
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.BadRequest, HttpStatusCode.NotFound },
            "US-FORM-002: update non-existent formula should fail");
    }

    #endregion

    #region US-FORM-003: List formulas (ownership filtered)

    [Fact]
    public async Task US_FORM_003_ListFormulas_ReturnsPaginatedResult()
    {
        // Arrange - create formulas
        var doctorClient = await LoginAsDoctorAsync();
        var herb = await CreateHerbAsync(doctorClient, "白术");
        await CreateFormulaWithHerbsAsync(
            doctorClient, $"列表验方甲_{Guid.NewGuid():N}"[..12],
            (herb.Id, herb.Name, 9));
        await CreateFormulaWithHerbsAsync(
            doctorClient, $"列表验方乙_{Guid.NewGuid():N}"[..12],
            (herb.Id, herb.Name, 12));

        // Act
        var response = await doctorClient.GetAsync("/api/v1/formulas?page=1&pageSize=10");

        // Assert
        var paged = await response.ShouldBePagedResultAsync<FormulaListDto>(
            expectedMinCount: 2,
            because: "US-FORM-003: should return at least 2 created formulas");
    }

    [Fact]
    public async Task US_FORM_003_ListFormulas_WithKeywordFilter()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var herb = await CreateHerbAsync(doctorClient, "枸杞");
        var uniquePrefix = $"关键字_{Guid.NewGuid():N}"[..8];
        await CreateFormulaWithHerbsAsync(
            doctorClient, $"{uniquePrefix}验方",
            (herb.Id, herb.Name, 15));

        // Act
        var response = await doctorClient.GetAsync(
            $"/api/v1/formulas?keyword={Uri.EscapeDataString(uniquePrefix)}&page=1&pageSize=10");

        // Assert
        var paged = await response.ShouldBePagedResultAsync<FormulaListDto>(
            expectedMinCount: 1,
            because: "US-FORM-003: keyword search should find formula");
    }

    #endregion

    #region US-FORM-004: Delete formula

    [Fact]
    public async Task US_FORM_004_DeleteFormula_Succeeds()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var herb = await CreateHerbAsync(doctorClient, "甘草");
        var created = await CreateFormulaWithHerbsAsync(
            doctorClient, $"待删除验方_{Guid.NewGuid():N}"[..12],
            (herb.Id, herb.Name, 6));

        // Act
        var response = await doctorClient.DeleteAsync($"/api/v1/formulas/{created.Id}");

        // Assert
        await response.ShouldBeSuccessAsync(
            "US-FORM-004: delete formula should succeed");

        // Verify - formula should not be found
        var getResp = await doctorClient.GetAsync($"/api/v1/formulas/{created.Id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task US_FORM_004_DeleteFormula_NonexistentId_Returns404()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var fakeId = Guid.NewGuid();

        // Act
        var response = await doctorClient.DeleteAsync($"/api/v1/formulas/{fakeId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "US-FORM-004: deleting non-existent formula should return 404");
    }

    #endregion

    #region US-FORM-005: Share formula

    [Fact]
    public async Task US_FORM_005_CreateSharedFormula_IsVisibleToOthers()
    {
        // Arrange - doctor creates a shared formula
        var doctorClient = await LoginAsDoctorAsync();
        var herb = await CreateHerbAsync(doctorClient, "金银花");
        var input = new FormulaInputDto
        {
            Name = $"共享验方_{Guid.NewGuid():N}"[..10],
            Effect = "清热解毒",
            Usage = "水煎服",
            IsShared = true,
            Herbs = new List<FormulaHerbItemInputDto>
            {
                new() { HerbId = herb.Id, HerbName = herb.Name, Dosage = 15, Unit = "克" }
            }
        };
        var response = await doctorClient.PostAsJsonAsync("/api/v1/formulas", input);
        var created = await response.ShouldBeSuccessWithDataAsync<FormulaDetailDto>();

        // Assert
        created.IsShared.Should().BeTrue(
            "US-FORM-005: shared formula should be marked as shared");

        // Verify - admin can also see it
        var adminClient = await LoginAsAdminAsync();
        var getResp = await adminClient.GetAsync($"/api/v1/formulas/{created.Id}");
        var data = await getResp.ShouldBeSuccessWithDataAsync<FormulaDetailDto>();
        data.IsShared.Should().BeTrue();
    }

    #endregion

    #region US-FORM-006: Validate formula herbs

    [Fact]
    public async Task US_FORM_006_FormulaWithMatchedHerbs_HerbsAreValidated()
    {
        // Arrange - create herb in library, then formula referencing it
        var doctorClient = await LoginAsDoctorAsync();
        var herb = await CreateHerbAsync(doctorClient, "黄连");

        var created = await CreateFormulaWithHerbsAsync(
            doctorClient, $"含已验证药材_{Guid.NewGuid():N}"[..12],
            (herb.Id, herb.Name, 10));

        // Assert
        created.Herbs.Should().NotBeEmpty(
            "US-FORM-006: formula with known herbs should have them");
        created.Herbs!.First().HerbId.Should().Be(herb.Id);
    }

    [Fact]
    public async Task US_FORM_006_FormulaWithUnknownHerb_CreatesUnvalidatedItem()
    {
        // Arrange - formula with herb name not in library (deferred binding)
        var doctorClient = await LoginAsDoctorAsync();
        var input = new FormulaInputDto
        {
            Name = $"含未知药材_{Guid.NewGuid():N}"[..12],
            Effect = "测试延迟绑定",
            Usage = "水煎服",
            Herbs = new List<FormulaHerbItemInputDto>
            {
                new() { HerbName = "不存在的稀有药材XYZ", Dosage = 5, Unit = "克" }
            }
        };

        // Act
        var response = await doctorClient.PostAsJsonAsync("/api/v1/formulas", input);

        // Assert - should still create (deferred binding pattern)
        var data = await response.ShouldBeSuccessWithDataAsync<FormulaDetailDto>(
            "US-FORM-006: formula with unknown herb should still be created");
        data.Herbs.Should().NotBeEmpty();
    }

    #endregion
}
