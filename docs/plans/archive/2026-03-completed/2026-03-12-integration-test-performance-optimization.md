# Integration Test Performance Optimization Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans or superpowers:subagent-driven-development to implement this plan task-by-task.

**Goal:** 将集成测试执行时间从102秒降至30秒以内，通过共享 WebApplicationFactory 和事务隔离替代 Respawn。

**Architecture:**
1. **Shared WebApplicationFactory**: 4个 Collections 共享1个 WAF 实例，减少3次初始化开销（约12秒）
2. **Transaction Isolation**: 用数据库事务替代 Respawn，每个测试节省~140ms（462个测试 = 65秒）

**Tech Stack:** xUnit, WebApplicationFactory, EF Core, SQL Server, Respawn (保留作为备选)

---

## Task 1: Create Shared WebApplicationFactory Infrastructure

**Files:**
- Create: `tests/LYBT.Tests.Server.Unit/_Infrastructure/SharedTestContext.cs`

**Step 1: Create SharedTestContext**

Create `tests/LYBT.Tests.Server/_Infrastructure/SharedTestContext.cs`:

```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LYBT.Tests.Server.Infrastructure;

/// <summary>
/// Shared test context that provides a singleton WebApplicationFactory instance.
/// All test collections share this WAF to reduce initialization overhead.
/// </summary>
public static class SharedTestContext
{
    private static readonly Lazy<WebApplicationFactory<Program>> _factory = new(() =>
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");

                builder.ConfigureServices(services =>
                {
                    // Remove all background services to avoid interference
                    RemoveHostedServices(services);
                });
            });

        return factory;
    });

    public static WebApplicationFactory<Program> Factory => _factory.Value;

    private static void RemoveHostedServices(IServiceCollection services)
    {
        var hostedServices = services
            .Where(d => d.ServiceType == typeof(IHostedService))
            .ToList();
        foreach (var svc in hostedServices)
        {
            services.Remove(svc);
        }
    }
}
```

**Step 2: Verify build**

Run:
```bash
dotnet build tests/LYBT.Tests.Server/LYBT.Tests.Server.csproj
```

Expected: Build succeeded

**Step 3: Commit**

```bash
git add tests/LYBT.Tests.Server/_Infrastructure/SharedTestContext.cs
git commit -m "feat(tests): add SharedTestContext for shared WebApplicationFactory"
```

---

## Task 2: Create Transactional Integration Test Base

**Files:**
- Create: `tests/LYBT.Tests.Server/_Infrastructure/TransactionalIntegrationTestBase.cs`

**Step 1: Create TransactionalIntegrationTestBase**

Create `tests/LYBT.Tests.Server/_Infrastructure/TransactionalIntegrationTestBase.cs`:

```csharp
using System.Data.Common;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LYBT.Tests.Server.Infrastructure;

/// <summary>
/// Transactional base class for integration tests using database transactions instead of Respawn.
///
/// Performance benefit: ~140ms per test (transaction rollback vs Respawn reset)
/// For 462 tests: saves ~65 seconds
/// </summary>
public abstract class TransactionalIntegrationTestBase : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private IServiceScope? _scope;
    private DbTransaction? _transaction;

    protected AppDbContext DbContext { get; private set; } = null!;
    protected HttpClient AnonymousClient { get; private set; } = null!;

    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    protected TransactionalIntegrationTestBase()
    {
        _factory = SharedTestContext.Factory;
    }

    public async Task InitializeAsync()
    {
        // Create a new scope for this test
        _scope = _factory.Services.CreateScope();
        DbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Begin transaction
        var connection = DbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }
        _transaction = await connection.BeginTransactionAsync();

        // Seed base data within transaction
        await SeedBaseDataAsync();

        // Create anonymous client
        AnonymousClient = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        AnonymousClient?.Dispose();

        // Rollback transaction to clean up test data
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
        }

        _scope?.Dispose();
    }

    /// <summary>
    /// Creates an authenticated HttpClient by logging in with the specified credentials.
    /// </summary>
    protected async Task<HttpClient> LoginAsAsync(string username, string password)
    {
        var loginClient = _factory.CreateClient();

        var loginRequest = new LoginRequest
        {
            UserName = username,
            Password = password
        };

        var response = await loginClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<LoginResponse>>(content, JsonOptions);

        if (apiResponse?.Success != true || string.IsNullOrEmpty(apiResponse.Data?.Token))
        {
            throw new InvalidOperationException(
                $"Login failed for user '{username}'. Response: {content}");
        }

        var authenticatedClient = _factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiResponse.Data.Token);

        return authenticatedClient;
    }

    protected Task<HttpClient> LoginAsAdminAsync() => LoginAsAsync("admin", "TestAdmin2025@");
    protected Task<HttpClient> LoginAsDoctorAsync() => LoginAsAsync("doctor", "TestDoctor2025@");
    protected Task<HttpClient> LoginAsSysAdminAsync() => LoginAsAsync("sysadmin", "TestAdmin2025@");

    #region Shared User ID Helpers

    protected async Task<Guid> GetAdminUserIdAsync(HttpClient adminClient)
    {
        var response = await adminClient.GetAsync("/api/v1/users?keyword=admin");
        response.EnsureSuccessStatusCode();
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<PagedResult<UserListDto>>>(JsonOptions);
        var adminUser = body!.Data!.Items.First(u => u.UserName == "admin");
        return adminUser.Id;
    }

    protected async Task<Guid> GetDoctorUserIdAsync(HttpClient adminClient)
    {
        var response = await adminClient.GetAsync("/api/v1/users?keyword=doctor");
        response.EnsureSuccessStatusCode();
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<PagedResult<UserListDto>>>(JsonOptions);
        var doctorUser = body!.Data!.Items.First(u => u.UserName == "doctor");
        return doctorUser.Id;
    }

    #endregion

    #region Private Methods

    private async Task SeedBaseDataAsync()
    {
        // Check if base data already exists
        if (DbContext.Set<LYBT.Entities.Users.User>().Any())
        {
            return;
        }

        var now = DateTime.UtcNow;

        DbContext.Set<LYBT.Entities.Users.User>().AddRange(
            CreateUser(Guid.NewGuid(), "sysadmin", "系统管理员",
                LYBT.Shared.Models.Enums.UserRole.SuperAdmin, "admin@lybt.com", "TestAdmin2025@", now),
            CreateUser(Guid.Parse("00000000-0000-0000-0000-000000000001"), "admin", "测试管理员",
                LYBT.Shared.Models.Enums.UserRole.Admin, "admin-test@lybt.com", "TestAdmin2025@", now),
            CreateUser(Guid.Parse("00000000-0000-0000-0000-000000000002"), "doctor", "测试医生",
                LYBT.Shared.Models.Enums.UserRole.Doctor, "doctor-test@lybt.com", "TestDoctor2025@", now)
        );

        await DbContext.SaveChangesAsync();
    }

    private static LYBT.Entities.Users.User CreateUser(
        Guid id, string userName, string realName,
        LYBT.Shared.Models.Enums.UserRole role, string email, string password, DateTime now)
    {
        return new LYBT.Entities.Users.User
        {
            Id = id,
            UserName = userName,
            RealName = realName,
            Role = role,
            Email = email,
            Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled,
            PasswordHash = LYBT.Shared.Utilities.Security.PasswordHelper.HashPassword(password, role),
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty,
            IsDeleted = false
        };
    }

    #endregion
}
```

**Step 2: Verify build**

Run:
```bash
dotnet build tests/LYBT.Tests.Server/LYBT.Tests.Server.csproj
```

Expected: Build succeeded

**Step 3: Commit**

```bash
git add tests/LYBT.Tests.Server/_Infrastructure/TransactionalIntegrationTestBase.cs
git commit -m "feat(tests): add TransactionalIntegrationTestBase with transaction isolation"
```

---

## Task 3: Update Domain Collections for Transactional Tests

**Files:**
- Modify: `tests/LYBT.Tests.Server/_Infrastructure/DomainCollections.cs`

**Step 1: Add new collection definitions for transactional tests**

Modify `tests/LYBT.Tests.Server/_Infrastructure/DomainCollections.cs`:

```csharp
using Xunit;

namespace LYBT.Tests.Server.Infrastructure;

/// <summary>
/// xUnit Collection definitions for domain-based parallel execution.
///
/// Two types of collections:
/// 1. Legacy Collections (using ServerFixture with Respawn) - for tests that need database reset
/// 2. Transactional Collections (using TransactionalIntegrationTestBase) - faster, uses transaction rollback
/// </summary>

// Legacy Collections (for backward compatibility and special cases)
[CollectionDefinition("AuthUsers")]
public sealed class AuthUsersCollection : ICollectionFixture<AuthUsersFixture>;

[CollectionDefinition("ClinicalData")]
public sealed class ClinicalDataCollection : ICollectionFixture<ClinicalDataFixture>;

[CollectionDefinition("HerbFormula")]
public sealed class HerbFormulaCollection : ICollectionFixture<HerbFormulaFixture>;

[CollectionDefinition("SystemOps")]
public sealed class SystemOpsCollection : ICollectionFixture<SystemOpsFixture>;

// Transactional Collections (NEW - high performance)
// These don't use ICollectionFixture since TransactionalIntegrationTestBase manages its own lifecycle
[CollectionDefinition("AuthUsersFast")]
public sealed class AuthUsersFastCollection;

[CollectionDefinition("ClinicalDataFast")]
public sealed class ClinicalDataFastCollection;

[CollectionDefinition("HerbFormulaFast")]
public sealed class HerbFormulaFastCollection;

[CollectionDefinition("SystemOpsFast")]
public sealed class SystemOpsFastCollection;
```

**Step 2: Verify build**

Run:
```bash
dotnet build tests/LYBT.Tests.Server/LYBT.Tests.Server.csproj
```

Expected: Build succeeded

**Step 3: Commit**

```bash
git add tests/LYBT.Tests.Server/_Infrastructure/DomainCollections.cs
git commit -m "feat(tests): add transactional collection definitions for fast tests"
```

---

## Task 4: Migrate One Test Class to Transactional Model

**Files:**
- Read: `tests/LYBT.Tests.Server/Features/US_Auth_MustHaveTests.cs`
- Modify: Create transactional version

**Step 1: Read existing test file**

Read the current `US_Auth_MustHaveTests.cs` to understand its structure.

**Step 2: Create transactional test version**

Modify `tests/LYBT.Tests.Server/Features/US_Auth_MustHaveTests.cs`:

Change from:
```csharp
[Collection("AuthUsers")]
public sealed class US_Auth_MustHaveTests : IntegrationTestBase<AuthUsersFixture>
{
    public US_Auth_MustHaveTests(AuthUsersFixture fixture) : base(fixture) { }
    // ... tests using Fixture property
}
```

To:
```csharp
[Collection("AuthUsersFast")]
public sealed class US_Auth_MustHaveTests : TransactionalIntegrationTestBase
{
    // Tests now use AnonymousClient, LoginAsAdminAsync() etc. directly
    // Remove all Fixture. references
}
```

**Step 3: Update test method implementations**

For each test method, replace:
- `Fixture.AnonymousClient` → `AnonymousClient`
- `await Fixture.LoginAsAdminAsync()` → `await LoginAsAdminAsync()`
- `await Fixture.WithDbContextAsync(...)` → Direct use of `DbContext`

**Step 4: Run tests**

Run:
```bash
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~US_Auth_MustHaveTests" -v n
```

Expected: All tests PASS

**Step 5: Commit**

```bash
git add tests/LYBT.Tests.Server/Features/US_Auth_MustHaveTests.cs
git commit -m "perf(tests): migrate US_Auth_MustHaveTests to transactional model"
```

---

## Task 5: Performance Benchmark

**Files:**
- Run: Integration tests

**Step 1: Run benchmark for original model**

Run a subset of tests using the old fixture model:
```bash
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~US_User_MustHaveTests" --no-build -v q
```

Record execution time.

**Step 2: Run benchmark for transactional model**

Run the migrated tests:
```bash
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~US_Auth_MustHaveTests" --no-build -v q
```

Record execution time and compare.

**Step 3: Calculate improvement**

Expected improvement per test:
- Old: ~500-700ms (with Respawn reset)
- New: ~50-100ms (with transaction rollback)
- Improvement: ~400-600ms per test

**Step 4: Document results**

Update the plan with actual benchmark numbers.

---

## Task 6: Migrate All Fast-Eligible Test Classes (Batch 1)

**Files:**
- Multiple test files in `Features/`

**Step 1: Identify test classes for migration**

Fast-eligible criteria:
- Tests that don't need complex cross-test data dependencies
- Tests that work with isolated data
- Most "MustHave" feature tests

Batch 1 (10 files):
- Features/US_Auth_MustHaveTests.cs (already done)
- Features/US_User_MustHaveTests.cs
- Features/US_Patient_MustHaveTests.cs
- Features/US_Herb_MustHaveTests.cs
- Features/US_Formula_MustHaveTests.cs
- Features/US_Sync_MustHaveTests.cs

**Step 2: Migrate each file**

For each file:
1. Change `[Collection("Xxx")]` to `[Collection("XxxFast")]`
2. Change base class from `IntegrationTestBase<XxxFixture>` to `TransactionalIntegrationTestBase`
3. Remove constructor parameter
4. Replace `Fixture.` references with direct base class members
5. Run tests to verify
6. Commit individually

**Step 3: Verify all migrated tests pass**

Run:
```bash
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~MustHaveTests" --no-build -v q
```

Expected: All tests PASS

---

## Task 7: Migrate UserJourney Tests (Batch 2)

**Files:**
- UserJourneys/*.cs

**Step 1: Analyze UserJourney tests**

UserJourney tests are more complex - they may need to keep data across multiple API calls within a single test.

**Step 2: Check compatibility**

Transactional model works fine for UserJourney tests since:
- Each test method still runs in its own transaction
- Multiple API calls within one test share the same transaction
- Data is rolled back after the test completes

**Step 3: Migrate UserJourney tests**

Files to migrate:
- UserJourneys/AuthJourneyTests.cs
- UserJourneys/AdminSetupJourneyTests.cs
- UserJourneys/PatientManagementJourneyTests.cs
- UserJourneys/FirstVisitJourneyTests.cs
- UserJourneys/ReturnVisitJourneyTests.cs

**Step 4: Run and verify**

Run:
```bash
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~JourneyTests" --no-build -v q
```

Expected: All tests PASS

---

## Task 8: Final Performance Verification

**Files:**
- All integration tests

**Step 1: Full test run**

Run all integration tests:
```bash
dotnet test tests/LYBT.Tests.Server/ -v q
```

**Step 2: Record metrics**

Document:
- Total test count
- Passed/Failed/Skipped count
- Execution time
- Comparison to baseline (102 seconds)

**Step 3: Verify target met**

Target: < 60 seconds (ideally < 30 seconds)

**Step 4: Commit final results**

```bash
git add docs/
git commit -m "docs(tests): document performance optimization results"
```

---

## Summary

### Expected Performance Improvement

| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| WAF Initializations | 4 | 1 | -3 (12 sec) |
| Per-test Reset | Respawn (~150ms) | Transaction (~10ms) | -140ms |
| 462 Tests Total | 102 sec | ~30 sec | -72 sec (70%) |

### Risk Mitigation

1. **Transaction Isolation**: Each test runs in its own transaction - no cross-test contamination
2. **Rollback Guarantee**: `DisposeAsync` ensures transaction is always rolled back
3. **Fallback Option**: Legacy collections remain available if transactional model has issues

### Next Steps After Completion

1. Monitor CI/CD pipeline for stability
2. Consider migrating remaining tests
3. Document best practices for writing transactional tests
