using System;
using AutoMapper;
using System.Threading.Tasks;
using Moq;
using Xunit;
using LYBT.Module.Settings.Services;
using LYBT.Module.Settings.Interfaces;
using LYBT.Module.Settings.Dtos;
using LYBT.Module.Settings.Mapping;

namespace LYBT.Tests.Services;

public class SettingsServiceTests
{
    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SettingsMappingProfile>());
        return config.CreateMapper();
    }

    [Fact]
    public async Task AddAsync_CallsRepository()
    {
        var repo = new Mock<ISettingsRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<LYBT.Models.Settings.SettingsModel>())).ReturnsAsync(true);
        var mapper = CreateMapper();
        var service = new SettingsService(repo.Object, mapper);

        var dto = new SettingsCreateDto { Key = "k" };
        var result = await service.AddAsync(dto);

        Assert.True(result);
        repo.Verify(r => r.AddAsync(It.IsAny<LYBT.Models.Settings.SettingsModel>()), Times.Once);
    }
}
