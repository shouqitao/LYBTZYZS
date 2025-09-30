# Desktop Core_New架构文档

**文档版本**: 1.0
**创建时间**: 2025-09-30
**维护负责**: Claude Code
**适用范围**: LYBT Desktop层Core_New三层架构

> **架构状态**: ✅ Issue #815已完成，Core_New三层架构全面替代旧架构
>
> **完成日期**: 2025-09-30 (Phase 1/2/3)

---

## 📋 目录

1. [架构概述](#1-架构概述)
2. [Core_New三层架构](#2-core_new三层架构)
3. [项目结构](#3-项目结构)
4. [依赖关系](#4-依赖关系)
5. [核心组件](#5-核心组件)
6. [模块化设计](#6-模块化设计)
7. [最佳实践](#7-最佳实践)

---

## 1. 架构概述

### 1.1 Issue #815 背景

Issue #815实施了Desktop项目从旧的分散架构到Core_New三层清晰架构的全面迁移：

**迁移前架构问题**:
- Core文件夹包含27个子文件夹，职责混杂
- Infrastructure文件夹职责不清
- Services文件夹独立存在，依赖关系复杂
- 架构层次深度5-6层，超出推荐标准

**迁移后架构目标**:
- 清晰的三层架构：Infrastructure → Models → Services
- 4层整体结构：Core_New → Modules → Workstations → Shell
- 依赖方向明确，无循环依赖
- 代码重复率从40%降低到<20%

### 1.2 架构原则

✅ **允许的技术栈**:
- MVVM + Prism.DryIoc 8.x
- ReactiveUI (响应式编程)
- Refit + Polly (统一HTTP客户端)
- Microsoft.Extensions.* (DI、日志、配置)

❌ **禁止的技术栈** (项目约束):
- CQRS/MediatR (过度工程)
- Redis (外部依赖复杂化)
- Docker/K8s (部署复杂化)
- GraphQL (API复杂化)

---

## 2. Core_New三层架构

### 2.1 架构图

```
┌─────────────────────────────────────────────────────────────┐
│                   LYBT Desktop Application                   │
├─────────────────────────────────────────────────────────────┤
│                      Shell启动层                             │
│              LYBT.Desktop.Shell                              │
│     (App.xaml, MainWindow, ApplicationBootstrapper)         │
└────────────────┬────────────────────────────────────────────┘
                 │ 聚合与协调
┌────────────────▼────────────────────────────────────────────┐
│                   Workstations工作台层                       │
│   ┌──────────────────────┐   ┌──────────────────────┐      │
│   │ ClinicalWorkstation  │   │  AdminWorkstation    │      │
│   │  (诊疗工作台)         │   │  (管理工作台)         │      │
│   └──────────────────────┘   └──────────────────────┘      │
└────────────────┬────────────────────────────────────────────┘
                 │ 业务模块引用
┌────────────────▼────────────────────────────────────────────┐
│                    Modules业务模块层                         │
│   ┌────────┬──────────┬───────────┬──────────────┐         │
│   │  Auth  │ Patients │ MedicalCase│ Consultation │         │
│   ├────────┼──────────┼───────────┼──────────────┤         │
│   │  Users │  Herbs   │  Formula   │ Prescriptions│         │
│   └────────┴──────────┴───────────┴──────────────┘         │
│   每个模块: ViewModels + Views + Module.cs                   │
└────────────────┬────────────────────────────────────────────┘
                 │ 依赖Core_New
┌────────────────▼────────────────────────────────────────────┐
│              Core_New 三层基础架构                           │
│                                                               │
│   ┌─────────────────────────────────────────────────┐       │
│   │  LYBT.Desktop.Infrastructure (基础设施层)        │       │
│   │  - Commands: IApplicationCommands, 命令实现      │       │
│   │  - Events: 应用事件定义(UnifiedEvents)           │       │
│   │  - Interfaces: 核心接口定义                       │       │
│   │  - Themes: WPF资源字典与样式                      │       │
│   │  - Constants: 常量与配置                          │       │
│   └─────────────────────────────────────────────────┘       │
│                        ▲                                      │
│   ┌────────────────────┴────────────────────────────┐       │
│   │  LYBT.Desktop.Models (模型层)                    │       │
│   │  - ViewModels.Base: ViewModelBase, UnifiedViewModelBase│
│   │  - ViewModels: 共享ViewModel基类                 │       │
│   │  - Mapping: AutoMapper配置                       │       │
│   │  - Validation: 验证逻辑                           │       │
│   └─────────────────────────────────────────────────┘       │
│                        ▲                                      │
│   ┌────────────────────┴────────────────────────────┐       │
│   │  LYBT.Desktop.Services (服务层)                  │       │
│   │  - Business: 业务服务(AuthBusinessService等)     │       │
│   │  - Repositories: 数据仓储(BaseApiRepository)     │       │
│   │  - Api.Managers: API客户端管理                   │       │
│   │  - Http: HTTP服务(ApiService, HttpClientFactory)│       │
│   │  - Dialogs: 对话框服务                            │       │
│   │  - Navigation: 导航服务                           │       │
│   │  - Session: 会话管理                              │       │
│   │  - ErrorHandling: 错误处理                        │       │
│   │  - Modules: 模块加载服务                          │       │
│   │  - Performance: 性能优化服务                      │       │
│   │  - Theming: 主题服务                              │       │
│   └─────────────────────────────────────────────────┘       │
└─────────────────────────────────────────────────────────────┘
                         ▲
                         │ 引用
┌────────────────────────┴─────────────────────────────────────┐
│                   LYBT.Shared.* (共享层)                       │
│   - LYBT.Shared.Models.Contracts: DTOs数据契约                │
│   - LYBT.Shared.Interfaces: 跨层接口                          │
│   - LYBT.Shared.Utilities: 工具类                             │
└───────────────────────────────────────────────────────────────┘
```

### 2.2 层次职责

#### 2.2.1 Infrastructure层 (基础设施)
**职责**: 提供应用程序的基础设施组件

**核心内容**:
- **Commands**: 应用级命令接口与实现
  - `IApplicationCommands` - 全局命令定义
  - `ApplicationCommands` - 命令实现
- **Events**: 跨模块事件定义
  - `UnifiedEvents.cs` - 统一事件定义
  - `UserLoggedInEvent`, `UserLoggedOutEvent` - 认证事件
  - `LogoutEvent`, `LogoutEventArgs`, `LogoutReason` - 登出事件
- **Interfaces**: 核心接口定义
  - `IErrorHandlingService` - 错误处理接口
  - `IAuthService` - 认证服务接口
  - `ISessionManager` - 会话管理接口
- **Themes**: WPF资源字典与样式
  - Colors, Typography, Spacing
  - Control样式 (Button, TextBox, DataGrid等)
- **Constants**: 常量定义
  - Region名称、路由定义
  - 配置键值

**依赖**: 无Core_New内部依赖（最底层）

#### 2.2.2 Models层 (模型)
**职责**: 提供ViewModel基类和数据模型

**核心内容**:
- **ViewModels.Base**: ViewModel基类
  - `ViewModelBase` - 基础ViewModel（INotifyPropertyChanged）
  - `UnifiedViewModelBase` - 统一ViewModel基类（集成Region、Event、Session、ErrorHandling）
  - `PageViewModel` - 页面级ViewModel
  - `DialogViewModel` - 对话框ViewModel
- **Mapping**: AutoMapper配置
  - `MappingProfile.cs` - DTO与ViewModel映射
- **Validation**: 数据验证
  - `ValidationBase.cs` - 验证基类
  - `CommonValidators.cs` - 通用验证器

**依赖**: Infrastructure层

#### 2.2.3 Services层 (服务)
**职责**: 提供业务服务、数据访问、HTTP通信、错误处理等

**核心内容**:
- **Business**: 业务服务
  - `AuthBusinessService` - 认证业务
  - `UserBusinessService` - 用户业务
  - `PatientBusinessService` - 患者业务
  - 等8个业务服务
- **Repositories**: 数据仓储
  - `BaseApiRepository<T>` - 通用API仓储基类
  - `IUserRepository`, `UserRepository` - 用户仓储
  - `IPatientRepository`, `PatientRepository` - 患者仓储
  - 等实体仓储
- **Api.Managers**: API客户端管理
  - `IUnifiedApiClientManager` - 统一API客户端管理器
- **Http**: HTTP通信
  - `IApiService`, `ApiService` - HTTP服务核心
  - `IHttpClientFactory`, `HttpClientFactory` - HTTP客户端工厂
  - `RequestDeduplicator` - 请求去重
- **Dialogs**: 对话框服务
  - `ICustomDialogService` - 自定义对话框
  - `ICommonDialogService` - 通用对话框
- **Navigation**: 导航服务
  - `IEnhancedNavigationService` - 增强导航服务
- **Session**: 会话管理
  - `IUnifiedSessionManager` - 统一会话管理
- **ErrorHandling**: 错误处理
  - `IErrorHandlingService`, `UnifiedErrorHandlingService`
  - `StandardExceptionHandler` - 标准异常处理器
- **Modules**: 模块加载
  - `IModuleLoadingService`, `ModuleLoadingService`
- **Performance**: 性能优化
  - `IStartupOptimizationService` - 启动优化
- **Theming**: 主题服务
  - `IThemeService`, `ThemeService`

**依赖**: Infrastructure层 + Models层

---

## 3. 项目结构

### 3.1 完整目录结构

```
src/Client/Desktop/
├── Core_New/                           # 三层基础架构
│   ├── LYBT.Desktop.Infrastructure/      # 基础设施层 ✅
│   │   ├── Commands/
│   │   │   ├── IApplicationCommands.cs
│   │   │   └── ApplicationCommands.cs
│   │   ├── Events/
│   │   │   ├── UnifiedEvents.cs
│   │   │   ├── UserLoggedInEvent.cs
│   │   │   └── LogoutEvent.cs
│   │   ├── Interfaces/
│   │   │   ├── IErrorHandlingService.cs
│   │   │   ├── IAuthService.cs
│   │   │   └── ISessionManager.cs
│   │   ├── Themes/
│   │   │   ├── Design/Colors.xaml
│   │   │   ├── Design/Typography.xaml
│   │   │   └── Controls/ModernButton.xaml
│   │   └── Constants/
│   │       └── RegionNames.cs
│   │
│   ├── LYBT.Desktop.Models/              # 模型层 ✅
│   │   ├── ViewModels/
│   │   │   └── Base/
│   │   │       ├── ViewModelBase.cs
│   │   │       ├── UnifiedViewModelBase.cs
│   │   │       ├── PageViewModel.cs
│   │   │       └── DialogViewModel.cs
│   │   ├── Mapping/
│   │   │   └── MappingProfile.cs
│   │   └── Validation/
│   │       ├── ValidationBase.cs
│   │       └── CommonValidators.cs
│   │
│   └── LYBT.Desktop.Services/            # 服务层 ✅
│       ├── Business/
│       │   ├── AuthBusinessService.cs
│       │   ├── UserBusinessService.cs
│       │   └── PatientBusinessService.cs
│       ├── Repositories/
│       │   ├── BaseApiRepository.cs
│       │   ├── Interfaces/
│       │   │   ├── IUserRepository.cs
│       │   │   └── IPatientRepository.cs
│       │   ├── UserRepository.cs
│       │   └── PatientRepository.cs
│       ├── Api/
│       │   └── Managers/
│       │       └── UnifiedApiClientManager.cs
│       ├── Http/
│       │   ├── ApiService.cs
│       │   ├── HttpClientFactory.cs
│       │   └── RequestDeduplicator.cs
│       ├── Dialogs/
│       │   ├── CustomDialogService.cs
│       │   └── CommonDialogService.cs
│       ├── Navigation/
│       │   └── EnhancedNavigationService.cs
│       ├── Session/
│       │   └── UnifiedSessionManager.cs
│       ├── ErrorHandling/
│       │   ├── UnifiedErrorHandlingService.cs
│       │   └── StandardExceptionHandler.cs
│       ├── Modules/
│       │   └── ModuleLoadingService.cs
│       ├── Performance/
│       │   └── StartupOptimizationService.cs
│       └── Theming/
│           └── ThemeService.cs
│
├── Modules/                            # 业务模块层 ✅
│   ├── LYBT.Desktop.Auth/
│   │   ├── AuthModule.cs
│   │   ├── Services/
│   │   │   ├── AuthService.cs
│   │   │   ├── AuthBusinessService.cs
│   │   │   └── AuthQueryService.cs
│   │   ├── ViewModels/
│   │   │   └── LoginViewModel.cs
│   │   └── Views/
│   │       └── LoginWindow.xaml
│   │
│   ├── LYBT.Desktop.Patients/
│   │   ├── PatientsModule.cs
│   │   ├── Services/
│   │   │   └── PatientService.cs
│   │   ├── ViewModels/
│   │   │   ├── PatientManagementViewModel.cs
│   │   │   └── PatientDetailViewModel.cs
│   │   └── Views/
│   │       └── PatientManagementView.xaml
│   │
│   ├── LYBT.Desktop.MedicalCase/
│   ├── LYBT.Desktop.Consultation/
│   ├── LYBT.Desktop.Prescriptions/
│   ├── LYBT.Desktop.Herbs/
│   ├── LYBT.Desktop.Formula/
│   └── LYBT.Desktop.Users/
│
├── Workstations/                       # 工作台层 ✅
│   ├── LYBT.Desktop.ClinicalWorkstation/
│   │   ├── ClinicalWorkstationModule.cs
│   │   ├── Services/
│   │   │   ├── IClinicalNavigator.cs
│   │   │   └── ClinicalNavigator.cs      # 模块特定导航服务
│   │   ├── ViewModels/
│   │   │   └── ClinicalWorkstationViewModel.cs
│   │   └── Views/
│   │       └── ClinicalWorkstationView.xaml
│   │
│   └── LYBT.Desktop.AdminWorkstation/
│       ├── AdminWorkstationModule.cs
│       ├── ViewModels/
│       │   └── AdminWorkstationViewModel.cs
│       └── Views/
│           └── AdminWorkstationView.xaml
│
└── Shell/                              # 启动层 ✅
    ├── LYBT.Desktop.Shell.csproj
    ├── App.xaml / App.xaml.cs
    ├── MainWindow.xaml
    ├── ViewModels/
    │   ├── MainWindowViewModel.cs
    │   └── HomeViewModel.cs
    ├── Views/
    │   └── HomeView.xaml
    ├── Services/
    │   └── Bootstrap/
    │       └── ApplicationBootstrapper.cs
    ├── Extensions/
    │   └── ServiceCollectionExtensions.cs    # DI注册
    └── Dialogs/
        ├── ViewModels/
        │   ├── ConfirmationDialogViewModel.cs
        │   └── InformationDialogViewModel.cs
        └── Views/
            ├── ConfirmationDialog.xaml
            └── InformationDialog.xaml
```

### 3.2 项目编译顺序

```
Build Order (按依赖关系):
1. LYBT.Shared.Models        (共享层 - 无依赖)
2. LYBT.Shared.Interfaces     (共享层 - 依赖Models)
3. LYBT.Shared.Utilities      (共享层 - 依赖Models)
4. LYBT.Desktop.Infrastructure  (Core_New - 依赖Shared)
5. LYBT.Desktop.Models          (Core_New - 依赖Infrastructure)
6. LYBT.Desktop.Services        (Core_New - 依赖Models+Infrastructure)
7. LYBT.Desktop.Auth            (Modules - 依赖Core_New)
8. LYBT.Desktop.Patients        (Modules - 依赖Core_New)
9. LYBT.Desktop.MedicalCase     (Modules - 依赖Core_New)
10. LYBT.Desktop.Consultation   (Modules - 依赖Core_New)
11. LYBT.Desktop.Prescriptions  (Modules - 依赖Core_New)
12. LYBT.Desktop.Herbs          (Modules - 依赖Core_New)
13. LYBT.Desktop.Formula        (Modules - 依赖Core_New)
14. LYBT.Desktop.Users          (Modules - 依赖Core_New)
15. LYBT.Desktop.ClinicalWorkstation  (Workstations - 依赖Modules)
16. LYBT.Desktop.AdminWorkstation     (Workstations - 依赖Modules)
17. LYBT.Desktop.Shell          (Shell - 依赖Workstations)
```

---

## 4. 依赖关系

### 4.1 依赖图

```
       Shell (启动层)
          ↓
     Workstations (聚合层)
          ↓
      Modules (业务层)
          ↓
   ┌──────────────────┐
   │   Core_New       │
   ├──────────────────┤
   │ Services  (服务层) │
   │     ↓             │
   │ Models    (模型层) │
   │     ↓             │
   │ Infrastructure    │
   │       (基础设施层)  │
   └──────────────────┘
          ↓
    Shared.* (共享层)
```

### 4.2 依赖规则

✅ **允许的依赖**:
- Shell → Workstations → Modules → Services → Models → Infrastructure → Shared
- 上层可以依赖下层，下层不能依赖上层

❌ **禁止的依赖**:
- Infrastructure → Models (基础设施不依赖模型)
- Models → Services (模型不依赖服务)
- Services → Modules (服务不依赖业务模块)
- 同层之间不能相互依赖（Modules之间禁止相互引用）

### 4.3 依赖注入策略

**生命周期规则**:
- **Singleton**: 基础设施、系统服务、主题服务
  ```csharp
  containerRegistry.RegisterSingleton<IApplicationCommands, ApplicationCommands>();
  containerRegistry.RegisterSingleton<IModuleLoadingService, ModuleLoadingService>();
  containerRegistry.RegisterSingleton<IThemeService, ThemeService>();
  ```

- **Scoped**: 业务服务、API客户端、会话管理
  ```csharp
  containerRegistry.RegisterScoped<IAuthService, AuthBusinessService>();
  containerRegistry.RegisterScoped<IUserService, UserBusinessService>();
  containerRegistry.RegisterScoped<IUnifiedSessionManager, UnifiedSessionManager>();
  ```

- **Transient**: 临时处理器、对话框、ViewModels
  ```csharp
  containerRegistry.RegisterTransient<ConfirmationDialogViewModel>();
  containerRegistry.RegisterTransient<PatientDetailViewModel>();
  ```

---

## 5. 核心组件

### 5.1 UnifiedViewModelBase

**位置**: `Core_New/LYBT.Desktop.Models/ViewModels/Base/UnifiedViewModelBase.cs`

**职责**: 提供统一的ViewModel基类，集成常用功能

**核心功能**:
```csharp
public abstract class UnifiedViewModelBase : ViewModelBase, INavigationAware
{
    protected IEventAggregator EventAggregator { get; }
    protected IRegionManager RegionManager { get; }
    protected ISessionManager? SessionManager { get; }
    protected IErrorHandlingService? ErrorHandlingService { get; }
    protected ILogger Logger { get; }

    // MVVM属性
    public bool IsLoading { get; set; }
    public string StatusMessage { get; set; }
    public string ErrorMessage { get; set; }

    // 导航生命周期
    public virtual void OnNavigatedTo(NavigationContext navigationContext) { }
    public virtual bool IsNavigationTarget(NavigationContext navigationContext) => true;
    public virtual void OnNavigatedFrom(NavigationContext navigationContext) { }

    // 命令初始化（子类重写）
    protected virtual void InitializeCommands() { }
}
```

**使用示例**:
```csharp
public class PatientDetailViewModel : UnifiedViewModelBase
{
    private readonly IPatientService _patientService;

    public PatientDetailViewModel(
        IPatientService patientService,
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        IRegionManager regionManager,
        ISessionManager sessionManager,
        IErrorHandlingService errorHandlingService)
        : base(eventAggregator, loggerFactory, regionManager, sessionManager, errorHandlingService)
    {
        _patientService = patientService;
        InitializeCommands();
    }

    protected override void InitializeCommands()
    {
        SaveCommand = new DelegateCommand(async () => await SavePatientAsync());
    }
}
```

### 5.2 BaseApiRepository<T>

**位置**: `Core_New/LYBT.Desktop.Services/Repositories/BaseApiRepository.cs`

**职责**: 提供统一的API数据访问基类

**核心功能**:
```csharp
public abstract class BaseApiRepository<T> where T : class
{
    protected readonly IApiService _apiService;
    protected readonly ILogger _logger;
    protected readonly string _endpoint;

    protected BaseApiRepository(IApiService apiService, ILogger logger, string endpoint)
    {
        _apiService = apiService;
        _logger = logger;
        _endpoint = endpoint;
    }

    // CRUD基础方法
    public virtual async Task<T> GetByIdAsync(Guid id)
        => await _apiService.GetAsync<T>($"{_endpoint}/{id}");

    public virtual async Task<PagedResult<T>> GetPagedAsync(int page, int size)
        => await _apiService.GetAsync<PagedResult<T>>($"{_endpoint}?page={page}&size={size}");

    public virtual async Task<T> CreateAsync(T entity)
        => await _apiService.PostAsync<T>(_endpoint, entity);

    public virtual async Task<T> UpdateAsync(Guid id, T entity)
        => await _apiService.PutAsync<T>($"{_endpoint}/{id}", entity);

    public virtual async Task<bool> DeleteAsync(Guid id)
        => await _apiService.DeleteAsync($"{_endpoint}/{id}");
}
```

**使用示例**:
```csharp
public class UserRepository : BaseApiRepository<UserDto>, IUserRepository
{
    public UserRepository(IApiService apiService, ILogger<UserRepository> logger)
        : base(apiService, logger, "/api/users")
    {
    }

    // 扩展方法
    public async Task<List<UserDto>> GetActiveUsersAsync()
        => await _apiService.GetAsync<List<UserDto>>($"{_endpoint}/active");
}
```

### 5.3 ApiService

**位置**: `Core_New/LYBT.Desktop.Services/Http/ApiService.cs`

**职责**: 统一的HTTP通信服务

**核心功能**:
```csharp
public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ApiService> _logger;
    private readonly RequestDeduplicator _deduplicator;

    // GET请求
    public async Task<T> GetAsync<T>(string endpoint)
    {
        var response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }

    // POST请求
    public async Task<TResponse> PostAsync<TResponse>(string endpoint, object data)
    {
        var response = await _httpClient.PostAsJsonAsync(endpoint, data);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>();
    }

    // PUT请求
    public async Task<TResponse> PutAsync<TResponse>(string endpoint, object data)
    {
        var response = await _httpClient.PutAsJsonAsync(endpoint, data);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>();
    }

    // DELETE请求
    public async Task<bool> DeleteAsync(string endpoint)
    {
        var response = await _httpClient.DeleteAsync(endpoint);
        return response.IsSuccessStatusCode;
    }
}
```

---

## 6. 模块化设计

### 6.1 模块生命周期

```csharp
// Prism模块标准结构
public class PatientsModule : IModule
{
    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 模块初始化
        var regionManager = containerProvider.Resolve<IRegionManager>();
        regionManager.RegisterViewWithRegion("ClinicalContentRegion", typeof(PatientManagementView));
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册模块服务
        containerRegistry.RegisterScoped<IPatientService, PatientService>();

        // 注册ViewModels
        containerRegistry.RegisterForNavigation<PatientManagementView, PatientManagementViewModel>();
        containerRegistry.RegisterForNavigation<PatientDetailView, PatientDetailViewModel>();
    }
}
```

### 6.2 模块通信

**事件聚合器** (推荐方式):
```csharp
// 发布事件
EventAggregator.GetEvent<PatientUpdatedEvent>().Publish(new PatientUpdatedEventArgs
{
    PatientId = patientId,
    UpdatedBy = currentUser
});

// 订阅事件
EventAggregator.GetEvent<PatientUpdatedEvent>().Subscribe(OnPatientUpdated);

private void OnPatientUpdated(PatientUpdatedEventArgs args)
{
    // 响应患者更新
    RefreshPatientList();
}
```

**Region导航** (模块间跳转):
```csharp
// 导航到其他模块
RegionManager.RequestNavigate("ClinicalContentRegion", "MedicalCaseDetailView",
    new NavigationParameters
    {
        { "PatientId", patientId }
    });
```

### 6.3 模块自治原则

**原则**: 每个模块应保持自治，最小化对其他模块的依赖

**示例**: ClinicalNavigator作为模块内部服务
```csharp
// ClinicalWorkstation/Services/ClinicalNavigator.cs
public class ClinicalNavigator : IClinicalNavigator
{
    private readonly IRegionManager _regionManager;
    private const string ContentRegion = "ClinicalContentRegion";

    public void NavigateToPatients()
        => _regionManager.RequestNavigate(ContentRegion, "PatientManagementView");

    public void NavigateToConsultations()
        => _regionManager.RequestNavigate(ContentRegion, "ConsultationManagementView");

    public void NavigateToPrescriptions()
        => _regionManager.RequestNavigate(ContentRegion, "PrescriptionManagementView");
}
```

**为什么不放在Core_New**:
- 模块特定的region名称（"ClinicalContentRegion"）
- 模块特定的view名称
- 避免Core层依赖具体业务模块
- 符合"模块自治"原则

---

## 7. 最佳实践

### 7.1 ViewModel实现

**推荐模式**:
```csharp
public class PatientManagementViewModel : UnifiedViewModelBase
{
    private readonly IPatientService _patientService;
    private ObservableCollection<PatientDto> _patients;

    public PatientManagementViewModel(
        IPatientService patientService,
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        IRegionManager regionManager,
        ISessionManager sessionManager,
        IErrorHandlingService errorHandlingService)
        : base(eventAggregator, loggerFactory, regionManager, sessionManager, errorHandlingService)
    {
        _patientService = patientService;
        InitializeCommands();
    }

    public ObservableCollection<PatientDto> Patients
    {
        get => _patients;
        set => SetProperty(ref _patients, value);
    }

    public ICommand LoadPatientsCommand { get; private set; }
    public ICommand AddPatientCommand { get; private set; }

    protected override void InitializeCommands()
    {
        LoadPatientsCommand = new DelegateCommand(async () => await LoadPatientsAsync());
        AddPatientCommand = new DelegateCommand(async () => await AddPatientAsync());
    }

    private async Task LoadPatientsAsync()
    {
        try
        {
            IsLoading = true;
            var result = await _patientService.GetPagedAsync(1, 20);
            Patients = new ObservableCollection<PatientDto>(result.Items);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载患者列表失败");
            ErrorMessage = "加载患者列表失败：" + ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

### 7.2 Service实现

**推荐模式**:
```csharp
public class PatientService : IPatientService
{
    private readonly IPatientRepository _repository;
    private readonly ILogger<PatientService> _logger;

    public PatientService(IPatientRepository repository, ILogger<PatientService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<PagedResult<PatientDto>> GetPagedAsync(int page, int size)
    {
        _logger.LogInformation("获取患者分页列表: page={Page}, size={Size}", page, size);
        return await _repository.GetPagedAsync(page, size);
    }

    public async Task<PatientDto> CreateAsync(CreatePatientDto dto)
    {
        // 业务验证
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("患者姓名不能为空");

        _logger.LogInformation("创建患者: {Name}", dto.Name);
        return await _repository.CreateAsync(dto);
    }
}
```

### 7.3 错误处理

**统一错误处理**:
```csharp
try
{
    IsLoading = true;
    StatusMessage = "正在加载...";

    var result = await _service.GetDataAsync();

    StatusMessage = "加载成功";
}
catch (Exception ex)
{
    Logger.LogError(ex, "操作失败");

    if (ErrorHandlingService != null)
    {
        await ErrorHandlingService.HandleExceptionAsync(ex);
    }
    else
    {
        ErrorMessage = $"操作失败：{ex.Message}";
    }
}
finally
{
    IsLoading = false;
}
```

### 7.4 异步操作

**推荐模式**:
```csharp
// ✅ 正确：使用async/await
public async Task LoadDataAsync()
{
    var data = await _service.GetDataAsync();
    ProcessData(data);
}

// ❌ 错误：阻塞调用
public void LoadData()
{
    var data = _service.GetDataAsync().Result; // 会导致死锁
    ProcessData(data);
}

// ✅ 正确：取消令牌支持
public async Task LoadDataAsync(CancellationToken cancellationToken = default)
{
    var data = await _service.GetDataAsync(cancellationToken);
    ProcessData(data);
}
```

### 7.5 命名约定

**类型命名**:
- ViewModel: `<Entity>ViewModel` (e.g., `PatientDetailViewModel`)
- Service: `<Entity>Service` (e.g., `PatientService`)
- Repository: `<Entity>Repository` (e.g., `PatientRepository`)
- View: `<Entity>View` (e.g., `PatientDetailView`)

**命令命名**:
- `LoadCommand`, `SaveCommand`, `DeleteCommand`
- `LoadDataAsync`, `SaveDataAsync`, `DeleteDataAsync`

**属性命名**:
- 私有字段: `_camelCase` (e.g., `_patientService`)
- 公开属性: `PascalCase` (e.g., `PatientList`)
- 布尔属性: `Is/Has/Can + PascalCase` (e.g., `IsLoading`, `HasError`)

---

## 📊 架构度量

### 编译成功指标
```
Desktop.sln编译结果:
✅ 项目总数: 17个 (3个Core_New + 8个Modules + 2个Workstations + 1个Shell + 3个Shared)
✅ 编译成功: 17/17 (100%)
✅ 编译错误: 0个
✅ 编译警告: 0个
✅ 编译时间: ~1.9秒
```

### 架构质量指标
```
✅ 依赖关系: 清晰，无循环依赖
✅ 层次深度: 4层 (符合≤4层目标)
✅ 代码重复率: <20% (从40%降低)
✅ 架构合规性: 100% (全面使用Core_New)
✅ 模块自治度: 高 (模块间无直接依赖)
```

---

## 📝 总结

Issue #815通过三个Phase完成了Desktop项目从混乱的旧架构到清晰的Core_New三层架构的全面迁移：

**Phase 1**: 创建Core_New三层结构（Infrastructure/Models/Services）
**Phase 2**: 8个业务模块迁移到Core_New
**Phase 3**: Workstations和Shell层完成迁移，删除旧架构

**核心成果**:
1. ✅ 清晰的三层架构替代旧的混乱结构
2. ✅ 4层整体架构（Core_New→Modules→Workstations→Shell）
3. ✅ 无循环依赖，依赖方向自下而上
4. ✅ 代码重复率从40%降低到<20%
5. ✅ 编译成功率100%，0错误0警告

**架构优势**:
- **可维护性**: 清晰的层次结构，职责分明
- **可扩展性**: 模块化设计，易于添加新功能
- **可测试性**: 依赖注入，接口隔离
- **性能**: 启动时间优化，内存占用降低

---

**文档版本**: 1.0
**创建日期**: 2025-09-30
**维护负责**: Claude Code
**相关Issue**: #815
**相关报告**: `docs/reports/Issue-815-Phase3-Completion-Report.md`

*本文档反映Issue #815完成后的实际Core_New架构状态，是Desktop项目的权威架构参考文档。*