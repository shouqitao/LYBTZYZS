# Task 2.6: 合并小型专项测试 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 将 CompatibilityTests (8 tests) 和 Formula.IntegrationTests (5 tests) 合并到 LYBT.Tests.Server.Integration，消除 2 个独立测试项目。

**Architecture:** 两个源项目使用不同的基础设施 (自建 WebApplicationFactory / 手动 ServiceCollection)，需统一到 WebApiFixture。CompatibilityTests 全部转为 HTTP 测试；Formula.IntegrationTests 保留 Service 层测试但通过 WebApiFixture.GetService<T>() 获取服务实例。

**Tech Stack:** xunit + FluentAssertions + WebApiFixture + SQL Server (LYBT_Test)

---

## 前置分析: 重叠与去重

### CompatibilityTests (8 tests) 重叠分析

| 源测试 | 与现有测试重叠? | 处理 |
|--------|----------------|------|
| Users_Api_Should_Return_ApiResponse_Format | 是 (UserIntegrationTests.GetUsers) | 去重: 仅保留格式断言增强 |
| Users_GetById_Api_Should_Accept_NotFound | 是 (UserIntegrationTests.GetUser_NonExistent) | 去重: 已覆盖 |
| Patients_Api_Should_Return_ApiResponse_Format | 是 | 去重 |
| Herbs_Api_Should_Return_ApiResponse_Format | 是 | 去重 |
| Formulas_Api_Should_Return_ApiResponse_Format | 是 | 去重 |
| All_Apis_Should_Return_Standard_ApiResponse_Format | **独有** | **迁移: 跨端点 ApiResponse envelope 契约验证** |
| Error_Response_Should_Use_ProblemDetails_Format | 部分 (AuthTests 401) | **迁移: ProblemDetails 格式验证独有** |
| NotFound_Response_Should_Return_404 | 是 | 去重 |

**结论**: 8 -> 2 个独有测试迁移 (Theory 参数化 + ProblemDetails 格式)

### Formula.IntegrationTests (5 tests) 重叠分析

| 源测试 | 与现有 FormulaIntegrationTests 重叠? | 处理 |
|--------|--------------------------------------|------|
| ImportFromData_WithRealHerbs | **独有** (Service 层导入流) | **迁移** |
| ImportFromData_WithUnknownHerbs | **独有** | **迁移** |
| ValidationFlow_DraftToValidated | **独有** (状态流转) | **迁移** |
| GetPendingValidationFormulas | 部分 (HTTP 版已有) | 去重: HTTP 版已覆盖 |
| FormulaCRUD | 是 (HTTP 版更完整) | 去重 |

**结论**: 5 -> 3 个独有测试迁移 (导入匹配 + 状态流转)

**合并后净增**: 5 个测试，删除 2 个项目

---

## Task 2.6a: 迁移 CompatibilityTests (2 独有测试)

### Step 1: 创建 Compatibility 目录和测试文件

**Files:**
- Create: `tests/LYBT.Tests.Server.Integration/Compatibility/ApiResponseContractTests.cs`

```csharp
using System.Net;
using System.Text.Json;
using LYBT.Tests.Server.Integration.Fixtures;

namespace LYBT.Tests.Server.Integration.Compatibility;

/// <summary>
/// API 响应契约测试 - 验证所有端点遵循标准 ApiResponse envelope 格式。
/// 迁移自 LYBT.Server.CompatibilityTests，使用 WebApiFixture 替代自建 WebApplicationFactory。
/// </summary>
[Collection("ServerIntegration")]
public class ApiResponseContractTests
{
    private readonly WebApiFixture _fixture;

    public ApiResponseContractTests(WebApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData("/api/v1/users")]
    [InlineData("/api/v1/patients")]
    [InlineData("/api/v1/herbs")]
    [InlineData("/api/v1/formulas")]
    public async Task AllListEndpoints_ShouldReturn_StandardApiResponseFormat(string endpoint)
    {
        // Act
        var response = await _fixture.AdminClient.GetAsync($"{endpoint}?page=1&pageSize=5");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        AssertStandardApiResponseFormat(content);
    }

    [Fact]
    public async Task UnauthorizedRequest_ShouldReturn401()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _fixture.AnonymousClient.GetAsync($"/api/v1/users/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// 验证标准 ApiResponse envelope 格式: { success: bool, data: any, message: string }
    /// </summary>
    private static void AssertStandardApiResponseFormat(string content)
    {
        var apiResponse = JsonSerializer.Deserialize<JsonElement>(content);

        apiResponse.TryGetProperty("success", out var successProperty)
            .Should().BeTrue("ApiResponse 应包含 'success' 字段");
        apiResponse.TryGetProperty("data", out _)
            .Should().BeTrue("ApiResponse 应包含 'data' 字段");
        apiResponse.TryGetProperty("message", out _)
            .Should().BeTrue("ApiResponse 应包含 'message' 字段");

        (successProperty.ValueKind == JsonValueKind.True ||
         successProperty.ValueKind == JsonValueKind.False)
            .Should().BeTrue("'success' 字段应为布尔类型");
    }
}
```

### Step 2: 编译验证

Run: `dotnet build tests/LYBT.Tests.Server.Integration/`
Expected: 0 errors

### Step 3: 运行新测试

Run: `dotnet test tests/LYBT.Tests.Server.Integration/ --filter "FullyQualifiedName~ApiResponseContractTests" -v n`
Expected: 5 passed (4 Theory + 1 Fact)

---

## Task 2.6b: 迁移 Formula.IntegrationTests (3 独有测试)

### Step 4: 创建 FormulaServiceIntegrationTests 文件

**Files:**
- Create: `tests/LYBT.Tests.Server.Integration/Formulas/FormulaServiceIntegrationTests.cs`

**关键适配**:
- 原测试使用 `LYBTDB` (生产数据库) 的真实药材数据
- WebApiFixture 使用 `LYBT_Test` (每次 Drop+Migrate，无药材数据)
- 解决方案: 通过 `WebApiFixture.SeedAsync()` 种子测试药材，然后用 `GetService<T>()` 获取服务实例

```csharp
using FluentAssertions;
using LYBT.Entities.Formulas;
using LYBT.Entities.Herbs;
using LYBT.Infrastructure.Data;
using LYBT.Module.Formulas.Interfaces;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Server.Integration.Fixtures;
using Microsoft.EntityFrameworkCore;
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
    /// 种子测试用药材到 LYBT_Test 数据库。
    /// 原测试依赖 LYBTDB 的真实药材库，迁移后需自行种子。
    /// </summary>
    private async Task<List<Guid>> SeedTestHerbsAsync(int count = 3)
    {
        var herbIds = new List<Guid>();
        await _fixture.SeedAsync(async db =>
        {
            for (int i = 0; i < count; i++)
            {
                var herb = new Herb
                {
                    Id = Guid.NewGuid(),
                    Name = $"测试药材_{Guid.NewGuid():N}"[..12],
                    Unit = "克",
                    Price = 10 + i * 5,
                    Status = CommonStatus.Enabled,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                db.Herbs.Add(herb);
                herbIds.Add(herb.Id);
            }
            await db.SaveChangesAsync();
        });
        return herbIds;
    }

    [Fact]
    public async Task ImportFromData_WithMatchingHerbs_ShouldImportWithValidatedHerbs()
    {
        // Arrange - 种子药材并获取名称
        var herbIds = await SeedTestHerbsAsync(3);
        List<string> herbNames;
        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            herbNames = await db.Herbs
                .Where(h => herbIds.Contains(h.Id))
                .Select(h => h.Name)
                .ToListAsync();
        }

        var importData = new List<FormulaImportItemDto>
        {
            new()
            {
                Name = $"导入测试验方_{Guid.NewGuid():N}"[..16],
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
                Name = $"未匹配测试验方_{Guid.NewGuid():N}"[..18],
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
    public async Task ValidationFlow_AllHerbsValidated_ShouldTransitionToDraftToValidated()
    {
        // Arrange - 种子 2 个真实药材
        var herbIds = await SeedTestHerbsAsync(2);

        // 创建 Draft 状态验方，药材未验证
        Guid formulaId;
        List<Guid> herbItemIds;
        await _fixture.SeedAsync(async db =>
        {
            var formula = new Formula
            {
                Id = Guid.NewGuid(),
                Name = $"验证流转测试_{Guid.NewGuid():N}"[..16],
                Category = "补益剂",
                ValidationStatus = FormulaValidationStatus.Draft,
                Status = CommonStatus.Enabled,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Herbs = new List<FormulaHerbItem>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        HerbName = "未验证药材A",
                        OriginalHerbName = "未验证药材A",
                        IsValidated = false,
                        Dosage = 10,
                        Unit = "g",
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        HerbName = "未验证药材B",
                        OriginalHerbName = "未验证药材B",
                        IsValidated = false,
                        Dosage = 15,
                        Unit = "g",
                        CreatedAt = DateTime.UtcNow
                    }
                }
            };
            db.Formulas.Add(formula);
            await db.SaveChangesAsync();
            formulaId = formula.Id;
            herbItemIds = formula.Herbs.Select(h => h.Id).ToList();
        });

        // Act - 逐个验证药材
        using var scope = _fixture.Services.CreateScope();
        var formulaService = scope.ServiceProvider.GetRequiredService<IFormulaService>();
        var formulaRepo = scope.ServiceProvider.GetRequiredService<IFormulaRepository>();

        // 验证第 1 个药材
        var result1 = await formulaService.ValidateFormulaHerbAsync(
            formulaId, herbItemIds[0], herbIds[0]);
        result1.IsSuccess.Should().BeTrue();

        var afterFirst = await formulaRepo.GetByIdWithHerbsAsync(formulaId);
        afterFirst!.ValidationStatus.Should().Be(FormulaValidationStatus.Draft,
            "仍有未验证药材，应保持 Draft");

        // 验证第 2 个药材
        var result2 = await formulaService.ValidateFormulaHerbAsync(
            formulaId, herbItemIds[1], herbIds[1]);
        result2.IsSuccess.Should().BeTrue();

        // Assert - 全部验证后应自动升级为 Validated
        var afterAll = await formulaRepo.GetByIdWithHerbsAsync(formulaId);
        afterAll!.ValidationStatus.Should().Be(FormulaValidationStatus.Validated,
            "所有药材已验证，应自动升级为 Validated");
        afterAll.Herbs.Should().OnlyContain(h => h.IsValidated);
    }
}
```

### Step 5: 检查 csproj 依赖

检查 `LYBT.Tests.Server.Integration.csproj` 是否已引用所需模块项目。
WebApiFixture 通过 `LYBT.WebAPI.csproj` 间接引用所有模块，GetService<T>() 应可获取 IFormulaService 等。

需要确认以下类型可解析:
- `IFormulaImportExportService` (via LYBT.Module.Formula)
- `IFormulaService` (via LYBT.Module.Formula)
- `IFormulaRepository` (via LYBT.Module.Formula)
- `Herb` entity (via LYBT.Entities)
- `Formula` / `FormulaHerbItem` entities (via LYBT.Entities)

如果编译报 `Herb` 类型不可用，需添加:
```xml
<ProjectReference Include="..\..\src\Server\Modules\LYBT.Module.Herbs\LYBT.Module.Herbs.csproj" />
```

### Step 6: 编译验证

Run: `dotnet build tests/LYBT.Tests.Server.Integration/`
Expected: 0 errors (如有缺失引用，按 Step 5 补充)

### Step 7: 运行新测试

Run: `dotnet test tests/LYBT.Tests.Server.Integration/ --filter "FullyQualifiedName~FormulaServiceIntegrationTests" -v n`
Expected: 3 passed

---

## Task 2.6c: 全量验证

### Step 8: 运行全部集成测试

Run: `dotnet test tests/LYBT.Tests.Server.Integration/ -v n`
Expected: 263 passed (258 existing + 5 new), 0 failed

### Step 9: 运行源项目测试确认等价

Run: `dotnet test tests/CompatibilityTests/ -v n`
Run: `dotnet test tests/IntegrationTests/Server/Modules/LYBT.Module.Formula.IntegrationTests/ -v n`
Expected: 源项目测试仍通过 (确认功能等价)

---

## Task 2.6d: 清理 (Phase 2d 统一执行)

以下清理在 Task 2.7 统一执行，本任务仅标记:
- [ ] 从 LYBT.All.sln 移除 LYBT.Server.CompatibilityTests
- [ ] 从 LYBT.All.sln 移除 LYBT.Module.Formula.IntegrationTests
- [ ] 删除 `tests/CompatibilityTests/` 目录
- [ ] 删除 `tests/IntegrationTests/Server/Modules/LYBT.Module.Formula.IntegrationTests/` 目录

---

## 风险与缓解

| 风险 | 缓解 |
|------|------|
| FormulaServiceIntegrationTests 依赖 DI 容器中的 Service 注册 | WebAPI 启动已注册所有模块，GetService<T>() 应可用 |
| LYBT_Test 无药材数据 (原测试用 LYBTDB) | SeedTestHerbsAsync 主动种子 |
| Herb entity 类型引用 | csproj 已通过 WebAPI 间接引用，如不够需显式添加 |
| Formula 实体缺少 CreatedAt/UpdatedAt | Seed 时显式设置时间戳 |

---

## 预计时间

| Step | 预计 | 说明 |
|------|------|------|
| Step 1-3 (CompatibilityTests) | 5 min | 简单适配 |
| Step 4-7 (Formula.IntegrationTests) | 10 min | Service 层适配 + 种子数据 |
| Step 8-9 (全量验证) | 5 min | 编译 + 测试 |
| **Total** | **20 min** | |
