using System;
using AutoMapper;
using System.Threading.Tasks;
using Moq;
using Xunit;
using LYBT.Module.Registration.Services;
using LYBT.Module.Registration.Interfaces;
using LYBT.Module.Registration.Dtos;
using LYBT.Module.Registration.Mapping;
using LYBT.Module.Queueing.Interfaces;

namespace LYBT.Tests.Services;

public class RegistrationServiceTests
{
    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<RegistrationMappingProfile>());
        return config.CreateMapper();
    }

    [Fact]
    public async Task AddAsync_CreatesQueueEntry()
    {
        var regRepo = new Mock<IRegistrationRepository>();
        regRepo.Setup(r => r.AddAsync(It.IsAny<LYBT.Models.Registration.RegistrationModel>())).ReturnsAsync(true);
        var queueRepo = new Mock<IQueueingRepository>();
        var mapper = CreateMapper();
        var service = new RegistrationService(regRepo.Object, queueRepo.Object, mapper);

        var dto = new RegistrationCreateDto { PatientId = Guid.NewGuid().ToString(), DoctorId = Guid.NewGuid().ToString(), RegistrationType = "General" };
        var result = await service.AddAsync(dto);

        Assert.True(result);
        regRepo.Verify(r => r.AddAsync(It.IsAny<LYBT.Models.Registration.RegistrationModel>()), Times.Once);
        queueRepo.Verify(r => r.AddAsync(It.IsAny<LYBT.Models.Queueing.QueueingModel>()), Times.Once);
    }

    [Fact]
    public async Task CancelAsync_UpdatesRepositories()
    {
        var regRepo = new Mock<IRegistrationRepository>();
        regRepo.Setup(r => r.CancelAsync(It.IsAny<Guid>())).ReturnsAsync(true);
        var queueRepo = new Mock<IQueueingRepository>();
        queueRepo.Setup(q => q.CancelAsync(It.IsAny<Guid>())).ReturnsAsync(true);
        var mapper = CreateMapper();
        var service = new RegistrationService(regRepo.Object, queueRepo.Object, mapper);

        var id = Guid.NewGuid();
        var result = await service.CancelAsync(id);

        Assert.True(result);
        regRepo.Verify(r => r.CancelAsync(id), Times.Once);
        queueRepo.Verify(q => q.CancelAsync(id), Times.Once);
    }
}
