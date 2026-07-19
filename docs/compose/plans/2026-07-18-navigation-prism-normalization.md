# 导航架构 Prism 规范化重构 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将导航架构对齐 Prism 推荐模式，清理死代码、修复 KeepAlive 默认值、消除同名视图冲突、委托前进/后退给 Prism Journal。

**Architecture:** 4 个独立改动，每个改动影响 1-3 个文件，无交叉依赖。改动顺序：S1（RegionNames 清理）→ S2（KeepAlive）→ S3（AdminModule 去重）→ S4（Journal 委托）。

**Tech Stack:** C# / .NET 8 / Prism.DryIoc / WPF

## Global Constraints

- 编译命令: `dotnet build LYBTZYZS.sln`
- 模块间禁止直接引用
- Code Style: 中文业务文档/注释，英文标识符
- 不添加新功能，仅重构现有导航逻辑

---

### Task 1: 清理 RegionNames 死代码

**Covers:** [S1]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Constants/RegionNames.cs`

**Interfaces:**
- Consumes: 无
- Produces: `RegionNames.ContentRegion`, `RegionNames.LoginRegion`（保留）

- [ ] **Step 1: 读取当前 RegionNames.cs**

确认文件内容，标记要删除的常量。

- [ ] **Step 2: 删除 13 个未使用的 Region 常量**

删除以下常量（保留 `ContentRegion` 和 `LoginRegion`）:
- `NavigationRegion`
- `ToolbarRegion`
- `StatusBarRegion`
- `SidebarRegion`
- `ModuleRegion`
- `PatientRegion`
- `ConsultationRegion`
- `PrescriptionRegion`
- `HerbRegion`
- `FormulaRegion`
- `SettingsRegion`
- `MainWindowRegion`
- `DialogRegion`

修改后的文件内容：

```csharp
namespace LYBT.Desktop.Infrastructure.Constants
{
    /// <summary>
    /// 区域名称常量
    /// UltraThink架构优化 - 统一区域管理
    /// </summary>
    public static class RegionNames
    {
        /// <summary>
        /// 主内容区域
        /// </summary>
        public const string ContentRegion = "ContentRegion";

        /// <summary>
        /// 登录区域
        /// </summary>
        public const string LoginRegion = "LoginRegion";
    }
}
```

- [ ] **Step 3: 编译验证**

Run: `dotnet build LYBTZYZS.sln`
Expected: 编译通过，无 CS0162 或引用错误

- [ ] **Step 4: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Constants/RegionNames.cs
git commit -m "refactor(navigation): remove unused RegionNames constants, keep only ContentRegion and LoginRegion"
```

---

### Task 2: KeepAlive 默认值改为 false

**Covers:** [S2]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/Base/NavigableViewModelBase.cs:115`
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/ClinicalWorkspaceViewModel.cs`（需确认 KeepAlive 重写）
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/MedicalCaseWorkspaceViewModel.cs`（需确认 KeepAlive 重写）

**Interfaces:**
- Consumes: 无
- Produces: `NavigableViewModelBase.KeepAlive` 默认 `false`

- [ ] **Step 1: 修改 NavigableViewModelBase 的 KeepAlive 默认值**

将 `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/Base/NavigableViewModelBase.cs` 第 115 行：

```csharp
// 修改前:
public virtual bool KeepAlive => true;

// 修改后:
public virtual bool KeepAlive => false;
```

- [ ] **Step 2: 为 ClinicalWorkspaceViewModel 添加 KeepAlive = true 重写**

确认 `ClinicalWorkspaceViewModel` 中是否已有 `KeepAlive` 重写。如果没有，在类中添加：

```csharp
/// <summary>医生工作台需要保持状态（患者选择、看诊上下文）</summary>
public override bool KeepAlive => true;
```

- [ ] **Step 3: 为 MedicalCaseWorkspaceViewModel 添加 KeepAlive = true 重写**

确认 `MedicalCaseWorkspaceViewModel` 中是否已有 `KeepAlive` 重写。如果没有，在类中添加：

```csharp
/// <summary>医案工作区需要保持状态（编辑中的医案数据）</summary>
public override bool KeepAlive => true;
```

- [ ] **Step 4: 编译验证**

Run: `dotnet build LYBTZYZS.sln`
Expected: 编译通过

- [ ] **Step 5: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/Base/NavigableViewModelBase.cs src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/ClinicalWorkspaceViewModel.cs src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/MedicalCaseWorkspaceViewModel.cs
git commit -m "refactor(navigation): change KeepAlive default to false, explicitly keep clinical workspaces alive"
```

---

### Task 3: 删除 AdminModule 重复的视图注册

**Covers:** [S3]

**Files:**
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Admin/AdminModule.cs`

**Interfaces:**
- Consumes: 无
- Produces: 无（纯删除）

- [ ] **Step 1: 删除 AdminModule 中 4 个重复的 RegisterForNavigation**

从 `src/Client/Desktop/Roles/LYBT.Desktop.Admin/AdminModule.cs` 的 `RegisterTypes` 方法中删除：

```csharp
// 删除以下 4 行:
containerRegistry.RegisterForNavigation<Views.HerbManagementView>();
containerRegistry.RegisterForNavigation<Views.FormulaManagementView>();
containerRegistry.RegisterForNavigation<Views.PatientManagementView>();
containerRegistry.RegisterForNavigation<Views.MedicalCaseManagementView>();
```

修改后的 `RegisterTypes` 方法：

```csharp
public void RegisterTypes(IContainerRegistry containerRegistry)
{
    // 注册视图模型
    containerRegistry.Register<ViewModels.AdminHomeViewModel>();
    containerRegistry.Register<ViewModels.SystemSettingsViewModel>();

    // 注册视图用于导航
    containerRegistry.RegisterForNavigation<Views.AdminHomeView>();
    containerRegistry.RegisterForNavigation<Views.SystemSettingsView>();
    // View在角色台，Control在业务模块
    containerRegistry.RegisterForNavigation<Views.UserManagementView>();
}
```

- [ ] **Step 2: 编译验证**

Run: `dotnet build LYBTZYZS.sln`
Expected: 编译通过

- [ ] **Step 3: Commit**

```bash
git add src/Client/Desktop/Roles/LYBT.Desktop.Admin/AdminModule.cs
git commit -m "refactor(navigation): remove duplicate RegisterForNavigation calls from AdminModule"
```

---

### Task 4: NavigationCoordinator 前进/后退委托给 Prism Journal

**Covers:** [S4]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Navigation/NavigationCoordinator.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Navigation/INavigationHistoryService.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Navigation/NavigationHistoryService.cs`

**Interfaces:**
- Consumes: Prism `IRegionNavigationJournal`（通过 `IRegionManager` 获取）
- Produces: 简化后的 `INavigationHistoryService`（仅面包屑 + 历史记录）

- [ ] **Step 1: 修改 INavigationHistoryService，删除前进栈相关方法**

从 `src/Client/Desktop/Core/LYBT.Desktop.Navigation/INavigationHistoryService.cs` 删除：

```csharp
// 删除以下 3 个成员:
bool CanNavigateForward { get; }
string? PopForwardStack();
void PushForwardStack(string viewName);
```

修改后的接口：

```csharp
using LYBT.Desktop.Contracts.UI;

namespace LYBT.Desktop.Navigation;

/// <summary>
/// 导航历史服务接口 — 管理导航历史和面包屑
/// </summary>
public interface INavigationHistoryService
{
    /// <summary>导航历史记录</summary>
    IReadOnlyList<string> NavigationHistory { get; }

    /// <summary>当前面包屑列表</summary>
    IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; }

    /// <summary>记录一次导航</summary>
    void RecordNavigation(string? fromView, string toView);

    /// <summary>清除历史</summary>
    void ClearHistory();
}
```

- [ ] **Step 2: 修改 NavigationHistoryService，删除前进栈实现**

从 `src/Client/Desktop/Core/LYBT.Desktop.Navigation/NavigationHistoryService.cs` 删除：

1. `_forwardStack` 字段
2. `CanNavigateForward` 属性
3. `PopForwardStack()` 方法
4. `PushForwardStack()` 方法
5. `RecordNavigation()` 中的 `_forwardStack.Clear()` 调用
6. `ClearHistory()` 中的 `_forwardStack.Clear()` 调用

修改后的实现：

```csharp
using LYBT.Desktop.Contracts.UI;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Navigation;

/// <summary>
/// 导航历史服务实现 — 仅管理面包屑和历史记录列表
/// 前进/后退由 Prism IRegionNavigationJournal 管理
/// </summary>
public class NavigationHistoryService : INavigationHistoryService
{
    private const int MaxHistorySize = 20;
    private readonly ILogger<NavigationHistoryService> _logger;
    private readonly List<string> _navigationHistory = new();
    private readonly List<BreadcrumbItem> _breadcrumbs = new();

    public IReadOnlyList<string> NavigationHistory => _navigationHistory.AsReadOnly();
    public IReadOnlyList<BreadcrumbItem> Breadcrumbs => _breadcrumbs.AsReadOnly();

    public NavigationHistoryService(ILogger<NavigationHistoryService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void RecordNavigation(string? fromView, string toView)
    {
        if (_navigationHistory.Count >= MaxHistorySize)
            _navigationHistory.RemoveAt(0);
        _navigationHistory.Add(toView);
        UpdateBreadcrumbs(fromView, toView);
        _logger.LogDebug("导航记录: {From} -> {To}", fromView, toView);
    }

    public void ClearHistory()
    {
        _navigationHistory.Clear();
        _breadcrumbs.Clear();
        _logger.LogDebug("导航历史已清除");
    }

    private void UpdateBreadcrumbs(string? fromView, string toView)
    {
        var toTitle = toView?.Replace("View", "") ?? toView;

        if (fromView == null)
            _breadcrumbs.Clear();

        for (var i = 0; i < _breadcrumbs.Count; i++)
        {
            if (_breadcrumbs[i].IsCurrent)
            {
                _breadcrumbs[i] = new BreadcrumbItem(
                    _breadcrumbs[i].Title, _breadcrumbs[i].ViewName, false);
                break;
            }
        }

        _breadcrumbs.Add(new BreadcrumbItem(
            toTitle ?? toView ?? "Unknown", toView ?? "Unknown", true));
    }
}
```

- [ ] **Step 3: 修改 NavigationCoordinator 的 NavigateBack 和 NavigateForward**

修改 `src/Client/Desktop/Core/LYBT.Desktop.Navigation/NavigationCoordinator.cs`：

**修改 `CanNavigateForward` 属性**（约第 85 行）:

```csharp
// 修改前:
public bool CanNavigateForward => _historyService.CanNavigateForward;

// 修改后:
public bool CanNavigateForward
{
    get
    {
        try
        {
            var region = _regionManager.Regions[RegionNames.ContentRegion];
            return region?.NavigationService?.Journal?.CanGoForward ?? false;
        }
        catch { return false; }
    }
}
```

**修改 `NavigateBack()` 方法**（约第 191-215 行）:

```csharp
/// <summary>导航后退</summary>
public void NavigateBack()
{
    try
    {
        var region = _regionManager.Regions[RegionNames.ContentRegion];
        if (region?.NavigationService?.Journal?.CanGoBack == true)
        {
            region.NavigationService.Journal.GoBack();
            _logger.LogDebug("导航回退成功");
        }
        else
        {
            _logger.LogWarning("无法回退，导航历史为空");
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "导航回退失败");
        _userNotificationService?.ShowErrorAsync($"导航回退失败：{ex.Message}");
    }
}
```

**修改 `NavigateForward()` 方法**（约第 218-226 行）:

```csharp
/// <summary>导航前进</summary>
public void NavigateForward()
{
    try
    {
        var region = _regionManager.Regions[RegionNames.ContentRegion];
        if (region?.NavigationService?.Journal?.CanGoForward == true)
        {
            region.NavigationService.Journal.GoForward();
            _logger.LogDebug("导航前进成功");
        }
        else
        {
            _logger.LogWarning("无法前进，无前进历史");
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "导航前进失败");
        _userNotificationService?.ShowErrorAsync($"导航前进失败：{ex.Message}");
    }
}
```

- [ ] **Step 4: 编译验证**

Run: `dotnet build LYBTZYZS.sln`
Expected: 编译通过

- [ ] **Step 5: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Navigation/NavigationCoordinator.cs src/Client/Desktop/Core/LYBT.Desktop.Navigation/INavigationHistoryService.cs src/Client/Desktop/Core/LYBT.Desktop.Navigation/NavigationHistoryService.cs
git commit -m "refactor(navigation): delegate forward/back navigation to Prism IRegionNavigationJournal"
```

---

### Task 5: 最终验证

**Covers:** [S4 验证标准]

**Files:**
- 无修改

**Interfaces:**
- Consumes: 所有前序 Task 的产出
- Produces: 编译通过确认

- [ ] **Step 1: 完整编译验证**

Run: `dotnet build LYBTZYZS.sln`
Expected: 编译通过，无错误

- [ ] **Step 2: 检查无残留引用**

确认 `RegionNames` 中已删除的常量在代码中无引用（编译器已保证，但额外确认）。

- [ ] **Step 3: 最终 Commit（如有遗漏文件）**

```bash
git status
# 检查是否有未提交的修改
```
