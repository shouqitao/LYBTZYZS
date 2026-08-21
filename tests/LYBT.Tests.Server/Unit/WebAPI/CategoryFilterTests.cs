using System.Reflection;
using FluentAssertions;
using LYBT.Module.Catalog.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LYBT.Tests.Server;

/// <summary>
/// 分类筛选参数守卫（I-5 / P2：验方导出 category 对齐 + 仓储 category 直通）
/// </summary>
public class CategoryFilterTests
{
    [Fact]
    public void CatalogController_FormulaExport_HasCategoryFromQueryParam()
    {
        var type = typeof(LYBT.WebAPI.Controllers.FormulasController); // P1-24 拆分后
        var method = type.GetMethod("FormulaExport", BindingFlags.Public | BindingFlags.Instance);
        method.Should().NotBeNull();
        var param = method!.GetParameters().FirstOrDefault(p => p.Name == "category");
        param.Should().NotBeNull("服务端 FormulaExport 应为 [FromQuery] string? category（对齐客户端）");
        var fromQuery = param!.GetCustomAttribute<FromQueryAttribute>();
        fromQuery.Should().NotBeNull();
    }

    [Fact]
    public void ICatalogQueryService_HasCategoryOverload_AndExportDetails()
    {
        var iface = typeof(ICatalogQueryService<LYBT.Shared.Models.Contracts.Formula.FormulaListDto, LYBT.Shared.Models.Contracts.Formula.FormulaDetailDto>);
        // 6参重载：GetPagedAsync(page,pageSize,keyword,category,operatorId,isAdmin,ct)
        var pagedWithCategory = iface.GetMethods()
            .Where(m => m.Name == "GetPagedAsync")
            .Any(m => m.GetParameters().Any(p => p.Name == "category"));
        pagedWithCategory.Should().BeTrue("ICatalogQueryService 应有 category 重载（直通仓储）");

        var export = iface.GetMethod("ExportDetailsAsync");
        export.Should().NotBeNull("应有 ExportDetailsAsync（含 Herbs 明细）");
        export!.GetParameters().Any(p => p.Name == "category").Should().BeTrue("ExportDetailsAsync 应支持 category");
    }

    [Fact]
    public void FormulaRepository_GetPagedAsync_SupportsCategoryFilter()
    {
        // 反射检查 FormulaRepository.GetPagedAsync 签名含 category
        var type = typeof(LYBT.Module.Catalog.Infrastructure.FormulaRepository);
        var method = type.GetMethod("GetPagedAsync");
        method.Should().NotBeNull();
        method!.GetParameters().Any(p => p.Name == "category").Should().BeTrue("FormulaRepository.GetPagedAsync 应有 category 参数");
        // 同时检查 HerbRepository 亦支持 category（Herb 按 Category 过滤）
        var herbType = typeof(LYBT.Module.Catalog.Infrastructure.HerbRepository);
        var herbMethod = herbType.GetMethod("GetPagedAsync");
        herbMethod.Should().NotBeNull();
        herbMethod!.GetParameters().Any(p => p.Name == "category").Should().BeTrue("HerbRepository.GetPagedAsync 应有 category 参数");
    }
}
