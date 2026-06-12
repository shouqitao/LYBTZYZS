# Task 2.5: Desktop 集成测试统一 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 将 LYBT.Desktop.IntegrationTests (84 tests) 合并到 LYBT.Tests.Desktop.Integration (24 tests)，去重后预计 ~95 tests。

**Architecture:** 机械迁移 + DI 修复。Source B 分三层 (LocalMode / EndToEnd / Foundation)，全部迁移到 Target A。DataSource 层 13 个测试与 Target A 完全重叠，跳过。27 个 ViewModel E2E 测试因 5 个缺失 DI 注册全部失败，修复 DesktopE2ETestFixture 后可通过。

**Tech Stack:** xunit + FluentAssertions + NSubstitute + SQLite InMemory + WPF/Prism + .NET 8

---

## 合并清单

### 跳过 (与 Target A 重叠)

| Source B 测试 | Target A 对应 | 决策 |
|-------------|---------------|------|
| DataSource DI 解析 ×5 | DI_AllDataSources_CanBeResolved | 跳过 (A 更简洁) |
| *_CRUD_EndToEnd ×5 | Patient/Herb/Formula/User/MedicalCase_CRUD_EndToEnd | 跳过 (完全重叠) |
| PatientDataSource_Paging | Patient_Paging | 跳过 |
| MultipleDataSources_SameServiceProvider | MultipleDataSources_ShareDatabase | 跳过 |
| DataSources_UsesSameDbContext_DataIsShared | MultipleDataSources_ShareDatabase | 跳过 (同义) |

**跳过总计: 13 个 DataSource 测试**

### 迁移 (B 独有)

| 来源 | 测试数 | 通过 | 失败 | 目标目录 |
|------|--------|------|------|----------|
| Foundation/Security/AuthenticationIntegrationTests | 4 | 4 | 0 | Foundation/Security/ |
| Foundation/Http/RetryPolicyIntegrationTests | 11 | 11 | 0 | Foundation/Http/ |
| Foundation/Http/TokenRefreshHandlerIntegrationTests | 5 | 5 | 0 | Foundation/Http/ |
| LocalMode/LoginFlowIntegrationTests | 7 | 7 | 0 | LocalMode/ |
| EndToEnd/Prescription/PrescriptionE2ETests | 5 | 5 | 0 | EndToEnd/Prescription/ |
| EndToEnd/MedicalCase/MedicalCaseAggregateE2ETests | 12 | 11 | 1 | EndToEnd/MedicalCase/ |
| EndToEnd/BusinessFlow/BusinessFlowE2ETests | 1 | 1 | 0 | EndToEnd/BusinessFlow/ |
| EndToEnd/Patients/PatientE2ETests | 6 | 0 | 6 | EndToEnd/Patients/ |
| EndToEnd/Herbs/HerbE2ETests | 4 | 0 | 4 | EndToEnd/Herbs/ |
| EndToEnd/Formula/FormulaE2ETests | 5 | 0 | 5 | EndToEnd/Formula/ |
| EndToEnd/MedicalCase/MedicalCaseE2ETests | 4 | 0 | 4 | EndToEnd/MedicalCase/ |
| EndToEnd/Navigation/NavigationFlowE2ETests | 4 | 0 | 4 | EndToEnd/Navigation/ |
| EndToEnd/Users/UserE2ETests | 3 | 0 | 3 | EndToEnd/Users/ |

**迁移总计: 71 个测试 (44 passing + 27 failing)**

### 27 个失败根因 (统一修复)

全部因 `DesktopE2ETestFixture` 缺少 5 个 DI 注册:

| 缺失接口 | 影响 ViewModel | 影响测试数 |
|----------|---------------|-----------|
| `IPatientStatusHandler` | PatientMasterDetailViewModel | 6 + 4 Nav |
| `IHerbStatusHandler` | HerbMasterDetailViewModel | 4 + 4 Nav |
| `IFormulaStatusHandler` | FormulaMasterDetailViewModel | 5 |
| `IDesktopCacheManager` | UserMasterDetailViewModel | 3 |
| `IHerbSearchProvider` | FormulaMasterDetailViewModel | 5 (共享) |

### Fixture 迁移

| 源 Fixture | 用于 | 迁移方式 |
|-----------|------|----------|
| LocalModeTestFixture | LoginFlow 测试 | 复制到 LocalMode/Fixtures/ |
| DesktopE2ETestFixture | ViewModel E2E 测试 | 复制到 EndToEnd/Fixtures/ + 修复 5 个 DI |

---

## Namespace 转换规则

| 源 Namespace | 目标 Namespace |
|-------------|---------------|
| `LYBT.Desktop.IntegrationTests.LocalMode` | `LYBT.Tests.Desktop.Integration.LocalMode` |
| `LYBT.Desktop.IntegrationTests.LocalMode.Fixtures` | `LYBT.Tests.Desktop.Integration.LocalMode.Fixtures` |
| `LYBT.Desktop.IntegrationTests.EndToEnd` | `LYBT.Tests.Desktop.Integration.EndToEnd` |
| `LYBT.Desktop.IntegrationTests.EndToEnd.Fixtures` | `LYBT.Tests.Desktop.Integration.EndToEnd.Fixtures` |
| `LYBT.Desktop.IntegrationTests.Foundation` | `LYBT.Tests.Desktop.Integration.Foundation` |

---

## Task 1: 更新 Tests.Desktop.Integration.csproj 依赖

**Files:**
- Modify: `tests/LYBT.Tests.Desktop.Integration/LYBT.Tests.Desktop.Integration.csproj`

添加 Source B 所需的额外依赖:

```xml
<!-- 新增 PackageReference -->
<PackageReference Include="Microsoft.Extensions.DependencyInjection" />
<PackageReference Include="Microsoft.Extensions.Logging" />
<PackageReference Include="BCrypt.Net-Next" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" />
<PackageReference Include="Microsoft.Extensions.Configuration" />
<PackageReference Include="Microsoft.Extensions.Configuration.Json" />

<!-- 新增 ProjectReference -->
<ProjectReference Include="..\..\src\Server\Core\LYBT.Entities\LYBT.Entities.csproj" />
<ProjectReference Include="..\..\src\Client\Desktop\Core\LYBT.Desktop.CardReader\LYBT.Desktop.CardReader.csproj" />
<ProjectReference Include="..\TestConfiguration\LYBT.Tests.Configuration.csproj" />
```

**Build check:** `dotnet build tests/LYBT.Tests.Desktop.Integration/`
Expected: 0 errors

---

## Task 2: 迁移 Foundation 测试 (20 tests, 3 files)

**Source:** `tests/IntegrationTests/Client/Desktop/LYBT.Desktop.IntegrationTests/Foundation/`

Foundation 测试自包含 (不依赖 Fixture)，是最安全的迁移目标。

| 源文件 | 目标文件 | Tests |
|--------|---------|-------|
| `Foundation/Security/AuthenticationIntegrationTests.cs` | `Foundation/Security/AuthenticationIntegrationTests.cs` | 4 |
| `Foundation/Http/RetryPolicyIntegrationTests.cs` | `Foundation/Http/RetryPolicyIntegrationTests.cs` | 11 |
| `Foundation/Http/TokenRefreshHandlerIntegrationTests.cs` | `Foundation/Http/TokenRefreshHandlerIntegrationTests.cs` | 5 |

**Namespace change:** `LYBT.Desktop.IntegrationTests.Foundation.{Sub}` → `LYBT.Tests.Desktop.Integration.Foundation.{Sub}`

**Process:**
1. Read each source file
2. Create target file with updated namespace
3. No logic changes

**Build + Test check:** `dotnet test tests/LYBT.Tests.Desktop.Integration/ --filter "FullyQualifiedName~Foundation" -v q`
Expected: 20 passed

---

## Task 3: 迁移 LocalModeTestFixture + LoginFlowIntegrationTests (7 tests)

**Source:** `tests/IntegrationTests/Client/Desktop/LYBT.Desktop.IntegrationTests/LocalMode/`

| 源文件 | 目标文件 | 说明 |
|--------|---------|------|
| `LocalMode/Fixtures/LocalModeTestFixture.cs` | `LocalMode/Fixtures/LocalModeTestFixture.cs` | Fixture |
| `LocalMode/LoginFlowIntegrationTests.cs` | `LocalMode/LoginFlowIntegrationTests.cs` | 7 tests |

**注意:** Source B 的 DataSourceIntegrationTests.cs 不迁移 (与 Target A 重叠)。

**Namespace change:** `LYBT.Desktop.IntegrationTests.LocalMode.{Sub}` → `LYBT.Tests.Desktop.Integration.LocalMode.{Sub}`

**特殊检查:**
- LocalModeTestFixture 可能依赖 appsettings.json -- 检查并复制
- LoginFlowIntegrationTests 引用 LocalModeTestFixture -- 确保 IClassFixture 正确指向

**Build + Test check:** `dotnet test tests/LYBT.Tests.Desktop.Integration/ --filter "FullyQualifiedName~LoginFlow" -v q`
Expected: 7 passed

---

## Task 4: 迁移 DesktopE2ETestFixture + 修复 5 个缺失 DI 注册

**Source:** `tests/IntegrationTests/Client/Desktop/LYBT.Desktop.IntegrationTests/EndToEnd/Fixtures/DesktopE2ETestFixture.cs`
**Target:** `tests/LYBT.Tests.Desktop.Integration/EndToEnd/Fixtures/DesktopE2ETestFixture.cs`

**Step 1: 复制 Fixture 并更新 namespace**

Namespace: `LYBT.Desktop.IntegrationTests.EndToEnd.Fixtures` → `LYBT.Tests.Desktop.Integration.EndToEnd.Fixtures`

**Step 2: 在 `CreateServiceProvider()` 中添加 5 个缺失的 DI 注册**

在 `// 13. 模块特有的 Service` 区域后添加:

```csharp
// 14. 缺失的 Handler/Service 注册 (修复 27 个 ViewModel E2E 测试)
// Patient StatusHandler
services.AddScoped<IPatientStatusHandler, PatientStatusHandler>();

// Herb StatusHandler
services.AddScoped<IHerbStatusHandler, HerbStatusHandler>();

// Formula StatusHandler
services.AddScoped<IFormulaStatusHandler, FormulaStatusHandler>();

// Desktop Cache Manager (mock: 测试不需要真实缓存)
services.AddSingleton(Substitute.For<IDesktopCacheManager>());

// Cross-module: HerbSearchProvider (mock: 测试不需要跨模块搜索)
services.AddSingleton(Substitute.For<IHerbSearchProvider>());
```

**注意:** StatusHandler 可能有自己的依赖。需要先读取各 Handler 构造函数:
- 如果依赖已注册的 DataSource/Repository → 用真实实现
- 如果依赖未注册的外部服务 → 用 Mock

如果 StatusHandler 构造函数复杂且依赖链长，改用 Mock:
```csharp
services.AddSingleton(Substitute.For<IPatientStatusHandler>());
services.AddSingleton(Substitute.For<IHerbStatusHandler>());
services.AddSingleton(Substitute.For<IFormulaStatusHandler>());
```

**Build check:** `dotnet build tests/LYBT.Tests.Desktop.Integration/`
Expected: 0 errors

---

## Task 5: 迁移 passing E2E 测试 (18 tests, 3 files)

**Source:** `tests/IntegrationTests/Client/Desktop/LYBT.Desktop.IntegrationTests/EndToEnd/`

这些测试当前在源项目已通过 (不依赖缺失的 Handler)。

| 源文件 | 目标文件 | Tests |
|--------|---------|-------|
| `EndToEnd/Prescription/PrescriptionE2ETests.cs` | `EndToEnd/Prescription/PrescriptionE2ETests.cs` | 5 |
| `EndToEnd/MedicalCase/MedicalCaseAggregateE2ETests.cs` | `EndToEnd/MedicalCase/MedicalCaseAggregateE2ETests.cs` | 12 |
| `EndToEnd/BusinessFlow/BusinessFlowE2ETests.cs` | `EndToEnd/BusinessFlow/BusinessFlowE2ETests.cs` | 1 |

**Namespace change:** `LYBT.Desktop.IntegrationTests.EndToEnd.{Sub}` → `LYBT.Tests.Desktop.Integration.EndToEnd.{Sub}`

**特殊注意:**
- 这些文件引用 `DesktopE2ETestFixture` -- 确保 using 指向 Task 4 创建的新 Fixture
- MedicalCaseAggregateE2ETests 有 1 个测试在源项目失败 (`MultipleMedicalCases_SamePatient_IndependentAggregates`) -- 迁移后一起修复

**Build + Test check:** `dotnet test tests/LYBT.Tests.Desktop.Integration/ --filter "FullyQualifiedName~EndToEnd.Prescription or FullyQualifiedName~MedicalCaseAggregate or FullyQualifiedName~BusinessFlowE2E" -v q`
Expected: 17 passed, 1 failed (MedicalCaseAggregate 已知问题)

---

## Task 6: 迁移 ViewModel E2E 测试 (27 tests, 6 files)

**Source:** `tests/IntegrationTests/Client/Desktop/LYBT.Desktop.IntegrationTests/EndToEnd/`

| 源文件 | 目标文件 | Tests |
|--------|---------|-------|
| `EndToEnd/Patients/PatientE2ETests.cs` | `EndToEnd/Patients/PatientE2ETests.cs` | 6 |
| `EndToEnd/Herbs/HerbE2ETests.cs` | `EndToEnd/Herbs/HerbE2ETests.cs` | 4 |
| `EndToEnd/Formula/FormulaE2ETests.cs` | `EndToEnd/Formula/FormulaE2ETests.cs` | 5 |
| `EndToEnd/MedicalCase/MedicalCaseE2ETests.cs` | `EndToEnd/MedicalCase/MedicalCaseE2ETests.cs` | 4 |
| `EndToEnd/Navigation/NavigationFlowE2ETests.cs` | `EndToEnd/Navigation/NavigationFlowE2ETests.cs` | 4 |
| `EndToEnd/Users/UserE2ETests.cs` | `EndToEnd/Users/UserE2ETests.cs` | 3 |

**Namespace change:** `LYBT.Desktop.IntegrationTests.EndToEnd.{Sub}` → `LYBT.Tests.Desktop.Integration.EndToEnd.{Sub}`

**Build + Test check (关键):** `dotnet test tests/LYBT.Tests.Desktop.Integration/ --filter "FullyQualifiedName~EndToEnd.Patients or FullyQualifiedName~EndToEnd.Herbs or FullyQualifiedName~EndToEnd.Formula or FullyQualifiedName~MedicalCaseE2ETests or FullyQualifiedName~Navigation or FullyQualifiedName~EndToEnd.Users" -v q`

**如果 Task 4 的 DI 修复正确:** Expected 26 passed, 0-1 failed
**如果仍有 DI 问题:** 分析错误，补充缺失注册

---

## Task 7: 修复残留失败 + 全量验证

**Step 1:** 修复 Task 5 和 Task 6 中发现的任何失败
- MedicalCaseAggregateE2ETests 的 1 个已知失败 -- 分析根因修复
- 任何新发现的 DI/接口不匹配 -- 逐个修复

**Step 2:** 全量验证
Run: `dotnet test tests/LYBT.Tests.Desktop.Integration/ -v q`
Expected: ~95 passed, 0 failed

**验证清单:**
- [ ] 编译 0 错误
- [ ] 原 24 个 Target A 测试无回归
- [ ] Foundation 20 个新测试通过
- [ ] LoginFlow 7 个新测试通过
- [ ] Passing E2E 18 个新测试通过 (含 Aggregate 修复)
- [ ] ViewModel E2E 26 个新测试通过 (DI 修复后)
- [ ] 没有引入重复测试

---

## 风险与缓解

| 风险 | 缓解 |
|------|------|
| StatusHandler 依赖链复杂 | 优先用 Mock，仅在需要真实行为时用实现 |
| WPF STA 线程问题 | DesktopE2ETestFixture.InitializeWpf() 已处理 |
| ViewModel 构造函数新增更多依赖 | 运行测试，根据错误逐步补充注册 |
| MedicalCaseAggregate 1 个已知失败 | 独立分析，可能是数据隔离问题 |
| csproj 依赖膨胀 | net8.0-windows 项目已较重，可接受 |

---

## 预计时间

| Task | 文件数 | 预计 |
|------|--------|------|
| Task 1 (csproj) | 1 | 3 min |
| Task 2 (Foundation) | 3 | 5 min |
| Task 3 (LocalMode + LoginFlow) | 2 | 5 min |
| Task 4 (E2E Fixture + DI fix) | 1 | 10 min |
| Task 5 (passing E2E) | 3 | 5 min |
| Task 6 (ViewModel E2E) | 6 | 8 min |
| Task 7 (修复 + 验证) | - | 15 min |
| **Total** | **16** | **~51 min** |
