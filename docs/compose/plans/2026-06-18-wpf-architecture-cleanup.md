# WPF 架构整理 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 4 个超过 590 行的核心文件拆分为职责单一的小文件（目标 <300 行/文件），不改公开 API。

**Architecture:** 纯提取——将逻辑从大文件搬到新文件，原文件变为协调者。所有公开接口不变。每个 Task 独立可提交。

**Tech Stack:** C# / .NET 8 / WPF / Prism / CommunityToolkit.Mvvm

---

### Task 1: MainWindowViewModel 拆分 — Token/时钟/活动追踪移到现有服务

**Covers:** [S4], [S3]

**Files:**
- Modify: `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs` (813行→~350行)
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Security/TokenLifecycleService.cs` — 接收从 VM 移出的 Token 监控逻辑
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/` 下的时钟/活动追踪服务

- [ ] **Step 1: 读取 MainWindowViewModel.cs 全文，标注每个 region 的行数和职责**

Run: `Read MainWindowViewModel.cs` — 标出以下 region 的行范围：
- Token 生命周期监控
- 时钟 DispatcherTimer
- 用户活动追踪
- 连接模式管理
- 导航状态
- 登录/登出
- UI 状态属性
- 菜单/快捷键

- [ ] **Step 2: 将 Token 监控逻辑移入 TokenLifecycleService**

找到 MainWindowViewModel 中的 `_tokenLifecycleService` 相关事件订阅和处理方法（如 `StartTokenLifecycleMonitoringAsync`, `OnTokenExpiring` 等）。将这些方法的实现移到 `TokenLifecycleService` 类中。MainWindowViewModel 仅调用 `_tokenLifecycleService.StartMonitoringAsync()`。

- [ ] **Step 3: 将时钟更新逻辑移入已有 IApplicationTickService 实现**

找到 MainWindowViewModel 中的 DispatcherTimer / CurrentTime 更新逻辑。移到 `ApplicationTickService` 中。MainWindowViewModel 仅绑定 `CurrentTime` 属性到服务。

- [ ] **Step 4: 将用户活动追踪逻辑移入已有 IUserActivityTracker 实现**

找到 MainWindowViewModel 中的 `OnUserActivity`, `ResetIdleTimer` 等方法。移到 `UserActivityTracker` 中。

- [ ] **Step 5: 删除 MainWindowViewModel 中已移出的方法、字段、事件订阅**

清理无用的 using 语句。

- [ ] **Step 6: 构建验证**

Run: `dotnet build LYBTZYZS.sln --no-restore`
Expected: 0 errors

- [ ] **Step 7: Commit**

```bash
git commit -m "refactor(arch): extract Token/Clock/Activity logic from MainWindowViewModel to existing services"
```

### Task 2: EnhancedNavigationService 拆分 — 面包屑/历史/建议分离

**Covers:** [S5], [S3]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Navigation/EnhancedNavigationService.cs` (612行→~200行)
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Navigation/BreadcrumbManager.cs`
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Navigation/NavigationHistoryManager.cs`

- [ ] **Step 1: 读取 EnhancedNavigationService.cs 全文**

标注面包屑相关方法、历史相关方法、搜索建议相关方法。

- [ ] **Step 2: 创建 BreadcrumbManager.cs**

提取面包屑的增删改查逻辑：`AddBreadcrumb`, `ClearBreadcrumbs`, `UpdateBreadcrumbs`, `NavigateToBreadcrumb`。

- [ ] **Step 3: 创建 NavigationHistoryManager.cs**

提取历史记录管理：`AddToHistory`, `ClearHistory`, `GetHistory`, 前进栈管理。

- [ ] **Step 4: EnhancedNavigationService 改为委托**

将面包屑和历史操作委托给新创建的 Manager。EnhancedNavigationService 仅保留：NavigateTo, NavigateBack, NavigateForward, 事件发布。

- [ ] **Step 5: 构建验证**

Run: `dotnet build LYBTZYZS.sln --no-restore`
Expected: 0 errors

- [ ] **Step 6: Commit**

```bash
git commit -m "refactor(arch): extract Breadcrumb and History managers from EnhancedNavigationService"
```

### Task 3: MasterDetailViewModelBase 瘦身 — 委托给组合服务

**Covers:** [S6], [S3]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/MasterDetailViewModelBase.cs` (590行→~250行)

- [ ] **Step 1: 读取 MasterDetailViewModelBase.cs 全文**

识别哪些逻辑可以直接委托给已注入的 `IListViewServices<T>` / `IMasterDetailServices<T,TDetail>`。

- [ ] **Step 2: 将分页属性改为委托**

```csharp
// Before:
public int CurrentPage { get; set; }
public int TotalPages { get; set; }

// After:
public int CurrentPage => MasterDetailServices.Pagination.CurrentPage;
public int TotalPages => MasterDetailServices.Pagination.TotalPages;
```

- [ ] **Step 3: 将搜索属性改为委托**

```csharp
public string SearchText
{
    get => MasterDetailServices.Search.SearchText;
    set => MasterDetailServices.Search.SearchText = value;
}
```

- [ ] **Step 4: 将选择逻辑改为委托**

Items, SelectedItem 通过 MasterDetailServices.Selection 访问。

- [ ] **Step 5: 清理不再需要的字段和 backing store**

删除 `_currentPage`, `_searchText`, `_selectedItem` 等私有字段。

- [ ] **Step 6: 构建验证**

Run: `dotnet build LYBTZYZS.sln --no-restore`
Expected: 0 errors

- [ ] **Step 7: Commit**

```bash
git commit -m "refactor(arch): slim MasterDetailViewModelBase by delegating to composition services"
```

### Task 4: CredentialVault 拆分 — DPAPI/存储/策略分离

**Covers:** [S7], [S3]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Security/CredentialVault.cs` (606行→~150行)
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Security/DpapiProtector.cs` (internal)
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Security/CredentialStorage.cs` (internal)
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Security/PasswordPolicy.cs` (internal)

- [ ] **Step 1: 读取 CredentialVault.cs 全文**

识别 DPAPI 加密/解密方法、文件 I/O 方法、密码强度检查方法。

- [ ] **Step 2: 创建 DpapiProtector.cs (internal)**

提取 `Protect`, `Unprotect`, `EncryptString`, `DecryptString` 等 DPAPI 操作。

- [ ] **Step 3: 创建 CredentialStorage.cs (internal)**

提取文件读写：`LoadCredentialsFromFile`, `SaveCredentialsToFile`, `GetCredentialFilePath`。

- [ ] **Step 4: 创建 PasswordPolicy.cs (internal)**

提取密码强度检查：`ValidatePasswordStrength`, `CheckPasswordRules`。

- [ ] **Step 5: CredentialVault 改为协调者**

`ICredentialVault` 接口不变。内部实例化三个 internal 类并委托调用。

- [ ] **Step 6: 构建验证**

Run: `dotnet build LYBTZYZS.sln --no-restore`
Expected: 0 errors

- [ ] **Step 7: Commit**

```bash
git commit -m "refactor(arch): split CredentialVault into DpapiProtector/CredentialStorage/PasswordPolicy"
```

### Task 5: 最终构建 + 验证

**Covers:** [S2], [S3]

- [ ] **Step 1: 全量构建**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

- [ ] **Step 2: 验证文件行数下降**

Run PowerShell:
```powershell
@("MainWindowViewModel.cs","EnhancedNavigationService.cs","MasterDetailViewModelBase.cs","CredentialVault.cs") | ForEach-Object {
    $f = Get-ChildItem -Recurse -Include $_ -Path src/Client/Desktop | Where-Object { $_.FullName -notmatch '\\obj\\' }
    [PSCustomObject]@{File=$_; Lines=(Get-Content $f.FullName | Measure-Object -Line).Lines}
} | Format-Table -AutoSize
```
Expected: 每个文件显著降低

- [ ] **Step 3: 提交最终状态**

```bash
git add -A
git commit -m "refactor(arch): architecture cleanup complete — 4 large files split into focused units"
```
