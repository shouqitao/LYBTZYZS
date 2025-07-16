using System;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel;
using System.Reflection;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using CommonUtil = LYBT.CommonUtils.CommonUtils;
using LYBT.Module.Herbs.Dtos;
using LYBT.Module.FormulaTemplates.Dtos;
using Xunit;

namespace LYBT.Tests.Helpers;

public class CommonUtilsTests {
    private static string GetDisplayName(Type type, string property) {
        var prop = type.GetProperty(property);
        var attr = prop?.GetCustomAttribute<DisplayNameAttribute>();
        return attr?.DisplayName ?? property;
    }
    [Fact]
    public void HerbRoundTrip() {
        var data = new List<HerbDetailDto> {
            new HerbDetailDto { Id=Guid.NewGuid(), Name="黄芪", Price=1.2m, Stock=10 },
            new HerbDetailDto { Id=Guid.NewGuid(), Name="当归", Price=2.3m, Stock=5 }
        };
        var bytes = CommonUtil.WriteHerbs(data);
        using var ms = new MemoryStream(bytes);
        var list = CommonUtil.ReadHerbs(ms);
        Assert.Equal(data.Count, list.Count);
        Assert.Equal("黄芪", list[0].Name);
        Assert.Equal(10, list[0].Stock);
    }

    [Fact]
    public void TemplateRoundTrip() {
        var data = new List<FormulaTemplateDetailDto> {
            new FormulaTemplateDetailDto { Id=Guid.NewGuid(), Name="test", Herbs=new List<HerbDto>{ new HerbDto{ Id=Guid.NewGuid(), Name="黄芩" } } }
        };
        var bytes = CommonUtil.WriteTemplates(data);
        using var ms = new MemoryStream(bytes);
        var list = CommonUtil.ReadTemplates(ms);
        Assert.Single(list);
        Assert.Equal("test", list[0].Name);
        Assert.Single(list[0].Herbs);
        Assert.Equal("黄芩", list[0].Herbs[0].Name);
    }

    [Fact]
    public void ReadHerbs_HandlesInvalidValuesAndIdColumn() {
        IWorkbook wb = new XSSFWorkbook();
        var sheet = wb.CreateSheet("herbs");
        var header = sheet.CreateRow(0);
        string[] heads = {
            "Id",
            GetDisplayName(typeof(HerbDetailDto), "Name"),
            GetDisplayName(typeof(HerbDetailDto), "Pinyin"),
            GetDisplayName(typeof(HerbDetailDto), "Origin"),
            GetDisplayName(typeof(HerbDetailDto), "Spec"),
            GetDisplayName(typeof(HerbDetailDto), "Unit"),
            GetDisplayName(typeof(HerbDetailDto), "Price"),
            GetDisplayName(typeof(HerbDetailDto), "Stock"),
            GetDisplayName(typeof(HerbDetailDto), "BatchNo"),
            GetDisplayName(typeof(HerbDetailDto), "ExpireDate"),
            GetDisplayName(typeof(HerbDetailDto), "Effect"),
            GetDisplayName(typeof(HerbDetailDto), "Remark")
        };
        for (int i = 0; i < heads.Length; i++)
            header.CreateCell(i).SetCellValue(heads[i]);
        var row = sheet.CreateRow(1);
        row.CreateCell(0).SetCellValue("1");
        row.CreateCell(1).SetCellValue("甘草");
        row.CreateCell(6).SetCellValue("bad");
        row.CreateCell(7).SetCellValue("nope");
        row.CreateCell(9).SetCellValue("unknown");
        using var ms = new MemoryStream();
        wb.Write(ms, true);
        ms.Position = 0;

        var list = CommonUtil.ReadHerbs(ms);

        Assert.Single(list);
        var dto = list[0];
        Assert.Equal("甘草", dto.Name);
        Assert.Equal(0, dto.Price);
        Assert.Equal(0, dto.Stock);
        Assert.Null(dto.ExpireDate);
    }
}
