# Client端处方管理开发指南

> 💡 **架构变更提示（2025-11-02）**: Desktop端已删除`IPrescriptionRepository`，所有操作通过`IMedicalCaseRepository`聚合根。详见[ADR-008](../../explanation/architecture/decisions/ADR-008-desktop-consultation-prescription-no-independent-repository.md)

> **版本**: v1.1
> **最后更新**: 2025-11-02
> **维护负责**: Client端开发组

---

## 📋 文档概述

本文档提供**Client端处方管理模块 (LYBT.Desktop.Prescriptions)** 的完整开发指南，包含：

- ✅ **快速开始** - 3分钟上手处方模块开发
- ✅ **8列DataGrid布局开发** - PrescriptionItemRow转换逻辑实现
- ✅ **Component组件开发** - 5个专门化组件使用指南
- ✅ **ViewModel开发模式** - 13个命令实现与生命周期管理
- ✅ **药材选择与过滤** - ComboBox拼音码过滤实现 (Issue #1362)
- ✅ **历史处方复制** - 患者最近5条处方复制功能 (Issue #1374)
- ✅ **验方导入** - SelectFormulaDialog使用与验方模板管理
- ✅ **打印功能开发** - IPrescriptionPrintService使用与PDF导出
- ✅ **Repository集成** - IMedicalCaseRepository聚合根操作 (Issue #1606)
- ✅ **最佳实践** - MVVM规范、性能优化、错误处理
- ✅ **常见问题** - 开发中常见问题解答
- ✅ **调试技巧** - 断点调试、日志分析、性能分析

---

## 📚 相关文档

**必读前置文档**：
- [Client端处方管理架构设计](../../explanation/architecture/client/prescriptions-design.md) - **核心架构文档**
- [Client端患者管理开发指南](./patients-development.md) - MVVM开发规范参考
- [Client端MVVM架构指南](../../explanation/architecture/client/README.md) - 五层架构规范

**参考文档**：
- [Client端共享组件使用指南](../shared/components-usage.md) - HerbCalculatorBase、HerbValidatorBase
- [Server端处方管理开发指南](../server/prescriptions-development.md) - API端点契约
- [快速参考：代码模式](../../quick-reference/code-patterns.md) - WPF常用模式

---

## 🚀 1. 快速开始

### 1.1 环境准备

**必需依赖**：
```xml
<!-- LYBT.Desktop.Prescriptions.csproj -->
<ItemGroup>
  <!-- Prism MVVM框架 -->
  <PackageReference Include="Prism.Wpf" Version="8.1.x" />

  <!-- 日志 -->
  <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />

  <!-- 内部依赖 -->
  <ProjectReference Include="..\..\..\Shared\Models\LYBT.Shared.Models.csproj" />
  <ProjectReference Include="..\..\Contracts\LYBT.Desktop.Contracts\LYBT.Desktop.Contracts.csproj" />
  <ProjectReference Include="..\..\Infrastructure\LYBT.Desktop.Infrastructure\LYBT.Desktop.Infrastructure.csproj" />
  <ProjectReference Include="..\LYBT.Desktop.MedicalCase\LYBT.Desktop.MedicalCase.csproj" />
  <ProjectReference Include="..\LYBT.Desktop.Herbs\LYBT.Desktop.Herbs.csproj" />
</ItemGroup>
```

**项目结构**：
```
LYBT.Desktop.Prescriptions/
├── ViewModels/
│   ├── PrescriptionViewModel.cs           # 主ViewModel（983行）
│   ├── PrescriptionItemViewModel.cs       # 处方项ViewModel
│   ├── Components/
│   │   ├── PrescriptionDataManager.cs     # 数据管理器
│   │   ├── PrescriptionCalculator.cs      # 价格计算器
│   │   ├── PrescriptionValidator.cs       # 数据验证器
│   │   ├── PrescriptionCommandHandler.cs  # 命令处理器
│   │   └── PrescriptionEventCoordinator.cs# 事件协调器
│   └── Models/
│       └── PrescriptionItemRow.cs         # 8列DataGrid行模型
├── Views/
│   ├── PrescriptionView.xaml              # 处方视图
│   └── SelectFormulaDialog.xaml           # 验方选择对话框
└── PrescriptionsModule.cs                 # Prism模块注册
```

---

### 1.2 基本使用步骤

#### Step 1: 注册模块

```csharp
// App.xaml.cs
protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
{
    // 依赖顺序：MedicalCase → Herbs → Prescriptions
    moduleCatalog.AddModule<MedicalCaseModule>();
    moduleCatalog.AddModule<HerbsModule>();
    moduleCatalog.AddModule<PrescriptionsModule>();
}
```

#### Step 2: 导航到处方视图

```csharp
// 从诊断模块导航到处方模块（Three-Step工作流）
var parameters = new NavigationParameters
{
    { "MedicalCaseId", medicalCaseId }
};
_regionManager.RequestNavigate("MainRegion", "PrescriptionView", parameters);
```

#### Step 3: ViewModel结构

```csharp
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Modules.Prescriptions.ViewModels.Components;

public class PrescriptionViewModel : UnifiedViewModelBase
{
    #region 服务依赖
    private readonly IPrescriptionApi _prescriptionApi;
    private readonly IMedicalCaseRepository _medicalCaseRepository;
    private readonly IHerbRepository _herbRepository;
    #endregion

    #region 组件依赖（Issue #1445 ARCH-2）
    private readonly PrescriptionDataManager _dataManager;
    private readonly PrescriptionCalculator _calculator;
    private readonly PrescriptionValidator _validator;
    private readonly PrescriptionCommandHandler _commandHandler;
    private readonly PrescriptionEventCoordinator _eventCoordinator;
    #endregion

    #region 数据属性
    public Guid MedicalCaseId { get; set; }
    public MedicalCaseDto? CurrentMedicalCase { get; set; }
    public ObservableCollection<PrescriptionItemViewModel> PrescriptionItems => _dataManager.PrescriptionItems;
    public ObservableCollection<PrescriptionItemRow> ItemRows { get; set; } // 8列布局
    #endregion

    #region 命令
    public DelegateCommand SaveCommand => _commandHandler.SaveCommand;
    public DelegateCommand AddHerbCommand => _commandHandler.AddHerbCommand;
    public DelegateCommand<PrescriptionItemViewModel> RemoveHerbCommand => _commandHandler.RemoveHerbCommand;
    public DelegateCommand ImportFormulaCommand => _commandHandler.ImportFormulaCommand;
    public DelegateCommand<PrescriptionSearchResultDto> CopyFromHistoryCommand { get; }
    #endregion

    public PrescriptionViewModel(
        IPrescriptionApi prescriptionApi,
        IMedicalCaseRepository medicalCaseRepository,
        IHerbRepository herbRepository,
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        IRegionManager regionManager,
        PrescriptionDataManager dataManager,
        PrescriptionCalculator calculator,
        PrescriptionValidator validator,
        PrescriptionCommandHandler commandHandler,
        PrescriptionEventCoordinator eventCoordinator,
        ISessionManager? sessionManager = null,
        IUserNotificationService? userNotificationService = null)
        : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
    {
        _prescriptionApi = prescriptionApi ?? throw new ArgumentNullException(nameof(prescriptionApi));
        _medicalCaseRepository = medicalCaseRepository ?? throw new ArgumentNullException(nameof(medicalCaseRepository));
        _herbRepository = herbRepository ?? throw new ArgumentNullException(nameof(herbRepository));
        _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
        _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
        _eventCoordinator = eventCoordinator ?? throw new ArgumentNullException(nameof(eventCoordinator));

        // 设置命令处理器的依赖
        _commandHandler.SetDependencies(_dataManager, _validator, _calculator);

        // 初始化自有命令
        CopyFromHistoryCommand = new DelegateCommand<PrescriptionSearchResultDto>(ExecuteCopyFromHistory, prescription => prescription != null && !IsBusy);

        // 订阅事件
        SubscribeToEvents();
    }

    protected override async Task InitializeAsync(NavigationParameters parameters)
    {
        await base.InitializeAsync(parameters);

        if (parameters.ContainsKey("MedicalCaseId"))
        {
            MedicalCaseId = parameters.GetValue<Guid>("MedicalCaseId");
        }

        if (MedicalCaseId != Guid.Empty)
        {
            await LoadPrescriptionDataAsync();
        }
    }

    private async Task LoadPrescriptionDataAsync()
    {
        SetIsBusy(true, "正在初始化处方数据...");

        try
        {
            // 加载医疗案例信息
            CurrentMedicalCase = await _medicalCaseRepository.GetByIdAsync(MedicalCaseId);

            // 加载药材数据（Issue #1362）
            await LoadAllHerbsAsync();

            // 加载患者历史处方（Issue #1374）
            await LoadRecentPrescriptionsAsync();

            // 初始化数据管理器
            await _dataManager.InitializeAsync(MedicalCaseId);

            // 初始计算
            RecalculatePrice();

            // 初始化ItemRows（Issue #1360）
            RefreshItemRows();
        }
        finally
        {
            SetIsBusy(false);
        }
    }
}
```

---

### 1.3 核心概念

#### 1.3.1 聚合根约束 (Issue #1606 Phase 3)

**核心原则**：
- ❌ **IPrescriptionRepository已移除** - 所有Write操作必须通过IMedicalCaseRepository聚合根
- ✅ **Read操作使用IPrescriptionApi** - 只读查询调用API服务
- ✅ **Write操作通过MedicalCase聚合根** - CreatePrescriptionAsync / UpdatePrescriptionAsync / DeletePrescriptionAsync

```csharp
// ✅ 正确：通过聚合根创建处方
var result = await _medicalCaseRepository.CreatePrescriptionAsync(medicalCaseId, prescriptionCreateDto);

// ❌ 错误：直接使用IPrescriptionRepository（已移除）
// var result = await _prescriptionRepository.CreateAsync(prescriptionCreateDto);
```

#### 1.3.2 8列DataGrid布局 (Issue #1360)

**核心思想**：
- 每行显示**4个药材项**（每项占2列：名称+用量）
- PrescriptionItemViewModel → PrescriptionItemRow转换
- RefreshItemRows()自动刷新行集合

```csharp
/// <summary>
/// 处方项行模型 - 用于8列DataGrid布局
/// Issue #1360: [ENTRY-2] 实现Items→ItemRows转换逻辑
/// </summary>
public class PrescriptionItemRow
{
    public PrescriptionItemViewModel? Item1 { get; set; }  // 列1-2
    public PrescriptionItemViewModel? Item2 { get; set; }  // 列3-4
    public PrescriptionItemViewModel? Item3 { get; set; }  // 列5-6
    public PrescriptionItemViewModel? Item4 { get; set; }  // 列7-8
}

private void RefreshItemRows()
{
    ItemRows.Clear();

    // 每4个项目组成一行
    for (int i = 0; i < PrescriptionItems.Count; i += 4)
    {
        var row = new PrescriptionItemRow
        {
            Item1 = i < PrescriptionItems.Count ? PrescriptionItems[i] : null,
            Item2 = i + 1 < PrescriptionItems.Count ? PrescriptionItems[i + 1] : null,
            Item3 = i + 2 < PrescriptionItems.Count ? PrescriptionItems[i + 2] : null,
            Item4 = i + 3 < PrescriptionItems.Count ? PrescriptionItems[i + 3] : null
        };
        ItemRows.Add(row);
    }
}
```

#### 1.3.3 Component组件化 (Issue #1445 ARCH-2)

**5个专门化组件**：

| 组件 | 职责 | 核心方法 |
|------|------|---------|
| **PrescriptionDataManager** | 数据CRUD和状态管理 | InitializeAsync, SaveAsync, Clear, MarkAsChanged |
| **PrescriptionCalculator** | 价格计算和用量分析 | CalculatePrescriptionPrice (继承HerbCalculatorBase) |
| **PrescriptionValidator** | 数据验证和配伍禁忌 | ValidatePrescriptionData (继承HerbValidatorBase) |
| **PrescriptionCommandHandler** | 命令逻辑处理 | SaveCommand, AddHerbCommand, ImportFormulaCommand |
| **PrescriptionEventCoordinator** | 事件协调和消息传递 | OnPriceRecalculated, OnPrescriptionSaved |

---

## 🏗️ 2. 8列DataGrid布局开发

### 2.1 架构设计

**设计目标**：
- ✅ 每行显示4个药材项（名称+用量+单位+单价+小计），共8列
- ✅ 自动换行（超过4个项目自动生成新行）
- ✅ XAML简洁（避免100+行的复杂布局）

**实现原理**：
```
PrescriptionItems (ObservableCollection<PrescriptionItemViewModel>)
  ↓ RefreshItemRows()
ItemRows (ObservableCollection<PrescriptionItemRow>)
  ↓ XAML DataGrid Binding
8列DataGrid显示
```

---

### 2.2 PrescriptionItemRow实现

#### 文件位置
- `ViewModels/Models/PrescriptionItemRow.cs`

#### 核心代码

```csharp
using LYBT.Desktop.Modules.Prescriptions.ViewModels;

namespace LYBT.Desktop.Modules.Prescriptions.ViewModels.Models
{
    /// <summary>
    /// 处方项行模型 - 用于8列DataGrid布局
    /// Issue #1360: [ENTRY-2] 实现Items→ItemRows转换逻辑
    /// </summary>
    public class PrescriptionItemRow
    {
        /// <summary>
        /// 第1个药材项（列1-2）
        /// </summary>
        public PrescriptionItemViewModel? Item1 { get; set; }

        /// <summary>
        /// 第2个药材项（列3-4）
        /// </summary>
        public PrescriptionItemViewModel? Item2 { get; set; }

        /// <summary>
        /// 第3个药材项（列5-6）
        /// </summary>
        public PrescriptionItemViewModel? Item3 { get; set; }

        /// <summary>
        /// 第4个药材项（列7-8）
        /// </summary>
        public PrescriptionItemViewModel? Item4 { get; set; }
    }
}
```

---

### 2.3 RefreshItemRows转换逻辑

#### PrescriptionViewModel实现

```csharp
/// <summary>
/// 刷新处方项行集合（Items → ItemRows转换）
/// Issue #1360: [ENTRY-2] 实现Items→ItemRows转换逻辑
/// </summary>
private void RefreshItemRows()
{
    ItemRows.Clear();

    var items = PrescriptionItems;
    if (items == null || items.Count == 0)
    {
        return;
    }

    // 每4个项目组成一行
    for (int i = 0; i < items.Count; i += 4)
    {
        var row = new PrescriptionItemRow
        {
            Item1 = i < items.Count ? items[i] : null,
            Item2 = i + 1 < items.Count ? items[i + 1] : null,
            Item3 = i + 2 < items.Count ? items[i + 2] : null,
            Item4 = i + 3 < items.Count ? items[i + 3] : null
        };
        ItemRows.Add(row);
    }

    Logger.LogDebug($"已刷新处方项行集合，共 {items.Count} 个项目，{ItemRows.Count} 行");
}
```

#### 自动刷新机制

```csharp
protected override void SubscribeToEvents()
{
    // 订阅处方项集合变化事件（Issue #1360）
    PrescriptionItems.CollectionChanged += (s, e) => RefreshItemRows();

    // 订阅验方导入成功事件（Issue #1368）
    _commandHandler.OnFormulaImported += OnFormulaImported;
}

private async void OnFormulaImported()
{
    Logger.LogInformation("验方导入成功，重新加载处方数据");

    SetIsBusy(true, "正在刷新处方数据...");

    try
    {
        // 重新初始化数据管理器
        await _dataManager.InitializeAsync(MedicalCaseId);

        // 刷新显示行
        RefreshItemRows();

        // 重新计算价格
        RecalculatePrice();
    }
    finally
    {
        SetIsBusy(false);
    }
}
```

---

### 2.4 XAML绑定

#### PrescriptionView.xaml

```xml
<DataGrid
    ItemsSource="{Binding ItemRows}"
    AutoGenerateColumns="False"
    CanUserAddRows="False"
    CanUserDeleteRows="False"
    SelectionMode="Single"
    GridLinesVisibility="All"
    HeadersVisibility="Column">

    <!-- 第1个药材项（列1-2）-->
    <DataGrid.Columns>
        <DataGridTextColumn Header="药材名称1" Binding="{Binding Item1.HerbName}" Width="*" />
        <DataGridTextColumn Header="用量1" Binding="{Binding Item1.QuantityDisplay}" Width="80" />
    </DataGrid.Columns>

    <!-- 第2个药材项（列3-4）-->
    <DataGrid.Columns>
        <DataGridTextColumn Header="药材名称2" Binding="{Binding Item2.HerbName}" Width="*" />
        <DataGridTextColumn Header="用量2" Binding="{Binding Item2.QuantityDisplay}" Width="80" />
    </DataGrid.Columns>

    <!-- 第3个药材项（列5-6）-->
    <DataGrid.Columns>
        <DataGridTextColumn Header="药材名称3" Binding="{Binding Item3.HerbName}" Width="*" />
        <DataGridTextColumn Header="用量3" Binding="{Binding Item3.QuantityDisplay}" Width="80" />
    </DataGrid.Columns>

    <!-- 第4个药材项（列7-8）-->
    <DataGrid.Columns>
        <DataGridTextColumn Header="药材名称4" Binding="{Binding Item4.HerbName}" Width="*" />
        <DataGridTextColumn Header="用量4" Binding="{Binding Item4.QuantityDisplay}" Width="80" />
    </DataGrid.Columns>
</DataGrid>
```

**关键点**：
- ✅ `ItemsSource="{Binding ItemRows}"` - 绑定到行集合
- ✅ 8列布局：4个药材项 × 2列（名称+用量）
- ✅ 自动换行：超过4个项目自动生成新行
- ✅ 空项目处理：Item2/Item3/Item4为null时显示空白

---

### 2.5 性能优化

#### 批量刷新

```csharp
// ❌ 错误：每次添加药材都刷新ItemRows
public void AddHerb(HerbDto herb)
{
    PrescriptionItems.Add(new PrescriptionItemViewModel(herb));
    RefreshItemRows(); // 频繁刷新，性能差
}

// ✅ 正确：批量添加后一次性刷新
public void AddHerbs(List<HerbDto> herbs)
{
    foreach (var herb in herbs)
    {
        PrescriptionItems.Add(new PrescriptionItemViewModel(herb));
    }
    RefreshItemRows(); // 一次刷新，性能好
}
```

#### 虚拟化支持

```xml
<DataGrid
    ItemsSource="{Binding ItemRows}"
    VirtualizingPanel.IsVirtualizing="True"
    VirtualizingPanel.VirtualizationMode="Recycling"
    VirtualizingPanel.CacheLength="10,10"
    VirtualizingPanel.CacheLengthUnit="Item">
    <!-- 大量数据时启用虚拟化 -->
</DataGrid>
```

---

## 🧩 3. Component组件开发

### 3.1 PrescriptionDataManager使用

#### 核心职责
- ✅ 处方数据CRUD操作
- ✅ 状态管理（IsNewPrescription, HasChanges）
- ✅ 处方编号管理（Client临时编号 + Server正式编号）

#### 完整实现

```csharp
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Modules.Prescriptions.ViewModels.Components
{
    /// <summary>
    /// 处方数据管理器 - UltraThink专门化组件
    /// 职责单一: 专注处方数据的CRUD操作和状态管理
    /// </summary>
    public class PrescriptionDataManager
    {
        private readonly IPrescriptionApi _prescriptionApi;
        private readonly IMedicalCaseRepository _medicalCaseRepository;
        private readonly ILogger<PrescriptionDataManager> _logger;

        public Guid MedicalCaseId { get; private set; }
        public Guid PrescriptionId { get; private set; }
        public PrescriptionDto? CurrentPrescription { get; private set; }
        public bool IsNewPrescription { get; private set; } = true;
        public bool HasChanges { get; private set; } = false;

        /// <summary>
        /// 处方编号（Server生成，只读）
        /// Issue #1551: 格式 RX-YYYYMMDD-NNNN（如 RX-20251021-0001）
        /// </summary>
        public string? PrescriptionNumber { get; private set; }

        /// <summary>
        /// 处方编号（Client临时，可修改）
        /// </summary>
        public string PrescriptionNo { get; set; } = string.Empty;

        public int DosageCount { get; set; } = 7;
        public string Usage { get; set; } = "每日1剂，水煎服，分早晚两次温服";
        public string MedicalAdvice { get; set; } = string.Empty;
        public string Remark { get; set; } = string.Empty;
        public decimal Discount { get; set; } = 1.0m;
        public PrescriptionItemViewModel? SelectedItem { get; set; }

        public ObservableCollection<PrescriptionItemViewModel> PrescriptionItems { get; } = new();

        public PrescriptionDataManager(
            IPrescriptionApi prescriptionApi,
            IMedicalCaseRepository medicalCaseRepository,
            ILogger<PrescriptionDataManager> logger)
        {
            _prescriptionApi = prescriptionApi ?? throw new ArgumentNullException(nameof(prescriptionApi));
            _medicalCaseRepository = medicalCaseRepository ?? throw new ArgumentNullException(nameof(medicalCaseRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 初始化数据管理器
        /// </summary>
        public async Task InitializeAsync(Guid medicalCaseId)
        {
            MedicalCaseId = medicalCaseId;

            // 尝试加载现有处方（Issue #1608: 使用IPrescriptionApi）
            var response = await _prescriptionApi.GetByMedicalCaseIdAsync(medicalCaseId);
            var prescriptions = response.Data ?? new List<PrescriptionDto>();

            if (prescriptions.Any())
            {
                CurrentPrescription = prescriptions.First();
                PrescriptionId = CurrentPrescription.Id;
                PrescriptionNumber = CurrentPrescription.PrescriptionNumber; // Server生成
                PrescriptionNo = CurrentPrescription.PrescriptionNo ?? string.Empty;
                DosageCount = CurrentPrescription.DosageCount;
                Usage = CurrentPrescription.Usage ?? "每日1剂，水煎服，分早晚两次温服";
                MedicalAdvice = CurrentPrescription.Advice ?? string.Empty;
                Remark = CurrentPrescription.Remark ?? string.Empty;
                Discount = 1.0m; // 默认无折扣

                // 加载处方项
                PrescriptionItems.Clear();
                foreach (var item in CurrentPrescription.Items)
                {
                    PrescriptionItems.Add(new PrescriptionItemViewModel
                    {
                        HerbId = item.HerbId,
                        HerbName = item.HerbName,
                        Dosage = item.Dosage,
                        Unit = item.Unit,
                        UnitPrice = item.UnitPrice,
                        Remark = item.Remark
                    });
                }

                IsNewPrescription = false;
                _logger.LogInformation($"已加载现有处方，ID: {PrescriptionId}");
            }
            else
            {
                // 新建处方（生成临时编号）
                PrescriptionNo = $"CF{DateTime.Now:yyyyMMddHHmmss}";
                IsNewPrescription = true;
                _logger.LogInformation("新建处方，临时编号: {PrescriptionNo}", PrescriptionNo);
            }

            HasChanges = false;
        }

        /// <summary>
        /// 保存处方数据
        /// Issue #1608: 使用IMedicalCaseRepository.CreatePrescriptionAsync替代IPrescriptionRepository
        /// </summary>
        public async Task<bool> SaveAsync()
        {
            if (PrescriptionItems.Count == 0)
            {
                _logger.LogWarning("处方项为空，无法保存");
                return false;
            }

            var prescriptionCreateDto = new PrescriptionCreateDto
            {
                PatientId = Guid.Empty,  // 从MedicalCase获取
                DoctorId = Guid.Empty,   // 从Session获取
                ConsultationId = MedicalCaseId,
                Diagnosis = "中医诊断",
                DosageCount = DosageCount,
                Quantity = DosageCount,
                Usage = Usage,
                TotalAmount = PrescriptionItems.Sum(x => x.Quantity * x.UnitPrice) * DosageCount * Discount,
                Advice = MedicalAdvice,
                Remark = Remark,
                Items = PrescriptionItems.Select(item => new PrescriptionItemCreateDto
                {
                    HerbId = item.HerbId,
                    HerbName = item.HerbName,
                    Quantity = item.Quantity,
                    Unit = item.Unit,
                    UnitPrice = item.UnitPrice,
                    Remark = item.Remark
                }).ToList()
            };

            // Issue #1608: 通过MedicalCase聚合根创建处方
            var result = await _medicalCaseRepository.CreatePrescriptionAsync(MedicalCaseId, prescriptionCreateDto);
            if (result != null)
            {
                // Issue #1551: 保存后更新服务端生成的处方编号
                PrescriptionNumber = result.PrescriptionNumber;
                PrescriptionId = result.Id;
                CurrentPrescription = result;
                IsNewPrescription = false;
                HasChanges = false;

                _logger.LogInformation($"处方保存成功，编号: {PrescriptionNumber}");
                return true;
            }

            _logger.LogError("处方保存失败");
            return false;
        }

        /// <summary>
        /// 标记为已修改
        /// </summary>
        public void MarkAsChanged()
        {
            HasChanges = true;
        }

        /// <summary>
        /// 清空处方数据
        /// </summary>
        public void Clear()
        {
            PrescriptionItems.Clear();
            DosageCount = 7;
            Usage = "每日1剂，水煎服，分早晚两次温服";
            MedicalAdvice = string.Empty;
            Remark = string.Empty;
            Discount = 1.0m;
            HasChanges = false;

            _logger.LogInformation("处方数据已清空");
        }
    }
}
```

---

### 3.2 PrescriptionCalculator使用

#### 核心职责
- ✅ 价格计算（单剂价格 + 总价格 + 优惠后价格）
- ✅ 继承HerbCalculatorBase共享基类
- ✅ 支持折扣计算

#### 完整实现

```csharp
using System.Collections.Generic;
using System.Linq;
using LYBT.Desktop.Infrastructure.Base;
using LYBT.Desktop.Modules.Prescriptions.ViewModels;

namespace LYBT.Desktop.Modules.Prescriptions.ViewModels.Components
{
    /// <summary>
    /// 处方计算器 - 继承HerbCalculatorBase
    /// </summary>
    public class PrescriptionCalculator : HerbCalculatorBase
    {
        /// <summary>
        /// 计算结果
        /// </summary>
        public class CalculationResult
        {
            /// <summary>
            /// 单剂价格
            /// </summary>
            public decimal SingleDosagePrice { get; set; }

            /// <summary>
            /// 总价格（不含折扣）
            /// </summary>
            public decimal TotalPrice { get; set; }

            /// <summary>
            /// 优惠后价格
            /// </summary>
            public decimal DiscountedPrice { get; set; }

            /// <summary>
            /// 节省金额
            /// </summary>
            public decimal TotalSaved => TotalPrice - DiscountedPrice;

            /// <summary>
            /// 药材总数量
            /// </summary>
            public int TotalItemCount { get; set; }

            /// <summary>
            /// 总重量（克）
            /// </summary>
            public decimal TotalWeight { get; set; }
        }

        /// <summary>
        /// 计算处方价格
        /// </summary>
        /// <param name="items">处方项集合</param>
        /// <param name="dosageCount">剂数</param>
        /// <param name="discount">折扣（0.0-1.0）</param>
        /// <returns>计算结果</returns>
        public CalculationResult CalculatePrescriptionPrice(
            IEnumerable<PrescriptionItemViewModel> items,
            int dosageCount,
            decimal discount)
        {
            if (items == null || !items.Any())
            {
                return new CalculationResult();
            }

            // 计算单剂价格（每味药的小计之和）
            var singleDosagePrice = items.Sum(item => item.Quantity * item.UnitPrice);

            // 计算总价格（单剂价格 × 剂数）
            var totalPrice = singleDosagePrice * dosageCount;

            // 计算优惠后价格
            var discountedPrice = totalPrice * discount;

            // 计算总重量
            var totalWeight = items.Sum(item => item.Quantity);

            return new CalculationResult
            {
                SingleDosagePrice = singleDosagePrice,
                TotalPrice = totalPrice,
                DiscountedPrice = discountedPrice,
                TotalItemCount = items.Count(),
                TotalWeight = totalWeight
            };
        }

        /// <summary>
        /// 计算单个药材项的小计
        /// </summary>
        public decimal CalculateItemSubtotal(PrescriptionItemViewModel item)
        {
            return item.Quantity * item.UnitPrice;
        }

        /// <summary>
        /// 验证剂数是否合理
        /// </summary>
        public bool ValidateDosageCount(int dosageCount)
        {
            return dosageCount >= 1 && dosageCount <= 365; // 1-365剂
        }

        /// <summary>
        /// 验证折扣是否合理
        /// </summary>
        public bool ValidateDiscount(decimal discount)
        {
            return discount >= 0.0m && discount <= 1.0m; // 0%-100%
        }
    }
}
```

#### ViewModel集成

```csharp
private void RecalculatePrice()
{
    try
    {
        CalculationResult = _calculator.CalculatePrescriptionPrice(
            PrescriptionItems,
            DosageCount,
            Discount);

        // 通知价格相关属性变更
        RaisePropertyChanged(nameof(TotalPrice));
        RaisePropertyChanged(nameof(ActualTotal));
        RaisePropertyChanged(nameof(DiscountAmount));
        RaisePropertyChanged(nameof(ItemCount));
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "重新计算价格时发生异常");
    }
}

// 绑定属性
public decimal SingleDosagePrice => CalculationResult?.SingleDosagePrice ?? 0m;
public decimal TotalPrice => CalculationResult?.TotalPrice ?? 0m;
public decimal DiscountedPrice => CalculationResult?.DiscountedPrice ?? 0m;
public decimal TotalSaved => CalculationResult?.TotalSaved ?? 0m;
public decimal ActualTotal => DiscountedPrice; // 别名
public int ItemCount => PrescriptionItems?.Count ?? 0;
```

---

### 3.3 PrescriptionValidator使用

#### 核心职责
- ✅ 数据验证（必填字段、数值范围）
- ✅ 继承HerbValidatorBase共享基类
- ✅ 配伍禁忌检查

#### 完整实现

```csharp
using System.Collections.Generic;
using System.Linq;
using LYBT.Desktop.Infrastructure.Base;
using LYBT.Desktop.Modules.Prescriptions.ViewModels;

namespace LYBT.Desktop.Modules.Prescriptions.ViewModels.Components
{
    /// <summary>
    /// 处方验证器 - 继承HerbValidatorBase
    /// </summary>
    public class PrescriptionValidator : HerbValidatorBase
    {
        /// <summary>
        /// 验证结果
        /// </summary>
        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public List<string> Errors { get; set; } = new();
            public List<string> Warnings { get; set; } = new();
        }

        /// <summary>
        /// 验证处方数据
        /// </summary>
        public ValidationResult ValidatePrescriptionData(
            IEnumerable<PrescriptionItemViewModel> items,
            int dosageCount,
            string usage)
        {
            var result = new ValidationResult { IsValid = true };

            // 1. 验证处方项数量
            if (items == null || !items.Any())
            {
                result.IsValid = false;
                result.Errors.Add("处方至少需要包含1味药材");
            }
            else if (items.Count() < 3)
            {
                result.Warnings.Add("处方药材数量较少（<3味），请确认是否正确");
            }

            // 2. 验证剂数
            if (dosageCount < 1)
            {
                result.IsValid = false;
                result.Errors.Add("剂数必须大于0");
            }
            else if (dosageCount > 365)
            {
                result.IsValid = false;
                result.Errors.Add("剂数不能超过365");
            }

            // 3. 验证用法
            if (string.IsNullOrWhiteSpace(usage))
            {
                result.IsValid = false;
                result.Errors.Add("用法不能为空");
            }

            // 4. 验证每个药材项
            if (items != null)
            {
                foreach (var item in items)
                {
                    if (string.IsNullOrWhiteSpace(item.HerbName))
                    {
                        result.IsValid = false;
                        result.Errors.Add("存在药材名称为空的项");
                    }

                    if (item.Quantity <= 0)
                    {
                        result.IsValid = false;
                        result.Errors.Add($"{item.HerbName} 的用量必须大于0");
                    }

                    if (item.UnitPrice < 0)
                    {
                        result.IsValid = false;
                        result.Errors.Add($"{item.HerbName} 的单价不能为负数");
                    }
                }
            }

            // 5. 配伍禁忌检查（简化版）
            if (items != null && items.Count() >= 2)
            {
                var herbNames = items.Select(i => i.HerbName).ToList();
                var incompatible = CheckIncompatibleHerbs(herbNames);
                if (incompatible.Any())
                {
                    result.Warnings.Add($"存在可能的配伍禁忌: {string.Join(", ", incompatible)}");
                }
            }

            return result;
        }

        /// <summary>
        /// 检查配伍禁忌（简化版）
        /// </summary>
        private List<string> CheckIncompatibleHerbs(List<string> herbNames)
        {
            // TODO: 实现完整的配伍禁忌检查
            // 当前返回空列表（示例）
            return new List<string>();
        }

        /// <summary>
        /// 验证单个药材项
        /// </summary>
        public bool ValidateItem(PrescriptionItemViewModel item, out List<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrWhiteSpace(item.HerbName))
            {
                errors.Add("药材名称不能为空");
            }

            if (item.Quantity <= 0)
            {
                errors.Add("用量必须大于0");
            }

            if (item.UnitPrice < 0)
            {
                errors.Add("单价不能为负数");
            }

            return errors.Count == 0;
        }
    }
}
```

#### ViewModel集成

```csharp
public DelegateCommand ValidateCommand => _commandHandler.ValidateCommand;

// CommandHandler实现
public async Task ExecuteValidateAsync()
{
    var result = _validator.ValidatePrescriptionData(
        _dataManager.PrescriptionItems,
        _dataManager.DosageCount,
        _dataManager.Usage);

    if (!result.IsValid)
    {
        var errorMessage = string.Join("\n", result.Errors);
        await ShowErrorMessageAsync($"数据验证失败:\n{errorMessage}");
    }
    else if (result.Warnings.Any())
    {
        var warningMessage = string.Join("\n", result.Warnings);
        await ShowWarningMessageAsync($"验证警告:\n{warningMessage}");
    }
    else
    {
        await ShowInfoMessageAsync("数据验证通过");
    }
}
```

---

### 3.4 PrescriptionCommandHandler使用

#### 核心职责
- ✅ 处理13个命令逻辑
- ✅ 协调DataManager、Calculator、Validator
- ✅ 触发事件通知

#### 核心命令

```csharp
public class PrescriptionCommandHandler
{
    private PrescriptionDataManager? _dataManager;
    private PrescriptionValidator? _validator;
    private PrescriptionCalculator? _calculator;

    public DelegateCommand SaveCommand { get; }
    public DelegateCommand ClearCommand { get; }
    public DelegateCommand AddHerbCommand { get; }
    public DelegateCommand<PrescriptionItemViewModel> RemoveHerbCommand { get; }
    public DelegateCommand ImportFormulaCommand { get; }
    public DelegateCommand GeneratePrescriptionNoCommand { get; }
    public DelegateCommand ValidateCommand { get; }
    public DelegateCommand RecalculateCommand { get; }
    public DelegateCommand PrintPreviewCommand { get; }

    public event Action? OnPriceRecalculated;
    public event Action? OnPrescriptionSaved;
    public event Action? OnPrescriptionCleared;
    public event Action? OnFormulaImported;

    public void SetDependencies(
        PrescriptionDataManager dataManager,
        PrescriptionValidator validator,
        PrescriptionCalculator calculator)
    {
        _dataManager = dataManager;
        _validator = validator;
        _calculator = calculator;
    }

    private async Task ExecuteSaveAsync()
    {
        // 1. 验证数据
        var validationResult = _validator.ValidatePrescriptionData(
            _dataManager.PrescriptionItems,
            _dataManager.DosageCount,
            _dataManager.Usage);

        if (!validationResult.IsValid)
        {
            // 显示错误
            return;
        }

        // 2. 保存数据
        var success = await _dataManager.SaveAsync();
        if (success)
        {
            OnPrescriptionSaved?.Invoke();
        }
    }

    private void ExecuteClear()
    {
        _dataManager.Clear();
        OnPrescriptionCleared?.Invoke();
    }

    private void ExecuteRecalculate()
    {
        OnPriceRecalculated?.Invoke();
    }
}
```

---

### 3.5 PrescriptionEventCoordinator使用

#### 核心职责
- ✅ 事件协调和消息传递
- ✅ 跨模块通信（Three-Step工作流）
- ✅ 资源清理

#### 完整实现

```csharp
using System;
using Prism.Events;

namespace LYBT.Desktop.Modules.Prescriptions.ViewModels.Components
{
    /// <summary>
    /// 处方事件协调器
    /// </summary>
    public class PrescriptionEventCoordinator : IDisposable
    {
        private readonly IEventAggregator _eventAggregator;

        public PrescriptionEventCoordinator(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        }

        /// <summary>
        /// 发布处方保存成功事件
        /// </summary>
        public void PublishPrescriptionSaved(Guid prescriptionId)
        {
            _eventAggregator.GetEvent<PrescriptionSavedEvent>().Publish(prescriptionId);
        }

        /// <summary>
        /// 发布处方清空事件
        /// </summary>
        public void PublishPrescriptionCleared()
        {
            _eventAggregator.GetEvent<PrescriptionClearedEvent>().Publish();
        }

        /// <summary>
        /// 订阅处方保存成功事件
        /// </summary>
        public void SubscribePrescriptionSaved(Action<Guid> action)
        {
            _eventAggregator.GetEvent<PrescriptionSavedEvent>().Subscribe(action);
        }

        public void Dispose()
        {
            // 取消所有订阅
            _eventAggregator.GetEvent<PrescriptionSavedEvent>().Unsubscribe(null);
            _eventAggregator.GetEvent<PrescriptionClearedEvent>().Unsubscribe(null);
        }
    }

    // 事件定义
    public class PrescriptionSavedEvent : PubSubEvent<Guid> { }
    public class PrescriptionClearedEvent : PubSubEvent { }
}
```

---

## 🔍 4. 药材选择与过滤 (Issue #1362)

### 4.1 ComboBox拼音码过滤

**核心需求**：
- ✅ 支持药材名称搜索（如"当归"）
- ✅ 支持拼音码搜索（如"DG"或"dg"）
- ✅ 限制最多5个结果
- ✅ 不区分大小写

#### ViewModel实现

```csharp
#region 药材过滤数据 (Issue #1362: [ENTRY-4] 实现ComboBox拼音码过滤)

private List<HerbDto> _allHerbs = new();
private ObservableCollection<HerbDto> _filteredHerbs = new();

/// <summary>
/// 所有药材列表（用于过滤）
/// </summary>
public List<HerbDto> AllHerbs
{
    get => _allHerbs;
    set => SetProperty(ref _allHerbs, value);
}

/// <summary>
/// 过滤后的药材列表（绑定到ComboBox）
/// </summary>
public ObservableCollection<HerbDto> FilteredHerbs
{
    get => _filteredHerbs;
    set => SetProperty(ref _filteredHerbs, value);
}

#endregion

/// <summary>
/// 加载所有药材数据
/// Issue #1362: [ENTRY-4] 实现ComboBox拼音码过滤
/// </summary>
private async Task LoadAllHerbsAsync()
{
    try
    {
        // 使用SearchAsync获取所有药材（传入空字符串）
        var herbs = await _herbRepository.SearchAsync(string.Empty);
        AllHerbs = herbs ?? new List<HerbDto>();
        Logger.LogInformation($"已加载 {AllHerbs.Count} 个药材");
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "加载药材数据失败");
        AllHerbs = new List<HerbDto>();
    }
}

/// <summary>
/// 根据输入文本过滤药材
/// Issue #1362: [ENTRY-4] 实现ComboBox拼音码过滤
/// </summary>
/// <param name="searchText">搜索文本（药材名称或拼音码）</param>
public void FilterHerbs(string searchText)
{
    try
    {
        FilteredHerbs.Clear();

        // 如果搜索文本为空，不显示任何结果
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return;
        }

        // 过滤逻辑：匹配药材名称或拼音码（不区分大小写）
        var filtered = AllHerbs
            .Where(h => h.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                       (h.PinYinCode?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false))
            .Take(5) // 限制最多5个结果
            .ToList();

        // 添加到过滤结果集合
        foreach (var herb in filtered)
        {
            FilteredHerbs.Add(herb);
        }

        Logger.LogDebug($"过滤药材：输入='{searchText}'，结果数={filtered.Count}");
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "过滤药材时发生异常");
    }
}
```

---

### 4.2 XAML绑定

#### PrescriptionView.xaml

```xml
<ComboBox
    x:Name="HerbComboBox"
    ItemsSource="{Binding FilteredHerbs}"
    DisplayMemberPath="Name"
    SelectedValuePath="Id"
    IsEditable="True"
    IsTextSearchEnabled="False"
    StaysOpenOnEdit="True"
    TextSearch.TextPath="Name"
    Width="200">

    <!-- 文本变化时触发过滤 -->
    <i:Interaction.Triggers>
        <i:EventTrigger EventName="TextChanged">
            <i:InvokeCommandAction
                Command="{Binding FilterHerbsCommand}"
                CommandParameter="{Binding Text, ElementName=HerbComboBox}" />
        </i:EventTrigger>
    </i:Interaction.Triggers>

    <!-- 下拉列表模板 -->
    <ComboBox.ItemTemplate>
        <DataTemplate>
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="{Binding Name}" FontWeight="Bold" Margin="0,0,10,0" />
                <TextBlock Text="{Binding PinYinCode}" Foreground="Gray" FontSize="11" />
            </StackPanel>
        </DataTemplate>
    </ComboBox.ItemTemplate>
</ComboBox>
```

**关键点**：
- ✅ `IsEditable="True"` - 允许手动输入
- ✅ `IsTextSearchEnabled="False"` - 禁用默认搜索，使用自定义过滤
- ✅ `TextChanged`事件 - 触发FilterHerbs方法
- ✅ ItemTemplate - 显示药材名称和拼音码

---

### 4.3 HerbSelectionDialog使用

**场景**：需要更复杂的药材选择UI时，使用对话框模式。

#### SelectHerbDialog.xaml

```xml
<Window
    x:Class="LYBT.Desktop.Prescriptions.Views.SelectHerbDialog"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    Title="选择药材" Height="400" Width="600"
    WindowStartupLocation="CenterOwner"
    ShowInTaskbar="False">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- 搜索框 -->
        <TextBox
            Grid.Row="0"
            Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}"
            Margin="10"
            Padding="5">
            <TextBox.InputBindings>
                <KeyBinding Key="Return" Command="{Binding SearchCommand}" />
            </TextBox.InputBindings>
        </TextBox>

        <!-- 药材列表 -->
        <DataGrid
            Grid.Row="1"
            ItemsSource="{Binding FilteredHerbs}"
            SelectedItem="{Binding SelectedHerb}"
            AutoGenerateColumns="False"
            IsReadOnly="True"
            SelectionMode="Single"
            Margin="10">
            <DataGrid.Columns>
                <DataGridTextColumn Header="药材名称" Binding="{Binding Name}" Width="*" />
                <DataGridTextColumn Header="拼音码" Binding="{Binding PinYinCode}" Width="100" />
                <DataGridTextColumn Header="单价" Binding="{Binding UnitPrice, StringFormat=¥{0:F2}}" Width="80" />
                <DataGridTextColumn Header="单位" Binding="{Binding Unit}" Width="60" />
            </DataGrid.Columns>
        </DataGrid>

        <!-- 按钮 -->
        <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Right" Margin="10">
            <Button Content="确定" Command="{Binding ConfirmCommand}" Width="80" Margin="0,0,10,0" />
            <Button Content="取消" Command="{Binding CancelCommand}" Width="80" />
        </StackPanel>
    </Grid>
</Window>
```

#### 调用示例

```csharp
private async Task ExecuteAddHerbAsync()
{
    var dialog = new SelectHerbDialog
    {
        Owner = Application.Current.MainWindow,
        DataContext = new SelectHerbDialogViewModel(_herbRepository, LoggerFactory)
    };

    if (dialog.ShowDialog() == true)
    {
        var selectedHerb = (dialog.DataContext as SelectHerbDialogViewModel)?.SelectedHerb;
        if (selectedHerb != null)
        {
            var newItem = new PrescriptionItemViewModel(EventAggregator, LoggerFactory, RegionManager, SessionManager, UserNotificationService)
            {
                HerbId = selectedHerb.Id,
                HerbName = selectedHerb.Name,
                Unit = selectedHerb.Unit,
                UnitPrice = selectedHerb.UnitPrice
            };
            _dataManager.PrescriptionItems.Add(newItem);
            RecalculatePrice();
            RefreshItemRows();
        }
    }
}
```

---

## 📋 5. 历史处方复制 (Issue #1374 ENTRY-16)

### 5.1 功能概述

**核心需求**：
- ✅ 加载患者最近5条处方
- ✅ 显示处方日期、医生、药材数量
- ✅ 一键复制历史处方的所有药材项
- ✅ 自动刷新ItemRows和价格

---

### 5.2 加载历史处方

#### ViewModel实现

```csharp
private ObservableCollection<PrescriptionSearchResultDto> _recentPrescriptions = new();

/// <summary>
/// 患者最近处方列表 (Issue #1374 ENTRY-16)
/// </summary>
public ObservableCollection<PrescriptionSearchResultDto> RecentPrescriptions
{
    get => _recentPrescriptions;
    set => SetProperty(ref _recentPrescriptions, value);
}

private PrescriptionSearchResultDto? _selectedRecentPrescription;

/// <summary>
/// 选中的历史处方 (Issue #1374 ENTRY-16)
/// </summary>
public PrescriptionSearchResultDto? SelectedRecentPrescription
{
    get => _selectedRecentPrescription;
    set
    {
        if (SetProperty(ref _selectedRecentPrescription, value) && value != null)
        {
            // 选中后自动复制
            CopyFromHistoryCommand?.Execute(value);
        }
    }
}

/// <summary>
/// 加载患者最近处方列表 (Issue #1374 ENTRY-16)
/// </summary>
private async Task LoadRecentPrescriptionsAsync()
{
    try
    {
        if (CurrentMedicalCase?.PatientId == null || CurrentMedicalCase.PatientId == Guid.Empty)
        {
            Logger.LogWarning("无法加载历史处方：患者ID无效");
            return;
        }

        // Issue #1608: 使用IPrescriptionApi替代IPrescriptionRepository
        var response = await _prescriptionApi.GetPatientRecentPrescriptionsAsync(
            CurrentMedicalCase.PatientId,
            count: 5);
        var recentPrescriptions = response.Data ?? new List<PrescriptionSearchResultDto>();

        RecentPrescriptions.Clear();
        foreach (var prescription in recentPrescriptions)
        {
            RecentPrescriptions.Add(prescription);
        }

        Logger.LogInformation("已加载患者最近处方，共 {Count} 条", recentPrescriptions.Count);
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "加载患者最近处方失败");
        // 不抛出异常，避免影响主流程
    }
}
```

---

### 5.3 执行复制逻辑

#### CopyFromHistoryCommand实现

```csharp
/// <summary>
/// 从历史处方复制命令 (Issue #1374 ENTRY-16)
/// </summary>
public DelegateCommand<PrescriptionSearchResultDto> CopyFromHistoryCommand { get; }

// 构造函数中初始化
CopyFromHistoryCommand = new DelegateCommand<PrescriptionSearchResultDto>(
    ExecuteCopyFromHistory,
    prescription => prescription != null && !IsBusy);

/// <summary>
/// 从历史处方复制 (Issue #1374 ENTRY-16)
/// </summary>
private void ExecuteCopyFromHistory(PrescriptionSearchResultDto prescription)
{
    if (prescription == null) return;

    try
    {
        Logger.LogInformation("从历史处方复制，处方ID: {PrescriptionId}, 患者: {PatientName}",
            prescription.Id, prescription.PatientName);

        // 清空当前处方项
        _dataManager.Clear();

        // 复制处方项
        foreach (var item in prescription.Items)
        {
            var newItem = new PrescriptionItemViewModel(
                EventAggregator,
                LoggerFactory,
                RegionManager,
                SessionManager,
                UserNotificationService)
            {
                HerbId = item.HerbId,
                HerbName = item.HerbName,
                Dosage = item.Dosage,
                Unit = item.Unit,
                UnitPrice = item.UnitPrice,
                Remark = item.Remark
            };
            _dataManager.PrescriptionItems.Add(newItem);
        }

        // 重新计算价格
        RecalculatePrice();

        // 刷新ItemRows
        RefreshItemRows();

        // 清空选择（避免重复触发）
        SelectedRecentPrescription = null;

        ShowInfoMessage($"已从历史处方复制 {prescription.Items.Count} 味药材");
        Logger.LogInformation("历史处方复制完成，共 {Count} 味药材", prescription.Items.Count);
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "从历史处方复制时发生异常");
        ShowErrorMessage("复制历史处方失败");
    }
}
```

---

### 5.4 XAML绑定

#### PrescriptionView.xaml

```xml
<!-- 历史处方列表 -->
<GroupBox Header="历史处方（最近5条）" Grid.Row="2" Margin="10,0,10,10">
    <DataGrid
        ItemsSource="{Binding RecentPrescriptions}"
        SelectedItem="{Binding SelectedRecentPrescription}"
        AutoGenerateColumns="False"
        IsReadOnly="True"
        SelectionMode="Single"
        Height="150">
        <DataGrid.Columns>
            <DataGridTextColumn Header="日期" Binding="{Binding PrescriptionDate, StringFormat=yyyy-MM-dd}" Width="100" />
            <DataGridTextColumn Header="处方编号" Binding="{Binding PrescriptionNumber}" Width="150" />
            <DataGridTextColumn Header="医生" Binding="{Binding DoctorName}" Width="100" />
            <DataGridTextColumn Header="药材数量" Binding="{Binding ItemCount}" Width="80" />
            <DataGridTextColumn Header="剂数" Binding="{Binding DosageCount}" Width="60" />
            <DataGridTextColumn Header="总价" Binding="{Binding TotalAmount, StringFormat=¥{0:F2}}" Width="100" />
        </DataGrid.Columns>
    </DataGrid>
</GroupBox>
```

**关键点**：
- ✅ `SelectedItem="{Binding SelectedRecentPrescription}"` - 选中后自动触发复制
- ✅ IsReadOnly - 历史处方只读，不可编辑
- ✅ 显示处方日期、编号、医生、药材数量、剂数、总价

---

### 5.5 性能优化

#### 异步加载 + 取消令牌

```csharp
private CancellationTokenSource? _loadHistoryCts;

private async Task LoadRecentPrescriptionsAsync()
{
    // 取消之前的加载任务
    _loadHistoryCts?.Cancel();
    _loadHistoryCts = new CancellationTokenSource();

    try
    {
        SetIsBusy(true, "正在加载历史处方...");

        var response = await _prescriptionApi.GetPatientRecentPrescriptionsAsync(
            CurrentMedicalCase.PatientId,
            count: 5,
            _loadHistoryCts.Token); // 传入取消令牌

        RecentPrescriptions.Clear();
        foreach (var prescription in response.Data ?? new List<PrescriptionSearchResultDto>())
        {
            RecentPrescriptions.Add(prescription);
        }
    }
    catch (OperationCanceledException)
    {
        Logger.LogInformation("历史处方加载已取消");
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "加载患者最近处方失败");
    }
    finally
    {
        SetIsBusy(false);
    }
}

protected override void Dispose(bool disposing)
{
    if (disposing)
    {
        _loadHistoryCts?.Cancel();
        _loadHistoryCts?.Dispose();
    }
    base.Dispose(disposing);
}
```

---

## 📝 6. 验方导入 (Issue #1368 ENTRY-10)

### 6.1 功能概述

**核心需求**：
- ✅ 从验方库中选择已保存的验方模板
- ✅ 一键导入验方的所有药材项
- ✅ 自动填充剂数、用法、医嘱
- ✅ 支持验方模板管理（搜索、分类）

---

### 6.2 SelectFormulaDialog实现

#### SelectFormulaDialog.xaml

```xml
<Window
    x:Class="LYBT.Desktop.Prescriptions.Views.SelectFormulaDialog"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    Title="选择验方" Height="500" Width="800"
    WindowStartupLocation="CenterOwner"
    ShowInTaskbar="False">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- 搜索和筛选 -->
        <Grid Grid.Row="0" Margin="10">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>

            <TextBox
                Grid.Column="0"
                Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}"
                Padding="5"
                VerticalContentAlignment="Center">
                <TextBox.InputBindings>
                    <KeyBinding Key="Return" Command="{Binding SearchCommand}" />
                </TextBox.InputBindings>
            </TextBox>

            <Button
                Grid.Column="1"
                Content="搜索"
                Command="{Binding SearchCommand}"
                Width="80"
                Margin="10,0,0,0"/>
        </Grid>

        <!-- 验方列表 -->
        <DataGrid
            Grid.Row="1"
            ItemsSource="{Binding Formulas}"
            SelectedItem="{Binding SelectedFormula}"
            AutoGenerateColumns="False"
            IsReadOnly="True"
            SelectionMode="Single"
            Margin="10">
            <DataGrid.Columns>
                <DataGridTextColumn Header="验方名称" Binding="{Binding Name}" Width="200" />
                <DataGridTextColumn Header="分类" Binding="{Binding Category}" Width="100" />
                <DataGridTextColumn Header="药材数量" Binding="{Binding ItemCount}" Width="80" />
                <DataGridTextColumn Header="功效" Binding="{Binding Efficacy}" Width="*" />
                <DataGridTextColumn Header="创建日期" Binding="{Binding CreatedAt, StringFormat=yyyy-MM-dd}" Width="100" />
            </DataGrid.Columns>
        </DataGrid>

        <!-- 按钮 -->
        <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Right" Margin="10">
            <Button Content="确定" Command="{Binding ConfirmCommand}" Width="80" Margin="0,0,10,0" />
            <Button Content="取消" Command="{Binding CancelCommand}" Width="80" />
        </StackPanel>
    </Grid>
</Window>
```

---

### 6.3 SelectFormulaDialogViewModel实现

```csharp
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LYBT.Desktop.Formula.Interfaces;
using LYBT.Shared.Models.Contracts.Formula;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Mvvm;

namespace LYBT.Desktop.Prescriptions.ViewModels
{
    public class SelectFormulaDialogViewModel : BindableBase
    {
        private readonly IFormulaRepository _formulaRepository;
        private readonly ILogger<SelectFormulaDialogViewModel> _logger;

        private ObservableCollection<FormulaDto> _formulas = new();
        private FormulaDto? _selectedFormula;
        private string _searchText = string.Empty;

        public ObservableCollection<FormulaDto> Formulas
        {
            get => _formulas;
            set => SetProperty(ref _formulas, value);
        }

        public FormulaDto? SelectedFormula
        {
            get => _selectedFormula;
            set => SetProperty(ref _selectedFormula, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _ = SearchFormulasAsync();
                }
            }
        }

        public DelegateCommand SearchCommand { get; }
        public DelegateCommand ConfirmCommand { get; }
        public DelegateCommand CancelCommand { get; }

        public SelectFormulaDialogViewModel(
            IFormulaRepository formulaRepository,
            ILoggerFactory loggerFactory)
        {
            _formulaRepository = formulaRepository ?? throw new ArgumentNullException(nameof(formulaRepository));
            _logger = loggerFactory.CreateLogger<SelectFormulaDialogViewModel>();

            SearchCommand = new DelegateCommand(async () => await SearchFormulasAsync());
            ConfirmCommand = new DelegateCommand(Confirm, CanConfirm);
            CancelCommand = new DelegateCommand(Cancel);

            // 初始加载所有验方
            _ = LoadAllFormulasAsync();
        }

        private async Task LoadAllFormulasAsync()
        {
            try
            {
                var formulas = await _formulaRepository.GetAllAsync();
                Formulas.Clear();
                foreach (var formula in formulas)
                {
                    Formulas.Add(formula);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载验方列表失败");
            }
        }

        private async Task SearchFormulasAsync()
        {
            try
            {
                var formulas = await _formulaRepository.SearchAsync(SearchText);
                Formulas.Clear();
                foreach (var formula in formulas)
                {
                    Formulas.Add(formula);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索验方失败");
            }
        }

        private bool CanConfirm()
        {
            return SelectedFormula != null;
        }

        private void Confirm()
        {
            (Application.Current.MainWindow as Window)?.Close();
            (Application.Current.MainWindow as Window).DialogResult = true;
        }

        private void Cancel()
        {
            (Application.Current.MainWindow as Window)?.Close();
            (Application.Current.MainWindow as Window).DialogResult = false;
        }
    }
}
```

---

### 6.4 PrescriptionCommandHandler集成

#### ImportFormulaCommand实现

```csharp
public DelegateCommand ImportFormulaCommand { get; }

// 构造函数中初始化
ImportFormulaCommand = new DelegateCommand(ExecuteImportFormula, CanExecuteImportFormula);

private bool CanExecuteImportFormula()
{
    return !_isBusy;
}

private async void ExecuteImportFormula()
{
    try
    {
        var dialog = new SelectFormulaDialog
        {
            Owner = Application.Current.MainWindow,
            DataContext = new SelectFormulaDialogViewModel(_formulaRepository, _loggerFactory)
        };

        if (dialog.ShowDialog() == true)
        {
            var selectedFormula = (dialog.DataContext as SelectFormulaDialogViewModel)?.SelectedFormula;
            if (selectedFormula != null)
            {
                await ImportFormulaDataAsync(selectedFormula);
            }
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "导入验方时发生异常");
        await ShowErrorMessageAsync("导入验方失败");
    }
}

private async Task ImportFormulaDataAsync(FormulaDto formula)
{
    try
    {
        _logger.LogInformation($"开始导入验方: {formula.Name}");

        // 清空当前处方项
        _dataManager.Clear();

        // 导入验方药材项
        foreach (var item in formula.Items)
        {
            var newItem = new PrescriptionItemViewModel(...)
            {
                HerbId = item.HerbId,
                HerbName = item.HerbName,
                Dosage = item.Dosage,
                Unit = item.Unit,
                UnitPrice = item.UnitPrice,
                Remark = item.Remark
            };
            _dataManager.PrescriptionItems.Add(newItem);
        }

        // 导入验方元数据
        _dataManager.DosageCount = formula.DosageCount;
        _dataManager.Usage = formula.Usage;
        _dataManager.MedicalAdvice = formula.Efficacy; // 验方功效作为医嘱

        // 触发验方导入成功事件
        OnFormulaImported?.Invoke();

        await ShowInfoMessageAsync($"验方"{formula.Name}"导入成功，共 {formula.Items.Count} 味药材");
        _logger.LogInformation($"验方导入完成: {formula.Name}, 共 {formula.Items.Count} 味药材");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"导入验方失败: {formula.Name}");
        throw;
    }
}
```

---

### 6.5 OnFormulaImported事件处理

#### PrescriptionViewModel实现

```csharp
protected override void SubscribeToEvents()
{
    // 订阅验方导入成功事件 (Issue #1368 ENTRY-10)
    _commandHandler.OnFormulaImported += OnFormulaImported;
}

/// <summary>
/// 验方导入成功后刷新处方数据 (Issue #1368 ENTRY-10)
/// </summary>
private async void OnFormulaImported()
{
    Logger.LogInformation("验方导入成功，重新加载处方数据");

    try
    {
        SetIsBusy(true, "正在刷新处方数据...");

        // 直接重新初始化数据管理器，会自动加载最新的处方数据
        await _dataManager.InitializeAsync(MedicalCaseId);

        // 刷新显示行
        RefreshItemRows();

        // 重新计算价格
        RecalculatePrice();

        Logger.LogInformation("处方数据刷新完成");
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "刷新处方数据时发生异常");
        await ShowErrorMessageAsync("刷新处方数据失败，请重新加载页面");
    }
    finally
    {
        SetIsBusy(false);
    }
}
```

---

## 🖨️ 7. 打印功能开发

### 7.1 IPrescriptionPrintService接口

**核心方法**：
```csharp
public interface IPrescriptionPrintService
{
    /// <summary>
    /// 打印处方
    /// </summary>
    Task PrintPrescriptionAsync(PrescriptionDto prescription);

    /// <summary>
    /// 打印预览
    /// </summary>
    Task PreviewPrescriptionAsync(PrescriptionDto prescription);

    /// <summary>
    /// 批量打印
    /// </summary>
    Task BatchPrintAsync(List<PrescriptionDto> prescriptions);

    /// <summary>
    /// 导出为PDF
    /// </summary>
    Task<string> ExportToPdfAsync(PrescriptionDto prescription, string outputPath);
}
```

---

### 7.2 PrescriptionFlowDocumentBuilder实现

**核心职责**：
- ✅ 构建FlowDocument打印文档
- ✅ 包含医院信息、患者信息、处方明细、医生签名
- ✅ 支持打印预览和直接打印

#### 完整实现

```csharp
using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.Infrastructure.Services.Print
{
    /// <summary>
    /// 处方FlowDocument构建器
    /// </summary>
    public class PrescriptionFlowDocumentBuilder
    {
        /// <summary>
        /// 构建处方打印文档
        /// </summary>
        public FlowDocument BuildPrescriptionDocument(PrescriptionDto prescription)
        {
            var doc = new FlowDocument
            {
                PagePadding = new Thickness(50),
                ColumnWidth = double.PositiveInfinity,
                FontFamily = new FontFamily("SimSun"),
                FontSize = 12
            };

            // 1. 标题
            var title = new Paragraph(new Run("中医处方笺"))
            {
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            doc.Blocks.Add(title);

            // 2. 医院信息
            var hospitalInfo = new Paragraph(new Run($"凌隐宝堂中医诊所"))
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            doc.Blocks.Add(hospitalInfo);

            // 3. 处方编号和日期
            var prescriptionInfo = new Paragraph()
            {
                Margin = new Thickness(0, 0, 0, 10)
            };
            prescriptionInfo.Inlines.Add(new Run($"处方编号: {prescription.PrescriptionNumber}"));
            prescriptionInfo.Inlines.Add(new LineBreak());
            prescriptionInfo.Inlines.Add(new Run($"日期: {prescription.PrescriptionDate:yyyy年MM月dd日}"));
            doc.Blocks.Add(prescriptionInfo);

            // 4. 患者信息
            var patientInfo = new Paragraph()
            {
                Margin = new Thickness(0, 0, 0, 20)
            };
            patientInfo.Inlines.Add(new Run($"患者姓名: {prescription.PatientName}"));
            patientInfo.Inlines.Add(new Run("  "));
            patientInfo.Inlines.Add(new Run($"性别: {prescription.PatientGender}"));
            patientInfo.Inlines.Add(new Run("  "));
            patientInfo.Inlines.Add(new Run($"年龄: {prescription.PatientAge}岁"));
            doc.Blocks.Add(patientInfo);

            // 5. 处方明细（表格）
            var table = BuildPrescriptionTable(prescription);
            doc.Blocks.Add(table);

            // 6. 用法用量
            var usage = new Paragraph()
            {
                Margin = new Thickness(0, 20, 0, 10)
            };
            usage.Inlines.Add(new Run($"用法: {prescription.Usage}"));
            usage.Inlines.Add(new LineBreak());
            usage.Inlines.Add(new Run($"剂数: {prescription.DosageCount}剂"));
            doc.Blocks.Add(usage);

            // 7. 医嘱
            if (!string.IsNullOrWhiteSpace(prescription.Advice))
            {
                var advice = new Paragraph(new Run($"医嘱: {prescription.Advice}"))
                {
                    Margin = new Thickness(0, 0, 0, 20)
                };
                doc.Blocks.Add(advice);
            }

            // 8. 医生签名
            var signature = new Paragraph()
            {
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0)
            };
            signature.Inlines.Add(new Run($"医生签名: {prescription.DoctorName}"));
            signature.Inlines.Add(new LineBreak());
            signature.Inlines.Add(new Run($"日期: {DateTime.Now:yyyy年MM月dd日}"));
            doc.Blocks.Add(signature);

            return doc;
        }

        /// <summary>
        /// 构建处方明细表格
        /// </summary>
        private Table BuildPrescriptionTable(PrescriptionDto prescription)
        {
            var table = new Table
            {
                CellSpacing = 0,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1)
            };

            // 定义列
            table.Columns.Add(new TableColumn { Width = new GridLength(40) }); // 序号
            table.Columns.Add(new TableColumn { Width = new GridLength(150) }); // 药材名称
            table.Columns.Add(new TableColumn { Width = new GridLength(80) }); // 用量
            table.Columns.Add(new TableColumn { Width = new GridLength(80) }); // 单价
            table.Columns.Add(new TableColumn { Width = new GridLength(80) }); // 小计

            // 表头
            var headerGroup = new TableRowGroup();
            var headerRow = new TableRow();
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("序号"))) { TextAlignment = TextAlignment.Center });
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("药材名称"))) { TextAlignment = TextAlignment.Center });
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("用量"))) { TextAlignment = TextAlignment.Center });
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("单价"))) { TextAlignment = TextAlignment.Center });
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("小计"))) { TextAlignment = TextAlignment.Center });
            headerGroup.Rows.Add(headerRow);
            table.RowGroups.Add(headerGroup);

            // 数据行
            var dataGroup = new TableRowGroup();
            int index = 1;
            foreach (var item in prescription.Items)
            {
                var row = new TableRow();
                row.Cells.Add(new TableCell(new Paragraph(new Run(index.ToString()))) { TextAlignment = TextAlignment.Center });
                row.Cells.Add(new TableCell(new Paragraph(new Run(item.HerbName))));
                row.Cells.Add(new TableCell(new Paragraph(new Run($"{item.Dosage}{item.Unit}"))) { TextAlignment = TextAlignment.Right });
                row.Cells.Add(new TableCell(new Paragraph(new Run($"¥{item.UnitPrice:F2}"))) { TextAlignment = TextAlignment.Right });
                row.Cells.Add(new TableCell(new Paragraph(new Run($"¥{item.Dosage * item.UnitPrice:F2}"))) { TextAlignment = TextAlignment.Right });
                dataGroup.Rows.Add(row);
                index++;
            }
            table.RowGroups.Add(dataGroup);

            // 合计行
            var totalGroup = new TableRowGroup();
            var totalRow = new TableRow();
            totalRow.Cells.Add(new TableCell(new Paragraph(new Run("合计"))) { ColumnSpan = 4, TextAlignment = TextAlignment.Right });
            totalRow.Cells.Add(new TableCell(new Paragraph(new Run($"¥{prescription.TotalAmount:F2}"))) { TextAlignment = TextAlignment.Right });
            totalGroup.Rows.Add(totalRow);
            table.RowGroups.Add(totalGroup);

            return table;
        }
    }
}
```

---

### 7.3 PrescriptionPrintService实现

```csharp
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.Services.Print
{
    public class PrescriptionPrintService : IPrescriptionPrintService
    {
        private readonly ILogger<PrescriptionPrintService> _logger;
        private readonly PrescriptionFlowDocumentBuilder _documentBuilder;

        public PrescriptionPrintService(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<PrescriptionPrintService>();
            _documentBuilder = new PrescriptionFlowDocumentBuilder();
        }

        /// <summary>
        /// 打印处方
        /// </summary>
        public async Task PrintPrescriptionAsync(PrescriptionDto prescription)
        {
            await Task.Run(() =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var doc = _documentBuilder.BuildPrescriptionDocument(prescription);
                    var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;

                    var dialog = new PrintDialog();
                    if (dialog.ShowDialog() == true)
                    {
                        dialog.PrintDocument(paginator, $"处方-{prescription.PrescriptionNumber}");
                        _logger.LogInformation($"处方打印完成: {prescription.PrescriptionNumber}");
                    }
                });
            });
        }

        /// <summary>
        /// 打印预览
        /// </summary>
        public async Task PreviewPrescriptionAsync(PrescriptionDto prescription)
        {
            await Task.Run(() =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var doc = _documentBuilder.BuildPrescriptionDocument(prescription);

                    var previewWindow = new Window
                    {
                        Title = $"打印预览 - {prescription.PrescriptionNumber}",
                        Width = 800,
                        Height = 1000,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen
                    };

                    var viewer = new FlowDocumentScrollViewer
                    {
                        Document = doc,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                    };

                    previewWindow.Content = viewer;
                    previewWindow.ShowDialog();

                    _logger.LogInformation($"处方预览完成: {prescription.PrescriptionNumber}");
                });
            });
        }

        /// <summary>
        /// 批量打印
        /// </summary>
        public async Task BatchPrintAsync(List<PrescriptionDto> prescriptions)
        {
            foreach (var prescription in prescriptions)
            {
                await PrintPrescriptionAsync(prescription);
            }
        }

        /// <summary>
        /// 导出为PDF
        /// </summary>
        public async Task<string> ExportToPdfAsync(PrescriptionDto prescription, string outputPath)
        {
            // TODO: 实现PDF导出功能（需要引入PDF库，如iTextSharp或PdfSharp）
            await Task.CompletedTask;
            throw new NotImplementedException("PDF导出功能开发中");
        }
    }
}
```

---

### 7.4 ViewModel集成

```csharp
public DelegateCommand PrintPreviewCommand => _commandHandler.PrintPreviewCommand;

// CommandHandler实现
private async Task ExecutePrintPreviewAsync()
{
    try
    {
        if (_dataManager.CurrentPrescription == null)
        {
            await ShowWarningMessageAsync("请先保存处方后再预览");
            return;
        }

        SetIsBusy(true, "正在生成打印预览...");

        await _printService.PreviewPrescriptionAsync(_dataManager.CurrentPrescription);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "打印预览失败");
        await ShowErrorMessageAsync("打印预览失败");
    }
    finally
    {
        SetIsBusy(false);
    }
}
```

---

## 🔗 8. Repository集成

> ⚠️ **架构决策**: Desktop端已**完全删除**`IPrescriptionRepository`接口（包括空接口桩）
> - 详见：[ADR-008: Desktop端不独立实现Repository](../../explanation/architecture/decisions/ADR-008-desktop-consultation-prescription-no-independent-repository.md)
> - 原决策：[ADR-003: Repository层简化](../../explanation/architecture/decisions/ADR-003-repository-simplification.md)
> - 历史参考：Issue #1606（Prescription聚合根整合）

### 8.1 IMedicalCaseRepository聚合根操作

**核心原则**：Prescription作为MedicalCase聚合根的子实体，所有Write操作必须通过聚合根访问。

- ✅ 所有Write操作通过IMedicalCaseRepository聚合根
- ✅ Read操作使用IPrescriptionApi（轻量级）
- ❌ IPrescriptionRepository已移除（Issue #1606）

---

### 8.2 CreatePrescriptionAsync实现

#### PrescriptionDataManager使用

```csharp
/// <summary>
/// 保存处方数据
/// Issue #1608: 使用IMedicalCaseRepository.CreatePrescriptionAsync替代IPrescriptionRepository
/// </summary>
public async Task<bool> SaveAsync()
{
    if (PrescriptionItems.Count == 0)
    {
        _logger.LogWarning("处方项为空，无法保存");
        return false;
    }

    var prescriptionCreateDto = new PrescriptionCreateDto
    {
        PatientId = Guid.Empty,  // 从MedicalCase获取
        DoctorId = Guid.Empty,   // 从Session获取
        ConsultationId = MedicalCaseId,
        Diagnosis = "中医诊断",
        DosageCount = DosageCount,
        Quantity = DosageCount,
        Usage = Usage,
        TotalAmount = PrescriptionItems.Sum(x => x.Quantity * x.UnitPrice) * DosageCount * Discount,
        Advice = MedicalAdvice,
        Remark = Remark,
        Items = PrescriptionItems.Select(item => new PrescriptionItemCreateDto
        {
            HerbId = item.HerbId,
            HerbName = item.HerbName,
            Quantity = item.Quantity,
            Unit = item.Unit,
            UnitPrice = item.UnitPrice,
            Remark = item.Remark
        }).ToList()
    };

    // Issue #1608: 通过MedicalCase聚合根创建处方
    var result = await _medicalCaseRepository.CreatePrescriptionAsync(MedicalCaseId, prescriptionCreateDto);
    if (result != null)
    {
        // Issue #1551: 保存后更新服务端生成的处方编号
        PrescriptionNumber = result.PrescriptionNumber;
        PrescriptionId = result.Id;
        CurrentPrescription = result;
        IsNewPrescription = false;
        HasChanges = false;

        _logger.LogInformation($"处方保存成功，编号: {PrescriptionNumber}");
        return true;
    }

    _logger.LogError("处方保存失败");
    return false;
}
```

---

### 8.3 IPrescriptionApi只读查询

#### 查询历史处方

```csharp
/// <summary>
/// 加载患者最近处方列表 (Issue #1374 ENTRY-16)
/// </summary>
private async Task LoadRecentPrescriptionsAsync()
{
    try
    {
        if (CurrentMedicalCase?.PatientId == null || CurrentMedicalCase.PatientId == Guid.Empty)
        {
            Logger.LogWarning("无法加载历史处方：患者ID无效");
            return;
        }

        // Issue #1608: 使用IPrescriptionApi替代IPrescriptionRepository（只读查询）
        var response = await _prescriptionApi.GetPatientRecentPrescriptionsAsync(
            CurrentMedicalCase.PatientId,
            count: 5);
        var recentPrescriptions = response.Data ?? new List<PrescriptionSearchResultDto>();

        RecentPrescriptions.Clear();
        foreach (var prescription in recentPrescriptions)
        {
            RecentPrescriptions.Add(prescription);
        }

        Logger.LogInformation("已加载患者最近处方，共 {Count} 条", recentPrescriptions.Count);
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "加载患者最近处方失败");
    }
}
```

---

### 8.4 错误处理

#### 网络异常处理

```csharp
private async Task<bool> SaveWithRetryAsync(int maxRetries = 3)
{
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            return await _dataManager.SaveAsync();
        }
        catch (HttpRequestException ex)
        {
            Logger.LogWarning(ex, $"保存处方失败（第{i + 1}次尝试），{maxRetries - i - 1}次重试机会");

            if (i == maxRetries - 1)
            {
                await ShowErrorMessageAsync("网络异常，保存失败，请稍后重试");
                return false;
            }

            await Task.Delay(1000 * (i + 1)); // 指数退避
        }
    }

    return false;
}
```

#### 数据验证异常处理

```csharp
private async Task ExecuteSaveAsync()
{
    try
    {
        SetIsBusy(true, "正在保存处方...");

        // 1. 验证数据
        var validationResult = _validator.ValidatePrescriptionData(
            _dataManager.PrescriptionItems,
            _dataManager.DosageCount,
            _dataManager.Usage);

        if (!validationResult.IsValid)
        {
            var errorMessage = string.Join("\n", validationResult.Errors);
            await ShowErrorMessageAsync($"数据验证失败:\n{errorMessage}");
            return;
        }

        // 2. 显示警告（如果有）
        if (validationResult.Warnings.Any())
        {
            var warningMessage = string.Join("\n", validationResult.Warnings);
            var result = await ShowConfirmationAsync($"验证警告:\n{warningMessage}\n\n是否继续保存？");
            if (!result)
            {
                return;
            }
        }

        // 3. 保存数据（带重试）
        var success = await SaveWithRetryAsync();
        if (success)
        {
            await ShowInfoMessageAsync("处方保存成功");
            OnPrescriptionSaved?.Invoke();
        }
        else
        {
            await ShowErrorMessageAsync("处方保存失败");
        }
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "保存处方时发生未处理的异常");
        await ShowErrorMessageAsync("保存处方时发生异常，请联系管理员");
    }
    finally
    {
        SetIsBusy(false);
    }
}
```

---

## 📋 9. 最佳实践

### 9.1 MVVM规范

#### 属性绑定

```csharp
// ✅ 正确：使用SetProperty自动触发PropertyChanged
private string _prescriptionNo = string.Empty;
public string PrescriptionNo
{
    get => _prescriptionNo;
    set => SetProperty(ref _prescriptionNo, value);
}

// ❌ 错误：直接赋值不触发PropertyChanged
public string PrescriptionNo { get; set; }
```

#### 命令绑定

```csharp
// ✅ 正确：使用DelegateCommand并提供CanExecute
public DelegateCommand SaveCommand { get; }

SaveCommand = new DelegateCommand(ExecuteSave, CanSave);

private bool CanSave()
{
    return !IsBusy && PrescriptionItems.Count > 0;
}

// ❌ 错误：直接在Code-behind中处理Click事件
private void SaveButton_Click(object sender, RoutedEventArgs e)
{
    // 违反MVVM原则
}
```

#### 依赖注入

```csharp
// ✅ 正确：通过构造函数注入
public PrescriptionViewModel(
    IPrescriptionApi prescriptionApi,
    IMedicalCaseRepository medicalCaseRepository,
    PrescriptionDataManager dataManager,
    ...)
{
    _prescriptionApi = prescriptionApi ?? throw new ArgumentNullException(nameof(prescriptionApi));
    _medicalCaseRepository = medicalCaseRepository ?? throw new ArgumentNullException(nameof(medicalCaseRepository));
    _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
}

// ❌ 错误：使用ServiceLocator反模式
var prescriptionApi = Container.Resolve<IPrescriptionApi>();
```

---

### 9.2 性能优化

#### 虚拟化大集合

```xml
<DataGrid
    ItemsSource="{Binding ItemRows}"
    VirtualizingPanel.IsVirtualizing="True"
    VirtualizingPanel.VirtualizationMode="Recycling"
    VirtualizingPanel.CacheLength="10,10">
</DataGrid>
```

#### 异步加载 + 取消令牌

```csharp
private CancellationTokenSource? _loadCts;

private async Task LoadDataAsync()
{
    _loadCts?.Cancel();
    _loadCts = new CancellationTokenSource();

    try
    {
        SetIsBusy(true);
        var data = await _repository.GetDataAsync(_loadCts.Token);
        // 处理数据
    }
    catch (OperationCanceledException)
    {
        // 忽略取消异常
    }
    finally
    {
        SetIsBusy(false);
    }
}

protected override void Dispose(bool disposing)
{
    if (disposing)
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
    }
    base.Dispose(disposing);
}
```

#### 批量操作优化

```csharp
// ✅ 正确：批量操作后一次刷新
public void AddHerbsBatch(List<HerbDto> herbs)
{
    foreach (var herb in herbs)
    {
        PrescriptionItems.Add(new PrescriptionItemViewModel(herb));
    }
    RefreshItemRows(); // 一次刷新
    RecalculatePrice(); // 一次计算
}

// ❌ 错误：每次添加都刷新
public void AddHerb(HerbDto herb)
{
    PrescriptionItems.Add(new PrescriptionItemViewModel(herb));
    RefreshItemRows(); // 频繁刷新
    RecalculatePrice(); // 频繁计算
}
```

---

### 9.3 错误处理

#### 分层错误处理

```csharp
// Layer 1: CommandHandler层
private async Task ExecuteSaveAsync()
{
    try
    {
        await _dataManager.SaveAsync();
        OnPrescriptionSaved?.Invoke();
    }
    catch (ValidationException ex)
    {
        _logger.LogWarning(ex, "保存处方验证失败");
        await ShowErrorMessageAsync($"数据验证失败: {ex.Message}");
    }
    catch (HttpRequestException ex)
    {
        _logger.LogError(ex, "保存处方网络异常");
        await ShowErrorMessageAsync("网络异常，请稍后重试");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "保存处方未知异常");
        await ShowErrorMessageAsync("保存失败，请联系管理员");
    }
}

// Layer 2: DataManager层
public async Task<bool> SaveAsync()
{
    try
    {
        var result = await _medicalCaseRepository.CreatePrescriptionAsync(MedicalCaseId, dto);
        return result != null;
    }
    catch (HttpRequestException)
    {
        throw; // 网络异常向上传递
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "保存处方失败");
        throw; // 其他异常向上传递
    }
}
```

---

## 📚 10. 常见问题

### Q1: 8列DataGrid为什么使用PrescriptionItemRow而不是直接绑定PrescriptionItems?

**答**：
- ✅ **简化XAML** - 避免100+行的复杂布局代码
- ✅ **自动换行** - 超过4个项目自动生成新行
- ✅ **空项处理** - Item2/Item3/Item4为null时显示空白
- ✅ **性能优化** - DataGrid只渲染可见行，提升性能

---

### Q2: 为什么所有Write操作必须通过IMedicalCaseRepository?

**答**：Issue #1606 Phase 3架构调整，实现聚合根约束：
- ✅ **聚合根一致性** - Prescription作为MedicalCase的一部分，保证事务一致性
- ✅ **架构简化** - 移除IPrescriptionRepository，减少Repository层复杂度
- ✅ **符合DDD原则** - Prescription是值对象，不独立存在

---

### Q3: FilterHerbs为什么限制最多5个结果?

**答**：Issue #1362要求：
- ✅ **用户体验** - 避免下拉列表过长，难以选择
- ✅ **性能优化** - 减少渲染开销
- ✅ **引导精确搜索** - 鼓励用户输入更精确的拼音码或药材名称

---

### Q4: CopyFromHistoryCommand为什么在SelectedRecentPrescription的setter中自动执行?

**答**：
- ✅ **用户体验优化** - 选中历史处方后自动复制，减少点击操作
- ✅ **防止重复触发** - ExecuteCopyFromHistory结尾设置`SelectedRecentPrescription = null`
- ⚠️ **注意** - 如果需要手动确认，应移除自动执行逻辑

---

### Q5: RefreshItemRows何时自动触发?

**答**：2种场景自动触发：
1. **PrescriptionItems.CollectionChanged事件** - 添加/删除药材项时
2. **OnFormulaImported事件** - 验方导入成功后

**手动触发场景**：
- 历史处方复制后（ExecuteCopyFromHistory）
- 初始化完成后（LoadPrescriptionDataAsync）

---

### Q6: PrescriptionNumber和PrescriptionNo的区别?

**答**：Issue #1551处方编号双轨制：
- **PrescriptionNumber** - Server端生成（RX-YYYYMMDD-NNNN），只读，保存后自动更新
- **PrescriptionNo** - Client端生成（CF+时间戳），可修改，临时编号

---

### Q7: 为什么Component组件需要SetDependencies?

**答**：依赖注入顺序问题：
1. Prism先注入PrescriptionViewModel（包含5个Component）
2. PrescriptionViewModel构造函数中调用`_commandHandler.SetDependencies(_dataManager, _validator, _calculator)`
3. CommandHandler才能使用DataManager、Validator、Calculator

---

## 🔧 11. 调试技巧

### 11.1 断点调试

#### 关键断点位置

```csharp
// 1. 初始化断点
protected override async Task InitializeAsync(NavigationParameters parameters)
{
    // 设置断点：检查MedicalCaseId是否正确传递
    if (parameters.ContainsKey("MedicalCaseId"))
    {
        MedicalCaseId = parameters.GetValue<Guid>("MedicalCaseId"); // 断点
    }

    await LoadPrescriptionDataAsync(); // 断点
}

// 2. 数据加载断点
private async Task LoadPrescriptionDataAsync()
{
    await LoadMedicalCaseAsync(); // 断点
    await LoadAllHerbsAsync(); // 断点
    await _dataManager.InitializeAsync(MedicalCaseId); // 断点
    RefreshItemRows(); // 断点：检查ItemRows是否正确生成
}

// 3. 过滤断点
public void FilterHerbs(string searchText)
{
    var filtered = AllHerbs
        .Where(h => h.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                   (h.PinYinCode?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false))
        .Take(5)
        .ToList(); // 断点：检查过滤结果

    foreach (var herb in filtered)
    {
        FilteredHerbs.Add(herb); // 断点：检查是否添加到集合
    }
}

// 4. 保存断点
private async Task ExecuteSaveAsync()
{
    var validationResult = _validator.ValidatePrescriptionData(...); // 断点：检查验证结果
    var success = await _dataManager.SaveAsync(); // 断点：检查保存结果
}

// 5. ItemRows转换断点
private void RefreshItemRows()
{
    for (int i = 0; i < PrescriptionItems.Count; i += 4)
    {
        var row = new PrescriptionItemRow
        {
            Item1 = i < PrescriptionItems.Count ? PrescriptionItems[i] : null,
            Item2 = i + 1 < PrescriptionItems.Count ? PrescriptionItems[i + 1] : null,
            Item3 = i + 2 < PrescriptionItems.Count ? PrescriptionItems[i + 2] : null,
            Item4 = i + 3 < PrescriptionItems.Count ? PrescriptionItems[i + 3] : null
        }; // 断点：检查每行是否正确
        ItemRows.Add(row);
    }
}
```

---

### 11.2 日志分析

#### 日志级别使用

```csharp
// Trace - 详细调试信息
Logger.LogTrace("FilterHerbs: 输入='{SearchText}', 结果数={Count}", searchText, filtered.Count);

// Debug - 开发阶段调试
Logger.LogDebug("已刷新处方项行集合，共 {ItemCount} 个项目，{RowCount} 行", items.Count, ItemRows.Count);

// Information - 关键操作记录
Logger.LogInformation("处方保存成功，编号: {PrescriptionNumber}", PrescriptionNumber);

// Warning - 警告信息
Logger.LogWarning("处方项为空，无法保存");

// Error - 错误信息
Logger.LogError(ex, "保存处方失败");
```

#### 日志查询

```bash
# 查看处方模块所有日志
Get-Content "logs/LYBT-20251030.log" | Select-String "PrescriptionViewModel"

# 查看特定功能日志（如历史处方复制）
Get-Content "logs/LYBT-20251030.log" | Select-String "从历史处方复制"

# 查看错误日志
Get-Content "logs/LYBT-20251030.log" | Select-String "Error"
```

---

### 11.3 性能分析

#### Stopwatch性能计时

```csharp
private async Task LoadPrescriptionDataAsync()
{
    var sw = System.Diagnostics.Stopwatch.StartNew();

    SetIsBusy(true, "正在初始化处方数据...");

    try
    {
        await LoadMedicalCaseAsync();
        Logger.LogDebug("LoadMedicalCaseAsync: {Elapsed}ms", sw.ElapsedMilliseconds);

        sw.Restart();
        await LoadAllHerbsAsync();
        Logger.LogDebug("LoadAllHerbsAsync: {Elapsed}ms", sw.ElapsedMilliseconds);

        sw.Restart();
        await _dataManager.InitializeAsync(MedicalCaseId);
        Logger.LogDebug("InitializeAsync: {Elapsed}ms", sw.ElapsedMilliseconds);

        sw.Restart();
        RefreshItemRows();
        Logger.LogDebug("RefreshItemRows: {Elapsed}ms", sw.ElapsedMilliseconds);

        sw.Stop();
        Logger.LogInformation("处方编写器初始化完成，总耗时: {TotalElapsed}ms", sw.ElapsedMilliseconds);
    }
    finally
    {
        SetIsBusy(false);
    }
}
```

#### Visual Studio诊断工具

1. **调试 → 性能探查器**
2. 选择 **CPU使用率** 和 **内存使用率**
3. 启动应用，操作处方模块
4. 停止分析，查看热点函数

---

## 📝 12. 完整示例

### 12.1 最小可用示例

#### PrescriptionViewMinimal.xaml

```xml
<UserControl x:Class="LYBT.Desktop.Prescriptions.Views.PrescriptionViewMinimal"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:prism="http://prismlibrary.com/">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- 头部信息 -->
        <Grid Grid.Row="0" Margin="10">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>

            <TextBlock Grid.Column="0" Text="{Binding PatientInfo}" FontSize="14" />
            <TextBlock Grid.Column="1" Text="{Binding DoctorInfo}" FontSize="14" HorizontalAlignment="Right" />
        </Grid>

        <!-- 8列DataGrid -->
        <DataGrid
            Grid.Row="1"
            ItemsSource="{Binding ItemRows}"
            AutoGenerateColumns="False"
            Margin="10">
            <DataGrid.Columns>
                <!-- 第1个药材项 -->
                <DataGridTextColumn Header="药材1" Binding="{Binding Item1.HerbName}" Width="*" />
                <DataGridTextColumn Header="用量1" Binding="{Binding Item1.QuantityDisplay}" Width="80" />

                <!-- 第2个药材项 -->
                <DataGridTextColumn Header="药材2" Binding="{Binding Item2.HerbName}" Width="*" />
                <DataGridTextColumn Header="用量2" Binding="{Binding Item2.QuantityDisplay}" Width="80" />

                <!-- 第3个药材项 -->
                <DataGridTextColumn Header="药材3" Binding="{Binding Item3.HerbName}" Width="*" />
                <DataGridTextColumn Header="用量3" Binding="{Binding Item3.QuantityDisplay}" Width="80" />

                <!-- 第4个药材项 -->
                <DataGridTextColumn Header="药材4" Binding="{Binding Item4.HerbName}" Width="*" />
                <DataGridTextColumn Header="用量4" Binding="{Binding Item4.QuantityDisplay}" Width="80" />
            </DataGrid.Columns>
        </DataGrid>

        <!-- 底部按钮 -->
        <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Right" Margin="10">
            <Button Content="添加药材" Command="{Binding AddHerbCommand}" Width="100" Margin="0,0,10,0" />
            <Button Content="保存处方" Command="{Binding SaveCommand}" Width="100" Margin="0,0,10,0" />
            <Button Content="清空" Command="{Binding ClearCommand}" Width="100" />
        </StackPanel>
    </Grid>
</UserControl>
```

#### PrescriptionViewModelMinimal.cs

```csharp
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Modules.Prescriptions.ViewModels.Components;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Prescriptions.ViewModels
{
    public class PrescriptionViewModelMinimal : UnifiedViewModelBase
    {
        private readonly PrescriptionDataManager _dataManager;
        private readonly PrescriptionCalculator _calculator;

        public Guid MedicalCaseId { get; set; }
        public string PatientInfo { get; set; } = "患者: 张三 | 性别: 男 | 年龄: 35岁";
        public string DoctorInfo { get; set; } = "医生: 李医生 | 科室: 内科";

        public ObservableCollection<PrescriptionItemViewModel> PrescriptionItems => _dataManager.PrescriptionItems;
        public ObservableCollection<PrescriptionItemRow> ItemRows { get; set; } = new();

        public DelegateCommand AddHerbCommand { get; }
        public DelegateCommand SaveCommand { get; }
        public DelegateCommand ClearCommand { get; }

        public PrescriptionViewModelMinimal(
            PrescriptionDataManager dataManager,
            PrescriptionCalculator calculator,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager)
            : base(eventAggregator, loggerFactory, regionManager)
        {
            _dataManager = dataManager;
            _calculator = calculator;

            AddHerbCommand = new DelegateCommand(ExecuteAddHerb);
            SaveCommand = new DelegateCommand(ExecuteSave);
            ClearCommand = new DelegateCommand(ExecuteClear);

            // 订阅集合变化事件
            PrescriptionItems.CollectionChanged += (s, e) => RefreshItemRows();
        }

        private void ExecuteAddHerb()
        {
            // 模拟添加药材
            var newItem = new PrescriptionItemViewModel(EventAggregator, LoggerFactory, RegionManager)
            {
                HerbName = "当归",
                Quantity = 10,
                Unit = "g",
                UnitPrice = 0.5m
            };
            PrescriptionItems.Add(newItem);
        }

        private void ExecuteSave()
        {
            ShowInfoMessage("保存成功");
        }

        private void ExecuteClear()
        {
            _dataManager.Clear();
        }

        private void RefreshItemRows()
        {
            ItemRows.Clear();

            for (int i = 0; i < PrescriptionItems.Count; i += 4)
            {
                var row = new PrescriptionItemRow
                {
                    Item1 = i < PrescriptionItems.Count ? PrescriptionItems[i] : null,
                    Item2 = i + 1 < PrescriptionItems.Count ? PrescriptionItems[i + 1] : null,
                    Item3 = i + 2 < PrescriptionItems.Count ? PrescriptionItems[i + 2] : null,
                    Item4 = i + 3 < PrescriptionItems.Count ? PrescriptionItems[i + 3] : null
                };
                ItemRows.Add(row);
            }
        }
    }
}
```

---

## 📚 13. 相关资源

### 13.1 内部文档

**架构文档**：
- [Client端处方管理架构设计](../../explanation/architecture/client/prescriptions-design.md) - 完整架构文档
- [Client端MVVM架构指南](../../explanation/architecture/client/README.md) - 五层架构规范
- [Server端处方管理架构设计](../../explanation/architecture/server/prescriptions-design.md) - API端点契约

**架构决策（ADR）**：
- **ADR-008**: [Desktop端不独立实现Repository](../../explanation/architecture/decisions/ADR-008-desktop-consultation-prescription-no-independent-repository.md) - 完全删除空接口桩（2025-11-02）
- **ADR-003**: [Repository层简化](../../explanation/architecture/decisions/ADR-003-repository-simplification.md) - Desktop端初步简化决策

**开发指南**：
- [Client端患者管理开发指南](./patients-development.md) - MVVM开发规范参考
- [Client端诊疗管理开发指南](./consultation-development.md) - Three-Step工作流参考
- [Server端处方管理开发指南](../server/prescriptions-development.md) - API端点开发

**业务规则**：
- **REQ-002**: 处方数据完整性验证（中药名称+剂量必填）
- **Issue #1606**: Prescription聚合根整合（历史参考）

**快速参考**：
- [代码模式](../../quick-reference/code-patterns.md) - WPF常用模式
- [API参考](../../quick-reference/api-reference.md) - IPrescriptionApi端点
- [配置模板](../../quick-reference/config-templates.md) - Prism模块注册

**相关Issue**：
- **Issue #1769**: ADR-008架构决策实施

---

### 13.2 外部资源

**Prism框架**：
- [Prism官方文档](https://prismlibrary.com/docs/)
- [Prism GitHub仓库](https://github.com/PrismLibrary/Prism)

**WPF性能优化**：
- [WPF性能最佳实践](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/advanced/optimizing-performance-data-binding)
- [DataGrid虚拟化](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/controls/how-to-improve-the-performance-of-a-treeview)

---

## 📝 14. 版本历史

| 版本 | 日期 | 修改内容 | 负责人 |
|------|------|---------|--------|
| v1.0 | 2025-10-30 | 初始版本，包含完整开发指南 | Client端开发组 |
| v1.1 | 2025-11-02 | 补充ADR-008引用，强调Desktop端不实现Repository的YAGNI原则 | Client端开发组 |

---

## 📞 15. 技术支持

**遇到问题？**
1. 查阅本文档的"常见问题"章节
2. 查看[Client端处方管理架构设计](../../explanation/architecture/client/prescriptions-design.md)
3. 联系Client端开发组技术支持

**反馈渠道**：
- GitHub Issues: [LYBTZYZS Issues](https://github.com/shouqitao/LYBTZYZS/issues)
- 内部Wiki: `docs/development/support.md`

---

**最后更新**: 2025-11-02
**维护负责**: Client端开发组
**文档状态**: ✅ 完整版（v1.1 - ADR-008适配）
