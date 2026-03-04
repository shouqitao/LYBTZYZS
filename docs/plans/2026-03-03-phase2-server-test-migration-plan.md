# Phase 2: Server Test Migration - Detailed Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 将 `LYBT.Tests.Server.Integration` 的全部集成测试迁移到 `LYBT.Tests.Server` (新 ServerFixture + Respawn)，并将 mock-heavy 单元测试替换为新的集成测试。

**Architecture:** 从旧 `WebApiFixture` (固定DB + 伪造JWT) 迁移到新 `ServerFixture` (唯一DB + Respawn + 真实登录)。迁移完成后 `LYBT.Tests.Server` 包含所有服务端测试。

**Tech Stack:** xunit, Respawn 7.x, FluentAssertions, WebApplicationFactory, SQL Server 2012 (local)

**Parent Plan:** docs/plans/2026-03-03-testing-trophy-redesign-plan.md (Phase 2 section)

---

## Migration Pattern Reference (所有 Task 通用)

### Code Transformation Rules

```csharp
// === 1. 类声明 ===
// 旧:
[Collection("ServerIntegration")]
public class XxxTests
{
    private readonly WebApiFixture _fixture;
    public XxxTests(WebApiFixture fixture) { _fixture = fixture; }
}
// 新:
public sealed class XxxTests : IntegrationTestBase
{
    public XxxTests(ServerFixture fixture) : base(fixture) { }
}

// === 2. 认证客户端 ===
// 旧: 预建属性 (伪造JWT, 整个fixture共享)
var response = await _fixture.AdminClient.GetAsync("/api/v1/xxx");
var response = await _fixture.DoctorClient.PostAsJsonAsync("/api/v1/xxx", req);
var response = await _fixture.SysAdminClient.DeleteAsync("/api/v1/xxx");
// 新: 每测试真实登录
var admin = await LoginAsAdminAsync();
var response = await admin.GetAsync("/api/v1/xxx");
var doctor = await LoginAsDoctorAsync();
var response = await doctor.PostAsJsonAsync("/api/v1/xxx", req);
var sysAdmin = await LoginAsSysAdminAsync();
var response = await sysAdmin.DeleteAsync("/api/v1/xxx");

// === 3. 匿名客户端 ===
// 旧:
var response = await _fixture.AnonymousClient.GetAsync(...);
// 新:
var response = await AnonymousClient.GetAsync(...);

// === 4. 数据种子 ===
// 旧:
await _fixture.SeedAsync(async db => { db.Add(entity); await db.SaveChangesAsync(); });
var result = await _fixture.SeedAsync(async db => { ...; return entity; });
// 新:
await Fixture.WithDbContextAsync(async db => { db.Add(entity); await db.SaveChangesAsync(); });
var result = await Fixture.WithDbContextAsync(async db => { ...; return entity; });

// === 5. 动态角色客户端 ===
// 旧: 伪造任意角色JWT
using var client = _fixture.CreateClientAs(UserRole.Receptionist, userId, "test_receptionist");
// 新: 先创建用户，再真实登录
var sysAdmin = await LoginAsSysAdminAsync();
var username = $"receptionist_{Guid.NewGuid():N}"[..20];
await sysAdmin.PostAsJsonAsync("/api/v1/users", new
{
    UserName = username, Password = "TestPass2025@",
    RealName = "测试前台", Role = "Receptionist", Email = $"{username}@test.com"
});
var client = await Fixture.LoginAsAsync(username, "TestPass2025@");

// === 6. Namespace ===
// 旧: namespace LYBT.Tests.Server.Integration.Auth;
// 新: namespace LYBT.Tests.Server.Auth;

// === 7. Using 语句 ===
// 新增:
using LYBT.Tests.Server.Infrastructure;
// 移除:
// using LYBT.Tests.Server.Integration.Fixtures;

// === 8. JsonOptions (保持不变) ===
private static readonly JsonSerializerOptions JsonOptions = new()
{
    PropertyNameCaseInsensitive = true,
    Converters = { new JsonStringEnumConverter() }
};

// === 9. GetService<T>() ===
// 旧:
var service = _fixture.GetService<ISomeService>();
// 新:
using var scope = Fixture.Services.CreateScope();
var service = scope.ServiceProvider.GetRequiredService<ISomeService>();
```

### Verify Command Template

每个 Task 完成后运行:
```bash
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~<ModuleName>" -v normal --no-build
```

---

## Task 2.1: Migrate Auth Integration Tests

**Files:**
- Read: `tests/LYBT.Tests.Server.Integration/Auth/AuthIntegrationTests.cs` (~15 tests)
- Read: `tests/LYBT.Tests.Server.Integration/Auth/AuthTokenAdvancedIntegrationTests.cs`
- Create: `tests/LYBT.Tests.Server/Auth/AuthIntegrationTests.cs`
- Create: `tests/LYBT.Tests.Server/Auth/AuthTokenAdvancedTests.cs`

**Step 1: Copy and adapt AuthIntegrationTests.cs**

Copy `tests/LYBT.Tests.Server.Integration/Auth/AuthIntegrationTests.cs` to `tests/LYBT.Tests.Server/Auth/AuthIntegrationTests.cs`.

Apply transformation rules:
- namespace: `LYBT.Tests.Server.Integration.Auth` -> `LYBT.Tests.Server.Auth`
- Remove `[Collection("ServerIntegration")]`
- Change class to `public sealed class AuthIntegrationTests : IntegrationTestBase`
- Constructor: `public AuthIntegrationTests(ServerFixture fixture) : base(fixture) { }`
- Replace `_fixture.AdminClient` -> `await LoginAsAdminAsync()` (declare `var admin = ...` at test start)
- Replace `_fixture.DoctorClient` -> `await LoginAsDoctorAsync()`
- Replace `_fixture.SysAdminClient` -> `await LoginAsSysAdminAsync()`
- Replace `_fixture.AnonymousClient` -> `AnonymousClient`
- Replace `_fixture.SeedAsync(...)` -> `Fixture.WithDbContextAsync(...)`
- Add `using LYBT.Tests.Server.Infrastructure;`

**Key Auth-specific concerns:**
- Old tests use `_fixture.AdminClient` which has pre-forged JWT. New tests do real login per test.
- Token refresh tests: old tests call login via `AnonymousClient.PostAsJsonAsync` to get initial token. This pattern still works.
- Rate limiting is disabled in test config, so login flood tests are not affected.
- `PasswordHelper.HashPassword(password, role)` is used by ServerFixture seed - matches production.

**Step 2: Copy and adapt AuthTokenAdvancedIntegrationTests.cs**

Same migration pattern. This file tests:
- Token expiration edge cases
- Concurrent refresh token usage
- Session info retrieval

**Step 3: Build and verify**

```bash
dotnet build tests/LYBT.Tests.Server/LYBT.Tests.Server.csproj
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~Auth" -v normal
```

Expected: All auth tests pass (smoke tests 3 + migrated ~18 tests = ~21 total).

**Troubleshooting:**
- If `CreateClientAs` is used in old tests -> replace with create-user-then-login pattern (see Migration Pattern Reference rule #5)
- If `_fixture.GetDbContext()` is used -> replace with `Fixture.WithDbContextAsync(...)`
- If hardcoded user IDs are compared -> use `Fixture.AdminUserId` / `Fixture.DoctorUserId` (public static fields, or find actual ID via GET /users/current)

**Step 4: Commit**

```bash
git add tests/LYBT.Tests.Server/Auth/
git commit -m "test: migrate Auth integration tests to Testing Trophy (Phase 2.1)"
```

---

## Task 2.2: Migrate Users Integration Tests

**Files:**
- Read: `tests/LYBT.Tests.Server.Integration/Users/UserIntegrationTests.cs` (~28 tests)
- Create: `tests/LYBT.Tests.Server/Users/UserIntegrationTests.cs`

**Step 1: Copy and adapt UserIntegrationTests.cs**

Apply standard transformation rules (see Migration Pattern Reference).

**Users-specific concerns:**
- `ResetPassword` requires `SuperAdminOnly` policy -> use `await LoginAsSysAdminAsync()`
- `Restore` requires `SuperAdminOnly` -> use `await LoginAsSysAdminAsync()`
- `ChangePassword` endpoint: `/api/v1/users/{id}/change-password` uses `ChangePasswordRequest` (not `ChangePasswordDto`)
- Last-admin protection: tests that verify "cannot delete last admin" need careful attention:
  - Old fixture seeds admin with fixed ID `00000000-0001`. New fixture creates admin via API with same username.
  - Test should GET /users to find admin's real ID, not hardcode.
- `BatchDelete`/`BatchEnable`/`BatchDisable`: body is `BatchDeleteInputDto { Ids: List<Guid> }`
- `CreateClientAs(UserRole.Receptionist, ...)` appears in permission tests -> use create-user-then-login pattern

**Step 2: Handle dynamic role tests**

For tests that use `_fixture.CreateClientAs(role, id, name)`:

```csharp
// Pattern for creating a dynamic test user with specific role
private async Task<HttpClient> CreateAndLoginUserAsync(string role, string? username = null)
{
    var sysAdmin = await LoginAsSysAdminAsync();
    var name = username ?? $"test_{role.ToLower()}_{Guid.NewGuid():N}"[..20];
    var password = "TestPass2025@";

    var createResponse = await sysAdmin.PostAsJsonAsync("/api/v1/users", new
    {
        UserName = name,
        Password = password,
        RealName = $"测试{role}",
        Role = role,
        Email = $"{name}@test.com"
    });
    createResponse.EnsureSuccessStatusCode();

    return await Fixture.LoginAsAsync(name, password);
}
```

Consider adding this as a protected method in `IntegrationTestBase` if many modules need it.

**Step 3: Build and verify**

```bash
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~Users" -v normal
```

Expected: ~28 tests pass.

**Step 4: Commit**

```bash
git add tests/LYBT.Tests.Server/Users/
git commit -m "test: migrate Users integration tests to Testing Trophy (Phase 2.2)"
```

---

## Task 2.3: Migrate Patients Integration Tests

**Files:**
- Read: `tests/LYBT.Tests.Server.Integration/Patients/PatientIntegrationTests.cs` (~24 tests)
- Create: `tests/LYBT.Tests.Server/Patients/PatientIntegrationTests.cs`

**Step 1: Copy and adapt PatientIntegrationTests.cs**

Apply standard transformation rules.

**Patients-specific concerns:**
- `PatientAccess` policy includes Receptionist -> some tests may need Receptionist client (use create-user-then-login pattern)
- Non-Admin users only see Enabled patients (controller filters by role)
- Age calculation is server-side computed from BirthDate -> verify computed `Age` in response
- Ownership check: some operations are ownership-protected (Update, Delete for non-admin)
- PinYin search: keyword search matches Chinese name via PinYin code
- `CheckReference` / `BatchCheckReference`: verify patient has no medical case references before deletion
- Import/Export endpoints use `IFormFile` / xlsx format -> may skip these in initial migration or test via direct DB seed

**Step 2: Build and verify**

```bash
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~Patients" -v normal
```

Expected: ~24 tests pass.

**Step 3: Commit**

```bash
git add tests/LYBT.Tests.Server/Patients/
git commit -m "test: migrate Patients integration tests to Testing Trophy (Phase 2.3)"
```

---

## Task 2.4: Migrate MedicalCases Integration Tests

**Files:**
- Read: `tests/LYBT.Tests.Server.Integration/MedicalCases/MedicalCaseIntegrationTests.cs` (~22 tests)
- Read: `tests/LYBT.Tests.Server.Integration/MedicalCases/MedicalCasePermissionAndFilterTests.cs`
- Read: `tests/LYBT.Tests.Server.Integration/MedicalCases/PrescriptionAggregateTests.cs`
- Create: `tests/LYBT.Tests.Server/MedicalCases/MedicalCaseIntegrationTests.cs`
- Create: `tests/LYBT.Tests.Server/MedicalCases/MedicalCasePermissionTests.cs`
- Create: `tests/LYBT.Tests.Server/MedicalCases/PrescriptionAggregateTests.cs`

**Step 1: Copy and adapt all MedicalCase test files**

Apply standard transformation rules to each file.

**MedicalCases-specific concerns (MOST COMPLEX MODULE):**

1. **CreateMedicalCase is Doctor-only** (`[Roles = Doctor]`):
   - Admin cannot create medical cases
   - All create tests must use `await LoginAsDoctorAsync()`

2. **State machine transitions** (Active -> Suspended -> Active -> Completed):
   - `PUT /medicalcases/{id}/suspend` requires `ConsultationInputDto?` body
   - `PUT /medicalcases/{id}/status` with `UpdateStatusRequest { Status }` for status changes
   - `PUT /medicalcases/{id}/close` skips workflow validation

3. **Aggregate Save** (`PUT /medicalcases/{id}`):
   - Body is `MedicalCaseInputDto` with nested `ConsultationInputDto?` + `PrescriptionInputDto?`
   - Single transaction for Consultation + Prescription update
   - `PrescriptionInputDto.Items` contains `PrescriptionItemInputDto` with `HerbId`, `Dosage`, etc.

4. **Business Rule BR-001**: One active case per patient:
   - Creating a second case while one is Active should return 400/409
   - After completing/canceling the first case, a new case can be created

5. **Business Rule AR-003**: One prescription per consultation (一诊一方):
   - Creating a second prescription for same case should fail

6. **Permission model** (`MedicalCasePermissionDto`):
   - `CanEdit` / `CanDelete` depends on role, status, and ownership
   - Completed cases require `EditReason` for admin edits

7. **Pre-requisites**: MedicalCase tests need:
   - A patient (create via `/api/v1/patients`)
   - A herb (for prescription items, create via `/api/v1/herbs`)

**Step 2: Create helper methods for test data setup**

Add a protected helper in each test class (or in IntegrationTestBase):

```csharp
/// <summary>
/// 创建测试患者并返回其 ID。
/// </summary>
private async Task<Guid> CreateTestPatientAsync(HttpClient client, string? name = null)
{
    var patientName = name ?? $"患者_{Guid.NewGuid():N}"[..8];
    var response = await client.PostAsJsonAsync("/api/v1/patients", new
    {
        Name = patientName,
        Gender = "Male",
        BirthDate = "1990-01-01"
    });
    response.EnsureSuccessStatusCode();
    var result = await response.Content.ReadFromJsonAsync<ApiResponse<PatientDetailDto>>(JsonOptions);
    return result!.Data!.Id;
}

/// <summary>
/// 创建测试医案并返回其 ID。
/// </summary>
private async Task<Guid> CreateTestMedicalCaseAsync(HttpClient doctorClient, Guid patientId)
{
    var response = await doctorClient.PostAsJsonAsync("/api/v1/medicalcases", new
    {
        PatientId = patientId,
        Consultation = new
        {
            PresentIllness = "测试主诉",
            TcmDiagnosis = "测试中医诊断"
        }
    });
    response.EnsureSuccessStatusCode();
    var result = await response.Content.ReadFromJsonAsync<ApiResponse<MedicalCaseDetailDto>>(JsonOptions);
    return result!.Data!.Id;
}

/// <summary>
/// 创建测试药材并返回其 ID。
/// </summary>
private async Task<Guid> CreateTestHerbAsync(HttpClient adminClient, string? name = null)
{
    var herbName = name ?? $"药材_{Guid.NewGuid():N}"[..8];
    var response = await adminClient.PostAsJsonAsync("/api/v1/herbs", new
    {
        Name = herbName,
        Unit = "g",
        Price = 10.0m,
        Category = "补益药"
    });
    response.EnsureSuccessStatusCode();
    var result = await response.Content.ReadFromJsonAsync<ApiResponse<HerbDetailDto>>(JsonOptions);
    return result!.Data!.Id;
}
```

**Step 3: Build and verify**

```bash
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~MedicalCases" -v normal
```

Expected: ~40+ tests pass (migrated from 3 files).

**Troubleshooting:**
- `MedicalCaseInputDto` field names may differ from old tests - check exact DTO definition
- Prescription items need real `HerbId` -> create herb first via API
- State transitions may require specific order (Active -> only can go to Suspended/Completed/Cancelled)

**Step 4: Commit**

```bash
git add tests/LYBT.Tests.Server/MedicalCases/
git commit -m "test: migrate MedicalCases integration tests to Testing Trophy (Phase 2.4)"
```

---

## Task 2.5: Migrate Herbs Integration Tests

**Files:**
- Read: `tests/LYBT.Tests.Server.Integration/Herbs/HerbIntegrationTests.cs` (~17 tests)
- Create: `tests/LYBT.Tests.Server/Herbs/HerbIntegrationTests.cs`

**Step 1: Copy and adapt HerbIntegrationTests.cs**

Apply standard transformation rules.

**Herbs-specific concerns:**
- `DoctorOrAdmin` policy -> both admin and doctor can access
- Reference protection: herb used in a PrescriptionItem cannot be deleted
  - Need to create full chain: patient -> medical case -> save prescription with herb -> try delete herb
- `HerbInputDto` requires: `Name`, `Unit`, `Price`. Optional: `Category`, `CostPrice`, `Properties`, etc.
- `ToggleStatus` / `Restore` follow standard patterns
- `BatchImport` (`POST /herbs/batch-import`) takes `HerbBatchImportInputDto` with `DuplicateStrategy`
- `CheckReference` / `BatchCheckReference`: verify herb has no prescription item references

**Step 2: Build and verify**

```bash
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~Herbs" -v normal
```

Expected: ~17 tests pass.

**Step 3: Commit**

```bash
git add tests/LYBT.Tests.Server/Herbs/
git commit -m "test: migrate Herbs integration tests to Testing Trophy (Phase 2.5)"
```

---

## Task 2.6: Migrate Formulas + Remaining Integration Tests

**Files:**
- Read: `tests/LYBT.Tests.Server.Integration/Formulas/FormulaIntegrationTests.cs` (~16 tests)
- Read: `tests/LYBT.Tests.Server.Integration/Formulas/FormulaServiceIntegrationTests.cs`
- Read: `tests/LYBT.Tests.Server.Integration/Sync/SyncIntegrationTests.cs` (~25 tests)
- Read: `tests/LYBT.Tests.Server.Integration/Health/HealthCheckIntegrationTests.cs`
- Read: `tests/LYBT.Tests.Server.Integration/Middleware/CorrelationIdMiddlewareIntegrationTests.cs`
- Read: `tests/LYBT.Tests.Server.Integration/Diagnostics/DiagnosticsControllerIntegrationTests.cs`
- Read: `tests/LYBT.Tests.Server.Integration/Compatibility/ApiResponseContractTests.cs`
- Read: `tests/LYBT.Tests.Server.Integration/Performance/PerformanceTests.cs`
- Create: `tests/LYBT.Tests.Server/Formulas/FormulaIntegrationTests.cs`
- Create: `tests/LYBT.Tests.Server/Formulas/FormulaServiceIntegrationTests.cs`
- Create: `tests/LYBT.Tests.Server/Sync/SyncIntegrationTests.cs`
- Create: `tests/LYBT.Tests.Server/Health/HealthCheckTests.cs`
- Create: `tests/LYBT.Tests.Server/Middleware/CorrelationIdTests.cs`
- Create: `tests/LYBT.Tests.Server/Diagnostics/DiagnosticsTests.cs`
- Create: `tests/LYBT.Tests.Server/Compatibility/ApiResponseContractTests.cs`
- Create: `tests/LYBT.Tests.Server/Performance/PerformanceTests.cs`

**Step 1: Migrate Formulas tests**

Apply standard transformation rules.

**Formulas-specific concerns:**
- `DoctorOrAdmin` policy; Doctor sees own + shared formulas, Admin sees all
- `FormulaInputDto` has nested `Items: List<FormulaHerbItemInputDto>`:
  - `FormulaHerbItemInputDto.HerbId` is nullable (late-binding support)
  - `HerbName` is required even when `HerbId` is provided
- `PendingValidation` endpoint: formulas with unbound herb references
- `ValidateHerb` endpoint: manually bind a herb to a formula item
- Ownership check: only creator or admin can update/delete

**Step 2: Migrate Sync tests**

- Sync tests are typically larger (~25 tests) covering metadata/compare/upload/download
- Reference-protected delete scenarios involve cross-module data
- These tests may need significant data setup (patients + medical cases + herbs)

**Step 3: Migrate remaining small test files**

Health, Middleware, Diagnostics, Compatibility, Performance tests are typically simpler (few tests each, basic HTTP checks). Apply standard rules.

**Step 4: Build and verify**

```bash
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~Formulas or FullyQualifiedName~Sync or FullyQualifiedName~Health or FullyQualifiedName~Middleware or FullyQualifiedName~Diagnostics or FullyQualifiedName~Compatibility or FullyQualifiedName~Performance" -v normal
```

**Step 5: Commit**

```bash
git add tests/LYBT.Tests.Server/Formulas/ tests/LYBT.Tests.Server/Sync/ tests/LYBT.Tests.Server/Health/ tests/LYBT.Tests.Server/Middleware/ tests/LYBT.Tests.Server/Diagnostics/ tests/LYBT.Tests.Server/Compatibility/ tests/LYBT.Tests.Server/Performance/
git commit -m "test: migrate Formulas, Sync and remaining integration tests to Testing Trophy (Phase 2.6)"
```

---

## Task 2.7: Migrate RateLimiting Tests (Separate Fixture)

**Files:**
- Read: `tests/LYBT.Tests.Server.Integration/Fixtures/RateLimitingFixture.cs`
- Read: `tests/LYBT.Tests.Server.Integration/Fixtures/RateLimitingCollection.cs`
- Read: `tests/LYBT.Tests.Server.Integration/RateLimiting/RateLimitingIntegrationTests.cs`
- Create: `tests/LYBT.Tests.Server/RateLimiting/RateLimitingFixture.cs`
- Create: `tests/LYBT.Tests.Server/RateLimiting/RateLimitingCollection.cs`
- Create: `tests/LYBT.Tests.Server/RateLimiting/RateLimitingTestBase.cs`
- Create: `tests/LYBT.Tests.Server/RateLimiting/RateLimitingTests.cs`

**Step 1: Create RateLimitingFixture**

RateLimiting needs a separate fixture because it requires `Security:RateLimiting:Enabled = true` (main ServerFixture disables it).

```csharp
// tests/LYBT.Tests.Server/RateLimiting/RateLimitingFixture.cs
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace LYBT.Tests.Server.RateLimiting;

/// <summary>
/// 独立 Fixture: 启用速率限制的 WAF 实例。
/// 使用独立数据库避免与主 ServerFixture 冲突。
/// </summary>
public sealed class RateLimitingFixture : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private readonly Infrastructure.LocalSqlServerProvider _dbProvider = new();

    public HttpClient AnonymousClient { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _dbProvider.InitializeAsync();

        // Serilog freeze workaround: 第二个 WAF 实例需要重置 Logger
        Log.CloseAndFlush();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                builder.UseSetting("ConnectionStrings:DefaultConnection", _dbProvider.ConnectionString);
                builder.UseSetting("Security:RateLimiting:Enabled", "true");
                builder.UseSetting("Security:RateLimiting:LoginPermitLimit", "5");
                builder.UseSetting("Security:RateLimiting:LoginWindowSeconds", "60");
                builder.ConfigureServices(services =>
                {
                    // 替换 ILoggerFactory 避免 Serilog freeze 冲突
                    services.AddLogging(lb => lb.ClearProviders());
                });
            });

        AnonymousClient = _factory.CreateClient();

        // 运行迁移
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        AnonymousClient?.Dispose();
        await _factory.DisposeAsync();
        await _dbProvider.DisposeAsync();
    }
}
```

**Important:** `RateLimitingFixture` 的代码示例是参考模板。实际实现时需要:
1. 读取旧 `RateLimitingFixture.cs` 获取准确的 using/配置
2. 确认 `Serilog.Log.CloseAndFlush()` 时机和 `ILoggerFactory` 替换方式
3. 确认 rate limiting 配置键名 (`Security:RateLimiting:*`)

**Step 2: Create RateLimitingCollection + TestBase**

```csharp
// tests/LYBT.Tests.Server/RateLimiting/RateLimitingCollection.cs
namespace LYBT.Tests.Server.RateLimiting;

[CollectionDefinition("RateLimiting")]
public sealed class RateLimitingCollection : ICollectionFixture<RateLimitingFixture>;
```

```csharp
// tests/LYBT.Tests.Server/RateLimiting/RateLimitingTestBase.cs
namespace LYBT.Tests.Server.RateLimiting;

[Collection("RateLimiting")]
public abstract class RateLimitingTestBase
{
    protected RateLimitingFixture Fixture { get; }
    protected HttpClient AnonymousClient => Fixture.AnonymousClient;

    protected RateLimitingTestBase(RateLimitingFixture fixture) => Fixture = fixture;
}
```

**Step 3: Migrate RateLimiting tests**

Copy and adapt from `RateLimitingIntegrationTests.cs`:
- namespace update
- Collection name: `"RateLimiting"` (matches new collection)
- Use `RateLimitingTestBase` as base class

**Step 4: Build and verify**

```bash
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~RateLimiting" -v normal
```

**Step 5: Commit**

```bash
git add tests/LYBT.Tests.Server/RateLimiting/
git commit -m "test: migrate RateLimiting tests with separate fixture to Testing Trophy (Phase 2.7)"
```

---

## Task 2.8: Migrate Pure Logic Unit Tests

**Files:**
- Read + Copy: `tests/LYBT.Tests.Unit/Entities/**` -> `tests/LYBT.Tests.Server/PureLogic/Entities/`
- Read + Copy: `tests/LYBT.Tests.Unit/Shared/Validators/**` -> `tests/LYBT.Tests.Server/PureLogic/Validators/`
- Read + Copy: `tests/LYBT.Tests.Unit/Shared/ExceptionHandling/**` -> `tests/LYBT.Tests.Server/PureLogic/Shared/`
- Read + Copy: `tests/LYBT.Tests.Unit/Shared/Logging/**` -> `tests/LYBT.Tests.Server/PureLogic/Shared/`
- Read + Copy: `tests/LYBT.Tests.Unit/Shared/Models/**` -> `tests/LYBT.Tests.Server/PureLogic/Shared/`
- Read + Copy: `tests/LYBT.Tests.Unit/Shared/Configuration/**` -> `tests/LYBT.Tests.Server/PureLogic/Shared/`
- Read + Copy: `tests/LYBT.Tests.Unit/Infrastructure/Serialization/**` -> `tests/LYBT.Tests.Server/PureLogic/Infrastructure/`
- Read + Copy: `tests/LYBT.Tests.Unit/Utilities/**` -> `tests/LYBT.Tests.Server/PureLogic/Utilities/`

**Step 1: Identify pure logic tests**

Pure logic tests have zero or minimal mocks (only NullLogger). They test:
- Entity property calculations (MedicalCase.HasPrescription, Patient.Age, etc.)
- FluentValidation validators (UserInputDtoValidator, etc.)
- Utility functions (PasswordHelper, ChecksumHelper, etc.)
- Serialization (JSON converters, etc.)
- Configuration binding
- ExceptionHandling

**Rule:** If a test uses `Substitute.For<IXxxRepository>()` or `Substitute.For<IXxxService>()`, it is NOT pure logic and is replaced by integration tests.

**Step 2: Copy files, update namespaces**

For each file:
- `namespace LYBT.Tests.Unit.XXX` -> `namespace LYBT.Tests.Server.PureLogic.XXX`
- No base class changes needed (these don't use fixtures)
- No mock changes needed (they use NullLogger or no mocks at all)

**Step 3: Migrate low-mock tests that should stay**

These tests use real DB (SQLite InMemory) but are NOT mock-heavy:

| Source | Target | Reason to keep |
|--------|--------|----------------|
| `Auth/Services/JwtServiceTests.cs` | `PureLogic/Auth/JwtServiceTests.cs` | Tests JWT generation logic, uses real keys |
| `Auth/Services/TokenRevocationServiceTests.cs` | `PureLogic/Auth/TokenRevocationServiceTests.cs` | Uses SQLite InMemory, tests token DB logic |
| `Auth/Services/SecurityAuditServiceTests.cs` | `PureLogic/Auth/SecurityAuditServiceTests.cs` | Uses SQLite InMemory |
| `Auth/Services/SecurityAuditCleanupServiceTests.cs` | `PureLogic/Auth/SecurityAuditCleanupServiceTests.cs` | Uses SQLite InMemory |
| `Auth/Security/JwtOptionsValidationTests.cs` | `PureLogic/Auth/JwtOptionsValidationTests.cs` | Pure validation logic |
| `Herbs/Repositories/HerbRepositoryTests.cs` | `PureLogic/Repositories/HerbRepositoryTests.cs` | Uses EF InMemory |
| `Patients/Repositories/PatientRepositoryTests.cs` | `PureLogic/Repositories/PatientRepositoryTests.cs` | Uses EF InMemory |
| `Sync/Services/ChecksumHelperTests.cs` | `PureLogic/Sync/ChecksumHelperTests.cs` | Pure logic |
| `WebAPI/Middleware/**` | `PureLogic/WebAPI/Middleware/` | Low-mock middleware tests |
| `Infrastructure/Data/DatabaseInitializationServiceTests.cs` | `PureLogic/Infrastructure/DatabaseInitTests.cs` | SQLite InMemory |

**Step 4: DO NOT migrate these (replaced by integration tests)**

| Source | Replacement |
|--------|-------------|
| `Modules/Auth/Services/AuthServiceTests.cs` (14 tests, 562 lines) | Auth integration tests (Task 2.1) |
| `Modules/Users/Services/UserServiceTests.cs` (26 tests, 1050 lines) | Users integration tests (Task 2.2) |
| `Modules/Patients/Services/PatientServiceTests.cs` (26 tests, 947 lines) | Patients integration tests (Task 2.3) |
| `Modules/MedicalCases/Services/MedicalCaseCommandServiceTests.cs` (9 tests, 396 lines) | MedicalCases integration tests (Task 2.4) |
| `Modules/Herbs/Services/HerbServiceTests.cs` (25 tests, 963 lines) | Herbs integration tests (Task 2.5) |
| `Modules/Formulas/Services/FormulaServiceTests.cs` (22 tests, 767 lines) | Formulas integration tests (Task 2.6) |
| `Modules/Patients/Controllers/PatientsControllerTests.cs` | Controller tests replaced by HTTP integration tests |
| `Infrastructure/Repositories/BaseRepositoryTests.cs` | InMemory DB tests replaced by real DB |
| `Infrastructure/Services/BaseServiceTests.cs` | Mock tests replaced by integration |
| `Infrastructure/Services/CrossModuleQueryServiceTests.cs` | Mock tests replaced by integration |

**Decision needed for:**

| Source | Decision criteria |
|--------|-------------------|
| `MedicalCases/Services/MedicalCaseQueryServiceTests.cs` | If mock-heavy -> skip; if SQLite -> migrate |
| `MedicalCases/Services/MedicalCaseStateServiceTests.cs` | If mock-heavy -> skip; if pure logic -> migrate |
| `MedicalCases/Services/MedicalCasePrintServiceTests.cs` | If mock-heavy -> skip; if pure logic -> migrate |

Check each file: `grep -c "Substitute.For" <file>`. If count > 2, skip.

**Step 5: Build and verify**

```bash
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~PureLogic" -v normal
```

**Step 6: Commit**

```bash
git add tests/LYBT.Tests.Server/PureLogic/
git commit -m "test: migrate pure logic unit tests to Testing Trophy (Phase 2.8)"
```

---

## Task 2.9: Add IntegrationTestBase Helper Methods

**Files:**
- Modify: `tests/LYBT.Tests.Server/_Infrastructure/IntegrationTestBase.cs`

Based on patterns discovered during Tasks 2.1-2.6, add commonly needed helper methods to the base class.

**Step 1: Add helper methods**

```csharp
// Add to IntegrationTestBase:

/// <summary>
/// JSON 序列化选项 (所有测试通用)。
/// </summary>
protected static readonly JsonSerializerOptions JsonOptions = new()
{
    PropertyNameCaseInsensitive = true,
    Converters = { new JsonStringEnumConverter() }
};

/// <summary>
/// 创建指定角色的测试用户并登录。
/// 用于需要 Receptionist 或其他非标准角色的测试。
/// </summary>
protected async Task<HttpClient> CreateAndLoginUserAsync(
    string role,
    string? username = null,
    string password = "TestPass2025@")
{
    var sysAdmin = await LoginAsSysAdminAsync();
    var name = username ?? $"test_{role.ToLower()}_{Guid.NewGuid():N}"[..20];

    var createResponse = await sysAdmin.PostAsJsonAsync("/api/v1/users", new
    {
        UserName = name,
        Password = password,
        RealName = $"测试{role}",
        Role = role,
        Email = $"{name}@test.com"
    });
    createResponse.EnsureSuccessStatusCode();

    return await Fixture.LoginAsAsync(name, password);
}

/// <summary>
/// 创建测试患者并返回其 ID。
/// </summary>
protected async Task<Guid> CreateTestPatientAsync(HttpClient client, string? name = null)
{
    var patientName = name ?? $"患者_{Guid.NewGuid():N}"[..8];
    var response = await client.PostAsJsonAsync("/api/v1/patients", new
    {
        Name = patientName,
        Gender = "Male",
        BirthDate = "1990-01-01"
    });
    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadAsStringAsync();
    var doc = System.Text.Json.JsonDocument.Parse(content);
    var idStr = doc.RootElement.GetProperty("data").GetProperty("id").GetString();
    return Guid.Parse(idStr!);
}

/// <summary>
/// 创建测试药材并返回其 ID。
/// </summary>
protected async Task<Guid> CreateTestHerbAsync(HttpClient client, string? name = null)
{
    var herbName = name ?? $"药材_{Guid.NewGuid():N}"[..8];
    var response = await client.PostAsJsonAsync("/api/v1/herbs", new
    {
        Name = herbName,
        Unit = "g",
        Price = 10.0,
        Category = "补益药"
    });
    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadAsStringAsync();
    var doc = System.Text.Json.JsonDocument.Parse(content);
    var idStr = doc.RootElement.GetProperty("data").GetProperty("id").GetString();
    return Guid.Parse(idStr!);
}
```

**Note:** 实际实现时，根据 API 的 `ApiResponse<T>` 格式调整 JSON 解析方式。如果 `ReadFromJsonAsync<ApiResponse<T>>` 可用，优先使用强类型。

**Step 2: Update existing tests to use base class helpers**

Refactor duplicate helper methods in individual test classes to use the base class versions. This is a cleanup pass.

**Step 3: Build and verify**

```bash
dotnet test tests/LYBT.Tests.Server/ -v normal
```

**Step 4: Commit**

```bash
git add tests/LYBT.Tests.Server/_Infrastructure/
git commit -m "refactor: add shared helper methods to IntegrationTestBase (Phase 2.9)"
```

---

## Task 2.10: Full Verification + Coverage Comparison

**Step 1: Run all new tests**

```bash
dotnet test tests/LYBT.Tests.Server/ -v normal --logger "console;verbosity=detailed"
```

Record: total tests, passed, failed, skipped.

**Step 2: Run all old tests (still exist)**

```bash
dotnet test tests/LYBT.Tests.Server.Integration/ -v normal
dotnet test tests/LYBT.Tests.Unit/ --filter "FullyQualifiedName~LYBT.Tests.Unit.Modules" -v normal
```

Record old counts for comparison.

**Step 3: Coverage comparison table**

| Module | Old Unit (mock) | Old Integration | New Server Tests | Delta |
|--------|----------------|-----------------|------------------|-------|
| Auth | 14 | 15+ | ? | |
| Users | 26 | 28 | ? | |
| Patients | 26 | 24 | ? | |
| MedicalCases | 9 | 22+ | ? | |
| Herbs | 25 | 17 | ? | |
| Formulas | 22 | 16+ | ? | |
| Pure Logic | ~200+ | 0 | ? | |
| Other (Sync, Health, etc.) | ~50 | ~40 | ? | |

**Acceptance criteria:**
- New `LYBT.Tests.Server` test count >= old `LYBT.Tests.Server.Integration` count
- All mock-heavy unit test scenarios are covered by integration tests
- Zero test failures
- No NSubstitute dependency in `LYBT.Tests.Server.csproj`

**Step 4: Update progress files**

Update `task_plan.md` Phase 2 status to `complete`.
Update `progress.md` with execution log.

**Step 5: Commit**

```bash
git commit -m "test: complete Phase 2 server test migration verification"
```

---

## Risk Mitigation

| Risk | Mitigation |
|------|-----------|
| SQL Server 连接失败 | 检查 SQL Server 服务运行状态; LocalSqlServerProvider 使用 Trusted_Connection |
| Respawn 兼容性问题 (SQL Server 2012) | 已在 Phase 1 验证通过; 若失败检查 Respawn 版本 |
| Serilog freeze (RateLimiting Fixture) | `Log.CloseAndFlush()` + 替换 `ILoggerFactory` |
| 测试间状态泄漏 | Respawn 每测试重置; 检查 static 状态 |
| 登录性能 (每测试真实 HTTP 登录) | 可接受; 每次 ~50ms; 总增量 <30s |
| CreateClientAs 替代方案复杂度 | 提供 `CreateAndLoginUserAsync` helper; 先创建后登录 |
| 旧测试使用 SeedAsync 直接插入不合法数据 | 改用 API 创建 (走验证); 特殊场景用 WithDbContextAsync 绕过 |

---

## Dependencies

- Phase 1 (complete): ServerFixture + IntegrationTestBase + smoke tests
- SQL Server localhost 可用
- 不依赖 Docker
- 不修改生产代码
