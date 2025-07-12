using System;
using AutoMapper;
using System.Threading.Tasks;
using Moq;
using Xunit;
using LYBT.Module.FormulaTemplates.Services;
using LYBT.Module.FormulaTemplates.Interfaces;
using LYBT.Module.FormulaTemplates.Dtos;
using LYBT.Module.FormulaTemplates.Mapping;

namespace LYBT.Tests.Services;

public class FormulaTemplatesServiceTests
{
    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<FormulaTemplateMappingProfile>());
        return config.CreateMapper();
    }

    [Fact]
    public async Task AddAsync_CallsRepository()
    {
        var repo = new Mock<IFormulaTemplateRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<LYBT.Models.FormulaTemplates.FormulaTemplateModel>())).ReturnsAsync(true);
        var mapper = CreateMapper();
        var service = new FormulaTemplateService(repo.Object, mapper);

        var dto = new FormulaTemplateCreateDto { Name = "test" };
        var result = await service.AddAsync(dto);

        Assert.True(result);
        repo.Verify(r => r.AddAsync(It.IsAny<LYBT.Models.FormulaTemplates.FormulaTemplateModel>()), Times.Once);
    }
}
