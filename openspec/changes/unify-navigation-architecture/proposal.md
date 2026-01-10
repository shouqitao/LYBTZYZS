# unify-navigation-architecture

## Why

### 发现的问题

在完成 `fix-button-navigation-system` 提案时，发现Desktop层导航架构存在严重的代码重复和设计不一致问题：

| 问题类型 | 位置 | 当前状态 | 期望状态 |
|----------|------|----------|----------|
| GetHomeViewName重复 | MenuManager, NavigableViewModelBase, UnifiedViewModelBase, RoleNavigationService, RoleRegistry | 5处几乎相同的实现 | 单一权威实现 |
| 视图名称硬编码 | 全项目XAML和ViewModel | 字符串分散在各处 | 类型安全的常量类 |
| IConfirmNavigationRequest不一致 | 仅NavigableViewModelBase实现 | 部分ViewModel支持导航确认 | 统一的导航确认机制 |
| NavigateToHome多点定义 | MenuManager, ViewModelBase等 | 导航逻辑分散 | 单一协调器入口 |
| ViewNavigationService未使用 | Shell/Services | 服务已创建但未集成 | 正式集成或移除 |
| 角色-视图映射不完整 | RoleRegistry | 缺少部分角色配置 | 完整的角色映射表 |

### 影响分析

**受影响模块**:
- Shell层: NavigationManager, MenuManager, MainWindowViewModel
- Infrastructure层: ViewModelBase, NavigableViewModelBase, UnifiedViewModelBase
- Roles层: Admin, Clinical所有ViewModel
- Modules层: 所有业务模块的ViewModel

**架构债务评估**:
- 代码重复率: ~40行重复代码 x 5处 = 200行冗余
- 维护风险: 修改角色-视图映射需同步5处
- 类型安全: 无编译时视图名称检查

## What Changes

### Phase 1: 消除GetHomeViewName重复

统一调用 `RoleRegistry.GetHomeViewName()`，删除其他4处重复实现：

- 修改 `MenuManager.GetHomeViewName()` → 调用 RoleRegistry
- 修改 `NavigableViewModelBase.GetHomeViewName()` → 调用 RoleRegistry
- 修改 `UnifiedViewModelBase.GetHomeViewName()` → 调用 RoleRegistry
- 修改 `RoleNavigationService.GetHomeViewName()` → 调用 RoleRegistry

**注意**: RoleRegistry作为唯一权威源

### Phase 2: 引入ViewNames常量类

创建类型安全的视图名称常量：

```csharp
// Shell/Constants/ViewNames.cs
public static class ViewNames
{
    // Admin角色视图
    public const string AdminHome = "AdminHomeView";
    public const string SystemSettings = "SystemSettingsView";
    public const string UserManagement = "UserManagementView";
    
    // Clinical角色视图
    public const string ClinicalHome = "ClinicalHomeView";
    public const string PatientSelection = "PatientSelectionView";
    public const string MedicalCaseWorkspace = "MedicalCaseWorkspaceView";
    
    // 共享视图
    public const string Login = "LoginView";
    public const string AccountSettings = "AccountSettingsView";
    
    // ... 其他视图
}
```

全项目替换硬编码字符串为常量引用。

### Phase 3: 统一INavigationAware实现

创建统一的导航感知基类：

- 定义 `NavigationAwareViewModelBase` 实现 `INavigationAware` + `IConfirmNavigationRequest`
- 提供可覆盖的生命周期钩子:
  - `OnNavigatingTo(NavigationContext)` - 导航进入前
  - `OnNavigatedTo(NavigationContext)` - 导航进入后
  - `OnNavigatingFrom(NavigationContext)` - 导航离开前确认
  - `OnNavigatedFrom(NavigationContext)` - 导航离开后
- 所有需要导航感知的ViewModel继承此基类

### Phase 4: 创建INavigationCoordinator统一入口

引入导航协调器作为单一导航入口：

```csharp
public interface INavigationCoordinator
{
    // 基础导航
    void NavigateTo(string viewName, NavigationParameters parameters = null);
    void NavigateToHome();
    void NavigateBack();
    
    // 角色导航
    void NavigateToRoleHome(UserRole role);
    
    // 导航状态
    bool CanNavigateBack { get; }
    string CurrentView { get; }
    
    // 导航历史
    IReadOnlyList<string> NavigationHistory { get; }
}
```

整合现有 NavigationManager 和 ViewNavigationService 功能。

### Phase 5: 清理和文档

- 移除未使用的 ViewNavigationService（如果功能已整合）
- 更新 RoleRegistry 完善角色-视图映射
- 更新架构文档说明新导航模式
- 添加导航使用示例

## Architecture

### 变更影响范围

```
src/Client/Desktop/
├── Shell/
│   ├── Constants/
│   │   └── ViewNames.cs                    [NEW]
│   ├── Services/
│   │   ├── NavigationManager.cs            [MODIFY - 使用ViewNames]
│   │   ├── MenuManager.cs                  [MODIFY - 调用RoleRegistry]
│   │   ├── NavigationCoordinator.cs        [NEW]
│   │   ├── RoleNavigationService.cs        [MODIFY - 调用RoleRegistry]
│   │   └── ViewNavigationService.cs        [DELETE or INTEGRATE]
│   ├── ViewModels/
│   │   └── MainWindowViewModel.cs          [MODIFY]
│   └── Config/
│       └── RoleRegistry.cs                 [MODIFY - 完善映射]
├── Infrastructure/
│   └── ViewModels/
│       ├── ViewModelBase.cs                [MODIFY]
│       ├── NavigableViewModelBase.cs       [MODIFY - 调用RoleRegistry]
│       ├── UnifiedViewModelBase.cs         [MODIFY - 调用RoleRegistry]
│       └── NavigationAwareViewModelBase.cs [NEW]
└── Roles/
    ├── LYBT.Desktop.Admin/ViewModels/      [MODIFY - 继承新基类]
    └── LYBT.Desktop.Clinical/ViewModels/   [MODIFY - 继承新基类]
```

### 导航架构图

```
┌─────────────────────────────────────────────────────────────────┐
│                      UI Layer (Views/XAML)                      │
│  Command Binding → NavigateToHomeCommand, NavigateTo{View}Command
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                   INavigationCoordinator                         │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │ NavigateTo(ViewNames.PatientSelection, params)              │ │
│  │ NavigateToHome() → RoleRegistry.GetHomeViewName(role)       │ │
│  │ NavigateBack()                                              │ │
│  └─────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│              IRegionManager.RequestNavigate()                    │
│                    (Prism Navigation)                            │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                 INavigationAware ViewModels                      │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │ OnNavigatingTo → OnNavigatedTo → OnNavigatingFrom         │  │
│  │                  (Lifecycle Hooks)                        │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

## Impact

- **新增文件**: 3个 (ViewNames.cs, NavigationCoordinator.cs, NavigationAwareViewModelBase.cs)
- **修改文件**: ~15-20个 (各ViewModel基类 + NavigationManager + MenuManager + 部分业务ViewModel)
- **删除文件**: 0-1个 (ViewNavigationService，如功能已整合)
- **风险等级**: Medium - 影响范围大但逻辑清晰，每个Phase独立验证

## Risks

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|----------|
| 遗漏视图名称替换 | 中 | 低 | Grep全量搜索字符串视图名称 |
| 导航行为变化 | 低 | 高 | 每个Phase编译验证+功能测试 |
| 循环依赖 | 低 | 中 | INavigationCoordinator通过DI注入 |
| ViewModel继承链变化 | 中 | 中 | 渐进式迁移，保持向后兼容 |

## Testing Strategy

### 每个Phase验证
- 编译验证: `dotnet build LYBT.All.sln -c Release --no-restore`
- 运行验证: 手动测试主要导航路径

### 最终验收
- [ ] Admin主页 ↔ 各管理视图导航正常
- [ ] Clinical主页 ↔ 诊疗流程导航正常
- [ ] 角色切换后正确导航到对应主页
- [ ] 返回主页功能在所有视图可用
- [ ] 导航历史记录正确

## References

- 前序提案: `fix-button-navigation-system` - 修复导航按钮绑定问题
- Prism文档: Navigation - INavigationAware, IConfirmNavigationRequest
- 项目架构: openspec/project.md

---

**创建时间**: 2026-01-10 10:03
**状态**: 待确认
