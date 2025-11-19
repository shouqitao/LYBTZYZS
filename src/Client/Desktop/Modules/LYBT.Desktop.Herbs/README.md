# LYBT.Desktop.Herbs - 中药材管理模块

## 📦 项目定位

- **层级**: Client端
- **类型**: 业务模块(中药材管理)
- **职责**: 提供中药材信息的完整UI管理界面,支持药材档案的增删改查、拼音快速检索、批量导入导出、价格维护、状态管理等功能。作为处方系统和验方系统的基础数据支撑模块,本模块采用**MVVM架构 + Repository模式**,通过IHerbRepository与Server端交互,实现药材信息的可视化管理。特别适合中医诊所的药材档案管理需求（Record-Only模式,不涉及库存管理）。

## 📂 代码结构

```
LYBT.Desktop.Herbs/
├── Interfaces/                           # 接口定义（1个）
│   └── IHerbRepository.cs                # 药材Repository接口（6个方法）
├── Models/                               # 数据模型（1个）
│   └── HerbItem.cs                       # 药材项模型（用于UI数据绑定）
├── Repositories/                         # 数据访问层（1个）
│   └── HerbRepository.cs                 # 药材Repository实现（继承BaseApiRepository）
├── ViewModels/                           # MVVM视图模型（2个）
│   ├── HerbManagementViewModel.cs        # 药材列表管理ViewModel（487行，19命令+17方法）
│   └── HerbDetailViewModel.cs            # 药材详情ViewModel（478行，16属性+15方法）
├── Views/                                # WPF视图（4个）
│   ├── HerbManagementView.xaml           # 药材列表管理视图
│   ├── HerbManagementView.xaml.cs        # 药材列表管理视图代码
│   ├── HerbDetailView.xaml               # 药材详情视图
│   └── HerbDetailView.xaml.cs            # 药材详情视图代码
├── HerbsModule.cs                        # Prism模块注册（2个方法）
├── LYBT.Desktop.Herbs.csproj             # 项目文件
└── README.md                             # 本文档

总计: 5个目录, 12个文件
```

**说明**:
- **HerbsModule**: Prism模块注册,统一注册ViewModels、Views和Repository
- **HerbManagementViewModel**: 487行核心ViewModel,19个Command + 17个方法,覆盖药材列表管理、搜索、分页、批量操作
- **HerbDetailViewModel**: 478行详情ViewModel,16个属性 + 15个方法,支持药材编辑、保存、打印、使用历史查询
- **IHerbRepository**: 6个方法（GetPagedAsync, GetByIdAsync, CreateAsync, UpdateAsync, DeleteAsync, SearchAsync）
- **Repository模式**: 通过BaseApiRepository与Server端/api/v1/herbs交互（返回裸类型）
- **拼音检索**: 支持PinyinAbbreviation快速输入（如输入"dg"可匹配"当归"）
- **批量操作**: Excel导入/导出、批量删除、分类搜索
- **价格管理**: Price（售价）、CostPrice（成本价）双价格体系
- **状态管理**: Status枚举（Active, Inactive）控制药材启用/禁用

## 🔗 依赖关系

### 依赖的项目
1. **LYBT.Desktop.Core** - 核心库(UnifiedViewModelBase、IPagedDataManager)
2. **LYBT.Desktop.Foundation** - 基础设施(BaseApiRepository、ApiService)
3. **LYBT.Desktop.Contracts** - 接口契约(IDialogService、INavigationService)
4. **LYBT.Shared.Models** - 共享DTO模型(HerbDto、CreateHerbDto、UpdateHerbDto)
5. **Prism.DryIoc** - MVVM框架与依赖注入容器

### 被依赖项目
1. **LYBT.Desktop.Prescriptions** - 处方模块使用HerbSelectionDialog选择药材
2. **LYBT.Desktop.Formula** - 验方模块使用Herbs作为基础数据源
3. **LYBT.Desktop.Shell** - Shell通过RegionManager加载Herbs模块
4. **LYBT.WebAPI** - Server端通过/api/v1/herbs提供API端点

### NuGet包
- **Prism.DryIoc** (8.x) - MVVM框架和依赖注入容器
- **MaterialDesignThemes** (5.1.x) - Material Design UI组件库
- **Microsoft.Extensions.Logging** (8.0.x) - 日志框架
- **Newtonsoft.Json** (13.0.x) - JSON序列化与反序列化

## 🛠 技术栈

- **.NET 8 & WPF**: Windows桌面应用基础框架
- **Prism.DryIoc 8.x**: MVVM框架,提供模块化、区域导航、命令、事件聚合器
- **MaterialDesignThemes 5.1.x**: Material Design风格的UI组件库（DataGrid、Button、TextBox等）
- **ObservableCollection**: WPF数据绑定核心,支持集合变更通知
- **ICommand / AsyncDelegateCommand**: Prism异步命令模式
- **Repository Pattern**: 通过IHerbRepository抽象数据访问层,实现与Server端解耦
- **BaseApiRepository**: Foundation提供的HTTP通信基类,返回裸类型（非Result<T>）
- **LINQ**: 用于集合过滤、分页、排序
- **Async/Await**: 全异步方法,提升UI响应性

##  快速开始

此项目是一个Prism模块库,由 `LYBT.Desktop.Shell` 在启动时加载。无法独立运行。

```bash
# 构建此项目
dotnet build src/Client/Desktop/Modules/LYBT.Desktop.Herbs/LYBT.Desktop.Herbs.csproj
```

**集成说明**:

### 1. Shell加载Herbs模块(在App.xaml.cs中)

```csharp
// Shell项目的App.xaml.cs
protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
{
    // 药材模块（WhenAvailable模式，Shell启动时立即加载）
    moduleCatalog.AddModule<HerbsModule>(InitializationMode.WhenAvailable);
}
```

### 2. HerbManagementViewModel核心属性与方法

**核心命令（19个Command）**:

| 命令名称 | 类型 | 说明 | 触发条件 |
|---------|------|------|---------|
| **AddCommand** | AsyncDelegateCommand | 打开HerbDetailView创建新药材 | 总是可用 |
| **DeleteCommand** | AsyncDelegateCommand | 删除选中的药材 | SelectedHerb != null |
| **EditCommand** | AsyncDelegateCommand | 编辑选中的药材（导航到HerbDetailView） | SelectedHerb != null |
| **RefreshCommand** | AsyncDelegateCommand | 刷新药材列表 | 总是可用 |
| **SearchCommand** | AsyncDelegateCommand | 按名称/拼音搜索药材 | SearchText != null |
| **NextPageCommand** | AsyncDelegateCommand | 下一页 | CurrentPage < TotalPages |
| **PreviousPageCommand** | AsyncDelegateCommand | 上一页 | CurrentPage > 1 |
| **EditHerbCommand** | DelegateCommand | 编辑药材（Region导航） | SelectedHerb != null |
| **ViewDetailCommand** | DelegateCommand | 查看药材详情（Region导航） | SelectedHerb != null |
| **CopyHerbCommand** | DelegateCommand | 复制药材为新药材 | SelectedHerb != null |
| **SearchByCategoryCommand** | AsyncDelegateCommand | 按分类搜索药材 | Category != null |
| **ToggleStatusCommand** | AsyncDelegateCommand | 切换药材状态（启用/禁用） | SelectedHerb != null |
| **ImportHerbsCommand** | AsyncDelegateCommand | 从Excel导入药材 | 总是可用 |
| **ExportTemplateCommand** | AsyncDelegateCommand | 导出Excel导入模板 | 总是可用 |
| **ExportHerbsCommand** | AsyncDelegateCommand | 导出药材到Excel | Herbs.Count > 0 |
| **FirstPageCommand** | DelegateCommand | 第一页 | CurrentPage > 1 |
| **LastPageCommand** | DelegateCommand | 最后一页 | CurrentPage < TotalPages |

**核心方法（17个Method）**:

| 方法名称 | 返回类型 | 说明 | 核心逻辑 |
|---------|---------|------|---------|
| **GetItemsAsync** | Task<PagedResult<HerbDto>> | 分页加载药材列表 | 调用IHerbRepository.GetPagedAsync |
| **InitializeAsync** | Task | 初始化ViewModel | 调用LoadPageAsync(1) |
| **InitializeCustomCommands** | void | 初始化自定义命令 | 注册19个Command |
| **OnExecuteAddAsync** | Task | 执行添加药材命令 | 导航到HerbDetailView（新建模式） |
| **OnExecuteDeleteAsync** | Task | 执行删除药材命令 | 调用IHerbRepository.DeleteAsync |
| **OnExecuteBatchDeleteAsync** | Task | 执行批量删除命令 | 调用IHerbRepository.DeleteAsync批量 |
| **EditHerb** | void | 编辑药材 | 导航到HerbDetailView（编辑模式） |
| **CopyHerb** | void | 复制药材 | 导航到HerbDetailView（复制模式,清空Id） |
| **ViewHerbDetail** | void | 查看药材详情 | 导航到HerbDetailView（只读模式） |
| **CanViewDetail** | bool | 是否可以查看详情 | SelectedHerb != null |
| **CanEditHerb** | bool | 是否可以编辑 | SelectedHerb != null |
| **CanCopyHerb** | bool | 是否可以复制 | SelectedHerb != null |
| **SearchByCategory** | Task | 按分类搜索 | 调用IHerbRepository.SearchAsync |
| **ToggleStatusAsync** | Task | 切换药材状态 | 更新Status字段后调用UpdateAsync |
| **ImportHerbsAsync** | Task | 导入Excel药材 | 调用Server端/api/v1/herbs/import |
| **ExportTemplateAsync** | Task | 导出导入模板 | 生成Excel模板文件 |
| **ExportHerbsAsync** | Task | 导出药材列表 | 调用Server端/api/v1/herbs/export |

### 3. HerbDetailViewModel核心属性与方法

**核心属性（16个Property）**:

| 属性名称 | 类型 | 说明 | 默认值 |
|---------|------|------|--------|
| **Herb** | HerbDto | 当前编辑的药材对象 | null |
| **Name** | string | 药材名称（双向绑定） | string.Empty |
| **PinYinCode** | string | 拼音首字母（快速检索） | string.Empty |
| **Origin** | string | 产地 | string.Empty |
| **Spec** | string | 规格 | string.Empty |
| **Unit** | string | 计量单位（如:克、两） | string.Empty |
| **Price** | decimal | 售价（元/单位） | 0 |
| **CostPrice** | decimal | 成本价（元/单位） | 0 |
| **Effect** | string | 功效 | string.Empty |
| **Usage** | string | 用法 | string.Empty |
| **Remark** | string | 备注 | string.Empty |
| **Status** | HerbStatus | 状态（Active/Inactive） | Active |
| **StatusOptions** | ObservableCollection<HerbStatus> | 状态下拉选项 | [Active, Inactive] |
| **SaveCommand** | DelegateCommand | 保存药材命令 | - |
| **CancelCommand** | DelegateCommand | 取消编辑命令 | - |
| **BackCommand** | DelegateCommand | 返回列表命令 | - |

**核心方法（15个Method）**:

| 方法名称 | 返回类型 | 说明 | 核心逻辑 |
|---------|---------|------|---------|
| **HerbDetailViewModel** | - | 构造函数 | 初始化Command,注入IHerbRepository |
| **SaveHerbAsync** | Task | 保存药材 | 新建:CreateAsync / 更新:UpdateAsync |
| **CanSave** | bool | 是否可以保存 | Name不为空 && Price >= 0 |
| **Cancel** | void | 取消编辑 | 重置字段,返回列表 |
| **EnableEdit** | void | 启用编辑模式 | IsReadOnly = false |
| **CanEdit** | bool | 是否可以编辑 | Herb != null && !IsReadOnly |
| **ExecutePrint** | void | 打印药材信息 | 生成打印预览 |
| **CanPrint** | bool | 是否可以打印 | Herb != null |
| **ExecuteViewUsageHistory** | void | 查看使用历史 | 查询Herb在Prescriptions中的使用 |
| **CanViewUsageHistory** | bool | 是否可以查看使用历史 | Herb != null && Herb.Id != Guid.Empty |
| **LoadHerbAsync** | Task | 加载药材详情 | 调用IHerbRepository.GetByIdAsync |
| **LoadFromDto** | void | 从DTO加载数据 | 映射HerbDto到ViewModel属性 |
| **NavigateToHerbManagement** | void | 导航到药材列表 | RegionManager.RequestNavigate |

### 4. 药材列表管理 - 分页、搜索、打印

```csharp
/// <summary>
/// HerbManagementViewModel - 药材列表管理核心ViewModel
/// 功能:分页查询、搜索、批量删除、导入导出、分类筛选、状态切换
/// </summary>
public class HerbManagementViewModel : UnifiedViewModelBase, IPagedDataManager<HerbDto>
{
    private readonly IHerbRepository _herbRepository;
    private readonly IDialogService _dialogService;
    private readonly IRegionManager _regionManager;
    private readonly ILogger<HerbManagementViewModel> _logger;

    // 药材列表
    public ObservableCollection<HerbDto> Herbs { get; set; }
    public HerbDto? SelectedHerb { get; set; }

    // 搜索与分页
    public string SearchText { get; set; }          // 搜索关键字（名称/拼音）
    public string SelectedCategory { get; set; }    // 选中的分类
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    // 命令
    public AsyncDelegateCommand AddCommand { get; }
    public AsyncDelegateCommand DeleteCommand { get; }
    public AsyncDelegateCommand<HerbDto> EditCommand { get; }
    public AsyncDelegateCommand RefreshCommand { get; }
    public AsyncDelegateCommand SearchCommand { get; }
    public AsyncDelegateCommand ImportHerbsCommand { get; }
    public AsyncDelegateCommand ExportHerbsCommand { get; }

    public HerbManagementViewModel(
        IHerbRepository herbRepository,
        IDialogService dialogService,
        IRegionManager regionManager,
        ILogger<HerbManagementViewModel> logger)
    {
        _herbRepository = herbRepository;
        _dialogService = dialogService;
        _regionManager = regionManager;
        _logger = logger;

        Herbs = new ObservableCollection<HerbDto>();
        InitializeCustomCommands();
    }

    /// <summary>
    /// 初始化ViewModel（加载第一页数据）
    /// </summary>
    public override async Task InitializeAsync()
    {
        await LoadPageAsync(1);
    }

    /// <summary>
    /// 分页加载药材列表（支持搜索）
    /// </summary>
    public async Task<PagedResult<HerbDto>> GetItemsAsync(int pageIndex, int pageSize)
    {
        var result = await _herbRepository.GetPagedAsync(
            pageIndex,
            pageSize,
            SearchText // 支持名称/拼音搜索
        );

        TotalCount = result.TotalCount;
        CurrentPage = pageIndex;

        return result;
    }

    /// <summary>
    /// 添加新药材（导航到HerbDetailView）
    /// </summary>
    private async Task OnExecuteAddAsync()
    {
        var parameters = new NavigationParameters
        {
            { "Mode", "Create" }
        };

        _regionManager.RequestNavigate("MainRegion", "HerbDetailView", parameters);
    }

    /// <summary>
    /// 删除药材（带确认对话框）
    /// </summary>
    private async Task OnExecuteDeleteAsync()
    {
        if (SelectedHerb == null) return;

        var result = await _dialogService.ShowConfirmationAsync(
            "确认删除",
            $"确定要删除药材 '{SelectedHerb.Name}' 吗？此操作不可恢复。"
        );

        if (result == ButtonResult.OK)
        {
            await _herbRepository.DeleteAsync(SelectedHerb.Id);
            await RefreshAsync();
            _logger.LogInformation($"药材已删除: {SelectedHerb.Name}");
        }
    }

    /// <summary>
    /// 按名称/拼音搜索药材
    /// 示例:输入"dg"可匹配"当归"
    /// </summary>
    private async Task ExecuteSearchAsync()
    {
        CurrentPage = 1; // 重置到第一页
        await LoadPageAsync(CurrentPage);
    }

    /// <summary>
    /// 切换药材状态（启用/禁用）
    /// </summary>
    private async Task ToggleStatusAsync()
    {
        if (SelectedHerb == null) return;

        // 切换状态
        SelectedHerb.Status = SelectedHerb.Status == HerbStatus.Active
            ? HerbStatus.Inactive
            : HerbStatus.Active;

        // 更新到Server
        var updateDto = new UpdateHerbDto
        {
            Status = SelectedHerb.Status
        };

        await _herbRepository.UpdateAsync(SelectedHerb.Id, updateDto);
        _logger.LogInformation($"药材状态已切换: {SelectedHerb.Name} -> {SelectedHerb.Status}");
    }
}
```

### 5. 药材详情编辑 - 双价格体系与拼音检索

```csharp
/// <summary>
/// HerbDetailViewModel - 药材详情编辑ViewModel
/// 功能:新建/编辑/复制药材,双价格体系(售价+成本价),拼音检索,状态管理
/// </summary>
public class HerbDetailViewModel : UnifiedViewModelBase
{
    private readonly IHerbRepository _herbRepository;
    private readonly IRegionManager _regionManager;
    private readonly ILogger<HerbDetailViewModel> _logger;

    // 药材基础信息
    public HerbDto Herb { get; set; }
    public string Name { get; set; }                // 药材名称
    public string PinYinCode { get; set; }          // 拼音首字母（快速检索）
    public string Origin { get; set; }              // 产地
    public string Spec { get; set; }                // 规格
    public string Unit { get; set; } = "克";        // 计量单位（默认:克）
    public decimal Price { get; set; }              // 售价（元/单位）
    public decimal CostPrice { get; set; }          // 成本价（元/单位）
    public string Effect { get; set; }              // 功效
    public string Usage { get; set; }               // 用法
    public string Remark { get; set; }              // 备注
    public HerbStatus Status { get; set; } = HerbStatus.Active; // 状态（默认:启用）

    // 命令
    public DelegateCommand SaveCommand { get; }
    public DelegateCommand CancelCommand { get; }
    public DelegateCommand PrintCommand { get; }
    public DelegateCommand ViewUsageHistoryCommand { get; }

    /// <summary>
    /// 保存药材（新建或更新）
    /// </summary>
    private async Task SaveHerbAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            await _dialogService.ShowAlertAsync("验证错误", "药材名称不能为空");
            return;
        }

        if (Price < 0 || CostPrice < 0)
        {
            await _dialogService.ShowAlertAsync("验证错误", "价格不能为负数");
            return;
        }

        // 利润率计算（可选）
        decimal profitMargin = Price > 0 ? (Price - CostPrice) / Price * 100 : 0;
        if (profitMargin < 0)
        {
            var result = await _dialogService.ShowConfirmationAsync(
                "价格警告",
                $"售价低于成本价,利润率为 {profitMargin:F2}%,是否继续保存？"
            );
            if (result != ButtonResult.OK) return;
        }

        if (Herb == null || Herb.Id == Guid.Empty)
        {
            // 新建药材
            var createDto = new CreateHerbDto
            {
                Name = Name,
                PinYinCode = PinYinCode,
                Origin = Origin,
                Spec = Spec,
                Unit = Unit,
                Price = Price,
                CostPrice = CostPrice,
                Effect = Effect,
                Usage = Usage,
                Remark = Remark,
                Status = Status
            };

            await _herbRepository.CreateAsync(createDto);
            _logger.LogInformation($"药材创建成功: {Name}");
        }
        else
        {
            // 更新药材
            var updateDto = new UpdateHerbDto
            {
                Name = Name,
                PinYinCode = PinYinCode,
                Origin = Origin,
                Spec = Spec,
                Unit = Unit,
                Price = Price,
                CostPrice = CostPrice,
                Effect = Effect,
                Usage = Usage,
                Remark = Remark,
                Status = Status
            };

            await _herbRepository.UpdateAsync(Herb.Id, updateDto);
            _logger.LogInformation($"药材更新成功: {Name}");
        }

        // 返回药材列表
        _regionManager.RequestNavigate("MainRegion", "HerbManagementView");
    }

    /// <summary>
    /// 查看药材使用历史（在处方中的使用情况）
    /// </summary>
    private async Task ExecuteViewUsageHistory()
    {
        if (Herb == null || Herb.Id == Guid.Empty) return;

        // 查询Herb在Prescriptions中的使用历史
        var parameters = new NavigationParameters
        {
            { "HerbId", Herb.Id },
            { "HerbName", Herb.Name }
        };

        _regionManager.RequestNavigate("MainRegion", "PrescriptionHistoryView", parameters);
    }
}
```

### 6. Repository模式与三层架构（ViewModel → Repository → API）

```csharp
/// <summary>
/// IHerbRepository - 药材数据访问接口
/// 定义6个核心方法
/// </summary>
public interface IHerbRepository
{
    Task<PagedResult<HerbDto>> GetPagedAsync(int pageIndex, int pageSize, string? searchTerm = null);
    Task<HerbDto> GetByIdAsync(Guid id);
    Task<HerbDto> CreateAsync(CreateHerbDto dto);
    Task<HerbDto> UpdateAsync(Guid id, UpdateHerbDto dto);
    Task DeleteAsync(Guid id);
    Task<List<HerbDto>> SearchAsync(string keyword);
}

/// <summary>
/// HerbRepository - Repository实现（继承BaseApiRepository）
/// 通过ApiService与Server端交互（/api/v1/herbs）
/// </summary>
public class HerbRepository : BaseApiRepository<HerbDto>, IHerbRepository
{
    private readonly IApiService _apiService;
    private readonly ILogger<HerbRepository> _logger;

    public HerbRepository(
        IApiService apiService,
        ILogger<HerbRepository> logger)
        : base(apiService, logger, "herbs")
    {
        _apiService = apiService;
        _logger = logger;
    }

    /// <summary>
    /// 分页查询药材（支持名称/拼音搜索）
    /// API: GET /api/v1/herbs?pageIndex=1&pageSize=20&searchTerm=dg
    /// </summary>
    public async Task<PagedResult<HerbDto>> GetPagedAsync(
        int pageIndex,
        int pageSize,
        string? searchTerm = null)
    {
        var queryString = $"?pageIndex={pageIndex}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            queryString += $"&searchTerm={Uri.EscapeDataString(searchTerm)}";
        }

        return await _apiService.GetAsync<PagedResult<HerbDto>>($"herbs{queryString}");
    }

    /// <summary>
    /// 按ID查询药材详情
    /// API: GET /api/v1/herbs/{id}
    /// </summary>
    public async Task<HerbDto> GetByIdAsync(Guid id)
    {
        return await _apiService.GetAsync<HerbDto>($"herbs/{id}");
    }

    /// <summary>
    /// 创建药材
    /// API: POST /api/v1/herbs
    /// </summary>
    public async Task<HerbDto> CreateAsync(CreateHerbDto dto)
    {
        return await _apiService.PostAsync<HerbDto>("herbs", dto);
    }

    /// <summary>
    /// 更新药材
    /// API: PUT /api/v1/herbs/{id}
    /// </summary>
    public async Task<HerbDto> UpdateAsync(Guid id, UpdateHerbDto dto)
    {
        return await _apiService.PutAsync<HerbDto>($"herbs/{id}", dto);
    }

    /// <summary>
    /// 删除药材
    /// API: DELETE /api/v1/herbs/{id}
    /// </summary>
    public async Task DeleteAsync(Guid id)
    {
        await _apiService.DeleteAsync($"herbs/{id}");
    }

    /// <summary>
    /// 搜索药材（名称/拼音/功效）
    /// API: GET /api/v1/herbs/search?keyword=当归
    /// </summary>
    public async Task<List<HerbDto>> SearchAsync(string keyword)
    {
        return await _apiService.GetAsync<List<HerbDto>>($"herbs/search?keyword={Uri.EscapeDataString(keyword)}");
    }
}

/// <summary>
/// 调用链:
/// HerbManagementViewModel → IHerbRepository → HerbRepository → ApiService → HTTP → Server /api/v1/herbs
/// </summary>
```

### 7. Excel批量导入药材（与Server端协同）

```csharp
/// <summary>
/// HerbManagementViewModel - Excel批量导入功能
/// 调用Server端 POST /api/v1/herbs/import 接口
/// </summary>
public class HerbManagementViewModel : UnifiedViewModelBase
{
    /// <summary>
    /// 导入Excel药材
    /// </summary>
    private async Task ImportHerbsAsync()
    {
        // 1. 打开文件选择对话框
        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择药材Excel文件",
            Filter = "Excel文件 (*.xlsx)|*.xlsx|所有文件 (*.*)|*.*",
            Multiselect = false
        };

        if (openFileDialog.ShowDialog() != true) return;

        try
        {
            IsBusy = true;
            var filePath = openFileDialog.FileName;

            // 2. 读取文件并上传到Server
            using var fileStream = File.OpenRead(filePath);
            var formData = new MultipartFormDataContent();
            formData.Add(new StreamContent(fileStream), "file", Path.GetFileName(filePath));

            var response = await _apiService.PostAsync<ImportResult>("herbs/import", formData);

            // 3. 显示导入结果
            var successCount = response.Succeeded?.Count ?? 0;
            var failedCount = response.Failed?.Count ?? 0;

            if (failedCount > 0)
            {
                // 显示错误详情对话框
                var errorMessage = string.Join("\n", response.Failed.Select(f =>
                    $"行{f.RowNumber}: {f.ErrorMessage}"
                ));

                await _dialogService.ShowAlertAsync(
                    "导入结果",
                    $"成功导入: {successCount}条\n失败: {failedCount}条\n\n错误详情:\n{errorMessage}"
                );
            }
            else
            {
                await _dialogService.ShowAlertAsync(
                    "导入成功",
                    $"成功导入 {successCount} 条药材数据"
                );
            }

            // 4. 刷新列表
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入药材失败");
            await _dialogService.ShowAlertAsync("导入失败", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// 导出Excel导入模板
    /// 调用 GET /api/v1/herbs/template 获取模板文件
    /// </summary>
    private async Task ExportTemplateAsync()
    {
        var saveFileDialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "保存药材导入模板",
            Filter = "Excel文件 (*.xlsx)|*.xlsx",
            FileName = $"药材导入模板_{DateTime.Now:yyyyMMdd}.xlsx"
        };

        if (saveFileDialog.ShowDialog() != true) return;

        try
        {
            IsBusy = true;

            // 下载模板文件
            var templateBytes = await _apiService.GetBytesAsync("herbs/template");
            await File.WriteAllBytesAsync(saveFileDialog.FileName, templateBytes);

            await _dialogService.ShowAlertAsync("导出成功", "模板文件已保存");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出模板失败");
            await _dialogService.ShowAlertAsync("导出失败", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

### 8. 拼音快速检索（中医药材快速输入）

```csharp
/// <summary>
/// HerbManagementViewModel - 拼音检索功能
/// 示例:输入"dg"可匹配"当归"（拼音首字母）
/// Server端通过HerbRepository.GetByNameOrPinyinAsync实现
/// </summary>
public class HerbManagementViewModel : UnifiedViewModelBase
{
    /// <summary>
    /// 搜索文本框绑定属性（支持名称/拼音搜索）
    /// </summary>
    public string SearchText { get; set; }

    /// <summary>
    /// 搜索命令（TextBox回车触发）
    /// </summary>
    public AsyncDelegateCommand SearchCommand { get; }

    /// <summary>
    /// 执行搜索（重置到第一页）
    /// </summary>
    private async Task ExecuteSearchAsync()
    {
        CurrentPage = 1;
        await LoadPageAsync(CurrentPage);
    }

    /// <summary>
    /// 分页加载药材列表（搜索参数传递到Server）
    /// Server端会调用:
    /// - h.Name.Contains(keyword) - 名称模糊匹配
    /// - h.PinyinAbbreviation.Contains(keyword) - 拼音首字母匹配
    /// </summary>
    public async Task<PagedResult<HerbDto>> GetItemsAsync(int pageIndex, int pageSize)
    {
        return await _herbRepository.GetPagedAsync(
            pageIndex,
            pageSize,
            SearchText // 传递搜索关键字（名称/拼音）
        );
    }
}
```

**拼音检索示例**:

| 输入 | 匹配药材 | 说明 |
|------|---------|------|
| `dg` | 当归 | PinyinAbbreviation = "DG" |
| `hq` | 黄芪 | PinyinAbbreviation = "HQ" |
| `rsh` | 人参 | PinyinAbbreviation = "RSH" |
| `当` | 当归 | Name.Contains("当") |

### 9. 按分类搜索药材（药材分类管理）

```csharp
/// <summary>
/// HerbManagementViewModel - 分类搜索功能
/// 支持按药材分类筛选（如:补益药、清热药、解表药等）
/// </summary>
public class HerbManagementViewModel : UnifiedViewModelBase
{
    /// <summary>
    /// 药材分类列表（从Server获取或本地配置）
    /// </summary>
    public ObservableCollection<string> Categories { get; set; }

    /// <summary>
    /// 选中的分类
    /// </summary>
    public string SelectedCategory { get; set; }

    /// <summary>
    /// 按分类搜索命令
    /// </summary>
    public AsyncDelegateCommand SearchByCategoryCommand { get; }

    /// <summary>
    /// 执行分类搜索
    /// </summary>
    private async Task SearchByCategory()
    {
        if (string.IsNullOrWhiteSpace(SelectedCategory)) return;

        // 调用Repository的SearchAsync方法
        var herbs = await _herbRepository.SearchAsync(SelectedCategory);

        // 更新UI列表
        Herbs.Clear();
        foreach (var herb in herbs)
        {
            Herbs.Add(herb);
        }

        TotalCount = herbs.Count;
        _logger.LogInformation($"分类搜索: {SelectedCategory}, 找到 {TotalCount} 条药材");
    }

    /// <summary>
    /// 加载分类列表（初始化时调用）
    /// </summary>
    private void LoadCategories()
    {
        Categories = new ObservableCollection<string>
        {
            "全部",
            "补益药",
            "清热药",
            "解表药",
            "活血化瘀药",
            "化痰止咳平喘药",
            "安神药",
            "理气药",
            "消食药",
            "利水渗湿药"
        };
    }
}
```

### 10. HerbsModule注册（Prism模块注册）

```csharp
/// <summary>
/// HerbsModule - Prism模块注册
/// 注册2个ViewModels、2个Views、1个Repository
/// </summary>
public class HerbsModule : IModule
{
    /// <summary>
    /// 模块初始化（Shell启动时调用）
    /// </summary>
    public void OnInitialized(IContainerProvider containerProvider)
    {
        var regionManager = containerProvider.Resolve<IRegionManager>();

        // 注册药材列表视图到MainRegion（可选）
        regionManager.RegisterViewWithRegion("MainRegion", typeof(HerbManagementView));
    }

    /// <summary>
    /// 注册类型（依赖注入容器配置）
    /// </summary>
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 1. 注册ViewModels（Transient：每次创建新实例）
        containerRegistry.Register<HerbManagementViewModel>();
        containerRegistry.Register<HerbDetailViewModel>();

        // 2. 注册Views（导航时使用）
        containerRegistry.RegisterForNavigation<HerbManagementView, HerbManagementViewModel>();
        containerRegistry.RegisterForNavigation<HerbDetailView, HerbDetailViewModel>();

        // 3. 注册Repository（Singleton：全局单例）
        containerRegistry.RegisterSingleton<IHerbRepository, HerbRepository>();
    }
}
```

### 11. 状态管理与批量删除

```csharp
/// <summary>
/// HerbManagementViewModel - 状态管理与批量删除
/// </summary>
public class HerbManagementViewModel : UnifiedViewModelBase
{
    /// <summary>
    /// 选中的药材列表（多选）
    /// </summary>
    public ObservableCollection<HerbDto> SelectedHerbs { get; set; }

    /// <summary>
    /// 批量删除命令
    /// </summary>
    public AsyncDelegateCommand BatchDeleteCommand { get; }

    /// <summary>
    /// 执行批量删除
    /// </summary>
    private async Task OnExecuteBatchDeleteAsync()
    {
        if (SelectedHerbs == null || SelectedHerbs.Count == 0)
        {
            await _dialogService.ShowAlertAsync("提示", "请先选择要删除的药材");
            return;
        }

        var count = SelectedHerbs.Count;
        var result = await _dialogService.ShowConfirmationAsync(
            "确认批量删除",
            $"确定要删除选中的 {count} 条药材吗？此操作不可恢复。"
        );

        if (result != ButtonResult.OK) return;

        try
        {
            IsBusy = true;

            // 批量删除
            var deleteTasks = SelectedHerbs.Select(herb =>
                _herbRepository.DeleteAsync(herb.Id)
            );

            await Task.WhenAll(deleteTasks);

            _logger.LogInformation($"批量删除成功: {count} 条药材");
            await _dialogService.ShowAlertAsync("删除成功", $"已删除 {count} 条药材");

            // 刷新列表
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量删除失败");
            await _dialogService.ShowAlertAsync("删除失败", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// 切换药材状态（启用/禁用）
    /// </summary>
    private async Task ToggleStatusAsync()
    {
        if (SelectedHerb == null) return;

        try
        {
            IsBusy = true;

            // 切换状态
            var newStatus = SelectedHerb.Status == HerbStatus.Active
                ? HerbStatus.Inactive
                : HerbStatus.Active;

            // 更新到Server
            var updateDto = new UpdateHerbDto
            {
                Status = newStatus
            };

            await _herbRepository.UpdateAsync(SelectedHerb.Id, updateDto);

            // 更新本地状态
            SelectedHerb.Status = newStatus;

            _logger.LogInformation($"药材状态已切换: {SelectedHerb.Name} -> {newStatus}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换状态失败");
            await _dialogService.ShowAlertAsync("操作失败", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

## 🎨 模块架构图

```
┌─────────────────────────────────────────────────────────────────┐
│                      LYBT.Desktop.Herbs                         │
│                      (药材管理模块)                              │
└─────────────────────────────────────────────────────────────────┘
                                │
                ┌───────────────┼───────────────┐
                │               │               │
        ┌───────▼──────┐ ┌─────▼──────┐ ┌─────▼──────┐
        │ HerbManage   │ │  HerbDetail │ │  Herbs     │
        │ mentView     │ │  View       │ │  Module    │
        │ (XAML)       │ │  (XAML)     │ │  (Prism)   │
        └───────┬──────┘ └─────┬──────┘ └─────┬──────┘
                │               │               │
        ┌───────▼──────────────▼───────────────▼──────┐
        │         MVVM ViewModel Layer                 │
        │  ┌──────────────────────────────────────┐   │
        │  │  HerbManagementViewModel             │   │
        │  │  - 19 Commands (Add/Delete/Edit...)  │   │
        │  │  - 17 Methods (CRUD/Search/Import)   │   │
        │  └──────────────────────────────────────┘   │
        │  ┌──────────────────────────────────────┐   │
        │  │  HerbDetailViewModel                 │   │
        │  │  - 16 Properties (Name/Price/...)    │   │
        │  │  - 15 Methods (Save/Load/Print)      │   │
        │  └──────────────────────────────────────┘   │
        └───────┬──────────────────────────────────────┘
                │ 依赖注入
        ┌───────▼──────────────────────────────────────┐
        │       Repository Layer                       │
        │  ┌──────────────────────────────────────┐   │
        │  │  IHerbRepository (Interface)         │   │
        │  │  - 6 Methods (GetPaged/CRUD/Search)  │   │
        │  └──────────────────────────────────────┘   │
        │  ┌──────────────────────────────────────┐   │
        │  │  HerbRepository (Implementation)     │   │
        │  │  - BaseApiRepository (继承)          │   │
        │  │  - ApiService (依赖)                 │   │
        │  └──────────────────────────────────────┘   │
        └───────┬──────────────────────────────────────┘
                │ HTTP通信
        ┌───────▼──────────────────────────────────────┐
        │     LYBT.Desktop.Foundation                  │
        │  ┌──────────────────────────────────────┐   │
        │  │  ApiService (IApiService)            │   │
        │  │  - HttpClient封装                    │   │
        │  │  - 返回裸类型（非Result<T>）         │   │
        │  └──────────────────────────────────────┘   │
        └───────┬──────────────────────────────────────┘
                │ REST API
        ┌───────▼──────────────────────────────────────┐
        │        LYBT.WebAPI (Server端)                │
        │  /api/v1/herbs/*                             │
        │  - GET /herbs (分页查询)                     │
        │  - POST /herbs (创建药材)                    │
        │  - PUT /herbs/{id} (更新药材)                │
        │  - DELETE /herbs/{id} (删除药材)             │
        │  - GET /herbs/search (搜索药材)              │
        │  - POST /herbs/import (Excel导入)            │
        │  - GET /herbs/export (Excel导出)             │
        └──────────────────────────────────────────────┘

特性:
1. MVVM架构 + Repository模式 + 三层分离
2. 拼音快速检索（PinyinAbbreviation字段）
3. 双价格体系（Price售价 + CostPrice成本价）
4. 批量操作（导入/导出/批量删除）
5. 状态管理（Active/Inactive切换）
6. 使用历史查询（ViewUsageHistoryCommand）
7. 分类搜索（按药材分类筛选）
```

## 🎯 设计原则

### 1. MVVM架构与数据绑定

**核心模式**:
- **ViewModel**: 封装UI逻辑,通过INotifyPropertyChanged实现双向绑定
- **ObservableCollection**: 药材列表自动同步到DataGrid
- **ICommand**: 按钮点击绑定到AsyncDelegateCommand
- **NavigationParameters**: View间参数传递（如HerbId、Mode）

**示例**:
```xml
<!-- HerbManagementView.xaml -->
<DataGrid ItemsSource="{Binding Herbs}"
          SelectedItem="{Binding SelectedHerb}">
    <DataGrid.Columns>
        <DataGridTextColumn Header="药材名称" Binding="{Binding Name}" />
        <DataGridTextColumn Header="拼音" Binding="{Binding PinYinCode}" />
        <DataGridTextColumn Header="售价" Binding="{Binding Price, StringFormat=¥{0:F2}}" />
        <DataGridTextColumn Header="成本价" Binding="{Binding CostPrice, StringFormat=¥{0:F2}}" />
        <DataGridTextColumn Header="状态" Binding="{Binding Status}" />
    </DataGrid.Columns>
</DataGrid>
```

**反模式**:
- ❌ 在ViewModel中直接操作UI控件（如TextBox.Text = "xxx"）
- ❌ 在View代码隐藏中编写业务逻辑（应该在ViewModel中）
- ❌ 直接依赖Server Service（应该通过Repository抽象）

### 2. Repository模式与三层架构

**核心思想**:
- **ViewModel层**: UI业务逻辑,依赖IHerbRepository接口
- **Repository层**: 数据访问抽象,通过ApiService与Server交互
- **ApiService层**: HTTP通信封装,返回裸类型（非Result<T>）

**优势**:
-  ViewModel与Server解耦,易于单元测试（Mock IHerbRepository）
-  Repository集中管理API调用,统一错误处理
-  返回裸类型简化ViewModel逻辑（无需Result<T>拆包）

**示例**:
```csharp
// ViewModel依赖IHerbRepository接口
public class HerbManagementViewModel
{
    private readonly IHerbRepository _herbRepository; // 抽象接口

    public async Task LoadPageAsync(int pageIndex)
    {
        var result = await _herbRepository.GetPagedAsync(pageIndex, PageSize);
        // 直接使用result,无需检查Result<T>.IsSuccess
    }
}

// Repository实现HTTP调用
public class HerbRepository : BaseApiRepository<HerbDto>, IHerbRepository
{
    public async Task<PagedResult<HerbDto>> GetPagedAsync(int pageIndex, int pageSize, string? searchTerm = null)
    {
        return await _apiService.GetAsync<PagedResult<HerbDto>>($"herbs?pageIndex={pageIndex}&pageSize={pageSize}");
    }
}
```

**反模式**:
- ❌ ViewModel直接依赖具体Repository类（应该依赖接口）
- ❌ 返回Result<T>增加ViewModel复杂度（Client端不需要Result包装）
- ❌ Repository混入UI逻辑（如MessageBox.Show）

### 3. 拼音快速检索与中医药材特性

**核心功能**:
- **PinyinAbbreviation字段**: 存储药材名称的拼音首字母（如"当归" → "DG"）
- **Server端模糊匹配**: `h.Name.Contains(keyword) || h.PinyinAbbreviation.Contains(keyword)`
- **即时搜索**: TextBox输入即触发SearchCommand

**优势**:
-  中医师快速输入（输入"dg"秒匹配"当归"）
-  减少鼠标操作,提升开方效率
-  支持名称/拼音/功效多字段搜索

**示例**:
```csharp
// HerbManagementViewModel - 搜索命令
public AsyncDelegateCommand SearchCommand { get; }

private async Task ExecuteSearchAsync()
{
    // 输入"dg"或"当归"都能匹配
    await LoadPageAsync(1);
}

// Server端HerbRepository
public async Task<List<HerbModel>> GetByNameOrPinyinAsync(string keyword)
{
    return await _dbSet
        .Where(h => h.Name.Contains(keyword) || h.PinyinAbbreviation.Contains(keyword))
        .ToListAsync();
}
```

**反模式**:
- ❌ 不支持拼音检索,强制全名输入（降低效率）
- ❌ 拼音字段为空或不维护（失去快速检索能力）

### 4. 双价格体系与利润率计算

**核心设计**:
- **Price（售价）**: 面向患者的销售价格
- **CostPrice（成本价）**: 药材采购成本价格
- **ProfitMargin（利润率）**: (Price - CostPrice) / Price × 100%

**业务逻辑**:
-  保存时自动计算利润率
-  售价低于成本价时弹出警告（但允许保存）
-  支持成本价为0（赠送药材或自采）

**示例**:
```csharp
// HerbDetailViewModel - 保存验证
private async Task SaveHerbAsync()
{
    // 利润率计算
    decimal profitMargin = Price > 0 ? (Price - CostPrice) / Price * 100 : 0;

    if (profitMargin < 0)
    {
        var result = await _dialogService.ShowConfirmationAsync(
            "价格警告",
            $"售价低于成本价,利润率为 {profitMargin:F2}%,是否继续保存？"
        );
        if (result != ButtonResult.OK) return;
    }

    // 保存药材...
}
```

**反模式**:
- ❌ 只存售价,不记录成本（无法分析利润）
- ❌ 强制要求售价>成本价（赠送药材无法录入）

### 5. 批量操作与Excel导入导出

**核心功能**:
- **批量删除**: 选中多个药材批量删除（Task.WhenAll并发）
- **Excel导入**: 从Excel批量导入药材（POST /api/v1/herbs/import）
- **Excel导出**: 导出药材到Excel（GET /api/v1/herbs/export）
- **导入模板**: 下载Excel导入模板（GET /api/v1/herbs/template）

**错误处理**:
-  导入失败行号+错误信息展示
-  成功/失败统计展示
-  Server端验证（名称重复、必填项校验）

**示例**:
```csharp
// HerbManagementViewModel - Excel导入
private async Task ImportHerbsAsync()
{
    var response = await _apiService.PostAsync<ImportResult>("herbs/import", formData);

    var successCount = response.Succeeded?.Count ?? 0;
    var failedCount = response.Failed?.Count ?? 0;

    if (failedCount > 0)
    {
        var errorMessage = string.Join("\n", response.Failed.Select(f =>
            $"行{f.RowNumber}: {f.ErrorMessage}"
        ));
        // 显示错误详情...
    }
}
```

**反模式**:
- ❌ 导入失败不提示具体行号（用户无法定位错误）
- ❌ 导入失败全部回滚（部分成功数据也丢失）

### 6. 状态管理与使用历史查询

**核心功能**:
- **Status枚举**: Active（启用）、Inactive（禁用）
- **ToggleStatusCommand**: 一键切换状态（UI按钮绑定）
- **ViewUsageHistoryCommand**: 查询药材在处方中的使用情况

**业务逻辑**:
-  禁用药材不影响历史处方（只读）
-  禁用药材不可添加到新处方（HerbSelectionDialog过滤）
-  使用历史显示处方编号、患者姓名、用量、日期

**示例**:
```csharp
// HerbManagementViewModel - 切换状态
private async Task ToggleStatusAsync()
{
    var newStatus = SelectedHerb.Status == HerbStatus.Active
        ? HerbStatus.Inactive
        : HerbStatus.Active;

    await _herbRepository.UpdateAsync(SelectedHerb.Id, new UpdateHerbDto
    {
        Status = newStatus
    });

    SelectedHerb.Status = newStatus;
}

// HerbDetailViewModel - 查看使用历史
private async Task ExecuteViewUsageHistory()
{
    // 导航到PrescriptionHistoryView,传递HerbId
    _regionManager.RequestNavigate("MainRegion", "PrescriptionHistoryView", new NavigationParameters
    {
        { "HerbId", Herb.Id },
        { "HerbName", Herb.Name }
    });
}
```

**反模式**:
- ❌ 删除药材而非禁用（历史处方引用丢失）
- ❌ 无使用历史查询（无法分析药材使用频率）

### 7. 异步优先与UI响应性

**核心原则**:
- **全异步方法**: 所有I/O操作（API调用、文件读写）使用async/await
- **IsBusy标志**: 长时间操作显示Loading指示器
- **AsyncDelegateCommand**: Prism异步命令模式,自动管理CanExecute状态
- **Task.WhenAll**: 批量操作并发执行,提升性能

**示例**:
```csharp
// HerbManagementViewModel - 批量删除并发执行
private async Task OnExecuteBatchDeleteAsync()
{
    IsBusy = true; // 显示Loading
    try
    {
        var deleteTasks = SelectedHerbs.Select(herb =>
            _herbRepository.DeleteAsync(herb.Id)
        );
        await Task.WhenAll(deleteTasks); // 并发删除
    }
    finally
    {
        IsBusy = false; // 隐藏Loading
    }
}
```

**反模式**:
- ❌ 同步方法阻塞UI线程（用户无法操作）
- ❌ 无IsBusy标志（用户不知道是否在处理）
- ❌ 批量操作串行执行（性能低下）

## 📚 详细文档

- **完整模块文档**: [docs/reference/modules/herbs/](../../../../docs/reference/modules/herbs/) *(待创建)*
- **架构设计**: [docs/explanation/architecture/client/herbs-design.md](../../../../docs/explanation/architecture/client/herbs-design.md) *(待创建)*
- **开发指南**: [docs/how-to-guides/client/herbs-development.md](../../../../docs/how-to-guides/client/herbs-development.md) *(待创建)*
- **集成文档**: [docs/tutorials/client/herbs-integration.md](../../../../docs/tutorials/client/herbs-integration.md) *(待创建)*

---

**最后更新**: 2025-10-29
**维护负责**: Client端开发组
