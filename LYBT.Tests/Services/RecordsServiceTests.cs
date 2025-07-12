using System;
using AutoMapper;
using System.Threading.Tasks;
using Moq;
using Xunit;
using LYBT.Module.Records.Services;
using LYBT.Module.Records.Interfaces;
using LYBT.Module.Records.Dtos;
using LYBT.Module.Records.Mapping;
using LYBT.Module.Logs.Interfaces;

namespace LYBT.Tests.Services;

public class RecordsServiceTests
{
    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<RecordMappingProfile>());
        return config.CreateMapper();
    }

    [Fact]
    public async Task AddAsync_CallsRepositoryAndLogs()
    {
        var repo = new Mock<IRecordRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<LYBT.Models.Records.RecordModel>())).ReturnsAsync(true);
        var logSvc = new Mock<ILogService>();
        var mapper = CreateMapper();
        var service = new RecordService(repo.Object, mapper, logSvc.Object);

        var dto = new RecordCreateDto { PatientId = Guid.NewGuid().ToString() };
        var result = await service.AddAsync(dto, Guid.Empty, "sys");

        Assert.True(result);
        repo.Verify(r => r.AddAsync(It.IsAny<LYBT.Models.Records.RecordModel>()), Times.Once);
        logSvc.Verify(l => l.AddLogAsync(It.IsAny<LYBT.Module.Logs.Dtos.LogDto>()), Times.Once);
    }
}
