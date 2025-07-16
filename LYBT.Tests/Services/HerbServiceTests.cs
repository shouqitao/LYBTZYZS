using System;
using AutoMapper;
using System.Threading.Tasks;
using System.IO;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Moq;
using Xunit;
using LYBT.Module.Herbs.Services;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Module.Herbs.Dtos;
using LYBT.Module.Herbs.Mapping;

namespace LYBT.Tests.Services;

public class HerbServiceTests
{
    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<HerbMappingProfile>());
        return config.CreateMapper();
    }

    [Fact]
    public async Task AddAsync_CallsRepository()
    {
        var repo = new Mock<IHerbRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<LYBT.Models.HerbModel>())).ReturnsAsync(true);
        var mapper = CreateMapper();
        var service = new HerbService(repo.Object, mapper);

        var dto = new HerbCreateDto { Name = "test" };
        var result = await service.AddAsync(dto);

        Assert.True(result);
        repo.Verify(r => r.AddAsync(It.IsAny<LYBT.Models.HerbModel>()), Times.Once);
    }

    [Fact]
    public async Task ImportFromExcelAsync_HandlesMalformedRows()
    {
        IWorkbook wb = new XSSFWorkbook();
        var sheet = wb.CreateSheet("herbs");
        var header = sheet.CreateRow(0);
        string[] heads = {"Id","Name","Pinyin","Origin","Spec","Unit","Price","Stock","BatchNo","ExpireDate","Effect","Remark"};
        for (int i = 0; i < heads.Length; i++)
            header.CreateCell(i).SetCellValue(heads[i]);
        var row = sheet.CreateRow(1);
        row.CreateCell(0).SetCellValue("3");
        row.CreateCell(1).SetCellValue("紫苏");
        row.CreateCell(6).SetCellValue("abc");
        row.CreateCell(7).SetCellValue("xyz");
        row.CreateCell(9).SetCellValue("no");
        using var ms = new MemoryStream();
        wb.Write(ms, true);
        ms.Position = 0;

        var repo = new Mock<IHerbRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<LYBT.Models.HerbModel>())).ReturnsAsync(true);
        var mapper = CreateMapper();
        var service = new HerbService(repo.Object, mapper);

        var count = await service.ImportFromExcelAsync(ms);

        Assert.Equal(1, count);
        repo.Verify(r => r.AddAsync(It.IsAny<LYBT.Models.HerbModel>()), Times.Once);
    }
}
