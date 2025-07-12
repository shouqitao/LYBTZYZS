using System;
using AutoMapper;
using System.Threading.Tasks;
using Moq;
using Xunit;
using LYBT.Module.Patients.Services;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Dtos;
using LYBT.Module.Patients.Mapping;
using LYBT.Module.Logs.Interfaces;
using LYBT.Module.Records.Interfaces;

namespace LYBT.Tests.Services;

public class PatientsServiceTests
{
    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<PatientMappingProfile>());
        return config.CreateMapper();
    }

    [Fact]
    public async Task AddAsync_CallsRepositoryAndLogs()
    {
        var repo = new Mock<IPatientRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<LYBT.Models.Patients.PatientModel>())).ReturnsAsync(true);
        var logSvc = new Mock<ILogService>();
        var recordSvc = new Mock<IRecordService>();
        var mapper = CreateMapper();
        var service = new PatientService(repo.Object, mapper, logSvc.Object, recordSvc.Object);

        var dto = new PatientDetailDto { Name = "test" };
        var result = await service.AddAsync(dto, Guid.Empty, "sys");

        Assert.True(result);
        repo.Verify(r => r.AddAsync(It.IsAny<LYBT.Models.Patients.PatientModel>()), Times.Once);
        logSvc.Verify(l => l.AddLogAsync(It.IsAny<LYBT.Module.Logs.Dtos.LogDto>()), Times.Once);
    }
}
