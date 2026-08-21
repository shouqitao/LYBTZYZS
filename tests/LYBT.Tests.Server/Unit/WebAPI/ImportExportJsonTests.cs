using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LYBT.Tests.Server;

/// <summary>
/// 导入导出 JSON 化守卫（IMPORTEXPORT-JSON: 2026-08-13 用户决策——后端不涉及 Excel 格式，
/// 患者/药材/验方导入模板与导出改 JSON——防回归到 File/Excel 响应）
/// </summary>
public class ImportExportJsonTests
{
    [Theory]
    [InlineData(typeof(LYBT.WebAPI.Controllers.PatientsController))]
    public void PatientExportTemplate_ReturnsJson_NotFile(Type controllerType)
    {
        AssertJsonResponse(
            controllerType.GetMethod("ImportTemplate")!,
            "患者导入模板端点必须返回 ApiResponse（JSON），不得返回 File/Excel"
        );
    }

    [Theory]
    [InlineData(typeof(LYBT.WebAPI.Controllers.PatientsController))]
    public void PatientExport_ReturnsJson_NotFile(Type controllerType)
    {
        AssertJsonResponse(
            controllerType.GetMethod("Export")!,
            "患者导出端点必须返回 ApiResponse（JSON 数组），不得返回 File/Excel"
        );
    }

    [Fact]
    public void HerbTemplateAndExport_ReturnJson_NotFile()
    {
        var type = typeof(LYBT.WebAPI.Controllers.HerbsController); // P1-24 拆分后
        AssertJsonResponse(
            type.GetMethod("HerbImportTemplate")!,
            "药材导入模板端点必须返回 ApiResponse（JSON）"
        );
        AssertJsonResponse(
            type.GetMethod("HerbExportAll")!,
            "药材导出端点必须返回 ApiResponse（JSON 数组）"
        );
        AssertJsonResponse(
            type.GetMethod("HerbExport")!,
            "药材筛选导出端点（US-HERB-013，Desktop 契约 GET /herbs/export）必须返回 ApiResponse（JSON 数组）"
        );
    }

    [Fact]
    public void FormulaTemplateAndExport_ReturnJson_NotFile()
    {
        var type = typeof(LYBT.WebAPI.Controllers.FormulasController); // P1-24 拆分后
        AssertJsonResponse(
            type.GetMethod("FormulaImportTemplate")!,
            "验方导入模板端点必须返回 ApiResponse（JSON）"
        );
        AssertJsonResponse(
            type.GetMethod("FormulaExport")!,
            "验方导出端点必须返回 ApiResponse（JSON 数组）"
        );
    }

    [Fact]
    public void NoImportExcelEndpoint_ServiceSideExcelRemoved()
    {
        // P1-24 拆分后：双控制器均无 ImportExcel
        typeof(LYBT.WebAPI.Controllers.HerbsController).GetMethod("ImportExcel")
            .Should().BeNull("服务端 Excel 解析路径（import-excel）已移除（Herbs）");
        typeof(LYBT.WebAPI.Controllers.FormulasController).GetMethod("ImportExcel")
            .Should().BeNull("服务端 Excel 解析路径（import-excel）已移除（Formulas）");
    }

    [Fact]
    public void BatchImportDtoEndpoint_StillExists()
    {
        // P1-24 拆分后：HerbsController.BatchImport + FormulasController.BatchImportFormulas
        typeof(LYBT.WebAPI.Controllers.HerbsController).GetMethod("BatchImport")
            .Should().NotBeNull("batch-import（DTO/JSON）是药材唯一导入路径——必须保留");
        typeof(LYBT.WebAPI.Controllers.FormulasController).GetMethod("ImportFormulas")
            .Should().NotBeNull("batch-import（DTO/JSON）是验方唯一导入路径——必须保留");
    }

    private static void AssertJsonResponse(MethodInfo method, string because)
    {
        var produces = method
            .GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Where(a => a.StatusCode == 200)
            .Select(a => a.Type)
            .ToList();
        produces.Should().NotBeEmpty($"{method.Name} 应有 200 响应类型标注");
        produces.Should().OnlyContain(t => t != null && t.Name.StartsWith("ApiResponse"), because);
        method.ReturnType.Should().NotBeAssignableTo(typeof(FileResult), because);
        produces
            .Should()
            .NotContain(t => t == typeof(FileResult) || t == typeof(FileContentResult), because);
    }
}
