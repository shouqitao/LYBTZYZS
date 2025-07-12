using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Moq;
using Xunit;
using LYBT.Module.Billing.Services;
using LYBT.Module.Billing.Interfaces;
using LYBT.Module.Billing.Dtos;
using LYBT.Module.Billing.Mapping;

namespace LYBT.Tests.Services;

public class BillingServiceTests
{
    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<BillingMappingProfile>());
        return config.CreateMapper();
    }

    [Fact]
    public async Task AddAsync_CallsRepository()
    {
        var repo = new Mock<IBillingRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<LYBT.Models.Billing.BillingModel>())).ReturnsAsync(true);
        var mapper = CreateMapper();
        var service = new BillingService(repo.Object, mapper);

        var dto = new BillingCreateDto
        {
            PatientId = Guid.NewGuid(),
            DoctorId = Guid.NewGuid()
        };
        var result = await service.AddAsync(dto);

        Assert.True(result);
        repo.Verify(r => r.AddAsync(It.IsAny<LYBT.Models.Billing.BillingModel>()), Times.Once);
    }
}
