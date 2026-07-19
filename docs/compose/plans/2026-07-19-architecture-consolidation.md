# Architecture Consolidation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Eliminate cross-tier dependency violations and reduce 40 projects to 36 by merging 4 oversplit projects.

**Architecture:** Move `Result<T>` from SharedKernel to Shared.Models (fixing dependency violations), then merge 3 small projects into their parent projects (Desktop.Shared→Contracts, Navigation→Infrastructure, Components→Models).

**Tech Stack:** .NET 8, C#, MSBuild, Prism, WPF, ASP.NET Core, EF Core, MediatR

## Global Constraints

- All `using` statements must use the NEW namespaces after moves
- `dotnet build LYBTZYZS.sln` must pass after each phase
- Architecture tests (`dotnet test tests/LYBT.Tests.Architecture/`) must pass after each phase
- Do NOT modify any business logic — only move code and update namespaces
- Follow existing code style: Chinese comments, English identifiers
- Commit after each phase with descriptive message

---

### Task 1: Move Result<T>/Result from SharedKernel to Shared.Models

**Covers:** [S3]

**Files:**
- Create: `src/Shared/LYBT.Shared.Models/Contracts/Common/Result.cs`
- Modify: `src/Server/Core/LYBT.SharedKernel/Common/Result.cs` (delete after move)

**Interfaces:**
- Produces: `LYBT.Shared.Models.Contracts.Common.Result<T>` and `LYBT.Shared.Models.Contracts.Common.Result`

- [ ] **Step 1: Create Result.cs in Shared.Models**

Create `src/Shared/LYBT.Shared.Models/Contracts/Common/Result.cs` with the exact content from `src/Server/Core/LYBT.SharedKernel/Common/Result.cs`, but change the namespace:

```csharp
using LYBT.Shared.Primitives.ErrorCodes;

namespace LYBT.Shared.Models.Contracts.Common;

/// <summary>
/// 操作结果封装。用于命令和查询的统一返回类型。
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public T? Data => Value;
    public string? Error { get; }
    public string? ErrorMessage => Error;
    public string? Message => Error;
    public IReadOnlyList<string> Errors { get; }
    public ErrorCode ErrorCode { get; }
    public ErrorCode? ModuleErrorCode { get; }
    public Exception? Exception { get; }

    private Result(bool isSuccess, T? value, string? error, IReadOnlyList<string>? errors, ErrorCode errorCode, Exception? exception = null, ErrorCode? moduleErrorCode = null)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        Errors = errors ?? Array.Empty<string>();
        ErrorCode = errorCode;
        Exception = exception;
        ModuleErrorCode = moduleErrorCode;
    }

    public static Result<T> Success(T value) => new(true, value, null, null, default);
    public static Result<T> Success(T value, string message) => new(true, value, message, null, default);
    public static Result<T> Failure(ErrorCode code, string error) => new(false, default, error, null, code);
    public static Result<T> Failure(string error) => new(false, default, error, null, ErrorCode.InternalError);
    public static Result<T> Failure(string error, Exception exception) => new(false, default, error, null, ErrorCode.InternalError, exception);
    public static Result<T> Failure(ErrorCode code, List<string> errors) => new(false, default, string.Join("; ", errors), errors, code);
    public static Result<T> Failure(List<string> errors) => new(false, default, string.Join("; ", errors), errors, ErrorCode.InternalError);
    public static Result<T> ValidationFailure(string error) => new(false, default, error, null, ErrorCode.ValidationFailed);
    public static Result<T> FromException(Exception ex, string? operationName = null)
    {
        var message = string.IsNullOrEmpty(operationName) ? ex.Message : $"{operationName}失败: {ex.Message}";
        return new(false, default, message, new List<string> { message }, ErrorCode.InternalError, ex);
    }
    public static implicit operator Result<T>(T value) => Success(value);
}

/// <summary>
/// 无数据的操作结果。
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public string? ErrorMessage => Error;
    public string? Message => Error;
    public IReadOnlyList<string> Errors { get; }
    public ErrorCode ErrorCode { get; }
    public ErrorCode? ModuleErrorCode { get; }
    public Exception? Exception { get; }

    private Result(bool isSuccess, string? error, IReadOnlyList<string>? errors, ErrorCode errorCode, Exception? exception = null, ErrorCode? moduleErrorCode = null)
    {
        IsSuccess = isSuccess;
        Error = error;
        Errors = errors ?? Array.Empty<string>();
        ErrorCode = errorCode;
        Exception = exception;
        ModuleErrorCode = moduleErrorCode;
    }

    public static Result Success() => new(true, null, null, default);
    public static Result Success(string message) => new(true, message, null, default);
    public static Result Failure(ErrorCode code, string error) => new(false, error, null, code);
    public static Result Failure(string error) => new(false, error, null, ErrorCode.InternalError);
    public static Result Failure(string error, Exception exception) => new(false, error, null, ErrorCode.InternalError, exception);
    public static Result Failure(ErrorCode code, List<string> errors) => new(false, string.Join("; ", errors), errors, code);
    public static Result Failure(List<string> errors) => new(false, string.Join("; ", errors), errors, ErrorCode.InternalError);
    public static Result FromException(Exception ex, string? operationName = null)
    {
        var message = string.IsNullOrEmpty(operationName) ? ex.Message : $"{operationName}失败: {ex.Message}";
        return new(false, message, new List<string> { message }, ErrorCode.InternalError, ex);
    }
}
```

- [ ] **Step 2: Delete old Result.cs from SharedKernel**

Delete: `src/Server/Core/LYBT.SharedKernel/Common/Result.cs`

- [ ] **Step 3: Update SharedKernel.csproj — it no longer needs Primitives reference for Result**

The SharedKernel.csproj already references Shared.Primitives and Shared.Models. After removing Result.cs, SharedKernel only has Entity, IAggregateRoot, and domain events. These don't need Primitives directly (Entity doesn't use ErrorCode). But keep the references since other Server modules may need them transitively.

No csproj change needed — SharedKernel still references Shared.Models (which now has Result<T>).

- [ ] **Step 4: Quick build check (expected to fail)**

Run: `dotnet build LYBTZYZS.sln`
Expected: Build FAILS with CS0246/Cannot resolve type errors — this is expected because Server modules and Desktop still reference the old `LYBT.SharedKernel.Common` namespace. Steps 5-9 fix all usings.

- [ ] **Step 5: Update all Server module usings**

Server modules that use `Result<T>` from SharedKernel need to update their usings. Search for all files with `using LYBT.Shared.Models.Contracts.Common;` in Server modules and replace with `using LYBT.Shared.Models.Contracts.Common;`.

Key files to update (use grep to find all):
- All `*CommandHandler.cs` and `*QueryHandler.cs` files in Server/Modules
- `src/Server/Services/LYBT.WebAPI/Configuration/` handlers
- `src/Server/Core/LYBT.Infrastructure/Services/BaseService.cs`

Use `serena_replace_in_files` or manual edits to replace `using LYBT.Shared.Models.Contracts.Common;` → `using LYBT.Shared.Models.Contracts.Common;` in all Server files.

- [ ] **Step 6: Update Desktop GlobalUsings.cs**

In `src/Client/Desktop/GlobalUsings.cs`, replace:
```csharp
global using LYBT.Shared.Models.Contracts.Common;
```
with:
```csharp
global using LYBT.Shared.Models.Contracts.Common;
```

- [ ] **Step 7: Update Shared.ExceptionHandling usings**

In all 4 files under `src/Shared/LYBT.Shared.ExceptionHandling/Handlers/`, replace:
```csharp
using LYBT.Shared.Models.Contracts.Common;
```
with:
```csharp
using LYBT.Shared.Models.Contracts.Common;
```

- [ ] **Step 8: Remove SharedKernel reference from ExceptionHandling.csproj**

In `src/Shared/LYBT.Shared.ExceptionHandling/LYBT.Shared.ExceptionHandling.csproj`, remove:
```xml
<ProjectReference Include="..\..\Server\Core\LYBT.SharedKernel\LYBT.SharedKernel.csproj" />
```

- [ ] **Step 9: Remove SharedKernel reference from Desktop.Contracts.csproj**

In `src/Client/Desktop/Core/LYBT.Desktop.Contracts/LYBT.Desktop.Contracts.csproj`, remove:
```xml
<ProjectReference Include="..\..\..\..\Server\Core\LYBT.SharedKernel\LYBT.SharedKernel.csproj" />
```

- [ ] **Step 10: Build and test**

Run: `dotnet build LYBTZYZS.sln`
Expected: Build succeeds

Run: `dotnet test tests/LYBT.Tests.Architecture/`
Expected: All architecture tests pass (no more cross-tier violations)

- [ ] **Step 11: Commit**

```bash
git add -A
git commit -m "refactor(Shared): move Result<T> from SharedKernel to Shared.Models, fix cross-tier dependency violations"
```

---

### Task 2: Merge Desktop.Shared into Desktop.Contracts

**Covers:** [S4]

**Files:**
- Move: 7 files from `Desktop.Shared/` → `Desktop.Contracts/`
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Shared/` directory
- Remove: `LYBT.Desktop.Shared` from solution
- Modify: `LYBT.Desktop.Contracts.csproj` (remove Shared reference)

**Interfaces:**
- Produces: `LYBT.Desktop.Contracts.Results.CommandResult`, `LYBT.Desktop.Contracts.Models.AuthState`, etc.

- [ ] **Step 1: Move files from Desktop.Shared to Desktop.Contracts**

Move each file, updating namespace:

| Source | Target | New Namespace |
|--------|--------|---------------|
| `Desktop.Shared/Results/CommandResult.cs` | `Desktop.Contracts/Results/CommandResult.cs` | `LYBT.Desktop.Contracts.Results` |
| `Desktop.Shared/Models/AuthState.cs` | `Desktop.Contracts/Models/AuthState.cs` | `LYBT.Desktop.Contracts.Models` |
| `Desktop.Shared/Models/PerformanceMetric.cs` | `Desktop.Contracts/Models/PerformanceMetric.cs` | `LYBT.Desktop.Contracts.Models` |
| `Desktop.Shared/Models/PerformanceReport.cs` | `Desktop.Contracts/Models/PerformanceReport.cs` | `LYBT.Desktop.Contracts.Models` |
| `Desktop.Shared/Events/CacheEvents.cs` | `Desktop.Contracts/Events/CacheEvents.cs` | `LYBT.Desktop.Contracts.Events` |
| `Desktop.Shared/Enums/UnfinishedCaseChoice.cs` | `Desktop.Contracts/Enums/UnfinishedCaseChoice.cs` | `LYBT.Desktop.Contracts.Enums` |
| `Desktop.Shared/UI/BreadcrumbItem.cs` | `Desktop.Contracts/UI/BreadcrumbItem.cs` | `LYBT.Desktop.Contracts.UI` |

- [ ] **Step 2: Update namespaces in moved files**

In each moved file, change `namespace LYBT.Shared.xxx` to `namespace LYBT.Desktop.Contracts.xxx`.

- [ ] **Step 3: Update all using statements (~33 files)**

Replace across the codebase:
- `using LYBT.Desktop.Contracts.Results;` → `using LYBT.Desktop.Contracts.Results;`
- `using LYBT.Desktop.Contracts.Models;` → `using LYBT.Desktop.Contracts.Models;`
- `using LYBT.Desktop.Contracts.Events;` → `using LYBT.Desktop.Contracts.Events;`
- `using LYBT.Desktop.Contracts.Enums;` → `using LYBT.Desktop.Contracts.Enums;`
- `using LYBT.Desktop.Contracts.UI;` → `using LYBT.Desktop.Contracts.UI;`

- [ ] **Step 4: Remove Shared reference from Contracts.csproj**

In `LYBT.Desktop.Contracts.csproj`, remove:
```xml
<ProjectReference Include="..\LYBT.Desktop.Shared\LYBT.Desktop.Shared.csproj" />
```

- [ ] **Step 5: Delete Desktop.Shared directory**

Delete: `src/Client/Desktop/Core/LYBT.Desktop.Shared/`

- [ ] **Step 6: Remove from solution**

Run: `dotnet sln LYBTZYZS.sln remove src/Client/Desktop/Core/LYBT.Desktop.Shared/LYBT.Desktop.Shared.csproj`

- [ ] **Step 7: Build and test**

Run: `dotnet build LYBTZYZS.sln`
Expected: Build succeeds

Run: `dotnet test tests/LYBT.Tests.Architecture/`
Expected: All tests pass

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "refactor(Desktop): merge Desktop.Shared into Desktop.Contracts, reduce 1 project"
```

---

### Task 3: Merge Desktop.Navigation into Desktop.Infrastructure

**Covers:** [S5]

**Files:**
- Move: 10 files from `Desktop.Navigation/` → `Desktop.Infrastructure/Navigation/`
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Navigation/` directory
- Remove: `LYBT.Desktop.Navigation` from solution

**Interfaces:**
- Produces: `LYBT.Desktop.Infrastructure.Navigation.NavigationCoordinator`, etc.

- [ ] **Step 1: Move files from Desktop.Navigation to Desktop.Infrastructure**

Create `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Navigation/` directory.

Move all .cs files (excluding obj/bin):

| Source | Target |
|--------|--------|
| `Navigation/INavigationServices.cs` | `Infrastructure/Navigation/INavigationServices.cs` |
| `Navigation/NavigationServices.cs` | `Infrastructure/Navigation/NavigationServices.cs` |
| `Navigation/INavigationHistoryService.cs` | `Infrastructure/Navigation/INavigationHistoryService.cs` |
| `Navigation/NavigationHistoryService.cs` | `Infrastructure/Navigation/NavigationHistoryService.cs` |
| `Navigation/INavigationCoordinator.cs` | `Infrastructure/Navigation/INavigationCoordinator.cs` |
| `Navigation/NavigationCoordinator.cs` | `Infrastructure/Navigation/NavigationCoordinator.cs` |
| `Navigation/IRegionMonitor.cs` | `Infrastructure/Navigation/IRegionMonitor.cs` |
| `Navigation/RegionMonitor.cs` | `Infrastructure/Navigation/RegionMonitor.cs` |
| `Navigation/IModuleLazyLoader.cs` | `Infrastructure/Navigation/IModuleLazyLoader.cs` |
| `Navigation/ModuleLazyLoader.cs` | `Infrastructure/Navigation/ModuleLazyLoader.cs` |
| `Navigation/NavigationArgs/*.cs` | `Infrastructure/Navigation/NavigationArgs/*.cs` |

- [ ] **Step 2: Update namespaces in moved files**

Change `namespace LYBT.Desktop.Navigation` → `namespace LYBT.Desktop.Infrastructure.Navigation` in all moved files.

- [ ] **Step 3: Update using statements (~5 files)**

In Shell files, replace:
- `using LYBT.Desktop.Infrastructure.Navigation;` → `using LYBT.Desktop.Infrastructure.Navigation;`
- `using LYBT.Desktop.Infrastructure.Navigation.NavigationArgs;` → `using LYBT.Desktop.Infrastructure.Navigation.NavigationArgs;`

Files:
- `src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs`
- `src/Client/Desktop/Shell/Services/ShellEventServices.cs`
- `src/Client/Desktop/Shell/Services/IShellEventServices.cs`
- `src/Client/Desktop/Shell/Services/NavigationManager.cs`
- `src/Client/Desktop/Shell/Services/ShellEventCoordinator.cs`

- [ ] **Step 4: Delete Desktop.Navigation directory**

Delete: `src/Client/Desktop/Core/LYBT.Desktop.Navigation/`

- [ ] **Step 5: Remove from solution**

Run: `dotnet sln LYBTZYZS.sln remove src/Client/Desktop/Core/LYBT.Desktop.Navigation/LYBT.Desktop.Navigation.csproj`

- [ ] **Step 6: Build and test**

Run: `dotnet build LYBTZYZS.sln`
Expected: Build succeeds

Run: `dotnet test tests/LYBT.Tests.Architecture/`
Expected: All tests pass

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "refactor(Desktop): merge Desktop.Navigation into Desktop.Infrastructure, reduce 1 project"
```

---

### Task 4: Merge Shared.Components into Shared.Models

**Covers:** [S6]

**Files:**
- Move: 3 files from `Shared.Components/` → `Shared.Models/Contracts/Herbs/`
- Delete: `src/Shared/LYBT.Shared.Components/` directory
- Remove: `LYBT.Shared.Components` from solution
- Modify: `LYBT.Desktop.Controls.csproj`, `LYBT.Desktop.Infrastructure.csproj` (remove Components reference)

**Interfaces:**
- Produces: `LYBT.Shared.Models.Contracts.Herbs.IHerbItem`, `IHerbItemEditable`, `HerbValidatorBase<T>`

- [ ] **Step 1: Move files from Shared.Components to Shared.Models**

Move to `src/Shared/LYBT.Shared.Models/Contracts/Herbs/`:

| Source | Target | New Namespace |
|--------|--------|---------------|
| `Components/IHerbItem.cs` | `Models/Contracts/Herbs/IHerbItem.cs` | `LYBT.Shared.Models.Contracts.Herbs` |
| `Components/IHerbItemEditable.cs` | `Models/Contracts/Herbs/IHerbItemEditable.cs` | `LYBT.Shared.Models.Contracts.Herbs` |
| `Components/HerbValidatorBase.cs` | `Models/Contracts/Herbs/HerbValidatorBase.cs` | `LYBT.Shared.Models.Contracts.Herbs` |

- [ ] **Step 2: Update namespaces in moved files**

Change `namespace LYBT.Shared.Components;` → `namespace LYBT.Shared.Models.Contracts.Herbs;` in all moved files.

- [ ] **Step 3: Update using statements (~2 files)**

Replace:
- `using LYBT.Shared.Models.Contracts.Herbs;` → `using LYBT.Shared.Models.Contracts.Herbs;`

Files:
- `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/Base/HerbItemViewModelBase.cs`
- `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/HerbItem/HerbItemControlViewModel.cs`

- [ ] **Step 4: Remove Components references from csproj files**

In `LYBT.Desktop.Controls.csproj`, remove:
```xml
<ProjectReference Include="..\..\..\..\Shared\LYBT.Shared.Components\LYBT.Shared.Components.csproj" />
```

In `LYBT.Desktop.Infrastructure.csproj`, remove:
```xml
<ProjectReference Include="..\..\..\..\Shared\LYBT.Shared.Components\LYBT.Shared.Components.csproj" />
```

- [ ] **Step 5: Delete Shared.Components directory**

Delete: `src/Shared/LYBT.Shared.Components/`

- [ ] **Step 6: Remove from solution**

Run: `dotnet sln LYBTZYZS.sln remove src/Shared/LYBT.Shared.Components/LYBT.Shared.Components.csproj`

- [ ] **Step 7: Build and test**

Run: `dotnet build LYBTZYZS.sln`
Expected: Build succeeds

Run: `dotnet test tests/LYBT.Tests.Architecture/`
Expected: All tests pass

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "refactor(Shared): merge Shared.Components into Shared.Models, reduce 1 project"
```

---

### Task 5: Final Verification

**Covers:** [S7, S8]

- [ ] **Step 1: Full build**

Run: `dotnet build LYBTZYZS.sln`
Expected: Build succeeds with 0 errors

- [ ] **Step 2: Architecture tests**

Run: `dotnet test tests/LYBT.Tests.Architecture/`
Expected: All ~76 architecture guard tests pass

- [ ] **Step 3: Server tests**

Run: `dotnet test tests/LYBT.Tests.Server/`
Expected: All ~1185 tests pass

- [ ] **Step 4: Desktop tests**

Run: `dotnet test tests/LYBT.Tests.Desktop/`
Expected: All ~760 tests pass

- [ ] **Step 5: Verify project count**

Run: `(Get-ChildItem -Recurse -Filter "*.csproj" src/ | Where-Object { $_.FullName -notmatch "obj|bin" }).Count`
Expected: 36 (was 40)

- [ ] **Step 6: Verify no cross-tier violations**

Run: `Select-String -Path "src/Shared/**/*.csproj" -Pattern "LYBT\.(Module|WebAPI|Infrastructure|Entities|SharedKernel)" | Select-Object -ExpandProperty Filename`
Expected: No matches (Shared projects don't reference Server projects)

Run: `Select-String -Path "src/Client/**/*.csproj" -Pattern "LYBT\.(Module|WebAPI|Infrastructure|Entities|SharedKernel)" | Select-Object -ExpandProperty Filename`
Expected: No matches (Client projects don't reference Server projects, except LocalData→Entities which is documented)
