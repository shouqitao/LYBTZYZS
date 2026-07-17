# LYBT.Desktop.Admin

管理员角色模块 -- 提供系统管理功能的统一入口，包含 6 个功能卡片导航和系统设置。

## 项目定位

管理员工作台是 Admin/SuperAdmin 角色的默认主页，通过卡片式导航提供用户管理、药材管理、验方管理、患者管理、医案管理、系统设置等功能入口。管理视图均为薄包装 View，复用业务模块的 MasterDetail Control。系统设置支持 clinic-settings.json 热更新。

## 目录结构

```
LYBT.Desktop.Admin/
├── AdminModule.cs                # Prism IModule 入口
├── ViewModels/
│   ├── AdminHomeViewModel.cs     # 管理员主页（6 卡片导航）
│   └── SystemSettingsViewModel.cs # 系统设置
├── Views/
│   ├── AdminHomeView.xaml(.cs)
│   ├── SystemSettingsView.xaml(.cs)
│   ├── HerbManagementView.xaml(.cs)       # 薄包装，复用 Herbs 模块 Control
│   ├── FormulaManagementView.xaml(.cs)    # 薄包装，复用 Formula 模块 Control
│   ├── PatientManagementView.xaml(.cs)    # 薄包装，复用 Patients 模块 Control
│   ├── MedicalCaseManagementView.xaml(.cs)# 薄包装，复用 MedicalCase 模块 Control
│   └── UserManagementView.xaml(.cs)       # 薄包装，复用 Users 模块 Control
└── Services/
    ├── ISystemSettingsService.cs
    └── SystemSettingsService.cs  # JSON 文件持久化
```

## 核心组件

### AdminModule

**设计依据**: Prism IModule 标准入口，注册 2 个 ViewModel + 7 个导航视图。

| 注册项 | 类型 | 说明 |
|--------|------|------|
| `AdminHomeViewModel` | ViewModel | 管理员主页 VM |
| `SystemSettingsViewModel` | ViewModel | 系统设置 VM |
| `AdminHomeView` | Navigation | 管理员主页 |
| `SystemSettingsView` | Navigation | 系统设置页 |
| `HerbManagementView` | Navigation | 药材管理（薄包装） |
| `FormulaManagementView` | Navigation | 验方管理（薄包装） |
| `PatientManagementView` | Navigation | 患者管理（薄包装） |
| `MedicalCaseManagementView` | Navigation | 医案管理（薄包装） |
| `UserManagementView` | Navigation | 用户管理（薄包装） |

**模块依赖**: 无（WhenAvailable 立即加载）

### AdminHomeViewModel

**设计依据**: NavigableViewModelBase 子类，6 个卡片导航 + 当前用户信息展示。

| 属性 | 说明 |
|------|------|
| `CurrentUserName` | 当前用户名（从 IAuthenticationService 加载） |
| `IsSysAdmin` | 是否为系统管理员 |

| 命令 | 导航目标 |
|------|----------|
| `NavigateToUserManagement` | `ViewNames.UserManagement` |
| `NavigateToHerbManagement` | `ViewNames.HerbManagement` |
| `NavigateToPatientManagement` | `ViewNames.PatientManagement` |
| `NavigateToFormulaManagement` | `ViewNames.FormulaManagement` |
| `NavigateToMedicalCaseManagement` | `ViewNames.MedicalCaseManagement` |
| `NavigateToSystemSettings` | `ViewNames.SystemSettings` |
| `NavigateToReports` | `ViewNames.ReportsHome` |

### SystemSettingsViewModel

**设计依据**: NavigableViewModelBase 子类，诊所配置 + 系统设置双服务协调。

| 依赖服务 | 用途 |
|----------|------|
| `ISystemSettingsService` | 系统级设置（系统名称、联系电话、备份路径等） |
| `IClinicSettingsService` | 诊所配置（clinic-settings.json 热更新） |

| 命令 | 说明 |
|------|------|
| `SaveCommand` | 保存设置 |
| `ResetCommand` | 重置为默认值 |
| `BrowseBackupPathCommand` | 浏览备份路径 |

### SystemSettingsService

**设计依据**: ISystemSettingsService 实现，JSON 文件持久化到 `%LOCALAPPDATA%\LYBT\Desktop\system-settings.json`。

| 属性 | 默认值 | 说明 |
|------|--------|------|
| `SystemName` | "中医诊疗系统" | 系统名称 |
| `HospitalName` | "" | 医院名称 |
| `ContactPhone` | "" | 联系电话 |
| `AutoBackupEnabled` | false | 自动备份开关 |
| `BackupPath` | "" | 备份路径 |

## 依赖关系

```
AdminModule
├── LYBT.Desktop.Contracts    # IAuthenticationService, INavigationCoordinator, IViewModelServices
├── LYBT.Desktop.Infrastructure  # NavigableViewModelBase, ViewNames, Extensions
├── LYBT.Desktop.Admin.Services  # ISystemSettingsService, SystemSettingsService
├── LYBT.Desktop.Infrastructure.Interfaces  # IClinicSettingsService
└── LYBT.Shared.Configuration    # ClinicSettingsOptions
```

## 设计决策

1. **薄包装 View 模式**: 管理视图（HerbManagementView 等）仅为 `UserControl` 包装，内部复用业务模块的 MasterDetail Control，实现 View 在角色台、Control 在业务模块的分离
2. **UserManagementView 支持 DefaultRoleFilter**: 通过导航参数 `DefaultRoleFilter` 支持按角色过滤用户列表
3. **双设置服务**: `ISystemSettingsService`（本地 JSON 文件）和 `IClinicSettingsService`（clinic-settings.json 热更新）分工明确
4. **SafeFireAndForget**: 构造函数中异步加载用户信息使用 `SafeFireAndForget` 避免阻塞

## 已知陷阱

- **薄包装 View 无 ViewModel**: HerbManagementView 等没有自己的 ViewModel，直接在 XAML 中嵌入业务模块的 Control，Prism ViewModelLocator 不生效
- **UserManagementView 导航参数**: 必须在 `OnNavigatedTo` 中手动读取 `DefaultRoleFilter` 参数并调用 `SetDefaultRoleFilter()`
- **SystemSettingsService 文件路径**: 使用 `Environment.SpecialFolder.LocalApplicationData`，不同用户有不同配置文件
- **IsSysAdmin 属性**: 构造时默认 `true`（避免按钮闪现），异步加载后根据实际用户信息更新
