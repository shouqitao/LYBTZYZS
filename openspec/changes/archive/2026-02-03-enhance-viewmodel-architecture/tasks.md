# enhance-viewmodel-architecture 任务清单

## Phase 1: 创建IViewModelServices聚合 [待执行]

### 1.1 创建接口
- [ ] 创建 `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/IViewModelServices.cs`

**文件内容**:
```csharp
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Regions;
using LYBT.Desktop.Contracts.Services.Session;
using LYBT.Desktop.Contracts.Services.Dialog;
using LYBT.Desktop.Contracts.Services.Auth;

namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// ViewModel服务聚合接口
/// OpenSpec: enhance-viewmodel-architecture
/// </summary>
public interface IViewModelServices
{
    ILoggerFactory LoggerFactory { get; }
    IEventAggregator EventAggregator { get; }
    IRegionManager RegionManager { get; }
    ISessionManager SessionManager { get; }
    IUserNotificationService UserNotificationService { get; }
    ICommonDialogService CommonDialogService { get; }
    IRoleRegistry RoleRegistry { get; }
}
```

### 1.2 创建实现
- [ ] 创建 `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/ViewModelServices.cs`

**文件内容**:
```csharp
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Regions;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Contracts.Services.Session;
using LYBT.Desktop.Contracts.Services.Dialog;
using LYBT.Desktop.Contracts.Services.Auth;

namespace LYBT.Desktop.Infrastructure.Services;

/// <summary>
/// ViewModel服务聚合实现
/// OpenSpec: enhance-viewmodel-architecture
/// </summary>
public sealed class ViewModelServices : IViewModelServices
{
    public ILoggerFactory LoggerFactory { get; }
    public IEventAggregator EventAggregator { get; }
    public IRegionManager RegionManager { get; }
    public ISessionManager SessionManager { get; }
    public IUserNotificationService UserNotificationService { get; }
    public ICommonDialogService CommonDialogService { get; }
    public IRoleRegistry RoleRegistry { get; }

    public ViewModelServices(
        ILoggerFactory loggerFactory,
        IEventAggregator eventAggregator,
        IRegionManager regionManager,
        ISessionManager sessionManager,
        IUserNotificationService userNotificationService,
        ICommonDialogService commonDialogService,
        IRoleRegistry roleRegistry)
    {
        LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        EventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        RegionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
        SessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        UserNotificationService = userNotificationService ?? throw new ArgumentNullException(nameof(userNotificationService));
        CommonDialogService = commonDialogService ?? throw new ArgumentNullException(nameof(commonDialogService));
        RoleRegistry = roleRegistry ?? throw new ArgumentNullException(nameof(roleRegistry));
    }
}
```

### 1.3 注册DI
- [ ] 更新 `src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs`

**变更内容**:
```csharp
// 在RegisterViewModelServices方法中添加
services.AddSingleton<IViewModelServices, ViewModelServices>();
```

### 1.4 验证
- [ ] `dotnet build LYBT.Desktop.sln -c Release --no-restore`

---

## Phase 2: 重构CoreViewModelBase [待执行]

### 2.1 修改文件
- [ ] `src/Client/Desktop/Core/LYBT.Desktop.Models/ViewModels/Base/CoreViewModelBase.cs`

**变更前** (约第45行):
```csharp
protected CoreViewModelBase(ILoggerFactory loggerFactory, IEventAggregator eventAggregator)
{
    LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    EventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
    Logger = loggerFactory.CreateLogger(GetType());
}
```

**变更后**:
```csharp
/// <summary>
/// 服务聚合对象，子类可通过此属性访问所有基础服务
/// </summary>
protected IViewModelServices Services { get; }

protected CoreViewModelBase(IViewModelServices services)
{
    Services = services ?? throw new ArgumentNullException(nameof(services));
    LoggerFactory = services.LoggerFactory;
    EventAggregator = services.EventAggregator;
    Logger = services.LoggerFactory.CreateLogger(GetType());
}
```

### 2.2 添加using语句
```csharp
using LYBT.Desktop.Contracts.Services;
```

### 2.3 验证
- [ ] 编译通过 (此时会有大量编译错误，因为子类尚未更新)

---

## Phase 3: 重构DialogViewModelBase [待执行]

### 3.1 修改文件
- [ ] `src/Client/Desktop/Core/LYBT.Desktop.Models/ViewModels/Base/DialogViewModelBase.cs`

**变更前** (约第30行):
```csharp
protected DialogViewModelBase(ILoggerFactory loggerFactory, IEventAggregator eventAggregator)
    : base(loggerFactory, eventAggregator)
{
}
```

**变更后**:
```csharp
protected DialogViewModelBase(IViewModelServices services)
    : base(services)
{
}
```

### 3.2 重构Dialog子类 (3个文件)

#### 3.2.1 ApiConnectionFailedDialogViewModel
- [ ] `src/Client/Desktop/Shell/Dialogs/ViewModels/ApiConnectionFailedDialogViewModel.cs`

**变更前**:
```csharp
public ApiConnectionFailedDialogViewModel(
    ILoggerFactory loggerFactory,
    IEventAggregator eventAggregator)
    : base(loggerFactory, eventAggregator)
```

**变更后**:
```csharp
public ApiConnectionFailedDialogViewModel(IViewModelServices services)
    : base(services)
```

#### 3.2.2 ConfirmationDialogViewModel
- [ ] `src/Client/Desktop/Shell/Dialogs/ViewModels/ConfirmationDialogViewModel.cs`

**变更前**:
```csharp
public ConfirmationDialogViewModel(
    ILoggerFactory loggerFactory,
    IEventAggregator eventAggregator)
    : base(loggerFactory, eventAggregator)
```

**变更后**:
```csharp
public ConfirmationDialogViewModel(IViewModelServices services)
    : base(services)
```

#### 3.2.3 EntityAuditLogDialogViewModel
- [ ] `src/Client/Desktop/Shell/Dialogs/ViewModels/EntityAuditLogDialogViewModel.cs`

**变更前**:
```csharp
public EntityAuditLogDialogViewModel(
    ILoggerFactory loggerFactory,
    IEventAggregator eventAggregator,
    IAuditLogRepository auditLogRepository)
    : base(loggerFactory, eventAggregator)
```

**变更后**:
```csharp
public EntityAuditLogDialogViewModel(
    IViewModelServices services,
    IAuditLogRepository auditLogRepository)
    : base(services)
```

### 3.3 验证
- [ ] 编译通过

---

## Phase 4: 重构NavigableViewModelBase [待执行]

### 4.1 修改文件
- [ ] `src/Client/Desktop/Core/LYBT.Desktop.Models/ViewModels/Base/NavigableViewModelBase.cs`

**变更前** (约第55行):
```csharp
protected NavigableViewModelBase(
    ILoggerFactory loggerFactory,
    IEventAggregator eventAggregator,
    IRegionManager regionManager,
    ISessionManager? sessionManager = null,
    IUserNotificationService? userNotificationService = null,
    ICommonDialogService? commonDialogService = null,
    IRoleRegistry? roleRegistry = null)
    : base(loggerFactory, eventAggregator)
{
    RegionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
    SessionManager = sessionManager;
    UserNotificationService = userNotificationService;
    CommonDialogService = commonDialogService;
    RoleRegistry = roleRegistry;
}
```

**变更后**:
```csharp
protected NavigableViewModelBase(IViewModelServices services)
    : base(services)
{
    RegionManager = services.RegionManager;
    SessionManager = services.SessionManager;
    UserNotificationService = services.UserNotificationService;
    CommonDialogService = services.CommonDialogService;
    RoleRegistry = services.RoleRegistry;
}
```

### 4.2 移除可空类型
修改属性声明从 `ISessionManager?` 改为 `ISessionManager` (因为IViewModelServices保证非空)

### 4.3 验证
- [ ] 编译通过 (此时Navigable子类会有编译错误)

---

## Phase 5: 重构NavigableViewModel子类 [待执行]

### 5.1 AdminHomeViewModel
- [ ] `src/Client/Desktop/Roles/LYBT.Desktop.Admin/ViewModels/AdminHomeViewModel.cs`

**变更前**:
```csharp
public AdminHomeViewModel(
    IRegionManager regionManager,
    IEventAggregator eventAggregator,
    ILoggerFactory loggerFactory,
    IAuthenticationService authService,
    IDialogService dialogService,
    INavigationCoordinator navigationCoordinator)
    : base(loggerFactory, eventAggregator, regionManager)
```

**变更后**:
```csharp
public AdminHomeViewModel(
    IViewModelServices services,
    IAuthenticationService authService,
    IDialogService dialogService,
    INavigationCoordinator navigationCoordinator)
    : base(services)
```

### 5.2 ClinicalHomeViewModel
- [ ] `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/ClinicalHomeViewModel.cs`

**变更模式同上**: 移除 `ILoggerFactory, IEventAggregator, IRegionManager` 参数，添加 `IViewModelServices services`

### 5.3 PatientSelectionViewModel
- [ ] `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/PatientSelectionViewModel.cs`

**变更模式同上**

### 5.4 LoginViewModel
- [ ] `src/Client/Desktop/Modules/LYBT.Desktop.Auth/ViewModels/LoginViewModel.cs`

**变更前** (注意此类使用了更多基类参数):
```csharp
public LoginViewModel(
    ILoginCoordinator loginCoordinator,
    ILoggerFactory loggerFactory,
    IEventAggregator eventAggregator,
    IRegionManager regionManager,
    IApplicationStateService applicationStateService,
    // ... 其他业务依赖
    ICommonDialogService? dialogService = null)
    : base(loggerFactory, eventAggregator, regionManager, null, null, dialogService)
```

**变更后**:
```csharp
public LoginViewModel(
    IViewModelServices services,
    ILoginCoordinator loginCoordinator,
    IApplicationStateService applicationStateService,
    // ... 其他业务依赖，移除dialogService因为已在services中
    )
    : base(services)
```

### 5.5 SystemSettingsViewModel
- [ ] `src/Client/Desktop/Roles/LYBT.Desktop.Admin/ViewModels/SystemSettingsViewModel.cs`

**变更模式同5.1**

### 5.6 MedicalCaseWorkspaceViewModel
- [ ] `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/MedicalCaseWorkspaceViewModel.cs`

**变更模式同5.1**

### 5.7 验证
- [ ] 编译通过

---

## Phase 6: 重构MasterDetailViewModelBase [待执行]

### 6.1 修改基类
- [ ] `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/MasterDetailViewModelBase.cs`

**核心变更**:
1. 继承链从 `ObservableObject` 改为 `CoreViewModelBase`
2. 移除 `IDisposable` 接口 (由CoreViewModelBase提供)
3. 添加 `IViewModelServices` 参数
4. 重命名字段 `_services` 为 `_masterDetailServices`

**变更前**:
```csharp
public abstract partial class MasterDetailViewModelBase<TListItem, TDetail>
    : ObservableObject, INavigationAware, IRegionMemberLifetime, IDisposable
    where TListItem : class
    where TDetail : class
{
    private readonly IMasterDetailServices<TListItem, TDetail> _services;
    protected ILogger Logger { get; }

    protected MasterDetailViewModelBase(
        IMasterDetailServices<TListItem, TDetail> services,
        ILoggerFactory loggerFactory)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        Logger = loggerFactory.CreateLogger(GetType());
        // ...
    }
}
```

**变更后**:
```csharp
public abstract partial class MasterDetailViewModelBase<TListItem, TDetail>
    : CoreViewModelBase, INavigationAware, IRegionMemberLifetime
    where TListItem : class
    where TDetail : class
{
    private readonly IMasterDetailServices<TListItem, TDetail> _masterDetailServices;

    protected IMasterDetailServices<TListItem, TDetail> MasterDetailServices => _masterDetailServices;

    protected MasterDetailViewModelBase(
        IViewModelServices services,
        IMasterDetailServices<TListItem, TDetail> masterDetailServices)
        : base(services)
    {
        _masterDetailServices = masterDetailServices ?? throw new ArgumentNullException(nameof(masterDetailServices));
        // ...
    }
}
```

### 6.2 更新内部服务引用
将所有 `_services.XXX` 改为 `_masterDetailServices.XXX` 或使用 `MasterDetailServices.XXX`

### 6.3 移除重复的Logger定义
`CoreViewModelBase` 已提供 `Logger` 属性，移除重复定义

### 6.4 验证
- [ ] 编译通过

---

## Phase 7: 重构MasterDetailViewModel子类 [待执行]

### 7.1 FormulaMasterDetailViewModel
- [ ] `src/Client/Desktop/Modules/LYBT.Desktop.Formula/ViewModels/FormulaMasterDetailViewModel.cs`

**变更前**:
```csharp
public FormulaMasterDetailViewModel(
    IMasterDetailServices<FormulaListDto, FormulaDetailDto> services,
    ILoggerFactory loggerFactory,
    IFormulaRepository formulaRepository)
    : base(services, loggerFactory)
```

**变更后**:
```csharp
public FormulaMasterDetailViewModel(
    IViewModelServices services,
    IMasterDetailServices<FormulaListDto, FormulaDetailDto> masterDetailServices,
    IFormulaRepository formulaRepository)
    : base(services, masterDetailServices)
```

### 7.2 HerbMasterDetailViewModel
- [ ] `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/ViewModels/HerbMasterDetailViewModel.cs`

**变更模式同上**

### 7.3 MedicalCaseMasterDetailViewModel
- [ ] `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseMasterDetailViewModel.cs`

**变更模式同上**

### 7.4 PatientMasterDetailViewModel
- [ ] `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientMasterDetailViewModel.cs`

**变更模式同上**

### 7.5 UserMasterDetailViewModel
- [ ] `src/Client/Desktop/Modules/LYBT.Desktop.Users/ViewModels/UserMasterDetailViewModel.cs`

**变更模式同上**

### 7.6 验证
- [ ] 编译通过

---

## Phase 8: 迁移AccountSettingsViewModel (消除特例) [待执行]

### 8.1 修改继承
- [ ] `src/Client/Desktop/Shell/ViewModels/AccountSettingsViewModel.cs`

**变更前**:
```csharp
public partial class AccountSettingsViewModel : CoreViewModelBase, INavigationAware
{
    private readonly ISessionManager _sessionManager;
    private readonly IRegionManager _regionManager;
    private readonly IUserNotificationService _notificationService;
    // ...

    public AccountSettingsViewModel(
        ILoggerFactory loggerFactory,
        IEventAggregator eventAggregator,
        ISessionManager sessionManager,
        IRegionManager regionManager,
        IUserNotificationService notificationService,
        // ... 业务依赖
    ) : base(loggerFactory, eventAggregator)
    {
        _sessionManager = sessionManager;
        _regionManager = regionManager;
        _notificationService = notificationService;
        // ...
    }
```

**变更后**:
```csharp
public partial class AccountSettingsViewModel : NavigableViewModelBase
{
    // 移除手动字段 - 使用基类属性: SessionManager, RegionManager, UserNotificationService

    public AccountSettingsViewModel(
        IViewModelServices services,
        IAuthService authService,
        IUserRepository userRepository
        // ... 仅保留业务依赖
    ) : base(services)
    {
        _authService = authService;
        _userRepository = userRepository;
        // ...
    }
```

### 8.2 移除手动INavigationAware实现
**移除以下方法**:
```csharp
// 删除这些方法
public bool IsNavigationTarget(NavigationContext navigationContext) => true;
public void OnNavigatedTo(NavigationContext navigationContext) { ... }
public void OnNavigatedFrom(NavigationContext navigationContext) { ... }
```

**替换为基类钩子**:
```csharp
protected override void OnNavigatedToCore(NavigationContext context)
{
    Logger.LogDebug("导航到账户设置页面");
    _ = LoadCurrentUserAsync();
}

protected override void OnNavigatedFromCore(NavigationContext context)
{
    _password = string.Empty;
    _newPassword = string.Empty;
    _confirmPassword = string.Empty;
}
```

### 8.3 更新内部服务引用
将 `_sessionManager` 改为 `SessionManager` (基类属性)
将 `_regionManager` 改为 `RegionManager` (基类属性)
将 `_notificationService` 改为 `UserNotificationService` (基类属性)

### 8.4 验证
- [ ] 编译通过
- [ ] 功能测试: 账户设置页面导航正常

---

## Phase 9: 重构MainWindowViewModel [待执行]

### 9.1 修改文件
- [ ] `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs`

**变更前**:
```csharp
public MainWindowViewModel(
    ILoggerFactory loggerFactory,
    IEventAggregator eventAggregator,
    // ... 业务依赖
) : base(loggerFactory, eventAggregator)
```

**变更后**:
```csharp
public MainWindowViewModel(
    IViewModelServices services,
    // ... 业务依赖
) : base(services)
```

### 9.2 验证
- [ ] 编译通过

---

## Phase 10: 最终验证 [待执行]

### 10.1 全量编译
- [ ] `dotnet build LYBT.Desktop.sln -c Release --no-restore`

### 10.2 功能测试
- [ ] 登录流程正常
- [ ] Admin主页导航正常
- [ ] Clinical主页导航正常
- [ ] 账户设置页面正常
- [ ] 各MasterDetail页面正常
- [ ] 对话框正常

### 10.3 架构验证
```bash
# 确认无遗留7参数构造函数
grep -r "ILoggerFactory loggerFactory.*IEventAggregator.*IRegionManager" --include="*.cs" src/Client/Desktop/

# 确认所有NavigableViewModelBase子类使用IViewModelServices
grep -r ": NavigableViewModelBase" --include="*.cs" src/Client/Desktop/

# 确认AccountSettingsViewModel已统一
grep -r "AccountSettingsViewModel.*: CoreViewModelBase" --include="*.cs" src/Client/Desktop/
```

### 10.4 清理
- [ ] 移除未使用的using语句
- [ ] 更新相关文档

---

## 完成标准

- [ ] 所有ViewModel基类使用IViewModelServices
- [ ] AccountSettingsViewModel统一到NavigableViewModelBase
- [ ] MasterDetailViewModelBase继承CoreViewModelBase
- [ ] 编译0错误0警告
- [ ] 所有功能正常

---

## 影响文件清单 (精确路径)

```
新增文件 (2个):
├── src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/IViewModelServices.cs
└── src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/ViewModelServices.cs

修改文件 - 基类 (4个):
├── src/Client/Desktop/Core/LYBT.Desktop.Models/ViewModels/Base/CoreViewModelBase.cs
├── src/Client/Desktop/Core/LYBT.Desktop.Models/ViewModels/Base/NavigableViewModelBase.cs
├── src/Client/Desktop/Core/LYBT.Desktop.Models/ViewModels/Base/DialogViewModelBase.cs
└── src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/MasterDetailViewModelBase.cs

修改文件 - Shell (3个):
├── src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs
├── src/Client/Desktop/Shell/ViewModels/AccountSettingsViewModel.cs
└── src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs

修改文件 - Dialog子类 (3个):
├── src/Client/Desktop/Shell/Dialogs/ViewModels/ApiConnectionFailedDialogViewModel.cs
├── src/Client/Desktop/Shell/Dialogs/ViewModels/ConfirmationDialogViewModel.cs
└── src/Client/Desktop/Shell/Dialogs/ViewModels/EntityAuditLogDialogViewModel.cs

修改文件 - Navigable子类 (6个):
├── src/Client/Desktop/Roles/LYBT.Desktop.Admin/ViewModels/AdminHomeViewModel.cs
├── src/Client/Desktop/Roles/LYBT.Desktop.Admin/ViewModels/SystemSettingsViewModel.cs
├── src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/ClinicalHomeViewModel.cs
├── src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/PatientSelectionViewModel.cs
├── src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/MedicalCaseWorkspaceViewModel.cs
└── src/Client/Desktop/Modules/LYBT.Desktop.Auth/ViewModels/LoginViewModel.cs

修改文件 - MasterDetail子类 (5个):
├── src/Client/Desktop/Modules/LYBT.Desktop.Formula/ViewModels/FormulaMasterDetailViewModel.cs
├── src/Client/Desktop/Modules/LYBT.Desktop.Herbs/ViewModels/HerbMasterDetailViewModel.cs
├── src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseMasterDetailViewModel.cs
├── src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientMasterDetailViewModel.cs
└── src/Client/Desktop/Modules/LYBT.Desktop.Users/ViewModels/UserMasterDetailViewModel.cs

总计: 2个新增 + 21个修改 = 23个文件
```

---

## 执行顺序与依赖关系

```
Phase 1 (IViewModelServices创建)
    │
    ▼
Phase 2 (CoreViewModelBase重构)
    │
    ├──► Phase 3 (DialogViewModelBase + 子类)
    │
    └──► Phase 4 (NavigableViewModelBase)
              │
              ├──► Phase 5 (Navigable子类)
              │
              └──► Phase 6 (MasterDetailViewModelBase)
                        │
                        ▼
                  Phase 7 (MasterDetail子类)
                        │
                        ▼
                  Phase 8 (AccountSettingsViewModel统一)
                        │
                        ▼
                  Phase 9 (MainWindowViewModel)
                        │
                        ▼
                  Phase 10 (最终验证)
```

**关键点**: Phase 2完成后会产生编译错误，必须连续完成Phase 3-9才能恢复编译通过

---

## 风险缓解检查点

| 检查点 | 验证命令 | 回滚点 |
|--------|----------|--------|
| Phase 1完成 | `dotnet build` 通过 | Git commit |
| Phase 3完成 | Dialog对话框正常 | Git commit |
| Phase 5完成 | 导航功能正常 | Git commit |
| Phase 7完成 | MasterDetail功能正常 | Git commit |
| Phase 8完成 | 账户设置功能正常 | Git commit |

---

**任务状态**: 待确认后执行
**预估工时**: 6-8小时
**执行建议**: 每个Phase完成后创建Git commit，便于问题回滚
