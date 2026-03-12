# Collection 内并行化 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans or superpowers:subagent-driven-development to implement this plan task-by-task.

**Goal:** 在 xUnit Collection 内启用并行执行，使用唯一标识符实现测试间数据隔离，将集成测试时间从87秒降至30-40秒。

**Architecture:**
1. **并行配置**: 修改 xunit.runner.json 启用 Collection 内并行
2. **数据隔离**: 添加 UniqueName/UniquePhone 生成器确保测试数据不冲突
3. **测试重构**: 修复所有共享数据依赖，添加唯一标识符前缀

**Tech Stack:** xUnit, WebApplicationFactory, Respawn, Parallel Execution

---

## Task 1: Update xUnit Configuration for Parallel Execution

**Files:**
- Modify: `tests/LYBT.Tests.Server/xunit.runner.json`

**Step 1: Update xunit.runner.json**

Current content:
```json
{
  "$schema": "https://xunit.net/schema/current/xunit.runner.schema.json",
  "parallelizeAssembly": false,
  "parallelizeTestCollections": true,
  "maxParallelThreads": 0,
  "diagnosticMessages": false,
  "methodDisplay": "classAndMethod"
}
```

New content:
```json
{
  "$schema": "https://xunit.net/schema/current/xunit.runner.schema.json",
  "parallelizeAssembly": true,
  "parallelizeTestCollections": true,
  "parallelizeTestMethods": true,
  "maxParallelThreads": 8,
  "diagnosticMessages": true,
  "methodDisplay": "classAndMethod",
  "shadowCopy": false
}
```

**Key changes:**
- `parallelizeAssembly`: true (程序集并行)
- `parallelizeTestMethods`: true (方法级并行 - NEW!)
- `maxParallelThreads`: 0 → 8 (限制线程数避免资源争抢)
- `shadowCopy`: false (禁用影子复制，提高性能)
- `diagnosticMessages`: true (启用诊断信息)

**Step 2: Verify build**

Run:
```bash
dotnet build tests/LYBT.Tests.Server/LYBT.Tests.Server.csproj
```

Expected: Build succeeded

**Step 3: Run quick test**

Run:
```bash
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~US_Auth_MustHaveTests" -v n
```

Expected: Tests pass (注意：可能有并行冲突)

**Step 4: Commit**

```bash
git add tests/LYBT.Tests.Server/xunit.runner.json
git commit -m "perf(tests): enable parallel execution within collections"
```

---

## Task 2: Create Thread-Safe Unique Name Generator

**Files:**
- Modify: `tests/LYBT.Tests.Server/_Infrastructure/IntegrationTestBase.cs`

**Step 1: Add unique identifier generators**

Add to `IntegrationTestBase<TFixture>` class:

```csharp
/// <summary>
/// Thread-safe unique name generator for parallel test execution.
/// Ensures test data isolation when tests run concurrently.
/// </summary>
public abstract class IntegrationTestBase<TFixture> : IAsyncLifetime
    where TFixture : ServerFixture
{
    // ... existing code ...

    #region Parallel-Safe Unique Generators

    // Thread-local storage for test-specific prefix
    private static readonly ThreadLocal<string> _testPrefix = new(() => Guid.NewGuid().ToString("N")[..8]);

    /// <summary>
    /// Generates a unique name with thread-specific prefix for parallel test isolation.
    /// </summary>
    protected static string UniqueName(string baseName)
    {
        return $"{_testPrefix.Value}_{baseName}";
    }

    /// <summary>
    /// Generates a unique phone number for parallel test isolation.
    /// </summary>
    protected static string UniquePhone()
    {
        // Generate unique 11-digit phone: 138 + 8 random digits based on thread prefix
        var prefix = _testPrefix.Value;
        var randomPart = prefix.GetHashCode() % 100000000;
        return $"138{Math.Abs(randomPart):D8}";
    }

    /// <summary>
    /// Generates a unique ID number for parallel test isolation.
    /// </summary>
    protected static string UniqueIdNumber()
    {
        var prefix = _testPrefix.Value;
        var random = new Random(prefix.GetHashCode());
        var year = random.Next(1960, 2000);
        var month = random.Next(1, 13);
        var day = random.Next(1, 29);
        var suffix = random.Next(1000, 9999);
        return $"320101{year}{month:D2}{day:D2}{suffix}";
    }

    /// <summary>
    /// Generates a unique email for parallel test isolation.
    /// </summary>
    protected static string UniqueEmail(string baseName)
    {
        return $"{baseName.ToLower()}_{_testPrefix.Value}@test.com";
    }

    /// <summary>
    /// Generates a unique username for parallel test isolation.
    /// </summary>
    protected static string UniqueUsername(string baseName)
    {
        return $"{baseName.ToLower()}_{_testPrefix.Value}";
    }

    #endregion
}
```

**Step 2: Update JourneyTestBase as well**

Apply same changes to `JourneyTestBase<TFixture>` class.

**Step 3: Verify build**

Run:
```bash
dotnet build tests/LYBT.Tests.Server/LYBT.Tests.Server.csproj
```

Expected: Build succeeded

**Step 4: Commit**

```bash
git add tests/LYBT.Tests.Server/_Infrastructure/IntegrationTestBase.cs
git add tests/LYBT.Tests.Server/_Infrastructure/JourneyTestBase.cs
git commit -m "feat(tests): add thread-safe unique generators for parallel execution"
```

---

## Task 3: Update Collection Fixtures for Parallel Safety

**Files:**
- Modify: `tests/LYBT.Tests.Server/_Infrastructure/ServerFixture.cs`

**Step 1: Make ResetAsync thread-safe**

Current `ResetAsync` uses Respawn which should be thread-safe, but we need to ensure database operations are synchronized.

Add to `ServerFixture` class:

```csharp
/// <summary>
/// Semaphore to serialize database reset operations within the same fixture.
/// Prevents concurrent reset conflicts when tests run in parallel.
/// </summary>
private readonly SemaphoreSlim _resetGate = new(1, 1);

/// <summary>
/// Resets the database to a clean state and re-seeds base data.
/// Thread-safe for parallel execution.
/// </summary>
public async Task ResetAsync()
{
    await _resetGate.WaitAsync();
    try
    {
        await using var connection = new SqlConnection(_dbProvider.ConnectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
        await SeedBaseDataAsync();
    }
    finally
    {
        _resetGate.Release();
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
git add tests/LYBT.Tests.Server/_Infrastructure/ServerFixture.cs
git commit -m "fix(tests): make ResetAsync thread-safe for parallel execution"
```

---

## Task 4: Refactor US_Auth_MustHaveTests for Parallel Safety

**Files:**
- Modify: `tests/LYBT.Tests.Server/Features/US_Auth_MustHaveTests.cs`

**Step 1: Identify data conflicts**

Tests that may conflict when run in parallel:
- User creation with same username
- Login with same credentials
- Token operations

**Step 2: Apply unique identifiers**

Replace hardcoded values:
- `"admin"` → Keep (seed data, read-only)
- `"testuser"` → `UniqueUsername("testuser")`
- `"13812345678"` → `UniquePhone()`
- `"test@example.com"` → `UniqueEmail("test")`

**Step 3: Example refactoring**

Before:
```csharp
var request = new LoginRequest { UserName = "admin", Password = "TestAdmin2025@" };
```

After (no change - admin is seed data, read-only):
```csharp
var request = new LoginRequest { UserName = "admin", Password = "TestAdmin2025@" };
```

Before:
```csharp
var request = new UserInputDto
{
    UserName = "testuser",
    Email = "test@test.com",
    PhoneNumber = "13812345678"
};
```

After:
```csharp
var request = new UserInputDto
{
    UserName = UniqueUsername("testuser"),
    Email = UniqueEmail("test"),
    PhoneNumber = UniquePhone()
};
```

**Step 4: Run tests**

Run:
```bash
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~US_Auth_MustHaveTests" -v n
```

Expected: All tests pass

**Step 5: Commit**

```bash
git add tests/LYBT.Tests.Server/Features/US_Auth_MustHaveTests.cs
git commit -m "perf(tests): refactor US_Auth_MustHaveTests for parallel safety"
```

---

## Task 5: Refactor US_User_MustHaveTests for Parallel Safety

**Files:**
- Modify: `tests/LYBT.Tests.Server/Features/US_User_MustHaveTests.cs`

**Step 1: Identify and fix data conflicts**

This test creates users, so needs unique identifiers for:
- UserName
- Email
- PhoneNumber
- RealName (optional but good practice)

**Step 2: Apply changes**

Example:
```csharp
var userInput = new UserInputDto
{
    UserName = UniqueUsername("newuser"),
    RealName = UniqueName("新员工"),
    Email = UniqueEmail("newuser"),
    PhoneNumber = UniquePhone(),
    // ...
};
```

**Step 3: Run tests**

Run:
```bash
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~US_User_MustHaveTests" -v n
```

Expected: All tests pass

**Step 4: Commit**

```bash
git add tests/LYBT.Tests.Server/Features/US_User_MustHaveTests.cs
git commit -m "perf(tests): refactor US_User_MustHaveTests for parallel safety"
```

---

## Task 6: Refactor Patient Tests for Parallel Safety

**Files:**
- Modify: `tests/LYBT.Tests.Server/Features/US_Patient_MustHaveTests.cs`

**Step 1: Apply unique identifiers**

Patient tests need unique:
- Name
- PhoneNumber
- IdNumber

**Step 2: Apply changes**

```csharp
var patientInput = new PatientInputDto
{
    Name = UniqueName("患者"),
    PhoneNumber = UniquePhone(),
    IdNumber = UniqueIdNumber(),
    // ...
};
```

**Step 3: Run tests**

Run:
```bash
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~US_Patient_MustHaveTests" -v n
```

Expected: All tests pass

**Step 4: Commit**

```bash
git commit -m "perf(tests): refactor US_Patient_MustHaveTests for parallel safety"
```

---

## Task 7: Refactor MedicalCase Tests for Parallel Safety

**Files:**
- Modify: `tests/LYBT.Tests.Server/Features/US_MedicalCase_MustHaveTests.cs`

**Step 1: Apply unique identifiers**

MedicalCase tests need unique:
- Patient data
- Case numbers (if generated)

**Step 2: Run and commit**

```bash
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~US_MedicalCase_MustHaveTests" -v n
git commit -m "perf(tests): refactor US_MedicalCase_MustHaveTests for parallel safety"
```

---

## Task 8: Refactor Herb/Formula Tests for Parallel Safety

**Files:**
- Modify: `tests/LYBT.Tests.Server/Features/US_Herb_MustHaveTests.cs`
- Modify: `tests/LYBT.Tests.Server/Features/US_Formula_MustHaveTests.cs`

**Step 1: Apply unique identifiers**

Herb tests need unique:
- Herb names

Formula tests need unique:
- Formula names

**Step 2: Apply changes**

```csharp
var herbInput = new HerbInputDto
{
    Name = UniqueName("黄芪"),
    // ...
};
```

**Step 3: Run and commit**

```bash
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~US_Herb_MustHaveTests" -v n
git commit -m "perf(tests): refactor Herb/Formula tests for parallel safety"
```

---

## Task 9: Refactor UserJourney Tests for Parallel Safety

**Files:**
- Multiple files in `tests/LYBT.Tests.Server/UserJourneys/`

**Step 1: Identify files to update**

UserJourney tests are more complex. Update in order:
1. `AuthJourneyTests.cs`
2. `AdminSetupJourneyTests.cs`
3. `PatientManagementJourneyTests.cs`
4. `FirstVisitJourneyTests.cs`
5. `ReturnVisitJourneyTests.cs`

**Step 2: Apply unique identifiers**

Each journey creates multiple entities, ensure all use UniqueXxx methods.

**Step 3: Run subset test**

Run one journey test:
```bash
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~AuthJourneyTests" -v n
```

Expected: Pass

**Step 4: Commit each file**

```bash
git add tests/LYBT.Tests.Server/UserJourneys/AuthJourneyTests.cs
git commit -m "perf(tests): refactor AuthJourneyTests for parallel safety"
```

Repeat for other journey files.

---

## Task 10: Performance Benchmark and Validation

**Files:**
- Run all tests

**Step 1: Run full test suite**

Run:
```bash
dotnet test tests/LYBT.Tests.Server/ -v q
```

**Step 2: Record metrics**

Document:
- Total test count
- Failed tests (should be 0)
- Execution time
- Compare to baseline (87 seconds)

**Expected improvement:**
- Before: 87 seconds (sequential)
- After: 30-40 seconds (parallel with 8 threads)
- Improvement: ~50-60%

**Step 3: Verify no parallel conflicts**

Run tests multiple times to ensure stability:
```bash
for i in {1..3}; do
    echo "Run $i:"
    dotnet test tests/LYBT.Tests.Server/ -v q
done
```

Expected: All runs pass consistently

**Step 4: Final commit**

```bash
git add docs/
git commit -m "docs(tests): document parallel execution performance improvements"
```

---

## Summary

### Performance Improvement Projection

| Configuration | Before | After | Improvement |
|---------------|--------|-------|-------------|
| Sequential | 87s | 87s | - |
| Parallel (4 threads) | 87s | ~45s | 48% |
| Parallel (8 threads) | 87s | ~35s | 60% |

### Risk Mitigation

1. **Thread Safety**: `SemaphoreSlim` in ResetAsync prevents concurrent DB resets
2. **Data Isolation**: Thread-local unique identifiers ensure no test conflicts
3. **Stability**: Multiple test runs verify no flaky tests

### Files Modified

- `xunit.runner.json` - Parallel configuration
- `IntegrationTestBase.cs` - Unique generators
- `JourneyTestBase.cs` - Unique generators
- `ServerFixture.cs` - Thread-safe ResetAsync
- All `Features/*Tests.cs` - Apply unique identifiers
- All `UserJourneys/*Tests.cs` - Apply unique identifiers

### Success Criteria

- All tests pass
- Execution time < 40 seconds
- No flaky tests (3 consecutive runs pass)
