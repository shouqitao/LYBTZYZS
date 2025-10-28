# Client端架构指南

**版本**：v5.1 Phase 4优化版
**更新时间**：2025-10-28
**对应代码层**：LYBT.Desktop  

## 🏗️ Client端WPF架构设计

凌隐宝堂中医诊所Client端采用\*\*WPF MVVM架构\*\*，严格遵循分层解耦原则。

### ⚠️ 架构演化说明（Phase 2）

**重要变更**：基于Issue #1114，Client端架构已从五层演化到四层架构：

| 架构版本 | 层次结构 | ViewModel依赖 | 实施时间 |
|---------|---------|--------------|----------|
| **Phase 1**（已废弃） | Shell → Core → **Services** → Infrastructure → Modules | ViewModel → Service → Repository | 2024年初 |
| **Phase 2**（当前） | Shell → Core → Infrastructure → Modules | ViewModel → **直接使用Repository** | 2024年Q2 |

**变更原因**：
- ✅ **简化架构**：去除中间Service层，减少抽象层级
- ✅ **提升性能**：减少一层调用，降低内存开销
- ✅ **代码精简**：避免Service层与Repository的重复逻辑
- ✅ **对齐Server**：与Server端保持一致的分层架构风格

**实际代码证据**（PatientDetailViewModel.cs:17-18）：
```csharp
/// <summary>
/// 患者详情视图模型 - Phase 2模块化架构
/// Issue #1114 - 直接使用Repository，去除Service层
/// </summary>
public class PatientDetailViewModel : UnifiedViewModelBase
{
    private readonly IPatientRepository _patientRepository;  // ⭐ 直接注入Repository
    // ...
}
```

### ⚠️ 架构优化说明（Phase 4 - Epic #1676）

**重要变更**：基于Epic #1676，进一步优化Desktop层架构，移除临时Service层遗留：

| 优化项目 | Phase 4前 | Phase 4后 | 实施时间 |
|---------|----------|----------|----------|
| **MedicalCase查询** | MedicalCaseQueryService（临时） | IMedicalCaseRepository | 2025-10-28 |
| **Patient查询** | PatientSelectionViewModel独立查询 | IMedicalCaseRepository统一查询 | 2025-10-28 |
| **依赖关系** | Patients ↔ MedicalCase循环依赖 | Patients → MedicalCase（单向） | 2025-10-28 |

**变更原因**：
- ✅ **彻底移除临时Service**：MedicalCaseQueryService是Phase 2遗留的临时方案，已完成历史使命
- ✅ **统一架构模式**：所有ViewModel统一使用Repository模式，无例外
- ✅ **解除循环依赖**：Prism模块依赖仅需运行时加载顺序，无需编译时项目引用
- ✅ **API能力对齐**：Desktop端与Server端API完全对齐（GetUnfinishedCase、CloseCase）

**实际代码证据**（PatientSelectionViewModel.cs）：
```csharp
// Epic #1676 Phase 4 Task 4.4 - 使用Repository替代临时Service
private readonly IMedicalCaseRepository _medicalCaseRepository;

public PatientSelectionViewModel(
    IRegionManager regionManager,
    IPatientRepository patientRepository,
    IMedicalCaseRepository medicalCaseRepository)  // ⭐ 注入Repository
{
    _regionManager = regionManager;
    _patientRepository = patientRepository;
    _medicalCaseRepository = medicalCaseRepository;  // ⭐ 替代IMedicalCaseQueryService
}
```

### 📐 当前架构（Phase 4优化）

```
LYBT.Desktop (WPF应用) - 四层架构
├── Shell层           # 主程序入口、窗口容器
├── Core层            # 核心基础设施、DI容器、事件聚合
├── Infrastructure层  # 外部依赖、HTTP客户端、本地存储
└── Modules层         # 业务模块、MVVM组件
    ├── ViewModels/   # ⭐ 直接依赖Repository（Phase 2变更）
    ├── Views/        # XAML视图
    ├── Models/       # 数据模型
    ├── Repositories/ # 数据访问（HTTP API调用）
    └── Interfaces/   # 接口定义
```

**核心数据流**（Phase 2/4 - Issue #1114, Epic #1676）：

**主流模式**（90%场景）：
```
User Interaction (View)
    ↓
ViewModel (Command + INotifyPropertyChanged)
    ↓
Repository (封装Refit API接口) ⭐ 直接调用，无中间业务Service层
    ↓
HTTP Request (Refit自动生成)
    ↓
Server API (REST Endpoint)
```

**替代模式**（10%场景 - Issue #1606标记）：
```
User Interaction (View)
    ↓
ViewModel (Command + INotifyPropertyChanged)
    ↓
Refit API接口 (IMedicalCaseApi等) ⭐ 直接注入，绕过Repository
    ↓
HTTP Request (Refit自动生成)
    ↓
Server API (REST Endpoint)
```

> **⚠️ 架构演进说明**：
> - **Phase 1**（已废弃）：ViewModel → Service → Repository → API
> - **Phase 2/4**（当前）：ViewModel → Repository → API （移除业务Service层）
> - **未来统一**：将10%直接API注入迁移到Repository模式（Epic #1606）

---

## 📐 架构层次详解

### 1. Shell层 - 应用程序容器

> **📚 详细设计文档**：[Shell层架构设计](shell-layer-design.md) - 职责边界、组件结构、交互模式、禁止模式

**职责**：应用程序启动、窗口管理、主题配置

**核心组件**：
- `App.xaml` - WPF应用程序入口
- `MainWindow.xaml` - 主窗口容器
- `MainWindowViewModel.cs` - 主窗口视图模型

**代码示例**（真实代码：Shell/App.xaml.cs）：

```csharp
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Modularity;

/// <summary>
/// 应用程序主入口 - WPF应用程序核心启动器
/// 集成Prism.DryIoc容器管理,支持7个业务模块的统一协调
/// Issue #1239: 修复 Prism 生命周期 - 同步调用 base.OnStartup
/// Issue #1221: 延迟显示主窗口，先显示 Splash Screen
/// </summary>
public partial class App : PrismApplication    // ⭐ 继承PrismApplication（不是Application）
{
    /// <summary>
    /// 应用程序启动入口
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        // 1. 显示 Splash Screen
        var splashScreen = new SplashScreenWindow();
        splashScreen.Show();

        // 2. ⭐ 同步调用 base.OnStartup（触发 Prism 生命周期）
        // Prism 会依次调用：CreateShell → InitializeShell → OnInitialized
        base.OnStartup(e);
    }

    /// <summary>
    /// 创建应用程序主窗体（Prism生命周期方法）
    /// </summary>
    protected override Window CreateShell()
    {
        // ⭐ 使用Prism容器解析主窗口（不是ASP.NET Core的IServiceProvider）
        var mainWindow = Container.Resolve<MainWindow>();
        return mainWindow;
    }

    /// <summary>
    /// 注册应用程序类型和服务（Prism生命周期方法）
    /// ⚠️ Phase 2架构：只注册Infrastructure服务，业务Repository由各模块自己注册
    /// </summary>
    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // ⭐ 使用扩展方法统一注册所有服务（参见Extensions/ServiceCollectionExtensions.cs）
        containerRegistry.RegisterAllServices();

        // 显式注册 ViewModels（Prism 8.x 要求）
        containerRegistry.Register<MainWindowViewModel>();
    }

    /// <summary>
    /// 配置模块目录（Prism生命周期方法）
    /// Issue #1553: 角色驱动模块加载
    /// </summary>
    protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
    {
        // ⭐ 注册7个业务模块（按依赖顺序）
        moduleCatalog.AddModule<AuthenticationModule>();          // 认证模块（基础）
        moduleCatalog.AddModule<UsersModule>();                   // 用户模块
        moduleCatalog.AddModule<PatientsModule>();                // 患者模块
        moduleCatalog.AddModule<MedicalCaseModule>();             // 病案模块
        moduleCatalog.AddModule<ConsultationModule>();            // 诊断模块
        moduleCatalog.AddModule<PrescriptionsModule>();           // 处方模块
        moduleCatalog.AddModule<HerbsModule>();                   // 中药模块
    }
}
```

**Prism应用启动特征**：
- ✅ **PrismApplication基类**：继承Prism.DryIoc.PrismApplication（不是WPF的Application）
- ✅ **Prism生命周期方法**：OnStartup → CreateShell → RegisterTypes → ConfigureModuleCatalog → OnInitialized
- ✅ **Container.Resolve**：使用Prism容器解析（不是ASP.NET Core的IServiceProvider.GetRequiredService）
- ✅ **IContainerRegistry**：Prism DI容器注册接口（不是IServiceCollection）
- ✅ **模块化架构**：ConfigureModuleCatalog注册7个业务模块（Prism Modularity）
- ❌ **无Host.CreateDefaultBuilder**：Prism不使用ASP.NET Core的Host

### 2. Core层 - 核心基础设施
**职责**：依赖注入、事件聚合、导航服务、共享工具

**核心组件**：
- `IocContainer.cs` - IoC容器配置
- `EventAggregator.cs` - 事件聚合器
- `NavigationService.cs` - 导航服务
- `ViewModelBase.cs` - 视图模型基类

**代码示例**：
```csharp
// Core/ViewModelBase.cs
public abstract class ViewModelBase : ObservableObject, IDisposable
{
    private readonly IEventAggregator _eventAggregator;
    
    protected ViewModelBase(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
    }
    
    protected void Publish<T>(T eventData) where T : class
    {
        _eventAggregator.Publish(eventData);
    }
    
    protected void Subscribe<T>(Action<T> handler) where T : class
    {
        _eventAggregator.Subscribe(handler);
    }
    
    public abstract string Title { get; }
    
    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }
}
```

### 3. ~~Services层 - 业务服务~~ （已废弃 - Phase 2/4）

> **⚠️ 架构演进**：Phase 2（Issue #1114）移除了业务Service层，Phase 4（Epic #1676）清理了临时Service遗留。

**历史状态**（Phase 1，已废弃）：
- ViewModel → Service → Repository（三层调用）
- 存在中间Service层处理业务逻辑

**当前状态**（Phase 4）：
- ViewModel → Repository（直接调用）
- **已移除的临时Service**：
  - `MedicalCaseQueryService` - Epic #1676 Phase 4移除，改用IMedicalCaseRepository
  - 其他业务Service - Phase 2已全部移除

**保留的Infrastructure服务**（非业务Service，仍然存在）：

**基础设施服务**：
- `ITokenService`, `ITokenStorageService` - JWT令牌管理
- `ISecurityService`, `IAuthenticationService` - 认证与安全
- `ICacheService`, `IConfigurationService` - 缓存与配置
- `IApiHealthCheckService` - API健康检查

**UI基础设施服务**（Prism框架）：
- `INavigationService` - 页面导航服务
- `IDialogService` - 对话框服务
- `INotificationService` - 用户通知服务
- `IEventAggregator` - 事件聚合器

> **⚠️ 区分要点**：Infrastructure服务是**通用技术能力**（如认证、缓存、导航），而非**业务逻辑封装**（如患者管理、病案管理）。

**架构决策理由**：
- ✅ **避免过度抽象**：Service层与Repository逻辑高度重复
- ✅ **提升性能**：减少一层调用开销
- ✅ **对齐Server**：与Server端Repository模式保持一致
- ✅ **MVP原则**：够用即好，避免不必要的中间层

### 4. Infrastructure层 - 基础设施
**职责**：数据持久化、外部服务集成、工具类

**核心组件**：
- `TokenService.cs` - 令牌管理服务
- `CacheService.cs` - 缓存服务
- `StorageService.cs` - 本地存储服务
- `HttpMessageHandlers.cs` - HTTP消息处理器

**代码示例**：
```csharp
// Infrastructure/TokenService.cs
public class TokenService : ITokenService
{
    private readonly ISecureStorage _secureStorage;
    private const string AccessTokenKey = "access_token";
    private const string RefreshTokenKey = "refresh_token";
    
    public TokenService(ISecureStorage secureStorage)
    {
        _secureStorage = secureStorage;
    }
    
    public async Task<string> GetAccessTokenAsync()
    {
        return await _secureStorage.GetAsync(AccessTokenKey);
    }
    
    public async Task<string> GetRefreshTokenAsync()
    {
        return await _secureStorage.GetAsync(RefreshTokenKey);
    }
    
    public async Task SaveTokensAsync(string accessToken, string refreshToken)
    {
        await _secureStorage.SetAsync(AccessTokenKey, accessToken);
        await _secureStorage.SetAsync(RefreshTokenKey, refreshToken);
    }
    
    public async Task ClearTokensAsync()
    {
        await _secureStorage.RemoveAsync(AccessTokenKey);
        await _secureStorage.RemoveAsync(RefreshTokenKey);
    }
}
```

### 5. Modules层 - 业务模块
**职责**：UI界面、业务逻辑、模块化组件

**模块结构**（Phase 2/4 - Repository模式）：
```
Modules/
├── Auth/                    # 认证模块
│   ├── Views/              # 视图（XAML）
│   ├── ViewModels/         # 视图模型（⭐ 直接依赖Repository）
│   ├── Models/             # 数据模型
│   ├── Repositories/       # 数据访问层（封装Refit API）⭐ Phase 2新增
│   ├── Interfaces/         # 接口定义（IXxxRepository）
│   └── XxxModule.cs        # Prism模块注册
├── Users/                  # 用户管理模块（结构同上）
├── Patients/               # 患者管理模块（结构同上）
├── MedicalCase/            # 医案管理模块（结构同上）
├── Consultation/           # 诊疗模块（无独立Repository，使用MedicalCaseRepository）
├── Prescriptions/          # 处方模块（无独立Repository，使用MedicalCaseRepository）
├── Herbs/                  # 药材模块（结构同上）
└── Formula/                # 验方模块（结构同上）
```

**关键变更**（Phase 2 - Issue #1114）：
- ✅ **新增Repositories/**：各模块独立封装Refit API调用逻辑
- ✅ **ViewModel直接依赖**：移除中间Service层，ViewModel直接注入IRepository
- ❌ **移除Services/**：业务Service层已废弃（Infrastructure服务除外）

#### 📦 Repository层 - 数据访问层（Phase 2核心）

> **⚠️ 架构要点**：Repository封装Refit API接口，提供统一的数据访问抽象，ViewModel不直接依赖HTTP细节。

**职责**：
- ✅ **API调用封装**：通过Refit自动生成HTTP请求
- ✅ **异常处理**：统一捕获API异常并记录日志
- ✅ **数据转换**：DTO与Model之间的映射（如需要）
- ✅ **聚合根边界维护**：如MedicalCaseRepository管理Consultation/Prescription关联操作

**核心设计模式**：

**1. RepositoryBase统一基类**（Project Standardization 3.0）：

```csharp
/// <summary>
/// Repository基类 - 提供CRUD操作的统一实现
/// Project Standardization 3.0 - 所有Repository继承此基类
/// </summary>
public abstract class RepositoryBase<TDto, TCreateDto, TUpdateDto, TApi>
    where TDto : class
    where TCreateDto : class
    where TUpdateDto : class
{
    protected readonly TApi _api;
    protected readonly ILogger _logger;

    protected RepositoryBase(TApi api, ILogger logger)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ⭐ 统一CRUD方法（子类只需实现API调用）
    public virtual async Task<TDto> CreateAsync(TCreateDto createDto)
    {
        try
        {
            var response = await CallApiCreateAsync(createDto);
            return response.Data ?? throw new InvalidOperationException("创建失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建实体失败");
            throw;
        }
    }

    // 其他CRUD方法：GetByIdAsync, GetPagedAsync, UpdateAsync, DeleteAsync...

    // ⭐ 子类需实现的抽象方法（API调用）
    protected abstract Task<ApiResponse<TDto>> CallApiCreateAsync(TCreateDto createDto);
    protected abstract Task<ApiResponse<TDto>> CallApiGetByIdAsync(Guid id);
    // ...其他抽象方法
}
```

**2. 具体Repository实现**（PatientRepository示例）：

```csharp
/// <summary>
/// 患者数据仓储实现 - RepositoryBase统一架构
/// Project Standardization 3.0 - 迁移到统一RepositoryBase
/// </summary>
public class PatientRepository : RepositoryBase<PatientDto, PatientCreateDto, PatientUpdateDto, IPatientApi>, IPatientRepository
{
    public PatientRepository(
        IPatientApi patientApi,  // ⭐ Refit API接口注入
        ILogger<PatientRepository> logger)
        : base(patientApi, logger)
    {
    }

    // ⭐ 实现RepositoryBase的抽象方法
    protected override Task<ApiResponse<PatientDto>> CallApiCreateAsync(PatientCreateDto createDto)
    {
        return _api.CreatePatientAsync(createDto);
    }

    protected override Task<ApiResponse<PatientDto>> CallApiGetByIdAsync(Guid id)
    {
        return _api.GetPatientByIdAsync(id);
    }

    // ⭐ 业务特定方法（超出标准CRUD）
    public async Task<List<PatientDto>> GetAllAsync()
    {
        try
        {
            var pagedResult = await GetPagedAsync(1, 10000);
            return pagedResult.Items ?? new List<PatientDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有患者失败");
            return new List<PatientDto>();
        }
    }
}
```

**3. 聚合根Repository**（MedicalCaseRepository示例 - DDD边界维护）：

```csharp
/// <summary>
/// 医案仓储 - 聚合根边界管理
/// Issue #1563, #1589 - 维护MedicalCase → Consultation/Prescription聚合根边界
/// </summary>
public class MedicalCaseRepository : RepositoryBase<MedicalCaseDto, MedicalCaseCreateDto, MedicalCaseUpdateDto, IMedicalCaseApi>, IMedicalCaseRepository
{
    // ⭐ 聚合根方法：更新诊断记录（保持聚合根边界）
    public async Task<ConsultationDto> UpdateConsultationAsync(Guid medicalCaseId, ConsultationUpdateDto dto)
    {
        if (medicalCaseId == Guid.Empty)
            throw new ArgumentException("医案ID不能为空", nameof(medicalCaseId));

        try
        {
            // ⭐ 通过聚合根API端点操作子实体
            var response = await _api.UpdateConsultationAsync(medicalCaseId, dto);
            return response.Data ?? throw new InvalidOperationException("更新诊断信息失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新医案诊断信息失败，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
            throw;
        }
    }

    // ⭐ 聚合根方法：创建医案（包含完整关联数据）
    public async Task<MedicalCaseDto> CreateWithDetailsAsync(MedicalCaseCreateDto dto)
    {
        // 一次性创建医案+诊断+处方（保证数据一致性）
        var response = await _api.CreateWithDetailsAsync(dto);
        return response.Data ?? throw new InvalidOperationException("创建医案失败");
    }
}
```

**Repository架构特征总结**：
- ✅ **RepositoryBase基类**：统一CRUD操作，减少重复代码
- ✅ **Refit API注入**：Repository依赖IXxxApi接口，Refit自动生成HTTP请求
- ✅ **异常日志统一**：所有API异常都在Repository层捕获并记录
- ✅ **聚合根边界**：MedicalCaseRepository管理Consultation/Prescription子实体操作
- ✅ **模块独立注册**：每个模块在XxxModule.cs中注册自己的Repository
- ❌ **无业务逻辑**：Repository只负责数据访问，不包含业务规则（业务规则在ViewModel）

#### 📋 Prescriptions模块架构演化（Issue #1445）

**重要变更**：处方模块视图架构已于2025-10-18统一，删除了Phase 4B空骨架实现。

| 演化阶段 | 视图实现 | 状态 | 问题 |
|---------|---------|------|------|
| **Phase 4B**（已废弃） | PrescriptionView（434行空骨架） | 2025-10-18删除 | 导航错误导致空白界面 |
| **统一架构**（当前） | PrescriptionView（932行完整实现） | 当前使用 | 重命名自PrescriptionComposerView |

**架构清理过程**（Epic #1445）：
- ✅ **ARCH-1** (#1446): 删除Phase 4B空骨架（PrescriptionView.xaml/cs/ViewModel）
- ✅ **ARCH-2** (#1447): 重命名PrescriptionComposerView → PrescriptionView
- ✅ **ARCH-3** (#1448): 更新所有导航配置引用
- 🔄 **ARCH-4** (#1449): 更新架构文档（本文档）

**当前Prescriptions视图结构**：
```
Modules/Prescriptions/
├── Views/
│   ├── PrescriptionView.xaml          # 处方编辑主界面（8列DataGrid，完整实现）
│   ├── PrescriptionManagementView.xaml # 处方列表管理界面
│   ├── PrescriptionDetailView.xaml    # 处方详情查看界面
│   └── FormulaTemplateSelectionDialog.xaml  # 验方模板选择对话框
├── ViewModels/
│   ├── PrescriptionViewModel.cs        # 处方编辑ViewModel（包含组件化架构）
│   ├── PrescriptionManagementViewModel.cs
│   ├── PrescriptionsMainViewModel.cs
│   └── FormulaTemplateSelectionDialogViewModel.cs
└── Components/                         # 组件化设计（PrescriptionViewModel依赖）
    ├── PrescriptionDataManager.cs      # 数据管理组件（Issue #1551: 添加PrescriptionNumber管理）
    ├── PrescriptionCommandHandler.cs   # 命令处理组件
    └── FormulaImportService.cs         # 验方导入服务
```

**导航配置**：
- 创建新处方：`NavigateTo("MainRegion", "PrescriptionView")`
- 编辑处方：`NavigateTo("MainRegion", "PrescriptionView", parameters)`
- 管理列表：`NavigateTo("PrescriptionContentRegion", "PrescriptionManagementView")`

**Prism模块标准实现**（真实代码：Modules/LYBT.Desktop.Patients/PatientsModule.cs）：

```csharp
using Prism.Ioc;
using Prism.Modularity;

/// <summary>
/// 患者管理模块 - Phase 2架构
/// Issue #1114: 移除业务Service层，ViewModel直接调用Repository
/// Issue #1487: 快速创建患者对话框
/// Issue #1547: 增强的患者数据导入导出功能
/// Issue #1557: 患者详情视图增强
/// </summary>
[Module(ModuleName = nameof(PatientsModule))]
[ModuleDependency("AuthenticationModule")]    // 依赖认证模块
[ModuleDependency("UsersModule")]             // 依赖用户模块
public class PatientsModule : IModule
{
    /// <summary>
    /// 注册模块类型到容器
    /// ⚠️ Phase 2架构：只注册Repository、ViewModel、View，无业务Service
    /// </summary>
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // ⭐ Phase 2：Repository由模块自己注册（不在Shell统一注册）
        containerRegistry.RegisterSingleton<IPatientRepository, PatientRepository>();

        // ⭐ 注册视图模型 - MVP核心功能
        containerRegistry.Register<ViewModels.PatientDetailViewModel>();          // Issue #1557
        containerRegistry.Register<ViewModels.PatientImportWizardViewModel>();    // Issue #1547
        containerRegistry.Register<ViewModels.PatientExportWizardViewModel>();

        // ⭐ 注册视图用于导航（Prism区域导航）
        containerRegistry.RegisterForNavigation<Views.PatientDetailView>();
        containerRegistry.RegisterForNavigation<Views.PatientManagementView>();

        // ⭐ Issue #1487: 快速创建患者对话框（Prism对话框服务）
        containerRegistry.RegisterDialog<Views.QuickCreatePatientDialog,
                                         ViewModels.QuickCreatePatientDialogViewModel>();
    }

    /// <summary>
    /// 模块初始化（应用启动后调用）
    /// Phase 2简化：无需启动时初始化，所有逻辑通过依赖注入延迟加载
    /// </summary>
    public void OnInitialized(IContainerProvider containerProvider)
    {
        // Phase 2架构：无需启动时初始化
        // ViewModel在导航时自动解析，Repository在ViewModel构造函数注入时解析
    }
}
```

**Prism模块关键特性**：
- ✅ **IModule接口**：Prism标准模块接口（无需自定义基类）
- ✅ **[Module]属性**：标记模块名称，用于模块发现和加载
- ✅ **[ModuleDependency]属性**：声明模块依赖关系，确保加载顺序
- ✅ **RegisterTypes方法**：注册模块内部类型（Repository、ViewModel、View）
- ✅ **OnInitialized方法**：模块启动后初始化（Phase 2架构为空）
- ✅ **IContainerRegistry**：Prism DI容器注册接口（不是ASP.NET Core的IServiceCollection）
- ❌ **无ModuleBase基类**：Prism不需要自定义基类，直接实现IModule即可
- ❌ **无业务Service注册**：Phase 2架构移除业务Service层

## 🎯 MVVM架构模式

### Model - 数据模型
**职责**：业务实体、数据验证、状态管理

```csharp
// Modules/Patients/Models/PatientModel.cs
public class PatientModel : ObservableObject
{
    private int _id;
    public int Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }
    
    private string _name;
    [Required(ErrorMessage = "患者姓名不能为空")]
    [StringLength(50, ErrorMessage = "姓名长度不能超过50个字符")]
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }
    
    private DateTime _birthDate;
    public DateTime BirthDate
    {
        get => _birthDate;
        set => SetProperty(ref _birthDate, value);
    }
    
    private string _phone;
    [Phone(ErrorMessage = "手机号格式不正确")]
    public string Phone
    {
        get => _phone;
        set => SetProperty(ref _phone, value);
    }
    
    private string _address;
    public string Address
    {
        get => _address;
        set => SetProperty(ref _address, value);
    }
    
    public int Age => DateTime.Today.Year - BirthDate.Year;
}
```

### View - 用户界面
**职责**：界面布局、用户交互、数据绑定

```xml
<!-- Modules/Patients/Views/PatientManagementView.xaml -->
<UserControl x:Class="LYBT.Desktop.Modules.Patients.Views.PatientManagementView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             mc:Ignorable="d"
             d:DesignHeight="600" d:DesignWidth="800">
    
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>
        
        <!-- 工具栏 -->
        <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="10">
            <Button Content="新增患者" Command="{Binding AddPatientCommand}" 
                    Style="{StaticResource PrimaryButtonStyle}" Margin="0,0,10,0"/>
            <Button Content="编辑患者" Command="{Binding EditPatientCommand}" 
                    Style="{StaticResource SecondaryButtonStyle}" Margin="0,0,10,0"/>
            <Button Content="删除患者" Command="{Binding DeletePatientCommand}" 
                    Style="{StaticResource DangerButtonStyle}" Margin="0,0,10,0"/>
        </StackPanel>
        
        <!-- 搜索栏 -->
        <Grid Grid.Row="1" Margin="10,5">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            <TextBox Grid.Column="0" Text="{Binding SearchKeyword, UpdateSourceTrigger=PropertyChanged}"
                     PlaceholderText="输入患者姓名或手机号搜索"/>
            <Button Grid.Column="1" Content="搜索" Command="{Binding SearchCommand}" 
                    Style="{StaticResource PrimaryButtonStyle}" Margin="5,0,0,0"/>
        </Grid>
        
        <!-- 患者列表 -->
        <DataGrid Grid.Row="2" ItemsSource="{Binding Patients}" 
                  SelectedItem="{Binding SelectedPatient}"
                  AutoGenerateColumns="False" CanUserAddRows="False" Margin="10">
            <DataGrid.Columns>
                <DataGridTextColumn Header="姓名" Binding="{Binding Name}" Width="*"/>
                <DataGridTextColumn Header="年龄" Binding="{Binding Age}" Width="80"/>
                <DataGridTextColumn Header="手机号" Binding="{Binding Phone}" Width="120"/>
                <DataGridTextColumn Header="地址" Binding="{Binding Address}" Width="*"/>
                <DataGridTextColumn Header="创建时间" Binding="{Binding CreatedAt, StringFormat='yyyy-MM-dd HH:mm'}" Width="150"/>
            </DataGrid.Columns>
        </DataGrid>
    </Grid>
</UserControl>
```

### ViewModel - 视图模型
**职责**：UI逻辑、命令处理、数据转换、状态管理

> **⚠️ 架构要点**：ViewModel **直接注入Repository**，无中间Service层（Phase 2/4架构）

**真实示例**：`QuickCreatePatientDialogViewModel.cs`（Issue #1487）

```csharp
public class QuickCreatePatientDialogViewModel : UnifiedViewModelBase, IDialogAware
{
    #region 服务依赖

    // ⭐ 直接注入Repository，无中间Service层
    private readonly IPatientRepository _patientRepository;

    #endregion

    #region 数据属性

    private string _name = string.Empty;
    private bool _isMale = true;
    private int _age;
    private string _phoneNumber = string.Empty;

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                UpdateCommandStates();
            }
        }
    }

    public int Age
    {
        get => _age;
        set
        {
            if (SetProperty(ref _age, value))
            {
                UpdateCommandStates();
            }
        }
    }

    public Gender Gender => IsMale ? Gender.Male : (IsFemale ? Gender.Female : Gender.Unknown);

    #endregion

    #region 命令

    public DelegateCommand SaveCommand { get; }
    public DelegateCommand CancelCommand { get; }

    #endregion

    #region 构造函数

    // ⭐ 构造函数注入：Repository + 基础设施服务
    public QuickCreatePatientDialogViewModel(
        IPatientRepository patientRepository,  // 业务数据访问
        IEventAggregator eventAggregator,      // Prism事件聚合器
        ILoggerFactory loggerFactory,           // 日志工厂
        IRegionManager regionManager)           // Prism区域管理器
        : base(eventAggregator, loggerFactory, regionManager)
    {
        _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));

        // 初始化命令
        SaveCommand = new DelegateCommand(async () => await SaveAsync(), CanSave);
        CancelCommand = new DelegateCommand(Cancel);
    }

    #endregion

    #region 命令实现

    /// <summary>
    /// 保存患者信息 - Issue #1487
    /// 数据流：ViewModel → Repository → Refit API → Server
    /// </summary>
    private async Task SaveAsync()
    {
        try
        {
            // 验证表单
            if (!ValidateForm(out string errorMessage))
            {
                ShowErrorMessage(errorMessage);
                return;
            }

            SetIsBusy(true, "正在保存...");

            // 根据年龄推算出生日期
            var birthDate = DateTime.Today.AddYears(-Age);

            // ⭐ 创建DTO - ViewModel负责数据准备
            var createDto = new PatientCreateDto
            {
                Name = Name.Trim(),
                Gender = Gender,
                BirthDate = birthDate,
                PhoneNumber = PhoneNumber.Trim(),
                Status = CommonStatus.Enabled
            };

            // ⭐ 调用Repository - Repository负责HTTP通信
            var newPatient = await _patientRepository.CreateAsync(createDto);

            Logger.LogInformation("快速创建患者成功: {PatientName} (ID: {PatientId})",
                newPatient.Name, newPatient.Id);

            // 通过对话框参数返回新创建的患者
            var parameters = new DialogParameters
            {
                { "NewPatient", newPatient }
            };

            RequestClose?.Invoke(new DialogResult(ButtonResult.OK, parameters));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "创建患者失败: {PatientName}", Name);
            await ShowErrorMessageAsync("保存失败，请稍后重试");
        }
        finally
        {
            SetIsBusy(false);
        }
    }

    private void Cancel()
    {
        RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
    }

    #endregion

    #region 验证逻辑

    private bool ValidateForm(out string errorMessage)
    {
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Name))
        {
            errorMessage = "请输入患者姓名";
            return false;
        }

        if (Age <= 0 || Age > 150)
        {
            errorMessage = "请输入有效的年龄（1-150）";
            return false;
        }

        if (string.IsNullOrWhiteSpace(PhoneNumber) || PhoneNumber.Trim().Length != 11)
        {
            errorMessage = "请输入正确的11位手机号码";
            return false;
        }

        return true;
    }

    private bool CanSave()
    {
        return !string.IsNullOrWhiteSpace(Name) &&
               Gender != Gender.Unknown &&
               Age > 0 &&
               !string.IsNullOrWhiteSpace(PhoneNumber) &&
               !IsBusy;
    }

    #endregion
}
```

**架构特征总结**：
- ✅ **依赖注入**：构造函数注入`IPatientRepository`（业务）+ 基础设施服务（Prism/Logging）
- ✅ **DTO模式**：ViewModel创建`PatientCreateDto`，Repository返回`PatientDto`
- ✅ **异步模式**：所有Repository调用都是`async/await`
- ✅ **错误处理**：ViewModel层处理异常并显示用户友好提示
- ✅ **状态管理**：`IsBusy`状态由ViewModel管理，UI自动响应
- ❌ **无Service层**：直接`ViewModel → Repository`，无中间业务Service

## 🔧 依赖注入配置

> **⚠️ 架构要点**：Phase 2/4架构移除业务Service层，ViewModel直接调用Repository + Infrastructure Service

### 真实注册架构（三处分离注册）

**1. Refit API接口注册**（Shell/Extensions/ServiceCollectionExtensions.cs）：

```csharp
/// <summary>
/// 注册HTTP相关服务
/// Issue #1239 修复: 使用延迟解析注册 Refit 客户端（避免在注册阶段解析 HttpClient）
/// </summary>
private static void RegisterHttpServices(IContainerRegistry containerRegistry, IConfiguration config)
{
    // 配置HttpClient（带Authorization header）
    var apiBaseUrl = config["ApiSettings:BaseUrl"] ?? "https://localhost:5001";
    containerRegistry.RegisterSingleton<HttpClient>(resolver =>
    {
        var authHandler = resolver.Resolve<AuthorizationMessageHandler>();
        authHandler.InnerHandler = new HttpClientHandler();
        return new HttpClient(authHandler)
        {
            BaseAddress = new Uri(apiBaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    });

    // ⭐ Refit API接口注册（8个模块API）
    containerRegistry.RegisterSingleton<IAuthApi>(resolver =>
        RestService.For<IAuthApi>(resolver.Resolve<HttpClient>()));

    containerRegistry.RegisterSingleton<IPatientApi>(resolver =>
        RestService.For<IPatientApi>(resolver.Resolve<HttpClient>()));

    containerRegistry.RegisterSingleton<IUserApi>(resolver =>
        RestService.For<IUserApi>(resolver.Resolve<HttpClient>()));

    containerRegistry.RegisterSingleton<IMedicalCaseApi>(resolver =>
        RestService.For<IMedicalCaseApi>(resolver.Resolve<HttpClient>()));

    containerRegistry.RegisterSingleton<IConsultationApi>(resolver =>
        RestService.For<IConsultationApi>(resolver.Resolve<HttpClient>()));

    containerRegistry.RegisterSingleton<IPrescriptionApi>(resolver =>
        RestService.For<IPrescriptionApi>(resolver.Resolve<HttpClient>()));

    containerRegistry.RegisterSingleton<IHerbApi>(resolver =>
        RestService.For<IHerbApi>(resolver.Resolve<HttpClient>()));

    containerRegistry.RegisterSingleton<IFormulaApi>(resolver =>
        RestService.For<IFormulaApi>(resolver.Resolve<HttpClient>()));
}
```

**2. Repository注册**（各模块的XxxModule.cs）：

```csharp
/// <summary>
/// 患者管理模块 - Phase 2模块化架构
/// Issue #1114 - Repository下沉到模块
/// </summary>
public class PatientsModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // ⭐ Phase 2：Repository由模块自己注册
        containerRegistry.RegisterSingleton<IPatientRepository, PatientRepository>();

        // 注册ViewModel
        containerRegistry.Register<PatientDetailViewModel>();
        containerRegistry.RegisterDialog<QuickCreatePatientDialog, QuickCreatePatientDialogViewModel>();

        // 注册视图用于导航
        containerRegistry.RegisterForNavigation<PatientDetailView>();
    }
}

// 其他模块类似：
// - MedicalCaseModule → IMedicalCaseRepository, MedicalCaseRepository
// - UsersModule → IUserRepository, UserRepository
// - HerbsModule → IHerbRepository, HerbRepository
// - FormulaModule → IFormulaRepository, FormulaRepository
```

**3. Infrastructure服务注册**（Shell/Extensions/ServiceCollectionExtensions.cs）：

```csharp
/// <summary>
/// 注册 Foundation 层服务（Infrastructure Services）
/// </summary>
private static void RegisterFoundationServices(IContainerRegistry containerRegistry)
{
    // 认证服务 - Foundation/Security
    containerRegistry.RegisterSingleton<IAuthenticationService, AuthenticationService>();

    // Token 存储服务 - Foundation/Security
    containerRegistry.RegisterSingleton<ITokenStorageService, TokenStorageService>();

    // 用户名存储服务 - Foundation/Security (Issue #1245)
    containerRegistry.RegisterSingleton<IUsernameStorageService, UsernameStorageService>();

    // 安全凭据存储服务 - Foundation/Security (Issue #1246)
    containerRegistry.RegisterSingleton<ISecureCredentialStorage, SecureCredentialStorage>();

    // API 健康检查服务 - Foundation/HealthCheck
    containerRegistry.RegisterSingleton<IApiHealthCheckService, ApiHealthCheckService>();
}

/// <summary>
/// 注册 Infrastructure 层服务
/// </summary>
private static void RegisterInfrastructureServices(IContainerRegistry containerRegistry)
{
    // 会话管理器
    containerRegistry.RegisterSingleton<ISessionManager, SessionManager>();

    // 用户通知服务
    containerRegistry.RegisterSingleton<IUserNotificationService, UserNotificationService>();

    // 主窗口服务门面
    containerRegistry.RegisterSingleton<IMainWindowServicesFacade, MainWindowServicesFacade>();

    // 标准错误处理器
    containerRegistry.RegisterSingleton<IStandardErrorHandler, StandardErrorHandler>();

    // 键盘快捷键服务
    containerRegistry.RegisterSingleton<IKeyboardShortcutService, KeyboardShortcutService>();

    // 功能开关服务 (Issue #1477 #1479)
    containerRegistry.RegisterSingleton<IFeatureToggleService, FeatureToggleService>();

    // 角色导航服务 (Issue #1553)
    containerRegistry.RegisterSingleton<IRoleNavigationService, RoleNavigationService>();
}
```

**架构特征总结**：
- ✅ **Refit API接口**：Shell统一注册，所有模块共享同一HttpClient（包含Authorization header）
- ✅ **Repository层**：各模块独立注册，封装API调用逻辑
- ✅ **Infrastructure服务**：Foundation + Infrastructure + Presentation三层服务，Shell统一注册
- ❌ **无业务Service层**：ViewModel直接注入Repository + Infrastructure服务

### AutoMapper配置

> **⚠️ 项目现状**：当前项目**未使用AutoMapper**进行DTO-Model映射。
> Phase 2架构中，Repository直接返回DTO，ViewModel使用DTO进行数据绑定。
> 如未来需要AutoMapper，可参考以下配置模式。

**当前手动映射方式**（真实代码：PatientSelectorViewModel.cs）：
```csharp
/// <summary>
/// Phase 2架构：手动映射DTO属性（不使用AutoMapper）
/// 注意：Presentation层不能引用Modules层，使用反射进行手动映射
/// </summary>
private PatientSelectedPayload CreatePatientSelectedPayload(object patientDto)
{
    var patientType = patientDto.GetType();
    return new PatientSelectedPayload
    {
        PatientId = (Guid)patientType.GetProperty("Id")!.GetValue(patientDto)!,
        PatientName = (string)patientType.GetProperty("Name")!.GetValue(patientDto)!,
        Gender = (string?)patientType.GetProperty("Gender")?.GetValue(patientDto),
        BirthDate = (DateTime?)patientType.GetProperty("BirthDate")?.GetValue(patientDto),
        PhoneNumber = (string?)patientType.GetProperty("PhoneNumber")?.GetValue(patientDto)
    };
}
```

**未来AutoMapper配置示例**（如需引入）：
```csharp
// Core/Mapping/MappingProfile.cs
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // ⚠️ 示例配置：项目当前未使用

        // Patient映射
        CreateMap<PatientDto, PatientModel>()
            .ForMember(dest => dest.Age, opt => opt.MapFrom(src => CalculateAge(src.BirthDate)));
        CreateMap<PatientModel, PatientCreateDto>();
        CreateMap<PatientModel, PatientUpdateDto>();

        // MedicalCase映射
        CreateMap<MedicalCaseDto, MedicalCaseModel>();
        CreateMap<MedicalCaseModel, MedicalCaseCreateDto>();
    }

    private static int CalculateAge(DateTime birthDate)
    {
        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        return age;
    }
}
```

## 🎨 主题与样式

### 资源字典
```xml
<!-- Styles/Colors.xaml -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    
    <!-- 主色调 -->
    <SolidColorBrush x:Key="PrimaryBrush" Color="#2196F3"/>
    <SolidColorBrush x:Key="SecondaryBrush" Color="#FFC107"/>
    <SolidColorBrush x:Key="AccentBrush" Color="#4CAF50"/>
    <SolidColorBrush x:Key="DangerBrush" Color="#F44336"/>
    <SolidColorBrush x:Key="WarningBrush" Color="#FF9800"/>
    <SolidColorBrush x:Key="InfoBrush" Color="#2196F3"/>
    
    <!-- 背景色 -->
    <SolidColorBrush x:Key="BackgroundBrush" Color="#F5F5F5"/>
    <SolidColorBrush x:Key="SurfaceBrush" Color="#FFFFFF"/>
    <SolidColorBrush x:Key="OnSurfaceBrush" Color="#212121"/>
    
    <!-- 文字颜色 -->
    <SolidColorBrush x:Key="PrimaryTextBrush" Color="#212121"/>
    <SolidColorBrush x:Key="SecondaryTextBrush" Color="#757575"/>
    <SolidColorBrush x:Key="DisabledTextBrush" Color="#BDBDBD"/>
</ResourceDictionary>

<!-- Styles/ButtonStyles.xaml -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    
    <Style x:Key="PrimaryButtonStyle" TargetType="Button">
        <Setter Property="Background" Value="{StaticResource PrimaryBrush}"/>
        <Setter Property="Foreground" Value="White"/>
        <Setter Property="Padding" Value="16,8"/>
        <Setter Property="Margin" Value="4"/>
        <Setter Property="BorderThickness" Value="0"/>
        <Setter Property="Cursor" Value="Hand"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border Background="{TemplateBinding Background}"
                            CornerRadius="4"
                            Padding="{TemplateBinding Padding}">
                        <ContentPresenter HorizontalAlignment="Center"
                                          VerticalAlignment="Center"/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter Property="Background" Value="#1976D2"/>
                        </Trigger>
                        <Trigger Property="IsPressed" Value="True">
                            <Setter Property="Background" Value="#1565C0"/>
                        </Trigger>
                        <Trigger Property="IsEnabled" Value="False">
                            <Setter Property="Background" Value="#BDBDBD"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>
    
    <Style x:Key="SecondaryButtonStyle" TargetType="Button" BasedOn="{StaticResource PrimaryButtonStyle}">
        <Setter Property="Background" Value="{StaticResource SecondaryBrush}"/>
        <Setter Property="Foreground" Value="Black"/>
        <Style.Triggers>
            <Trigger Property="IsMouseOver" Value="True">
                <Setter Property="Background" Value="#FFA000"/>
            </Trigger>
            <Trigger Property="IsPressed" Value="True">
                <Setter Property="Background" Value="#FF8F00"/>
            </Trigger>
        </Style.Triggers>
    </Style>
    
    <Style x:Key="DangerButtonStyle" TargetType="Button" BasedOn="{StaticResource PrimaryButtonStyle}">
        <Setter Property="Background" Value="{StaticResource DangerBrush}"/>
        <Style.Triggers>
            <Trigger Property="IsMouseOver" Value="True">
                <Setter Property="Background" Value="#D32F2F"/>
            </Trigger>
            <Trigger Property="IsPressed" Value="True">
                <Setter Property="Background" Value="#C62828"/>
            </Trigger>
        </Style.Triggers>
    </Style>
</ResourceDictionary>
```

## 🚀 性能优化

### 1. 数据绑定优化
```csharp
// 使用OneTime绑定减少更新开销
<TextBlock Text="{Binding Title, Mode=OneTime}"/>

// 对大数据集合使用虚拟化
<ListBox VirtualizingPanel.IsVirtualizing="True"
         VirtualizingPanel.VirtualizationMode="Recycling"
         ScrollViewer.IsDeferredScrollingEnabled="True"/>
```

### 2. 异步操作优化
```csharp
// 使用ConfigureAwait减少上下文切换
var result = await _patientService.GetPatientsAsync().ConfigureAwait(false);

// 使用CancellationToken取消长时间操作
public async Task LoadPatientsAsync(CancellationToken cancellationToken = default)
{
    IsBusy = true;
    try
    {
        var result = await _patientService.GetPatientsAsync(cancellationToken);
        // 处理结果
    }
    finally
    {
        IsBusy = false;
    }
}
```

### 3. 内存管理
```csharp
// 实现IDisposable接口
public class PatientManagementViewModel : ViewModelBase, IDisposable
{
    private bool _disposed = false;
    
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // 释放托管资源
                Patients?.Clear();
            }
            _disposed = true;
        }
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
```

## 🧪 测试策略

> **⚠️ Phase 2测试原则**：ViewModel测试Mock IRepository（不是IService），测试ViewModel业务逻辑和UI交互

### 单元测试示例（真实代码：UserManagementViewModelTests.cs）

```csharp
using FluentAssertions;
using LYBT.Desktop.Users.Interfaces;
using LYBT.Desktop.Users.ViewModels;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.Logging;
using Moq;
using Prism.Events;
using Prism.Regions;
using Xunit;

/// <summary>
/// UserManagementViewModel 单元测试
/// Phase 2架构：Mock IUserRepository，测试ViewModel逻辑
/// </summary>
public class UserManagementViewModelTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;      // ⭐ Mock Repository（不是Service）
    private readonly Mock<IEventAggregator> _mockEventAggregator;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<IRegionManager> _mockRegionManager;
    private readonly UserManagementViewModel _viewModel;

    public UserManagementViewModelTests()
    {
        // Arrange - Setup Mocks
        _mockUserRepository = new Mock<IUserRepository>();
        _mockEventAggregator = new Mock<IEventAggregator>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockRegionManager = new Mock<IRegionManager>();

        // ⭐ Phase 2构造函数：注入Repository + Infrastructure服务
        _viewModel = new UserManagementViewModel(
            _mockUserRepository.Object,       // Repository
            _mockEventAggregator.Object,      // Prism EventAggregator
            _mockLoggerFactory.Object,        // Logger
            _mockRegionManager.Object         // Prism RegionManager
        );
    }

    [Fact]
    public async Task LoadPageAsync_WhenRepositoryReturnsData_ShouldPopulateUsers()
    {
        // Arrange - 准备测试数据
        var users = new List<UserDto>
        {
            new UserDto { Id = Guid.NewGuid(), Username = "张三", Role = UserRole.Doctor },
            new UserDto { Id = Guid.NewGuid(), Username = "李四", Role = UserRole.Admin }
        };
        var pagedResult = new PagedResult<UserDto>
        {
            Items = users,
            TotalCount = 2,
            PageNumber = 1,
            PageSize = 20
        };

        // ⭐ Mock Repository方法（不是Service方法）
        _mockUserRepository
            .Setup(x => x.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<UserRole?>()))
            .ReturnsAsync(pagedResult);

        // Act - 执行命令
        await _viewModel.LoadPageAsync(1);

        // Assert - 验证结果（使用FluentAssertions）
        _viewModel.Users.Should().HaveCount(2);
        _viewModel.Users[0].Username.Should().Be("张三");
        _viewModel.TotalCount.Should().Be(2);

        // 验证Repository调用
        _mockUserRepository.Verify(
            x => x.GetPagedAsync(1, 20, null),
            Times.Once);
    }
}
```

**Phase 2测试特征**：
- ✅ **Mock IRepository**：测试Mock IUserRepository（不是IUserService）
- ✅ **xUnit框架**：使用[Fact]特性（不是NUnit的[Test]）
- ✅ **FluentAssertions**：Should().HaveCount()、Should().Be()等流式断言
- ✅ **AAA模式**：Arrange-Act-Assert清晰分离
- ✅ **依赖注入**：构造函数注入Repository + Prism服务（EventAggregator、RegionManager）
- ❌ **无Service Mock**：Phase 2架构移除业务Service层

## 📋 最佳实践

### 1. 代码规范
- **命名约定**：使用PascalCase，接口以I开头
- **依赖注入**：优先使用构造函数注入
- **异步编程**：I/O操作使用async/await
- **错误处理**：统一使用try-catch包装异步操作

### 2. 架构原则
- **单一职责**：每个类只负责一个功能
- **开闭原则**：对扩展开放，对修改封闭
- **依赖倒置**：依赖抽象，不依赖具体实现
- **关注分离**：UI、业务逻辑、数据访问分离

### 3. 性能原则
- **避免阻塞**：UI线程不执行长时间操作
- **合理缓存**：缓存常用数据，减少网络请求
- **资源释放**：及时释放不再使用的资源
- **延迟加载**：大数据集使用分页或虚拟化

### 4. 聚合根设计模式（Issue #1463）

> **📚 权威参考**：详细实体关系定义参见 [clinical-workflow-entity-relationships.md](../shared/clinical-workflow-entity-relationships.md)（⭐⭐⭐权威文档）

**核心原则**：MedicalCase是聚合根，统一管理Consultation和Prescription的生命周期。

**本节重点**：从MVVM视角说明如何在Desktop端实现聚合根模式，避免ViewModel直接操作Consultation/Prescription Repository。

#### ❌ 错误实现
```csharp
public class ConsultationEntryViewModel
{
    private readonly IConsultationRepository _consultationRepository;
    private readonly IMedicalCaseRepository _medicalCaseRepository;

    // ❌ 错误：分两步创建，破坏聚合根模式
    private async Task SaveAsync()
    {
        // 1. 单独创建MedicalCase
        if (!MedicalCaseId.HasValue)
        {
            var medicalCase = await _medicalCaseRepository.CreateAsync(medicalCaseDto);
            MedicalCaseId = medicalCase.Id;
        }

        // 2. 单独创建Consultation
        consultationDto.MedicalCaseId = MedicalCaseId.Value;
        await _consultationRepository.CreateAsync(consultationDto);
    }
}
```

**问题**：
- 破坏原子性（两次API调用，可能部分失败）
- 违反DDD聚合根模式（子实体独立创建）
- 依赖混乱（同时注入MedicalCase和Consultation的Repository）

#### ✅ 正确实现
```csharp
public class ConsultationEntryViewModel
{
    // ✅ 只依赖聚合根Repository
    private readonly IMedicalCaseRepository _medicalCaseRepository;

    private async Task SaveAsync()
    {
        if (!ValidateInput()) return;

        // 构造聚合根数据
        var medicalCaseDto = new MedicalCaseCreateDto
        {
            PatientId = CurrentPatient!.Id,
            DoctorId = SessionManager?.CurrentUser?.Id ?? Guid.Empty,
            ChiefComplaint = ChiefComplaint,
            Remark = $"创建于: {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
        };

        // 构造子实体数据
        var consultationDto = new ConsultationCreateDto
        {
            PatientId = CurrentPatient!.Id,
            UserId = SessionManager?.CurrentUser?.Id ?? Guid.Empty,
            PatientName = CurrentPatient.Name,
            DoctorName = SessionManager?.CurrentUser?.RealName ?? "未知医生",
            ChiefComplaint = ChiefComplaint,
            PresentIllness = PresentIllness,
            Inspection = Inspection,
            AuscultationOlfaction = AuscultationOlfaction,
            Inquiry = Inquiry,
            Palpation = Palpation,
            TCMDiagnosis = TCMDiagnosis,
            TreatmentPrinciple = TreatmentPrinciple,
            Remark = Remarks,
            StartTime = DateTime.Now
        };

        // ✅ 使用聚合根方法一次性创建（原子操作）
        var result = await _medicalCaseRepository.CreateWithDetailsAsync(
            medicalCaseDto,
            consultationDto,
            null // 暂无处方
        );

        MedicalCaseId = result.Id;

        Logger.LogInformation("诊疗记录保存成功, MedicalCaseId: {MedicalCaseId}, 患者: {PatientName}",
            result.Id, CurrentPatient.Name);
    }
}
```

**优势**：
- ✅ **原子性**：一次API调用完成整个聚合创建
- ✅ **一致性**：Server端保证MedicalCase和Consultation的共享主键关系
- ✅ **符合DDD**：聚合根统一管理子实体生命周期
- ✅ **简化依赖**：ViewModel只需注入IMedicalCaseRepository

#### ✅ 正确实现（Issue #1563 - 更新诊断信息）

**场景**：MedicalCase已创建，后续填写或更新Consultation信息

```csharp
public class ConsultationFormViewModel
{
    // ✅ 只依赖聚合根Repository
    private readonly IMedicalCaseRepository _medicalCaseRepository;

    // MedicalCaseId由上一步（患者选择）传递而来
    public Guid MedicalCaseId { get; set; }

    public async Task<bool> SaveAsync()
    {
        if (!Validate()) return false;

        // 构造ConsultationUpdateDto（不需要PatientId/UserId等关联字段）
        var updateDto = new ConsultationUpdateDto
        {
            Id = MedicalCaseId, // Consultation使用与MedicalCase相同的ID（共享主键）
            ChiefComplaint = ChiefComplaint.Trim(),
            PresentIllness = PresentIllness?.Trim(),
            Inspection = Inspection?.Trim(),
            AuscultationOlfaction = AuscultationOlfaction?.Trim(),
            Inquiry = Inquiry?.Trim(),
            Palpation = Palpation?.Trim(),
            TCMDiagnosis = TCMDiagnosis.Trim(),
            TreatmentPrinciple = TreatmentPrinciple?.Trim(),
            Remark = Remark?.Trim()
        };

        // ✅ 使用聚合根方法更新Consultation
        await _medicalCaseRepository.UpdateConsultationAsync(MedicalCaseId, updateDto);

        Logger.LogInformation("诊断信息保存成功，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);
        return true;
    }
}
```

**关键点**：
- ✅ **聚合根方法**：使用`UpdateConsultationAsync`而非直接调用IConsultationRepository
- ✅ **共享主键**：ConsultationUpdateDto.Id = MedicalCaseId（一对一关系）
- ✅ **字段简化**：UpdateDto不需要PatientId/UserId（Server端从MedicalCase获取）
- ✅ **依赖单一**：ViewModel只注入IMedicalCaseRepository

**实际应用**：
- `LYBT.Desktop.Consultation.ViewModels.ConsultationFormViewModel`（Issue #1563修复后）

#### 架构规范
1. **聚合识别**：MedicalCase = Consultation + Prescription（一对一关系，共享主键）
2. **创建规则**：必须通过`IMedicalCaseRepository.CreateWithDetailsAsync()`创建
3. **更新规则**：必须通过`IMedicalCaseRepository.UpdateConsultationAsync()`或`UpdatePrescriptionAsync()`更新子实体
4. **禁止模式**：禁止ViewModel直接调用`IConsultationRepository`的Create/Update方法
5. **模块依赖**：ConsultationModule保留`[ModuleDependency("MedicalCaseModule")]`确保初始化顺序

**参考**：
- Server端实现：
  - `LYBT.Module.MedicalCase.Services.MedicalCaseService:CreateWithDetailsAsync()`
  - `LYBT.Module.MedicalCase.Services.MedicalCaseService:UpdateConsultationAsync()` (Issue #1563)
- Desktop端实现：
  - `LYBT.Desktop.MedicalCase.Repositories.MedicalCaseRepository:CreateWithDetailsAsync()`
  - `LYBT.Desktop.MedicalCase.Repositories.MedicalCaseRepository:UpdateConsultationAsync()` (Issue #1563)
- 修复Issue：#1463, #1563

## 🔗 相关文档

- **[Shell层架构设计](shell-layer-design.md)** - Shell层职责边界、组件结构、交互模式详解
- **[架构总览](../README.md)** - 三层对齐架构设计原理
- **[Server端架构](../server/README.md)** - 服务端三层架构实现
- **[共享架构](../shared/README.md)** - 跨端组件和标准
- **[Client端开发指南](../../development/client/README.md)** - WPF开发规范和实践
- **[模块设计指南](../module-design-guide.md)** - 业务模块化设计标准
- **[ADR-003 Workstation架构重构](../decisions/adr-003-workstation-refactoring.md)** - Shell层架构决策记录

---

**文档维护**：架构组 | **最后更新**：2025-10-20
**适用版本**：v5.0 对齐架构版 | **审核状态**：已审核