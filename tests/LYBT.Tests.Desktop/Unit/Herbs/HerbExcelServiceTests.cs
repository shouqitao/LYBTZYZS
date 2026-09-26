using System;
using System.IO;
using System.Text;
using ClosedXML.Excel;
using FluentAssertions;
using LYBT.Desktop.Catalog.Services;
using LYBT.Shared.Models.Enums;
using Xunit;

namespace LYBT.Tests.Desktop.Unit;

/// <summary>
/// US-SHELL-021 AC① 药材 Excel 导入导出转换测试（真实 .xlsx 字节——验证模板/导入/导出契约与行级校验）。
/// </summary>
public class HerbExcelServiceTests
{
    private readonly HerbExcelService _service = new();

    /// <summary>服务端 import-template 响应（ApiResponse 信封，字段说明为 SSOT）</summary>
    private const string TemplateJson = """
        {
          "success": true,
          "message": "药材导入模板（JSON）",
          "data": {
            "description": "药材批量导入 JSON 模板",
            "fields": [
              { "field": "Name", "required": true, "description": "药材名称" },
              { "field": "PinYinCode", "required": false, "description": "拼音码" },
              { "field": "Category", "required": false, "description": "分类" },
              { "field": "Properties", "required": false, "description": "性味" },
              { "field": "Origin", "required": false, "description": "产地" },
              { "field": "Spec", "required": false, "description": "规格" },
              { "field": "Unit", "required": false, "description": "单位（默认 克）" },
              { "field": "Price", "required": false, "description": "单价" },
              { "field": "CostPrice", "required": false, "description": "成本价" }
            ],
            "example": [
              { "name": "人参", "pinYinCode": "renshen", "category": "补益药", "properties": "甘微苦温", "origin": "吉林", "spec": "一等", "unit": "克", "price": 10.5, "costPrice": 5.0 }
            ]
          }
        }
        """;

    /// <summary>服务端 export 响应（ApiResponse 信封，HerbListDto camelCase）</summary>
    private const string ExportJson = """
        {
          "success": true,
          "message": "药材导出（JSON）",
          "data": [
            { "id": "11111111-1111-1111-1111-111111111111", "name": "人参", "pinYinCode": "renshen", "category": "补益药", "origin": "吉林", "spec": "一等", "unit": "克", "price": 10.5, "status": "Enabled", "createdAt": "2026-09-01T08:30:00" },
            { "id": "22222222-2222-2222-2222-222222222222", "name": "甘草", "pinYinCode": null, "category": null, "origin": null, "spec": null, "unit": "克", "price": 3.5, "status": "Disabled", "createdAt": "2026-09-02T09:00:00" }
          ]
        }
        """;

    private static byte[] Utf8(string json) => Encoding.UTF8.GetBytes(json);

    [Fact]
    public void GenerateTemplate_WritesStableHeaders_AndServerFieldDescriptionsInGuideSheet()
    {
        var bytes = _service.GenerateTemplate(Utf8(TemplateJson));

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var data = workbook.Worksheet(HerbExcelService.DataSheetName);
        var guide = workbook.Worksheet(HerbExcelService.GuideSheetName);

        // 数据表：仅表头（示例值不落数据表——避免用户忘删示例行被当数据导入）
        data.Cell(1, 1).GetString().Should().Be("药材名称（必填）");
        data.Cell(1, 2).GetString().Should().Be("拼音码");
        data.Cell(1, 8).GetString().Should().Be("单价");
        data.Cell(1, 9).GetString().Should().Be("成本价");
        data.LastRowUsed()!.RowNumber().Should().Be(1);

        // 说明表：字段说明取自服务端 JSON（SSOT）+ 示例值
        guide.Cell(2, 1).GetString().Should().Be("药材名称");
        guide.Cell(2, 2).GetString().Should().Be("是");
        guide.Cell(2, 3).GetString().Should().Be("药材名称");
        guide.Cell(2, 4).GetString().Should().Be("人参");
        guide.Cell(3, 2).GetString().Should().Be("否", "拼音码非必填");
        guide.Cell(8, 3).GetString().Should().Be("单位（默认 克）");
    }

    [Fact]
    public void ParseImportFile_GeneratedTemplate_ReturnsNoHerbs()
    {
        var templateBytes = _service.GenerateTemplate(Utf8(TemplateJson));

        var request = _service.ParseImportFile(new MemoryStream(templateBytes));

        request.Herbs.Should().BeEmpty("模板仅含表头——示例值在说明表，不应被当数据导入");
        request.Strategy.Should().Be(DuplicateStrategy.Skip, "策略默认跳过（与批导入 DTO 默认一致）");
    }

    [Fact]
    public void ParseImportFile_MapsColumnsAndDefaults()
    {
        using var source = BuildSheet(
            "药材数据",
            new[] { "药材名称（必填）", "拼音码", "分类", "性味", "产地", "规格", "单位", "单价", "成本价" },
            new object?[] { "人参", "renshen", "补益药", "甘微苦温", "吉林", "一等", "克", 10.5m, 5.0m },
            new object?[] { "甘草", "gancao", null, null, null, null, null, "3.50", null },
            null, // 第 4 行全空——应跳过
            new object?[] { "当归", null, null, null, null, null, null, "2", null });

        var request = _service.ParseImportFile(source);

        request.Herbs.Should().HaveCount(3);
        request.Herbs[0].Name.Should().Be("人参");
        request.Herbs[0].PinYinCode.Should().Be("renshen");
        request.Herbs[0].Category.Should().Be("补益药");
        request.Herbs[0].Properties.Should().Be("甘微苦温");
        request.Herbs[0].Origin.Should().Be("吉林");
        request.Herbs[0].Spec.Should().Be("一等");
        request.Herbs[0].Unit.Should().Be("克");
        request.Herbs[0].Price.Should().Be(10.5m);
        request.Herbs[0].CostPrice.Should().Be(5.0m);

        request.Herbs[1].Name.Should().Be("甘草");
        request.Herbs[1].Category.Should().BeNull();
        request.Herbs[1].Unit.Should().Be("克", "空单位保留 DTO 默认「克」");
        request.Herbs[1].Price.Should().Be(3.50m);
        request.Herbs[1].CostPrice.Should().BeNull();

        request.Herbs[2].Name.Should().Be("当归");
        request.Herbs[2].PinYinCode.Should().BeNull();
        request.Herbs[2].Price.Should().Be(2m);
    }

    [Fact]
    public void ParseImportFile_InvalidPrice_ThrowsWithRowNumberAndColumn()
    {
        using var source = BuildSheet(
            "药材数据",
            new[] { "药材名称（必填）", "单价", "成本价" },
            new object?[] { "人参", "abc", null });

        var act = () => _service.ParseImportFile(source);

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*第 2 行*单价*格式无效*");
    }

    [Fact]
    public void ParseImportFile_UnknownHeadersOnly_Throws()
    {
        using var source = BuildSheet("药材数据", new[] { "备注", "联系电话" }, new object?[] { "x", "y" });

        var act = () => _service.ParseImportFile(source);

        act.Should().Throw<InvalidDataException>().WithMessage("*未识别到药材字段列*");
    }

    [Fact]
    public void ParseImportFile_NotAnExcelFile_Throws()
    {
        using var source = new MemoryStream(Encoding.UTF8.GetBytes("not an xlsx"));

        var act = () => _service.ParseImportFile(source);

        act.Should().Throw<InvalidDataException>().WithMessage("*无法读取 Excel 文件*");
    }

    [Fact]
    public void GenerateExportFile_WritesRowsWithLabelsAndValues()
    {
        var bytes = _service.GenerateExportFile(Utf8(ExportJson));

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var sheet = workbook.Worksheet(HerbExcelService.DataSheetName);

        sheet.Cell(1, 1).GetString().Should().Be("药材ID");
        sheet.Cell(1, 8).GetString().Should().Be("单价");
        sheet.Cell(1, 9).GetString().Should().Be("状态");
        sheet.Cell(1, 10).GetString().Should().Be("创建时间");

        sheet.Cell(2, 2).GetString().Should().Be("人参");
        sheet.Cell(2, 7).GetString().Should().Be("克");
        sheet.Cell(2, 8).GetValue<decimal>().Should().Be(10.5m);
        sheet.Cell(2, 9).GetString().Should().Be("启用");
        sheet.Cell(2, 10).GetDateTime().Should().Be(new DateTime(2026, 9, 1, 8, 30, 0));

        sheet.Cell(3, 9).GetString().Should().Be("禁用");
        sheet.Cell(3, 3).IsEmpty().Should().BeTrue("拼音码为 null 时留空");
        sheet.LastRowUsed()!.RowNumber().Should().Be(3);
    }

    [Fact]
    public void GenerateExportFile_ResponseWithoutData_Throws()
    {
        var act = () => _service.GenerateExportFile(Utf8("""{ "success": false, "message": "导出失败" }"""));

        act.Should().Throw<InvalidDataException>().WithMessage("*缺少 data*");
    }

    /// <summary>构造数据表（首行表头；null 行跳过——用于全空行场景）</summary>
    private static MemoryStream BuildSheet(string sheetName, string[] headers, params object?[]?[] rows)
    {
        var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.Worksheets.Add(sheetName);
            for (var i = 0; i < headers.Length; i++)
                sheet.Cell(1, i + 1).Value = headers[i];

            for (var r = 0; r < rows.Length; r++)
            {
                var values = rows[r];
                if (values == null)
                    continue;

                for (var c = 0; c < values.Length; c++)
                {
                    switch (values[c])
                    {
                        case null:
                            break;
                        case string text:
                            sheet.Cell(r + 2, c + 1).Value = text;
                            break;
                        case decimal number:
                            sheet.Cell(r + 2, c + 1).Value = number;
                            break;
                        case int integer:
                            sheet.Cell(r + 2, c + 1).Value = integer;
                            break;
                        default:
                            sheet.Cell(r + 2, c + 1).Value = values[c]!.ToString();
                            break;
                    }
                }
            }

            workbook.SaveAs(stream);
        }

        stream.Position = 0;
        return stream;
    }
}
