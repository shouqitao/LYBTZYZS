# WPF前端MVVM架构标准文档

**版本**: v2.0  
**最后更新**: 2025-09-01  
**适用项目**: LYBTZYZS - 凌隐宝堂中医诊所系统  
**状态**: 🎯 **核心架构标准** - 所有前端模块必须遵循

---

## 📋 目录

- [1. 架构概览](#1-架构概览)
- [2. 核心组件](#2-核心组件)
- [3. 实现模式](#3-实现模式)
- [4. 代码示例](#4-代码示例)
- [5. 开发规范](#5-开发规范)
- [6. 性能优化](#6-性能优化)
- [7. 测试策略](#7-测试策略)

---

## 1. 架构概览

### 1.1 MVVM模式定义

MVVM (Model-View-ViewModel) 是WPF应用的标准架构模式，通过数据绑定实现界面与业务逻辑的分离，专为小型诊所系统设计的实用化架构。

```
┌─────────────────────────────────────────────────┐
│                   View (XAML)                   │
│                 用户界面层                        │
└─────────────────────────────────────────────────┘
                           ↕ 数据绑定
┌─────────────────────────────────────────────────┐
│                ViewModel (Prism)                │
│           界面逻辑、状态管理、业务调用              │
└─────────────────────────────────────────────────┘
                           ↕ 直接调用
┌─────────────────────────────────────────────────┐
│              Service Module (API)               │
│           后端API服务调用和数据处理                 │
└─────────────────────────────────────────────────┘
                           ↕ 数据传输
┌─────────────────────────────────────────────────┐
│                Model (DTOs)                     │
│               数据传输对象                        │
└─────────────────────────────────────────────────┘
```

### 1.2 架构优势

- **🎯 简洁性**: 三层架构，避免过度设计
- **🔄 实用性**: 适合小型诊所20人以下规模
- **🧪 可测试性**: ViewModel与Service独立测试
- **📈 可维护性**: 清晰的职责分离，便于理解
- **⚡ 高性能**: 直接API调用，无中间层开销

---

## 2. 核心组件

### 2.1 View (视图层)

**职责**: 纯粹的用户界面呈现
- XAML声明式界面定义
- 数据绑定和样式应用
- 用户交互事件路由

**约束**:
- ❌ 不允许包含业务逻辑
- ❌ 不允许直接调用API
- ✅ 只能通过DataBinding与ViewModel通信

### 2.2 ViewModel (视图模型层)

**职责**: 界面逻辑、状态管理和业务调用
- 界面状态维护 (IsBusy, IsEnabled等)
- 命令处理 (ICommand实现)
- 数据绑定属性 (INotifyPropertyChanged)
- 直接调用Service模块处理业务
- 异常处理和用户提示
- 界面导航逻辑

**关键特征**:
- 继承自Prism `BindableBase`、`ModernViewModelBase`或自定义基类
- 通过依赖注入获取Service Module实例
- 集成界面逻辑与业务调用，避免过度抽象

### 2.3 Service Module (服务模块层)

**职责**: 后端API服务调用和数据处理
- REST API调用封装 (使用Refit)
- 数据格式转换和验证
- 网络异常处理和重试
- 本地数据缓存管理
- 认证状态管理

**核心能力**:
- **API调用**: 使用Refit进行类型安全的REST调用
- **数据管理**: CRUD操作、分页查询、批量处理
- **缓存策略**: 智能本地缓存和过期管理
- **状态通知**: 通过事件通知界面数据变更

### 2.4 Model (数据模型层)

**职责**: 数据传输和结构定义
- DTO类定义 (Data Transfer Objects)
- 业务实体映射
- 数据验证注解

**包含类型**:
- `XxxDto`: 查询和显示用DTO
- `XxxCreateDto`: 创建操作DTO  
- `XxxUpdateDto`: 更新操作DTO
- `XxxPagedQueryDto`: 分页查询参数

---

## 3. 实现模式

### 3.1 标准ViewModel实现模板

```csharp
/// <summary>
/// [视图名]视图模型 - 负责[界面功能]的界面逻辑和业务调用
/// </summary>
public class [View]ViewModel : NewBaseListViewModel<[Module]Dto>
{
    #region Fields
    private readonly [Module]Module _moduleService;
    private readonly ICustomDialogService _dialogService;
    private readonly IMapper _mapper;
    private [Module]Dto? _selectedItem;
    #endregion

    #region Constructor
    public [View]ViewModel(
        [Module]Module moduleService,
        ICustomDialogService dialogService,
        IMapper mapper,
        ISessionManager sessionManager,
        INotificationService notificationService,
        ILogger<[View]ViewModel> logger,
        IPaginationCoordinator? paginationCoordinator = null,
        ISearchManager? searchManager = null)
        : base(sessionManager, notificationService, logger, paginationCoordinator, searchManager)
    {
        _moduleService = moduleService ?? throw new ArgumentNullException(nameof(moduleService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        
        InitializeCommands();
    }
    #endregion

    #region Properties
    /// <summary>选中的项目 - 直接使用DTO</summary>
    public [Module]Dto? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
            {
                // 更新命令状态
                EditCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
                ViewDetailsCommand.RaiseCanExecuteChanged();
            }
        }
    }
    #endregion

    #region Commands
    public DelegateCommand AddCommand { get; private set; } = null!;
    public DelegateCommand<[Module]Dto> EditCommand { get; private set; } = null!;
    public DelegateCommand<[Module]Dto> DeleteCommand { get; private set; } = null!;
    public DelegateCommand<[Module]Dto> ViewDetailsCommand { get; private set; } = null!;
    #endregion

    #region Command Initialization
    protected override void InitializeCommands()
    {
        AddCommand = new DelegateCommand(async () => await AddItemAsync());
        EditCommand = new DelegateCommand<[Module]Dto>(async item => await EditItemAsync(item), CanExecuteItemCommand);
        DeleteCommand = new DelegateCommand<[Module]Dto>(async item => await DeleteItemAsync(item), CanExecuteItemCommand);
        ViewDetailsCommand = new DelegateCommand<[Module]Dto>(async item => await ViewDetailsAsync(item), CanExecuteItemCommand);
        
        base.InitializeCommands();
    }

    private bool CanExecuteItemCommand([Module]Dto item)
    {
        return item != null && !IsLoading;
    }
    #endregion

    #region Data Loading Override
    protected override async Task<ServiceResult<PagedResult<[Module]Dto>>> LoadDataAsync(PagedQueryBaseDto request)
    {
        // 转换为具体的查询DTO
        var moduleQuery = new [Module]PagedQueryDto
        {
            Keyword = request.Keyword,
            PageIndex = request.PageIndex,
            PageSize = request.PageSize,
            SortField = request.SortField,
            IsDescending = request.IsDescending
        };
        return await _moduleService.GetPagedAsync(moduleQuery);
    }
    #endregion

    #region CRUD Operations
    private async Task AddItemAsync()
    {
        try
        {
            var parameters = new Dictionary<string, object>
            {
                ["IsEditMode"] = false
            };
            
            var result = await _dialogService.ShowDialogAsync("[Module]AddEditDialog", parameters);
            
            if (result.Result == true)
            {
                await RefreshDataAsync();
                await _dialogService.ShowSuccessAsync("添加成功", "成功");
            }
        }
        catch (Exception ex)
        {
            LogError(ex, "添加项目失败");
            ShowError($"添加失败: {ex.Message}");
            await _dialogService.ShowErrorAsync($"添加失败: {ex.Message}", "错误");
        }
    }

    private async Task EditItemAsync([Module]Dto item)
    {
        if (item == null) return;
        
        try
        {
            var parameters = new Dictionary<string, object>
            {
                ["IsEditMode"] = true,
                ["Item"] = item
            };
            
            var result = await _dialogService.ShowDialogAsync("[Module]AddEditDialog", parameters);
            
            if (result.Result == true)
            {
                await RefreshDataAsync();
                await _dialogService.ShowSuccessAsync($"{item.Name} 更新成功", "成功");
            }
        }
        catch (Exception ex)
        {
            LogError(ex, "编辑项目失败: {ItemId}", item.Id);
            ShowError($"编辑失败: {ex.Message}");
            await _dialogService.ShowErrorAsync($"编辑失败: {ex.Message}", "错误");
        }
    }

    private async Task DeleteItemAsync([Module]Dto item)
    {
        if (item == null) return;
        
        var confirm = await _dialogService.ShowConfirmationAsync(
            $"确定要删除 {item.Name} 吗？",
            "确认删除");

        if (confirm)
        {
            try
            {
                var result = await _moduleService.DeleteAsync(item.Id);
                
                if (result.IsSuccess)
                {
                    await RefreshDataAsync();
                    await _dialogService.ShowInformationAsync("删除成功", "成功");
                }
                else
                {
                    await _dialogService.ShowErrorAsync(
                        result.ErrorMessage ?? "删除失败",
                        "错误");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "删除项目失败: {ItemId}", item.Id);
                ShowError($"删除失败: {ex.Message}");
                await _dialogService.ShowErrorAsync($"删除失败: {ex.Message}", "错误");
            }
        }
    }

    private async Task ViewDetailsAsync([Module]Dto item)
    {
        if (item == null) return;

        try
        {
            var result = await _moduleService.GetByIdAsync(item.Id);
            
            if (result.IsSuccess && result.Data != null)
            {
                var detailInfo = $"详细信息：\n\n" +
                               $"名称: {result.Data.Name}\n" +
                               $"状态: {(result.Data.Status == CommonStatus.Enabled ? "正常" : "禁用")}\n" +
                               // 添加更多字段...
                               ;

                await _dialogService.ShowInformationAsync(detailInfo, $"详情 - {result.Data.Name}");
            }
            else
            {
                await _dialogService.ShowErrorAsync(
                    result.ErrorMessage ?? "获取详情失败", 
                    "错误");
            }
        }
        catch (Exception ex)
        {
            LogError(ex, "查看详情失败: {ItemId}", item.Id);
            ShowError($"查看详情失败: {ex.Message}");
            await _dialogService.ShowErrorAsync($"查看详情失败: {ex.Message}", "错误");
        }
    }
    #endregion
}
```

### 3.2 标准Service Module实现模板

```csharp
/// <summary>
/// [模块名]服务模块 - 负责[业务域]的API调用和数据处理
/// </summary>
public class [Module]Module
{
    #region Fields
    private readonly I[Module]Api _api;
    private readonly ILogger<[Module]Module> _logger;
    private readonly IMemoryCache _cache;
    private readonly IMapper _mapper;
    #endregion

    #region Events
    public event EventHandler<([Module]Dto Data, string Operation)>? DataChanged;
    public event EventHandler<(bool IsConnected, string Message)>? ApiConnectionChanged;
    #endregion

    #region Constructor
    public [Module]Module(
        I[Module]Api api,
        ILogger<[Module]Module> logger,
        IMemoryCache cache,
        IMapper mapper)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }
    #endregion

    #region Query Operations
    public async Task<ServiceResult<PagedResult<[Module]Dto>>> GetPagedAsync([Module]PagedQueryDto query)
    {
        try
        {
            _logger.LogInformation("开始分页查询[业务实体]，页码: {Page}", query.PageIndex);
            
            var response = await _api.GetPagedAsync(query);
            
            if (response.IsSuccessStatusCode && response.Content != null)
            {
                var apiResponse = response.Content;
                if (apiResponse.Success && apiResponse.Data != null)
                {
                    // 缓存结果
                    foreach (var item in apiResponse.Data.Items)
                    {
                        var cacheKey = $"[module]:{item.Id}";
                        _cache.Set(cacheKey, item, TimeSpan.FromMinutes(10));
                    }
                    
                    return ServiceResult<PagedResult<[Module]Dto>>.Success(apiResponse.Data);
                }
                return ServiceResult<PagedResult<[Module]Dto>>.Failure(apiResponse.Message ?? "查询失败");
            }
            
            return ServiceResult<PagedResult<[Module]Dto>>.Failure($"API调用失败: {response.Error?.Content}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[业务实体]分页查询异常");
            return ServiceResult<PagedResult<[Module]Dto>>.Failure($"查询异常: {ex.Message}");
        }
    }

    public async Task<ServiceResult<[Module]Dto>> GetByIdAsync(Guid id)
    {
        try
        {
            // 先检查缓存
            var cacheKey = $"[module]:{id}";
            if (_cache.TryGetValue(cacheKey, out [Module]Dto cachedItem))
            {
                return ServiceResult<[Module]Dto>.Success(cachedItem);
            }
            
            var response = await _api.GetByIdAsync(id);
            
            if (response.IsSuccessStatusCode && response.Content != null)
            {
                var apiResponse = response.Content;
                if (apiResponse.Success && apiResponse.Data != null)
                {
                    // 更新缓存
                    _cache.Set(cacheKey, apiResponse.Data, TimeSpan.FromMinutes(10));
                    return ServiceResult<[Module]Dto>.Success(apiResponse.Data);
                }
                return ServiceResult<[Module]Dto>.Failure(apiResponse.Message ?? "获取失败");
            }
            
            return ServiceResult<[Module]Dto>.Failure($"API调用失败: {response.Error?.Content}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取[业务实体]异常: {Id}", id);
            return ServiceResult<[Module]Dto>.Failure($"获取异常: {ex.Message}");
        }
    }
    #endregion

    #region CRUD Operations
    public async Task<ServiceResult<[Module]Dto>> CreateAsync([Module]CreateDto createDto)
    {
        try
        {
            _logger.LogInformation("创建[业务实体]: {Name}", createDto.Name);
            
            var response = await _api.CreateAsync(createDto);
            
            if (response.IsSuccessStatusCode && response.Content != null)
            {
                var apiResponse = response.Content;
                if (apiResponse.Success && apiResponse.Data != null)
                {
                    // 触发数据变更事件
                    DataChanged?.Invoke(this, (apiResponse.Data, "Created"));
                    return ServiceResult<[Module]Dto>.Success(apiResponse.Data);
                }
                return ServiceResult<[Module]Dto>.Failure(apiResponse.Message ?? "创建失败");
            }
            
            return ServiceResult<[Module]Dto>.Failure($"API调用失败: {response.Error?.Content}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建[业务实体]异常");
            return ServiceResult<[Module]Dto>.Failure($"创建异常: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("删除[业务实体]: {Id}", id);
            
            var response = await _api.DeleteAsync(id);
            
            if (response.IsSuccessStatusCode && response.Content != null)
            {
                var apiResponse = response.Content;
                if (apiResponse.Success)
                {
                    // 清除缓存
                    var cacheKey = $"[module]:{id}";
                    _cache.Remove(cacheKey);
                    
                    // 触发数据变更事件
                    DataChanged?.Invoke(this, (new [Module]Dto { Id = id }, "Deleted"));
                    return ServiceResult<bool>.Success(true);
                }
                return ServiceResult<bool>.Failure(apiResponse.Message ?? "删除失败");
            }
            
            return ServiceResult<bool>.Failure($"API调用失败: {response.Error?.Content}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除[业务实体]异常: {Id}", id);
            return ServiceResult<bool>.Failure($"删除异常: {ex.Message}");
        }
    }
    #endregion
}
```

---

## 4. 实际代码示例

### 4.1 PatientManagementViewModel实现 (338行)

**特征**: 标准CRUD操作，继承自NewBaseListViewModel基类

```csharp
public class PatientManagementViewModel : NewBaseListViewModel<PatientDto>
{
    private readonly PatientModule _patientService;  // 直接注入Service模块
    private readonly ICustomDialogService _dialogService;
    private readonly IMapper _mapper;
    private PatientDto? _selectedPatient;

    // 核心功能:
    // 1. 分页查询和搜索 (基类提供)
    // 2. 标准CRUD操作 (AddPatient/EditPatient/DeletePatient)
    // 3. 状态管理 (ToggleStatus - 启用/禁用)
    // 4. 详情查看 (ViewDetails)
    // 5. 异常处理和用户提示

    // UltraThink v2.0简化特征:
    // - 直接使用DTO，移除复杂的ViewModel包装
    // - 删除批量操作功能 (20人以下小诊所不需要)
    // - 删除多选功能 (避免过度设计)
    // - 基类提供标准分页和搜索功能
}
```

### 4.2 LoginViewModel实现 (380行)

**特征**: 认证专用ViewModel，事件驱动设计

```csharp
public class LoginViewModel : ModernViewModelBase
{
    private readonly AuthModule _authModule;  // 直接注入AuthModule服务
    private readonly IMapper _mapper;
    
    // 核心功能:
    // 1. 用户登录验证
    // 2. 记住我功能 (凭据保存和加载)
    // 3. API连接状态监控
    // 4. 认证状态事件处理
    // 5. 异步登录流程管理

    // 事件驱动特征:
    // - 订阅AuthModule的状态变更事件
    // - 使用EventAggregator进行模块间通信
    // - UI线程安全的状态更新处理
}
```

### 4.3 AuthModule服务实现

**特征**: 认证服务模块，封装API调用和状态管理

```csharp
public class AuthModule
{
    private readonly IAuthApi _authApi;        // Refit生成的API接口
    private readonly ITokenManager _tokenManager;
    private readonly ICredentialManager _credentialManager;
    private readonly ILogger<AuthModule> _logger;

    // 事件通知系统:
    public event EventHandler<(bool IsLoggedIn, string? Username, string? Message)>? AuthStatusChanged;
    public event EventHandler<(bool IsConnected, string Message)>? ApiConnectionChanged;

    // 核心功能:
    // 1. 用户认证 - LoginAsync()
    // 2. Token管理 - RefreshTokenAsync()
    // 3. 凭据管理 - LoadSavedCredentials()
    // 4. API连接监控 - StartApiHealthCheck()
    // 5. 异常处理和重试机制
}
```

---

## 5. 开发规范

### 5.1 命名约定

| 组件类型 | 命名格式 | 示例 |
|---------|---------|------|
| ViewModel | `[View]ViewModel` | `PatientManagementViewModel` |
| Service Module | `[Module]Module` | `PatientModule` |
| API Interface | `I[Module]Api` | `IPatientApi` |
| View | `[Module][Purpose]View` | `PatientManagementView` |
| DTO | `[Module]Dto` | `PatientDto` |
| CreateDTO | `[Module]CreateDto` | `PatientCreateDto` |
| UpdateDTO | `[Module]UpdateDto` | `PatientUpdateDto` |
| QueryDTO | `[Module]PagedQueryDto` | `PatientPagedQueryDto` |

### 5.2 文件组织

```
src/Client/Desktop/Modules/[ModuleName]/
├── Api/
│   └── I[Module]Api.cs              # Refit API接口定义
├── ViewModels/
│   ├── [Module]ManagementViewModel.cs
│   ├── [Module]DetailViewModel.cs
│   └── [Module]AddEditDialogViewModel.cs
├── Views/
│   ├── [Module]ManagementView.xaml
│   ├── [Module]DetailView.xaml
│   └── [Module]AddEditDialog.xaml
├── Services/
│   └── [Module]Module.cs            # 服务模块
└── [ModuleName]Module.cs            # Prism模块注册
```

### 5.3 依赖注入配置

```csharp
// [ModuleName]Module.cs - Prism模块注册
public class PatientsModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册Service Module (Singleton - 状态保持和缓存)
        containerRegistry.RegisterSingleton<PatientModule>();
        
        // 注册ViewModel (Transient - 每次导航新实例)
        containerRegistry.RegisterTransient<PatientManagementViewModel>();
        containerRegistry.RegisterTransient<PatientAddEditDialogViewModel>();
        
        // 注册API接口
        containerRegistry.RegisterInstance(
            RestService.For<IPatientApi>("https://localhost:7001"));
        
        // 注册视图
        containerRegistry.RegisterForNavigation<PatientManagementView>();
    }
}
```

### 5.4 事件处理规范

```csharp
// 服务模块中的事件定义
public event EventHandler<(PatientDto Data, string Operation)>? DataChanged;
public event EventHandler<(bool IsConnected, string Message)>? ApiConnectionChanged;

// ViewModel中订阅事件 (构造函数)
_patientService.DataChanged += OnDataChanged;
_patientService.ApiConnectionChanged += OnApiConnectionChanged;

// 事件处理 (UI线程安全)
private void OnDataChanged(object? sender, (PatientDto Data, string Operation) e)
{
    if (Application.Current?.Dispatcher?.CheckAccess() == false)
    {
        Application.Current.Dispatcher.BeginInvoke(() => OnDataChanged(sender, e));
        return;
    }
    
    // 更新界面数据
    if (e.Operation == "Created" || e.Operation == "Updated")
    {
        await RefreshDataAsync();
    }
}

// 事件取消订阅 (Dispose中)
protected override void OnDisposing()
{
    if (_patientService != null)
    {
        _patientService.DataChanged -= OnDataChanged;
        _patientService.ApiConnectionChanged -= OnApiConnectionChanged;
    }
    base.OnDisposing();
}
```

---

## 6. 性能优化

### 6.1 Service模块缓存策略

```csharp
public class PatientModule
{
    private readonly IMemoryCache _cache;
    
    // 智能缓存 - 先查缓存，再调用API
    public async Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id)
    {
        var cacheKey = $"patient:{id}";
        
        // 检查缓存
        if (_cache.TryGetValue(cacheKey, out PatientDto cachedItem))
        {
            _logger.LogDebug("从缓存获取患者数据: {Id}", id);
            return ServiceResult<PatientDto>.Success(cachedItem);
        }
        
        // API调用
        var response = await _api.GetByIdAsync(id);
        if (response.IsSuccessStatusCode && response.Content?.Success == true)
        {
            // 缓存10分钟
            _cache.Set(cacheKey, response.Content.Data, TimeSpan.FromMinutes(10));
            return ServiceResult<PatientDto>.Success(response.Content.Data);
        }
        
        return ServiceResult<PatientDto>.Failure("获取失败");
    }
    
    // 缓存失效策略
    public async Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto updateDto)
    {
        var result = await _api.UpdateAsync(id, updateDto);
        
        if (result.IsSuccessStatusCode && result.Content?.Success == true)
        {
            // 更新缓存
            var cacheKey = $"patient:{id}";
            _cache.Set(cacheKey, result.Content.Data, TimeSpan.FromMinutes(10));
            
            // 清除分页查询缓存
            _cache.Remove("patient:paged");
            
            return ServiceResult<PatientDto>.Success(result.Content.Data);
        }
        
        return ServiceResult<PatientDto>.Failure("更新失败");
    }
}
```

### 6.2 ViewModel性能优化

```csharp
public class PatientManagementViewModel : NewBaseListViewModel<PatientDto>
{
    // 虚拟化和分页结合
    protected override async Task<ServiceResult<PagedResult<PatientDto>>> LoadDataAsync(PagedQueryBaseDto request)
    {
        // 使用较小的页面大小，支持虚拟化滚动
        request.PageSize = Math.Min(request.PageSize, 50);
        
        var query = new PatientPagedQueryDto
        {
            Keyword = request.Keyword,
            PageIndex = request.PageIndex,
            PageSize = request.PageSize,
            SortField = request.SortField,
            IsDescending = request.IsDescending
        };
        
        return await _patientService.GetPagedAsync(query);
    }
    
    // 防抖搜索
    private Timer? _searchTimer;
    public string SearchKeyword
    {
        get => SearchManager.SearchKeyword;
        set
        {
            if (SearchManager.SearchKeyword != value)
            {
                SearchManager.SearchKeyword = value;
                RaisePropertyChanged();
                
                // 防抖500ms
                _searchTimer?.Dispose();
                _searchTimer = new Timer(async _ => await SearchManager.ExecuteSearchAsync(), 
                                       null, 500, Timeout.Infinite);
            }
        }
    }
    
    // 异步操作状态管理
    private readonly SemaphoreSlim _operationSemaphore = new(1, 1);
    
    private async Task ExecuteActionWithLockAsync(Func<Task> action)
    {
        if (!await _operationSemaphore.WaitAsync(100)) return; // 防止重复点击
        
        try
        {
            IsLoading = true;
            await action();
        }
        finally
        {
            IsLoading = false;
            _operationSemaphore.Release();
        }
    }
}
```

### 6.3 UI线程优化

```csharp
// 大数据量处理时的UI响应性优化
private async Task ProcessLargeDataAsync()
{
    const int batchSize = 100;
    var totalItems = Data?.Items?.Count ?? 0;
    
    for (int i = 0; i < totalItems; i += batchSize)
    {
        var batch = Data.Items.Skip(i).Take(batchSize);
        
        // 处理批次数据
        foreach (var item in batch)
        {
            // 处理逻辑
            ProcessItem(item);
        }
        
        // 让出UI线程控制权
        await Task.Yield();
        
        // 更新进度
        var progress = Math.Min(100, (i + batchSize) * 100 / totalItems);
        ProgressValue = progress;
    }
}

// 异步命令优化
public DelegateCommand RefreshCommand => _refreshCommand ??= new DelegateCommand(
    async () => await RefreshDataAsync(), 
    () => !IsLoading);  // 防止重复执行
```

---

## 7. 测试策略

### 7.1 Service Module单元测试

```csharp
[Test]
public async Task GetByIdAsync_WithCache_ShouldReturnCachedData()
{
    // Arrange
    var mockApi = new Mock<IPatientApi>();
    var mockLogger = new Mock<ILogger<PatientModule>>();
    var mockCache = new Mock<IMemoryCache>();
    var mockMapper = new Mock<IMapper>();
    
    var cachedPatient = new PatientDto { Id = Guid.NewGuid(), Name = "缓存患者" };
    mockCache.Setup(x => x.TryGetValue("patient:" + cachedPatient.Id, out It.Ref<object?>.IsAny))
           .Returns((string key, out object? value) =>
           {
               value = cachedPatient;
               return true;
           });
    
    var service = new PatientModule(mockApi.Object, mockLogger.Object, mockCache.Object, mockMapper.Object);
    
    // Act
    var result = await service.GetByIdAsync(cachedPatient.Id);
    
    // Assert
    Assert.IsTrue(result.IsSuccess);
    Assert.AreEqual(cachedPatient.Name, result.Data?.Name);
    mockApi.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never); // 不应该调用API
}
```

### 7.2 ViewModel单元测试

```csharp
[Test]
public async Task LoadDataAsync_ValidRequest_ShouldLoadItems()
{
    // Arrange
    var mockPatientService = new Mock<PatientModule>();
    var mockDialogService = new Mock<ICustomDialogService>();
    var mockMapper = new Mock<IMapper>();
    var mockSessionManager = new Mock<ISessionManager>();
    var mockNotificationService = new Mock<INotificationService>();
    var mockLogger = new Mock<ILogger<PatientManagementViewModel>>();
    
    var expectedResult = new PagedResult<PatientDto>
    {
        Items = [new PatientDto { Name = "测试患者" }],
        TotalCount = 1
    };
    
    mockPatientService.Setup(x => x.GetPagedAsync(It.IsAny<PatientPagedQueryDto>()))
                    .ReturnsAsync(ServiceResult<PagedResult<PatientDto>>.Success(expectedResult));
    
    var viewModel = new PatientManagementViewModel(
        mockPatientService.Object, 
        mockDialogService.Object, 
        mockMapper.Object,
        mockSessionManager.Object,
        mockNotificationService.Object,
        mockLogger.Object);
    
    // Act
    await viewModel.RefreshDataAsync();
    
    // Assert
    Assert.AreEqual(1, viewModel.Data?.Items?.Count);
    Assert.AreEqual("测试患者", viewModel.Data.Items[0].Name);
    Assert.IsFalse(viewModel.IsLoading);
}
```

### 7.3 集成测试策略

```csharp
[Test]
public async Task FullWorkflow_CreatePatient_ShouldUpdateUI()
{
    // 测试完整的创建患者工作流
    // 1. 打开添加对话框
    // 2. 填写患者信息
    // 3. 保存患者
    // 4. 验证UI更新
    // 5. 验证缓存更新
    
    // 这种测试需要模拟整个MVVM流程
    // 包括ViewModel -> Service -> API -> 缓存 -> 事件通知 -> UI更新
}
```

---

## 8. 最佳实践总结

### 8.1 设计原则 ✅

- **🎯 实用优先**: 避免过度设计，专注业务价值
- **🔄 简洁架构**: 三层结构，职责清晰分离
- **📦 模块化**: Prism模块化设计，松耦合
- **⚡ 性能优先**: 智能缓存和异步操作
- **🧪 可测试性**: Service和ViewModel独立测试

### 8.2 强制性规则 ⚠️

- **Service生命周期**: Service为Singleton，ViewModel为Transient
- **事件订阅**: 必须在OnDisposing中取消订阅
- **异常处理**: 所有async方法必须try-catch
- **UI线程安全**: 事件处理必须检查线程安全
- **缓存策略**: 写操作必须清除相关缓存

### 8.3 性能要求 ⚡

- **响应时间**: UI操作响应<200ms，API调用<2s
- **内存使用**: Service模块缓存合理控制，定期清理
- **并发支持**: 支持多个ViewModel并发操作
- **缓存命中率**: 常用数据缓存命中率>70%

### 8.4 适用场景 🏥

- **团队规模**: 2-5人小团队开发
- **用户规模**: <20人并发使用
- **业务复杂度**: 中等复杂度业务场景
- **维护要求**: 易理解、易维护、易扩展

---

## 9. 版本历史

| 版本 | 日期 | 作者 | 变更说明 |
|-----|------|------|---------|
| v1.0 | 2025-09-01 | UltraThink | 创建MVVM-C架构标准文档 |
| v2.0 | 2025-09-01 | UltraThink | 重构为MVVM架构，移除Coordinator层，体现实际实现 |

---

**文档状态**: ✅ **v2.0完成** - 架构验证和实现对齐完成  
**适用范围**: 凌隐宝堂中医诊所系统8个前端模块统一架构标准  
**下一步**: 继续完善API规格文档，确保前后端架构文档完整性