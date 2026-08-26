# Shell 公共组件 + VM 层整体设计 调研与规划

> 日期: 2026-08-24 | 类型: 调研+规划（只产出文档，不改代码） | 范围: `src/Client/Desktop/Shell` + `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels` + 各模块 `ViewModels/`
> 依据: `docs/03-architecture/16-desktop-architecture-spec.md`、`docs/03-architecture/02-desktop.md`、`src/Client/Desktop/Shell/` 及 `src/Client/Desktop/Core/` 代码审计

---

## 1. 背景与目标

- **补充要求**: 在调研 Shell 公共组件的同时，加入 VM 层整体设计的调研与规划。
- **目标**: 沉淀 Shell 作为“公共底座”的职责边界与可复用组件清单，并对 VM 层（ViewModel 体系）做全景盘点，给出分层、职责、复用与演进规划，为后续 `A-31 C-*` / `A-26` 及启动链路治理提供输入。

---

## 2. 调研方法

- 代码走查: `Shell/{Services,ViewModels,Views,Controls,Dialogs,Extensions,Assets}` 全量目录；`Core/LYBT.Desktop.Infrastructure/ViewModels/{Base,Composition,Handlers}` 及 7 个业务模块 `ViewModels/` 采样
- 文档对标: 对照 `16-desktop-architecture-spec.md` 的“标准目录 / 标准数据流 / 命名规范 / 架构约束 DP-M1..M3”
- 约束对齐: Prism MVVM（1 View=1 VM, AutoWire, Region 导航）、DTO 不暴露到 UI、EditContext 可取消

---

## 3. Shell 公共组件盘点（现状）

### 3.1 目录全景

```
Shell/
├── App.xaml / App.xaml.cs          # PrismApplication, 资源合并（MaterialDesign, Surfaces, TcmBrands, Icons）
├── Controls/ AccountSettingsControl # 个人设置左右分栏（260 + *）
├── Dialogs/ {Confirmation,Input,Message} x 2（View+ViewModel）
├── Extensions/ {UnifiedApiClient, PrismConfiguration, ServiceCollection, DataSourceRegistration}
├── Services/
│   ├── Bootstrap/ApplicationBootstrapper
│   ├── HealthCheck/ApiHealthMonitor
│   ├── Login/{LoginCoordinator, LoginStateManager}
│   ├── Session/{SessionLifecycleManager, SessionBasedCurrentUserProvider}
│   ├── Startup/{StartupPipeline + Steps/*}  # ApiHealthCheck, LocalWebApi, ModuleCoordinator, ErrorHandling, DesktopUpdate
│   ├── {MenuManager, NavigationManager, StatusBarManager, ThemeService, ShellServices, ShellEventCoordinator}
│   └── NativeMethods
├── ViewModels/{MainWindowViewModel, AccountSettingsViewModel}
├── Views/{MainWindow, AccountSettingsView}
└── Assets/{Fonts, Icons/App, Images/Backgrounds}
```

### 3.2 核心公共服务

| 组件 | 职责 | 依赖 / 备注 |
|------|------|-------------|
| `MainWindowViewModel` | 侧边栏展开/收拢、导航、登出、健康检查、主题 | 依赖 `INavigationManager/IMenuManager/IStatusBarManager/IThemeService` |
| `StatusBarManager` / `NavigationManager` / `MenuManager` | 底部状态、页面导航、菜单构建 | Shell 单例，跨模块共享 |
| `ThemeService` | 明/暗主题切换，同步 `Surfaces/TcmBrands` | `ApplyCustomPalette` |
| `LoginCoordinator` + `LoginStateManager` | 登录流程编排、状态持久化 | 被 `LoginViewModel` 委托 |
| `SessionLifecycleManager` | 登录态生命周期、过期/切换 | 结合 `TokenRefreshHandler` |
| `ApplicationBootstrapper` + `StartupPipeline` | 启动管线 5 Steps 串行编排 | 可观测、可重试 |
| `UnifiedApiClientExtensions` | `SwitchingApiClient`（Remote/Local 双模切换）、`IHttpClientFactory`、`TokenRefreshHandler` 链 | Remote: Logging→Auth→Refresh→HttpHandler |
| `ShellEventCoordinator` / `ShellDialogHelper` | Prism `IEventAggregator` 事件协调、通用对话框 | `Confirmation/Input/Message` |

### 3.3 公共 UI 资源

- `App.xaml` 合并 7 个 ResourceDictionary：`BundledTheme(Brown/Amber) + MaterialDesign2.Defaults + Surfaces + TcmBrands + Icons + Spacing + Functions + Converters`
- `Surfaces.xaml`: 4 级 Surface（L0 `FAF8F5` → L3 白）+ Divider + Badge + Elevation
- `TcmBrands.xaml`: `TcmGreen/Gold/Orange`（药材/警告/进行中）
- `AccountSettingsControl`: 唯一“Shell 公开的业务型 Control”，左右分栏，左 260 菜单，右表单

### 3.4 对话框体系

- 3 个通用 Dialog（Confirmation/Input/Message）+ 对应 ViewModel（`DialogViewModelBase` 派生），`ShellDialogHelper` 统一封装 `ShowConfirm/Success/Error`

### 3.5 发现的问题（Shell）

| 编号 | 现象 | 影响 | 建议级别 |
|------|------|------|----------|
| S-01 | `Shell/Services` 下 18 个服务文件与 `Core/LYBT.Desktop.Infrastructure` 部分服务职责重叠（Menu/Navigation/Theme 各有一份抽象） | 新人分不清该注 Shell 还是 Infrastructure | P1 |
| S-02 | `AccountSettingsControl` 既是 Shell 资产又被 `AccountSettingsView` 薄包装，且 `Infrastructure` 也有 `Account` 相关 VM | 层级模糊 | P2 |
| S-03 | `UnifiedApiClientExtensions` 承担过多：Refit 配置 + 切换策略 + Handler 链组装 + Local 工厂 | 变更风险集中 | P2 |
| S-04 | `Assets/Images/Backgrounds/img-login-background.jpg` 仅 Login 使用却放在 Shell | 归属不纯 | P3 |

---

## 4. VM 层整体设计盘点（现状）

### 4.1 基础设施 VM 基座（`Core/LYBT.Desktop.Infrastructure/ViewModels`）

| 基类/组件 | 职责 | 关键成员 |
|-----------|------|----------|
| `NavigableViewModelBase` | 导航生命周期（`OnNavigatedTo/From`）、Busy/Error/Status、导航 home | `IsLoading`, `ErrorMessage`, `StatusMessage`, `NavigateToHome` |
| `DialogViewModelBase` | 对话框确认/取消 | `Confirm/Cancel` |
| `ValidatableModelBase` | `INotifyDataErrorInfo` 校验基座 | `Errors`, `HasErrors` |
| `EditorViewModelBase<TModel,TInput>` | Model↔InputDto 往返、校验、脏检查 | `Model`, `IsDirty`, `Validate()` |
| `MasterDetailViewModelBase<TList,TDetail>` | 列表分页/搜索/选择/新增/编辑/删除 模板 | `Items`, `SelectedItem`, `CurrentDetail`, `IsEditMode`, 9 个 RelayCommand |
| `MasterDetailCommandGroup` | MasterDetail 命令分组（工具栏/分页/批量） | `CreateNew/Refresh/Search/BatchEnable/Disable/Restore/Delete/Save/Cancel` |
| `ChildViewModelBase` | 组合式子 VM（Credentials/ConnectionStatus 等） | `Parent` 引用, `OnPropertyChanged` 转发 |
| `BaseStatusHandler<TListDto>` | 状态切换/恢复的确认-执行-通知模板 | `ToggleStatusAsync(entity)` + `ExecuteSetStatusAsync(id, targetStatus)`（本次已迁移 SetStatus） |
| `ServiceEventBridge` / `ValidationAccessors` | Service 事件桥接、校验访问器 |  |

**特征**: 全部基于 `CommunityToolkit.Mvvm`（`[ObservableProperty]/[RelayCommand]` SourceGenerator），`NavigableViewModelBase` 为最顶层，体系已完整。

### 4.2 业务模块 VM 分布（抽样）

| 模块 | ViewModels 数量 | 典型 VM | 模式 |
|------|-----------------|---------|------|
| `Auth` | 6 | `LoginViewModel`（组合 `LoginCredentialsViewModel + ConnectionStatusViewModel`） + `ServerConfigViewModel/FirstRunSetupViewModel` | 组合式（D3） |
| `Patients` | 4 | `PatientMasterDetailViewModel`, `PatientSelectionViewModel` | MasterDetail 模板 |
| `Catalog` (Herb+Formula 合并) | 8 | `HerbMasterDetailViewModel`, `FormulaMasterDetailViewModel`, `HerbEditorViewModel` | MasterDetail + Editor + StatusHandler |
| `Users` | 4 | `UserMasterDetailViewModel` | 同上 |
| `Registrations` | 2 | `RegistrationListViewModel`, `RegistrationCreateDialogViewModel` | List + Dialog |
| `MedicalCase` | 12 | `MedicalCaseMasterDetailViewModel`, `MedicalCaseWorkspaceViewModel`, `ConsultationEditorViewModel`, `PrescriptionEditorViewModel`, `MedicalCaseCommandsViewModel` | 工作台复合（Workspace 聚合多子 VM） |
| `Shell` | 2 | `MainWindowViewModel`, `AccountSettingsViewModel` | 框架 + 设置 |

### 4.3 数据流现状 vs 规范

- **正例**: `MedicalCaseWorkspaceViewModel` 已按 `16-desktop-spec` 的“Model 为真源 + EditContext 快照”重构，不直接持有 DTO
- **待收敛**: `Registration` 仍有“VM 直接持有 DTO + 对话框直接构造 InputDto”残留（`16-spec §4.5` 已标记 🔴），与 `Patients/Users/Catalog` 的 Model 层不一致
- **跨模块**: `Registration`/`MedicalCase` 之间有 `StartVisit → 原子建医案` 链路，VM 通过 `IRegistrationService/IMedicalCaseService` 解耦，已无直接引用（A-11 已收敛）

### 4.4 命名与目录现状

- 命名空间：`LYBT.Desktop.{Module}` 已统一（`Q-03` 后），但 `LYBT.Desktop.MedicalCase` 下仍有历史 `LYBT.Desktop.Modules.*` 残留注释（非代码）
- 目录：`Controls/` vs `Views/` 并存，`Catalog/MedicalCase/Patients/Users` 用 `Controls/MasterDetailControl`，`Registrations` 用 `Views/RegistrationListView`，属 Prism 两种合法形态，已在 `16-spec §3.3` 明确“均合理”

---

## 5. VM 层整体设计规划

### 5.1 分层目标（保持不变，强化落地）

```
View (XAML, AutoWire) 
  ↓ Binding
ViewModel (编排, 持有 Model/EditContext, 暴露 Command/Property)
  ↓ Service (封装 API + 错误处理 → CommandResult<T>)
Model / EditContext (可编辑副本, 校验, 脏检查, 快照恢复)
  ↓ Mapper (DTO↔Model, InputDto)
DTO / InputDto (传输契约, 不暴露到 View)
```

**约束重申**: `DP-M1` VM 禁止直接编辑 DTO；`DP-M2` 每个 MasterDetail 必须有 DetailModel；`DP-M3` 命名空间禁止 `Modules.` 前缀。

### 5.2 VM 基座演进（3 项）

| 项 | 现状 | 目标 | 优先级 |
|----|------|------|--------|
| V-01 | `EditorViewModelBase` 已有但 `Registration` 未使用 | `Registration` 补 `RegistrationDetailModel + EditContext + EditorViewModel`，对齐其他 5 域 | P1 |
| V-02 | `MasterDetailViewModelBase` 模板覆盖 80% 场景，`MedicalCaseWorkspace` 却大量手写聚合 | 抽取 `WorkspaceViewModelBase<T>`（聚合 `ChildViewModelBase` + 导航 + Busy 统一），`MedicalCaseWorkspaceViewModel` 迁移 | P2 |
| V-03 | `BaseStatusHandler` 已迁移 SetStatus，但 `FormulaStatusHandler` 仍对 Repository Toggle（无显式状态） | Repository 层补 `SetStatusAsync(id, status)`，Handler 完全显式化 | P2 |

### 5.3 Shell 公共组件治理（3 项）

| 项 | 动作 | 说明 | 优先级 |
|----|------|------|--------|
| S-05 | **Shell 服务注册表文档化** | 新增 `docs/03-architecture/05-shell-services.md`，明确 18 个服务的“归属/生命周期/依赖/替代” | P1 |
| S-06 | **Handler 链工厂抽取** | 将 `UnifiedApiClientExtensions.remoteHttpClientFactory` 中的 4 层 Handler 组装抽为 `HttpHandlerChainFactory`（Shell/Services/Http） | P2 |
| S-07 | **AccountSettingsControl 归属澄清** | 明确为“Shell 公开 Control”，`Shell/Views/AccountSettingsView` 仅作 Region 宿主，`Infrastructure` 不再重复定义 Account VM | P2 |

### 5.4 实施路线（不改代码，仅文档先行）

| 阶段 | 产出 | 关联任务 |
|------|------|----------|
| 阶段 1（本文） | 本调研与规划定稿 | — |
| 阶段 2（文档先行） | `16-desktop-architecture-spec.md` §4.5 补充 V-01 方案；`05-shell-services.md` 新建 | A-26 文档先行 |
| 阶段 3（代码） | V-01 `Registration` Model 化（1d）；S-06 工厂抽取（0.5d）；V-02/V-03 各 0.5d | A-27 技术栈减法后 |

---

## 6. 验收标准（文档）

- [ ] Shell 公共组件清单与 VM 基座清单与代码一致（抽检 3 个服务/3 个 VM 可追溯到文件）
- [ ] 规划项 V-01/V-02/V-03、S-05/S-06/S-07 有明确优先级与依赖
- [ ] 与 `16-desktop-architecture-spec.md` 无矛盾（必要时提 ADR）

---

## 7. 下一步

- 本文档经确认后：① 归档至 `docs/compose/archive/` 或保留于 `docs/compose/reports/`；② 按需拆出 `05-shell-services.md` 与 `16-spec` 增补
- 代码实施前需另起 `docs/compose/plans/` 任务书并走“文档先行”门禁

