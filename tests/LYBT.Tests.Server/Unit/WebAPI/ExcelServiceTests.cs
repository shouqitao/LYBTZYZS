using LYBT.WebAPI.Services;

namespace LYBT.Tests.Server.Unit.WebAPI;

/// <summary>
/// ExcelService 通用工具单元测试（无数据库依赖）。
/// </summary>
public class ExcelServiceTests
{
    private readonly ExcelService _service = new();

    private sealed class SampleRow
    {
        public string Name { get; set; } = string.Empty;
        public int Amount { get; set; }
        public DateTime? Date { get; set; }
    }

    [Fact]
    public void Export_Then_Parse_Should_RoundTrip_Data()
    {
        var data = new[]
        {
            new SampleRow { Name = "当归", Amount = 10, Date = new DateTime(2026, 1, 1) },
            new SampleRow { Name = "甘草", Amount = 3, Date = null }
        };

        var bytes = _service.ExportToExcel(data, "药材", new Dictionary<string, Func<SampleRow, object?>>
        {
            ["名称"] = i => i.Name,
            ["用量"] = i => i.Amount,
            ["日期"] = i => i.Date
        });

        Assert.True(bytes.Length > 0);

        var parsed = _service.ParseExcel(new MemoryStream(bytes), new Dictionary<string, Action<SampleRow, string>>
        {
            ["名称"] = (i, v) => i.Name = v,
            ["用量"] = (i, v) => i.Amount = int.Parse(v),
            ["日期"] = (i, v) => i.Date = string.IsNullOrWhiteSpace(v) ? null : DateTime.Parse(v)
        });

        Assert.Equal(2, parsed.Count);
        Assert.Equal("当归", parsed[0].Name);
        Assert.Equal(10, parsed[0].Amount);
        Assert.Equal(new DateTime(2026, 1, 1), parsed[0].Date);
        Assert.Equal("甘草", parsed[1].Name);
        Assert.Equal(3, parsed[1].Amount);
        Assert.Null(parsed[1].Date);
    }

    [Fact]
    public void GenerateTemplate_Should_Produce_Header_And_Sample_Row()
    {
        var bytes = _service.GenerateTemplate("患者", new Dictionary<string, string>
        {
            ["姓名"] = "张三",
            ["性别"] = "男"
        });

        var parsed = _service.ParseExcel(new MemoryStream(bytes), new Dictionary<string, Action<SampleRow, string>>
        {
            ["姓名"] = (i, v) => i.Name = v
        });

        Assert.Single(parsed);
        Assert.Equal("张三", parsed[0].Name);
    }

    [Fact]
    public void Export_Empty_Data_Should_Produce_Header_Only()
    {
        var bytes = _service.ExportToExcel(Array.Empty<SampleRow>(), "空表", new Dictionary<string, Func<SampleRow, object?>>
        {
            ["名称"] = i => i.Name
        });

        var parsed = _service.ParseExcel(new MemoryStream(bytes), new Dictionary<string, Action<SampleRow, string>>
        {
            ["名称"] = (i, v) => i.Name = v
        });

        Assert.Empty(parsed);
    }

    [Fact]
    public void ParseExcel_Should_Skip_Empty_Rows()
    {
        var workbook = new NPOI.XSSF.UserModel.XSSFWorkbook();
        var sheet = workbook.CreateSheet("测试");
        var header = sheet.CreateRow(0);
        header.CreateCell(0).SetCellValue("名称");
        header.CreateCell(1).SetCellValue("用量");
        sheet.CreateRow(1).CreateCell(0).SetCellValue("当归");
        sheet.CreateRow(2); // 空行
        sheet.CreateRow(3).CreateCell(0).SetCellValue("甘草");
        using var ms = new MemoryStream();
        workbook.Write(ms);

        var parsed = _service.ParseExcel(new MemoryStream(ms.ToArray()), new Dictionary<string, Action<SampleRow, string>>
        {
            ["名称"] = (i, v) => i.Name = v,
            ["用量"] = (i, v) => i.Amount = string.IsNullOrWhiteSpace(v) ? 0 : int.Parse(v)
        });

        Assert.Equal(2, parsed.Count);
        Assert.Equal("当归", parsed[0].Name);
        Assert.Equal("甘草", parsed[1].Name);
    }
}
