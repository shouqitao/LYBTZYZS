using FluentAssertions;
using FluentValidation;
using LYBT.Module.Catalog.Application.Commands;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Tests.Server.Integration;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Tests.Server.Integration.System;

/// <summary>
/// 深挖（formula-deep-fix-2026-08-13）: 真机 POST /formulas 空 herbs → 201（预期 400）——
/// 疑点：CreateFormulaValidator 未被 DI 注册（ValidationBehavior 找不到 IValidator → 直接 next() → 成功创建）。
/// 用真实宿主（Program + CatalogModule 完整注册链）验证 DI 解析。
/// </summary>
public class FormulaValidatorRegistrationTests : IClassFixture<ProductionWebApiTestFactory>
{
    private readonly ProductionWebApiTestFactory _factory;

    public FormulaValidatorRegistrationTests(ProductionWebApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void CreateFormulaValidator_IsRegisteredInDi()
    {
        var validators = _factory.Services
            .GetServices<IValidator<CreateEntityCommand<FormulaInputDto, FormulaDetailDto>>>()
            .ToList();

        validators.Should().ContainSingle(v => v.GetType().Name == "CreateFormulaValidator");
    }

    [Fact]
    public async Task ValidationBehavior_Rejects_EmptyHerbs()
    {
        var command = new CreateEntityCommand<FormulaInputDto, FormulaDetailDto>(
            new FormulaInputDto { Name = "空组成方", Herbs = new List<FormulaHerbItemInputDto>() },
            Guid.NewGuid());

        var validators = _factory.Services
            .GetServices<IValidator<CreateEntityCommand<FormulaInputDto, FormulaDetailDto>>>()
            .ToList();

        var context = new ValidationContext<CreateEntityCommand<FormulaInputDto, FormulaDetailDto>>(command);
        var results = await Task.WhenAll(validators.Select(v => v.ValidateAsync(context)));
        results.Any(r => !r.IsValid).Should().BeTrue("空 herbs 必须被 NotEmpty 规则拦截");
    }
}
