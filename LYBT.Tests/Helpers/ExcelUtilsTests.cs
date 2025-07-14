using System;
using System.Collections.Generic;
using System.IO;
using ExcelUtil = LYBT.ExcelUtils.ExcelUtils;
using LYBT.Module.Herbs.Dtos;
using LYBT.Module.FormulaTemplates.Dtos;
using Xunit;

namespace LYBT.Tests.Helpers;

public class ExcelUtilsTests {
    [Fact]
    public void HerbRoundTrip() {
        var data = new List<HerbDetailDto> {
            new HerbDetailDto { Id=Guid.NewGuid(), Name="黄芪", Price=1.2m, Stock=10 },
            new HerbDetailDto { Id=Guid.NewGuid(), Name="当归", Price=2.3m, Stock=5 }
        };
        var bytes = ExcelUtil.WriteHerbs(data);
        using var ms = new MemoryStream(bytes);
        var list = ExcelUtil.ReadHerbs(ms);
        Assert.Equal(data.Count, list.Count);
        Assert.Equal("黄芪", list[0].Name);
        Assert.Equal(10, list[0].Stock);
    }

    [Fact]
    public void TemplateRoundTrip() {
        var data = new List<FormulaTemplateDetailDto> {
            new FormulaTemplateDetailDto { Id=Guid.NewGuid(), Name="test", Herbs=new List<HerbDto>{ new HerbDto{ Id=Guid.NewGuid(), Name="黄芩" } } }
        };
        var bytes = ExcelUtil.WriteTemplates(data);
        using var ms = new MemoryStream(bytes);
        var list = ExcelUtil.ReadTemplates(ms);
        Assert.Single(list);
        Assert.Equal("test", list[0].Name);
        Assert.Single(list[0].Herbs);
        Assert.Equal("黄芩", list[0].Herbs[0].Name);
    }
}
