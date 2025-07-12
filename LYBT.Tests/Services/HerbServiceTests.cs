using System;
using AutoMapper;
using System.Threading.Tasks;
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
}
