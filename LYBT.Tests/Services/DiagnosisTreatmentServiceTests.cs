using System;
using AutoMapper;
using System.Threading.Tasks;
using Moq;
using Xunit;
using LYBT.Module.DiagnosisTreatment.Services;
using LYBT.Module.DiagnosisTreatment.Interfaces;
using LYBT.Module.DiagnosisTreatment.Models.Dtos;
using LYBT.Module.DiagnosisTreatment.Mapping;

namespace LYBT.Tests.Services;

public class DiagnosisTreatmentServiceTests
{
    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<DiagnosisTreatmentMappingProfile>());
        return config.CreateMapper();
    }

    [Fact]
    public async Task AddAsync_CallsRepository()
    {
        var repo = new Mock<IDiagnosisTreatmentRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<LYBT.Models.DiagnosisTreatment.DiagnosisTreatmentModel>())).ReturnsAsync(true);
        var mapper = CreateMapper();
        var service = new DiagnosisTreatmentService(repo.Object, mapper);

        var dto = new DiagnosisTreatmentCreateDto { PatientId = Guid.NewGuid(), Diagnosis = "" };
        var result = await service.AddAsync(dto);

        Assert.True(result);
        repo.Verify(r => r.AddAsync(It.IsAny<LYBT.Models.DiagnosisTreatment.DiagnosisTreatmentModel>()), Times.Once);
    }
}
