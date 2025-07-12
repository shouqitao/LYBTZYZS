using System;
using AutoMapper;
using System.Threading.Tasks;
using Moq;
using Xunit;
using LYBT.Module.TreatmentRoom.Services;
using LYBT.Module.TreatmentRoom.Interfaces;
using LYBT.Module.TreatmentRoom.Dtos;
using LYBT.Module.TreatmentRoom.Mapping;

namespace LYBT.Tests.Services;

public class TreatmentRoomServiceTests
{
    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<TreatmentRoomMappingProfile>());
        return config.CreateMapper();
    }

    [Fact]
    public async Task AddAsync_CallsRepository()
    {
        var repo = new Mock<ITreatmentRoomRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<LYBT.Models.TreatmentRoom.TreatmentRoomModel>())).ReturnsAsync(true);
        var mapper = CreateMapper();
        var service = new TreatmentRoomService(repo.Object, mapper);

        var dto = new TreatmentRoomCreateDto { TreatmentItem = "t" };
        var result = await service.AddAsync(dto);

        Assert.True(result);
        repo.Verify(r => r.AddAsync(It.IsAny<LYBT.Models.TreatmentRoom.TreatmentRoomModel>()), Times.Once);
    }
}
