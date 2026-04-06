# LYBT.Desktop.Admin

> 管理员角色模块 | 6功能导航工作台 | Prism模块化

## 项目定位

- **层级**: Desktop Roles
- **职责**: 提供管理员角色专属工作台主页,集成用户管理、中药管理、患者管理、方剂管理、病案管理、系统设置6个功能模块的快速导航,支持基于角色的权限控制
- **状态**: Active

## 目录结构

```
LYBT.Desktop.Admin/
├── AdminModule.cs           # Prism模块注册
├── ViewModels/
│   └── AdminHomeViewModel.cs   # 管理员主页ViewModel(6个导航命令)
└── Views/
    ├── AdminHomeView.xaml       # 管理员主页视图
    └── AdminHomeView.xaml.cs    # 视图后置代码
```

## 核心组件

| 名称 | 说明 |
|------|------|
| AdminModule | Prism模块注册,自动发现Views和ViewModels |
| AdminHomeViewModel | 6个导航命令 + INavigationAware实现 + 权限检查 |
| AdminHomeView | 管理员工作台主页UI,包含6个功能卡片 |

## 设计依据

管理员工作台采用"导航枢纽"模式:单一主页通过6个DelegateCommand导航到各功能模块,每个命令绑定权限检查(SessionManager.HasPermission)。这种设计将角色入口与功能模块解耦,新增管理功能只需添加导航命令,无需修改现有模块。

## 依赖关系

### 依赖
- LYBT.Desktop.Foundation (Desktop端基础类型和接口)
- LYBT.Desktop.Infrastructure (区域管理、导航服务)
- LYBT.Desktop.Models (ViewModelBase基类)
- LYBT.Desktop.Contracts (区域名称常量)
- LYBT.Shared.Models (共享DTO模型)

### 被依赖
- LYBT.Desktop.Shell (Prism模块注册,加载管理员模块)

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-03-01 | 按精简规范重写README,代码示例迁移至CLAUDE.md |
| 2025-10-29 | 初始版本 |

## 开发笔记

# LYBT.Desktop.Admin 代码知识

## 架构决策

| 决策 | 原因 | 日期 |
|------|------|------|
| 导航枢纽模式 (9个 [RelayCommand]) | 角色入口与功能模块解耦，新增功能只需添加命令 | 2025-10-29 |
| INavigationCoordinator 统一导航 | 通过聚合服务解耦 ViewModel 与 IRegionManager 直接依赖 | 2025-10-29 |
| 薄包装视图复用业务模块 Control | 5 个管理视图 (Herb/Formula/Patient/MedicalCase/User) 复用各模块 MasterDetailControl，无独立 ViewModel | 2025-10-29 |

## 已知陷阱

| 问题 | 原因 | 解决方案 |
|------|------|----------|
| LoadCurrentUser 是 async void | 构造函数中调用，异常仅被日志记录不会向上传播 | 确保 IAuthenticationService.GetCurrentUserAsync 内部有完整异常处理 |
| SystemSettingsViewModel 仍使用 DelegateCommand | 与 AdminHomeViewModel 的 [RelayCommand] 风格不一致 | 低优先级统一，当前功能正常 |

## 代码示例

### Shell 层加载模块 (App.xaml.cs)

```csharp
protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
{
    moduleCatalog.AddModule<AdminModule>(InitializationMode.WhenAvailable);
}
```

### AdminHomeViewModel 导航模式

```csharp
public partial class AdminHomeViewModel : NavigableViewModelBase
{
    private readonly INavigationCoordinator _navigationCoordinator;

    public AdminHomeViewModel(
        IViewModelServices services,
        IAuthenticationService authService,
        IDialogService dialogService,
        INavigationCoordinator navigationCoordinator)
        : base(services)
    {
        _navigationCoordinator = navigationCoordinator;
    }

    [RelayCommand]
    private void NavigateToUserManagement()
        => NavigateTo(ViewNames.UserManagementView);

    private void NavigateTo(string viewName)
        => _navigationCoordinator.NavigateToView(viewName);
}
```

## 代码文件结构

### 模块注册 (AdminModule.cs, 37行)

Prism模块入口，`[Module(ModuleName = nameof(AdminModule))]`。

注册内容:
- ViewModel: `AdminHomeViewModel`, `SystemSettingsViewModel`
- 导航视图: `AdminHomeView`, `SystemSettingsView`
- 薄包装管理视图: `HerbManagementView`, `FormulaManagementView`, `PatientManagementView`, `MedicalCaseManagementView`, `UserManagementView` (复用业务模块Control，无独立ViewModel)

### ViewModels/AdminHomeViewModel.cs (240行)

管理员工作台主页ViewModel。继承 `NavigableViewModelBase`。

依赖服务:
- `IAuthenticationService` - 获取当前用户信息
- `IDialogService` - 对话框
- `INavigationCoordinator` - 统一导航

可观察属性:
- `CurrentUserName` (string) - 当前用户名，默认"系统管理员"
- `IsSysAdmin` (bool) - 是否系统管理员，联动 `IsNotSysAdmin` 计算属性

命令 (全部 `[RelayCommand]`):
- `NavigateToUserManagement()` - 导航到用户管理
- `NavigateToHerbManagement()` - 导航到药材管理
- `NavigateToPatientManagement()` - 导航到患者管理
- `NavigateToFormulaManagement()` - 导航到验方管理
- `NavigateToMedicalCaseManagement()` - 导航到医案管理
- `NavigateToSystemSettings()` - 导航到系统设置
- `NavigateToSync()` - 导航到数据同步
- `EditProfile()` - 导航到账户设置(个人资料)
- `ChangePassword()` - 导航到账户设置(修改密码)，传递 `Tab=Password` 参数

关键方法:
- `LoadCurrentUser()` (async void) - 构造函数调用，通过 `IAuthenticationService.GetCurrentUserAsync()` 加载用户信息
- `NavigateTo(string viewName)` - 统一导航辅助方法，使用 `INavigationCoordinator`

INavigationAware: `IsNavigationTarget` 返回 true (单例复用)

### ViewModels/SystemSettingsViewModel.cs (232行)

系统设置ViewModel。继承 `NavigableViewModelBase`。

依赖服务:
- `ISystemSettingsService` - 系统设置读写

属性 (SetProperty模式):
- `SystemName` (string) - 系统名称，默认"中医诊疗系统"
- `HospitalName` (string) - 医院名称
- `ContactPhone` (string) - 联系电话
- `AutoBackupEnabled` (bool) - 自动备份开关
- `BackupPath` (string) - 备份路径

命令 (DelegateCommand):
- `SaveCommand` - 保存设置到本地文件
- `ResetCommand` - 重置为默认值（带确认弹窗）
- `BrowseBackupPathCommand` - 打开文件对话框选择备份路径

关键方法:
- `InitializeAsync(NavigationContext)` - 从 `ISystemSettingsService` 加载设置到属性
- `ExecuteSaveAsync()` - 将属性写回服务并调用 `Save()`
- `ExecuteResetAsync()` - 确认后调用 `ResetToDefaults()` 并刷新属性
- `ExecuteBrowseBackupPathAsync()` - 使用 `OpenFileDialog` 选择路径

### Services/ISystemSettingsService.cs (45行)

系统设置服务接口。管理 `%LOCALAPPDATA%\LYBT\Desktop\system-settings.json`。

属性: `SystemName`, `HospitalName`, `ContactPhone`, `AutoBackupEnabled`, `BackupPath`
方法: `Save()`, `ResetToDefaults()`

### Services/SystemSettingsService.cs (194行)

`ISystemSettingsService` 实现。使用 `System.Text.Json` 序列化。

关键实现:
- 构造函数中创建 `%LOCALAPPDATA%\LYBT\Desktop\` 目录并加载设置
- 每个属性 setter 内含变更检测，变更时自动调用 `Save()`
- 内嵌私有类 `SystemSettings` 作为序列化模型
- `LoadSettings()` - 文件存在则反序列化，否则返回默认配置
- `CreateDefaultSettings()` - 静态方法创建默认设置实例

### Views/AdminHomeView.xaml.cs (16行)

管理员工作台主页视图，标准code-behind，仅 `InitializeComponent()`。

### Views/SystemSettingsView.xaml.cs (15行)

系统设置视图，标准code-behind，仅 `InitializeComponent()`。

### Views/UserManagementView.xaml.cs (19行)

用户管理薄包装视图，复用业务模块 `UserMasterDetailControl`。无独立ViewModel。

### Views/HerbManagementView.xaml.cs (19行)

药材管理薄包装视图，复用业务模块 `HerbMasterDetailControl`。无独立ViewModel。

### Views/FormulaManagementView.xaml.cs (19行)

经验方管理薄包装视图，复用业务模块 `FormulaMasterDetailControl`。无独立ViewModel。

### Views/PatientManagementView.xaml.cs (19行)

患者管理薄包装视图，复用业务模块 `PatientMasterDetailControl`。无独立ViewModel。

### Views/MedicalCaseManagementView.xaml.cs (19行)

医案管理薄包装视图，复用业务模块 `MedicalCaseMasterDetailControl`。无独立ViewModel。

### 死代码分析

无死代码。所有类型均有引用:
- `AdminHomeViewModel`, `SystemSettingsViewModel` - 模块注册 + Shell Logger注册
- `ISystemSettingsService` / `SystemSettingsService` - Shell `ServiceCollectionExtensions` 注册为 Singleton
- 5个薄包装View - 模块 `RegisterForNavigation` + AdminHomeViewModel 导航命令引用
- 模块由 Shell `App.xaml.cs` 加载: `moduleCatalog.AddModule<AdminModule>(InitializationMode.WhenAvailable)`

## 模块演进记录

| 日期 | 变更 |
|------|------|
| 2025-10-29 | 初始版本,6个功能导航 |
