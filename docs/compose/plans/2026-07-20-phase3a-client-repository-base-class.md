# Phase 3a: Client Repository Base Class Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extract `ApiClientRepositoryBase` base class to eliminate duplicate code across 6 Client repositories and unify CancellationToken support.

**Architecture:** Create an abstract base class in `LYBT.Desktop.Foundation` with standard CRUD methods, unified exception handling, and CancellationToken support. Each repository inherits from this base class and only implements its unique methods.

**Tech Stack:** .NET 8, WPF/Prism, CommunityToolkit.Mvvm, Refit

## Global Constraints

- All existing functionality must be preserved
- No behavioral changes (only structural refactoring)
- Build must succeed after each task
- All tests must pass after each task
- Follow existing code patterns (sealed classes, constructor injection)

---

## Task 1: Create ApiClientRepositoryBase

**Covers:** [S3]

**Files:**
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Repositories/ApiClientRepositoryBase.cs`

**Interfaces:**
- Produces: `ApiClientRepositoryBase<TListDto, TDetailDto, TCreateDto, TUpdateDto>` abstract class

- [ ] **Step 1: Create base class**

```csharp
// src/Client/Desktop/Core/LYBT.Desktop.Foundation/Repositories/ApiClientRepositoryBase.cs

using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Foundation.Repositories;

/// <summary>
/// Client端 Repository 泛型基类
/// 统一异常处理、日志记录、CancellationToken 支持
/// </summary>
public abstract class ApiClientRepositoryBase<TListDto, TDetailDto, TCreateDto, TUpdateDto>
{
    protected readonly ILogger Logger;

    protected ApiClientRepositoryBase(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 日志前缀，子类可覆盖
    /// </summary>
    protected virtual string LogPrefix => $"[REPO] {GetType().Name}";

    /// <summary>
    /// 统一异常处理：记录日志后重新抛出
    /// </summary>
    protected void HandleException(Exception ex, string operation, object? context = null)
    {
        var contextInfo = context != null ? $" ({context})" : "";
        Logger.LogError(ex, "{Prefix}.{Operation} 失败{Context}", LogPrefix, operation, contextInfo);
        throw; // 统一重新抛出，由 ViewModel 层处理
    }

    /// <summary>
    /// 执行带异常处理的异步操作
    /// </summary>
    protected async Task<T> ExecuteAsync<T>(string operation, Func<Task<T>> action, object? context = null)
    {
        try
        {
            return await action();
        }
        catch (Exception ex)
        {
            HandleException(ex, operation, context);
            throw; // 不可达，但编译器需要
        }
    }

    /// <summary>
    /// 执行带异常处理的异步操作（无返回值）
    /// </summary>
    protected async Task ExecuteAsync(string operation, Func<Task> action, object? context = null)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            HandleException(ex, operation, context);
        }
    }
}
```

- [ ] **Step 2: Verify build**

```bash
dotnet build src/Client/Desktop/Core/LYBT.Desktop.Foundation/LYBT.Desktop.Foundation.csproj
```

Expected: Build succeeds with zero errors.

- [ ] **Step 3: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Foundation/Repositories/ApiClientRepositoryBase.cs
git commit -m "feat(Desktop): add ApiClientRepositoryBase base class"
```

---

## Task 2: Migrate RegistrationRepository to Base Class

**Covers:** [S3]

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Registration/Repositories/RegistrationRepository.cs`

**Interfaces:**
- Consumes: `ApiClientRepositoryBase` from Task 1
- Produces: Migrated `RegistrationRepository` (simplest, good first migration)

- [ ] **Step 1: Update RegistrationRepository to inherit from base class**

```csharp
// Change class declaration
public sealed class RegistrationRepository : ApiClientRepositoryBase<object, object, object, object>, IRegistrationRepository
{
    private readonly IApiClient _apiClient;

    public RegistrationRepository(IApiClient apiClient, ILogger<RegistrationRepository> logger)
        : base(logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    // ... rest of implementation uses ExecuteAsync helper
}
```

- [ ] **Step 2: Refactor methods to use base class helpers**

Update each method to use `ExecuteAsync` instead of manual try/catch:

```csharp
public async Task<RegistrationDetailDto> CreateAsync(RegistrationInputDto input)
{
    return await ExecuteAsync("CreateAsync", async () =>
    {
        var response = await _apiClient.Registrations.CreateAsync(input);
        if (!response.Success || response.Data == null)
            throw new InvalidOperationException(response.Message ?? "创建挂号失败");
        return response.Data;
    }, input);
}
```

- [ ] **Step 3: Add CancellationToken support**

Add `CancellationToken ct = default` to all public methods.

- [ ] **Step 4: Verify build**

```bash
dotnet build src/Client/Desktop/Modules/LYBT.Desktop.Registration/LYBT.Desktop.Registration.csproj
```

Expected: Build succeeds with zero errors.

- [ ] **Step 5: Commit**

```bash
git add src/Client/Desktop/Modules/LYBT.Desktop.Registration/Repositories/RegistrationRepository.cs
git commit -m "refactor(Desktop): migrate RegistrationRepository to base class"
```

---

## Task 3: Migrate HerbRepository to Base Class

**Covers:** [S3]

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Repositories/HerbRepository.cs`

**Interfaces:**
- Consumes: `ApiClientRepositoryBase` from Task 1
- Produces: Migrated `HerbRepository`

- [ ] **Step 1: Update HerbRepository to inherit from base class**

- [ ] **Step 2: Refactor methods to use base class helpers**

- [ ] **Step 3: Add CancellationToken support to all methods**

- [ ] **Step 4: Verify build**

```bash
dotnet build src/Client/Desktop/Modules/LYBT.Desktop.Herbs/LYBT.Desktop.Herbs.csproj
```

- [ ] **Step 5: Commit**

```bash
git add src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Repositories/HerbRepository.cs
git commit -m "refactor(Desktop): migrate HerbRepository to base class"
```

---

## Task 4: Migrate PatientRepository to Base Class

**Covers:** [S3]

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Repositories/PatientRepository.cs`

**Interfaces:**
- Consumes: `ApiClientRepositoryBase` from Task 1
- Produces: Migrated `PatientRepository`

- [ ] **Step 1: Update PatientRepository to inherit from base class**

- [ ] **Step 2: Refactor methods to use base class helpers**

- [ ] **Step 3: Verify CancellationToken already exists (no changes needed)**

- [ ] **Step 4: Verify build**

```bash
dotnet build src/Client/Desktop/Modules/LYBT.Desktop.Patients/LYBT.Desktop.Patients.csproj
```

- [ ] **Step 5: Commit**

```bash
git add src/Client/Desktop/Modules/LYBT.Desktop.Patients/Repositories/PatientRepository.cs
git commit -m "refactor(Desktop): migrate PatientRepository to base class"
```

---

## Task 5: Migrate FormulaRepository to Base Class

**Covers:** [S3]

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Repositories/FormulaRepository.cs`

**Interfaces:**
- Consumes: `ApiClientRepositoryBase` from Task 1
- Produces: Migrated `FormulaRepository`

- [ ] **Step 1: Update FormulaRepository to inherit from base class**

- [ ] **Step 2: Refactor methods to use base class helpers**

- [ ] **Step 3: Add CancellationToken support to all methods**

- [ ] **Step 4: Verify build**

```bash
dotnet build src/Client/Desktop/Modules/LYBT.Desktop.Formula/LYBT.Desktop.Formula.csproj
```

- [ ] **Step 5: Commit**

```bash
git add src/Client/Desktop/Modules/LYBT.Desktop.Formula/Repositories/FormulaRepository.cs
git commit -m "refactor(Desktop): migrate FormulaRepository to base class"
```

---

## Task 6: Migrate UserRepository to Base Class

**Covers:** [S3]

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Users/Repositories/UserRepository.cs`

**Interfaces:**
- Consumes: `ApiClientRepositoryBase` from Task 1
- Produces: Migrated `UserRepository`

- [ ] **Step 1: Update UserRepository to inherit from base class**

- [ ] **Step 2: Refactor methods to use base class helpers**

- [ ] **Step 3: Add CancellationToken support to all methods**

- [ ] **Step 4: Fix BatchDeleteAsync to return BatchOperationResultDto instead of null**

- [ ] **Step 5: Verify build**

```bash
dotnet build src/Client/Desktop/Modules/LYBT.Desktop.Users/LYBT.Desktop.Users.csproj
```

- [ ] **Step 6: Commit**

```bash
git add src/Client/Desktop/Modules/LYBT.Desktop.Users/Repositories/UserRepository.cs
git commit -m "refactor(Desktop): migrate UserRepository to base class"
```

---

## Task 7: Migrate MedicalCaseRepository to Base Class

**Covers:** [S3]

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Repositories/MedicalCaseRepository.cs`

**Interfaces:**
- Consumes: `ApiClientRepositoryBase` from Task 1
- Produces: Migrated `MedicalCaseRepository`

- [ ] **Step 1: Update MedicalCaseRepository to inherit from base class**

- [ ] **Step 2: Refactor methods to use base class helpers**

- [ ] **Step 3: Add CancellationToken support to all methods**

- [ ] **Step 4: Verify build**

```bash
dotnet build src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/LYBT.Desktop.MedicalCase.csproj
```

- [ ] **Step 5: Commit**

```bash
git add src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Repositories/MedicalCaseRepository.cs
git commit -m "refactor(Desktop): migrate MedicalCaseRepository to base class"
```

---

## Task 8: Update Repository Interfaces for CancellationToken

**Covers:** [S3]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Repositories/IHerbRepository.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Repositories/IUserRepository.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Repositories/IFormulaRepository.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Repositories/IMedicalCaseRepository.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Repositories/IRegistrationRepository.cs`

**Interfaces:**
- Consumes: All migrated repositories from Tasks 2-7
- Produces: Updated interfaces with CancellationToken

- [ ] **Step 1: Add CancellationToken to IHerbRepository**

- [ ] **Step 2: Add CancellationToken to IUserRepository**

- [ ] **Step 3: Add CancellationToken to IFormulaRepository**

- [ ] **Step 4: Add CancellationToken to IMedicalCaseRepository**

- [ ] **Step 5: Add CancellationToken to IRegistrationRepository**

- [ ] **Step 6: Verify build**

```bash
dotnet build LYBTZYZS.sln
```

- [ ] **Step 7: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Contracts/Repositories/
git commit -m "refactor(Desktop): add CancellationToken to all repository interfaces"
```

---

## Task 9: Final Verification

**Covers:** [S7]

**Files:**
- None (verification only)

**Interfaces:**
- Consumes: All previous tasks

- [ ] **Step 1: Run full build**

```bash
dotnet build LYBTZYZS.sln
```

Expected: Build succeeds with zero errors.

- [ ] **Step 2: Run all tests**

```bash
$env:DOTNET_ROOT = "C:\Program Files\dotnet"; dotnet test tests/ --verbosity quiet
```

Expected: All tests pass.

- [ ] **Step 3: Run architecture tests**

```bash
$env:DOTNET_ROOT = "C:\Program Files\dotnet"; dotnet test tests/LYBT.Tests.Architecture/ --verbosity quiet
```

Expected: All architecture tests pass.

- [ ] **Step 4: Commit final verification**

```bash
git add -A
git commit -m "docs(Phase 3a): verify Client Repository base class migration complete"
```

---

## Summary

**Total Tasks:** 9
**Estimated Time:** 2-3 days
**Key Changes:**
- New: `ApiClientRepositoryBase` abstract class
- Modified: 6 repository implementations
- Modified: 5 repository interfaces
- Added: CancellationToken support to all repository methods
