# Design: optimize-desktop-core

## 设计概述

本设计分为四个层次：
1. **Phase 0**: Core层纯净化 - 清理业务代码污染
2. **Phase 1**: ViewModel继承扁平化 - 重构基类体系
3. **Phase 2-3**: Components化 - 拆分大型ViewModel
4. **Phase 4**: 规范更新

## Phase 0: Core层纯净化设计

### 当前Core层问题

```
LYBT.Desktop.Core/
├── Contracts/        ← OK: API接口定义
├── Foundation/       ← OK: HTTP, Security
├── Infrastructure/   ← OK但有重复: WPF服务, Repository
├── Models/           ← 问题: 包含业务代码
│   ├── ViewModels/Base/
│   │   └── HerbItemViewModelBase.cs (304行) ← 业务代码污染
│   └── Items/
│       ├── ConsultationItem.cs   ← 业务代码
│       ├── FormulaItem.cs        ← 业务代码
│       ├── FormulaHerbItem.cs    ← 业务代码
│       ├── HerbItem.cs           ← 业务代码
│       ├── MedicalCaseItem.cs    ← 业务代码
│       ├── PatientItem.cs        ← 业务代码
│       ├── PrescriptionHerbItem.cs ← 业务代码
│       └── UserItem.cs           ← 业务代码
└── Utilities/        ← 问题: 与Infrastructure重复
    ├── ExcelHelper.cs (705行)            ← 重复
    └── ClientErrorMessageMapper.cs (366行) ← 重复
```

### 目标Core层结构

```
LYBT.Desktop.Core/
├── Contracts/        ← API接口定义
├── Foundation/       ← HTTP, Security
├── Infrastructure/   ← WPF服务, Repository, Excel, ErrorMapping
└── Models/           ← 纯框架级ViewModel基类
    └── ViewModels/Base/
        ├── ViewModelCore.cs       ← 新: 最小化基类
        ├── ViewModelBase.cs       ← 保留: 兼容
        └── UnifiedViewModelBase.cs ← 保留: 兼容

注: Utilities项目删除，功能合并到Infrastructure
注: Items/目录删除，Item类迁移到各业务模块
注: HerbItemViewModelBase迁移到Herbs模块
```

### 代码迁移映射

| 源文件 | 目标位置 | 说明 |
|--------|----------|------|
| Core/Models/Items/ConsultationItem.cs | Consultation/Models/ | 业务归属 |
| Core/Models/Items/FormulaItem.cs | Formula/Models/ | 业务归属 |
| Core/Models/Items/FormulaHerbItem.cs | Formula/Models/ | 业务归属 |
| Core/Models/Items/HerbItem.cs | Herbs/Models/ | 业务归属 |
| Core/Models/Items/MedicalCaseItem.cs | MedicalCase/Models/ | 业务归属 |
| Core/Models/Items/PatientItem.cs | Patients/Models/ | 业务归属 |
| Core/Models/Items/PrescriptionHerbItem.cs | Prescriptions/Models/ | 业务归属 |
| Core/Models/Items/UserItem.cs | Users/Models/ | 业务归属 |
| Core/Models/ViewModels/Base/HerbItemViewModelBase.cs | Herbs/ViewModels/Base/ | 业务归属 |
| Utilities/ExcelHelper.cs | 删除 | 保留Infrastructure版本 |
| Utilities/ClientErrorMessageMapper.cs | 删除 | 保留Infrastructure版本 |

## Phase 1: ViewModel继承扁平化设计

### 当前继承结构 (4层，问题)

```
BindableBase (Prism ~50行)
    └── ViewModelBase (362行)
            └── UnifiedViewModelBase (231行)
                    └── MasterDetailViewModelBase (484行)
                            └── 具体ViewModel

总计: ~1100+ 行基类代码
问题: 职责混杂, 难以测试, 违反单一职责
```

### 目标继承结构 (2-3层)

```
BindableBase (Prism)
    └── ViewModelCore (~100行)
            └── 具体ViewModel + Mixins

或 (需要列表功能时):
BindableBase (Prism)
    └── ViewModelCore (~100行)
            └── ListViewModelBase (~150行)
                    └── 具体ViewModel + Mixins
```

### ViewModelCore设计 (新基类)

```csharp
/// <summary>
/// ViewModel核心基类 - 最小化职责
/// 继承: BindableBase (INotifyPropertyChanged)
/// 实现: IDisposable
/// </summary>
public abstract class ViewModelCore : BindableBase, IDisposable
{
    protected readonly ILogger Logger;
    private readonly CompositeDisposable _disposables = new();
    private bool _disposed;

    // 最小状态
    private bool _isLoading;
    private bool _isBusy;

    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }

    protected ViewModelCore(ILoggerFactory loggerFactory)
    {
        Logger = loggerFactory.CreateLogger(GetType());
    }

    // 安全执行 (最简实现)
    protected async Task<T?> ExecuteSafelyAsync<T>(Func<Task<T>> action, T? fallback = default)
    {
        try { IsBusy = true; return await action(); }
        catch (Exception ex) { Logger.LogError(ex, "操作失败"); return fallback; }
        finally { IsBusy = false; }
    }

    // Dispose模式
    protected void AddDisposable(IDisposable d) => _disposables.Add(d);
    public void Dispose() { if (!_disposed) { _disposables.Dispose(); _disposed = true; } }
}
```

**行数目标**: ~100行

### Mixin接口设计 (组合模式)

```csharp
/// <summary>导航感知接口</summary>
public interface INavigatable : INavigationAware
{
    IRegionManager RegionManager { get; }
}

/// <summary>验证感知接口</summary>
public interface IValidatable : INotifyDataErrorInfo
{
    void AddError(string property, string error);
    void ClearErrors(string? property = null);
}

/// <summary>会话感知接口</summary>
public interface ISessionAware
{
    ISessionManager SessionManager { get; }
    Guid CurrentUserId { get; }
}

/// <summary>HTTP错误处理接口</summary>
public interface IHttpErrorHandler
{
    Task HandleApiExceptionAsync(ApiException ex, string operation);
    Task HandleUnauthorizedAsync();
}
```

### Mixin默认实现 (可复用)

```csharp
/// <summary>验证功能Mixin</summary>
public class ValidatableMixin : IValidatable
{
    private readonly Dictionary<string, List<string>> _errors = new();
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public bool HasErrors => _errors.Any();
    public IEnumerable GetErrors(string? name) =>
        string.IsNullOrEmpty(name) ? _errors.SelectMany(x => x.Value)
                                   : _errors.GetValueOrDefault(name, new());

    public void AddError(string property, string error) { /* 实现 */ }
    public void ClearErrors(string? property = null) { /* 实现 */ }
}
```

### 使用示例

```csharp
public class HerbMasterDetailViewModel : ViewModelCore, INavigatable, IValidatable
{
    // 组合Mixin
    private readonly ValidatableMixin _validation = new();
    private readonly HerbCommandHandler _commands;
    private readonly HerbDataProvider _data;

    // IValidatable委托
    public bool HasErrors => _validation.HasErrors;
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged
    {
        add => _validation.ErrorsChanged += value;
        remove => _validation.ErrorsChanged -= value;
    }

    // 业务逻辑
    // ...
}
```

## Phase 2-3: Components标准架构

### 命名规范

| Component类型 | 命名模式 | 职责 |
|---------------|----------|------|
| CommandHandler | `{Entity}CommandHandler` | 处理用户命令(Add/Edit/Delete/Enable/Disable) |
| DataProvider | `{Entity}DataProvider` | 数据加载、保存、导入导出 |
| Validator | `{Entity}Validator` | 业务验证逻辑 |
| Calculator | `{Entity}Calculator` | 计算逻辑(如需要) |
| Coordinator | `{Entity}Coordinator` | 跨组件协调(如需要) |

### 目录结构

```
LYBT.Desktop.{Domain}/
├── {Domain}Module.cs
├── Models/                    ← 新: 承接Item类
│   └── {Entity}Item.cs
├── Views/
│   ├── {Entity}MasterDetailView.xaml
│   └── Dialogs/
├── ViewModels/
│   ├── Base/                  ← 新: 模块特定基类
│   │   └── HerbItemViewModelBase.cs (仅Herbs模块)
│   ├── {Entity}MasterDetailViewModel.cs
│   └── Components/
│       ├── {Entity}CommandHandler.cs
│       ├── {Entity}DataProvider.cs
│       └── {Entity}Validator.cs
└── Services/ (可选)
```

### Herbs模块详细设计

#### HerbCommandHandler

```csharp
/// <summary>
/// 药材命令处理器 - 处理用户操作命令
/// </summary>
public class HerbCommandHandler
{
    private readonly IHerbRepository _repository;
    private readonly ICommonDialogService _dialogService;
    private readonly IEventAggregator _eventAggregator;
    private readonly ILogger<HerbCommandHandler> _logger;

    public HerbCommandHandler(
        IHerbRepository repository,
        ICommonDialogService dialogService,
        IEventAggregator eventAggregator,
        ILogger<HerbCommandHandler> logger)
    {
        _repository = repository;
        _dialogService = dialogService;
        _eventAggregator = eventAggregator;
        _logger = logger;
    }

    public async Task<bool> AddAsync(HerbDto herb);
    public async Task<bool> EditAsync(HerbDto herb);
    public async Task<bool> DeleteAsync(Guid id);
    public async Task<bool> BatchDeleteAsync(IEnumerable<Guid> ids);
    public async Task<bool> EnableAsync(Guid id);
    public async Task<bool> DisableAsync(Guid id);
    public async Task<bool> BatchEnableAsync(IEnumerable<Guid> ids);
    public async Task<bool> BatchDisableAsync(IEnumerable<Guid> ids);
}
```

#### HerbDataProvider

```csharp
/// <summary>
/// 药材数据提供器 - 处理数据加载和导入导出
/// </summary>
public class HerbDataProvider
{
    private readonly IHerbRepository _repository;
    private readonly IExcelHelper _excelHelper;
    private readonly ILogger<HerbDataProvider> _logger;

    public async Task<PagedResult<HerbListDto>> LoadListAsync(HerbQueryDto query);
    public async Task<HerbDto?> LoadDetailAsync(Guid id);
    public async Task<List<HerbDto>> LoadAllAsync();

    public async Task<ImportResult> ImportAsync(Stream fileStream);
    public async Task<byte[]> ExportAsync(HerbQueryDto query);
    public byte[] GetExportTemplate();
}
```

#### HerbValidator

```csharp
/// <summary>
/// 药材验证器 - 处理业务验证逻辑
/// </summary>
public class HerbValidator
{
    private readonly IHerbRepository _repository;
    private readonly ILogger<HerbValidator> _logger;

    public async Task<ValidationResult> ValidateAsync(HerbDto herb);
    public async Task<bool> CheckReferenceAsync(Guid id);
    public async Task<bool> CheckNameDuplicateAsync(string name, Guid? excludeId = null);
    public async Task<bool> CheckPinYinCodeDuplicateAsync(string code, Guid? excludeId = null);
}
```

#### 重构后的HerbMasterDetailViewModel

```csharp
/// <summary>
/// 药材主从视图ViewModel
/// 目标行数: < 400行
/// </summary>
public class HerbMasterDetailViewModel : ViewModelCore, INavigatable
{
    private readonly HerbCommandHandler _commands;
    private readonly HerbDataProvider _dataProvider;
    private readonly HerbValidator _validator;

    // 构造函数: 依赖注入Components
    public HerbMasterDetailViewModel(
        HerbCommandHandler commands,
        HerbDataProvider dataProvider,
        HerbValidator validator,
        IRegionManager regionManager,
        ILoggerFactory loggerFactory)
        : base(loggerFactory)
    {
        _commands = commands;
        _dataProvider = dataProvider;
        _validator = validator;
        RegionManager = regionManager;

        InitializeCommands();
    }

    // ViewModel主要职责:
    // 1. 定义UI绑定属性 (~50行)
    // 2. 定义DelegateCommand (~30行)
    // 3. 协调Components完成业务 (~200行)
    // 4. 处理导航和生命周期 (~50行)
    // 5. UI状态管理 (~50行)
}
```

## DI注册模式

```csharp
public class HerbsModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // Components注册为Transient
        containerRegistry.Register<HerbCommandHandler>();
        containerRegistry.Register<HerbDataProvider>();
        containerRegistry.Register<HerbValidator>();

        // ViewModel注册
        containerRegistry.Register<HerbMasterDetailViewModel>();

        // View导航
        containerRegistry.RegisterForNavigation<HerbMasterDetailView>();
    }
}
```

## 数据流标准化

### 目标数据流

```
┌─────────┐     ┌────────────┐     ┌─────────────┐     ┌──────┐
│  API    │ ──► │ Repository │ ──► │  ViewModel  │ ──► │ View │
│ (Refit) │     │  (DTO)     │     │ (Component) │     │(XAML)│
└─────────┘     └────────────┘     └─────────────┘     └──────┘

注: 无Item中间层，直接使用DTO
```

### 删除Item中间层

Item类只是DTO的封装，增加复杂度无明显价值。迁移后逐步删除：

1. 保留Item类到业务模块（Phase 0迁移）
2. 逐步将ViewModel改为直接使用DTO
3. 最终删除Item类（作为后续优化）

## 参考实现

MedicalCase模块的Components结构作为最佳实践参考:

```
LYBT.Desktop.MedicalCase/ViewModels/Components/
├── MedicalCaseEditModeStateMachine.cs  - 状态机管理
├── MedicalCaseWorkspaceCoordinator.cs  - 工作区协调
├── PrescriptionCalculator.cs           - 处方计算
├── PrescriptionDataLoader.cs           - 数据加载
├── PrescriptionImportHandler.cs        - 导入处理
├── PrescriptionItemHandler.cs          - 处方项处理
├── PrescriptionSaveHandler.cs          - 保存处理
└── PrescriptionValidator.cs            - 验证逻辑
```

## 迁移策略

### Phase 0 策略
1. 先删除代码重复（无风险）
2. 迁移Item类到各模块（低风险）
3. 迁移HerbItemViewModelBase（中风险，需验证）
4. 删除Utilities项目（需确认无遗漏）

### Phase 1 策略
1. 创建ViewModelCore并行存在
2. 新ViewModel使用ViewModelCore
3. 逐步迁移旧ViewModel
4. 标记旧基类Obsolete

### Phase 2-3 策略
1. 先创建Components
2. 逐步将代码从ViewModel迁移到Components
3. 每步迁移后确保编译通过
4. 保留ViewModel的公开接口不变
