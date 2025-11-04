# LYBT.Desktop.Formula - 验方管理模块

## 📦 项目定位

- **层级**:Client端 (WPF桌面应用)
- **类型**:业务模块 (验方管理)
- **职责**:为医生提供管理经典方剂和个人验方的用户界面，支持方剂的创建、编辑、克隆、查询、验证和组方配置。作为中医知识管理和经验积累的重要工具，支持从处方快速创建验方、Excel批量导入导出、智能药材匹配、待验证验方管理等功能。采用MVVM架构（Prism.DryIoc + MaterialDesign），通过Repository模式与Server端/api/v1/formulas交互，实现验方模板化管理，提高医生开方效率。

## 📂 代码结构

```
LYBT.Desktop.Formula/
├── Interfaces/                              # 接口定义 (1个接口)
│   └── IFormulaRepository.cs                # 验方Repository接口 (9方法)
├── Models/                                  # 数据模型 (1个Model)
│   └── FormulaItem.cs                       # 验方条目模型
├── Repositories/                            # 数据访问层 (1个Repository)
│   └── FormulaRepository.cs                 # 验方Repository实现 (继承BaseApiRepository)
├── ViewModels/                              # 视图模型层 (6个ViewModel)
│   ├── FormulaManagementViewModel.cs        # 验方列表管理ViewModel (458行: 20 Commands + 20 Methods)
│   ├── FormulaDetailViewModel.cs            # 验方详情编辑ViewModel (675行: 25 Properties + 11 Commands + 22 Methods)
│   ├── EditFormulaDialogViewModel.cs        # 编辑对话框ViewModel
│   ├── FormulaValidationViewModel.cs        # 验方验证ViewModel (待验证列表管理)
│   ├── FormulaHerbItemViewModel.cs          # 药材条目ViewModel (单个药材管理)
│   └── Components/                          # 组件式辅助类 (4个Helper)
│       ├── FormulaCalculator.cs             # 验方总价计算器
│       ├── FormulaCommandHandler.cs         # 命令处理器 (封装命令逻辑)
│       ├── FormulaDataManager.cs            # 数据管理器 (封装数据加载逻辑)
│       └── FormulaValidator.cs              # 验证器 (封装验证逻辑)
├── Views/                                   # 视图层 (4个View: 8个文件.xaml+.xaml.cs)
│   ├── FormulaManagementView.xaml           # 验方列表管理界面
│   ├── FormulaManagementView.xaml.cs
│   ├── FormulaDetailView.xaml               # 验方详情编辑界面
│   ├── FormulaDetailView.xaml.cs
│   ├── EditFormulaDialog.xaml               # 编辑对话框 (快速编辑)
│   ├── EditFormulaDialog.xaml.cs
│   ├── FormulaValidationView.xaml           # 验方验证界面 (待验证列表)
│   └── FormulaValidationView.xaml.cs
├── FormulaModule.cs                         # Prism模块定义 (2方法: OnInitialized + RegisterTypes)
├── LYBT.Desktop.Formula.csproj              # 项目配置文件
└── README.md                                # 本文档

**文件统计**:
- 6个目录
- 26个文件 (1接口 + 1模型 + 1Repository + 6ViewModel + 4辅助类 + 5View[10文件] + 1Module + 1配置 + 1文档)
```

**说明**:
- **FormulaManagementViewModel**:458行核心ViewModel，提供20个Command（添加、删除、编辑、克隆、搜索、导入/导出、批量操作等）和20个Method（分页加载、搜索、分类筛选、批量删除等）
- **FormulaDetailViewModel**:675行详情ViewModel，提供25个Property（验方信息、药材列表、总价等）、11个Command（保存、取消、编辑、打印、查看使用历史等）和22个Method（数据加载、验证、保存、克隆等）
- **IFormulaRepository**:定义9个核心方法（GetPagedAsync、GetByIdAsync、CreateAsync、UpdateAsync、DeleteAsync、SearchAsync、CloneFormulaAsync、GetPendingValidationFormulasAsync、ValidateFormulaHerbAsync）
- **FormulaRepository**:继承BaseApiRepository，通过ApiService与Server端/api/v1/formulas交互，返回裸类型（非Result<T>包装）
- **Components辅助类**:将复杂逻辑拆分为4个可重用组件（计算器、命令处理器、数据管理器、验证器），降低ViewModel复杂度
- **多视图支持**:5个独立View（管理界面、详情界面、编辑对话框、查看对话框、验证界面），满足不同场景需求

## 🔗 依赖关系

### 依赖的项目
1. **LYBT.Desktop.Core** - 核心架构（UnifiedViewModelBase、IPagedDataManager等）
2. **LYBT.Desktop.Foundation** - 基础服务（ApiService、BaseApiRepository、PagedResult等）
3. **LYBT.Desktop.Presentation** - UI基础设施（布局、主题、通用控件）
4. **LYBT.Shared.Models** - 共享DTO模型（FormulaDto、CreateFormulaDto、UpdateFormulaDto、FormulaHerbItemDto等）
5. **LYBT.Shared.Interfaces** - 共享接口定义（如果有验方相关接口）

### 被依赖项目
1. **LYBT.Desktop.Shell** - Shell通过Prism加载此模块
2. **LYBT.Desktop.Prescriptions** - 处方模块可能引用验方作为模板（创建处方时选择验方）

### NuGet包
- **Prism.DryIoc** (9.0.x) - MVVM框架、依赖注入、区域导航
- **MaterialDesignThemes** (5.1.x) - Material Design UI组件库
- **MaterialDesignExtensions** (3.3.x) - Material Design扩展控件
- **Newtonsoft.Json** (13.0.x) - JSON序列化（用于ApiService交互）

## 🛠 技术栈

- **.NET 8**: 基础框架
- **WPF**: Windows Presentation Foundation桌面UI框架
- **Prism.DryIoc 9.0.x**: MVVM框架，提供模块化、依赖注入、区域导航、命令支持
- **MaterialDesignThemes 5.1.x**: Material Design UI组件库，提供现代化界面风格
- **MVVM架构**: Model-View-ViewModel设计模式，实现UI与业务逻辑分离
- **Repository模式**: FormulaRepository → BaseApiRepository → ApiService → HTTP → Server
- **异步编程**: 全异步方法（async/await），所有数据操作使用Task<T>，提升UI响应性
- **ObservableCollection**: WPF数据绑定核心集合，支持集合变更通知（INotifyCollectionChanged）
- **AsyncDelegateCommand**: Prism异步命令模式，支持CanExecute/Execute逻辑分离
- **NavigationParameters & RegionManager**: View间导航和参数传递机制
- **IsBusy模式**: Loading状态管理，避免重复提交
- **返回裸类型**: Repository返回FormulaDto而非Result<FormulaDto>（Client端不需要Result包装）

## 🚀 快速开始

此项目是一个Prism模块库，由 `LYBT.Desktop.Shell` 在启动时通过模块目录（WhenAvailable）自动加载。无法独立运行。

```bash
# 构建此项目
dotnet build src/Client/Desktop/Modules/LYBT.Desktop.Formula/LYBT.Desktop.Formula.csproj

# 或构建整个解决方案
dotnet build LYBT.All.sln -c Release --no-restore
```

### 集成说明

#### 1. Shell加载Formula模块（LYBT.Desktop.Shell启动时）

```csharp
/// <summary>
/// Shell启动时通过Prism模块目录（WhenAvailable）自动加载Formula模块
/// 文件: LYBT.Desktop.Shell/App.xaml.cs
/// </summary>
protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
{
    // Prism通过DirectoryModuleCatalog自动扫描Modules目录
    // 发现LYBT.Desktop.Formula.dll后自动加载FormulaModule
    moduleCatalog.AddModule<FormulaModule>(InitializationMode.WhenAvailable);
}

/// <summary>
/// FormulaModule初始化（自动注册所有ViewModels、Views和Repository）
/// 文件: LYBT.Desktop.Formula/FormulaModule.cs
/// </summary>
public class FormulaModule : IModule
{
    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 模块初始化完成后的操作（如果需要）
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册ViewModels (6个)
        containerRegistry.Register<FormulaManagementViewModel>();
        containerRegistry.Register<FormulaDetailViewModel>();
        containerRegistry.Register<EditFormulaDialogViewModel>();
        containerRegistry.Register<FormulaValidationViewModel>();
        containerRegistry.Register<FormulaHerbItemViewModel>();

        // 注册辅助类 (4个Components)
        containerRegistry.RegisterSingleton<FormulaCalculator>();
        containerRegistry.Register<FormulaCommandHandler>();
        containerRegistry.Register<FormulaDataManager>();
        containerRegistry.Register<FormulaValidator>();

        // 注册Views (4个)
        containerRegistry.RegisterForNavigation<FormulaManagementView>();
        containerRegistry.RegisterForNavigation<FormulaDetailView>();
        containerRegistry.RegisterDialog<EditFormulaDialog>();
        // Issue #1802: ViewFormulaDialog已删除（改用FormulaDetailView进行只读查看）
        containerRegistry.RegisterForNavigation<FormulaValidationView>();

        // 注册Repository
        containerRegistry.Register<IFormulaRepository, FormulaRepository>();
    }
}
```

#### 2. FormulaManagementViewModel核心属性与方法

**FormulaManagementViewModel** (458行) 负责验方列表管理、分页查询、搜索、批量操作、导入/导出等功能。

**核心Commands** (20个):

| Command | 功能描述 | 关联方法 |
|---------|---------|---------|
| `AddCommand` | 添加新验方 | OnExecuteAddAsync() |
| `DeleteCommand` | 删除选中验方 | OnExecuteDeleteAsync() |
| `AddFormulaCommand` | 快速添加验方（对话框方式） | - |
| `ViewDetailsCommand` | 查看验方详情（只读） | - |
| `ViewDetailCommand` | 查看验方详情（导航到FormulaDetailView） | ViewFormulaDetail() |
| `EditCommand` | 编辑选中验方 | EditFormula() |
| `CopyCommand` | 克隆选中验方 | CopyFormula() |
| `RefreshCommand` | 刷新列表 | 继承自UnifiedViewModelBase |
| `SearchCommand` | 搜索验方（名称/分类） | 继承自UnifiedViewModelBase |
| `NextPageCommand` | 下一页 | 继承自UnifiedViewModelBase |
| `PreviousPageCommand` | 上一页 | 继承自UnifiedViewModelBase |
| `FirstPageCommand` | 第一页 | ExecuteFirstPage() |
| `LastPageCommand` | 最后一页 | ExecuteLastPage() |
| `ImportFormulasCommand` | Excel导入验方 | ExecuteImportFormulasAsync() |
| `ExportTemplateCommand` | 下载Excel导入模板 | ExecuteExportTemplateAsync() |
| `ExportFormulasCommand` | 导出验方到Excel | ExecuteExportFormulasAsync() |
| `SearchByCategoryCommand` | 按分类搜索验方 | SearchByCategory() |
| `ClearFiltersCommand` | 清除所有搜索条件 | ExecuteClearFilters() |
| BatchDeleteCommand | 批量删除验方 | OnExecuteBatchDeleteAsync() |
| LoadPageCommand | 加载指定页 | 继承自UnifiedViewModelBase |

**核心Methods** (20个):

| 方法名 | 功能描述 | 返回值 |
|-------|---------|-------|
| `GetItemsAsync(pageIndex, pageSize)` | 分页查询验方（支持搜索） | Task<PagedResult<FormulaDto>> |
| `OnExecuteAddAsync()` | 执行添加验方命令 | Task |
| `OnExecuteDeleteAsync()` | 执行删除验方命令 | Task |
| `OnExecuteBatchDeleteAsync()` | 批量删除验方（Task.WhenAll并发） | Task |
| `InitializeAsync()` | 初始化ViewModel，加载初始数据 | Task |
| `ViewFormulaDetail(formula)` | 导航到验方详情页 | void |
| `EditFormula(formula)` | 编辑验方（导航到详情页编辑模式） | void |
| `CopyFormula(formula)` | 克隆验方（复制后打开编辑） | void |
| `CanViewDetail()` | 判断是否可查看详情 | bool |
| `CanEditFormula()` | 判断是否可编辑验方 | bool |
| `CanCopyFormula()` | 判断是否可克隆验方 | bool |
| `ExecuteFirstPage()` | 跳转到第一页 | void |
| `ExecuteLastPage()` | 跳转到最后一页 | void |
| `ExecuteImportFormulasAsync()` | 导入验方（OpenFileDialog + API调用） | Task |
| `ExecuteExportTemplateAsync()` | 下载Excel模板（SaveFileDialog + API调用） | Task |
| `ExecuteExportFormulasAsync()` | 导出验方到Excel（SaveFileDialog + API调用） | Task |
| `ExecuteClearFilters()` | 清除所有搜索条件并刷新 | void |
| `SearchByCategory(category)` | 按分类搜索验方 | Task |
| `RefreshCanExecuteChanged()` | 刷新所有命令的CanExecute状态 | override void |
| OnPropertyChanged(propertyName) | 属性变更通知（继承） | override void |

#### 3. FormulaDetailViewModel核心属性与方法

**FormulaDetailViewModel** (675行) 负责验方详情编辑、药材配置、验方克隆、总价计算、使用历史查询等功能。

**核心Properties** (25个):

| 属性名 | 类型 | 功能描述 |
|-------|------|---------|
| `Formula` | FormulaDto | 当前验方对象 |
| `FormulaId` | Guid | 验方ID（导航参数） |
| `IsEditMode` | bool | 是否编辑模式（true=编辑，false=查看） |
| `FormulaName` | string | 验方名称（绑定TextBox） |
| `Effect` | string | 功效（如:补气养血、清热解毒） |
| `Usage` | string | 用法（如:水煎服、每日一剂） |
| `Property` | string | 性味（如:寒凉、温热） |
| `Remark` | string | 备注 |
| `IsShared` | bool | 是否共享验方（共享后其他医生可见） |
| `Category` | string | 验方分类（如:补益方、清热方） |
| `CreatedAtDisplay` | string | 创建时间显示（格式化） |
| `UpdatedAtDisplay` | string | 更新时间显示（格式化） |
| `StatusDisplay` | string | 状态显示（启用/禁用） |
| `HerbCount` | int | 药材数量（HerbItems.Count） |
| `TotalPrice` | decimal | 验方总价（通过FormulaCalculator计算） |
| `HerbItems` | ObservableCollection<FormulaHerbItemDto> | 药材列表（支持集合绑定） |
| `LoadDataCommand` | AsyncDelegateCommand | 加载数据命令 |
| `EditCommand` | AsyncDelegateCommand | 进入编辑模式命令 |
| `SaveCommand` | AsyncDelegateCommand | 保存验方命令 |
| `CancelEditCommand` | AsyncDelegateCommand | 取消编辑命令 |
| `BackCommand` | DelegateCommand | 返回列表命令 |
| `CopyFormulaCommand` | AsyncDelegateCommand | 克隆验方命令 |
| `PrintCommand` | AsyncDelegateCommand | 打印验方命令 |
| `ViewUsageHistoryCommand` | AsyncDelegateCommand | 查看使用历史命令 |
| IsBusy | bool | Loading状态（继承） |

**核心Commands** (11个):

| Command | 功能描述 | CanExecute条件 |
|---------|---------|--------------|
| `LoadDataCommand` | 加载验方数据 | !IsBusy |
| `EditCommand` | 进入编辑模式 | !IsEditMode && Formula != null |
| `SaveCommand` | 保存验方 | IsEditMode && 验证通过 |
| `CancelEditCommand` | 取消编辑（恢复原数据） | IsEditMode |
| `BackCommand` | 返回列表页 | 无条件 |
| `CopyFormulaCommand` | 克隆验方（复制后可编辑） | !IsEditMode && Formula != null |
| `PrintCommand` | 打印验方 | !IsEditMode && Formula != null |
| `ViewUsageHistoryCommand` | 查看使用历史（在哪些处方中使用） | !IsEditMode && Formula != null |
| AddHerbItemCommand | 添加药材到验方 | IsEditMode |
| RemoveHerbItemCommand | 移除药材 | IsEditMode && 选中药材 |
| CalculateTotalCommand | 重新计算总价 | HerbItems.Count > 0 |

**核心Methods** (22个):

| 方法名 | 功能描述 | 返回值 |
|-------|---------|-------|
| `InitializeAsync()` | 初始化ViewModel，处理导航参数 | Task |
| `ProcessNavigationParameters(parameters)` | 处理导航参数（FormulaId, IsEditMode） | override void |
| `IsNavigationTarget(parameters)` | 判断是否可重用当前实例 | bool |
| `LoadDataAsync()` | 加载验方数据（通过Repository） | Task |
| `LoadFormulaData()` | 加载验方并填充属性 | void |
| `SaveAsync()` | 保存验方（新建或更新） | Task |
| `CopyFormulaAsync()` | 克隆验方（包括药材列表） | Task |
| `EnableEdit()` | 进入编辑模式 | void |
| `CancelEdit()` | 取消编辑，恢复原数据 | void |
| `NavigateBack()` | 返回验方列表页 | void |
| `ExecutePrint()` | 打印验方（生成PDF） | Task |
| `ExecuteViewUsageHistory()` | 查看验方使用历史 | Task |
| `CanEdit()` | 判断是否可编辑 | bool |
| `CanSave()` | 判断是否可保存（验证通过） | bool |
| `CanCancelEdit()` | 判断是否可取消编辑 | bool |
| `CanCopyFormula()` | 判断是否可克隆验方 | bool |
| `CanPrint()` | 判断是否可打印 | bool |
| `CanViewUsageHistory()` | 判断是否可查看使用历史 | bool |
| `UpdateCommandStates()` | 更新所有命令的CanExecute状态 | void |
| `ValidateInputs()` | 验证输入数据（名称、功效必填） | bool |
| `RefreshDisplayProperties()` | 刷新显示属性（格式化日期等） | void |
| OnPropertyChanged(propertyName) | 属性变更通知（继承） | override void |

#### 4. 验方列表管理 - 分页、搜索、批量操作

```csharp
/// <summary>
/// FormulaManagementViewModel - 验方列表管理核心ViewModel
/// 功能:分页查询、搜索、批量删除、克隆、导入导出、分类筛选
/// 文件: LYBT.Desktop.Formula/ViewModels/FormulaManagementViewModel.cs
/// </summary>
public class FormulaManagementViewModel : UnifiedViewModelBase, IPagedDataManager<FormulaDto>
{
    private readonly IFormulaRepository _formulaRepository;
    private readonly IDialogService _dialogService;
    private readonly IRegionManager _regionManager;
    private readonly ILogger<FormulaManagementViewModel> _logger;

    // 验方列表
    public ObservableCollection<FormulaDto> Formulas { get; set; }
    public FormulaDto? SelectedFormula { get; set; }

    // 搜索与分页
    public string SearchText { get; set; }          // 搜索关键字（名称/功效）
    public string SelectedCategory { get; set; }    // 选中的分类
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    // 命令（20个Command）
    public AsyncDelegateCommand AddCommand { get; }
    public AsyncDelegateCommand DeleteCommand { get; }
    public AsyncDelegateCommand<FormulaDto> EditCommand { get; }
    public AsyncDelegateCommand<FormulaDto> CopyCommand { get; }
    public AsyncDelegateCommand RefreshCommand { get; }
    public AsyncDelegateCommand SearchCommand { get; }
    public AsyncDelegateCommand ImportFormulasCommand { get; }
    public AsyncDelegateCommand ExportFormulasCommand { get; }

    /// <summary>
    /// 分页加载验方列表（支持搜索和分类筛选）
    /// </summary>
    public async Task<PagedResult<FormulaDto>> GetItemsAsync(int pageIndex, int pageSize)
    {
        // 调用Repository分页查询
        var result = await _formulaRepository.GetPagedAsync(
            pageIndex,
            pageSize,
            SearchText,           // 搜索关键字（名称/功效）
            SelectedCategory      // 分类筛选（如:补益方、清热方）
        );

        TotalCount = result.TotalCount;
        CurrentPage = pageIndex;

        return result;
    }

    /// <summary>
    /// 克隆验方（复制验方及药材配置）
    /// </summary>
    private async Task CopyFormulaAsync(FormulaDto formula)
    {
        if (formula == null) return;

        // 调用Repository克隆验方
        var newFormula = await _formulaRepository.CloneFormulaAsync(
            formula.Id,
            $"{formula.Name}_副本"  // 新验方名称
        );

        _logger.LogInformation($"验方克隆成功: {formula.Name} → {newFormula.Name}");

        // 刷新列表
        await RefreshAsync();
    }

    /// <summary>
    /// 批量删除验方（Task.WhenAll并发删除）
    /// </summary>
    private async Task OnExecuteBatchDeleteAsync()
    {
        var selectedFormulas = Formulas.Where(f => f.IsSelected).ToList();
        if (!selectedFormulas.Any())
        {
            await _dialogService.ShowAlertAsync("提示", "请先选择要删除的验方");
            return;
        }

        var result = await _dialogService.ShowConfirmationAsync(
            "批量删除",
            $"确定要删除选中的 {selectedFormulas.Count} 个验方吗？"
        );

        if (result != ButtonResult.OK) return;

        IsBusy = true;
        try
        {
            // 并发删除（Task.WhenAll）
            var deleteTasks = selectedFormulas.Select(f =>
                _formulaRepository.DeleteAsync(f.Id)
            );
            await Task.WhenAll(deleteTasks);

            _logger.LogInformation($"批量删除成功: {selectedFormulas.Count}个验方");

            // 刷新列表
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量删除失败");
            await _dialogService.ShowAlertAsync("错误", $"批量删除失败: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}

/// <summary>
/// 调用链:
/// FormulaManagementViewModel → IFormulaRepository → FormulaRepository → ApiService → HTTP → Server /api/v1/formulas
/// </summary>
```

#### 5. 验方详情编辑 - 药材配置与总价计算

```csharp
/// <summary>
/// FormulaDetailViewModel - 验方详情编辑ViewModel
/// 功能:新建/编辑/克隆验方,药材配置,总价计算,验证,使用历史查询
/// 文件: LYBT.Desktop.Formula/ViewModels/FormulaDetailViewModel.cs
/// </summary>
public class FormulaDetailViewModel : UnifiedViewModelBase
{
    private readonly IFormulaRepository _formulaRepository;
    private readonly IRegionManager _regionManager;
    private readonly ILogger<FormulaDetailViewModel> _logger;

    // 辅助类（Components）
    private readonly FormulaDataManager _dataManager;
    private readonly FormulaCommandHandler _commandHandler;
    private readonly FormulaCalculator _calculator;
    private readonly FormulaValidator _validator;

    // 验方基础信息（25个Property）
    public FormulaDto Formula { get; set; }
    public string FormulaName { get; set; }             // 验方名称
    public string Effect { get; set; }                  // 功效（如:补气养血）
    public string Usage { get; set; }                   // 用法（如:水煎服）
    public string Property { get; set; }                // 性味（如:寒凉）
    public string Remark { get; set; }                  // 备注
    public string Category { get; set; }                // 分类（如:补益方）
    public bool IsShared { get; set; } = false;         // 是否共享（默认:否）
    public bool IsEditMode { get; set; } = false;       // 是否编辑模式

    // 药材列表与总价
    public ObservableCollection<FormulaHerbItemDto> HerbItems { get; set; }
    public int HerbCount => HerbItems?.Count ?? 0;
    public decimal TotalPrice => _calculator?.CalculateTotalPrice(HerbItems) ?? 0;

    /// <summary>
    /// 保存验方（新建或更新）- 包含药材配置验证
    /// </summary>
    private async Task SaveAsync()
    {
        // 验证输入
        if (!_validator.ValidateInputs(FormulaName, Effect, HerbItems))
        {
            await _dialogService.ShowAlertAsync("验证错误", "请检查输入数据");
            return;
        }

        // 验证药材数量
        if (HerbItems == null || HerbItems.Count == 0)
        {
            await _dialogService.ShowAlertAsync("验证错误", "验方必须包含至少一味药材");
            return;
        }

        if (Formula == null || Formula.Id == Guid.Empty)
        {
            // 新建验方
            var createDto = new CreateFormulaDto
            {
                Name = FormulaName,
                Effect = Effect,
                Usage = Usage,
                Property = Property,
                Remark = Remark,
                Category = Category,
                IsShared = IsShared,
                HerbItems = HerbItems.Select(item => new CreateFormulaHerbItemDto
                {
                    HerbId = item.HerbId,
                    Dosage = item.Dosage,
                    Unit = item.Unit,
                    Notes = item.Notes
                }).ToList()
            };

            await _formulaRepository.CreateAsync(createDto);
            _logger.LogInformation($"验方创建成功: {FormulaName}");
        }
        else
        {
            // 更新验方
            var updateDto = new UpdateFormulaDto
            {
                Name = FormulaName,
                Effect = Effect,
                Usage = Usage,
                Property = Property,
                Remark = Remark,
                Category = Category,
                IsShared = IsShared,
                HerbItems = HerbItems.Select(item => new UpdateFormulaHerbItemDto
                {
                    HerbId = item.HerbId,
                    Dosage = item.Dosage,
                    Unit = item.Unit,
                    Notes = item.Notes
                }).ToList()
            };

            await _formulaRepository.UpdateAsync(Formula.Id, updateDto);
            _logger.LogInformation($"验方更新成功: {FormulaName}");
        }

        // 返回验方列表
        _regionManager.RequestNavigate("MainRegion", "FormulaManagementView");
    }

    /// <summary>
    /// 克隆验方（复制验方及药材配置）
    /// </summary>
    private async Task CopyFormulaAsync()
    {
        if (Formula == null) return;

        // 调用Repository克隆验方
        var newFormula = await _formulaRepository.CloneFormulaAsync(
            Formula.Id,
            $"{Formula.Name}_副本"
        );

        _logger.LogInformation($"验方克隆成功: {Formula.Name} → {newFormula.Name}");

        // 导航到新验方编辑页
        var parameters = new NavigationParameters
        {
            { "FormulaId", newFormula.Id },
            { "IsEditMode", true }
        };
        _regionManager.RequestNavigate("MainRegion", "FormulaDetailView", parameters);
    }

    /// <summary>
    /// 查看验方使用历史（在哪些处方中使用）
    /// </summary>
    private async Task ExecuteViewUsageHistory()
    {
        if (Formula == null) return;

        // 调用API查询使用历史
        var history = await _formulaRepository.GetUsageHistoryAsync(Formula.Id);

        // 显示使用历史对话框
        await _dialogService.ShowDialogAsync(
            "FormulaUsageHistoryDialog",
            new DialogParameters
            {
                { "FormulaId", Formula.Id },
                { "History", history }
            }
        );
    }
}

/// <summary>
/// FormulaCalculator - 验方总价计算器
/// 文件: LYBT.Desktop.Formula/ViewModels/Components/FormulaCalculator.cs
/// </summary>
public class FormulaCalculator
{
    /// <summary>
    /// 计算验方总价（所有药材价格之和）
    /// </summary>
    public decimal CalculateTotalPrice(IEnumerable<FormulaHerbItemDto> herbItems)
    {
        if (herbItems == null || !herbItems.Any())
            return 0;

        return herbItems.Sum(item =>
        {
            // 计算单味药材价格: 单价 × 用量
            var price = item.HerbPrice ?? 0;        // 药材单价（元/克）
            var dosage = item.Dosage ?? 0;          // 用量（克）
            return price * dosage;
        });
    }

    /// <summary>
    /// 计算单味药材价格
    /// </summary>
    public decimal CalculateHerbPrice(FormulaHerbItemDto item)
    {
        return (item.HerbPrice ?? 0) * (item.Dosage ?? 0);
    }
}
```

#### 6. Repository模式与三层架构

```csharp
/// <summary>
/// IFormulaRepository - 验方数据访问接口
/// 定义9个核心方法
/// 文件: LYBT.Desktop.Formula/Interfaces/IFormulaRepository.cs
/// </summary>
public interface IFormulaRepository
{
    Task<PagedResult<FormulaDto>> GetPagedAsync(int pageIndex, int pageSize, string? searchTerm = null, string? category = null);
    Task<FormulaDto> GetByIdAsync(Guid id);
    Task<FormulaDto> CreateAsync(CreateFormulaDto dto);
    Task<FormulaDto> UpdateAsync(Guid id, UpdateFormulaDto dto);
    Task DeleteAsync(Guid id);
    Task<List<FormulaDto>> SearchAsync(string keyword);
    Task<FormulaDto> CloneFormulaAsync(Guid sourceId, string newName);
    Task<List<FormulaDto>> GetPendingValidationFormulasAsync();
    Task ValidateFormulaHerbAsync(Guid formulaId);
}

/// <summary>
/// FormulaRepository - Repository实现（继承BaseApiRepository）
/// 通过ApiService与Server端交互（/api/v1/formulas）
/// 文件: LYBT.Desktop.Formula/Repositories/FormulaRepository.cs
/// </summary>
public class FormulaRepository : BaseApiRepository<FormulaDto>, IFormulaRepository
{
    private readonly IApiService _apiService;
    private readonly ILogger<FormulaRepository> _logger;

    public FormulaRepository(
        IApiService apiService,
        ILogger<FormulaRepository> logger)
        : base(apiService, logger, "formulas")
    {
        _apiService = apiService;
        _logger = logger;
    }

    /// <summary>
    /// 分页查询验方（支持名称/功效搜索和分类筛选）
    /// API: GET /api/v1/formulas?pageIndex=1&pageSize=20&searchTerm=补气&category=补益方
    /// </summary>
    public async Task<PagedResult<FormulaDto>> GetPagedAsync(
        int pageIndex,
        int pageSize,
        string? searchTerm = null,
        string? category = null)
    {
        var queryString = $"?pageIndex={pageIndex}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            queryString += $"&searchTerm={Uri.EscapeDataString(searchTerm)}";
        }
        if (!string.IsNullOrWhiteSpace(category))
        {
            queryString += $"&category={Uri.EscapeDataString(category)}";
        }

        return await _apiService.GetAsync<PagedResult<FormulaDto>>($"formulas{queryString}");
    }

    /// <summary>
    /// 克隆验方（复制验方及药材配置）
    /// API: POST /api/v1/formulas/{id}/clone?newName=XXX
    /// </summary>
    public async Task<FormulaDto> CloneFormulaAsync(Guid sourceId, string newName)
    {
        return await _apiService.PostAsync<FormulaDto>(
            $"formulas/{sourceId}/clone?newName={Uri.EscapeDataString(newName)}",
            null
        );
    }

    /// <summary>
    /// 获取待验证验方列表（包含无效药材的验方）
    /// API: GET /api/v1/formulas/pending-validation
    /// </summary>
    public async Task<List<FormulaDto>> GetPendingValidationFormulasAsync()
    {
        return await _apiService.GetAsync<List<FormulaDto>>("formulas/pending-validation");
    }

    /// <summary>
    /// 验证验方药材有效性（检查药材是否存在/被删除）
    /// API: POST /api/v1/formulas/{id}/validate-herbs
    /// </summary>
    public async Task ValidateFormulaHerbAsync(Guid formulaId)
    {
        await _apiService.PostAsync<object>($"formulas/{formulaId}/validate-herbs", null);
    }
}

/// <summary>
/// 调用链:
/// FormulaManagementViewModel → IFormulaRepository → FormulaRepository → ApiService → HTTP → Server /api/v1/formulas
///
/// 数据流:
/// 1. ViewModel调用Repository方法（如GetPagedAsync）
/// 2. Repository继承BaseApiRepository，通过ApiService发送HTTP请求
/// 3. ApiService封装HttpClient，统一处理认证、错误、序列化
/// 4. Server端FormulasController接收请求，调用FormulaService处理业务逻辑
/// 5. Server端返回FormulaDto，ApiService反序列化后返回给Repository
/// 6. Repository返回给ViewModel，ViewModel更新ObservableCollection
/// 7. WPF数据绑定自动更新UI
/// </summary>
```

#### 7. Excel批量导入验方

```csharp
/// <summary>
/// FormulaManagementViewModel - Excel批量导入验方
/// 支持智能药材匹配（名称/别名）、错误处理、导入结果统计
/// 文件: LYBT.Desktop.Formula/ViewModels/FormulaManagementViewModel.cs
/// </summary>
public class FormulaManagementViewModel : UnifiedViewModelBase
{
    /// <summary>
    /// 导入验方命令
    /// </summary>
    public AsyncDelegateCommand ImportFormulasCommand { get; }

    /// <summary>
    /// 执行Excel导入验方
    /// </summary>
    private async Task ExecuteImportFormulasAsync()
    {
        // 打开文件选择对话框
        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择验方Excel文件",
            Filter = "Excel文件|*.xlsx;*.xls",
            FilterIndex = 1
        };

        if (openFileDialog.ShowDialog() != true)
            return;

        IsBusy = true;
        try
        {
            // 读取Excel文件
            var fileBytes = await File.ReadAllBytesAsync(openFileDialog.FileName);

            // 调用API导入
            var result = await _apiService.PostAsync<ImportResult>(
                "formulas/import",
                new { File = fileBytes }
            );

            // 显示导入结果
            var message = $"导入完成！\n" +
                         $"成功: {result.SuccessCount}个\n" +
                         $"失败: {result.FailedCount}个";

            if (result.FailedCount > 0)
            {
                message += $"\n\n失败详情:\n";
                foreach (var error in result.Errors.Take(5))
                {
                    message += $"- 行{error.RowNumber}: {error.ErrorMessage}\n";
                }
                if (result.Errors.Count > 5)
                {
                    message += $"... 还有{result.Errors.Count - 5}个错误";
                }
            }

            await _dialogService.ShowAlertAsync("导入结果", message);

            // 刷新列表
            if (result.SuccessCount > 0)
            {
                await RefreshAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入验方失败");
            await _dialogService.ShowAlertAsync("错误", $"导入失败: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// 下载Excel导入模板
    /// </summary>
    private async Task ExecuteExportTemplateAsync()
    {
        var saveFileDialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "保存验方导入模板",
            FileName = $"验方导入模板_{DateTime.Now:yyyyMMdd}.xlsx",
            Filter = "Excel文件|*.xlsx"
        };

        if (saveFileDialog.ShowDialog() != true)
            return;

        IsBusy = true;
        try
        {
            // 调用API下载模板
            var fileBytes = await _apiService.GetAsync<byte[]>("formulas/template");

            // 保存文件
            await File.WriteAllBytesAsync(saveFileDialog.FileName, fileBytes);

            await _dialogService.ShowAlertAsync("成功", "模板下载成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "下载模板失败");
            await _dialogService.ShowAlertAsync("错误", $"下载失败: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// 导出验方到Excel
    /// </summary>
    private async Task ExecuteExportFormulasAsync()
    {
        var saveFileDialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "导出验方到Excel",
            FileName = $"验方列表_{DateTime.Now:yyyyMMdd}.xlsx",
            Filter = "Excel文件|*.xlsx"
        };

        if (saveFileDialog.ShowDialog() != true)
            return;

        IsBusy = true;
        try
        {
            // 调用API导出
            var fileBytes = await _apiService.GetAsync<byte[]>("formulas/export");

            // 保存文件
            await File.WriteAllBytesAsync(saveFileDialog.FileName, fileBytes);

            await _dialogService.ShowAlertAsync("成功", $"已导出 {Formulas.Count} 个验方");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出验方失败");
            await _dialogService.ShowAlertAsync("错误", $"导出失败: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}

/// <summary>
/// Server端智能药材匹配逻辑
/// 文件: LYBT.Module.Formula/Services/FormulaService.cs
/// </summary>
public class FormulaService : IFormulaService
{
    /// <summary>
    /// Excel导入验方（智能药材匹配）
    /// </summary>
    private async Task<ImportResult> ImportFromExcelAsync(Stream stream)
    {
        var result = new ImportResult();
        var formulas = ParseExcelData(stream);

        foreach (var (rowNumber, formula) in formulas)
        {
            try
            {
                // 验证必填项
                if (string.IsNullOrWhiteSpace(formula.Name))
                {
                    result.Failed.Add(new ImportError
                    {
                        RowNumber = rowNumber,
                        ErrorMessage = "验方名称不能为空",
                        Data = formula
                    });
                    continue;
                }

                // 智能匹配药材（精确匹配 + 别名匹配）
                var herbItems = new List<FormulaHerbItem>();
                foreach (var herbName in formula.HerbNames)
                {
                    var herb = await TryMatchHerbAsync(herbName);
                    if (herb == null)
                    {
                        result.Failed.Add(new ImportError
                        {
                            RowNumber = rowNumber,
                            ErrorMessage = $"找不到药材: {herbName}",
                            Data = formula
                        });
                        continue;
                    }

                    herbItems.Add(new FormulaHerbItem
                    {
                        HerbId = herb.HerbId,
                        Dosage = herb.Dosage,
                        Unit = herb.Unit
                    });
                }

                // 保存验方
                formula.HerbItems = herbItems;
                await _repository.AddAsync(formula);
                result.Succeeded.Add(formula);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"导入验方失败:行{rowNumber}");
                result.Failed.Add(new ImportError
                {
                    RowNumber = rowNumber,
                    ErrorMessage = ex.Message,
                    Data = formula
                });
            }
        }

        return result;
    }

    /// <summary>
    /// 智能匹配药材（精确匹配 + 别名匹配）
    /// </summary>
    private async Task<HerbItemData?> TryMatchHerbAsync(string herbName)
    {
        // 精确匹配
        var herb = await _herbRepository.GetByNameAsync(herbName);
        if (herb != null) return new HerbItemData { HerbId = herb.Id, Name = herb.Name };

        // 模糊匹配（中医药材别名）
        herb = await _herbRepository.SearchByAliasAsync(herbName);
        if (herb != null)
        {
            _logger.LogWarning($"使用别名匹配:{herbName} → {herb.Name}");
            return new HerbItemData { HerbId = herb.Id, Name = herb.Name };
        }

        return null; // 匹配失败
    }
}
```

#### 8. 验方药材验证 - 待验证列表管理

```csharp
/// <summary>
/// FormulaValidationViewModel - 验方药材验证ViewModel
/// 功能:待验证验方列表,药材有效性检查,批量验证
/// 文件: LYBT.Desktop.Formula/ViewModels/FormulaValidationViewModel.cs
/// </summary>
public class FormulaValidationViewModel : UnifiedViewModelBase
{
    private readonly IFormulaRepository _formulaRepository;
    private readonly IDialogService _dialogService;

    // 待验证验方列表
    public ObservableCollection<FormulaDto> PendingFormulas { get; set; }
    public FormulaDto? SelectedFormula { get; set; }

    /// <summary>
    /// 加载待验证验方列表（包含无效药材的验方）
    /// </summary>
    public async Task LoadPendingFormulasAsync()
    {
        IsBusy = true;
        try
        {
            var formulas = await _formulaRepository.GetPendingValidationFormulasAsync();

            PendingFormulas.Clear();
            foreach (var formula in formulas)
            {
                PendingFormulas.Add(formula);
            }

            _logger.LogInformation($"加载待验证验方: {formulas.Count}个");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载待验证验方失败");
            await _dialogService.ShowAlertAsync("错误", $"加载失败: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// 验证单个验方药材有效性
    /// </summary>
    private async Task ValidateFormulaAsync(FormulaDto formula)
    {
        if (formula == null) return;

        IsBusy = true;
        try
        {
            // 调用API验证药材有效性
            await _formulaRepository.ValidateFormulaHerbAsync(formula.Id);

            await _dialogService.ShowAlertAsync("成功", "验方药材验证通过");

            // 从待验证列表中移除
            PendingFormulas.Remove(formula);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"验证验方失败: {formula.Name}");
            await _dialogService.ShowAlertAsync("验证失败", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// 批量验证所有待验证验方
    /// </summary>
    private async Task ValidateAllFormulasAsync()
    {
        if (!PendingFormulas.Any())
        {
            await _dialogService.ShowAlertAsync("提示", "没有待验证的验方");
            return;
        }

        IsBusy = true;
        try
        {
            var validatedCount = 0;
            var failedCount = 0;

            foreach (var formula in PendingFormulas.ToList())
            {
                try
                {
                    await _formulaRepository.ValidateFormulaHerbAsync(formula.Id);
                    PendingFormulas.Remove(formula);
                    validatedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"验证验方失败: {formula.Name}");
                    failedCount++;
                }
            }

            await _dialogService.ShowAlertAsync(
                "批量验证完成",
                $"成功: {validatedCount}个\n失败: {failedCount}个"
            );
        }
        finally
        {
            IsBusy = false;
        }
    }
}

/// <summary>
/// Server端验方药材验证逻辑
/// 文件: LYBT.Module.Formula/Services/FormulaService.cs
/// </summary>
public async Task ValidateFormulaHerbAsync(Guid formulaId)
{
    var formula = await _repository.GetByIdWithHerbsAsync(formulaId);
    if (formula == null) throw new NotFoundException("验方不存在");

    var invalidHerbs = new List<string>();
    foreach (var item in formula.HerbItems)
    {
        var herb = await _herbRepository.GetByIdAsync(item.HerbId);
        if (herb == null || herb.IsDeleted)
        {
            invalidHerbs.Add(item.Notes ?? item.HerbId.ToString());
        }
    }

    if (invalidHerbs.Any())
    {
        throw new ValidationException(
            $"验方包含无效药材:{string.Join(", ", invalidHerbs)}"
        );
    }
}
```

#### 9. 按分类搜索验方

```csharp
/// <summary>
/// FormulaManagementViewModel - 按分类搜索验方
/// 支持验方分类筛选（补益方、清热方、解表方等）
/// 文件: LYBT.Desktop.Formula/ViewModels/FormulaManagementViewModel.cs
/// </summary>
public class FormulaManagementViewModel : UnifiedViewModelBase
{
    // 验方分类列表（从Server端加载或预定义）
    public ObservableCollection<string> Categories { get; set; } = new()
    {
        "全部",
        "补益方",       // 补气养血类方剂
        "清热方",       // 清热解毒类方剂
        "解表方",       // 发汗解表类方剂
        "理气方",       // 理气调中类方剂
        "活血方",       // 活血化瘀类方剂
        "祛湿方",       // 祛湿利水类方剂
        "止咳方",       // 止咳化痰类方剂
        "安神方"        // 安神定志类方剂
    };

    public string SelectedCategory { get; set; } = "全部";

    public AsyncDelegateCommand<string> SearchByCategoryCommand { get; }

    /// <summary>
    /// 按分类搜索验方
    /// </summary>
    private async Task SearchByCategory(string category)
    {
        SelectedCategory = category;

        // 重置到第一页
        CurrentPage = 1;

        // 重新加载数据
        await LoadPageAsync(CurrentPage);
    }

    /// <summary>
    /// 分页加载验方（支持分类筛选）
    /// </summary>
    public async Task<PagedResult<FormulaDto>> GetItemsAsync(int pageIndex, int pageSize)
    {
        // 如果选择"全部"，则不传递category参数
        var category = SelectedCategory == "全部" ? null : SelectedCategory;

        return await _formulaRepository.GetPagedAsync(
            pageIndex,
            pageSize,
            SearchText,     // 搜索关键字
            category        // 分类筛选
        );
    }
}
```

#### 10. FormulaModule注册

```csharp
/// <summary>
/// FormulaModule - Prism模块注册
/// 注册6个ViewModels、4个辅助类、5个Views、1个Repository
/// 文件: LYBT.Desktop.Formula/FormulaModule.cs
/// </summary>
public class FormulaModule : IModule
{
    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 模块初始化完成后的操作（如果需要）
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册ViewModels (6个)
        containerRegistry.Register<FormulaManagementViewModel>();
        containerRegistry.Register<FormulaDetailViewModel>();
        containerRegistry.Register<EditFormulaDialogViewModel>();
        containerRegistry.Register<FormulaValidationViewModel>();
        containerRegistry.Register<FormulaHerbItemViewModel>();

        // 注册辅助类 (4个Components)
        containerRegistry.RegisterSingleton<FormulaCalculator>();      // 单例（计算器无状态）
        containerRegistry.Register<FormulaCommandHandler>();
        containerRegistry.Register<FormulaDataManager>();
        containerRegistry.Register<FormulaValidator>();

        // 注册Views (4个)
        containerRegistry.RegisterForNavigation<FormulaManagementView>();
        containerRegistry.RegisterForNavigation<FormulaDetailView>();
        containerRegistry.RegisterDialog<EditFormulaDialog>();         // 对话框
        // Issue #1802: ViewFormulaDialog已删除（改用FormulaDetailView进行只读查看）
        containerRegistry.RegisterForNavigation<FormulaValidationView>();

        // 注册Repository
        containerRegistry.Register<IFormulaRepository, FormulaRepository>();
    }
}
```

#### 11. 共享验方管理与使用历史

```csharp
/// <summary>
/// FormulaDetailViewModel - 共享验方管理
/// 支持将验方标记为"共享"，其他医生可见
/// 文件: LYBT.Desktop.Formula/ViewModels/FormulaDetailViewModel.cs
/// </summary>
public class FormulaDetailViewModel : UnifiedViewModelBase
{
    /// <summary>
    /// 是否共享验方（共享后其他医生可见）
    /// </summary>
    public bool IsShared { get; set; } = false;

    /// <summary>
    /// 保存验方时同步IsShared属性
    /// </summary>
    private async Task SaveAsync()
    {
        var updateDto = new UpdateFormulaDto
        {
            Name = FormulaName,
            Effect = Effect,
            Usage = Usage,
            Property = Property,
            Remark = Remark,
            Category = Category,
            IsShared = IsShared,  // 共享状态
            HerbItems = HerbItems.Select(item => new UpdateFormulaHerbItemDto
            {
                HerbId = item.HerbId,
                Dosage = item.Dosage,
                Unit = item.Unit,
                Notes = item.Notes
            }).ToList()
        };

        await _formulaRepository.UpdateAsync(Formula.Id, updateDto);
        _logger.LogInformation($"验方{(IsShared ? "已共享" : "已取消共享")}: {FormulaName}");
    }

    /// <summary>
    /// 查看验方使用历史（在哪些处方中使用）
    /// </summary>
    private async Task ExecuteViewUsageHistory()
    {
        if (Formula == null) return;

        IsBusy = true;
        try
        {
            // 调用API查询使用历史
            var history = await _apiService.GetAsync<List<PrescriptionDto>>(
                $"formulas/{Formula.Id}/usage-history"
            );

            // 显示使用历史对话框
            var parameters = new DialogParameters
            {
                { "FormulaName", Formula.Name },
                { "UsageHistory", history }
            };

            await _dialogService.ShowDialogAsync("FormulaUsageHistoryDialog", parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询使用历史失败");
            await _dialogService.ShowAlertAsync("错误", $"查询失败: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}

/// <summary>
/// FormulaManagementViewModel - 筛选共享验方
/// 支持查看"我的验方"或"共享验方"
/// 文件: LYBT.Desktop.Formula/ViewModels/FormulaManagementViewModel.cs
/// </summary>
public class FormulaManagementViewModel : UnifiedViewModelBase
{
    // 验方来源筛选
    public bool ShowSharedFormulas { get; set; } = false;  // false=我的验方，true=共享验方

    /// <summary>
    /// 切换验方来源（我的验方 / 共享验方）
    /// </summary>
    private async Task ToggleFormulaSourceAsync()
    {
        ShowSharedFormulas = !ShowSharedFormulas;

        // 重新加载列表
        CurrentPage = 1;
        await LoadPageAsync(CurrentPage);
    }

    /// <summary>
    /// 分页加载验方（支持共享验方筛选）
    /// </summary>
    public async Task<PagedResult<FormulaDto>> GetItemsAsync(int pageIndex, int pageSize)
    {
        var queryString = $"?pageIndex={pageIndex}&pageSize={pageSize}";
        queryString += $"&isShared={ShowSharedFormulas}";  // 筛选共享验方

        return await _apiService.GetAsync<PagedResult<FormulaDto>>($"formulas{queryString}");
    }
}
```

## 🎨 模块架构图

```
┌─────────────────────────────────────────────────────────────────┐
│                    LYBT.Desktop.Formula                          │
│                      (验方管理模块)                                │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ 依赖
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                        Prism.DryIoc                              │
│              (模块化 + 依赖注入 + 区域导航)                        │
└─────────────────────────────────────────────────────────────────┘
                              │
        ┌─────────────────────┼─────────────────────┐
        │                     │                     │
        ↓                     ↓                     ↓
┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│  ViewModels  │    │    Views     │    │ Repository   │
│   (6个VM)    │    │   (5个View)  │    │   (1个Repo)  │
└──────────────┘    └──────────────┘    └──────────────┘
        │                     │                     │
        │                     │                     │
        ↓                     ↓                     ↓
┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│ Components   │    │ XAML + Code  │    │  ApiService  │
│  (4个辅助类)  │    │   Behind     │    │   (HTTP)     │
└──────────────┘    └──────────────┘    └──────────────┘
        │                                       │
        └───────────────┬───────────────────────┘
                        │
                        ↓
        ┌───────────────────────────────┐
        │    Server端 /api/v1/formulas   │
        │       (FormulaService)         │
        └───────────────────────────────┘

**MVVM数据流**:
1. View (XAML) ← DataBinding → ViewModel (ObservableCollection)
2. ViewModel → Command → IFormulaRepository → FormulaRepository
3. FormulaRepository → ApiService → HTTP Request → Server端
4. Server端返回 FormulaDto → ApiService → FormulaRepository → ViewModel
5. ViewModel更新ObservableCollection → WPF自动更新View

**Repository模式**:
- IFormulaRepository (接口) ← FormulaManagementViewModel/FormulaDetailViewModel依赖
- FormulaRepository (实现) → 继承BaseApiRepository → 使用ApiService
- ApiService → 封装HttpClient → 统一处理认证、错误、序列化

**Components辅助类**:
- FormulaCalculator: 计算验方总价（所有药材价格之和）
- FormulaCommandHandler: 封装命令逻辑（如批量删除、导入导出）
- FormulaDataManager: 封装数据加载逻辑（如分页查询、搜索）
- FormulaValidator: 封装验证逻辑（如必填项、药材数量验证）
```

## 🎯 设计原则

### 1. MVVM架构与数据绑定

**核心原则**:
- **ViewModel**:负责业务逻辑和数据处理，不直接操作UI
- **View (XAML)**:通过DataBinding绑定ViewModel属性，实现UI自动更新
- **Repository**:封装数据访问逻辑，ViewModel通过接口依赖Repository

**设计优势**:
- ✅ UI与业务逻辑完全分离，易于单元测试（ViewModel可独立测试）
- ✅ ObservableCollection自动触发UI更新，无需手动刷新
- ✅ AsyncDelegateCommand支持CanExecute逻辑，自动管理按钮禁用状态
- ✅ Prism RegionManager支持模块化导航，避免View间直接依赖

**反模式（禁止）**:
- ❌ ViewModel中直接操作UI控件（如MessageBox.Show）
- ❌ View CodeBehind中包含业务逻辑
- ❌ ViewModel直接依赖具体Repository实现（应依赖接口）

**代码示例**:
```csharp
// ✅ 正确: ViewModel通过IDialogService显示消息
await _dialogService.ShowAlertAsync("提示", "保存成功");

// ❌ 错误: ViewModel直接操作UI
MessageBox.Show("保存成功");

// ✅ 正确: ViewModel依赖IFormulaRepository接口
private readonly IFormulaRepository _formulaRepository;

// ❌ 错误: ViewModel依赖具体实现
private readonly FormulaRepository _formulaRepository;
```

### 2. Repository模式与三层架构

**核心原则**:
- **ViewModel层**:负责UI业务逻辑，通过Repository获取数据
- **Repository层**:封装数据访问，继承BaseApiRepository，使用ApiService
- **ApiService层**:封装HTTP通信，统一处理认证、错误、序列化

**设计优势**:
- ✅ ViewModel不关心数据来源（HTTP API、本地缓存、Mock数据等）
- ✅ Repository返回裸类型（FormulaDto），Client端不需要Result<T>包装
- ✅ ApiService统一处理JWT认证、错误码映射、重试机制
- ✅ 易于切换数据源（如离线模式、Mock测试）

**调用链**:
```
FormulaManagementViewModel
  → IFormulaRepository.GetPagedAsync()
  → FormulaRepository.GetPagedAsync()
  → BaseApiRepository (基类)
  → ApiService.GetAsync<PagedResult<FormulaDto>>()
  → HttpClient.SendAsync()
  → Server端 /api/v1/formulas
```

**反模式（禁止）**:
- ❌ ViewModel直接调用HttpClient（绕过Repository）
- ❌ Repository返回Result<T>（Client端不需要）
- ❌ ViewModel依赖Server端Services（会导致运行时崩溃）

### 3. Components辅助类与职责分离

**核心原则**:
- **FormulaCalculator**:验方总价计算器（单例，无状态）
- **FormulaCommandHandler**:命令处理器（封装批量删除、导入导出等逻辑）
- **FormulaDataManager**:数据管理器（封装分页加载、搜索等逻辑）
- **FormulaValidator**:验证器（封装必填项、药材数量验证等逻辑）

**设计优势**:
- ✅ 降低ViewModel复杂度（FormulaDetailViewModel从1000+行降至675行）
- ✅ 提高代码复用性（多个ViewModel共享计算器、验证器）
- ✅ 易于单元测试（辅助类可独立测试）
- ✅ 符合单一职责原则（每个类只负责一件事）

**反模式（禁止）**:
- ❌ 所有逻辑都写在ViewModel中（导致ViewModel过于庞大）
- ❌ 辅助类包含业务逻辑（应只包含纯计算或验证逻辑）

### 4. 验方克隆功能与数据复用

**核心原则**:
- **克隆验方**:复制验方及药材配置，生成新验方（名称自动添加"_副本"后缀）
- **验方模板化**:医生可将常用验方克隆后微调，提高开方效率
- **数据一致性**:克隆后的验方独立存储，修改不影响原验方

**设计优势**:
- ✅ 减少重复录入工作（复制验方比重新创建快10倍）
- ✅ 支持个性化调整（克隆后可修改药材用量、添加/删除药材）
- ✅ 验方库积累（将处方转换为验方，形成个人经验库）

**代码示例**:
```csharp
// 克隆验方（Server端API）
POST /api/v1/formulas/{id}/clone?newName=六味地黄丸_副本

// 克隆后自动创建新验方，包含原验方的所有药材配置
// 医生可以在克隆后的验方上进行个性化调整
```

### 5. 待验证验方管理与数据完整性

**核心原则**:
- **待验证列表**:定期检查验方中的药材是否被删除或禁用
- **药材有效性验证**:确保验方中的药材在Herbs表中存在且未被删除
- **数据完整性保护**:防止使用包含无效药材的验方创建处方

**设计优势**:
- ✅ 及时发现数据问题（药材被删除后及时通知医生）
- ✅ 提高处方质量（避免使用无效药材）
- ✅ 支持批量验证（一次性检查所有待验证验方）

**代码示例**:
```csharp
// 获取待验证验方列表
GET /api/v1/formulas/pending-validation

// 返回包含无效药材的验方列表
// 医生可以逐个验证或批量验证

// 验证单个验方
POST /api/v1/formulas/{id}/validate-herbs

// Server端检查所有药材是否存在且未被删除
// 如果验证失败，抛出ValidationException并返回无效药材列表
```

### 6. Excel导入导出与智能药材匹配

**核心原则**:
- **智能匹配**:支持药材名称精确匹配和别名模糊匹配（如"当归"可匹配"当归头"、"当归尾"）
- **错误处理**:导入失败时返回详细错误信息（行号、错误原因、原始数据）
- **批量操作**:支持一次性导入/导出数百个验方

**设计优势**:
- ✅ 快速批量录入（从Excel导入比手动录入快100倍）
- ✅ 降低学习成本（医生熟悉Excel，无需学习新界面）
- ✅ 支持离线编辑（在Excel中编辑验方，完成后一次性导入）
- ✅ 智能容错（别名匹配减少导入失败率）

**代码示例**:
```csharp
// 下载导入模板
GET /api/v1/formulas/template

// 导入验方（智能匹配药材）
POST /api/v1/formulas/import

// 返回导入结果
{
  "SuccessCount": 50,
  "FailedCount": 5,
  "Errors": [
    { "RowNumber": 3, "ErrorMessage": "找不到药材: 人参片", "Data": {...} },
    { "RowNumber": 7, "ErrorMessage": "验方名称不能为空", "Data": {...} }
  ]
}

// 导出验方到Excel
GET /api/v1/formulas/export
```

### 7. 异步优先与UI响应性

**核心原则**:
- **全异步方法**:所有数据操作使用async/await，避免阻塞UI线程
- **IsBusy模式**:Loading状态管理，避免重复提交
- **AsyncDelegateCommand**:Prism异步命令，支持自动禁用按钮

**设计优势**:
- ✅ UI始终保持响应（即使数据加载耗时5秒，用户仍可操作其他按钮）
- ✅ 防止重复提交（IsBusy=true时自动禁用保存按钮）
- ✅ 用户体验更好（显示Loading动画而不是界面卡顿）

**反模式（禁止）**:
- ❌ 同步阻塞方法（如`Task.Wait()`、`.Result`）
- ❌ 未设置IsBusy导致重复提交
- ❌ 使用DelegateCommand而非AsyncDelegateCommand（无法自动管理Loading状态）

**代码示例**:
```csharp
// ✅ 正确: 使用AsyncDelegateCommand
public AsyncDelegateCommand SaveCommand { get; }

private async Task SaveAsync()
{
    IsBusy = true;  // 自动禁用保存按钮
    try
    {
        await _formulaRepository.UpdateAsync(Formula.Id, updateDto);
        await _dialogService.ShowAlertAsync("成功", "保存成功");
    }
    finally
    {
        IsBusy = false;  // 恢复保存按钮
    }
}

// ❌ 错误: 使用同步阻塞方法
private void Save()
{
    var task = _formulaRepository.UpdateAsync(Formula.Id, updateDto);
    task.Wait();  // ❌ 阻塞UI线程
}
```

## 📚 详细文档

- **完整模块文档**:[docs/reference/modules/formula/](../../../../docs/reference/modules/formula/) *(待创建)*
- **架构设计**:[docs/explanation/architecture/client/formula-design.md](../../../../docs/explanation/architecture/client/formula-design.md) *(待创建)*
- **开发指南**:[docs/how-to-guides/client/formula-development.md](../../../../docs/how-to-guides/client/formula-development.md) *(待创建)*
- **Server端Formula模块**:[docs/reference/modules/formula/server.md](../../../../docs/reference/modules/formula/server.md) *(待创建)*

---

**最后更新**:2025-10-29
**维护负责**:Client端开发组
