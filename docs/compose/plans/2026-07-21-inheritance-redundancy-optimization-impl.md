# 继承冗余与过度设计优化 实施计划

> [!NOTE]
> This document may not reflect the current implementation.
> See the final report for up-to-date state:
> [Final Report](../reports/inheritance-redundancy-optimization.md)

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 消除 12 处继承过深/过度设计/冗余设计问题，降低认知负担和维护成本

**Architecture:** 分 4 批次（P3→P4→P1→P2），每批次独立可验证。P3/P4 低风险先行，P1/P2 高影响后置。

**Tech Stack:** .NET 8, WPF/Prism, CommunityToolkit.Mvvm, EF Core

## Global Constraints

- `dotnet build LYBTZYZS.sln` 每个 Task 结束必须通过
- 所有现有测试不得回归
- 不引入新项目（.csproj），仅在现有项目内重构
- 不改变业务行为，纯结构性重构
- 中文注释/英文标识符

---

## Phase A: BaseRepository 瘦身（P3）

### Task 1: 提取分页扩展方法

**Covers:** [S5]

**Files:**
- Create: `src/Server/Core/LYBT.Infrastructure/Extensions/QueryablePagingExtensions.cs`
- Modify: `src/Server/Core/LYBT.Infrastructure/Repositories/BaseRepository.cs`

**Interfaces:**
- Produces: `QueryablePagingExtensions.GetPagedResultAsync<T>(IQueryable<T>, int, int)` 静态扩展方法

- [ ] **Step 1: 创建扩展方法文件**

```csharp
// src/Server/Core/LYBT.Infrastructure/Extensions/QueryablePagingExtensions.cs
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Infrastructure.Extensions;

/// <summary>
/// IQueryable 分页扩展方法
/// 从 BaseRepository.GetPagedResultAsync 提取
/// </summary>
public static class QueryablePagingExtensions
{
    /// <summary>
    /// 将 IQueryable 转换为分页结果
    /// </summary>
    public static async Task<PagedResult<T>> GetPagedResultAsync<T>(
        this IQueryable<T> query,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = pageNumber,
            PageSize = pageSize
        };
    }
}
```

- [ ] **Step 2: 修改 BaseRepository — 将 GetPagedResultAsync 替换为扩展方法**

在 `BaseRepository.cs` 中：
1. 添加 `using LYBT.Infrastructure.Extensions;`
2. 将 `protected async Task<PagedResult<TEntity>> GetPagedResultAsync(...)` 方法体改为调用扩展方法，或直接删除该方法（如果子类已改用扩展方法）

```csharp
// BaseRepository.cs 中删除或替换：
// 旧代码（约40行）:
protected async Task<PagedResult<TEntity>> GetPagedResultAsync(
    IQueryable<TEntity> query, int pageNumber, int pageSize)
{
    var totalCount = await query.CountAsync();
    var items = await query
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
    return new PagedResult<TEntity> { ... };
}

// 新代码: 直接删除此方法，子类改用扩展方法
```

- [ ] **Step 3: 更新所有子类 Repository 调用**

将所有 `_repository.GetPagedResultAsync(query, page, size)` 改为 `query.GetPagedResultAsync(page, size)`。

涉及文件（Server 端 Repository）：
- `src/Server/Modules/LYBT.Module.Formula/Repositories/FormulaRepository.cs`
- `src/Server/Modules/LYBT.Module.Herbs/Repositories/HerbRepository.cs`
- `src/Server/Modules/LYBT.Module.Patients/Repositories/PatientRepository.cs`
- `src/Server/Modules/LYBT.Module.Users/Repositories/UserRepository.cs`
- `src/Server/Modules/LYBT.Module.Registration/Repositories/RegistrationRepository.cs`
- `src/Server/Modules/LYBT.Module.MedicalCase/Repositories/MedicalCaseRepository.cs`

- [ ] **Step 4: 编译验证**

Run: `dotnet build LYBTZYZS.sln`
Expected: Build succeeded

- [ ] **Step 5: 运行 Server 测试**

Run: `dotnet test tests/LYBT.Tests.Server/`
Expected: All tests pass

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "refactor(server): extract QueryablePagingExtensions from BaseRepository"
```

---

### Task 2: 提取 SelectAsync 投影查询为扩展方法

**Covers:** [S5]

**Files:**
- Modify: `src/Server/Core/LYBT.Infrastructure/Extensions/QueryablePagingExtensions.cs`
- Modify: `src/Server/Core/LYBT.Infrastructure/Repositories/BaseRepository.cs`

- [ ] **Step 1: 添加 SelectAsync 扩展方法**

在 `QueryablePagingExtensions.cs` 中追加：

```csharp
/// <summary>
/// 投影查询 — 从 BaseRepository.SelectAsync 提取
/// </summary>
public static async Task<List<TResult>> SelectAsync<TEntity, TResult>(
    this IQueryable<TEntity> query,
    System.Linq.Expressions.Expression<Func<TEntity, bool>>? predicate,
    System.Linq.Expressions.Expression<Func<TEntity, TResult>> selector,
    CancellationToken cancellationToken = default)
    where TEntity : class
{
    if (predicate != null)
        query = query.Where(predicate);
    return await query.Select(selector).ToListAsync(cancellationToken);
}
```

- [ ] **Step 2: 从 BaseRepository 中删除 SelectAsync 方法**

删除 `BaseRepository.cs` 中的 `SelectAsync<TResult>` 方法（约20行）。

- [ ] **Step 3: 编译验证**

Run: `dotnet build LYBTZYZS.sln`

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "refactor(server): extract SelectAsync to QueryablePagingExtensions"
```

---

### Task 3: 简化 ApiClientRepositoryBase 泛型参数（4→2）

**Covers:** [S5]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Repositories/ApiClientRepositoryBase.cs`
- Modify: 所有 6 个继承它的 Repository

**Interfaces:**
- Before: `ApiClientRepositoryBase<TListDto, TDetailDto, TCreateDto, TUpdateDto>`
- After: `ApiClientRepositoryBase<TListDto, TDetailDto>`

- [ ] **Step 1: 重构 ApiClientRepositoryBase**

```csharp
// 新版本 — 2 个泛型参数
public abstract class ApiClientRepositoryBase<TListDto, TDetailDto>
{
    protected readonly ILogger Logger;

    protected ApiClientRepositoryBase(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected virtual string LogPrefix => GetType().Name;

    protected void HandleException(Exception ex, string operation, params object?[] args)
    {
        Logger.LogError(ex, $"[REPO] {{LogPrefix}}.{operation} failed", args);
        throw ex;
    }

    protected async Task ExecuteAsync(Func<Task> action, string operation, LogLevel logLevel = LogLevel.Debug)
    {
        try
        {
            Logger.Log(logLevel, "[REPO] {LogPrefix}.{Operation}", LogPrefix, operation);
            await action();
        }
        catch (Exception ex) { HandleException(ex, operation); }
    }

    protected async Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> func, string operation, LogLevel logLevel = LogLevel.Debug)
    {
        try
        {
            Logger.Log(logLevel, "[REPO] {LogPrefix}.{Operation}", LogPrefix, operation);
            return await func();
        }
        catch (Exception ex) { HandleException(ex, operation); throw; }
    }

    protected async Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> func, string operation, string logMessage, object?[] logArgs, LogLevel logLevel = LogLevel.Debug)
    {
        try
        {
            Logger.Log(logLevel, logMessage, logArgs);
            return await func();
        }
        catch (Exception ex) { HandleException(ex, operation); throw; }
    }
}
```

- [ ] **Step 2: 更新 6 个子类 Repository 声明**

将每个 Repository 的基类声明从 4 泛型改为 2 泛型：

```csharp
// Before:
public sealed class FormulaRepository : ApiClientRepositoryBase<FormulaListDto, FormulaDetailDto, FormulaInputDto, FormulaInputDto>

// After:
public sealed class FormulaRepository : ApiClientRepositoryBase<FormulaListDto, FormulaDetailDto>
```

涉及文件：
- `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Repositories/FormulaRepository.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Repositories/HerbRepository.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Repositories/PatientRepository.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.Users/Repositories/UserRepository.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Repositories/MedicalCaseRepository.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.Registration/Repositories/RegistrationRepository.cs`

- [ ] **Step 3: 编译验证**

Run: `dotnet build LYBTZYZS.sln`

- [ ] **Step 4: 运行 Desktop 测试**

Run: `dotnet test tests/LYBT.Tests.Desktop/`

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "refactor(desktop): simplify ApiClientRepositoryBase from 4 to 2 generic params"
```

---

## Phase B: DTO 继承精简 + CrossModule 接口合并（P4）

### Task 4: 合并 ICreatorTrackable 到 IAuditable，删除 StatusDto 继承层

**Covers:** [S6]

**Files:**
- Modify: `src/Shared/LYBT.Shared.Models/Contracts/Common/DtoBase.cs`
- Modify: 所有实现 `ICreatorTrackable` 的 DTO
- Modify: 所有继承 `StatusDto` 的 DTO

- [ ] **Step 1: 统计 StatusDto 和 ICreatorTrackable 使用情况**

搜索 `: StatusDto` 和 `ICreatorTrackable` 确认影响范围。

- [ ] **Step 2: 合并 ICreatorTrackable 到 IAuditable**

```csharp
// DtoBase.cs — 修改 IAuditable
public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
    Guid? CreatedBy { get; set; }  // 从 ICreatorTrackable 合并
}

// 删除 ICreatorTrackable 接口定义
```

- [ ] **Step 3: 更新 TimestampDto**

```csharp
public abstract class TimestampDto : BaseDto, IAuditable  // 移除 ICreatorTrackable
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
}
```

- [ ] **Step 4: 更新所有实现 ICreatorTrackable 的 DTO**

将 `ICreatorTrackable` 从接口列表中移除（已通过 `IAuditable` 获得）。

涉及文件需搜索确认，预期在：
- `src/Shared/LYBT.Shared.Models/Contracts/*/` 下的各 DetailDto

- [ ] **Step 5: 精简 StatusDto 继承层**

将继承 `StatusDto` 的 DTO 改为继承 `TimestampDto` + 直接添加 `Status` 属性。

```csharp
// Before:
public class XxxDetailDto : StatusDto { ... }

// After:
public class XxxDetailDto : TimestampDto
{
    public CommonStatus Status { get; set; } = CommonStatus.Enabled;
    public bool IsEnabled => Status == CommonStatus.Enabled;
}
```

- [ ] **Step 6: 编译验证**

Run: `dotnet build LYBTZYZS.sln`

- [ ] **Step 7: 全量测试**

Run: `dotnet test tests/LYBT.Tests.Server/ && dotnet test tests/LYBT.Tests.Desktop/`

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "refactor(shared): flatten DTO hierarchy — merge ICreatorTrackable into IAuditable, remove StatusDto layer"
```

---

### Task 5: 合并 CrossModule 接口为单一接口

**Covers:** [S6]

**Files:**
- Modify/Create: `src/Server/Core/LYBT.Infrastructure/Interfaces/ICrossModuleService.cs`
- Modify: 各模块中的 CrossModuleService 实现
- Modify: 各模块中注入域专用接口的 Service

- [ ] **Step 1: 读取现有 4 个域接口，确认方法清单**

读取：
- `IPatientCrossModuleService`
- `IHerbCrossModuleService`
- `IUserCrossModuleService`
- `ICrossModuleAuthService`

- [ ] **Step 2: 创建统一 ICrossModuleService 接口**

```csharp
// src/Server/Core/LYBT.Infrastructure/Interfaces/ICrossModuleService.cs
namespace LYBT.Infrastructure.Interfaces;

/// <summary>
/// 跨模块通信服务 — 统一接口
/// 替代旧的 IPatientCrossModuleService/IHerbCrossModuleService/IUserCrossModuleService/ICrossModuleAuthService
/// </summary>
public interface ICrossModuleService
{
    // Patient
    Task<PatientBasicInfo?> GetPatientBasicInfoAsync(Guid patientId, CancellationToken ct = default);
    Task<bool> HasPatientReferencesAsync(Guid patientId, CancellationToken ct = default);

    // Herb
    Task<HerbBasicInfo?> GetHerbBasicInfoAsync(Guid herbId, CancellationToken ct = default);
    Task<bool> HasHerbReferencesAsync(Guid herbId, CancellationToken ct = default);

    // User
    Task<UserBasicInfo?> GetUserBasicInfoAsync(Guid userId, CancellationToken ct = default);
    Task<bool> ValidateUserCredentialsAsync(string username, string password, CancellationToken ct = default);

    // Auth
    Task RevokeTokenFamilyAsync(string familyId, CancellationToken ct = default);
}
```

- [ ] **Step 3: 创建合并实现类**

```csharp
// src/Server/Core/LYBT.Infrastructure/Services/CrossModuleService.cs
public class CrossModuleService : ICrossModuleService
{
    // 委托给现有的各域 Service
}
```

- [ ] **Step 4: 更新所有注入点**

将各模块中注入 `IPatientCrossModuleService` / `IHerbCrossModuleService` 等改为注入 `ICrossModuleService`。

- [ ] **Step 5: 删除旧的 4 个域接口文件**

- [ ] **Step 6: 编译验证 + 测试**

Run: `dotnet build LYBTZYZS.sln && dotnet test tests/LYBT.Tests.Server/`

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "refactor(server): consolidate 4 CrossModule interfaces into single ICrossModuleService"
```

---

## Phase C: IViewModelServices 内部解构（P1）

### Task 6: CoreViewModelBase 内部提取服务为 protected 属性

**Covers:** [S3]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/Base/CoreViewModelBase.cs`

- [ ] **Step 1: 重构 CoreViewModelBase 构造函数**

将 `IViewModelServices` 的属性提取为 protected 字段：

```csharp
protected CoreViewModelBase(IViewModelServices services)
{
    Services = services ?? throw new ArgumentNullException(nameof(services));
    Logger = services.LoggerFactory.CreateLogger(GetType());
    EventAggregator = services.EventAggregator;
    UiThreadDispatcher = services.UiThreadDispatcher;
    // 不再存储 Services 引用（子类通过 protected 属性访问）
}
```

保留 `Services` 属性但标记为 `[Obsolete("Use protected properties directly")]`。

- [ ] **Step 2: 编译验证**

Run: `dotnet build src/Client/Desktop/`

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "refactor(desktop): CoreViewModelBase — extract services to protected properties"
```

---

### Task 7: NavigableViewModelBase 内部提取服务为 protected 属性

**Covers:** [S3]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/Base/NavigableViewModelBase.cs`

- [ ] **Step 1: 重构 NavigableViewModelBase 构造函数**

```csharp
protected NavigableViewModelBase(IViewModelServices services)
    : base(services)
{
    RegionManager = services.RegionManager;
    SessionManager = services.SessionManager;
    UserNotificationService = services.UserNotificationService;
    CommonDialogService = services.CommonDialogService;
    ToastService = services.ToastService;
    RoleRegistry = services.RoleRegistry;
}
```

所有子类通过 `RegionManager`、`SessionManager` 等 protected 属性访问，不再通过 `Services.RegionManager`。

- [ ] **Step 2: 全局搜索替换 `Services.` 调用**

搜索所有具体 ViewModel 中的 `Services.XXX` 调用，替换为直接属性名（`XXX`）。

- [ ] **Step 3: 编译验证 + 测试**

Run: `dotnet build LYBTZYZS.sln && dotnet test tests/LYBT.Tests.Desktop/`

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "refactor(desktop): NavigableViewModelBase — extract services to protected properties"
```

---

## Phase D: ViewModel 继承链扁平化（P2）

### Task 8: 合并 CoreViewModelBase 到 NavigableViewModelBase

**Covers:** [S4]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/Base/NavigableViewModelBase.cs`
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/Base/CoreViewModelBase.cs`
- Modify: 所有引用 `CoreViewModelBase` 的文件（~30个）

- [ ] **Step 1: 将 CoreViewModelBase 的全部成员合并到 NavigableViewModelBase**

将 `CoreViewModelBase.cs` 中的以下内容合并到 `NavigableViewModelBase.cs`：
- `using` 语句
- `[ObservableProperty]` 字段：`IsBusy`、`StatusMessage`、`ErrorMessage`
- `OnIsBusyChanged` partial method
- `OnIsBusyChangedCore` 虚方法
- `SetBusy`、`ClearError`、`SetError` 方法
- `IViewModelServices Services` 属性
- `ILoggerFactory LoggerFactory` 属性

- [ ] **Step 2: 删除 CoreViewModelBase.cs**

- [ ] **Step 3: 全局搜索替换**

将所有 `: CoreViewModelBase` 替换为 `: NavigableViewModelBase`（如果直接继承 CoreViewModelBase 的类）
将所有 `base.CoreViewModelBase(...)` 替换为 `base.NavigableViewModelBase(...)`

- [ ] **Step 4: 编译验证**

Run: `dotnet build LYBTZYZS.sln`

- [ ] **Step 5: 运行全量测试**

Run: `dotnet test tests/LYBT.Tests.Desktop/`

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "refactor(desktop): flatten ViewModel inheritance — merge CoreViewModelBase into NavigableViewModelBase"
```

---

## 验证清单

所有 Task 完成后，执行最终验证：

- [ ] `dotnet build LYBTZYZS.sln` — 0 errors, 0 warnings (除 obsolete 警告)
- [ ] `dotnet test tests/LYBT.Tests.Server/` — All pass
- [ ] `dotnet test tests/LYBT.Tests.Desktop/` — All pass
- [ ] `dotnet test tests/LYBT.Tests.Architecture/` — All pass
- [ ] 继承链验证：ViewModel ≤ 3 层
- [ ] DTO 继承链验证：≤ 2 层
- [ ] BaseRepository 行数验证：< 400 行
- [ ] ApiClientRepositoryBase 泛型参数验证：≤ 2 个
- [ ] CrossModule 接口验证：≤ 2 个
