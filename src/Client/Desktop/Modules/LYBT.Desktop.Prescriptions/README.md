# LYBT.Desktop.Prescriptions - 处方管理模块

## 📦 项目定位

- **层级**:Client端 > Desktop端 > Modules层（业务模块）
- **类型**:WPF桌面客户端业务模块
- **职责**:提供中医处方开具和管理的用户界面,支持药材选择、验方模板加载、剂量计算、配伍检查、价格预览和处方打印等核心功能。作为MedicalCase看诊流程的Step2组件,实现ISaveable接口契约与MedicalCaseFlowViewModel集成。采用**Dialog-based架构**,通过对话框（PrescriptionEditorDialog、HerbSelectionDialog、FormulaTemplateDialog）组织复杂交互。支持从Herbs模块选择药材、从Formula模块加载验方模板,提供完整的处方开具与管理能力。

## 📂 代码结构

```
LYBT.Desktop.Prescriptions/
├── Components/                                     # 通用组件(2个文件)
│   ├── BasicValidator.cs                          # 基础验证器
│   └── PriceCalculator.cs                         # 价格计算器
├── Constants/                                      # 常量定义(1个文件)
│   └── PrescriptionConstants.cs                   # 处方常量(状态、默认值)
├── Interfaces/                                     # 接口定义(1个文件)
│   └── IPrescriptionRepository.cs                 # 处方仓储接口
├── Models/                                         # 本地模型(2个文件)
│   ├── PrescriptionItem.cs                        # 处方条目模型
│   └── PrescriptionPrintDto.cs                    # 打印数据传输对象
├── Services/                                       # 服务层(4个文件)
│   ├── IPrescriptionPrintService.cs               # 打印服务接口
│   ├── PrescriptionEditorService.cs               # 编辑服务(处方数据管理)
│   ├── PrescriptionFlowDocumentBuilder.cs         # FlowDocument构建器(打印文档生成)
│   └── PrescriptionPrintService.cs                # 打印服务实现(WPF打印)
├── ViewModels/                                     # 视图模型层(9个文件)
│   ├── Components/                                 # ViewModel组件(5个文件)
│   │   ├── PrescriptionCalculator.cs              # 价格计算组件(总价、折扣、单价)
│   │   ├── PrescriptionCommandHandler.cs          # 命令处理组件(添加/删除/编辑药材)
│   │   ├── PrescriptionDataManager.cs             # 数据管理组件(加载/保存/重置)
│   │   ├── PrescriptionEventCoordinator.cs        # 事件协调组件(HasChanges标记)
│   │   └── PrescriptionValidator.cs               # 验证组件(药材条目、剂量、价格)
│   ├── FormulaTemplateDialogViewModel.cs          # 验方模板对话框ViewModel(加载验方并应用到处方)
│   ├── HerbSelectionDialogViewModel.cs            # 药材选择对话框ViewModel(从Herbs模块选择药材)
│   ├── PrescriptionEditorDialogViewModel.cs       # 处方编辑对话框ViewModel(30个属性+15个方法)
│   ├── PrescriptionItemRow.cs                     # 处方条目行(DataGrid绑定)
│   ├── PrescriptionItemViewModel.cs               # 处方条目ViewModel(单个药材条目)
│   ├── PrescriptionManagementViewModel.cs         # 处方管理ViewModel(25个属性+20个命令+20个方法)
│   ├── PrescriptionViewModel.cs                   # 处方ViewModel(单个处方详情)
│   └── SelectFormulaDialogViewModel.cs            # 选择验方对话框ViewModel(验方列表选择)
├── Views/                                          # WPF视图层(12个文件:6个XAML+6个CodeBehind)
│   ├── FormulaTemplateDialog.xaml                 # 验方模板对话框视图
│   ├── FormulaTemplateDialog.xaml.cs              # 验方模板对话框CodeBehind
│   ├── HerbSelectionDialog.xaml                   # 药材选择对话框视图
│   ├── HerbSelectionDialog.xaml.cs                # 药材选择对话框CodeBehind
│   ├── PrescriptionEditorDialog.xaml              # 处方编辑对话框视图
│   ├── PrescriptionEditorDialog.xaml.cs           # 处方编辑对话框CodeBehind
│   ├── PrescriptionManagementView.xaml            # 处方管理视图
│   ├── PrescriptionManagementView.xaml.cs         # 处方管理CodeBehind
│   ├── PrescriptionView.xaml                      # 处方详情视图
│   ├── PrescriptionView.xaml.cs                   # 处方详情CodeBehind
│   ├── SelectFormulaDialog.xaml                   # 选择验方对话框视图
│   └── SelectFormulaDialog.xaml.cs                # 选择验方对话框CodeBehind
├── PrescriptionsModule.cs                          # Prism模块注册(OnInitialized + RegisterTypes)
├── LYBT.Desktop.Prescriptions.csproj               # 项目文件
└── README.md                                       # 本文档
```

**说明**:
- **Components**:2个通用组件,提供基础验证和价格计算功能
- **Constants**:1个常量文件,定义处方状态和默认值
- **Interfaces**:1个仓储接口,定义处方数据访问契约
- **Models**:2个本地模型,处方条目和打印DTO
- **Services**:4个服务文件,处理处方编辑逻辑和打印文档生成
- **ViewModels**:9个ViewModel(含5个组件),668行PrescriptionEditorDialogViewModel和597行PrescriptionManagementViewModel为核心
- **ViewModels/Components**:5个组件化ViewModel,分离计算器、命令处理器、数据管理器、事件协调器、验证器逻辑
- **Views**:6个对话框和视图,12个文件(XAML+CodeBehind)
- **Dialog-based架构**:通过对话框组织复杂交互(编辑处方、选择药材、选择验方)
- **ISaveable接口契约**:PrescriptionEditorDialogViewModel实现ISaveable,与MedicalCaseFlowViewModel集成
- **Herbs/Formula集成**:通过HerbSelectionDialog选择药材,通过FormulaTemplateDialog加载验方

## 🔗 依赖关系

### 依赖的项目
1. **LYBT.Desktop.Core** - 核心库(UnifiedViewModelBase、INavigationService、DialogService等)
2. **LYBT.Desktop.Contracts** - 契约接口(ISaveable、IValidatable、IDataContext等)
3. **LYBT.Desktop.Presentation** - 展示层基础(ViewModelBase、DelegateCommand等)
4. **LYBT.Desktop.Foundation** - 基础设施(BaseApiRepository、IApiService、HttpClient等)
5. **LYBT.Shared.Models** - 共享DTO模型(PrescriptionDto、CreatePrescriptionDto、HerbDto、FormulaDto等)
6. **LYBT.Shared.Interfaces** - 共享接口定义
7. **Prism.Core** - Prism核心库(IModule、IRegion、IEventAggregator等)
8. **Prism.DryIoc** - Prism依赖注入容器

### 被依赖项目
1. **LYBT.Desktop.Shell** - Desktop端主程序(通过Prism模块化加载Prescriptions模块)
2. **LYBT.Desktop.MedicalCase** - 医案模块(MedicalCaseFlowViewModel通过ISaveable接口调用处方功能)

### NuGet包
- **Prism.DryIoc** (8.x) - MVVM框架与依赖注入容器
- **MaterialDesignThemes** (5.1.x) - Material Design UI组件库
- **Microsoft.Extensions.Logging.Abstractions** (8.0.x) - 日志抽象
- **System.Text.Json** (8.0.x) - JSON序列化

## 🛠 技术栈

- **.NET 8**: 基础框架
- **WPF (Windows Presentation Foundation)**: UI框架
- **Prism.DryIoc 8.x**: MVVM框架、模块化、依赖注入、区域导航、事件聚合器
- **MaterialDesignThemes 5.1.x**: Material Design风格的UI组件库
- **XAML**: WPF声明式UI标记语言
- **Data Binding**: WPF数据绑定机制(OneWay、TwoWay、UpdateSourceTrigger)
- **ICommand & AsyncDelegateCommand**: Prism异步命令模式
- **ObservableCollection<T>**: 集合变更通知,自动更新UI
- **INotifyPropertyChanged**: 属性变更通知接口
- **Dependency Injection**: 构造函数注入,避免ServiceLocator反模式
- **Repository Pattern**: 三层架构(ViewModel → Repository → BaseApiRepository → IApiService → HttpClient)
- **ISaveable/IValidatable Interface**: 接口契约模式,与MedicalCase集成
- **Dialog-based Architecture**: 对话框驱动的复杂交互组织方式
- **Async/Await Pattern**: 全异步I/O操作,保证UI响应性
- **IsBusy Pattern**: 加载状态管理,防止重复操作

##  快速开始

此项目是一个Prism模块库,作为Desktop端的一部分被 `LYBT.Desktop.Shell` 在启动时动态加载。无法独立运行。

```bash
# 构建此项目
dotnet build src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/LYBT.Desktop.Prescriptions.csproj

# 或构建整个Desktop端解决方案
dotnet build src/Client/Desktop/LYBT.Desktop.sln
```

**集成说明**:

### 1. Shell加载Prescriptions模块(WhenAvailable模式)

```csharp
// LYBT.Desktop.Shell/App.xaml.cs
protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
{
    // 按需加载处方模块(医案流程启动时自动加载)
    moduleCatalog.AddModule<PrescriptionsModule>(
        InitializationMode.WhenAvailable  // Shell启动后自动加载
    );
}
```

### 2. PrescriptionEditorDialogViewModel核心属性与方法

**核心属性**（约30个）:

| 属性名称 | 类型 | 说明 |
|---------|------|------|
| **处方基础信息** | | |
| `PrescriptionId` | `Guid?` | 处方ID(新建为null,编辑时有值) |
| `PrescriptionNo` | `string` | 处方编号(自动生成,如RX20250129001) |
| `DosageCount` | `int` | 剂数(默认7剂,影响总价计算) |
| `Usage` | `string` | 用法(如:水煎服,每日两次,饭后温服) |
| `MedicalAdvice` | `string` | 医嘱(如:忌食生冷、辛辣) |
| `Remark` | `string` | 备注 |
| `Discount` | `decimal` | 折扣(0-1,如0.9表示9折) |
| `TotalAmount` | `decimal` | 总金额(单价×剂数×折扣,自动计算) |
| **状态与控制** | | |
| `OriginalPrescription` | `PrescriptionDto?` | 原始处方数据(用于HasActualChanges比对) |
| `IsReadOnly` | `bool` | 只读模式(已确认/已配药的处方不可编辑) |
| `HasChanges` | `bool` | 数据变更标记(控制保存按钮启用) |
| `IsValidationEnabled` | `bool` | 验证启用标记 |
| `ReadOnlyReason` | `string` | 只读原因说明(如"处方已确认,无法修改") |
| `CanEdit` | `bool` | 可编辑标记(=!IsReadOnly) |
| `ChangeInfo` | `string` | 变更信息提示 |
| `Title` | `string` | 对话框标题(新建处方/编辑处方) |
| **命令** | | |
| `SaveCommand` | `AsyncDelegateCommand` | 保存处方(验证→保存→触发RequestClose) |
| `CancelCommand` | `DelegateCommand` | 取消编辑(HasChanges时弹出确认对话框) |
| `ResetCommand` | `DelegateCommand` | 重置表单(恢复到OriginalPrescription) |
| `ValidateCommand` | `DelegateCommand` | 触发验证(调用ValidateAll) |
| `AddHerbCommand` | `AsyncDelegateCommand` | 添加药材(打开HerbSelectionDialog) |
| `EditHerbCommand` | `AsyncDelegateCommand<PrescriptionItemRow>` | 编辑药材条目 |
| `RemoveHerbCommand` | `DelegateCommand<PrescriptionItemRow>` | 删除药材条目 |
| `LoadFormulaTemplateCommand` | `AsyncDelegateCommand` | 加载验方模板(打开FormulaTemplateDialog) |
| `PreviewCommand` | `AsyncDelegateCommand` | 预览处方(打开打印预览) |

**核心方法**（约15个）:

| 方法名称 | 返回类型 | 说明 |
|---------|---------|------|
| **对话框生命周期** | | |
| `OnDialogOpened(IDialogParameters)` | `Task` | 对话框打开时初始化(接收MedicalCaseId/PrescriptionId参数,加载处方数据) |
| `OnDialogClosed()` | `void` | 对话框关闭时清理资源 |
| `CanCloseDialog()` | `bool` | 检查是否可关闭对话框(HasChanges时弹出确认) |
| **数据加载与保存** | | |
| `LoadPrescriptionAsync(Guid)` | `Task` | 异步加载处方数据(含药材条目) |
| `LoadFromPrescription(PrescriptionDto)` | `void` | 从DTO恢复ViewModel属性 |
| `SaveAsync()` | `Task` | 异步保存处方(新建调CreateAsync/编辑调UpdateAsync) |
| **验证与状态管理** | | |
| `ValidateAll()` | `bool` | 验证所有字段(剂数>0、药材条目>0、剂量有效) |
| `ValidateAllWrapper()` | `void` | 验证包装方法(更新ValidationMessage) |
| `MarkAsChanged()` | `void` | 标记数据已变更(HasChanges=true) |
| `HasActualChanges()` | `bool` | 检查是否有实际变更(对比OriginalPrescription) |
| **命令执行** | | |
| `Cancel()` | `void` | 取消编辑(HasChanges时弹出确认对话框) |
| `Reset()` | `void` | 重置表单(恢复到OriginalPrescription) |
| **命令状态** | | |
| `CanSave()` | `bool` | 检查是否可保存(HasChanges && !IsReadOnly) |
| `CanReset()` | `bool` | 检查是否可重置(HasChanges) |
| `UpdateCommandStates()` | `void` | 更新命令状态(RaiseCanExecuteChanged) |

### 3. PrescriptionManagementViewModel核心属性与方法

**核心属性**（约25个）:

| 属性名称 | 类型 | 说明 |
|---------|------|------|
| **数据集合** | | |
| `Prescriptions` | `ObservableCollection<PrescriptionDto>` | 处方列表(DataGrid数据源) |
| `SelectedPrescription` | `PrescriptionDto?` | 当前选中的处方(触发命令状态更新) |
| **搜索与过滤** | | |
| `SearchText` | `string` | 搜索关键字(按处方编号/患者姓名搜索) |
| `StartDate` | `DateTime?` | 开始日期(按日期范围过滤) |
| `EndDate` | `DateTime?` | 结束日期(按日期范围过滤) |
| **分页参数** | | |
| `CurrentPage` | `int` | 当前页码(从1开始) |
| `PageSize` | `int` | 每页数量(默认20) |
| `TotalCount` | `int` | 总记录数(用于计算总页数) |
| **命令状态** | | |
| `CanCreate` | `bool` | 可创建处方(始终true) |
| `CanDelete` | `bool` | 可删除处方(SelectedPrescription!=null且状态为Draft) |
| `CanClone` | `bool` | 可克隆处方(SelectedPrescription!=null) |
| `CanExport` | `bool` | 可导出(Prescriptions.Count>0) |
| `CanSearch` | `bool` | 可搜索(!string.IsNullOrWhiteSpace(SearchText)) |
| `CanViewDetail` | `bool` | 可查看详情(SelectedPrescription!=null) |

**核心命令**（约20个）:

| 命令名称 | 类型 | 说明 |
|---------|------|------|
| `LoadDataCommand` | `AsyncDelegateCommand` | 加载数据(分页查询处方列表) |
| `SearchCommand` | `AsyncDelegateCommand` | 搜索处方(按SearchText/StartDate/EndDate) |
| `CreateCommand` | `DelegateCommand` | 创建处方(打开PrescriptionEditorDialog) |
| `EditCommand` | `DelegateCommand` | 编辑处方(打开PrescriptionEditorDialog) |
| `DeleteCommand` | `AsyncDelegateCommand` | 删除处方(确认后调DeleteAsync) |
| `ViewDetailCommand` | `DelegateCommand` | 查看详情(导航到PrescriptionView) |
| `PrintCommand` | `DelegateCommand` | 打印处方(调用PrescriptionPrintService) |
| `RefreshCommand` | `AsyncDelegateCommand` | 刷新列表(重新加载当前页) |
| `PreviousPageCommand` | `AsyncDelegateCommand` | 上一页(CurrentPage--) |
| `NextPageCommand` | `AsyncDelegateCommand` | 下一页(CurrentPage++) |
| `AddPrescriptionCommand` | `DelegateCommand` | 添加处方(=CreateCommand) |
| `ClearFiltersCommand` | `DelegateCommand` | 清除过滤器(SearchText/StartDate/EndDate=null) |
| `ExportPrescriptionsCommand` | `AsyncDelegateCommand` | 导出处方到Excel |
| `ViewPrescriptionCommand` | `DelegateCommand<PrescriptionDto>` | 查看指定处方(DataGrid双击) |
| `EditPrescriptionCommand` | `DelegateCommand<PrescriptionDto>` | 编辑指定处方(DataGrid右键菜单) |
| `CopyPrescriptionCommand` | `DelegateCommand<PrescriptionDto>` | 克隆处方(复制现有处方创建新处方) |
| `DeletePrescriptionCommand` | `AsyncDelegateCommand<PrescriptionDto>` | 删除指定处方(DataGrid右键菜单) |
| `ViewPatientHistoryCommand` | `DelegateCommand<PrescriptionDto>` | 查看患者历史处方 |

**核心方法**（约20个）:

| 方法名称 | 返回类型 | 说明 |
|---------|---------|------|
| **初始化与加载** | | |
| `InitializeAsync()` | `Task` | 异步初始化(调用LoadDataAsync加载第一页数据) |
| `LoadDataAsync()` | `Task` | 异步加载数据(调用PrescriptionApi.GetPagedAsync) |
| `SearchAsync()` | `Task` | 异步搜索(重置CurrentPage=1并调用LoadDataAsync) |
| `RefreshAsync()` | `Task` | 异步刷新(调用LoadDataAsync重新加载当前页) |
| **CRUD操作** | | |
| `Create()` | `void` | 创建处方(打开PrescriptionEditorDialog,传入MedicalCaseId) |
| `Edit()` | `void` | 编辑处方(打开PrescriptionEditorDialog,传入PrescriptionId) |
| `DeleteAsync()` | `Task` | 异步删除处方(弹出确认对话框→调用API→刷新列表) |
| `ViewDetail()` | `void` | 查看详情(导航到PrescriptionView,传入PrescriptionId) |
| **打印与导出** | | |
| `Print()` | `void` | 打印处方(调用PrescriptionPrintService.PrintAsync) |
| `ExportPrescriptionsAsync()` | `Task` | 异步导出处方到Excel(调用API.ExportAsync) |
| **分页导航** | | |
| `PreviousPageAsync()` | `Task` | 异步上一页(CurrentPage--并调用LoadDataAsync) |
| `NextPageAsync()` | `Task` | 异步下一页(CurrentPage++并调用LoadDataAsync) |
| **过滤器操作** | | |
| `ClearFilters()` | `void` | 清除过滤器(SearchText/StartDate/EndDate=null) |
| **其他操作** | | |
| `ViewPrescriptionItem(PrescriptionItemDto)` | `void` | 查看药材条目详情 |
| `EditPrescriptionItem(PrescriptionItemDto)` | `void` | 编辑药材条目 |
| `ViewPatientHistory(PrescriptionDto)` | `void` | 查看患者历史处方 |
| `CopyPrescription(PrescriptionDto)` | `void` | 克隆处方(复制现有处方创建新处方) |
| `DeletePrescriptionItemAsync(PrescriptionItemDto)` | `Task` | 异步删除药材条目 |
| **命令状态管理** | | |
| `CanEditInternal()` | `bool` | 内部检查是否可编辑 |
| `CanDeleteInternal()` | `bool` | 内部检查是否可删除 |
| `CanViewDetailInternal()` | `bool` | 内部检查是否可查看详情 |
| `CanPrint()` | `bool` | 检查是否可打印 |
| `CanPreviousPage()` | `bool` | 检查是否可上一页 |
| `CanNextPage()` | `bool` | 检查是否可下一页 |
| `UpdateCommandStates()` | `void` | 更新所有命令状态(RaiseCanExecuteChanged) |

### 4. 处方编辑器对话框 - 添加药材与总价计算

```csharp
/// <summary>
/// 处方编辑对话框ViewModel - 实现ISaveable接口（与MedicalCase集成）
/// 核心功能:添加药材、计算总价、验证处方、保存处方
/// </summary>
public class PrescriptionEditorDialogViewModel : UnifiedViewModelBase, ISaveable, IValidatable, IDialogAware
{
    private readonly IPrescriptionRepository _prescriptionApi;
    private readonly IMedicalCaseRepository _medicalCaseRepository;

    // 处方基础信息
    public Guid? PrescriptionId { get; set; }           // 处方ID(新建为null)
    public string PrescriptionNo { get; set; }          // 处方编号(自动生成)
    public int DosageCount { get; set; } = 7;          // 剂数(默认7剂)
    public string Usage { get; set; }                   // 用法(如:水煎服,每日两次)
    public string MedicalAdvice { get; set; }           // 医嘱(如:忌食生冷)
    public string Remark { get; set; }                  // 备注
    public decimal Discount { get; set; } = 1.0m;      // 折扣(默认无折扣)
    public decimal TotalAmount { get; private set; }    // 总金额(自动计算)

    // 状态控制
    public bool IsReadOnly { get; set; }                // 只读模式(已确认/已配药的处方不可编辑)
    public bool HasChanges { get; set; }                // 数据变更标记
    public PrescriptionDto? OriginalPrescription { get; set; }  // 原始处方(用于HasActualChanges比对)

    // 药材条目集合
    public ObservableCollection<PrescriptionItemRow> HerbItems { get; set; }

    // 命令
    public AsyncDelegateCommand SaveCommand { get; }
    public DelegateCommand CancelCommand { get; }
    public DelegateCommand ResetCommand { get; }
    public AsyncDelegateCommand AddHerbCommand { get; }
    public AsyncDelegateCommand<PrescriptionItemRow> EditHerbCommand { get; }
    public DelegateCommand<PrescriptionItemRow> RemoveHerbCommand { get; }
    public AsyncDelegateCommand LoadFormulaTemplateCommand { get; }
    public AsyncDelegateCommand PreviewCommand { get; }
    public DelegateCommand ValidateCommand { get; }

    // IDialogAware事件
    public event Action<IDialogResult> RequestClose;

    public PrescriptionEditorDialogViewModel(
        IPrescriptionRepository prescriptionApi,
        IMedicalCaseRepository medicalCaseRepository,
        IDialogService dialogService,
        ILogger<PrescriptionEditorDialogViewModel> logger)
    {
        _prescriptionApi = prescriptionApi;
        _medicalCaseRepository = medicalCaseRepository;

        HerbItems = new ObservableCollection<PrescriptionItemRow>();

        // 初始化命令
        SaveCommand = new AsyncDelegateCommand(SaveAsync, CanSave);
        CancelCommand = new DelegateCommand(Cancel);
        ResetCommand = new DelegateCommand(Reset, CanReset);
        AddHerbCommand = new AsyncDelegateCommand(AddHerbAsync);
        EditHerbCommand = new AsyncDelegateCommand<PrescriptionItemRow>(EditHerbAsync);
        RemoveHerbCommand = new DelegateCommand<PrescriptionItemRow>(RemoveHerb);
        LoadFormulaTemplateCommand = new AsyncDelegateCommand(LoadFormulaTemplateAsync);
        PreviewCommand = new AsyncDelegateCommand(PreviewAsync);
        ValidateCommand = new DelegateCommand(ValidateAllWrapper);

        // 监听属性变更 → 重新计算总价
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(DosageCount) || e.PropertyName == nameof(Discount))
            {
                CalculateTotalAmount();
                MarkAsChanged();
            }
        };

        // 监听HerbItems变更 → 重新计算总价
        HerbItems.CollectionChanged += (s, e) =>
        {
            CalculateTotalAmount();
            MarkAsChanged();
        };
    }

    // 添加药材(打开HerbSelectionDialog)
    private async Task AddHerbAsync()
    {
        try
        {
            IsBusy = true;

            var parameters = new DialogParameters();
            _dialogService.ShowDialog(
                "HerbSelectionDialog",
                parameters,
                result =>
                {
                    if (result.Result == ButtonResult.OK)
                    {
                        // 获取选中的药材列表
                        var selectedHerbs = result.Parameters.GetValue<List<HerbDto>>("SelectedHerbs");

                        foreach (var herb in selectedHerbs)
                        {
                            // 检查药材是否已存在
                            var existingItem = HerbItems.FirstOrDefault(x => x.HerbId == herb.Id);
                            if (existingItem != null)
                            {
                                // 药材已存在,剂量+1
                                existingItem.Dosage += 1;
                            }
                            else
                            {
                                // 添加新药材条目
                                HerbItems.Add(new PrescriptionItemRow
                                {
                                    HerbId = herb.Id,
                                    HerbName = herb.Name,
                                    Dosage = herb.DefaultDosage ?? 10m,  // 默认剂量(克)
                                    Unit = herb.DefaultUnit ?? "克",      // 默认单位
                                    UnitPrice = herb.UnitPrice ?? 0m,     // 单价(元/克)
                                    Notes = string.Empty
                                });
                            }
                        }

                        _logger.LogInformation($"已添加{selectedHerbs.Count}个药材");
                    }
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加药材失败");
            SetErrorMessage($"添加药材失败:{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // 计算总价(单价 × 剂数 × 折扣)
    private void CalculateTotalAmount()
    {
        try
        {
            if (HerbItems == null || HerbItems.Count == 0)
            {
                TotalAmount = 0m;
                return;
            }

            // 计算单剂金额(所有药材的 UnitPrice × Dosage)
            decimal singleDoseAmount = HerbItems.Sum(item => item.UnitPrice * item.Dosage);

            // 总金额 = 单剂金额 × 剂数 × 折扣
            TotalAmount = singleDoseAmount * DosageCount * Discount;

            _logger.LogDebug($"总价计算:单剂金额={singleDoseAmount:F2},剂数={DosageCount},折扣={Discount:F2},总金额={TotalAmount:F2}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "计算总价失败");
        }
    }

    // 加载验方模板(打开FormulaTemplateDialog)
    private async Task LoadFormulaTemplateAsync()
    {
        try
        {
            IsBusy = true;

            var parameters = new DialogParameters();
            _dialogService.ShowDialog(
                "FormulaTemplateDialog",
                parameters,
                result =>
                {
                    if (result.Result == ButtonResult.OK)
                    {
                        // 获取选中的验方
                        var selectedFormula = result.Parameters.GetValue<FormulaDto>("SelectedFormula");

                        if (selectedFormula != null)
                        {
                            // 清空现有药材条目
                            HerbItems.Clear();

                            // 加载验方药材条目
                            foreach (var formulaItem in selectedFormula.HerbItems)
                            {
                                HerbItems.Add(new PrescriptionItemRow
                                {
                                    HerbId = formulaItem.HerbId,
                                    HerbName = formulaItem.HerbName,
                                    Dosage = formulaItem.Dosage,
                                    Unit = formulaItem.Unit,
                                    UnitPrice = formulaItem.UnitPrice,
                                    Notes = formulaItem.Notes
                                });
                            }

                            // 应用验方的用法和医嘱
                            Usage = selectedFormula.UsageInstructions;
                            MedicalAdvice = selectedFormula.Description;

                            _logger.LogInformation($"已加载验方模板:{selectedFormula.Name},包含{selectedFormula.HerbItems.Count}个药材");
                            SetSuccessMessage($"已加载验方模板:{selectedFormula.Name}");
                        }
                    }
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载验方模板失败");
            SetErrorMessage($"加载验方模板失败:{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // 验证处方(IValidatable接口实现)
    public bool Validate()
    {
        // 必填项验证
        if (DosageCount <= 0)
        {
            ValidationMessage = "剂数必须大于0";
            return false;
        }

        if (HerbItems == null || HerbItems.Count == 0)
        {
            ValidationMessage = "处方至少需要包含1个药材";
            return false;
        }

        // 验证药材条目剂量
        foreach (var item in HerbItems)
        {
            if (item.Dosage <= 0)
            {
                ValidationMessage = $"药材{item.HerbName}剂量必须大于0";
                return false;
            }
        }

        ValidationMessage = string.Empty;
        return true;
    }

    // 保存处方(ISaveable接口实现)
    public async Task SaveAsync()
    {
        try
        {
            IsBusy = true;
            ClearMessage();

            // 验证数据完整性
            if (!Validate())
            {
                SetWarningMessage(ValidationMessage);
                return;
            }

            if (PrescriptionId.HasValue)
            {
                // 更新现有处方
                var updateDto = new UpdatePrescriptionDto
                {
                    DosageCount = DosageCount,
                    Usage = Usage,
                    MedicalAdvice = MedicalAdvice,
                    Remark = Remark,
                    Discount = Discount,
                    HerbItems = HerbItems.Select(item => new PrescriptionItemDto
                    {
                        HerbId = item.HerbId,
                        Dosage = item.Dosage,
                        Unit = item.Unit,
                        UnitPrice = item.UnitPrice,
                        Notes = item.Notes
                    }).ToList()
                };

                await _prescriptionApi.UpdateAsync(PrescriptionId.Value, updateDto);
                _logger.LogInformation($"处方已更新:PrescriptionId={PrescriptionId.Value}");
            }
            else
            {
                // 创建新处方
                var createDto = new CreatePrescriptionDto
                {
                    MedicalCaseId = CurrentMedicalCaseId,  // 从参数传入
                    DosageCount = DosageCount,
                    Usage = Usage,
                    MedicalAdvice = MedicalAdvice,
                    Remark = Remark,
                    Discount = Discount,
                    HerbItems = HerbItems.Select(item => new PrescriptionItemDto
                    {
                        HerbId = item.HerbId,
                        Dosage = item.Dosage,
                        Unit = item.Unit,
                        UnitPrice = item.UnitPrice,
                        Notes = item.Notes
                    }).ToList()
                };

                var created = await _prescriptionApi.CreateAsync(createDto);
                PrescriptionId = created.Id;
                _logger.LogInformation($"处方已创建:PrescriptionId={created.Id}");
            }

            HasChanges = false;
            SetSuccessMessage("处方已保存");

            // 触发对话框关闭
            RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存处方失败");
            SetErrorMessage($"保存失败:{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

### 5. 处方列表管理 - 分页、搜索、打印

```csharp
/// <summary>
/// 处方管理ViewModel - 处方列表、搜索、CRUD、打印
/// 核心功能:分页查询、搜索过滤、创建/编辑/删除处方、打印、导出
/// </summary>
public class PrescriptionManagementViewModel : UnifiedViewModelBase, IInitializeAsync
{
    private readonly IPrescriptionRepository _prescriptionApi;
    private readonly IMedicalCaseRepository _medicalCaseRepository;
    private readonly IPrescriptionPrintService _printService;
    private readonly IFeatureToggleService _featureToggleService;

    // 数据集合
    public ObservableCollection<PrescriptionDto> Prescriptions { get; set; }
    public PrescriptionDto? SelectedPrescription { get; set; }

    // 搜索与过滤
    public string SearchText { get; set; }      // 按处方编号/患者姓名搜索
    public DateTime? StartDate { get; set; }    // 按日期范围过滤
    public DateTime? EndDate { get; set; }

    // 分页参数
    public int CurrentPage { get; set; } = 1;   // 当前页码(从1开始)
    public int PageSize { get; set; } = 20;     // 每页数量
    public int TotalCount { get; set; }         // 总记录数

    // 命令状态
    public bool CanCreate => true;
    public bool CanDelete => SelectedPrescription != null && SelectedPrescription.Status == PrescriptionStatus.Draft;
    public bool CanClone => SelectedPrescription != null;
    public bool CanExport => Prescriptions.Count > 0;
    public bool CanSearch => !string.IsNullOrWhiteSpace(SearchText);
    public bool CanViewDetail => SelectedPrescription != null;

    // 命令
    public AsyncDelegateCommand LoadDataCommand { get; }
    public AsyncDelegateCommand SearchCommand { get; }
    public DelegateCommand CreateCommand { get; }
    public DelegateCommand EditCommand { get; }
    public AsyncDelegateCommand DeleteCommand { get; }
    public DelegateCommand ViewDetailCommand { get; }
    public DelegateCommand PrintCommand { get; }
    public AsyncDelegateCommand RefreshCommand { get; }
    public AsyncDelegateCommand PreviousPageCommand { get; }
    public AsyncDelegateCommand NextPageCommand { get; }
    public DelegateCommand AddPrescriptionCommand { get; }
    public DelegateCommand ClearFiltersCommand { get; }
    public AsyncDelegateCommand ExportPrescriptionsCommand { get; }

    // DataGrid右键菜单命令
    public DelegateCommand<PrescriptionDto> ViewPrescriptionCommand { get; }
    public DelegateCommand<PrescriptionDto> EditPrescriptionCommand { get; }
    public DelegateCommand<PrescriptionDto> CopyPrescriptionCommand { get; }
    public AsyncDelegateCommand<PrescriptionDto> DeletePrescriptionCommand { get; }
    public DelegateCommand<PrescriptionDto> ViewPatientHistoryCommand { get; }

    public PrescriptionManagementViewModel(
        IPrescriptionRepository prescriptionApi,
        IMedicalCaseRepository medicalCaseRepository,
        IPrescriptionPrintService printService,
        IFeatureToggleService featureToggleService,
        IDialogService dialogService,
        ILogger<PrescriptionManagementViewModel> logger)
    {
        _prescriptionApi = prescriptionApi;
        _medicalCaseRepository = medicalCaseRepository;
        _printService = printService;
        _featureToggleService = featureToggleService;

        Prescriptions = new ObservableCollection<PrescriptionDto>();

        // 初始化命令
        LoadDataCommand = new AsyncDelegateCommand(LoadDataAsync);
        SearchCommand = new AsyncDelegateCommand(SearchAsync);
        CreateCommand = new DelegateCommand(Create);
        EditCommand = new DelegateCommand(Edit, CanEditInternal);
        DeleteCommand = new AsyncDelegateCommand(DeleteAsync, CanDeleteInternal);
        ViewDetailCommand = new DelegateCommand(ViewDetail, CanViewDetailInternal);
        PrintCommand = new DelegateCommand(Print, CanPrint);
        RefreshCommand = new AsyncDelegateCommand(RefreshAsync);
        PreviousPageCommand = new AsyncDelegateCommand(PreviousPageAsync, CanPreviousPage);
        NextPageCommand = new AsyncDelegateCommand(NextPageAsync, CanNextPage);
        AddPrescriptionCommand = new DelegateCommand(Create);
        ClearFiltersCommand = new DelegateCommand(ClearFilters);
        ExportPrescriptionsCommand = new AsyncDelegateCommand(ExportPrescriptionsAsync);

        // DataGrid右键菜单命令
        ViewPrescriptionCommand = new DelegateCommand<PrescriptionDto>(ViewPrescriptionItem);
        EditPrescriptionCommand = new DelegateCommand<PrescriptionDto>(EditPrescriptionItem);
        CopyPrescriptionCommand = new DelegateCommand<PrescriptionDto>(CopyPrescription);
        DeletePrescriptionCommand = new AsyncDelegateCommand<PrescriptionDto>(DeletePrescriptionItemAsync);
        ViewPatientHistoryCommand = new DelegateCommand<PrescriptionDto>(ViewPatientHistory);

        // 监听SelectedPrescription变更 → 更新命令状态
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SelectedPrescription))
            {
                UpdateCommandStates();
            }
        };
    }

    // 初始化(加载第一页数据)
    public async Task InitializeAsync()
    {
        await LoadDataAsync();
    }

    // 加载数据(分页查询)
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            ClearMessage();

            // 调用API分页查询
            var result = await _prescriptionApi.GetPagedAsync(
                pageIndex: CurrentPage,
                pageSize: PageSize,
                searchText: SearchText,
                startDate: StartDate,
                endDate: EndDate
            );

            Prescriptions.Clear();
            foreach (var item in result.Items)
            {
                Prescriptions.Add(item);
            }

            TotalCount = result.TotalCount;

            _logger.LogInformation($"已加载处方列表:当前页={CurrentPage},每页数量={PageSize},总记录数={TotalCount}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载处方列表失败");
            SetErrorMessage($"加载失败:{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // 搜索(重置到第一页)
    private async Task SearchAsync()
    {
        CurrentPage = 1;
        await LoadDataAsync();
    }

    // 创建处方(打开PrescriptionEditorDialog)
    private void Create()
    {
        try
        {
            var parameters = new DialogParameters
            {
                { "MedicalCaseId", CurrentMedicalCaseId }  // 传入医案ID
            };

            _dialogService.ShowDialog(
                "PrescriptionEditorDialog",
                parameters,
                async result =>
                {
                    if (result.Result == ButtonResult.OK)
                    {
                        // 刷新列表
                        await RefreshAsync();
                    }
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "打开创建处方对话框失败");
            SetErrorMessage($"操作失败:{ex.Message}");
        }
    }

    // 编辑处方(打开PrescriptionEditorDialog)
    private void Edit()
    {
        if (SelectedPrescription == null) return;

        try
        {
            var parameters = new DialogParameters
            {
                { "PrescriptionId", SelectedPrescription.Id }  // 传入处方ID
            };

            _dialogService.ShowDialog(
                "PrescriptionEditorDialog",
                parameters,
                async result =>
                {
                    if (result.Result == ButtonResult.OK)
                    {
                        // 刷新列表
                        await RefreshAsync();
                    }
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "打开编辑处方对话框失败");
            SetErrorMessage($"操作失败:{ex.Message}");
        }
    }

    // 删除处方(弹出确认对话框)
    private async Task DeleteAsync()
    {
        if (SelectedPrescription == null) return;

        try
        {
            // 弹出确认对话框
            var confirmed = await _dialogService.ShowConfirmationDialogAsync(
                "确认删除",
                $"是否删除处方【{SelectedPrescription.PrescriptionNo}】?此操作不可撤销。"
            );

            if (!confirmed) return;

            IsBusy = true;

            // 调用API删除
            await _prescriptionApi.DeleteAsync(SelectedPrescription.Id);

            _logger.LogInformation($"处方已删除:PrescriptionId={SelectedPrescription.Id}");
            SetSuccessMessage("处方已删除");

            // 刷新列表
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除处方失败");
            SetErrorMessage($"删除失败:{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // 打印处方(调用PrescriptionPrintService)
    private void Print()
    {
        if (SelectedPrescription == null) return;

        try
        {
            IsBusy = true;

            // 调用打印服务
            _printService.PrintAsync(SelectedPrescription);

            _logger.LogInformation($"处方已发送到打印机:PrescriptionId={SelectedPrescription.Id}");
            SetSuccessMessage("处方已发送到打印机");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "打印处方失败");
            SetErrorMessage($"打印失败:{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // 刷新列表
    private async Task RefreshAsync()
    {
        await LoadDataAsync();
    }

    // 上一页
    private async Task PreviousPageAsync()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            await LoadDataAsync();
        }
    }

    // 下一页
    private async Task NextPageAsync()
    {
        int totalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
        if (CurrentPage < totalPages)
        {
            CurrentPage++;
            await LoadDataAsync();
        }
    }

    // 清除过滤器
    private void ClearFilters()
    {
        SearchText = null;
        StartDate = null;
        EndDate = null;
    }

    // 导出处方到Excel
    private async Task ExportPrescriptionsAsync()
    {
        try
        {
            IsBusy = true;

            // 调用API导出
            var fileBytes = await _prescriptionApi.ExportAsync(
                searchText: SearchText,
                startDate: StartDate,
                endDate: EndDate
            );

            // 保存文件
            var fileName = $"处方列表_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            var savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
            File.WriteAllBytes(savePath, fileBytes);

            _logger.LogInformation($"处方列表已导出:{savePath}");
            SetSuccessMessage($"导出成功:{savePath}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出处方列表失败");
            SetErrorMessage($"导出失败:{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // 更新命令状态
    private void UpdateCommandStates()
    {
        EditCommand.RaiseCanExecuteChanged();
        DeleteCommand.RaiseCanExecuteChanged();
        ViewDetailCommand.RaiseCanExecuteChanged();
        PrintCommand.RaiseCanExecuteChanged();
        PreviousPageCommand.RaiseCanExecuteChanged();
        NextPageCommand.RaiseCanExecuteChanged();
    }
}
```

### 6. ISaveable接口契约 - MedicalCase集成

```csharp
/// <summary>
/// ISaveable接口契约示例 - PrescriptionEditorDialogViewModel实现ISaveable
/// MedicalCaseFlowViewModel通过ISaveable接口调用处方功能
/// </summary>

// Step 1: PrescriptionEditorDialogViewModel实现ISaveable接口
public class PrescriptionEditorDialogViewModel : UnifiedViewModelBase, ISaveable, IValidatable
{
    // ISaveable接口实现:验证方法
    public bool Validate()
    {
        // 必填项验证
        if (DosageCount <= 0)
        {
            ValidationMessage = "剂数必须大于0";
            return false;
        }

        if (HerbItems == null || HerbItems.Count == 0)
        {
            ValidationMessage = "处方至少需要包含1个药材";
            return false;
        }

        // 验证药材条目剂量
        foreach (var item in HerbItems)
        {
            if (item.Dosage <= 0)
            {
                ValidationMessage = $"药材{item.HerbName}剂量必须大于0";
                return false;
            }
        }

        ValidationMessage = string.Empty;
        return true;
    }

    // ISaveable接口实现:保存方法
    public async Task SaveAsync()
    {
        try
        {
            if (!Validate())
            {
                throw new ValidationException(ValidationMessage);
            }

            if (PrescriptionId.HasValue)
            {
                // 更新现有处方
                var updateDto = new UpdatePrescriptionDto
                {
                    DosageCount = DosageCount,
                    Usage = Usage,
                    MedicalAdvice = MedicalAdvice,
                    Remark = Remark,
                    Discount = Discount,
                    HerbItems = HerbItems.Select(item => new PrescriptionItemDto
                    {
                        HerbId = item.HerbId,
                        Dosage = item.Dosage,
                        Unit = item.Unit,
                        UnitPrice = item.UnitPrice,
                        Notes = item.Notes
                    }).ToList()
                };

                await _prescriptionApi.UpdateAsync(PrescriptionId.Value, updateDto);
            }
            else
            {
                // 创建新处方
                var createDto = new CreatePrescriptionDto
                {
                    MedicalCaseId = CurrentMedicalCaseId,
                    DosageCount = DosageCount,
                    Usage = Usage,
                    MedicalAdvice = MedicalAdvice,
                    Remark = Remark,
                    Discount = Discount,
                    HerbItems = HerbItems.Select(item => new PrescriptionItemDto
                    {
                        HerbId = item.HerbId,
                        Dosage = item.Dosage,
                        Unit = item.Unit,
                        UnitPrice = item.UnitPrice,
                        Notes = item.Notes
                    }).ToList()
                };

                var created = await _prescriptionApi.CreateAsync(createDto);
                PrescriptionId = created.Id;
            }

            HasChanges = false;
            _logger.LogInformation($"处方已保存:PrescriptionId={PrescriptionId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存处方失败");
            throw;
        }
    }
}

// Step 2: MedicalCaseFlowViewModel通过ISaveable接口调用处方功能
public class MedicalCaseFlowViewModel : UnifiedViewModelBase
{
    private ISaveable? _currentStepViewModel;  // 当前步骤的ViewModel(可能是ConsultationFormViewModel或PrescriptionEditorDialogViewModel)

    // 完成Step2处方开具(保存处方)
    private async Task CompleteStep2Async()
    {
        try
        {
            IsBusy = true;

            // 验证当前步骤ViewModel
            if (_currentStepViewModel is IValidatable validatable)
            {
                if (!validatable.Validate())
                {
                    SetWarningMessage(validatable.ValidationMessage);
                    return;
                }
            }

            // 保存当前步骤数据(调用ISaveable.SaveAsync)
            if (_currentStepViewModel != null)
            {
                await _currentStepViewModel.SaveAsync();
            }

            // 标记Step2完成
            await _medicalCaseRepository.CompleteStep2Async(MedicalCaseId, DateTime.Now);

            SetSuccessMessage("处方已开具,可以进入下一步");

            // 通知属性变更(更新UI)
            RaisePropertyChanged(nameof(Step3Enabled));
            RaisePropertyChanged(nameof(Step3Disabled));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "完成Step2失败");
            SetErrorMessage($"操作失败:{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

### 7. 药材选择对话框 - 从Herbs模块选择药材

```csharp
/// <summary>
/// 药材选择对话框ViewModel - 从Herbs模块查询药材并选择
/// 核心功能:药材列表、拼音搜索、多选、添加到处方
/// </summary>
public class HerbSelectionDialogViewModel : UnifiedViewModelBase, IDialogAware
{
    private readonly IHerbRepository _herbApi;

    // 数据集合
    public ObservableCollection<HerbDto> Herbs { get; set; }                    // 药材列表
    public ObservableCollection<HerbDto> SelectedHerbs { get; set; }            // 选中的药材

    // 搜索参数
    public string SearchKeyword { get; set; }  // 搜索关键字(支持名称/拼音/功效)

    // 分页参数
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 50;    // 每页显示50个药材
    public int TotalCount { get; set; }

    // 命令
    public AsyncDelegateCommand SearchCommand { get; }
    public AsyncDelegateCommand LoadMoreCommand { get; }
    public DelegateCommand<HerbDto> SelectHerbCommand { get; }
    public DelegateCommand<HerbDto> DeselectHerbCommand { get; }
    public DelegateCommand ConfirmCommand { get; }
    public DelegateCommand CancelCommand { get; }

    // IDialogAware事件
    public event Action<IDialogResult> RequestClose;

    public HerbSelectionDialogViewModel(
        IHerbRepository herbApi,
        ILogger<HerbSelectionDialogViewModel> logger)
    {
        _herbApi = herbApi;

        Herbs = new ObservableCollection<HerbDto>();
        SelectedHerbs = new ObservableCollection<HerbDto>();

        SearchCommand = new AsyncDelegateCommand(SearchAsync);
        LoadMoreCommand = new AsyncDelegateCommand(LoadMoreAsync);
        SelectHerbCommand = new DelegateCommand<HerbDto>(SelectHerb);
        DeselectHerbCommand = new DelegateCommand<HerbDto>(DeselectHerb);
        ConfirmCommand = new DelegateCommand(Confirm);
        CancelCommand = new DelegateCommand(Cancel);
    }

    // 对话框打开时初始化(加载药材列表)
    public async Task OnDialogOpened(IDialogParameters parameters)
    {
        await LoadDataAsync();
    }

    // 加载药材数据(支持拼音搜索)
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;

            // 调用Herbs模块API查询药材
            var result = await _herbApi.GetPagedAsync(
                pageIndex: CurrentPage,
                pageSize: PageSize,
                searchText: SearchKeyword  // 支持名称/拼音/功效搜索
            );

            Herbs.Clear();
            foreach (var herb in result.Items)
            {
                Herbs.Add(herb);
            }

            TotalCount = result.TotalCount;

            _logger.LogInformation($"已加载药材列表:当前页={CurrentPage},总记录数={TotalCount}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载药材列表失败");
            SetErrorMessage($"加载失败:{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // 搜索药材(支持拼音快速输入)
    private async Task SearchAsync()
    {
        CurrentPage = 1;
        await LoadDataAsync();
    }

    // 加载更多药材(分页)
    private async Task LoadMoreAsync()
    {
        CurrentPage++;
        await LoadDataAsync();
    }

    // 选择药材(添加到SelectedHerbs)
    private void SelectHerb(HerbDto herb)
    {
        if (herb == null) return;

        if (!SelectedHerbs.Contains(herb))
        {
            SelectedHerbs.Add(herb);
            _logger.LogDebug($"已选择药材:{herb.Name}");
        }
    }

    // 取消选择药材(从SelectedHerbs移除)
    private void DeselectHerb(HerbDto herb)
    {
        if (herb == null) return;

        if (SelectedHerbs.Contains(herb))
        {
            SelectedHerbs.Remove(herb);
            _logger.LogDebug($"已取消选择药材:{herb.Name}");
        }
    }

    // 确认选择(关闭对话框并返回选中的药材)
    private void Confirm()
    {
        if (SelectedHerbs.Count == 0)
        {
            SetWarningMessage("请至少选择一个药材");
            return;
        }

        var result = new DialogResult(ButtonResult.OK);
        result.Parameters.Add("SelectedHerbs", SelectedHerbs.ToList());

        RequestClose?.Invoke(result);
    }

    // 取消选择(关闭对话框)
    private void Cancel()
    {
        RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
    }
}
```

### 8. 验方模板加载 - 从Formula模块加载验方

```csharp
/// <summary>
/// 验方模板对话框ViewModel - 从Formula模块加载验方并应用到处方
/// 核心功能:验方列表、搜索、选择验方、加载药材条目到处方
/// </summary>
public class FormulaTemplateDialogViewModel : UnifiedViewModelBase, IDialogAware
{
    private readonly IFormulaRepository _formulaApi;

    // 数据集合
    public ObservableCollection<FormulaDto> Formulas { get; set; }              // 验方列表
    public FormulaDto? SelectedFormula { get; set; }                            // 选中的验方

    // 搜索参数
    public string SearchKeyword { get; set; }      // 搜索关键字(按名称/分类)
    public string SelectedCategory { get; set; }   // 选中的分类(如:感冒类、脾胃类)

    // 分页参数
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }

    // 命令
    public AsyncDelegateCommand LoadDataCommand { get; }
    public AsyncDelegateCommand SearchCommand { get; }
    public DelegateCommand<FormulaDto> SelectFormulaCommand { get; }
    public DelegateCommand ConfirmCommand { get; }
    public DelegateCommand CancelCommand { get; }

    // IDialogAware事件
    public event Action<IDialogResult> RequestClose;

    public FormulaTemplateDialogViewModel(
        IFormulaRepository formulaApi,
        ILogger<FormulaTemplateDialogViewModel> logger)
    {
        _formulaApi = formulaApi;

        Formulas = new ObservableCollection<FormulaDto>();

        LoadDataCommand = new AsyncDelegateCommand(LoadDataAsync);
        SearchCommand = new AsyncDelegateCommand(SearchAsync);
        SelectFormulaCommand = new DelegateCommand<FormulaDto>(SelectFormula);
        ConfirmCommand = new DelegateCommand(Confirm);
        CancelCommand = new DelegateCommand(Cancel);
    }

    // 对话框打开时初始化(加载验方列表)
    public async Task OnDialogOpened(IDialogParameters parameters)
    {
        await LoadDataAsync();
    }

    // 加载验方数据
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;

            // 调用Formula模块API查询验方
            var result = await _formulaApi.GetPagedAsync(
                pageIndex: CurrentPage,
                pageSize: PageSize,
                searchText: SearchKeyword,
                category: SelectedCategory
            );

            Formulas.Clear();
            foreach (var formula in result.Items)
            {
                Formulas.Add(formula);
            }

            TotalCount = result.TotalCount;

            _logger.LogInformation($"已加载验方列表:当前页={CurrentPage},总记录数={TotalCount}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载验方列表失败");
            SetErrorMessage($"加载失败:{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // 搜索验方(按名称/分类)
    private async Task SearchAsync()
    {
        CurrentPage = 1;
        await LoadDataAsync();
    }

    // 选择验方
    private void SelectFormula(FormulaDto formula)
    {
        if (formula == null) return;

        SelectedFormula = formula;
        _logger.LogDebug($"已选择验方:{formula.Name}");
    }

    // 确认选择(关闭对话框并返回选中的验方)
    private void Confirm()
    {
        if (SelectedFormula == null)
        {
            SetWarningMessage("请选择一个验方");
            return;
        }

        var result = new DialogResult(ButtonResult.OK);
        result.Parameters.Add("SelectedFormula", SelectedFormula);

        RequestClose?.Invoke(result);
    }

    // 取消选择(关闭对话框)
    private void Cancel()
    {
        RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
    }
}
```

### 9. 处方打印功能 - 生成FlowDocument并打印

```csharp
/// <summary>
/// 处方打印服务 - 生成FlowDocument并调用WPF打印
/// 核心功能:处方格式化、FlowDocument生成、打印预览、打印机打印
/// </summary>
public class PrescriptionPrintService : IPrescriptionPrintService
{
    private readonly PrescriptionFlowDocumentBuilder _documentBuilder;
    private readonly ILogger<PrescriptionPrintService> _logger;

    public PrescriptionPrintService(
        PrescriptionFlowDocumentBuilder documentBuilder,
        ILogger<PrescriptionPrintService> logger)
    {
        _documentBuilder = documentBuilder;
        _logger = logger;
    }

    // 打印处方(调用WPF PrintDialog)
    public async Task PrintAsync(PrescriptionDto prescription)
    {
        try
        {
            _logger.LogInformation($"开始打印处方:PrescriptionId={prescription.Id}");

            // 生成FlowDocument
            var document = await _documentBuilder.BuildAsync(prescription);

            // 创建WPF打印对话框
            var printDialog = new PrintDialog();

            // 显示打印对话框,用户选择打印机
            if (printDialog.ShowDialog() == true)
            {
                // 获取DocumentPaginator
                var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;

                // 打印文档
                printDialog.PrintDocument(paginator, $"处方单-{prescription.PrescriptionNo}");

                _logger.LogInformation($"处方已发送到打印机:PrescriptionId={prescription.Id}");
            }
            else
            {
                _logger.LogInformation("用户取消打印");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"打印处方失败:PrescriptionId={prescription.Id}");
            throw;
        }
    }

    // 打印预览(显示FlowDocument)
    public async Task PreviewAsync(PrescriptionDto prescription)
    {
        try
        {
            _logger.LogInformation($"打开打印预览:PrescriptionId={prescription.Id}");

            // 生成FlowDocument
            var document = await _documentBuilder.BuildAsync(prescription);

            // 创建预览窗口
            var previewWindow = new Window
            {
                Title = $"打印预览 - {prescription.PrescriptionNo}",
                Width = 800,
                Height = 600,
                Content = new FlowDocumentScrollViewer
                {
                    Document = document
                }
            };

            previewWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"打开打印预览失败:PrescriptionId={prescription.Id}");
            throw;
        }
    }
}

/// <summary>
/// FlowDocument构建器 - 生成处方打印文档
/// 核心功能:格式化处方信息、药材条目列表、总价计算、医嘱显示
/// </summary>
public class PrescriptionFlowDocumentBuilder
{
    private readonly ILogger<PrescriptionFlowDocumentBuilder> _logger;

    public PrescriptionFlowDocumentBuilder(ILogger<PrescriptionFlowDocumentBuilder> logger)
    {
        _logger = logger;
    }

    // 生成FlowDocument
    public async Task<FlowDocument> BuildAsync(PrescriptionDto prescription)
    {
        try
        {
            var document = new FlowDocument
            {
                PageWidth = 793.7,   // A4纸宽度(像素)
                PageHeight = 1122.5, // A4纸高度(像素)
                PagePadding = new Thickness(50),
                FontFamily = new FontFamily("微软雅黑"),
                FontSize = 14
            };

            // 标题
            var titleParagraph = new Paragraph(new Run("中医处方单"))
            {
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            document.Blocks.Add(titleParagraph);

            // 处方基础信息
            var infoParagraph = new Paragraph
            {
                Margin = new Thickness(0, 0, 0, 10)
            };
            infoParagraph.Inlines.Add(new Run($"处方编号:{prescription.PrescriptionNo}"));
            infoParagraph.Inlines.Add(new LineBreak());
            infoParagraph.Inlines.Add(new Run($"患者姓名:{prescription.PatientName}"));
            infoParagraph.Inlines.Add(new LineBreak());
            infoParagraph.Inlines.Add(new Run($"开方日期:{prescription.CreatedAt:yyyy-MM-dd HH:mm}"));
            infoParagraph.Inlines.Add(new LineBreak());
            infoParagraph.Inlines.Add(new Run($"医生:{prescription.DoctorName}"));
            document.Blocks.Add(infoParagraph);

            // 药材条目表格
            var table = new Table
            {
                CellSpacing = 0,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1)
            };

            // 定义列
            table.Columns.Add(new TableColumn { Width = new GridLength(50) });  // 序号
            table.Columns.Add(new TableColumn { Width = new GridLength(200) }); // 药材名称
            table.Columns.Add(new TableColumn { Width = new GridLength(100) }); // 剂量
            table.Columns.Add(new TableColumn { Width = new GridLength(100) }); // 单价
            table.Columns.Add(new TableColumn { Width = new GridLength(100) }); // 小计

            // 表头
            var headerRowGroup = new TableRowGroup();
            var headerRow = new TableRow();
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("序号"))) { FontWeight = FontWeights.Bold });
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("药材名称"))) { FontWeight = FontWeights.Bold });
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("剂量"))) { FontWeight = FontWeights.Bold });
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("单价(元/克)"))) { FontWeight = FontWeights.Bold });
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("小计(元)"))) { FontWeight = FontWeights.Bold });
            headerRowGroup.Rows.Add(headerRow);
            table.RowGroups.Add(headerRowGroup);

            // 药材条目数据
            var dataRowGroup = new TableRowGroup();
            int index = 1;
            foreach (var item in prescription.HerbItems)
            {
                var row = new TableRow();
                row.Cells.Add(new TableCell(new Paragraph(new Run(index.ToString()))));
                row.Cells.Add(new TableCell(new Paragraph(new Run(item.HerbName))));
                row.Cells.Add(new TableCell(new Paragraph(new Run($"{item.Dosage}{item.Unit}"))));
                row.Cells.Add(new TableCell(new Paragraph(new Run($"{item.UnitPrice:F2}"))));
                row.Cells.Add(new TableCell(new Paragraph(new Run($"{(item.UnitPrice * item.Dosage):F2}"))));
                dataRowGroup.Rows.Add(row);
                index++;
            }
            table.RowGroups.Add(dataRowGroup);

            document.Blocks.Add(table);

            // 总价信息
            var summaryParagraph = new Paragraph
            {
                Margin = new Thickness(0, 10, 0, 10)
            };
            summaryParagraph.Inlines.Add(new Run($"剂数:{prescription.DosageCount}剂"));
            summaryParagraph.Inlines.Add(new LineBreak());
            summaryParagraph.Inlines.Add(new Run($"折扣:{(prescription.Discount * 100):F0}%"));
            summaryParagraph.Inlines.Add(new LineBreak());
            summaryParagraph.Inlines.Add(new Run($"总金额:{prescription.TotalAmount:F2}元")
            {
                FontWeight = FontWeights.Bold,
                FontSize = 16
            });
            document.Blocks.Add(summaryParagraph);

            // 用法
            if (!string.IsNullOrEmpty(prescription.Usage))
            {
                var usageParagraph = new Paragraph
                {
                    Margin = new Thickness(0, 0, 0, 10)
                };
                usageParagraph.Inlines.Add(new Run("用法:"));
                usageParagraph.Inlines.Add(new LineBreak());
                usageParagraph.Inlines.Add(new Run(prescription.Usage));
                document.Blocks.Add(usageParagraph);
            }

            // 医嘱
            if (!string.IsNullOrEmpty(prescription.MedicalAdvice))
            {
                var adviceParagraph = new Paragraph
                {
                    Margin = new Thickness(0, 0, 0, 10)
                };
                adviceParagraph.Inlines.Add(new Run("医嘱:"));
                adviceParagraph.Inlines.Add(new LineBreak());
                adviceParagraph.Inlines.Add(new Run(prescription.MedicalAdvice));
                document.Blocks.Add(adviceParagraph);
            }

            _logger.LogDebug($"FlowDocument已生成:PrescriptionId={prescription.Id}");

            return document;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"生成FlowDocument失败:PrescriptionId={prescription.Id}");
            throw;
        }
    }
}
```

### 10. Repository模式与三层架构

```csharp
/// <summary>
/// Repository模式示例 - IPrescriptionRepository接口定义与实现
/// 三层架构:ViewModel → Repository → BaseApiRepository → IApiService → HttpClient
/// </summary>

// Layer 1: 接口定义(Interfaces/IPrescriptionRepository.cs)
public interface IPrescriptionRepository : IBaseRepository<PrescriptionDto>
{
    // 继承自IBaseRepository的方法:
    // - GetByIdAsync(Guid id)
    // - GetPagedAsync(int pageIndex, int pageSize)
    // - CreateAsync(CreatePrescriptionDto dto)
    // - UpdateAsync(Guid id, UpdatePrescriptionDto dto)
    // - DeleteAsync(Guid id)
}

// Layer 2: Repository实现(使用BaseApiRepository基类)
public class PrescriptionRepository : BaseApiRepository<PrescriptionDto>, IPrescriptionRepository
{
    public PrescriptionRepository(IApiService apiService, ILogger<PrescriptionRepository> logger)
        : base(apiService, logger, "/api/v1/prescriptions")  // API端点基础路径
    {
    }

    // 继承自BaseApiRepository的方法会自动实现
    // 如需自定义API调用,可重写基类方法或添加新方法
}

// Layer 3: ViewModel调用Repository
public class PrescriptionManagementViewModel : UnifiedViewModelBase
{
    private readonly IPrescriptionRepository _prescriptionApi;

    public PrescriptionManagementViewModel(IPrescriptionRepository prescriptionApi, ...)
    {
        _prescriptionApi = prescriptionApi;  // 构造函数注入
    }

    // 调用Repository方法
    private async Task LoadDataAsync()
    {
        // Repository返回裸类型PrescriptionDto,无需Result<T>包装
        var result = await _prescriptionApi.GetPagedAsync(
            pageIndex: CurrentPage,
            pageSize: PageSize,
            searchText: SearchText,
            startDate: StartDate,
            endDate: EndDate
        );

        Prescriptions.Clear();
        foreach (var item in result.Items)
        {
            Prescriptions.Add(item);
        }
    }
}

// Layer 4: BaseApiRepository实现(Foundation层)
public abstract class BaseApiRepository<TDto>
{
    protected readonly IApiService _apiService;
    protected readonly ILogger _logger;
    protected readonly string _baseUrl;

    protected BaseApiRepository(IApiService apiService, ILogger logger, string baseUrl)
    {
        _apiService = apiService;
        _logger = logger;
        _baseUrl = baseUrl;
    }

    public virtual async Task<PagedResult<TDto>> GetPagedAsync(int pageIndex, int pageSize)
    {
        var url = $"{_baseUrl}?pageIndex={pageIndex}&pageSize={pageSize}";
        return await _apiService.GetAsync<PagedResult<TDto>>(url);
    }

    public virtual async Task<TDto?> GetByIdAsync(Guid id)
    {
        var url = $"{_baseUrl}/{id}";
        return await _apiService.GetAsync<TDto>(url);
    }

    public virtual async Task<TDto> CreateAsync(object createDto)
    {
        return await _apiService.PostAsync<TDto>(_baseUrl, createDto);
    }

    public virtual async Task UpdateAsync(Guid id, object updateDto)
    {
        var url = $"{_baseUrl}/{id}";
        await _apiService.PutAsync(url, updateDto);
    }

    public virtual async Task DeleteAsync(Guid id)
    {
        var url = $"{_baseUrl}/{id}";
        await _apiService.DeleteAsync(url);
    }
}

// Layer 5: IApiService实现(Foundation层,统一HTTP通信)
public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiService> _logger;

    public ApiService(HttpClient httpClient, ILogger<ApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string url)
    {
        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"GET请求失败:Url={url}");
            throw;
        }
    }

    public async Task<T?> PostAsync<T>(string url, object data)
    {
        try
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"POST请求失败:Url={url}");
            throw;
        }
    }

    // PutAsync, DeleteAsync等方法省略...
}
```

### 11. PrescriptionsModule注册

```csharp
/// <summary>
/// Prism模块注册 - 注册ViewModels、Views、Services、Repositories
/// 核心功能:依赖注入配置、区域导航配置、对话框注册
/// </summary>
public class PrescriptionsModule : IModule
{
    private readonly IRegionManager _regionManager;

    public PrescriptionsModule(IRegionManager regionManager)
    {
        _regionManager = regionManager;
    }

    // 模块初始化(Shell启动后调用)
    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 模块已加载,无需额外初始化
        var logger = containerProvider.Resolve<ILogger<PrescriptionsModule>>();
        logger.LogInformation("PrescriptionsModule已加载");
    }

    // 注册类型(依赖注入配置)
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册ViewModels(单例模式)
        // Issue #1801: PrescriptionsMainViewModel已删除（功能与PrescriptionManagementView重复）
        containerRegistry.RegisterSingleton<PrescriptionManagementViewModel>();
        containerRegistry.Register<PrescriptionEditorDialogViewModel>();
        containerRegistry.Register<HerbSelectionDialogViewModel>();
        containerRegistry.Register<FormulaTemplateDialogViewModel>();
        containerRegistry.Register<SelectFormulaDialogViewModel>();
        containerRegistry.Register<PrescriptionViewModel>();

        // 注册Views(用于区域导航)
        // Issue #1801: PrescriptionsMainView已删除（功能与PrescriptionManagementView重复）
        containerRegistry.RegisterForNavigation<PrescriptionManagementView, PrescriptionManagementViewModel>();
        containerRegistry.RegisterForNavigation<PrescriptionView, PrescriptionViewModel>();

        // 注册对话框(用于Dialog Service)
        containerRegistry.RegisterDialog<PrescriptionEditorDialog, PrescriptionEditorDialogViewModel>();
        containerRegistry.RegisterDialog<HerbSelectionDialog, HerbSelectionDialogViewModel>();
        containerRegistry.RegisterDialog<FormulaTemplateDialog, FormulaTemplateDialogViewModel>();
        containerRegistry.RegisterDialog<SelectFormulaDialog, SelectFormulaDialogViewModel>();

        // 注册Services
        containerRegistry.RegisterSingleton<IPrescriptionPrintService, PrescriptionPrintService>();
        containerRegistry.Register<PrescriptionEditorService>();
        containerRegistry.Register<PrescriptionFlowDocumentBuilder>();

        // 注册Repositories
        containerRegistry.Register<IPrescriptionRepository, PrescriptionRepository>();
    }
}
```

## 🎨 模块架构图

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                        LYBT.Desktop.Prescriptions 模块架构                      │
│                          (Dialog-based + ISaveable契约)                        │
└──────────────────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────────────────┐
│ 外部集成 (External Integration)                                                │
├──────────────────────────────────────────────────────────────────────────────┤
│ LYBT.Desktop.MedicalCase                                                      │
│   └── MedicalCaseFlowViewModel                                                │
│         └── Step2: 处方开具 (调用ISaveable接口)                               │
│             ├── _currentStepViewModel.Validate()     // IValidatable接口      │
│             └── _currentStepViewModel.SaveAsync()    // ISaveable接口         │
│                                                                                │
│ LYBT.Desktop.Shell                                                            │
│   └── App.xaml.cs                                                             │
│         └── ConfigureModuleCatalog(IModuleCatalog)                            │
│             └── moduleCatalog.AddModule<PrescriptionsModule>()                │
└──────────────────────────────────────────────────────────────────────────────┘
                                      ↓
┌──────────────────────────────────────────────────────────────────────────────┐
│ 展示层 (Presentation Layer) - ViewModels + Views                              │
├──────────────────────────────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────────────────────────────┐  │
│ │ PrescriptionManagementViewModel (597行)                                  │  │
│ │ - 处方列表管理(分页、搜索、CRUD)                                           │  │
│ │ - 25个属性(Prescriptions, SelectedPrescription, SearchText等)            │  │
│ │ - 20个命令(LoadData, Search, Create, Edit, Delete, Print等)              │  │
│ │ - 20个方法(LoadDataAsync, SearchAsync, DeleteAsync, PrintAsync等)       │  │
│ └─────────────────────────────────────────────────────────────────────────┘  │
│                                      ↓                                         │
│                            打开对话框 (ShowDialog)                             │
│                                      ↓                                         │
│ ┌─────────────────────────────────────────────────────────────────────────┐  │
│ │ PrescriptionEditorDialogViewModel (668行) ★核心★                         │  │
│ │ - 实现ISaveable + IValidatable + IDialogAware接口                        │  │
│ │ - 30个属性(PrescriptionId, DosageCount, Usage, HerbItems等)              │  │
│ │ - 9个命令(Save, Cancel, AddHerb, LoadFormula, Preview等)                │  │
│ │ - 15个方法(OnDialogOpened, SaveAsync, Validate, AddHerbAsync等)         │  │
│ │                                                                            │  │
│ │ 关键功能:                                                                  │  │
│ │ 1. 添加药材 → 打开HerbSelectionDialog (从Herbs模块选择药材)               │  │
│ │ 2. 加载验方 → 打开FormulaTemplateDialog (从Formula模块加载验方)           │  │
│ │ 3. 计算总价 → CalculateTotalAmount() (单价×剂数×折扣)                     │  │
│ │ 4. 验证处方 → Validate() (剂数>0、药材条目>0、剂量有效)                   │  │
│ │ 5. 保存处方 → SaveAsync() (调用IPrescriptionRepository)                  │  │
│ └─────────────────────────────────────────────────────────────────────────┘  │
│                                      ↓                                         │
│                            ┌────────────────────┐                             │
│                            │  对话框组件集合    │                             │
│                            └────────────────────┘                             │
│                                      ↓                                         │
│ ┌──────────────────────────┬──────────────────────────┬────────────────────┐ │
│ │ HerbSelectionDialog      │ FormulaTemplateDialog    │ SelectFormulaDialog│ │
│ │ ViewModel                │ ViewModel                │ ViewModel          │ │
│ │ - 药材列表               │ - 验方列表               │ - 验方选择         │ │
│ │ - 拼音搜索               │ - 分类过滤               │ - 快速应用         │ │
│ │ - 多选药材               │ - 选择验方               │                    │ │
│ │ - 返回SelectedHerbs      │ - 返回SelectedFormula    │                    │ │
│ └──────────────────────────┴──────────────────────────┴────────────────────┘ │
└──────────────────────────────────────────────────────────────────────────────┘
                                      ↓
┌──────────────────────────────────────────────────────────────────────────────┐
│ 服务层 (Service Layer) - Components + Services                                │
├──────────────────────────────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────────────────────────────┐  │
│ │ ViewModel Components (组件化设计)                                         │  │
│ │ ├── PrescriptionCalculator        // 价格计算器(总价、折扣、单价)         │  │
│ │ ├── PrescriptionCommandHandler    // 命令处理器(添加/删除/编辑药材)       │  │
│ │ ├── PrescriptionDataManager       // 数据管理器(加载/保存/重置)           │  │
│ │ ├── PrescriptionEventCoordinator  // 事件协调器(HasChanges标记)          │  │
│ │ └── PrescriptionValidator         // 验证器(药材条目、剂量、价格)         │  │
│ └─────────────────────────────────────────────────────────────────────────┘  │
│                                      ↓                                         │
│ ┌─────────────────────────────────────────────────────────────────────────┐  │
│ │ 打印服务 (Print Services)                                                 │  │
│ │ ├── IPrescriptionPrintService      // 打印服务接口                        │  │
│ │ ├── PrescriptionPrintService       // 打印服务实现(WPF PrintDialog)       │  │
│ │ │   ├── PrintAsync()                // 打印处方(调用WPF打印)               │  │
│ │ │   └── PreviewAsync()              // 打印预览(显示FlowDocument)          │  │
│ │ └── PrescriptionFlowDocumentBuilder // FlowDocument构建器                 │  │
│ │     └── BuildAsync()                // 生成处方打印文档(含药材表格、总价) │  │
│ └─────────────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────────────┘
                                      ↓
┌──────────────────────────────────────────────────────────────────────────────┐
│ 数据访问层 (Data Access Layer) - Repository + API                             │
├──────────────────────────────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────────────────────────────┐  │
│ │ IPrescriptionRepository                                                   │  │
│ │ └── BaseApiRepository<PrescriptionDto>                                    │  │
│ │     ├── GetPagedAsync()   // 分页查询                                     │  │
│ │     ├── GetByIdAsync()    // 按ID查询                                     │  │
│ │     ├── CreateAsync()     // 创建处方                                     │  │
│ │     ├── UpdateAsync()     // 更新处方                                     │  │
│ │     └── DeleteAsync()     // 删除处方                                     │  │
│ └─────────────────────────────────────────────────────────────────────────┘  │
│                                      ↓                                         │
│ ┌─────────────────────────────────────────────────────────────────────────┐  │
│ │ IApiService (Foundation层)                                                │  │
│ │ └── ApiService                                                            │  │
│ │     ├── GetAsync<T>()     // HTTP GET                                     │  │
│ │     ├── PostAsync<T>()    // HTTP POST                                    │  │
│ │     ├── PutAsync()        // HTTP PUT                                     │  │
│ │     └── DeleteAsync()     // HTTP DELETE                                  │  │
│ └─────────────────────────────────────────────────────────────────────────┘  │
│                                      ↓                                         │
│                          HttpClient → LYBT.WebAPI                              │
│                          /api/v1/prescriptions/*                               │
└──────────────────────────────────────────────────────────────────────────────┘
                                      ↓
┌──────────────────────────────────────────────────────────────────────────────┐
│ 外部依赖模块 (External Dependencies)                                           │
├──────────────────────────────────────────────────────────────────────────────┤
│ ┌──────────────────────┐    ┌──────────────────────┐                         │
│ │ LYBT.Desktop.Herbs   │    │ LYBT.Desktop.Formula │                         │
│ │ (药材选择)           │    │ (验方模板)           │                         │
│ └──────────────────────┘    └──────────────────────┘                         │
│         ↓                              ↓                                      │
│ HerbSelectionDialog           FormulaTemplateDialog                           │
│ - 查询药材列表                - 查询验方列表                                  │
│ - 拼音快速搜索                - 加载验方药材条目                              │
│ - 返回选中药材                - 应用验方到处方                                │
└──────────────────────────────────────────────────────────────────────────────┘

═══════════════════════════════════════════════════════════════════════════════
关键接口契约 (Key Interface Contracts)
═══════════════════════════════════════════════════════════════════════════════

ISaveable接口:
  - bool Validate()        // 验证必填项(剂数>0、药材条目>0、剂量有效)
  - Task SaveAsync()       // 保存处方(新建/更新)

IValidatable接口:
  - bool Validate()        // 验证数据完整性
  - string ValidationMessage { get; set; }  // 验证错误信息

IDialogAware接口:
  - Task OnDialogOpened(IDialogParameters)  // 对话框打开时初始化
  - void OnDialogClosed()                   // 对话框关闭时清理
  - bool CanCloseDialog()                   // 检查是否可关闭(HasChanges时弹出确认)
  - event Action<IDialogResult> RequestClose  // 请求关闭对话框事件

═══════════════════════════════════════════════════════════════════════════════
设计模式 (Design Patterns)
═══════════════════════════════════════════════════════════════════════════════

 Dialog-based架构:通过对话框组织复杂交互(编辑处方、选择药材、选择验方)
 ISaveable接口契约:与MedicalCase模块集成(通过接口解耦)
 Repository模式:三层架构(ViewModel → Repository → API)
 组件化ViewModel:分离计算器、命令处理器、数据管理器、事件协调器、验证器逻辑
 异步优先:全异步I/O操作,保证UI响应性
 IsBusy模式:加载状态管理,防止重复操作
 HasChanges模式:变更检测,控制保存按钮启用与对话框关闭确认
```

##  设计原则

### 1. ISaveable接口契约 - 与MedicalCase集成
-  **接口解耦**:PrescriptionEditorDialogViewModel实现ISaveable接口,MedicalCaseFlowViewModel通过接口调用处方功能(无需依赖具体类型)
-  **Validate验证**:处方保存前验证必填项(剂数>0、药材条目>0、剂量有效),返回ValidationMessage错误信息
-  **SaveAsync保存**:异步保存处方,支持新建(CreateAsync)和更新(UpdateAsync)两种模式
-  **HasChanges标记**:数据变更检测,控制保存按钮启用与对话框关闭确认(防止意外丢失数据)
-  **避免紧耦合**:不直接依赖PrescriptionEditorDialogViewModel具体类型,通过ISaveable接口实现松耦合
-  **避免返回Result<T>**:Repository直接返回DTO裸类型,不使用Result<T>包装(简化调用代码)

### 2. Dialog-based架构 - 对话框驱动的复杂交互组织方式
-  **对话框封装**:复杂功能封装为对话框(PrescriptionEditorDialog、HerbSelectionDialog、FormulaTemplateDialog),通过DialogService.ShowDialog调用
-  **参数传递**:通过DialogParameters传递参数(MedicalCaseId、PrescriptionId等),通过DialogResult.Parameters返回结果(SelectedHerbs、SelectedFormula)
-  **模态交互**:对话框模态显示,用户完成操作后关闭,触发RequestClose事件并返回DialogResult(ButtonResult.OK或Cancel)
-  **CanCloseDialog**:对话框关闭前检查HasChanges,如有未保存变更弹出确认对话框(防止意外丢失数据)
-  **OnDialogOpened**:对话框打开时初始化数据(LoadPrescriptionAsync加载处方、LoadDataAsync加载药材列表)
-  **避免Region导航**:处方编辑、药材选择、验方模板加载等复杂交互不适合Region导航,应使用Dialog对话框封装

### 3. 价格计算器 - 自动计算总价与单价
-  **总价计算公式**:TotalAmount = Σ(UnitPrice × Dosage) × DosageCount × Discount(所有药材单价×剂量的总和 × 剂数 × 折扣)
-  **自动更新**:监听DosageCount、Discount、HerbItems.CollectionChanged属性变更,自动重新计算总价(实时更新UI)
-  **单价计算**:PrescriptionItemRow.Subtotal = UnitPrice × Dosage(单个药材的小计金额)
-  **默认值设置**:DosageCount默认7剂,Discount默认1.0(无折扣),药材剂量默认从HerbDto.DefaultDosage获取
-  **精度控制**:所有金额使用decimal类型(避免浮点数精度问题),格式化时保留2位小数(F2)
-  **避免手动计算**:价格计算逻辑集中在CalculateTotalAmount方法,避免在多处重复计算逻辑

### 4. 验方模板支持 - 从Formula模块加载验方并应用到处方
-  **验方模板加载**:通过FormulaTemplateDialog从Formula模块查询验方列表,选择验方后返回FormulaDto
-  **药材条目应用**:将FormulaDto.HerbItems转换为PrescriptionItemRow并添加到HerbItems集合(自动加载药材名称、剂量、单位、单价)
-  **用法医嘱应用**:将FormulaDto.UsageInstructions、Description自动填充到处方的Usage、MedicalAdvice字段
-  **清空现有条目**:加载验方前先清空HerbItems集合(避免与现有药材混淆)
-  **智能匹配**:Formula模块的HerbId与Herbs模块的HerbId一致,确保药材正确匹配
-  **避免手动添加**:使用验方模板时不需要逐个手动添加药材,一次性加载所有验方药材条目

### 5. 打印服务 - FlowDocument生成与WPF打印
-  **FlowDocument构建**:PrescriptionFlowDocumentBuilder生成处方打印文档(包含标题、基础信息、药材表格、总价、用法、医嘱)
-  **WPF打印**:PrescriptionPrintService调用WPF PrintDialog显示打印对话框,用户选择打印机后调用PrintDocument打印
-  **打印预览**:PreviewAsync方法显示FlowDocument预览窗口(FlowDocumentScrollViewer),用户可预览后再打印
-  **A4纸适配**:FlowDocument.PageWidth=793.7、PageHeight=1122.5(A4纸像素尺寸),PagePadding=50(边距)
-  **表格布局**:使用Table、TableRow、TableCell生成药材条目表格(序号、药材名称、剂量、单价、小计)
-  **避免直接打印**:不直接调用Printer API,统一通过PrintDialog让用户选择打印机和打印选项

### 6. Repository模式与三层架构 - ViewModel → Repository → API
-  **三层分离**:ViewModel → IPrescriptionRepository → BaseApiRepository → IApiService → HttpClient(各层职责清晰)
-  **依赖注入**:ViewModel通过构造函数注入IPrescriptionRepository(避免ServiceLocator反模式)
-  **Repository返回裸类型**:Repository直接返回PrescriptionDto、PagedResult<PrescriptionDto>(不使用Result<T>包装)
-  **BaseApiRepository基类**:IPrescriptionRepository继承IBaseRepository<PrescriptionDto>,自动获得CRUD方法(GetPagedAsync、GetByIdAsync、CreateAsync、UpdateAsync、DeleteAsync)
-  **异常传播**:Repository层不捕获异常,直接抛出让ViewModel层处理(集中错误处理逻辑)
-  **避免直接调用Server Service**:Desktop端禁止直接依赖LYBT.Server.Services的Service(会导致运行时崩溃),必须通过Repository → API调用

### 7. 异步优先与UI响应性 - Async/Await + IsBusy模式
-  **全异步方法**:所有I/O操作使用async/await(LoadDataAsync、SaveAsync、DeleteAsync、PrintAsync等),避免阻塞UI线程
-  **IsBusy模式**:异步操作前设置IsBusy=true,操作完成后设置IsBusy=false(显示加载提示,禁用操作按钮)
-  **AsyncDelegateCommand**:使用Prism的AsyncDelegateCommand支持异步命令(自动处理CanExecute状态)
-  **try-finally保证**:IsBusy在finally块中设置为false(确保异常时也能恢复UI状态)
-  **Task返回类型**:异步方法返回Task或Task<T>(不使用async void,避免异常无法捕获)
-  **避免同步阻塞**:不使用.Result、.Wait()等同步阻塞方法(会导致UI卡死)

---

## 📚 详细文档

- **完整模块文档**:[docs/reference/modules/prescriptions/](../../../../docs/reference/modules/prescriptions/) *(待创建)*
- **架构设计**:[docs/explanation/architecture/client/prescriptions-design.md](../../../../docs/explanation/architecture/client/prescriptions-design.md) *(待创建)*
- **开发指南**:[docs/how-to-guides/client/prescriptions-development.md](../../../../docs/how-to-guides/client/prescriptions-development.md) *(待创建)*
- **Herbs模块集成**:[docs/how-to-guides/client/herbs-integration.md](../../../../docs/how-to-guides/client/herbs-integration.md) *(待创建)*
- **Formula模块集成**:[docs/how-to-guides/client/formula-integration.md](../../../../docs/how-to-guides/client/formula-integration.md) *(待创建)*
- **打印功能开发**:[docs/how-to-guides/client/print-functionality.md](../../../../docs/how-to-guides/client/print-functionality.md) *(待创建)*

---

**最后更新**:2025-01-29
**维护负责**:Client端Desktop模块开发组
