# 项目差距修复 — 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 修复经代码核实后确认的项目差距：返回类型不一致、UserService 重构、TODO 清理、过时文档归档。

**Architecture:** 4个阶段按依赖顺序执行。Phase 1 为基础设施统一（影响所有模块），Phase 2 为单一模块重构，Phase 3-4 为清理工作。每阶段独立可测试。

**Tech Stack:** C# / .NET 8 / Refit / ASP.NET Core / EF Core

---

## 核实说明

经逐文件核实，原差距报告 (`docs/api-endpoint-gap-report.md`) 已过时：
- 所有 Local Refit 接口（MedicalCase/Registration/Patients/Herbs/Formula）**已补齐全部方法**
- CORS `AllowAnyOrigin` 问题**已修复**
- AuthService 实际 363 行（非 845 行），**无需拆分**

本计划仅包含经核实确认的待修复项。

---

## Phase 1: 返回类型一致性统一

**目标**: 消除 Remote/Local 返回类型差异，使 Repository 层解包逻辑统一。

**背景**: Remote Refit 接口返回 `ApiResponse<T>`（需要 `.Data` 解包），Local Refit 接口直接返回 DTO。Repository 层需要条件分支处理两种情况。

**策略**: 不改变 Local Controller 行为（已正确返回 DTO），而是在 Repository 层引入统一解包辅助方法。

### Task 1.1: 创建 ApiResponse 解包辅助方法

**Files:**
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Http/ApiResponseHelper.cs`

- [ ] **Step 1: 创建辅助类**

```csharp
// src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Http/ApiResponseHelper.cs
namespace LYBT.Desktop.Infrastructure.Http;

/// <summary>
/// 统一处理 Remote (ApiResponse&lt;T&gt;) 和 Local (直接 DTO) 的返回值解包。
/// Remote 模式下 Refit 返回 ApiResponse&lt;T&gt;，需要 .Data 访问实际数据。
/// Local 模式下 Refit 直接返回 DTO，无需解包。
/// </summary>
public static class ApiResponseHelper
{
    /// <summary>
    /// 从 ApiResponse&lt;T&gt; 或直接 T 中提取数据。
    /// 如果 response 是 ApiResponse&lt;T&gt;，返回 .Data。
    /// 如果 response 已经是 T，直接返回。
    /// </summary>
    public static T Unwrap<T>(T response) where T : class
    {
        // ApiResponse<T> 的 Data 属性在成功时非空
        // 如果 T 本身就是 ApiResponse<T>，调用者应直接访问 .Data
        // 此方法用于统一 Repository 层的返回值处理
        return response;
    }

    /// <summary>
    /// 检查 ApiResponse 是否成功（对于直接返回 DTO 的 Local 模式，始终返回 true）
    /// </summary>
    public static bool IsSuccess<T>(ApiResponse<T>? apiResponse) where T : class
    {
        return apiResponse?.IsSuccess ?? false;
    }
}
```

- [ ] **Step 2: 编译验证**

Run: `dotnet build src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/`
Expected: 0 errors

- [ ] **Step 3: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Http/ApiResponseHelper.cs
git commit -m "feat(desktop): Add ApiResponse helper for Remote/Local return type unification"
```

### Task 1.2: 审计 Repository 层返回类型处理

**Files:**
- Read: `src/Client/Desktop/Modules/*/Repositories/*.cs` — 所有模块 Repository

- [ ] **Step 1: 扫描 Repository 中的 ApiResponse 解包逻辑**

```bash
grep -rn "ApiResponse\|\.Data\b" src/Client/Desktop/Modules/*/Repositories/*.cs
```

记录每个 Repository 中 Remote/Local 分支的返回类型处理方式。

- [ ] **Step 2: 记录审计结果**

将发现的差异记录到 `docs/api-return-type-audit.md`，标记哪些 Repository 需要统一。

- [ ] **Step 3: Commit**

```bash
git add docs/api-return-type-audit.md
git commit -m "docs: Add API return type audit for Remote/Local consistency"
```

---

## Phase 2: UserService 重构

**目标**: 将 497 行的 UserService 拆分为 UserQueryService + UserCommandService，保持原接口为 Facade。

**策略**: Extract Class 模式 — 原 UserService 保留为 Facade，内部委托给子服务。所有现有调用者不受影响。

### Task 2.1: 为 UserService 补充测试

**Files:**
- Create: `tests/LYBT.Tests.Server/PureLogic/Users/UserServiceRefactoringTests.cs`

- [ ] **Step 1: 读取 UserService 了解公共方法**

```bash
grep -n "public.*async\|public.*Task" src/Server/Modules/LYBT.Module.Users/Services/UserService.cs
```

- [ ] **Step 2: 编写 UserService 的行为测试**

为 UserService 的每个公共方法编写集成测试，确保重构前有基线。测试应覆盖：
- CRUD 操作（Create/Get/Update/Delete）
- 列表查询（分页、搜索）
- 批量操作（BatchDelete）
- 密码重置

- [ ] **Step 3: 运行测试确认基线**

Run: `dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~UserServiceRefactoring"`
Expected: 全部 PASS

- [ ] **Step 4: Commit**

```bash
git add tests/LYBT.Tests.Server/PureLogic/Users/UserServiceRefactoringTests.cs
git commit -m "test(server): Add UserService baseline tests before refactoring"
```

### Task 2.2: 提取 UserQueryService

**Files:**
- Create: `src/Server/Modules/LYBT.Module.Users/Services/UserQueryService.cs`
- Modify: `src/Server/Modules/LYBT.Module.Users/Services/UserService.cs`

- [ ] **Step 1: 创建 UserQueryService**

将 UserService 中的查询方法（GetById, GetList, Search 等）提取到 UserQueryService。

```csharp
// src/Server/Modules/LYBT.Module.Users/Services/UserQueryService.cs
namespace LYBT.Module.Users.Services;

public class UserQueryService
{
    private readonly IUserRepository _repository;
    private readonly ILogger<UserQueryService> _logger;

    public UserQueryService(IUserRepository repository, ILogger<UserQueryService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    // 从 UserService 移植查询方法
    // ...
}
```

- [ ] **Step 2: 修改 UserService 委托查询方法**

```csharp
// UserService.cs 中将查询方法委托给 UserQueryService
private readonly UserQueryService _queryService;

public async Task<UserDetailDto> GetByIdAsync(Guid id, CancellationToken ct)
{
    return await _queryService.GetByIdAsync(id, ct);
}
```

- [ ] **Step 3: 运行测试确认不回归**

Run: `dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~User"`
Expected: 全部 PASS

- [ ] **Step 4: Commit**

```bash
git add src/Server/Modules/LYBT.Module.Users/Services/
git commit -m "refactor(server): Extract UserQueryService from UserService"
```

### Task 2.3: 提取 UserCommandService

**Files:**
- Create: `src/Server/Modules/LYBT.Module.Users/Services/UserCommandService.cs`
- Modify: `src/Server/Modules/LYBT.Module.Users/Services/UserService.cs`

- [ ] **Step 1: 创建 UserCommandService**

将 UserService 中的命令方法（Create, Update, Delete, ResetPassword, BatchDelete 等）提取到 UserCommandService。

- [ ] **Step 2: 修改 UserService 委托命令方法**

- [ ] **Step 3: 运行全量测试**

Run: `dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~User"`
Expected: 全部 PASS

- [ ] **Step 4: Commit**

```bash
git add src/Server/Modules/LYBT.Module.Users/Services/
git commit -m "refactor(server): Extract UserCommandService from UserService"
```

### Task 2.4: 注册子服务 DI

**Files:**
- Modify: `src/Server/Modules/LYBT.Module.Users/` — DI 注册扩展方法

- [ ] **Step 1: 在模块 DI 注册中添加 UserQueryService 和 UserCommandService**

确保 UserService、UserQueryService、UserCommandService 都注册到 DI 容器。

- [ ] **Step 2: 编译验证**

Run: `dotnet build src/Server/Services/LYBT.WebAPI/`
Expected: 0 errors

- [ ] **Step 3: 运行全量测试**

Run: `dotnet test tests/LYBT.Tests.Server/`
Expected: 全部 PASS

- [ ] **Step 4: Commit**

```bash
git add src/Server/Modules/LYBT.Module.Users/
git commit -m "refactor(server): Register UserQueryService and UserCommandService in DI"
```

---

## Phase 3: TODO 清理

**目标**: 清理源码中 12 处 TODO 注释，关联到具体 Issue 或删除。

### Task 3.1: Navigation TODO 清理 (8处)

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Navigation/EnhancedNavigationService.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Navigation/NavigationShortcuts.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Navigation/NavigationAnalyticsService.cs`
- Modify: `src/Client/Desktop/Shell/Services/MenuManager.cs`

- [ ] **Step 1: 评估每个 TODO 的必要性**

对每个 TODO 决策：实现 / 关联 Issue / 删除（功能不需要）。

| 文件 | TODO | 建议决策 |
|------|------|----------|
| EnhancedNavigationService:486 | Implement state restoration | 评估：是否为 PRD 需求？如不是，删除 TODO 并保留空方法 |
| EnhancedNavigationService:664 | Subscribe to region navigation events | 删除 TODO，保留空方法（非核心功能） |
| EnhancedNavigationService:673 | Publish navigation event | 删除 TODO，保留空方法 |
| NavigationShortcuts:102 | Implement show history panel | 删除 TODO，保留空方法 |
| NavigationShortcuts:112 | Implement cycle through regions | 删除 TODO，保留空方法 |
| NavigationAnalyticsService:503 | Integrate with authentication service | 关联到具体 Issue 或使用 ICurrentUserService |
| NavigationAnalyticsService:507 | Get from IAuthenticationService | 同上 |
| MenuManager:222 | Publish event to open/focus NavigationHistoryPanel | 删除 TODO，保留通知占位 |
| MenuManager:231 | Implement region cycling logic | 删除 TODO，保留通知占位 |

- [ ] **Step 2: 执行清理**

对每个文件：删除 TODO 注释，保留代码逻辑。如需关联 Issue，添加 `// Issue #XXX: description` 格式。

- [ ] **Step 3: 编译验证**

Run: `dotnet build LYBT.Desktop.sln`
Expected: 0 errors

- [ ] **Step 4: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Navigation/
git add src/Client/Desktop/Shell/Services/MenuManager.cs
git commit -m "chore(desktop): Clean up 8 Navigation/Menu TODO comments"
```

### Task 3.2: MedicalCase 价格刷新 TODO 清理 (3处)

**Files:**
- Modify: `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseCommandService.cs`

- [ ] **Step 1: 评估价格刷新 TODO**

3 处 TODO 均在 `MedicalCaseCommandService.cs` 的处方复制逻辑中（line 368, 375, 387），关联 US-MC-016。

决策：
- 如 US-MC-016 已实现：删除 TODO
- 如 US-MC-016 未实现：保留 TODO 并添加 Issue 链接
- 如不需要实现：删除 TODO 和相关占位代码

- [ ] **Step 2: 执行清理**

- [ ] **Step 3: 编译验证**

Run: `dotnet build src/Server/Services/LYBT.WebAPI/`
Expected: 0 errors

- [ ] **Step 4: Commit**

```bash
git add src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseCommandService.cs
git commit -m "chore(server): Clean up 3 price refresh TODO comments in MedicalCaseCommandService"
```

---

## Phase 4: 文档清理

**目标**: 归档过时计划文档，更新差距报告。

### Task 4.1: 归档 2026-03 及更早的计划文档

**Files:**
- Move: `docs/plans/2026-03-*.md` → `docs/plans/archive/`

- [ ] **Step 1: 列出待归档文件**

```bash
ls docs/plans/2026-03-*.md docs/plans/2025-*.md 2>/dev/null
```

- [ ] **Step 2: 移动到归档目录**

```bash
mkdir -p docs/plans/archive/2026-03
mv docs/plans/2026-03-*.md docs/plans/archive/2026-03/
mv docs/plans/2025-*.md docs/plans/archive/2026-03/
```

- [ ] **Step 3: 更新 docs/plans/README.md 索引**

- [ ] **Step 4: Commit**

```bash
git add docs/plans/
git commit -m "docs: Archive 2026-03 and earlier plan documents"
```

### Task 4.2: 更新差距报告

**Files:**
- Modify: `docs/api-endpoint-gap-report.md`
- Modify: `docs/remote-vs-local-api-gap-report.md`

- [ ] **Step 1: 在差距报告顶部添加过时声明**

在两个报告顶部添加：

```markdown
> **⚠️ 状态: 已过时 (2026-05-04)**
> 经代码核实，本报告中列出的 Local Refit 缺失方法已全部补齐。
> 当前实际差距见 `docs/superpowers/specs/2026-05-04-project-gap-fix-design.md`。
```

- [ ] **Step 2: Commit**

```bash
git add docs/api-endpoint-gap-report.md docs/remote-vs-local-api-gap-report.md
git commit -m "docs: Mark outdated gap reports with current status"
```

---

## 验收标准

完成全部 4 个 Phase 后：

1. `dotnet build LYBTZYZS.sln` — 零错误
2. `dotnet test LYBTZYZS.sln` — 全部通过
3. `grep -r "TODO" --include="*.cs" src/` — TODO 数量 ≤ 3（仅保留有 Issue 关联的）
4. `ls docs/plans/2026-03-*` — 无文件（已归档）
5. `ls docs/plans/ | wc -l` — ≤ 20 个活跃文件

<!-- MANUAL: -->
