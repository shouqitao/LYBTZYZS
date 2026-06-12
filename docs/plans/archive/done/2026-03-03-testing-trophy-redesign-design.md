# Testing Trophy 测试架构重新设计

## Date: 2026-03-03
## Status: APPROVED
## Strategy: Testing Trophy (方案 B -- 消灭 Mock)

---

## Background

Phase 1-5 审计修复了 26 个偏差中的 20 个，但根因未消除：Mock 是偏差的结构性来源。只要存在 mock-heavy 服务层测试，偏差就会随代码演进不断漂移。

行业共识 (2025/2026): Testing Trophy 模式 -- 以集成测试为核心，最小化 mock 使用。

参考资料:
- Milan Jovanovic: "After writing thousands of tests, I no longer believe unit testing for application use cases is the best approach"
- Jimmy Bogard: Vertical Slice Testing
- Martin Fowler: Testing shapes

---

## Design Goals

1. **Zero Mock for Server Business Logic** -- 服务层测试全部走真实 HTTP 管线
2. **Minimal Mock for Desktop** -- 仅保留 WPF Runtime 边界的 mock (16 个接口，含 Shell/硬件/远程/I/O 边界)
3. **Database Engine Consistency** -- 所有测试使用 SQL Server (消除 InMemory/SQLite 行为差异)
4. **Per-Test Data Isolation** -- Respawn 在每个测试前重置数据
5. **Real Authentication** -- 通过真实登录获取 JWT，不硬编码
6. **Structural Prevention** -- 架构测试防止 mock 回流

---

## Architecture Overview

### Testing Trophy Structure

```
                    +---------------+
                    |   E2E (少)     |  Desktop 关键流程
                    +-------+-------+
               +------------+------------+
               |  Integration (核心)      |  全部业务逻辑
               |  SQL Server + WAF       |  Respawn 重置
               +------------+------------+
          +------------------+------------------+
          |  Unit Tests (纯逻辑，无 mock)         |  实体、验证器、映射
          +------------------+------------------+
     +------------------------+------------------------+
     |  Static Analysis (架构约束 + Anti-Mock Rules)     |
     +---------------------------------------------------+
```

### Project Structure: 5 -> 3

```
tests/
+-- LYBT.Tests.Server/              (net8.0)    <- 合并 Unit + Integration
|   +-- _Infrastructure/            Respawn + WAF + ServerFixture
|   |   +-- ServerFixture.cs        唯一的 Fixture (本地 SQL Server)
|   |   +-- TestDataSeeder.cs       声明式种子数据
|   |   +-- IntegrationTestBase.cs  所有集成测试的基类
|   |   +-- ITestDatabaseProvider.cs 抽象接口 (预留 Testcontainers)
|   |   +-- LocalSqlServerProvider.cs 当前实现
|   +-- Auth/                       认证全场景 (真实 HTTP)
|   +-- Users/                      用户管理全场景
|   +-- Patients/                   患者管理全场景
|   +-- MedicalCases/               医案全生命周期
|   +-- Herbs/                      药材 CRUD + 引用保护
|   +-- Formulas/                   验方 CRUD + 详情
|   +-- RateLimiting/               速率限制专项 (独立 Fixture)
|   +-- PureLogic/                  纯逻辑: 实体状态机、验证器、映射
|
+-- LYBT.Tests.Desktop/             (net8.0-windows) <- 合并 Unit + Integration
|   +-- _Infrastructure/
|   |   +-- DesktopFixture.cs       SQLite + 真实 Repository + 最小 Mock
|   |   +-- ViewModelTestBase.cs    ViewModel 测试基类
|   +-- ViewModels/                 ViewModel 行为测试
|   +-- LocalData/                  本地数据层 E2E
|   +-- PureLogic/                  纯逻辑: 转换器、验证、工具
|
+-- LYBT.Tests.Architecture/        (net8.0-windows) <- 保持独立
|   +-- LayerDependencyTests.cs
|   +-- NamingConventionTests.cs
|   +-- SecurityAttributeTests.cs
|   +-- AntiMockRuleTests.cs        NEW: 防止 mock 回流
|
+-- TestConfiguration/              共享库 (精简)
    +-- Assertions/                 FluentAssertions 扩展
    +-- Builders/                   测试数据 Builder
    +-- Constants/                  共享常量
```

---

## Infrastructure Layer

### ServerFixture

```
ServerFixture (IAsyncLifetime)
|
+-- InitializeAsync()
|   +-- 1. 连接本地 SQL Server (localhost)
|   +-- 2. 创建独立测试数据库 (LYBT_Test_{timestamp})
|   +-- 3. WebApplicationFactory<Program> (动态注入连接字符串)
|   +-- 4. MigrateAsync() (一次性，整个测试套件)
|   +-- 5. Respawner.CreateAsync() (一次性分析表依赖)
|   +-- 6. 种子基础数据 (通过 API 调用，走生产路径)
|
+-- ResetDatabaseAsync()         <- 每个测试前调用
|   +-- Respawn DELETE (保留 schema, ~50-100ms)
|   +-- 重新种子基础数据
|
+-- LoginAsAsync(role)           <- 真实登录获取 token
|   +-- POST /api/v1/auth/login -> 真实 JWT token
|
+-- DisposeAsync()
    +-- EnsureDeletedAsync() (清理测试数据库)
```

### Key Differences from Current WebApiFixture

| Dimension | Current WebApiFixture | New ServerFixture |
|-----------|----------------------|-------------------|
| DB Reset | EnsureDeleted + Migrate (per suite) | Respawn (per test, ~50-100ms) |
| User Setup | Manual UserEntity + direct DB write | API calls or production seed service |
| Password | Direct BCrypt hash assignment | PasswordHelper.HashPassword() |
| Token | Hardcoded key JWT generation | Real POST /api/v1/auth/login |
| Test Isolation | None (data accumulates) | Full (Respawn per test) |

### Testcontainers Readiness

```csharp
interface ITestDatabaseProvider : IAsyncLifetime
{
    string ConnectionString { get; }
}

class LocalSqlServerProvider : ITestDatabaseProvider { ... }    // Current
// class TestcontainersSqlServerProvider : ITestDatabaseProvider { ... }  // Future
```

### DesktopFixture

```
DesktopFixture (IAsyncLifetime)
|
+-- Real Components:
|   +-- SQLite InMemory AppDbContext
|   +-- All Repository (real implementations)
|   +-- All DataSource (real implementations)
|   +-- AuthenticationService (real + SQLite)
|   +-- PaginationService / AsyncExecutor
|   +-- IConfiguration (real appsettings)
|   +-- EventAggregator (real Prism implementation)
|
+-- Minimal Mocks (WPF boundary only):
|   +-- IRegionManager -> Substitute (navigation recording)
|   +-- IDialogService -> Substitute (preset results)
|   +-- IDialogManager -> Substitute (returns true)
|   +-- ICurrentUserProvider -> Substitute (fixed user)
|
+-- ViewModel Creation:
    +-- Via DI container (production-consistent)
```

---

## Server Test Migration: Mock Elimination

### Tests to Delete (Mock-Heavy)

| Test File | Mock Count | Replacement |
|-----------|-----------|-------------|
| AuthServiceTests | 5 mocks | Auth integration tests |
| UserServiceTests | 4 mocks | Users integration tests |
| PatientServiceTests | 3 mocks | Patients integration tests |
| MedicalCaseCommandServiceTests | 8 mocks | MedicalCases integration tests |
| HerbServiceTests | 3 mocks | Herbs integration tests |
| FormulaServiceTests | 3 mocks | Formulas integration tests |

### Tests to Keep (Pure Logic)

| Category | Examples | Reason |
|----------|----------|--------|
| Entity State Machine | MedicalCaseEntity.Complete(), .Suspend() | Pure domain logic |
| FluentValidation | UserInputDtoValidatorTests | Pure validation rules |
| Mapping Logic | DTO <-> Entity mapping | Pure transformation |
| Utility Functions | PasswordHelper, PaginationService | Pure computation |
| Configuration | PasswordPolicyOptions validation | Pure logic |

### New Integration Test Scenarios

| Module | Scenario | Priority |
|--------|----------|----------|
| Auth | Real login -> token refresh -> token revoke chain | CRITICAL |
| Auth | ForceChangeOnFirstLogin full flow | HIGH |
| Auth | Password policy validation (length/complexity/history) | HIGH |
| MedicalCase | State machine full path: Active->Suspended->Active->Completed | HIGH |
| MedicalCase | Concurrent edit conflict (RowVersion) | MEDIUM |
| Users | Pagination boundary (page=0, pageSize=0, oversized) | MEDIUM |
| Herbs | Reference protection: delete herb referenced by prescription | HIGH |

---

## Desktop Test Design

### Mock Usage Rules

| Allowed Mock | Forbidden Mock |
|-------------|----------------|
| IRegionManager (WPF navigation) | IXxxRepository (use real DB) |
| IDialogService (WPF dialogs) | IXxxService (use real impl) |
| IDialogManager (project dialog) | IXxxCrossModuleService (use real) |
| ICurrentUserProvider (audit) | DbContext (use SQLite) |
| | IConfiguration (use real config) |

### ViewModel Test Pattern

```
Old (mock-heavy):
  var repoMock = Substitute.For<IPatientRepository>();
  repoMock.GetPagedAsync(...).Returns(fakePagedResult);
  var vm = new PatientListViewModel(repoMock, ...);

New (real data):
  await fixture.SeedPatients(5);
  var vm = fixture.Resolve<PatientListViewModel>();
  await vm.LoadCommand.ExecuteAsync();
  vm.Patients.Should().HaveCount(5);
```

### WPF-Specific Handling

| Issue | Solution |
|-------|----------|
| WPF Dispatcher absence | SynchronizationContext.SetSynchronizationContext(new()) |
| Application.Current null | WpfTestFixture minimal Application init |
| STA thread requirement | [StaFact] / [StaTheory] |
| Async command timeout | Real AsyncExecutor + reasonable timeout |

---

## Prevention Mechanisms

### Architecture Tests: AntiMockRuleTests

```
Rule 1: LYBT.Tests.Server must NOT reference NSubstitute package
Rule 2: LYBT.Tests.Desktop mock usage limited to whitelist interfaces
Rule 3: TestConfiguration.CreateMock<T>() marked [Obsolete] then deleted
```

### Code Review Checklist

- Server tests: requests via HTTP? (no direct new Service())
- Desktop tests: mocks only whitelist interfaces?
- No Verify() calls? (mock verification is anti-pattern)
- Test data seeded within test? (not dependent on other tests)

---

## Migration Execution Plan

### Phase 1: Infrastructure Setup (2 days)
- 1.1 Create LYBT.Tests.Server project + ServerFixture
- 1.2 Add Respawn NuGet package
- 1.3 Implement ITestDatabaseProvider (LocalSqlServer)
- 1.4 Implement ResetDatabaseAsync + seed data (production path)
- 1.5 Implement LoginAsAsync (real login)
- 1.6 Verify: 1 smoke test passes

### Phase 2: Server Test Migration (3 days)
- 2.1 Auth module: login/refresh/revoke/ForceChange
- 2.2 Users module: CRUD + validation + pagination
- 2.3 Patients module: CRUD + status filter
- 2.4 MedicalCases module: full lifecycle + state machine
- 2.5 Herbs module: CRUD + reference protection
- 2.6 Formulas module: CRUD + details
- 2.7 Delete old mock-heavy test files

### Phase 3: Desktop Test Refactoring (2 days)
- 3.1 Create LYBT.Tests.Desktop project + DesktopFixture
- 3.2 Migrate ViewModel tests (eliminate Repository mocks)
- 3.3 Migrate local data layer E2E tests
- 3.4 Delete old Desktop test projects

### Phase 4: Cleanup + Prevention (1 day)
- 4.1 Delete old test projects (5 -> 3)
- 4.2 Slim down TestConfiguration (delete TestBase, IntegrationTestBase)
- 4.3 Add AntiMockRuleTests architecture tests
- 4.4 Update CLAUDE.md and project documentation
- 4.5 Full validation (build + all tests)

---

## Risk Mitigation

| Risk | Mitigation |
|------|-----------|
| SQL Server 2012 EF Core 8 feature gaps | Verify Migration compatibility in Phase 1 |
| Respawn SQL Server 2012 compatibility | Check minimum version; fallback if needed |
| Test count decrease concerns | Integration tests cover more real paths, higher confidence |
| Desktop mock whitelist too strict | Whitelist extendable with documented justification |
| Performance regression (more DB operations) | Respawn is fast (~50-100ms); parallel test execution |

---

## Success Criteria

- [x] Server zero mock (NSubstitute not in Server project dependencies)
- [x] Desktop mock limited to whitelist (16 WPF/边界 interfaces, expanded from initial 5)
- [x] All tests pass (2021 tests, exceeds 1800+ target)
- [x] Per-test data isolation (Respawn)
- [x] Authentication via real login (no hardcoded JWT)
- [x] Architecture tests prevent mock creep (AntiMockRuleTests)

---

## Key Metrics

| Metric | Before | Target | Actual |
|--------|--------|--------|--------|
| Test projects | 5 + 1 shared | 3 + 1 shared | 3 (TestConfiguration deleted) |
| Total tests | ~2387 | ~1800-2000 | 2021 |
| Mock usage (Server) | ~200+ | 0 | 0 |
| Mock usage (Desktop) | ~200+ | <30 | 35 occurrences (16 interfaces) |
| DB engine types | 3 (InMemory/SQLite/SQL Server) | 1 | 2 (SQL Server + SQLite for Desktop) |
| Test data isolation | None (shared fixture) | Full | Full (Respawn per test) |

---

Last updated: 2026-03-04
