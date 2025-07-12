using System;
using AutoMapper;
using System.Threading.Tasks;
using Moq;
using Xunit;
using LYBT.Module.Sync.Services;
using LYBT.Module.Sync.Interfaces;
using LYBT.Module.Sync.Dtos;
using LYBT.Module.Sync.Mapping;

namespace LYBT.Tests.Services;

public class SyncServiceTests
{
    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SyncMappingProfile>());
        return config.CreateMapper();
    }

    [Fact]
    public async Task AddLogAsync_CallsRepository()
    {
        var repo = new Mock<ISyncRepository>();
        repo.Setup(r => r.AddLogAsync(It.IsAny<LYBT.Models.SyncLogModel>())).ReturnsAsync(true);
        var mapper = CreateMapper();
        var service = new SyncService(repo.Object, mapper);

        var dto = new SyncLogCreateDto { Message = "ok" };
        var result = await service.AddLogAsync(dto);

        Assert.True(result);
        repo.Verify(r => r.AddLogAsync(It.IsAny<LYBT.Models.SyncLogModel>()), Times.Once);
    }
}
