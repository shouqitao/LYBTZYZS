using FluentAssertions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Tests.Desktop.EndToEnd.Infrastructure;
using Refit;
using System.Net;
using Xunit;
using Xunit.Abstractions;

namespace LYBT.Tests.Desktop.EndToEnd.Modules;

public class FormulaNegativeTests : WebApiE2ETestBase
{
    private readonly ITestOutputHelper _output;

    public FormulaNegativeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private async Task<Guid> CreateTestHerbAsync()
    {
        var herb = new HerbInputDto
        {
            Name = $"负测药材_{Guid.NewGuid():N}",
            PinYinCode = "FCCYC",
            Unit = "g",
            Price = 10m
        };
        var response = await HerbApi.CreateHerbAsync(herb);
        return response.Data!.Id;
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Negative")]
    public async Task CreateFormula_EmptyHerbsList_ShouldFail()
    {
        await LoginAsSysadminAsync();
        var input = new FormulaInputDto
        {
            Name = $"空药材方_{Guid.NewGuid():N}",
            Herbs = new List<FormulaHerbItemInputDto>()
        };

        try
        {
            var response = await FormulaApi.CreateFormulaAsync(input);
            response.Success.Should().BeFalse("没有药材组成的验方不应创建成功");
        }
        catch (ApiException ex)
        {
            ex.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Negative")]
    public async Task CreateFormula_EmptyName_ShouldFail()
    {
        await LoginAsSysadminAsync();
        var herbId = await CreateTestHerbAsync();
        var input = new FormulaInputDto
        {
            Name = "",
            Herbs = new List<FormulaHerbItemInputDto>
            {
                new() { HerbId = herbId, HerbName = "测试药材", Dosage = 10, Unit = "g" }
            }
        };

        try
        {
            var response = await FormulaApi.CreateFormulaAsync(input);
            response.Success.Should().BeFalse("空名称验方不应创建成功");
        }
        catch (ApiException ex)
        {
            ex.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Negative")]
    public async Task GetFormula_NonexistentId_ShouldFail()
    {
        await LoginAsSysadminAsync();
        var fakeId = Guid.NewGuid();

        try
        {
            var response = await FormulaApi.GetFormulaByIdAsync(fakeId);
            response.Success.Should().BeFalse("不存在的验方ID应返回失败");
        }
        catch (ApiException ex)
        {
            ex.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Negative")]
    public async Task CloneFormula_NonexistentId_ShouldFail()
    {
        await LoginAsSysadminAsync();
        var fakeId = Guid.NewGuid();

        try
        {
            var response = await FormulaApi.CloneFormulaAsync(fakeId);
            response.Success.Should().BeFalse("克隆不存在的验方应返回失败");
        }
        catch (ApiException ex)
        {
            ex.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
        }
    }
}
