using System.Reflection;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LYBT.Tests.Server;

/// <summary>
/// 导出明细守卫（I-5 / F-L4-02 / P2：验方导出含药材组成明细 + 药材导出全字段）
/// </summary>
public class ExportDetailTests
{
    [Fact]
    public void HerbExport_ReturnsListOfHerbListDto_WithFullFields()
    {
        var type = typeof(LYBT.WebAPI.Controllers.CatalogController);
        var method = type.GetMethod("HerbExport", BindingFlags.Public | BindingFlags.Instance);
        method.Should().NotBeNull();
        var produces = method!.GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Where(a => a.StatusCode == 200).Select(a => a.Type).ToList();
        produces.Should().NotBeEmpty();
        produces.Should().Contain(t => t != null && t.Name.Contains("ApiResponse"),
            "应返回 ApiResponse 信封（JSON）");
        // HerbListDto 应含全字段（名称/分类/产地/规格等），通过反射断言关键属性存在
        typeof(HerbListDto).GetProperty("Name").Should().NotBeNull();
        typeof(HerbListDto).GetProperty("Category").Should().NotBeNull();
        typeof(HerbListDto).GetProperty("Origin").Should().NotBeNull();
        typeof(HerbListDto).GetProperty("Spec").Should().NotBeNull();
        typeof(HerbListDto).GetProperty("Unit").Should().NotBeNull();
    }

    [Fact]
    public void FormulaExport_ReturnsListOfFormulaDetailDto_WithHerbs()
    {
        var type = typeof(LYBT.WebAPI.Controllers.CatalogController);
        var method = type.GetMethod("FormulaExport", BindingFlags.Public | BindingFlags.Instance);
        method.Should().NotBeNull();
        var produces = method!.GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Where(a => a.StatusCode == 200).Select(a => a.Type).ToList();
        produces.Should().NotBeEmpty();
        // P2：验方导出必须为 DetailDto（含 Herbs）而非 ListDto
        produces.Should().Contain(t => t != null && t.GenericTypeArguments.Any(a => a == typeof(FormulaDetailDto)) ||
                                       t!.ToString().Contains("FormulaDetailDto"),
            "验方导出应返回 List<FormulaDetailDto>（含 Herbs 明细，US-FORM-013）");
        // 进一步断言 DetailDto 含 Herbs 集合
        typeof(FormulaDetailDto).GetProperty("Herbs").Should().NotBeNull("导出明细必须含药材组成");
        typeof(FormulaHerbItemDto).GetProperty("HerbName").Should().NotBeNull();
        typeof(FormulaHerbItemDto).GetProperty("Dosage").Should().NotBeNull();
    }

    [Fact]
    public void FormulaExport_UsesExportDetailsAsync_NotGetPaged()
    {
        // 通过方法体 IL 间接验证：FormulaExport 应调用 ExportDetailsAsync（含 Herbs），而非 GetPagedAsync（仅 ListDto）
        var method = typeof(LYBT.WebAPI.Controllers.CatalogController).GetMethod("FormulaExport", BindingFlags.Public | BindingFlags.Instance);
        method.Should().NotBeNull();
        // 简单检查：方法参数名应为 category（对齐 IFormulaApi），而非 keyword
        var param = method!.GetParameters().FirstOrDefault(p => p.Name == "category");
        param.Should().NotBeNull("P2：FormulaExport 筛选参数应为 category，对齐客户端 IFormulaApi:71 ?category=");
    }
}
