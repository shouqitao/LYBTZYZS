using System.IO;
using FluentAssertions;
using LYBT.Infrastructure.Excel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Xunit;

namespace LYBT.Tests.Server;

/// <summary>
/// ExcelImportHelper 解析单测（AC-TEST P0#4 闭环：T4 导入导出修复后无测试守护——
/// 服务端 .xlsx 解析路径 B2 新增，解析正确性回归由本测试守护）
/// </summary>
public class ExcelImportHelperTests
{
    private static MemoryStream CreateWorkbookStream(params string[][] rows)
    {
        using var workbook = new XSSFWorkbook();
        var sheet = workbook.CreateSheet("药材");
        for (var r = 0; r < rows.Length; r++)
        {
            var row = sheet.CreateRow(r);
            for (var c = 0; c < rows[r].Length; c++)
                row.CreateCell(c).SetCellValue(rows[r][c]);
        }
        var ms = new MemoryStream();
        workbook.Write(ms, leaveOpen: true);
        ms.Position = 0;
        return ms;
    }

    [Fact]
    public void ParseWorkbook_WithHeader_SkipsHeaderRow()
    {
        using var ms = CreateWorkbookStream(
            new[] { "名称", "拼音码", "分类", "药性", "产地", "规格", "单位", "单价", "成本价" },
            new[] { "黄芪", "HQ", "补气药", "温", "甘肃", "1kg", "克", "50.5", "30" },
            new[] { "当归", "DG", "补血药", "温", "甘肃", "500g", "克", "60", "40" });

        var rows = ExcelImportHelper.ParseWorkbook(ms, maxRows: 10000);

        rows.Should().HaveCount(2);
        rows[0][0].Should().Be("黄芪");
        rows[0][7].Should().Be("50.5");
        rows[1][1].Should().Be("DG");
    }

    [Fact]
    public void ParseWorkbook_NumericCells_AreFormattedAsText()
    {
        using var ms = CreateWorkbookStream(
            new[] { "名称", "单价" },
            new[] { "甘草", "25" });

        var rows = ExcelImportHelper.ParseWorkbook(ms, maxRows: 10000);

        rows[0][1].Should().Be("25"); // 数字单元格经 DataFormatter → 字符串
    }

    [Fact]
    public void ParseWorkbook_MaxRows_LimitsResult()
    {
        var data = Enumerable.Range(1, 10)
            .Select(i => new[] { $"药材{i}" })
            .ToArray();
        using var ms = CreateWorkbookStream(data);

        var rows = ExcelImportHelper.ParseWorkbook(ms, maxRows: 3);

        rows.Should().HaveCount(3);
    }

    [Fact]
    public void ParseWorkbook_EmptyRows_AreSkipped()
    {
        using var ms = CreateWorkbookStream(
            new[] { "名称" },
            new[] { "黄芪" },
            Array.Empty<string>(), // 空行（无单元格）
            new[] { "当归" });

        var rows = ExcelImportHelper.ParseWorkbook(ms, maxRows: 10000);

        rows.Should().HaveCount(2);
        rows[1][0].Should().Be("当归");
    }

    [Fact]
    public void ParseWorkbook_InvalidWorkbook_Throws()
    {
        using var ms = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });

        var act = () => ExcelImportHelper.ParseWorkbook(ms, maxRows: 10000);

        act.Should().Throw<Exception>(); // NPOI 解析失败——端点层转为业务失败消息
    }
}
