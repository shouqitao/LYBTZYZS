# LYBT.Desktop.Admin

管理员角色模块 -- 提供系统管理功能的统一入口，包含 6 个功能卡片导航和系统设置。

## 项目定位

管理员工作台是 Admin/SuperAdmin 角色的默认主页，通过卡片式导航提供入口。**注意**：管理视图（患者/药材/验方/医案/用户管理）绝大多数位于 `Roles/LYBT.Desktop.Clinical` 的 `Views/`（薄包装 View）与各业务模块的 `Controls/`；Admin 模块自身只有 3 个导航视图（`AdminHomeView` / `SystemSettingsView` / `UserManagementView`），`AdminHomeViewModel` 的卡片按视图名导航到 Clinical 注册的视图。运维配置（`Sysadmin/`）为独立 Prism 子模块。系统设置支持 `clinic-settings.json` 热更新。

## 目录结构

```
LYBT.Desktop.Admin/
├── AdminModule.cs                        # Prism IModule 入口（无 ModuleDependency；WhenAvailable）
├── ViewModels/
│   ├── AdminHomeViewModel.cs             # 管理员主页（卡片导航 + 当前用户信息）
│   └── SystemSettingsViewModel.cs        # 系统设置（诊所配置 + 系统设置 + 服务端配置）
├── Views/
│   ├── AdminHomeView.xaml(.cs)
│   ├── SystemSettingsView.xaml(.cs)
│   └── UserManagementView.xaml(.cs)      # 薄包装，复用 Users 模块 UserMasterDetailControl
├── Services/
│   ├── ISystemSettingsService.cs
│   ├── SystemSettingsService.cs          # 本地 JSON 持久化（system-settings.json）
│   ├── ServerConfigurationService.cs     # 服务端配置门面（经 IApiClient.Configuration）
│   ├── IServerConfigurationService.cs
│   ├── DeploymentService.cs / IDeploymentService.cs
│   └── DiagnosticsService.cs / IDiagnosticsService.cs
└── Sysadmin/                             # 运维设置子模块（独立 [Module] SysadminModule）
    ├── SysadminModule.cs
    ├── Views/                            # SysadminHomeView / LogLevelControlView / DeploymentView /
    │                                     #   BackupManagementView / SecurityAuditLogView
    ├── ViewModels/                       # SysadminHomeViewModel + 7 个面板/页面 VM
    ├── Services/                         # SecurityAuditQueryService / AuthHealthService
    └── Models/                           # DashboardStatus 等
```

> 药材/验方/患者/医案管理视图**不在本模块**：它们由 `Roles/LYBT.Desktop.Clinical/Views/` 提供（薄包装 View），Admin 台通过 `ViewNames` 导航过去。

## 视图 / ViewModel 清单

**计数口径**：View = 页面/导航级 XAML（`*/Views/*.xaml`，含 `Sysadmin/Views/`）；Control = 内嵌组件（本模块无 `Controls/`）；Dialog = `*/Dialogs/**/*.xaml`（本模块无）；ViewModel 按「每文件 1 个 VM 类型」计。

| 类别 | 数量 | 明细 |
|------|------|------|
| View | 8 | 顶层 3：`AdminHomeView`、`SystemSettingsView`、`UserManagementView`；`Sysadmin/Views/` 5：`SysadminHomeView`、`LogLevelControlView`、`DeploymentView`、`BackupManagementView`、`SecurityAuditLogView` |
| Control | 0 | — |
| Dialog | 0 | — |
| ViewModel | 10 | 顶层 2：`AdminHomeViewModel`、`SystemSettingsViewModel`；`Sysadmin/ViewModels/` 8：`SysadminHomeViewModel`、`ConfigurationCenterViewModel`、`ServerConfigSectionViewModel`、`CardReaderDiagnosticsViewModel`、`LogLevelControlViewModel`、`DeploymentViewModel`、`BackupManagementViewModel`、`SecurityAuditLogViewModel` |

> `ConfigurationCenterViewModel` / `ServerConfigSectionViewModel` / `CardReaderDiagnosticsViewModel` 是 `SysadminHomeView` 的**内嵌面板子 VM**（`ConfigCenter` / `ServerConfig` / `CardReaderDiagnostics` 属性），**没有独立视图**（对应历史清单里的 SY-05/SY-07/SY-08 幽灵视图）。
>
> 全桌面口径：View 30 / Control 33 / Dialog 7 / ViewModel 55（代码实际：`src/Client/Desktop`）。

## 核心组件

### AdminModule

**设计依据**: Prism IModule 标准入口，注册 2 个 ViewModel + 1 个服务门面 + 3 个导航视图（其余卡片指向 Clinical 注册的视图）。

| 注册项 | 类型 | 说明 |
|--------|------|------|
| `AdminHomeViewModel` | ViewModel | 管理员主页 VM |
| `SystemSettingsViewModel` | ViewModel | 系统设置 VM |
| `IServerConfigurationService` → `ServerConfigurationService` | Service | 服务端配置门面（D6/DP10 收口，经 `IApiClient.Configuration`） |
| `AdminHomeView` | Navigation | 管理员主页 |
| `SystemSettingsView` | Navigation | 系统设置页 |
| `UserManagementView` | Navigation | 用户管理（薄包装，复用 `LYBT.Desktop.Users` 的 `UserMasterDetailControl`） |

**模块依赖**: 无 `[ModuleDependency]`；Shell 以 `WhenAvailable` 立即加载。`Sysadmin/` 子模块 `SysadminModule` 同样 `WhenAvailable` 加载。

### AdminHomeViewModel

**设计依据**: NavigableViewModelBase 子类，卡片导航 + 当前用户信息展示。

| 属性 | 说明 |
|------|------|
| `CurrentUserName` | 当前用户名 |
| `IsSysAdmin` | 是否为系统管理员 |

| 命令 | 导航目标（`ViewNames`） |
|------|----------|
| `NavigateToUserManagement` | `UserManagement`（本模块视图） |
| `NavigateToSystemSettings` | `SystemSettings`（本模块视图） |
| `NavigateToHerbManagement` | `HerbManagement`（Clinical 注册的薄包装视图） |
| `NavigateToPatientManagement` | `PatientManagement`（同上） |
| `NavigateToFormulaManagement` | `FormulaManagement`（同上） |
| `NavigateToMedicalCaseManagement` | `MedicalCaseManagement`（同上） |
| `NavigateToReports` | `ReportsHome`（`ReportsModule`，OnDemand 懒加载） |
| `NavigateToAuditLog` | `AuditLog`（`MedicalCaseModule`，OnDemand 懒加载） |

### SystemSettingsViewModel

**设计依据**: NavigableViewModelBase 子类，三服务协调（本地系统设置 + 诊所配置 + 服务端配置）。

| 依赖服务 | 用途 |
|----------|------|
| `ISystemSettingsService` | 系统级设置（系统名称、医院名称、联系电话、备份路径等） |
| `IClinicSettingsService` | 诊所配置（`clinic-settings.json` 热更新） |
| `IServerConfigurationService` | 服务端配置段（`IApiClient.Configuration` 门面，D6 收口） |

| 命令 | 说明 |
|------|------|
| `SaveAsync` | 保存设置 |
| `ResetAsync` | 重置为默认值 |
| `BrowseBackupPathAsync` | 浏览备份路径 |
| `LoadServerConfigAsync` | 加载服务端配置段 |
| `SaveServerConfigAsync` | 保存服务端配置段 |
| `ValidateConfigAsync` | 校验配置 |

### SystemSettingsService

**设计依据**: ISystemSettingsService 实现，JSON 文件持久化到 `%LOCALAPPDATA%\LYBT\Desktop\system-settings.json`（属性 setter 即时落盘）。

| 属性 | 默认值 | 说明 |
|------|--------|------|
| `SystemName` | `"中医诊疗系统"` | 系统名称 |
| `HospitalName` | `""` | 医院名称 |
| `ContactPhone` | `""` | 联系电话 |
| `AutoBackupEnabled` | `false` | 自动备份开关 |
| `BackupPath` | `""` | 备份路径 |

### SysadminModule（运维设置子模块）

**设计依据**: 独立 `[Module]`，为 sysadmin 用户提供配置运维体验；`[ModuleDependency]` 无。

| 注册项 | 类型 | 说明 |
|--------|------|------|
| 8 个 VM | ViewModel | `SysadminHomeViewModel`、`ConfigurationCenterViewModel`、`ServerConfigSectionViewModel`、`CardReaderDiagnosticsViewModel`、`LogLevelControlViewModel`、`DeploymentViewModel`、`BackupManagementViewModel`、`SecurityAuditLogViewModel` |
| `IAuthHealthService` → `AuthHealthService` / `IDeploymentService` / `IDiagnosticsService` / `IServerConfigurationService` / `ISecurityAuditQueryService` | Service | 运维服务（均经 `IApiClient` 子域门面，D6/DP10 收口） |
| `SysadminHomeView` / `LogLevelControlView` / `DeploymentView` / `BackupManagementView` / `SecurityAuditLogView` | Navigation | 5 个导航视图 |

**SysadminHomeViewModel**: `Dashboard`（30 秒轮询 `IAuthHealthService.HealthCheckAsync`）+ 三个内嵌面板 `ConfigCenter` / `ServerConfig` / `CardReaderDiagnostics`；命令 `NavigateToUserManagement` / `NavigateToLogLevelControl` / `NavigateToDeployment` / `NavigateToSecurityAuditLog`。

## 依赖关系

### 依赖（编译时 ProjectReference）

| 项目 | 用途 |
|------|------|
| LYBT.Desktop.Foundation | 基础设施（`Application`、`ExceptionHandling` 等） |
| LYBT.Desktop.Infrastructure | `NavigableViewModelBase`、`Constants.ViewNames`、`Interfaces.IClinicSettingsService`、`Extensions`、`Services` |
| LYBT.Desktop.Contracts | `Services`（`INavigationCoordinator`、`IAuthHealthService`、`ISecurityAuditQueryService` 等）、`ApiClient` 门面 |
| LYBT.Desktop.Catalog / Patients / MedicalCase / Users | 编译期引用（供 `UserManagementView` 等嵌入 Control；Catalog/Patients/MedicalCase 引用为跨台共用预留） |
| LYBT.Shared.Models | DTOs、`Enums` |

NuGet：`Prism.Core` / `Prism.DryIoc` / `Prism.Wpf`、`CommunityToolkit.Mvvm`、`Microsoft.Extensions.Logging.Abstractions`。

### 被依赖

| 消费方 | 说明 |
|--------|------|
| Shell `App.ConfigureModuleCatalog` | `AdminModule` 与 `SysadminModule` 均 `WhenAvailable` 立即加载 |
| `RoleRegistry` | `AdminRoleDefinition`（Admin，主页 `AdminHomeView`）与 `SuperAdminRoleDefinition`（SuperAdmin，主页 `SysadminHomeView`）分别注册；未注册角色 fallback 到 `ClinicalHome` |
| `UsersModule` | 无反向依赖：`UserManagementView` 由 Admin 单向编译期引用 `LYBT.Desktop.Users` |

## 设计决策

1. **薄包装 View 模式**: `UserManagementView` 仅为 `UserControl` 包装，内部嵌入 `LYBT.Desktop.Users` 的 `UserMasterDetailControl`，实现 View 在角色台、Control 在业务模块的分离；药材/验方/患者/医案管理视图同理，但托管在 `ClinicalModule`
2. **卡片按视图名导航**: `AdminHomeViewModel` 不持有被导航视图的类型，只传 `ViewNames` 字符串，跨角色台复用同一批薄包装视图
3. **三设置服务**: `ISystemSettingsService`（本地 JSON）、`IClinicSettingsService`（`clinic-settings.json` 热更新）、`IServerConfigurationService`（服务端配置段）分工明确
4. **运维子模块独立**: `Sysadmin/` 作为独立 `[Module]` 加载，与管理员主页解耦；配置中心/服务端配置/读卡器诊断以内嵌面板子 VM 形式挂在 `SysadminHomeView`，不单独注册视图
5. **轮询式仪表盘**: `SysadminHomeViewModel` 在 `OnNavigatedTo` 启动 30 秒轮询、`OnNavigatedFrom` 停止，`OnDisposing` 退订 `ModeChanged` 并释放 CTS（P1-12 事件泄漏修复）

## 已知陷阱

- **薄包装 View 无自有 ViewModel**: `UserManagementView` 直接在 XAML 中嵌入业务模块的 Control，Prism ViewModelLocator 不生成对应 VM
- **卡片导航目标由 Clinical 注册**: `HerbManagement`/`FormulaManagement`/`PatientManagement`/`MedicalCaseManagement` 不在 Admin 模块注册；若 `ClinicalModule` 未加载/视图未注册，这些卡片会导航失败
- **UserManagementView 导航参数**: 必须在 `OnNavigatedTo` 中手动读取 `DefaultRoleFilter` 参数并调用 `SetDefaultRoleFilter()`
- **SystemSettingsService 文件路径**: 使用 `Environment.SpecialFolder.LocalApplicationData`，不同用户有不同配置文件
- **Sysadmin 面板 VM 无独立视图**: 需要单独打开某个面板时不要新建 `CardReaderDiagnosticsView`/`ConfigurationCenterView`，应先改 `SysadminHomeView` 的布局
- **IsSysAdmin 属性**: 构造时默认 `true`（避免按钮闪现），异步加载后根据实际用户信息更新

---

2026-09-13 docs 复盘：与代码对齐（View/VM 清单、目录树、依赖）
