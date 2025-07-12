using System;
using AutoMapper;
using System.Threading.Tasks;
using Moq;
using Xunit;
using LYBT.Module.Doctors.Services;
using LYBT.Module.Doctors.Interfaces;
using LYBT.Module.Doctors.Dtos;
using LYBT.Module.Doctors.Mapping;

namespace LYBT.Tests.Services;

public class DoctorServiceTests
{
    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<DoctorMappingProfile>());
        return config.CreateMapper();
    }

    [Fact]
    public async Task AddAsync_CallsRepository()
    {
        var repo = new Mock<IDoctorRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<LYBT.Models.Doctors.DoctorModel>())).ReturnsAsync(true);
        var mapper = CreateMapper();
        var service = new DoctorService(repo.Object, mapper);

        var dto = new DoctorDetailDto { UserId = Guid.NewGuid() };
        var result = await service.AddAsync(dto);

        Assert.True(result);
        repo.Verify(r => r.AddAsync(It.IsAny<LYBT.Models.Doctors.DoctorModel>()), Times.Once);
    }
}
