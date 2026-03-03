using FluentAssertions;
using LYBT.Entities.Formulas;
using LYBT.Entities.Herbs;
using LYBT.Module.Formulas.Interfaces;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Server.Integration.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Tests.Server.Integration.Formulas;

/// <summary>
/// 验方 Service 层集成测试 - 导入/验证工作流。
/// 迁移自 LYBT.Module.Formula.IntegrationTests，使用 WebApiFixture 的 DI 容器。
/// 测试 Service 层逻辑而非 HTTP 端点，补充 FormulaIntegrationTests 未覆盖的导入和验证流程。
/// </summary>
[Collection("ServerIntegration")]
public class FormulaServiceIntegrationTests
{
    private readonly WebApiFixture _fixture;

    public FormulaServiceIntegrationTests(WebApiFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// 种子测试用药材到 LYBT_Test 数据库，返回 (Id, Name) 列表。
    /// 原测试依赖 LYBTDB 的真实药材库，迁移后需自行种子。
    /// 直接返回内存中的名称，避免 EF Core 8 OPENJSON/Contains 兼容性问题。
    /// </summary>
    private async Task<List<(Guid Id, string Name)>> SeedTestHerbsAsync(int count = 3)
    {
        var herbs = new List<(Guid Id, string Name)>();
        await _fixture.SeedAsync(async db =>
        {
            for (int i = 0; i < count; i++)
            {
                var id = Guid.NewGuid();
                var name = $"Svc测试药材{i}_{Guid.NewGuid():N}"[..14];
                var herb = new Herb
                {
                    Id = id,
                    Name = name,
                    Unit = "克",
                    Price = 10 + i * 5,
                    Status = CommonStatus.Enabled
                };
                db.Herbs.Add(herb);
                herbs.Add((id, name));
            }
            await db.SaveChangesAsync();
        });
        return herbs;
    }

    [Fact]
    public async Task ImportFromData_WithMatchingHerbs_ShouldImportWithValidatedHerbs()
    {
        // Arrange - 种子药材，直接使用内存中的名称(避免 Contains LINQ 兼容性问题)
        var seededHerbs = await SeedTestHerbsAsync(3);
        var herbNames = seededHerbs.Select(h => h.Name).ToList();

        var importData = new List<FormulaImportItemDto>
        {
            new()
            {
                Name = $"导入匹配测试_{Guid.NewGuid():N}"[..16],
                Effect = "补气养血",
                Usage = "水煎服",
                IsShared = false,
                Herbs = herbNames.Select((name, i) => new FormulaHerbImportItemDto
                {
                    HerbName = name,
                    Dosage = 10 + i * 5,
                    Unit = "g",
                    SortOrder = i
                }).ToList()
            }
        };

        // Act
        using var scope2 = _fixture.Services.CreateScope();
        var importService = scope2.ServiceProvider.GetRequiredService<IFormulaImportExportService>();
        var result = await importService.ImportFromDataAsync(importData);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Data!.SuccessCount.Should().Be(1);
        result.Data.MatchedHerbsCount.Should().BeGreaterThan(0,
            "种子的药材应被自动匹配");
    }

    [Fact]
    public async Task ImportFromData_WithUnknownHerbs_ShouldReportUnmatched()
    {
        // Arrange - 使用不存在的药材名
        var importData = new List<FormulaImportItemDto>
        {
            new()
            {
                Name = $"未匹配测试_{Guid.NewGuid():N}"[..14],
                Effect = "测试功效",
                Usage = "测试用法",
                IsShared = false,
                Herbs = new List<FormulaHerbImportItemDto>
                {
                    new() { HerbName = "绝不存在的药材AAA", Dosage = 10, Unit = "g", SortOrder = 0 },
                    new() { HerbName = "绝不存在的药材BBB", Dosage = 15, Unit = "g", SortOrder = 1 }
                }
            }
        };

        // Act
        using var scope = _fixture.Services.CreateScope();
        var importService = scope.ServiceProvider.GetRequiredService<IFormulaImportExportService>();
        var result = await importService.ImportFromDataAsync(importData);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Data!.SuccessCount.Should().Be(1);
        result.Data.UnmatchedHerbsCount.Should().Be(2,
            "不存在的药材应报告为未匹配");
    }

    [Fact]
    public async Task ValidationFlow_AllHerbsValidated_ShouldTransitionFromDraftToValidated()
    {
        // Arrange - 种子 2 个真实药材
        var seededHerbs = await SeedTestHerbsAsync(2);

        // 创建 Draft 状态验方，药材未验证
        var formulaId = Guid.NewGuid();
        var herbItemId1 = Guid.NewGuid();
        var herbItemId2 = Guid.NewGuid();

        await _fixture.SeedAsync(async db =>
        {
            var formula = new Formula
            {
                Id = formulaId,
                Name = $"验证流转测试_{Guid.NewGuid():N}"[..16],
                Category = "补益剂",
                ValidationStatus = FormulaValidationStatus.Draft,
                Status = CommonStatus.Enabled,
                Herbs = new List<FormulaHerbItem>
                {
                    new()
                    {
                        Id = herbItemId1,
                        HerbName = "未验证药材A",
                        OriginalHerbName = "未验证药材A",
                        IsValidated = false,
                        Dosage = 10,
                        Unit = "g"
                    },
                    new()
                    {
                        Id = herbItemId2,
                        HerbName = "未验证药材B",
                        OriginalHerbName = "未验证药材B",
                        IsValidated = false,
                        Dosage = 15,
                        Unit = "g"
                    }
                }
            };
            db.Formulas.Add(formula);
            await db.SaveChangesAsync();
        });

        // Act - 逐个验证药材
        using var scope = _fixture.Services.CreateScope();
        var formulaService = scope.ServiceProvider.GetRequiredService<IFormulaService>();
        var formulaRepo = scope.ServiceProvider.GetRequiredService<IFormulaRepository>();

        // 验证第 1 个药材
        var result1 = await formulaService.ValidateFormulaHerbAsync(
            formulaId, herbItemId1, seededHerbs[0].Id);
        result1.IsSuccess.Should().BeTrue();

        var afterFirst = await formulaRepo.GetByIdWithHerbsAsync(formulaId);
        afterFirst.ValidationStatus.Should().Be(FormulaValidationStatus.Draft,
            "仍有未验证药材，应保持 Draft");

        // 验证第 2 个药材
        var result2 = await formulaService.ValidateFormulaHerbAsync(
            formulaId, herbItemId2, seededHerbs[1].Id);
        result2.IsSuccess.Should().BeTrue();

        // Assert - 全部验证后应自动升级为 Validated
        var afterAll = await formulaRepo.GetByIdWithHerbsAsync(formulaId);
        afterAll.ValidationStatus.Should().Be(FormulaValidationStatus.Validated,
            "所有药材已验证，应自动升级为 Validated");
        afterAll.Herbs.Should().OnlyContain(h => h.IsValidated);
    }
}
