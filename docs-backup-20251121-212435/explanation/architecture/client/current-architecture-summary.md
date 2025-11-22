# Client端当前架构总结（Phase 2架构）

**文档版本**: v1.0  
**创建日期**: 2025-11-07  
**架构里程碑**: Issue #1114 - Desktop架构模块化重构完成  
**架构版本**: Phase 2 - 完全模块化架构

---

## 📋 架构演进历史

### Phase 1：集中式架构（已废弃）
```
Desktop/
├── Desktop.Services/        # ❌ 单体Service层（已删除）
│   ├── PatientService
│   ├── UserService
│   └── ...（73个文件）
└── Desktop.Infrastructure/
```

**问题**：
- P0性能问题：客户端分页导致10,000条记录全量加载
- 架构不对称：Server模块化 vs Desktop单体
- Service层价值不足：仅做Repository包装（2-5行业务逻辑）

### Phase 2：完全模块化架构（当前架构 - 2025-10）⭐

**里程碑Issue**: [#1114 - Desktop架构模块化重构 - 移除冗余Service层](https://github.com/shouqitao/LYBTZYZS/issues/1114)

**核心变更**:
- ❌ 删除 `Desktop.Services` 整个项目（73文件）
- ✅ 新建 `Desktop.Foundation`（技术基础设施）
- ✅ 新建 `Desktop.Presentation`（UI基础设施）
- ✅ Repository下沉到各业务模块
- ✅ **移除Service层，ViewModel直接调用Repository**

---

## 🏗️ 当前架构全景

### 1. 整体架构图

```
LYBTZYZS Desktop/
│
├── Core/（核心基础设施层）
│   ├── Desktop.Foundation/         # 技术基础设施
│   │   ├── Security/               # IAuthenticationService, ITokenStorageService
│   │   ├── Caching/
│   │   ├── Configuration/
│   │   ├── Http/                   # IApiClient
│   │   ├── Performance/
│   │   ├── HealthCheck/
│   │   └── ...
│   │
│   ├── Desktop.Presentation/       # UI基础设施
│   │   ├── Navigation/
│   │   ├── Notifications/
│   │   ├── Theming/
│   │   └── UserExperience/
│   │
│   ├── Desktop.Infrastructure/     # 基础设施实现
│   ├── Desktop.Models/             # 共享模型
│   └── Desktop.Contracts/          # API契约（DTO）
│
├── Shell/（应用程序Shell）
│   └── Desktop.Shell/              # Prism Shell + 依赖注入配置
│
└── Modules/（业务模块层）
    ├── LYBT.Desktop.Auth/          # 认证模块（特殊：有Services）
    │   ├── Services/               # ConnectionSettingsService
    │   ├── ViewModels/
    │   └── Views/
    │
    ├── LYBT.Desktop.Users/         # 用户模块（标准模式1：CommandHandler）
    │   ├── Interfaces/             # IUserRepository
    │   ├── Repositories/           # UserRepository
    │   ├── ViewModels/
    │   │   ├── UserManagementViewModel.cs
    │   │   └── Components/         # UserCommandHandler
    │   └── Views/
    │
    ├── LYBT.Desktop.Patients/      # 患者模块（标准模式2：直接Repository）
    │   ├── Interfaces/             # IPatientRepository
    │   ├── Repositories/           # PatientRepository
    │   ├── Services/               # PatientSearchManager, PendingQueueManager（辅助服务）
    │   ├── ViewModels/
    │   └── Views/
    │
    ├── LYBT.Desktop.MedicalCase/   # 病历模块
    ├── LYBT.Desktop.Consultation/  # 诊疗模块
    ├── LYBT.Desktop.Prescriptions/ # 处方模块
    ├── LYBT.Desktop.Herbs/         # 中药模块
    └── LYBT.Desktop.Formula/       # 方剂模块
```

### 2. 模块化架构模式

**完全模块化的核心原则**:
1. **Repository下沉**：每个业务模块有独立的Repositories目录
2. **无业务Service层**：ViewModel直接调用Repository
3. **可选CommandHandler**：轻量级命令处理器（如Users模块）
4. **可选辅助Services**：特定业务逻辑服务（如PatientSearchManager）

---

## 📊 三种架构模式对比

### 模式1：ViewModel → CommandHandler → Repository（Users模块）

**适用场景**：
- 多个ViewModel共享相同的Repository调用逻辑
- 需要统一的异常处理和日志记录
- Repository方法调用需要轻量级编排

**示例结构**：
```
LYBT.Desktop.Users/
├── Interfaces/
│   └── IUserRepository.cs
├── Repositories/
│   └── UserRepository.cs
├── ViewModels/
│   ├── UserManagementViewModel.cs       # 注入 UserCommandHandler
│   ├── UserProfileDialogViewModel.cs    # 注入 UserCommandHandler
│   └── Components/
│       └── UserCommandHandler.cs        # 注入 IUserRepository
└── Views/
```

**调用链**：
```csharp
// UserManagementViewModel.cs
public class UserManagementViewModel
{
    private readonly UserCommandHandler _commandHandler;

    public UserManagementViewModel(UserCommandHandler commandHandler)
    {
        _commandHandler = commandHandler;
    }

    private async Task LoadUsersAsync()
    {
        var (success, users, errorMessage) = await _commandHandler.GetPagedAsync(...);
    }
}

// UserCommandHandler.cs
public class UserCommandHandler
{
    private readonly IUserRepository _repository;

    public async Task<(bool success, IEnumerable<UserDto>? users, string? errorMessage)> 
        GetPagedAsync(...)
    {
        try
        {
            var result = await _repository.GetPagedAsync(...);
            return (result.Success, result.Data, result.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户列表失败");
            return (false, null, ex.Message);
        }
    }
}

// UserRepository.cs
public class UserRepository : IUserRepository
{
    private readonly IApiClient _apiClient;

    public async Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(...)
    {
        return await _apiClient.GetAsync<PagedResult<UserDto>>($"/api/users/paged?...");
    }
}
```

**特点**：
- ✅ 多个ViewModel共享CommandHandler逻辑
- ✅ 统一的异常处理和日志记录
- ⚠️ 增加了一层间接调用（但非常轻量级，符合Phase 2精神）

### 模式2：ViewModel → Repository（Patients模块）

**适用场景**：
- ViewModel与Repository 1对1映射
- 不需要共享Repository调用逻辑
- 最简单直接的架构

**示例结构**：
```
LYBT.Desktop.Patients/
├── Interfaces/
│   └── IPatientRepository.cs
├── Repositories/
│   └── PatientRepository.cs
├── ViewModels/
│   └── PatientManagementViewModel.cs    # 直接注入 IPatientRepository
└── Views/
```

**调用链**：
```csharp
// PatientManagementViewModel.cs
public class PatientManagementViewModel
{
    private readonly IPatientRepository _repository;

    public PatientManagementViewModel(IPatientRepository repository)
    {
        _repository = repository;
    }

    private async Task LoadPatientsAsync()
    {
        try
        {
            var result = await _repository.GetPagedAsync(...);
            if (result.Success)
            {
                Patients = new ObservableCollection<PatientDto>(result.Data.Items);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取患者列表失败");
        }
    }
}

// PatientRepository.cs
public class PatientRepository : IPatientRepository
{
    private readonly IApiClient _apiClient;

    public async Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(...)
    {
        // ⭐ Issue #1114修复：使用服务端分页（不是GetAllAsync）
        return await _apiClient.GetAsync<PagedResult<PatientDto>>($"/api/patients/paged?...");
    }
}
```

**特点**：
- ✅ 最简单直接的架构（符合MVP原则）
- ✅ 减少间接调用层级
- ✅ Issue #1114修复：服务端分页，不再全量加载

### 模式3：ViewModel → 辅助Services + Repository（Patients模块增强）

**适用场景**：
- 需要复杂的业务逻辑编排（非CRUD操作）
- 需要跨Repository协调
- 需要特定领域的管理服务

**示例结构**：
```
LYBT.Desktop.Patients/
├── Interfaces/
│   └── IPatientRepository.cs
├── Repositories/
│   └── PatientRepository.cs
├── Services/                            # 辅助服务（非CRUD）
│   ├── PatientSearchManager.cs         # 患者搜索管理
│   ├── PendingQueueManager.cs          # 待诊队列管理
│   └── UnfinishedCaseHandler.cs        # 未完成病历处理
├── ViewModels/
│   └── PatientManagementViewModel.cs   # 注入 IPatientRepository + PatientSearchManager
└── Views/
```

**调用链**：
```csharp
// PatientManagementViewModel.cs
public class PatientManagementViewModel
{
    private readonly IPatientRepository _repository;
    private readonly PatientSearchManager _searchManager;
    private readonly PendingQueueManager _queueManager;

    public PatientManagementViewModel(
        IPatientRepository repository,
        PatientSearchManager searchManager,
        PendingQueueManager queueManager)
    {
        _repository = repository;
        _searchManager = searchManager;
        _queueManager = queueManager;
    }

    // CRUD操作：直接调用Repository
    private async Task LoadPatientsAsync()
    {
        var result = await _repository.GetPagedAsync(...);
    }

    // 复杂搜索：调用辅助服务
    private async Task AdvancedSearchAsync()
    {
        var result = await _searchManager.SearchWithFiltersAsync(...);
    }

    // 队列管理：调用辅助服务
    private async Task ManagePendingQueueAsync()
    {
        await _queueManager.AddToQueueAsync(...);
    }
}

// PatientSearchManager.cs（辅助服务）
public class PatientSearchManager
{
    private readonly IPatientRepository _patientRepository;
    private readonly IMedicalCaseRepository _caseRepository;

    // 复杂搜索逻辑：协调多个Repository
    public async Task<SearchResult> SearchWithFiltersAsync(...)
    {
        // 1. 搜索患者
        var patients = await _patientRepository.SearchAsync(...);
        
        // 2. 查询关联病历
        var cases = await _caseRepository.GetByCriteriaAsync(...);
        
        // 3. 聚合结果
        return new SearchResult { Patients = patients, Cases = cases };
    }
}
```

**特点**：
- ✅ 辅助Services仅处理复杂逻辑（非CRUD）
- ✅ CRUD操作仍直接调用Repository
- ✅ 符合Phase 2精神（不是传统的业务Service层）

---

## 🔑 Foundation层核心服务

### Security服务（认证与安全）

**核心服务接口**：
```csharp
// Desktop.Foundation/Security/

public interface IAuthenticationService
{
    Task<LoginResult> LoginAsync(string username, string password);
    Task<Result> LogoutAsync();
    Task<TokenResponse> RefreshTokenAsync();
    Task<Result> ChangeSysAdminPasswordAsync(ChangeSysAdminPassword dto);
    // ⚠️ 当前缺失：ClearTokensAsync()（Issue #1905需添加）
}

public interface ITokenStorageService
{
    Task<string?> GetAccessTokenAsync();
    Task<string?> GetRefreshTokenAsync();
    Task SaveTokensAsync(string accessToken, string refreshToken);
    // ⚠️ Issue #1905新增：Task ClearTokensAsync();
}

public interface ITokenValidator
{
    Task<bool> ValidateTokenAsync(string token);
    bool IsTokenExpired(string token);
}

public interface ISecureCredentialStorage
{
    Task<string?> GetCredentialAsync(string key);
    Task SaveCredentialAsync(string key, string value);
    Task DeleteCredentialAsync(string key);
}
```

**实现类**：
- `AuthenticationService`：实现IAuthenticationService，调用AuthAPI
- `SecureTokenStorage`：实现ITokenStorageService，使用DPAPI加密
- `LocalTokenValidator`：实现ITokenValidator，本地JWT验证
- `SecureCredentialStorage`：实现ISecureCredentialStorage，Windows CredentialManager

### Presentation服务（UI基础设施）

**核心服务**：
- `INavigationService`：Prism区域导航管理
- `INotificationService`：Toast通知、消息框
- `IThemeManager`：主题切换管理
- `IUserExperienceService`：加载指示器、进度条

---

## 📐 依赖注入配置

### Shell模块注册（Foundation层服务）

```csharp
// Desktop.Shell/App.xaml.cs
protected override void RegisterTypes(IContainerRegistry containerRegistry)
{
    // ⭐ Phase 2：只注册Foundation基础设施服务
    
    // Security服务
    containerRegistry.RegisterSingleton<IAuthenticationService, AuthenticationService>();
    containerRegistry.RegisterSingleton<ITokenStorageService, SecureTokenStorage>();
    containerRegistry.RegisterSingleton<ITokenValidator, LocalTokenValidator>();
    
    // Http客户端
    containerRegistry.RegisterSingleton<IApiClient, ApiClient>();
    
    // Presentation服务
    containerRegistry.RegisterSingleton<INavigationService, NavigationService>();
    containerRegistry.RegisterSingleton<INotificationService, NotificationService>();
    
    // ❌ 不注册业务Repository（由各模块自己注册）
    // ❌ 不注册业务Service（已移除）
}

protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
{
    // ⭐ Phase 2：模块化加载
    moduleCatalog.AddModule<AuthenticationModule>(InitializationMode.WhenAvailable);
    moduleCatalog.AddModule<UsersModule>(InitializationMode.OnDemand);
    moduleCatalog.AddModule<PatientsModule>(InitializationMode.OnDemand);
    moduleCatalog.AddModule<MedicalCaseModule>(InitializationMode.OnDemand);
    moduleCatalog.AddModule<ConsultationModule>(InitializationMode.OnDemand);
    moduleCatalog.AddModule<PrescriptionsModule>(InitializationMode.OnDemand);
    moduleCatalog.AddModule<HerbsModule>(InitializationMode.OnDemand);
    moduleCatalog.AddModule<FormulaModule>(InitializationMode.OnDemand);
}
```

### 业务模块注册（Repository + ViewModel + View）

```csharp
// Modules/LYBT.Desktop.Patients/PatientsModule.cs
[Module(ModuleName = nameof(PatientsModule))]
[ModuleDependency("AuthenticationModule")]
[ModuleDependency("UsersModule")]
public class PatientsModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // ⭐ Phase 2：Repository由模块自己注册
        containerRegistry.RegisterSingleton<IPatientRepository, PatientRepository>();
        
        // ⭐ 辅助服务（可选）
        containerRegistry.RegisterSingleton<PatientSearchManager>();
        containerRegistry.RegisterSingleton<PendingQueueManager>();
        
        // ⭐ ViewModels
        containerRegistry.Register<PatientManagementViewModel>();
        containerRegistry.Register<PatientDetailViewModel>();
        
        // ⭐ Views（用于导航）
        containerRegistry.RegisterForNavigation<PatientManagementView>();
        containerRegistry.RegisterForNavigation<PatientDetailView>();
        
        // ⭐ Dialogs
        containerRegistry.RegisterDialog<QuickCreatePatientDialog, 
                                         QuickCreatePatientDialogViewModel>();
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
        // ⭐ Phase 2：无需启动时初始化
        // ViewModel在导航时自动解析，Repository在构造函数注入时解析
    }
}
```

---

## 🎯 MVVM模式实践

### ViewModel基类（UnifiedViewModelBase）

```csharp
// Desktop.Models/ViewModels/Base/UnifiedViewModelBase.cs
public abstract class UnifiedViewModelBase : BindableBase, INavigationAware, IDisposable
{
    protected ILogger Logger { get; }
    protected IEventAggregator EventAggregator { get; }
    protected IRegionManager RegionManager { get; }
    protected ISessionManager SessionManager { get; }

    // ⭐ Busy状态管理
    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    // ⭐ 错误状态管理
    private bool _hasError;
    public bool HasError
    {
        get => _hasError;
        set => SetProperty(ref _hasError, value);
    }

    // ⭐ 异步命令帮助方法
    protected async Task ExecuteAsync(Func<Task> asyncAction, string? busyMessage = null)
    {
        try
        {
            SetIsBusy(true, busyMessage);
            await asyncAction();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "执行异步操作时发生异常");
            SetError(ex.Message);
        }
        finally
        {
            SetIsBusy(false);
        }
    }
}
```

### ViewModel示例（标准MVVM模式）

```csharp
// Modules/LYBT.Desktop.Users/ViewModels/UserManagementViewModel.cs
public class UserManagementViewModel : UnifiedViewModelBase
{
    private readonly UserCommandHandler _commandHandler;

    // ⭐ 依赖注入（构造函数注入）
    public UserManagementViewModel(
        UserCommandHandler commandHandler,
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        IRegionManager regionManager,
        ISessionManager sessionManager)
        : base(eventAggregator, loggerFactory, regionManager, sessionManager)
    {
        _commandHandler = commandHandler;
        
        // ⭐ 初始化命令
        LoadUsersCommand = new DelegateCommand(async () => await LoadUsersAsync());
        CreateUserCommand = new DelegateCommand(async () => await CreateUserAsync());
    }

    // ⭐ 绑定属性
    private ObservableCollection<UserDto> _users = new();
    public ObservableCollection<UserDto> Users
    {
        get => _users;
        set => SetProperty(ref _users, value);
    }

    // ⭐ 命令
    public DelegateCommand LoadUsersCommand { get; }
    public DelegateCommand CreateUserCommand { get; }

    // ⭐ 异步方法
    private async Task LoadUsersAsync()
    {
        await ExecuteAsync(async () =>
        {
            var (success, users, errorMessage) = await _commandHandler.GetPagedAsync(...);
            if (success && users != null)
            {
                Users = new ObservableCollection<UserDto>(users);
            }
            else
            {
                SetError(errorMessage ?? "加载用户列表失败");
            }
        }, "正在加载用户列表...");
    }
}
```

---

## 📊 架构收益与验证

### Issue #1114的关键修复

**P0性能问题修复**：
- ❌ 修复前：`PatientService.GetPagedAsync` 调用 `GetAllAsync()`（10,000条记录）
- ✅ 修复后：`PatientRepository.GetPagedAsync` 使用服务端分页（20条/页）

**性能提升**：
- 网络流量减少：95%（800KB → 16KB）
- 响应时间提升：25倍（5秒 → 200ms）
- 内存占用减少：90%

### 架构质量指标

**模块化程度**：
- ✅ 8个业务模块独立开发
- ✅ Repository下沉到各模块
- ✅ 无循环依赖（架构测试验证）

**代码质量**：
- ✅ 编译通过（LYBT.All.sln -c Release）：0错误0警告
- ✅ 架构测试通过：DesktopLayerArchTests 100%通过
- ✅ 单元测试通过：Desktop.sln 全部测试通过

**可维护性**：
- ✅ 代码行数减少：20%+（移除Service层冗余代码）
- ✅ 职责清晰：Foundation（技术基础设施）+ Modules（业务逻辑）
- ✅ 依赖方向正确：Modules → Foundation（单向依赖）

---

## 🚨 当前架构的已知限制（与Issue #1905相关）

### 1. Token生命周期管理不完整

**问题描述**：
- ✅ Token生成：完整实现（LoginAsync）
- ✅ Token验证：完整实现（LocalTokenValidator）
- ✅ Token刷新：完整实现（RefreshTokenAsync）
- ❌ **Token主动失效：不完整**（密码修改未触发Token清除）

**影响**：
- 🔴 高危：密码修改后，旧Token仍可使用15分钟（AccessToken）
- 🔴 高危：旧RefreshToken可刷新获得新AccessToken（7天内）

**修复方案**（Issue #1905）：
1. **ITokenStorageService新增方法**：`Task ClearTokensAsync()`
2. **ChangePasswordDialogViewModel修改**：密码修改成功后调用ClearTokensAsync()
3. **AuthService.ChangeSysAdminPasswordAsync修改**：撤销所有RefreshToken

### 2. Foundation层服务接口不完整

**当前Foundation层服务**：
- ✅ IAuthenticationService（认证）
- ✅ ITokenStorageService（Token存储）
- ✅ ITokenValidator（Token验证）
- ❌ **缺失：IUserRepository接口**（用户管理跨模块依赖）

**问题**：
- Users模块的IUserRepository在Users模块内定义
- 其他模块（如Auth模块）需要用户信息时，必须依赖Users模块
- 违反了"Foundation提供跨模块共享服务"的原则

**建议**（后续优化）：
- 考虑将IUserRepository提升到Foundation层（如果多模块需要）
- 或使用事件总线（IEventAggregator）解耦跨模块依赖

---

## 📝 架构决策记录（ADR）

### ADR-005: Desktop完全模块化架构

**决策日期**: 2025-10-09  
**决策状态**: 已接受  
**关联Issue**: #1114

**决策内容**：
1. 删除Desktop.Services整个项目（73文件）
2. 新建Desktop.Foundation（技术基础设施）
3. 新建Desktop.Presentation（UI基础设施）
4. Repository下沉到各业务模块
5. 移除Service层，ViewModel直接调用Repository

**决策理由**：
- P0性能问题：服务端分页 vs 客户端分页
- 架构对称性：Server模块化 → Desktop模块化
- Service层价值不足：平均仅2-5行业务逻辑

**预期收益**：
- 短期：网络流量减少50%+，响应速度提升10x-25x
- 长期：开发效率提升30%，维护成本降低40%

**实施状态**: ✅ 已完成（2025-10-10）

---

## 🔗 相关文档

### 架构文档
- [Client端架构详细设计](./README.md)（完整版，26000+ tokens）
- [Client端认证架构设计](./auth-design.md)
- [用户个人资料修改设计](./user-profile-modification-design.md)

### Issue与决策
- [Issue #1114: Desktop架构模块化重构](https://github.com/shouqitao/LYBTZYZS/issues/1114)
- [Issue #1905: 密码修改后Token未清除](https://github.com/shouqitao/LYBTZYZS/issues/1905)
- [ADR-005: Desktop完全模块化架构](../decisions/ADR-005-desktop-modular-architecture.md)

### 实施报告
- [Desktop模块化架构决策深度分析](../../reports/desktop-modular-architecture-decision.md)（25步UltraThink分析）

---

**文档状态**: ✅ 已完成  
**维护责任**: 架构组  
**下次更新**: Issue #1905修复后（添加ClearTokensAsync方法）
