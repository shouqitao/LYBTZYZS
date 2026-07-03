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
/// Batch operation and herb validation tests for Formulas module.
/// Covers: batch-delete, validate-herb, restore endpoints.
/// Collection: HerbFormula (isolated DB)
/// </summary>
[Collection("HerbFormula")]
public sealed class US_Formula_BatchAndValidateTests : IntegrationTestBase<HerbFormulaFixture>
{
    public US_Formula_BatchAndValidateTests(HerbFormulaFixture fixture) : base(fixture) { }

    #region Helpers

    private async Task<(Guid Id, string Name)> CreateHerbAsync(HttpClient client, string name = "测试药材")
    {
        var payload = HerbBuilder.Default().WithName($"{name}_{Guid.NewGuid():N}"[..12]).Build();
        var response = await client.PostAsJsonAsync("/api/v1/herbs", payload);
        var data = await response.ShouldBeSuccessWithDataAsync<HerbDetailDto>();
        return (data.Id, data.Name);
    }

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

    private async Task<FormulaDetailDto> CreateFormulaWithUnvalidatedHerbAsync(
        HttpClient client, string name, string unknownHerbName)
    {
        var input = new FormulaInputDto
        {
            Name = name,
            Effect = "测试延迟绑定",
            Usage = "水煎服",
            Herbs = new List<FormulaHerbItemInputDto>
            {
                new() { HerbName = unknownHerbName, Dosage = 5, Unit = "克" }
            }
        };
        var response = await client.PostAsJsonAsync("/api/v1/formulas", input);
        return await response.ShouldBeSuccessWithDataAsync<FormulaDetailDto>();
    }

    #endregion

    #region US-FORM-Batch: Batch Delete

    [Fact]
    public async Task BatchDeleteFormulas_DeletesMultiple()
    {
        var doctorClient = await LoginAsDoctorAsync();
        var herb = await CreateHerbAsync(doctorClient, "批量删除药材");

        var f1 = await CreateFormulaWithHerbsAsync(
            doctorClient, $"批量方1_{Guid.NewGuid():N}"[..12], (herb.Id, herb.Name, 10));
        var f2 = await CreateFormulaWithHerbsAsync(
            doctorClient, $"批量方2_{Guid.NewGuid():N}"[..12], (herb.Id, herb.Name, 12));
        var f3 = await CreateFormulaWithHerbsAsync(
            doctorClient, $"批量方3_{Guid.NewGuid():N}"[..12], (herb.Id, herb.Name, 15));

        var input = new BatchDeleteInputDto { Ids = new List<Guid> { f1.Id, f2.Id, f3.Id } };
        var response = await doctorClient.PostAsJsonAsync("/api/v1/formulas/batch-delete", input);

        var result = await response.ShouldBeSuccessWithDataAsync<BatchOperationResultDto>();
        result.SuccessCount.Should().Be(3, "all 3 formulas should be deleted");
        result.FailureCount.Should().Be(0);
        result.TotalCount.Should().Be(3);

        foreach (var id in new[] { f1.Id, f2.Id, f3.Id })
        {
            var getResp = await doctorClient.GetAsync($"/api/v1/formulas/{id}");
            getResp.StatusCode.Should().Be(HttpStatusCode.NotFound,
                "deleted formula should not be found");
        }
    }

    [Fact]
    public async Task BatchDeleteFormulas_SkipsNonExistent()
    {
        var doctorClient = await LoginAsDoctorAsync();
        var herb = await CreateHerbAsync(doctorClient, "跳过删除药材");

        var f1 = await CreateFormulaWithHerbsAsync(
            doctorClient, $"跳过方_{Guid.NewGuid():N}"[..12], (herb.Id, herb.Name, 10));
        var fakeId = Guid.NewGuid();

        var input = new BatchDeleteInputDto { Ids = new List<Guid> { f1.Id, fakeId } };
        var response = await doctorClient.PostAsJsonAsync("/api/v1/formulas/batch-delete", input);

        var result = await response.ShouldBeSuccessWithDataAsync<BatchOperationResultDto>();
        result.SuccessCount.Should().Be(1, "only existing formula should be deleted");
        result.FailureCount.Should().Be(1, "non-existent ID should be recorded as failure");
        result.FailedItems.Should().HaveCount(1);
        result.FailedItems[0].Id.Should().Be(fakeId);
    }

    #endregion

    #region US-FORM-Validate: Validate Formula Herb

    [Fact]
    public async Task ValidateFormulaHerb_BindsHerbToFormula()
    {
        var doctorClient = await LoginAsDoctorAsync();
        var realHerb = await CreateHerbAsync(doctorClient, "绑定药材");

        var formula = await CreateFormulaWithUnvalidatedHerbAsync(
            doctorClient, $"待验证方_{Guid.NewGuid():N}"[..12], "未知药材ABC");

        formula.Herbs.Should().NotBeEmpty("formula should have at least one herb item");
        var herbItem = formula.Herbs!.First();
        herbItem.HerbId.Should().BeNull("unvalidated herb should have null HerbId");

        var response = await doctorClient.PostAsJsonAsync(
            $"/api/v1/formulas/{formula.Id}/herbs/{herbItem.Id}/validate",
            new { SelectedHerbId = realHerb.Id });

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "validating a herb item should succeed");

        var getResp = await doctorClient.GetAsync($"/api/v1/formulas/{formula.Id}");
        var updated = await getResp.ShouldBeSuccessWithDataAsync<FormulaDetailDto>();
        var validatedItem = updated.Herbs!.First(h => h.Id == herbItem.Id);
        validatedItem.HerbId.Should().Be(realHerb.Id,
            "herb should now be bound to the real herb");
        validatedItem.IsValidated.Should().BeTrue("herb should be marked as validated");
    }

    [Fact]
    public async Task ValidateFormulaHerb_AutoPromotesToValidated()
    {
        var doctorClient = await LoginAsDoctorAsync();
        var realHerb = await CreateHerbAsync(doctorClient, "最终验证药材");

        var formula = await CreateFormulaWithUnvalidatedHerbAsync(
            doctorClient, $"最后验证方_{Guid.NewGuid():N}"[..12], "仅有一味未知药材");

        var herbItem = formula.Herbs!.First();

        var response = await doctorClient.PostAsJsonAsync(
            $"/api/v1/formulas/{formula.Id}/herbs/{herbItem.Id}/validate",
            new { SelectedHerbId = realHerb.Id });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResp = await doctorClient.GetAsync($"/api/v1/formulas/{formula.Id}");
        var updated = await getResp.ShouldBeSuccessWithDataAsync<FormulaDetailDto>();
        updated.ValidationStatus.Should().Be(FormulaValidationStatus.Validated,
            "validating the last herb should auto-promote formula to Validated");
    }

    #endregion

    #region US-FORM-Restore: Restore Formula

    [Fact]
    public async Task RestoreFormula_RestoresDeletedFormula()
    {
        var doctorClient = await LoginAsDoctorAsync();
        var herb = await CreateHerbAsync(doctorClient, "恢复药材");

        var formula = await CreateFormulaWithHerbsAsync(
            doctorClient, $"待恢复方_{Guid.NewGuid():N}"[..12], (herb.Id, herb.Name, 10));

        var deleteResp = await doctorClient.DeleteAsync($"/api/v1/formulas/{formula.Id}");
        await deleteResp.ShouldBeSuccessAsync();

        var getAfterDelete = await doctorClient.GetAsync($"/api/v1/formulas/{formula.Id}");
        getAfterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "deleted formula should not be found");

        var restoreResp = await doctorClient.PostAsJsonAsync(
            $"/api/v1/formulas/{formula.Id}/restore", (object?)null);

        restoreResp.StatusCode.Should().Be(HttpStatusCode.OK,
            "restoring a deleted formula should succeed");

        var getAfterRestore = await doctorClient.GetAsync($"/api/v1/formulas/{formula.Id}");
        var restored = await getAfterRestore.ShouldBeSuccessWithDataAsync<FormulaDetailDto>();
        restored.Id.Should().Be(formula.Id);
        restored.Name.Should().Be(formula.Name,
            "restored formula should have original name");
    }

    #endregion
}
