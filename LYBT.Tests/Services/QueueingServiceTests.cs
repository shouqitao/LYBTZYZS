using System;
using AutoMapper;
using System.Threading.Tasks;
using Moq;
using Xunit;
using LYBT.Module.Queueing.Services;
using LYBT.Module.Queueing.Interfaces;
using LYBT.Module.Queueing.Dtos;
using LYBT.Module.Queueing.Mapping;

namespace LYBT.Tests.Services;

public class QueueingServiceTests
{
    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<QueueingMappingProfile>());
        return config.CreateMapper();
    }

    [Fact]
    public async Task AddAsync_CallsRepository()
    {
        var repo = new Mock<IQueueingRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<LYBT.Models.Queueing.QueueingModel>())).ReturnsAsync(true);
        var mapper = CreateMapper();
        var service = new QueueingService(repo.Object, mapper);

        var dto = new QueueingCreateDto { PatientId = Guid.NewGuid().ToString(), DoctorId = Guid.NewGuid().ToString() };
        var result = await service.AddAsync(dto);

        Assert.True(result);
        repo.Verify(r => r.AddAsync(It.IsAny<LYBT.Models.Queueing.QueueingModel>()), Times.Once);
    }
}
