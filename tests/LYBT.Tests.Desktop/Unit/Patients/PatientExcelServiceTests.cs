using System;
using System.IO;
using System.Text;
using ClosedXML.Excel;
using FluentAssertions;
using LYBT.Desktop.Patients.Services;
using LYBT.Shared.Models.Enums;
using Xunit;

namespace LYBT.Tests.Desktop.Unit;

/// <summary>
/// B-12 患者 Excel 导入导出转换测试（真实 .xlsx 字节——验证模板/导入/导出契约与行级校验）。
/// </summary>
public class PatientExcelServiceTests
{
    private readonly PatientExcelService _service = new();

    /// <summary>服务端 import-template 响应（ApiResponse 信封，字段说明为 SSOT）</summary>
    private const string TemplateJson = """
        {
          "success": true,
          "message": "患者导入模板（JSON）",
          "data": {
            "description": "患者批量导入 JSON 模板",
            "fields": [
              { "field": "Name", "required": true, "description": "姓名" },
              { "field": "Gender", "required": true, "description": "性别（Male/Female）" },
              { "field": "BirthDate", "required": false, "description": "出生日期（yyyy-MM-dd）" },
              { "field": "IdNumber", "required": false, "description": "身份证号（敏感字段）" },
              { "field": "PhoneNumber", "required": false, "description": "手机号（敏感字段）" },
              { "field": "PinYinCode", "required": false, "description": "拼音码" }
            ],
            "example": [
              { "name": "张三", "gender": "Male", "birthDate": "1990-01-01", "idNumber": "110101199001010011", "phoneNumber": "13800138000", "pinYinCode": "zhangsan" }
            ]
          }
        }
        """;

    /// <summary>服务端 export 响应（ApiResponse 信封，敏感字段已脱敏）</summary>
    private const string ExportJson = """
        {
          "success": true,
          "message": "患者导出（JSON）",
          "data": [
            { "id": "11111111-1111-1111-1111-111111111111", "name": "张三", "gender": "Male", "age": 35, "phoneNumber": "138****8000", "pinYinCode": "zhangsan", "status": "Enabled", "createdAt": "2026-09-01T08:30:00" },
            { "id": "22222222-2222-2222-2222-222222222222", "name": "李四", "gender": "Female", "age": null, "phoneNumber": null, "pinYinCode": null, "status": "Disabled", "createdAt": "2026-09-02T09:00:00" }
          ]
        }
        """;

    private static byte[] Utf8(string json) => Encoding.UTF8.GetBytes(json);

    [Fact]
    public void GenerateTemplate_WritesStableHeaders_AndServerFieldDescriptionsInGuideSheet()
    {
        var bytes = _service.GenerateTemplate(Utf8(TemplateJson));

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var data = workbook.Worksheet(PatientExcelService.DataSheetName);
        var guide = workbook.Worksheet(PatientExcelService.GuideSheetName);

        // 数据表：仅表头（示例值不落数据表——避免用户忘删示例行被当数据导入）
        data.Cell(1, 1).GetString().Should().Be("姓名（必填）");
        data.Cell(1, 2).GetString().Should().Be("性别（必填）");
        data.Cell(1, 3).GetString().Should().Be("出生日期");
        data.Cell(1, 6).GetString().Should().Be("拼音码");
        data.LastRowUsed()!.RowNumber().Should().Be(1);

        // 说明表：字段说明取自服务端 JSON（SSOT）+ 示例值
        guide.Cell(2, 1).GetString().Should().Be("姓名");
        guide.Cell(2, 2).GetString().Should().Be("是");
        guide.Cell(3, 3).GetString().Should().Be("性别（Male/Female）");
        guide.Cell(2, 4).GetString().Should().Be("张三");
    }

    [Fact]
    public void ParseImportFile_GeneratedTemplate_ReturnsNoPatients()
    {
        var templateBytes = _service.GenerateTemplate(Utf8(TemplateJson));

        var request = _service.ParseImportFile(new MemoryStream(templateBytes));

        request.Patients.Should().BeEmpty("模板仅含表头——示例值在说明表，不应被当数据导入");
        request.Strategy.Should().Be(DuplicateStrategy.Skip, "策略默认跳过（与批导入 DTO 默认一致）");
    }

    [Fact]
    public void ParseImportFile_MapsColumnsGenderAndDateVariants()
    {
        using var source = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.Worksheets.Add("患者数据");
            string[] headers = { "姓名（必填）", "性别（必填）", "出生日期", "身份证号", "手机号", "拼音码" };
            for (var i = 0; i < headers.Length; i++)
                sheet.Cell(1, i + 1).Value = headers[i];

            sheet.Cell(2, 1).Value = "张三";
            sheet.Cell(2, 2).Value = "男";
            sheet.Cell(2, 3).Value = new DateTime(1990, 1, 1); // Excel 日期单元格
            sheet.Cell(2, 4).Value = "110101199001010011";
            sheet.Cell(2, 5).Value = "13800138000";
            sheet.Cell(2, 6).Value = "zhangsan";

            sheet.Cell(3, 1).Value = "李四";
            sheet.Cell(3, 2).Value = "Female";
            sheet.Cell(3, 3).Value = "1991/2/3"; // 文本日期
            sheet.Cell(3, 5).Value = "13900139000";

            sheet.Cell(5, 1).Value = "王五"; // 第 4 行为全空行——应跳过
            sheet.Cell(5, 2).Value = "male";

            workbook.SaveAs(source);
        }

        source.Position = 0;
        var request = _service.ParseImportFile(source);

        request.Patients.Should().HaveCount(3);
        request.Patients[0].Name.Should().Be("张三");
        request.Patients[0].Gender.Should().Be(Gender.Male);
        request.Patients[0].BirthDate.Should().Be(new DateTime(1990, 1, 1));
        request.Patients[0].IdNumber.Should().Be("110101199001010011");
        request.Patients[0].PhoneNumber.Should().Be("13800138000");
        request.Patients[0].PinYinCode.Should().Be("zhangsan");

        request.Patients[1].Gender.Should().Be(Gender.Female);
        request.Patients[1].BirthDate.Should().Be(new DateTime(1991, 2, 3));
        request.Patients[1].IdNumber.Should().BeNull();

        request.Patients[2].Name.Should().Be("王五");
        request.Patients[2].Gender.Should().Be(Gender.Male);
        request.Patients[2].BirthDate.Should().BeNull();
    }

    [Fact]
    public void ParseImportFile_InvalidBirthDate_ThrowsWithRowNumber()
    {
        using var source = BuildSheet(
            ("姓名（必填）", "性别（必填）", "出生日期"),
            ("张三", "男", "1990年1月1日"));

        var act = () => _service.ParseImportFile(source);

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*第 2 行*出生日期*");
    }

    [Fact]
    public void ParseImportFile_InvalidGender_ThrowsWithRowNumber()
    {
        using var source = BuildSheet(
            ("姓名（必填）", "性别（必填）"),
            ("张三", "男x"));

        var act = () => _service.ParseImportFile(source);

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*第 2 行*性别*");
    }

    [Fact]
    public void ParseImportFile_UnknownHeadersOnly_Throws()
    {
        using var source = BuildSheet(("备注", "分类"), ("x", "y"));

        var act = () => _service.ParseImportFile(source);

        act.Should().Throw<InvalidDataException>().WithMessage("*未识别到患者字段列*");
    }

    [Fact]
    public void ParseImportFile_NotAnExcelFile_Throws()
    {
        using var source = new MemoryStream(Encoding.UTF8.GetBytes("not an xlsx"));

        var act = () => _service.ParseImportFile(source);

        act.Should().Throw<InvalidDataException>().WithMessage("*无法读取 Excel 文件*");
    }

    [Fact]
    public void GenerateExportFile_WritesRowsWithLabelsAndMaskedSensitiveValues()
    {
        var bytes = _service.GenerateExportFile(Utf8(ExportJson));

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var sheet = workbook.Worksheet(PatientExcelService.DataSheetName);

        sheet.Cell(1, 1).GetString().Should().Be("患者ID");
        sheet.Cell(1, 3).GetString().Should().Be("性别");
        sheet.Cell(1, 8).GetString().Should().Be("创建时间");

        sheet.Cell(2, 2).GetString().Should().Be("张三");
        sheet.Cell(2, 3).GetString().Should().Be("男");
        sheet.Cell(2, 4).GetValue<int>().Should().Be(35);
        sheet.Cell(2, 5).GetString().Should().Be("138****8000", "服务端脱敏结果原样导出");
        sheet.Cell(2, 7).GetString().Should().Be("启用");
        sheet.Cell(2, 8).GetDateTime().Should().Be(new DateTime(2026, 9, 1, 8, 30, 0));

        sheet.Cell(3, 3).GetString().Should().Be("女");
        sheet.Cell(3, 7).GetString().Should().Be("停用");
        sheet.Cell(3, 4).IsEmpty().Should().BeTrue("年龄为 null 时留空");
        sheet.LastRowUsed()!.RowNumber().Should().Be(3);
    }

    [Fact]
    public void GenerateExportFile_ResponseWithoutData_Throws()
    {
        var act = () => _service.GenerateExportFile(Utf8("""{ "success": false, "message": "导出失败" }"""));

        act.Should().Throw<InvalidDataException>().WithMessage("*缺少 data*");
    }

    /// <summary>构造三列数据表（首行表头）</summary>
    private static MemoryStream BuildSheet((string, string, string) headers, params (string, string, string)[] rows)
    {
        var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.Worksheets.Add("患者数据");
            string[] headerValues = { headers.Item1, headers.Item2, headers.Item3 };
            for (var i = 0; i < headerValues.Length; i++)
                sheet.Cell(1, i + 1).Value = headerValues[i];

            for (var r = 0; r < rows.Length; r++)
            {
                string[] values = { rows[r].Item1, rows[r].Item2, rows[r].Item3 };
                for (var c = 0; c < values.Length; c++)
                {
                    if (!string.IsNullOrEmpty(values[c]))
                        sheet.Cell(r + 2, c + 1).Value = values[c];
                }
            }

            workbook.SaveAs(stream);
        }

        stream.Position = 0;
        return stream;
    }

    /// <summary>两列表头场景（性别校验用）</summary>
    private static MemoryStream BuildSheet((string, string) headers, params (string, string)[] rows)
    {
        var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.Worksheets.Add("患者数据");
            string[] headerValues = { headers.Item1, headers.Item2 };
            for (var i = 0; i < headerValues.Length; i++)
                sheet.Cell(1, i + 1).Value = headerValues[i];

            for (var r = 0; r < rows.Length; r++)
            {
                string[] values = { rows[r].Item1, rows[r].Item2 };
                for (var c = 0; c < values.Length; c++)
                {
                    if (!string.IsNullOrEmpty(values[c]))
                        sheet.Cell(r + 2, c + 1).Value = values[c];
                }
            }

            workbook.SaveAs(stream);
        }

        stream.Position = 0;
        return stream;
    }
}
