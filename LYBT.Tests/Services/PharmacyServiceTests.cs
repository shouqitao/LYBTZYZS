using System;
using AutoMapper;
using System.Threading.Tasks;
using Moq;
using Xunit;
using LYBT.Module.Pharmacy.Services;
using LYBT.Module.Pharmacy.Interfaces;
using LYBT.Module.Pharmacy.Dtos;
using LYBT.Module.Pharmacy.Mapping;

namespace LYBT.Tests.Services;

public class PharmacyServiceTests
{
    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<PharmacyMappingProfile>());
        return config.CreateMapper();
    }

    [Fact]
    public async Task AddAsync_CallsRepository()
    {
        var repo = new Mock<IPharmacyRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<LYBT.Models.Pharmacy.PharmacyModel>())).ReturnsAsync(true);
        var mapper = CreateMapper();
        var service = new PharmacyService(repo.Object, mapper);

        var dto = new PharmacyCreateDto { PrescriptionId = Guid.NewGuid() };
        var result = await service.AddAsync(dto);

        Assert.True(result);
        repo.Verify(r => r.AddAsync(It.IsAny<LYBT.Models.Pharmacy.PharmacyModel>()), Times.Once);
    }
}
