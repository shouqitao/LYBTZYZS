using System;
using System.IO;
using System.Text;
using ClosedXML.Excel;
using FluentAssertions;
using LYBT.Desktop.Catalog.Services;
using Xunit;

namespace LYBT.Tests.Desktop.Unit;

/// <summary>
/// US-SHELL-021 AC① 验方 Excel 导入导出转换测试（真实 .xlsx 字节——验证模板/导入/导出契约、药材明细列与行级校验）。
/// </summary>
public class FormulaExcelServiceTests
{
    private readonly FormulaExcelService _service = new();

    /// <summary>服务端 import-template 响应（ApiResponse 信封，字段说明为 SSOT）</summary>
    private const string TemplateJson = """
        {
          "success": true,
          "message": "验方导入模板（JSON）",
          "data": {
            "description": "验方批量导入 JSON 模板",
            "fields": [
              { "field": "Name", "required": true, "description": "验方名称" },
              { "field": "Category", "required": false, "description": "分类" },
              { "field": "Effect", "required": false, "description": "功效" },
              { "field": "Usage", "required": false, "description": "用法" },
              { "field": "Herbs", "required": true, "description": "药材组成（对象数组 [{HerbName, Dosage, Unit}]）" }
            ],
            "example": [
              { "name": "四君子汤", "category": "补益剂", "effect": "益气健脾", "usage": "水煎服", "herbs": [ { "herbName": "人参", "dosage": 10, "unit": "g" } ] }
            ]
          }
        }
        """;

    private const string HerbsDescription = "药材组成（对象数组 [{HerbName, Dosage, Unit}]）";

    /// <summary>服务端 export 响应（ApiResponse 信封，FormulaDetailDto camelCase，含药材明细）</summary>
    private const string ExportJson = """
        {
          "success": true,
          "message": "验方导出（JSON）",
          "data": [
            {
              "id": "11111111-1111-1111-1111-111111111111",
              "name": "四君子汤",
              "category": "补益剂",
              "effect": "益气健脾",
              "usage": "水煎服",
              "property": "甘温",
              "indication": "脾胃气虚",
              "herbCount": 3,
              "totalPrice": 15.50,
              "status": "Enabled",
              "validationStatus": "Validated",
              "createdAt": "2026-09-01T08:30:00",
              "herbs": [
                { "herbName": "人参", "dosage": 10, "unit": "g" },
                { "herbName": "白术", "dosage": 10, "unit": "g" },
                { "herbName": "茯苓", "dosage": 10, "unit": "g" }
              ]
            },
            {
              "id": "22222222-2222-2222-2222-222222222222",
              "name": "桂枝汤",
              "category": "解表剂",
              "herbCount": 1,
              "totalPrice": 5.5,
              "status": "Disabled",
              "validationStatus": "Draft",
              "createdAt": "2026-09-02T09:00:00",
              "herbs": [ { "herbName": "桂枝", "dosage": 9, "unit": "克" } ]
            }
          ]
        }
        """;

    private static byte[] Utf8(string json) => Encoding.UTF8.GetBytes(json);

    [Fact]
    public void GenerateTemplate_WritesBaseAndHerbDetailHeaders_AndGuideSheet()
    {
        var bytes = _service.GenerateTemplate(Utf8(TemplateJson));

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var data = workbook.Worksheet(FormulaExcelService.DataSheetName);
        var guide = workbook.Worksheet(FormulaExcelService.GuideSheetName);

        // 数据表：基础列 + 药材明细列组（药材1..药材10 × 名称/剂量/单位），仅表头
        data.Cell(1, 1).GetString().Should().Be("验方名称（必填）");
        data.Cell(1, 2).GetString().Should().Be("分类");
        data.Cell(1, 3).GetString().Should().Be("功效");
        data.Cell(1, 4).GetString().Should().Be("用法");
        data.Cell(1, 5).GetString().Should().Be("药材1名称（必填）");
        data.Cell(1, 6).GetString().Should().Be("药材1剂量");
        data.Cell(1, 7).GetString().Should().Be("药材1单位");
        data.Cell(1, 8).GetString().Should().Be("药材2名称");
        data.Cell(1, 34).GetString().Should().Be("药材10单位");
        data.LastColumnUsed()!.ColumnNumber().Should().Be(34);
        data.LastRowUsed()!.RowNumber().Should().Be(1);

        // 说明表：基础字段说明取自服务端 JSON；药材明细列逐列说明（SSOT 描述落在第 1 味药材名称行）
        guide.Cell(2, 1).GetString().Should().Be("验方名称");
        guide.Cell(2, 2).GetString().Should().Be("是");
        guide.Cell(2, 3).GetString().Should().Be("验方名称");
        guide.Cell(5, 2).GetString().Should().Be("否");
        guide.Cell(6, 1).GetString().Should().Be("药材1名称");
        guide.Cell(6, 2).GetString().Should().Be("是");
        guide.Cell(6, 3).GetString().Should().Be(HerbsDescription);
        guide.Cell(7, 1).GetString().Should().Be("药材1剂量");
        guide.Cell(8, 4).GetString().Should().Be("g");
    }

    [Fact]
    public void ParseImportFile_GeneratedTemplate_ReturnsNoFormulas()
    {
        var templateBytes = _service.GenerateTemplate(Utf8(TemplateJson));

        var request = _service.ParseImportFile(new MemoryStream(templateBytes));

        request.Formulas.Should().BeEmpty("模板仅含表头——示例值在说明表，不应被当数据导入");
    }

    [Fact]
    public void ParseImportFile_MapsBaseFieldsAndHerbDetails()
    {
        using var source = BuildSheet(
            "验方数据",
            new[]
            {
                "验方名称（必填）", "分类", "功效", "用法",
                "药材1名称（必填）", "药材1剂量", "药材1单位",
                "药材2名称", "药材2剂量", "药材2单位",
            },
            new object?[] { "四君子汤", "补益剂", "益气健脾", "水煎服", "人参", "10", "g", "白术", 10m, null },
            null, // 第 3 行全空——应跳过
            new object?[] { "桂枝汤", "解表剂", null, null, "桂枝", "9", "克", null, null, null });

        var request = _service.ParseImportFile(source);

        request.Formulas.Should().HaveCount(2);

        request.Formulas[0].Name.Should().Be("四君子汤");
        request.Formulas[0].Category.Should().Be("补益剂");
        request.Formulas[0].Effect.Should().Be("益气健脾");
        request.Formulas[0].Usage.Should().Be("水煎服");
        request.Formulas[0].Herbs.Should().HaveCount(2);
        request.Formulas[0].Herbs[0].HerbName.Should().Be("人参");
        request.Formulas[0].Herbs[0].Dosage.Should().Be(10);
        request.Formulas[0].Herbs[0].Unit.Should().Be("g");
        request.Formulas[0].Herbs[1].HerbName.Should().Be("白术");
        request.Formulas[0].Herbs[1].Dosage.Should().Be(10);
        request.Formulas[0].Herbs[1].Unit.Should().Be("g", "空单位默认 g");

        request.Formulas[1].Name.Should().Be("桂枝汤");
        request.Formulas[1].Effect.Should().BeNull();
        request.Formulas[1].Herbs.Should().HaveCount(1);
        request.Formulas[1].Herbs[0].Unit.Should().Be("克");
    }

    [Fact]
    public void ParseImportFile_InvalidDosage_ThrowsWithRowNumberAndColumn()
    {
        using var source = BuildSheet(
            "验方数据",
            new[] { "验方名称（必填）", "药材1名称（必填）", "药材1剂量", "药材1单位" },
            new object?[] { "四君子汤", "人参", "十", "g" });

        var act = () => _service.ParseImportFile(source);

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*第 2 行*药材1剂量*格式无效*");
    }

    [Fact]
    public void ParseImportFile_DosageOutOfRange_Throws()
    {
        using var source = BuildSheet(
            "验方数据",
            new[] { "验方名称（必填）", "药材1名称（必填）", "药材1剂量" },
            new object?[] { "四君子汤", "人参", "501" });

        var act = () => _service.ParseImportFile(source);

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*第 2 行*药材1剂量*501*");
    }

    [Fact]
    public void ParseImportFile_HerbNameWithoutDosage_Throws()
    {
        using var source = BuildSheet(
            "验方数据",
            new[] { "验方名称（必填）", "药材1名称（必填）", "药材1剂量", "药材1单位" },
            new object?[] { "四君子汤", "人参", null, "g" });

        var act = () => _service.ParseImportFile(source);

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*第 2 行*药材1剂量*不能为空*");
    }

    [Fact]
    public void ParseImportFile_UnknownHeadersOnly_Throws()
    {
        using var source = BuildSheet("验方数据", new[] { "备注", "产地" }, new object?[] { "x", "y" });

        var act = () => _service.ParseImportFile(source);

        act.Should().Throw<InvalidDataException>().WithMessage("*未识别到验方字段列*");
    }

    [Fact]
    public void GenerateExportFile_WritesBaseAndHerbColumns()
    {
        var bytes = _service.GenerateExportFile(Utf8(ExportJson));

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var sheet = workbook.Worksheet(FormulaExcelService.DataSheetName);

        sheet.Cell(1, 1).GetString().Should().Be("验方ID");
        sheet.Cell(1, 8).GetString().Should().Be("药材数");
        sheet.Cell(1, 9).GetString().Should().Be("总价");
        sheet.Cell(1, 10).GetString().Should().Be("状态");
        sheet.Cell(1, 11).GetString().Should().Be("校验状态");
        sheet.Cell(1, 12).GetString().Should().Be("创建时间");
        sheet.Cell(1, 13).GetString().Should().Be("药材1名称");
        sheet.Cell(1, 14).GetString().Should().Be("药材1剂量");
        sheet.Cell(1, 21).GetString().Should().Be("药材3单位", "药材明细列数按数据中最多的药材味数展开");
        sheet.LastColumnUsed()!.ColumnNumber().Should().Be(21);

        sheet.Cell(2, 2).GetString().Should().Be("四君子汤");
        sheet.Cell(2, 8).GetValue<int>().Should().Be(3);
        sheet.Cell(2, 9).GetValue<decimal>().Should().Be(15.50m);
        sheet.Cell(2, 10).GetString().Should().Be("启用");
        sheet.Cell(2, 11).GetString().Should().Be("已验证");
        sheet.Cell(2, 12).GetDateTime().Should().Be(new DateTime(2026, 9, 1, 8, 30, 0));
        sheet.Cell(2, 13).GetString().Should().Be("人参");
        sheet.Cell(2, 14).GetValue<int>().Should().Be(10);
        sheet.Cell(2, 15).GetString().Should().Be("g");
        sheet.Cell(2, 19).GetString().Should().Be("茯苓");

        sheet.Cell(3, 10).GetString().Should().Be("禁用");
        sheet.Cell(3, 11).GetString().Should().Be("待校验");
        sheet.Cell(3, 13).GetString().Should().Be("桂枝");
        sheet.Cell(3, 16).IsEmpty().Should().BeTrue("第 2 味药材缺失时留空");
        sheet.LastRowUsed()!.RowNumber().Should().Be(3);
    }

    [Fact]
    public void ExportFile_CanBeParsedBack_AsImportFile()
    {
        var bytes = _service.GenerateExportFile(Utf8(ExportJson));

        var request = _service.ParseImportFile(new MemoryStream(bytes));

        request.Formulas.Should().HaveCount(2);
        request.Formulas[0].Name.Should().Be("四君子汤");
        request.Formulas[0].Category.Should().Be("补益剂");
        request.Formulas[0].Herbs.Should().HaveCount(3);
        request.Formulas[0].Herbs[2].HerbName.Should().Be("茯苓");
        request.Formulas[0].Herbs[2].Dosage.Should().Be(10);
        request.Formulas[0].Herbs[2].Unit.Should().Be("g");
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
