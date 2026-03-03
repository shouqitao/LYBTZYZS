# Phase 2: 双测试层级合并设计

**日期**: 2026-03-02
**状态**: 已规划，待执行
**目标**: 25 -> 10 个测试项目，两边取长补短合并为一套

---

## 1. 当前测试项目清单

### Structure A (LYBT.Tests.*, 保留目标)

| # | 项目 | 测试数 | 框架 | 数据库 |
|---|------|--------|------|--------|
| 1 | LYBT.Tests.Unit | 325 | net8.0 | InMemory |
| 2 | LYBT.Tests.Desktop.Unit | 609 | net8.0-windows | SQLite |
| 3 | LYBT.Tests.Architecture | 66 | net8.0-windows | - |
| 4 | LYBT.Tests.Server.Integration | 149 | net8.0 | SQL Server(LYBT_Test) |
| 5 | LYBT.Tests.Desktop.Integration | 24 | net8.0-windows | SQLite |

### Structure B (UnitTests/*, 合并来源)

| # | 项目 | 测试数 | 合并目标 |
|---|------|--------|----------|
| 6 | LYBT.Infrastructure.Tests | 85 | Tests.Unit/Infrastructure |
| 7 | LYBT.Module.Auth.Tests | 66 | Tests.Unit/Modules/Auth |
| 8 | LYBT.Module.Users.Tests | 34 | Tests.Unit/Modules/Users |
| 9 | LYBT.Module.Herbs.Tests | 52 | Tests.Unit/Modules/Herbs |
| 10 | LYBT.Module.Patients.Tests | 47 | Tests.Unit/Modules/Patients |
| 11 | LYBT.Module.MedicalCase.Tests | 39 | Tests.Unit/Modules/MedicalCase |
| 12 | LYBT.Module.Formula.Tests | 28 | Tests.Unit/Modules/Formula |
| 13 | LYBT.Module.Sync.Tests | 63 | Tests.Unit/Modules/Sync |
| 14 | LYBT.WebAPI.Tests | 38 | Tests.Unit/WebAPI |
| 15 | LYBT.Shared.Validators.Tests | 125 | Tests.Unit/Shared/Validators |
| 16 | LYBT.Shared.Configuration.Tests | 50 | Tests.Unit/Shared/Configuration |
| 17 | LYBT.Shared.ExceptionHandling.Tests | 70 | Tests.Unit/Shared/ExceptionHandling |
| 18 | LYBT.Shared.Models.Tests | 4 | Tests.Unit/Shared/Models |

### Structure B (IntegrationTests/*, 合并来源)

| # | 项目 | 测试数 | 合并目标 |
|---|------|--------|----------|
| 19 | WebAPI.IntegrationTests | 237 | Tests.Server.Integration |
| 20 | LYBT.Desktop.IntegrationTests | 84 | Tests.Desktop.Integration |
| 21 | LYBT.Module.Formula.IntegrationTests | 5 | Tests.Server.Integration |

### 其他 (保留不变)

| # | 项目 | 测试数 | 处理 |
|---|------|--------|------|
| 22 | LYBT.Server.CompatibilityTests | 8 | 合并到 Tests.Server.Integration |
| 23 | LYBT.Server.PerformanceTests | 0(Benchmark) | 保留 |
| 24 | LYBT.QueryLayer.Benchmarks | 0(Benchmark) | 保留 |
| 25 | LYBT.Tests.Configuration | 0(基础设施) | 保留 |

---

## 2. 重叠分析

### 完全重复 (必须去重, 16 tests)

| Structure A | Structure B | 测试数 |
|-------------|-------------|--------|
| Tests.Unit/Infrastructure/Services/BaseServiceTests.cs | LYBT.Infrastructure.Tests/BaseServiceTests.cs | 12 |
| Tests.Unit/Infrastructure/Serialization/SensitiveDataJsonConverterTests.cs | LYBT.Infrastructure.Tests/Serialization/SensitiveDataJsonConverterTests.cs | 4 |

**策略**: 保留断言更丰富的版本，删除重复。

### 互补的集成测试 (取长补短)

| 端点 | Structure A | Structure B | 策略 |
|------|-------------|-------------|------|
| Auth | 19: 基础登录/刷新/验证/登出 | 3: Token撤销/审计/轮换 | B 全部追加到 A |
| Herbs | 18: CRUD/状态/价格 | 31: CRUD/批量/导出/引用 | 去重 ~8 CRUD，保留 B 独有 |
| Formulas | 16: CRUD/绑定/批量 | 28: CRUD/导出/引用/状态 | 同 Herbs |
| Patients | 23: CRUD/搜索/拼音/分页 | 17: CRUD/搜索 | B 大部分被 A 覆盖，保留独有 |
| MedicalCases | 24: 聚合/状态流转 | 54: 聚合/权限/待完成/Issue修复 | B 有大量专项测试，保留 |
| Users | 24: CRUD/密码/权限 | 17: CRUD | A 更全面，保留 B 独有 |
| Sync | 25: 元数据/比对/上传/下载 | 17 | 保留 B 独有 |
| Diagnostics | - | 7 | B 独有，直接迁入 |
| Health | - | 12 | B 独有，直接迁入 |
| Middleware | - | 14 | B 独有，直接迁入 |
| Logging | - | 9 | B 独有，直接迁入 |
| Batch | - | 17 | B 独有，直接迁入 |
| Performance | - | 6 | B 独有，直接迁入 |

---

## 3. 集成测试基础设施合并

### 对比

| 特性 | WebApiFixture (A, 保留) | IntegrationTestBase (B, 废弃) |
|------|------------------------|-------------------------------|
| 数据库 | LYBT_Test (Drop+Migrate) | LYBTDB (EnsureCreated) |
| 客户端 | Admin/Doctor/SysAdmin/Anonymous | 单一 Admin (随机UserId) |
| 共享模式 | xunit Collection + IAsyncLifetime | 每类独立 Factory (继承) |
| 种子数据 | Upsert 固定ID | 无统一种子 |
| 隔离性 | 更好 (独立DB) | 更差 (共享LYBTDB) |

### WebApiFixture 增强点

从 IntegrationTestBase 吸收:
- `CreateJsonContent<T>()` 辅助方法 (便捷)
- 按名称移除 HostedService 的模式 (更精确)

### 迁移变换规则

```
// Before (Structure B)
public class FooTests : IntegrationTestBase
{
    [Fact]
    public async Task Test1()
    {
        var response = await Client.GetAsync("/api/v1/foo");
    }
}

// After (Structure A)
[Collection("ServerIntegration")]
public class FooTests
{
    private readonly WebApiFixture _fixture;
    public FooTests(WebApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Test1()
    {
        var response = await _fixture.AdminClient.GetAsync("/api/v1/foo");
    }
}
```

---

## 4. 目标架构

```
tests/
├── LYBT.Tests.Unit/                               # 服务端单元测试 (合并后)
│   ├── Entities/                                  # (现有)
│   ├── Infrastructure/                            # (现有 + LYBT.Infrastructure.Tests 去重合并)
│   ├── Modules/                                   # (新增，来自 UnitTests/Server/Modules/*)
│   │   ├── Auth/
│   │   ├── Users/
│   │   ├── Herbs/
│   │   ├── Patients/
│   │   ├── MedicalCase/
│   │   ├── Formula/
│   │   └── Sync/
│   ├── Shared/                                    # (新增，来自 UnitTests/Shared/*)
│   │   ├── Configuration/
│   │   ├── ExceptionHandling/
│   │   ├── Models/
│   │   └── Validators/
│   ├── Utilities/                                 # (现有)
│   └── WebAPI/                                    # (新增，来自 LYBT.WebAPI.Tests)
│
├── LYBT.Tests.Desktop.Unit/                       # (不变)
├── LYBT.Tests.Architecture/                       # (不变)
│
├── LYBT.Tests.Server.Integration/                 # 服务端集成测试 (合并后)
│   ├── Fixtures/WebApiFixture.cs                  # 统一基础设施
│   ├── Auth/                                      # (现有 + B 的 Token撤销/审计)
│   ├── Herbs/                                     # (现有 + B 独有测试)
│   ├── Formulas/                                  # (现有 + B 独有测试)
│   ├── Patients/                                  # (现有 + B 独有测试)
│   ├── MedicalCases/                              # (现有 + B 的权限/待完成/Issue修复)
│   ├── Users/                                     # (现有 + B 独有测试)
│   ├── Sync/                                      # (现有 + B 独有测试)
│   ├── Batch/                                     # (新增，来自 B)
│   ├── Diagnostics/                               # (新增，来自 B)
│   ├── Health/                                    # (新增，来自 B)
│   ├── Logging/                                   # (新增，来自 B)
│   ├── Middleware/                                 # (新增，来自 B)
│   └── Performance/                               # (新增，来自 B)
│
├── LYBT.Tests.Desktop.Integration/                # (合并 LYBT.Desktop.IntegrationTests)
├── TestConfiguration/                             # (保留，共享基础设施)
├── BenchmarkTests/                                # (保留)
└── PerformanceTests/                              # (保留)
```

**净减少**: 25 -> 10 项目 (消除 15 个)
**测试保留**: ~2,200 -> ~2,180 (去重 ~20 个完全重复)

---

## 5. 执行计划

### Phase 2a: Server 集成测试统一

**Task 2.1**: 增强 WebApiFixture
- 添加 `CreateJsonContent<T>()` 辅助方法
- 优化 HostedService 移除逻辑

**Task 2.2**: 迁移 WebAPI.IntegrationTests (237 tests)
- 无重叠文件: 直接移动 + 改 namespace + 改基类 (11 个文件)
- 有重叠文件: 逐个比对，保留更丰富版本 (7 个文件)
- 变换: `IntegrationTestBase` 继承 -> `[Collection("ServerIntegration")]` + 构造注入

**Task 2.6**: 合并 CompatibilityTests (8) + Formula.IntegrationTests (5)

### Phase 2b: Server 单元测试统一

**Task 2.3**: 合并 Shared 测试 (4 项目, 249 tests)
- 添加 ProjectReference + PackageReference
- 移动文件到 `LYBT.Tests.Unit/Shared/`

**Task 2.4**: 合并 Module + Infrastructure + WebAPI 测试 (9 项目, 452 tests)
- 去重 BaseServiceTests + SensitiveDataJsonConverterTests
- 移动文件到 `LYBT.Tests.Unit/{Infrastructure,Modules,WebAPI}/`

### Phase 2c: Desktop 集成测试统一

**Task 2.5**: 合并 LYBT.Desktop.IntegrationTests (84 tests)

### Phase 2d: 清理收尾

**Task 2.7**: 更新 sln + 删除废弃目录
**Task 2.8**: 清理空架构测试方法

---

## 6. 去重规则

1. **完全相同测试**: 保留断言更丰富的版本
2. **功能重叠测试**: 保留覆盖更深（持久化验证、异常路径）的版本
3. **互补测试**: 全部保留，合并到同一文件
4. **Namespace**: 统一使用 `LYBT.Tests.*` 命名约定

## 7. 风险控制

| 风险 | 缓解 |
|------|------|
| 测试数据污染 (共享 DB) | 每个测试使用 Guid.NewGuid() 唯一标识 |
| csproj 依赖膨胀 | 合并后测量编译时间，>30s 则拆分 |
| Namespace 冲突 | 统一为 LYBT.Tests.Unit.* |
| 迁移中间状态编译失败 | 逐文件迁移，每批验证编译 |
