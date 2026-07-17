# 导航架构 Prism 规范化重构设计

## [S1] 问题

当前导航架构基本遵循 Prism 推荐模式，但存在 4 个偏差：

1. `RegionNames` 定义了 15+ 个 Region 常量，XAML 中只使用了 `LoginRegion` 和 `ContentRegion`，其余为死代码
2. `NavigableViewModelBase.KeepAlive` 默认 `true`，导致所有视图常驻内存，Prism 推荐默认 `false`
3. `AdminModule` 和 `ClinicalModule` 注册了同名视图（`HerbManagementView`、`FormulaManagementView`、`PatientManagementView`、`MedicalCaseManagementView`），Prism 按最后注册者胜出，行为不确定
4. `NavigationCoordinator` 维护了自定义前进栈（`PushForwardStack`/`PopForwardStack`），Prism 的 `IRegionNavigationJournal` 已提供 `GoForward()` 能力

## [S2] 改造方案

### S2.1 清理 RegionNames 死代码

**文件**: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Constants/RegionNames.cs`

**删除以下未使用的常量**:
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

**保留**:
- `ContentRegion` — 主内容区域（XAML + NavigationCoordinator 使用）
- `LoginRegion` — 登录区域（XAML + NavigationCoordinator 使用）

**影响范围**: 仅 `RegionNames.cs` 文件本身，无外部引用。

### S2.2 KeepAlive 默认值改为 false

**文件**: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/Base/NavigableViewModelBase.cs`

**改动**: 第 115 行 `KeepAlive` 从 `true` 改为 `false`

**需要保持 KeepAlive = true 的视图**（重写属性）:
- `ClinicalWorkspaceViewModel` — 医生工作台，包含患者选择状态和看诊上下文
- `MedicalCaseWorkspaceViewModel` — 医案工作区，包含编辑中的医案数据

**文件**: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/MasterDetailViewModelBase.cs`
- 已有 `KeepAlive => false`，无需改动

### S2.3 消除同名视图注册冲突

**文件**: `src/Client/Desktop/Roles/LYBT.Desktop.Admin/AdminModule.cs`

**删除以下重复注册**（这些视图已由 ClinicalModule 和业务模块注册）:
```csharp
// 删除:
containerRegistry.RegisterForNavigation<Views.HerbManagementView>();
containerRegistry.RegisterForNavigation<Views.FormulaManagementView>();
containerRegistry.RegisterForNavigation<Views.PatientManagementView>();
containerRegistry.RegisterForNavigation<Views.MedicalCaseManagementView>();
```

**保留**:
- `AdminHomeView` — Admin 专属主页
- `SystemSettingsView` — Admin 专属系统设置
- `UserManagementView` — 仅 Admin/SuperAdmin 注册（ClinicalModule 未注册此视图）

**说明**: Admin 角色通过侧边栏导航到这些视图，导航目标是视图类型名。由于这些视图已在 ClinicalModule 中注册，Admin 模块无需重复注册。Prism 的 `RequestNavigate` 按类型名解析，ClinicalModule 的注册对所有角色生效。

### S2.4 NavigationCoordinator 前进逻辑委托给 Prism Journal

**文件**: `src/Client/Desktop/Core/LYBT.Desktop.Navigation/NavigationCoordinator.cs`

**改动 `NavigateBack()` 方法**（第 191-215 行）:
- 移除 `_historyService.PushForwardStack(currentView)` 调用
- Prism Journal 在 `GoBack()` 时自动维护前进能力

**改动 `NavigateForward()` 方法**（第 218-226 行）:
- 改为使用 `region.NavigationService.Journal.GoForward()` 而非 `_historyService.PopForwardStack()`

**改动 `CanNavigateForward` 属性**（第 85 行）:
- 改为使用 `region.NavigationService.Journal.CanGoForward` 而非 `_historyService.CanNavigateForward`

**文件**: `src/Client/Desktop/Core/LYBT.Desktop.Navigation/INavigationHistoryService.cs`
- 删除 `PopForwardStack()` 方法
- 删除 `PushForwardStack()` 方法
- 删除 `CanNavigateForward` 属性

**文件**: `src/Client/Desktop/Core/LYBT.Desktop.Navigation/NavigationHistoryService.cs`
- 删除 `_forwardStack` 字段
- 删除 `PopForwardStack()` 方法
- 删除 `PushForwardStack()` 方法
- `CanNavigateForward` 属性改为 throw NotSupportedException（或删除）
- `ClearHistory()` 中移除 `_forwardStack.Clear()`
- `RecordNavigation()` 中移除 `_forwardStack.Clear()`

**保留**:
- `NavigationHistory` — 历史记录列表（面包屑依赖）
- `Breadcrumbs` — 面包屑列表（UI 绑定）
- `RecordNavigation()` — 记录导航 + 更新面包屑
- `ClearHistory()` — 清除历史和面包屑

## [S3] 不改动的部分

以下 Prism 模式已正确使用，无需改动：

- `IModule` + `RegisterForNavigation<T>()` 模块注册模式
- `IRegionManager.RequestNavigate()` 导航调用
- `INavigationAware` / `IConfirmNavigationRequest` / `IRegionMemberLifetime` 生命周期
- `ViewModelLocationProvider` ViewModel 映射
- `IModuleCatalog` + `InitializationMode.OnDemand` 按需加载
- `ModuleLazyLoader` 懒加载机制
- `NavigationManager` 侧边栏构建逻辑
- `ShellEventCoordinator` 登录/登出事件处理

## [S4] 验证标准

1. `dotnet build LYBTZYZS.sln` 编译通过
2. 无编译警告（RegionNames 引用清理后不应有 CS0162 等警告）
3. 导航前进/后退功能正常（Prism Journal 替代自定义栈）
4. 侧边栏导航到各视图正常
5. 视图离开后不常驻内存（KeepAlive=false 生效）
6. 面包屑显示正常

## [S5] 风险评估

| 风险 | 影响 | 缓解 |
|------|------|------|
| 删除 AdminModule 重复注册后 Admin 角色无法导航到这些视图 | 高 | 验证 ClinicalModule 注册对所有角色生效 |
| Prism Journal 前进行为与自定义栈不一致 | 中 | 手动测试前进/后退场景 |
| KeepAlive=false 导致需要保持状态的视图丢失上下文 | 中 | 已确认 ClinicalWorkspaceVM 和 MedicalCaseWorkspaceVM 需要重写 |
