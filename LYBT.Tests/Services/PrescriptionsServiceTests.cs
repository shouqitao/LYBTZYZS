using System;
using AutoMapper;
using System.Threading.Tasks;
using Moq;
using Xunit;
using LYBT.Module.Prescriptions.Services;
using LYBT.Module.Prescriptions.Repositories;
using LYBT.Module.Prescriptions.Dtos;
using LYBT.Module.Prescriptions.Mapping;
using LYBT.Module.Logs.Interfaces;

namespace LYBT.Tests.Services;

public class PrescriptionsServiceTests
{
    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<PrescriptionMappingProfile>());
        return config.CreateMapper();
    }

    [Fact]
    public async Task CreateAsync_CallsRepositoryAndLogs()
    {
        var repo = new Mock<IPrescriptionRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<LYBT.Models.Prescriptions.PrescriptionModel>())).ReturnsAsync(true);
        var logSvc = new Mock<ILogService>();
        var mapper = CreateMapper();
        var service = new PrescriptionService(repo.Object, logSvc.Object, mapper);

        var dto = new PrescriptionCreateDto { PatientId = Guid.NewGuid() };
        var result = await service.CreateAsync(dto, Guid.Empty, "sys");

        Assert.True(result);
        repo.Verify(r => r.AddAsync(It.IsAny<LYBT.Models.Prescriptions.PrescriptionModel>()), Times.Once);
        logSvc.Verify(l => l.AddLogAsync(It.IsAny<LYBT.Module.Logs.Dtos.LogDto>()), Times.Once);
    }
}
