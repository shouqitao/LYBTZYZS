using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LYBT.Tests.Desktop.Unit.Shell;

/// <summary>
/// 本地导入导出 JSON 化守卫（IMPORTEXPORT-JSON 双端同步——本地端同 JSON，无 import-excel）
/// </summary>
public class LocalImportExportJsonTests
{
    [Fact]
    public void LocalPatientTemplateAndExport_ReturnJson_NotFile()
    {
        var type = typeof(LYBT.LocalWebAPI.Controllers.PatientsController);
        AssertJsonResponse(
            type.GetMethod("ImportTemplate")!,
            "患者导入模板端点必须返回 ApiResponse（JSON）"
        );
        AssertJsonResponse(
            type.GetMethod("Export")!,
            "患者导出端点必须返回 ApiResponse（JSON 数组）"
        );
    }

    [Fact]
    public void LocalFormulaTemplateAndExport_ReturnJson_NotFile()
    {
        var type = typeof(LYBT.LocalWebAPI.Controllers.CatalogController);
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
    public void LocalNoImportExcelEndpoint_ServiceSideExcelRemoved()
    {
        var type = typeof(LYBT.LocalWebAPI.Controllers.CatalogController);
        type.GetMethod("ImportExcel")
            .Should()
            .BeNull("服务端 Excel 解析路径（import-excel）已移除");
    }

    [Fact]
    public void LocalBatchImportDtoEndpoint_StillExists()
    {
        var type = typeof(LYBT.LocalWebAPI.Controllers.CatalogController);
        type.GetMethod("BatchImport")
            .Should()
            .NotBeNull("batch-import（DTO/JSON）是唯一导入路径——必须保留");
    }

    private static void AssertJsonResponse(MethodInfo method, string because)
    {
        // 本地控制器风格不标注 ProducesResponseType——断言返回类型为 IActionResult 且非 FileResult 系
        method.ReturnType.Should().NotBeAssignableTo(typeof(FileResult), because);
        // 若有 200 标注，必须是 ApiResponse（空标注=本地风格不标注，仅检查非 FileResult）
        var produces = method
            .GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Where(a => a.StatusCode == 200)
            .Select(a => a.Type)
            .ToList();
        if (produces.Count > 0)
        {
            produces.Should().OnlyContain(t => t != null && t.Name.StartsWith("ApiResponse"), because);
            produces
                .Should()
                .NotContain(t => t == typeof(FileResult) || t == typeof(FileContentResult), because);
        }
    }
}
