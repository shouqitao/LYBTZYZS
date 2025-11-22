# Client端处方管理架构设计文档

## 文档版本

| 版本 | 日期 | 作者 | 变更说明 |
|------|------|------|----------|
| v1.0.0 | 2025-10-30 | Claude Code | 初始版本 - 完整架构设计 |

---

## 1. 模块概述

### 1.1 模块定位

处方管理模块(LYBT.Desktop.Prescriptions)是凌隐宝堂中医诊疗系统Client端的**Step2核心模块**,负责提供完整的处方编写、管理和打印功能。本模块作为"看诊三步流程"中的第二步(施治),承上启下:

- **上游**:接收Consultation模块(Step1:辩证)的诊断结果
- **下游**:生成完整的处方数据,供打印和归档

**核心定位**:
- 🏥 **业务核心**:中医处方的完整数字化实现,支持8列DataGrid布局、验方导入、历史处方复制等功能
- 🔄 **三步工作流的枢纽**:连接诊断(Step1)和总结(Step3),承载核心施治逻辑
- 📦 **聚合根约束**:遵循Issue #1606架构调整,所有Write操作通过MedicalCaseRepository聚合根
- 🎨 **WPF MVVM实现**:严格遵循Prism框架MVVM模式和Client端五层架构

### 1.2 核心职责

**数据管理职责**:
- ✅ 处方数据的本地管理和状态跟踪(PrescriptionDataManager)
- ✅ 处方项目的增删改查(8列DataGrid布局支持)
- ✅ 药材选择和用量配置(ComboBox拼音码过滤)
- ✅ 验方模板导入和历史处方复制

**业务逻辑职责**:
- ✅ 处方价格计算(单剂价格、总价、折扣价、节省金额)
- ✅ 处方数据验证(必填项、用量安全、配伍禁忌、重复药材)
- ✅ 用法用量模板管理(CommonUsages常量)
- ✅ 处方编号生成(Client端临时编号 + Server端自动编号)

**UI交互职责**:
- ✅ 处方编写界面(PrescriptionView)
- ✅ 处方管理界面(PrescriptionsMainView, PrescriptionManagementView)
- ✅ 药材选择对话框(HerbSelectionDialog)
- ✅ 验方选择对话框(SelectFormulaDialog, FormulaTemplateDialog)

**打印导出职责**:
- ✅ 处方打印预览和打印(IPrescriptionPrintService)
- ✅ 处方PDF导出
- ✅ 批量打印支持

### 1.3 架构重大变更 (Issue #1606 Phase 3 → ADR-008)

**核心变更**:所有操作从IPrescriptionRepository迁移至IMedicalCaseRepository聚合根

> ⚠️ **最新更新（2025-11-02 - ADR-008）**
> Desktop端已**完全删除**`IPrescriptionRepository`接口（不再是Obsolete空接口桩）。
> 详见：[ADR-008: Desktop端Consultation/Prescription不独立实现Repository](../decisions/ADR-008-desktop-consultation-prescription-no-independent-repository.md)

```csharp
// ❌ 旧方式 (Issue #1606前)
public class PrescriptionModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册Repository（已删除）
        containerRegistry.RegisterSingleton<IPrescriptionRepository, PrescriptionRepository>();
    }
}

// ✅ 新方式 (Issue #1606 Phase 3)
public class PrescriptionsModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // Issue #1606 Phase 3: IPrescriptionRepository已删除
        // 所有Write操作通过MedicalCaseRepository聚合根

        // 保留打印服务
        containerRegistry.RegisterSingleton<IPrescriptionPrintService, PrescriptionPrintService>();

        // 保留编辑器服务（依赖倒置）
        containerRegistry.RegisterSingleton<IPrescriptionEditorService, PrescriptionEditorService>();

        // ⚠️ 多个ViewModel临时注释（待Issue #1608重构）
        // containerRegistry.Register<PrescriptionViewModel>();
        // containerRegistry.Register<PrescriptionManagementViewModel>();
    }
}
```

**影响范围**:
- **IPrescriptionRepository** → ~~Obsolete空接口桩(Issue #1606)~~ → **完全删除**(ADR-008, 2025-11-02)
- **PrescriptionDataManager** → 使用IPrescriptionApi和IMedicalCaseRepository
- **6个ViewModel待重构** → 临时注释DI注册(Issue #1608)

**设计原因**:
- 🏗️ **聚合根一致性**:确保Prescription作为MedicalCase的一部分,维护聚合根边界
- 🔒 **数据一致性**:避免直接操作Prescription绕过MedicalCase验证
- 📐 **架构清晰**:明确Write职责归属,防止职责分散

---

## 2. 模块架构

### 2.1 架构图

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        LYBT.Desktop.Prescriptions                       │
│                         处方管理模块(Client端)                           │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┼───────────────┐
                    │               │               │
            ┌───────▼──────┐ ┌─────▼─────┐ ┌──────▼──────┐
            │  Views       │ │ ViewModels│ │  Services   │
            │  (XAML/UI)   │ │  (MVVM)   │ │  (打印/编辑)│
            └───────┬──────┘ └─────┬─────┘ └──────┬──────┘
                    │               │               │
        ┌───────────┼───────────────┼───────────────┼──────────┐
        │           │               │               │          │
    ┌───▼───┐  ┌───▼────┐   ┌──────▼──────┐  ┌────▼─────┐  ┌▼─────┐
    │Prescri│  │Herb    │   │Prescription │  │Prescrip  │  │Print │
    │ption  │  │Selectio│   │Management   │  │tionEditor│  │Servic│
    │View   │  │nDialog │   │ViewModel    │  │Dialog    │  │e     │
    └───┬───┘  └───┬────┘   └──────┬──────┘  └────┬─────┘  └┬─────┘
        │          │                │               │         │
        └──────────┴────────────────┴───────────────┴─────────┘
                                    │
                    ┌───────────────┼───────────────┐
                    │               │               │
            ┌───────▼──────┐ ┌─────▼─────┐ ┌──────▼──────┐
            │  Components  │ │  Models   │ │  Constants  │
            │  (5个组件)    │ │  (UI模型) │ │  (常量配置)  │
            └───────┬──────┘ └─────┬─────┘ └──────┬──────┘
                    │               │               │
    ┌───────────────┼───────────────┼───────────────┼─────────────┐
    │               │               │               │             │
┌───▼────┐  ┌───────▼───┐  ┌──────▼──────┐  ┌─────▼──────┐  ┌──▼───┐
│Data    │  │Calculator │  │Validator    │  │Prescription│  │Usage │
│Manager │  │(价格计算)  │  │(数据验证)    │  │Item        │  │模板  │
└───┬────┘  └───────┬───┘  └──────┬──────┘  │(UI模型)    │  └──────┘
    │               │               │         └────────────┘
    └───────────────┴───────────────┘
                    │
        ┌───────────┴───────────────────────────────┐
        │           外部依赖接口                      │
        ├───────────────────────────────────────────┤
        │  • IPrescriptionApi (WebAPI调用)          │
        │  • IMedicalCaseRepository (聚合根操作)   │
        │  • IHerbRepository (药材数据)             │
        │  • IEventAggregator (Prism事件)           │
        │  • IRegionManager (导航)                  │
        │  • ISessionManager (会话)                 │
        └───────────────────────────────────────────┘
```

### 2.2 代码结构

```
LYBT.Desktop.Prescriptions/
├── PrescriptionsModule.cs              # Prism模块注册(DI配置)
├── Constants/
│   └── PrescriptionConstants.cs        # 常量定义(用法模板、默认值、验证规则)
├── Models/
│   ├── PrescriptionItem.cs             # UI数据模型(21属性)
│   │   └── PrescriptionHerbItem        # 药材子项(9属性)
│   └── PrescriptionPrintDto.cs         # 打印DTO
├── ViewModels/
│   ├── PrescriptionViewModel.cs        # 处方编写ViewModel(985行, 13个命令)
│   ├── PrescriptionsMainViewModel.cs   # ⚠️ 临时注释(Issue #1608)
│   ├── PrescriptionManagementViewModel.cs # ⚠️ 临时注释(Issue #1608)
│   ├── PrescriptionEditorDialogViewModel.cs # ⚠️ 临时注释(Issue #1608)
│   ├── HerbSelectionDialogViewModel.cs # 药材选择对话框
│   ├── SelectFormulaDialogViewModel.cs # 验方选择对话框
│   ├── FormulaTemplateDialogViewModel.cs # 验方模板对话框
│   ├── PrescriptionItemViewModel.cs    # 处方项ViewModel
│   ├── PrescriptionItemRow.cs          # 8列布局行模型
│   └── Components/                     # 5个专门化组件
│       ├── PrescriptionDataManager.cs  # 数据CRUD和状态管理(337行)
│       ├── PrescriptionCalculator.cs   # 价格计算(继承HerbCalculatorBase)
│       ├── PrescriptionValidator.cs    # 数据验证(继承HerbValidatorBase)
│       ├── PrescriptionCommandHandler.cs # 命令处理
│       └── PrescriptionEventCoordinator.cs # 事件协调
├── Views/
│   ├── PrescriptionView.xaml           # 处方编写视图
│   ├── PrescriptionsMainView.xaml      # 主页视图
│   ├── PrescriptionManagementView.xaml # 管理视图
│   ├── HerbSelectionDialog.xaml        # 药材选择对话框
│   ├── SelectFormulaDialog.xaml        # 验方选择对话框
│   ├── FormulaTemplateDialog.xaml      # 验方模板对话框
│   └── PrescriptionEditorDialog.xaml   # 处方编辑对话框
├── Services/
│   ├── IPrescriptionPrintService.cs    # 打印服务接口
│   ├── PrescriptionPrintService.cs     # 打印服务实现
│   ├── PrescriptionEditorService.cs    # 编辑器服务(方案B - 包装模式)
│   └── PrescriptionFlowDocumentBuilder.cs # FlowDocument构建器
├── Components/
│   ├── BasicValidator.cs               # 基础验证器
│   └── PriceCalculator.cs              # 价格计算器
└── Interfaces/
    └── (空目录 - IPrescriptionRepository已删除, ADR-008)
```

### 2.3 依赖关系

**核心依赖**:

```csharp
public class PrescriptionViewModel : UnifiedViewModelBase
{
    // 外部API依赖
    private readonly IPrescriptionApi _prescriptionApi;               // WebAPI调用
    private readonly IMedicalCaseRepository _medicalCaseRepository;   // 聚合根操作(Write)
    private readonly IHerbRepository _herbRepository;                 // 药材数据(搜索)

    // 组件依赖(5个专门化组件)
    private readonly PrescriptionDataManager _dataManager;            // 数据管理
    private readonly PrescriptionCalculator _calculator;              // 价格计算
    private readonly PrescriptionValidator _validator;                // 数据验证
    private readonly PrescriptionCommandHandler _commandHandler;      // 命令处理
    private readonly PrescriptionEventCoordinator _eventCoordinator;  // 事件协调

    // 基类依赖(来自UnifiedViewModelBase)
    // - IEventAggregator: Prism事件
    // - ILoggerFactory: 日志
    // - IRegionManager: 导航
    // - ISessionManager: 会话管理
    // - IUserNotificationService: 消息通知
}
```

**依赖方向**:
```
Views → ViewModels → Components → Models → Interfaces
  ↓         ↓            ↓
Services  ←─┘            ↓
  ↓                      ↓
Shared.Models ←──────────┘
  ↓
Contracts/Prescriptions (DTOs)
```

**关键约束**:
- ✅ **聚合根约束**:Write操作必须通过IMedicalCaseRepository
- ✅ **依赖倒置**:IPrescriptionPrintService接口定义在Services层,实现在Desktop层
- ✅ **跨模块依赖**:依赖Herbs模块(IHerbRepository)、MedicalCase模块(IMedicalCaseRepository)

---

## 3. 数据模型

### 3.1 PrescriptionItem (UI数据模型)

**设计目标**:Desktop层与Shared层解耦的UI专用模型

```csharp
/// <summary>
/// 处方列表项UI模型 - 用于DataGrid/ListView显示
/// 替代直接使用PrescriptionDto,实现Desktop层与Shared层的解耦
/// 保持属性名与PrescriptionDto一致,确保XAML绑定兼容
/// </summary>
public class PrescriptionItem : BindableBase
{
    // 基础信息(5个)
    public Guid Id { get; set; }
    public string PrescriptionNumber { get; set; }         // RX-YYYYMMDD-NNNN格式
    public Guid PatientId { get; set; }
    public string PatientName { get; set; }
    public string? PatientGender { get; set; }
    public int? PatientAge { get; set; }

    // 关联ID(2个)
    public Guid? MedicalCaseId { get; set; }
    public Guid? ConsultationId { get; set; }

    // 诊断信息(3个)
    public string? Diagnosis { get; set; }                 // 诊断
    public string? Syndrome { get; set; }                  // 证型
    public string? TreatmentPrinciple { get; set; }        // 治则

    // 用法用量(4个)
    public int Doses { get; set; } = 1;                    // 剂数
    public string? Usage { get; set; }                     // 用法
    public string? Frequency { get; set; }                 // 频次
    public string? Note { get; set; }                      // 备注

    // 价格信息(2个)
    public decimal TotalAmount { get; set; }
    public PrescriptionStatus Status { get; set; }

    // 人员信息(2个)
    public string? DoctorName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DispensedAt { get; set; }             // 配药时间
    public string? DispensedBy { get; set; }               // 配药人

    // 药材列表
    public ObservableCollection<PrescriptionHerbItem> Herbs { get; set; }

    // UI状态(3个)
    public bool IsSelected { get; set; }
    public bool IsExpanded { get; set; }
    public bool IsPrinted { get; set; }

    // ========== 计算属性 ==========

    /// <summary>状态显示文本</summary>
    public string StatusText => Status switch
    {
        PrescriptionStatus.Draft => "草稿",
        PrescriptionStatus.Completed => "已完成",
        _ => "未知"
    };

    /// <summary>状态颜色</summary>
    public string StatusColor => Status switch
    {
        PrescriptionStatus.Draft => "#9E9E9E",
        PrescriptionStatus.Completed => "#4CAF50",
        _ => "#757575"
    };

    /// <summary>药材数量</summary>
    public int HerbCount => Herbs?.Count ?? 0;

    /// <summary>单剂金额</summary>
    public decimal SingleDoseAmount => Doses > 0 ? TotalAmount / Doses : 0;

    /// <summary>是否可编辑</summary>
    public bool CanEdit => Status == PrescriptionStatus.Draft;

    /// <summary>处方组成简述</summary>
    public string CompositionSummary
    {
        get
        {
            if (Herbs == null || Herbs.Count == 0)
                return "暂无药材";

            var mainHerbs = Herbs.Take(3).Select(h => h.HerbName);
            var summary = string.Join("、", mainHerbs);

            if (Herbs.Count > 3)
                summary += $" 等{HerbCount}味";

            return summary;
        }
    }

    /// <summary>用法用量文本</summary>
    public string UsageText
    {
        get
        {
            var text = $"{Doses}剂";
            if (!string.IsNullOrWhiteSpace(Usage))
                text += $"，{Usage}";
            if (!string.IsNullOrWhiteSpace(Frequency))
                text += $"，{Frequency}";
            return text;
        }
    }

    /// <summary>金额显示文本</summary>
    public string AmountText => $"¥{TotalAmount:F2}";

    // ========== DTO转换 ==========

    /// <summary>从PrescriptionDetailDto创建PrescriptionItem</summary>
    public static PrescriptionItem FromDto(PrescriptionDetailDto dto) { /* ... */ }

    /// <summary>转换为PrescriptionDetailDto(用于API调用)</summary>
    public PrescriptionDetailDto ToDto() { /* ... */ }
}
```

### 3.2 PrescriptionHerbItem (药材子项)

```csharp
/// <summary>
/// 处方中的药材项
/// </summary>
public class PrescriptionHerbItem : BindableBase
{
    public Guid HerbId { get; set; }          // 药材ID
    public string HerbName { get; set; }      // 药材名称
    public decimal Dosage { get; set; }       // 用量
    public string Unit { get; set; }          // 单位
    public decimal UnitPrice { get; set; }    // 单价
    public string? Usage { get; set; }        // 特殊用法
    public int Sequence { get; set; }         // 序号
    public decimal Subtotal { get; set; }     // 小计
    public bool IsSelected { get; set; }      // 选中状态

    // 显示属性
    public string DisplayText => $"{HerbName} {Dosage}{Unit}" +
                                 (string.IsNullOrWhiteSpace(Usage) ? "" : $"({Usage})");
    public string PriceText => $"¥{UnitPrice:F2}/{Unit}";
    public string SubtotalText => $"¥{Subtotal:F2}";

    /// <summary>计算小计</summary>
    public void CalculateSubtotal()
    {
        Subtotal = Dosage * UnitPrice;
    }
}
```

### 3.3 PrescriptionItemRow (8列布局行)

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

// 转换逻辑
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

---

## 4. ViewModel设计

### 4.1 PrescriptionViewModel (处方编写)

**核心职责**:处方编写界面的完整业务逻辑实现

```csharp
/// <summary>
/// 处方视图模型 - 统一架构实现
/// Issue #1445 (ARCH-2): 已废弃Phase 4B骨架,统一到PrescriptionComposerView重命名版本
/// 完整实现包含: 8列DataGrid、验方导入、价格计算、历史复制等功能
/// </summary>
public class PrescriptionViewModel : UnifiedViewModelBase
{
    // ========== 数据属性 ==========

    public Guid MedicalCaseId { get; set; }                          // 医疗案例ID
    public MedicalCaseDto? CurrentMedicalCase { get; set; }          // 当前医疗案例
    public string PatientInfo { get; set; }                          // 患者信息
    public string DoctorInfo { get; set; }                           // 医生信息

    // 处方数据
    public ObservableCollection<PrescriptionItemViewModel> PrescriptionItems { get; }  // 处方项
    public ObservableCollection<PrescriptionItemRow> ItemRows { get; set; }            // 8列布局行
    public PrescriptionItemViewModel? SelectedItem { get; set; }                       // 选中项

    // 处方信息
    public string PrescriptionNo { get; set; }                       // 临时编号(Client生成)
    public string? PrescriptionNumber { get; }                       // 正式编号(Server生成,RX-YYYYMMDD-NNNN)
    public int DosageCount { get; set; } = 7;                        // 剂数
    public string Usage { get; set; } = "水煎服,一日三次,饭后服用"; // 用法
    public string MedicalAdvice { get; set; }                        // 医嘱
    public string Remark { get; set; }                               // 备注
    public decimal Discount { get; set; } = 1.0m;                    // 折扣

    // 药材过滤 (Issue #1362: ComboBox拼音码过滤)
    public List<HerbDto> AllHerbs { get; set; }                     // 所有药材
    public ObservableCollection<HerbDto> FilteredHerbs { get; set; } // 过滤结果

    // 历史处方 (Issue #1374 ENTRY-16)
    public ObservableCollection<PrescriptionSearchResultDto> RecentPrescriptions { get; set; }
    public PrescriptionSearchResultDto? SelectedRecentPrescription { get; set; }

    // 价格计算
    public PrescriptionCalculator.CalculationResult? CalculationResult { get; set; }
    public decimal SingleDosagePrice => CalculationResult?.SingleDosagePrice ?? 0m;
    public decimal TotalPrice => CalculationResult?.TotalPrice ?? 0m;
    public decimal DiscountedPrice => CalculationResult?.DiscountedPrice ?? 0m;
    public decimal TotalSaved => CalculationResult?.TotalSaved ?? 0m;
    public int ItemCount => PrescriptionItems?.Count ?? 0;

    // ========== 13个命令 ==========

    public DelegateCommand SaveCommand { get; }                      // 保存处方
    public DelegateCommand ClearCommand { get; }                     // 清空处方
    public DelegateCommand AddHerbCommand { get; }                   // 添加药材
    public DelegateCommand<PrescriptionItemViewModel> RemoveHerbCommand { get; }  // 移除药材
    public DelegateCommand ImportFormulaCommand { get; }             // 导入验方
    public DelegateCommand GeneratePrescriptionNoCommand { get; }    // 生成处方编号
    public DelegateCommand ValidateCommand { get; }                  // 验证处方
    public DelegateCommand RecalculateCommand { get; }               // 重新计算
    public DelegateCommand PrintPreviewCommand { get; }              // 打印预览
    public DelegateCommand BackCommand { get; }                      // 返回
    public DelegateCommand SaveDraftCommand { get; }                 // 保存草稿
    public DelegateCommand<PrescriptionItemViewModel> EditHerbCommand { get; }  // 编辑药材
    public DelegateCommand<PrescriptionSearchResultDto> CopyFromHistoryCommand { get; }  // 复制历史处方

    // ========== 生命周期 ==========

    protected override async Task InitializeAsync(NavigationParameters parameters)
    {
        await base.InitializeAsync(parameters);

        // 获取MedicalCaseId参数
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

        // Step 1: 加载医疗案例信息
        await LoadMedicalCaseAsync();

        // Step 2: 加载所有药材 (ComboBox过滤)
        await LoadAllHerbsAsync();

        // Step 3: 加载患者历史处方
        await LoadRecentPrescriptionsAsync();

        // Step 4: 初始化数据管理器
        await _dataManager.InitializeAsync(MedicalCaseId);

        // Step 5: 初始计算
        RecalculatePrice();

        // Step 6: 初始化8列布局
        RefreshItemRows();

        SetIsBusy(false);
    }

    // ========== 核心方法 ==========

    /// <summary>
    /// 药材过滤 (Issue #1362: ComboBox拼音码过滤)
    /// </summary>
    public void FilterHerbs(string searchText)
    {
        FilteredHerbs.Clear();

        if (string.IsNullOrWhiteSpace(searchText))
            return;

        // 匹配药材名称或拼音码(不区分大小写)
        var filtered = AllHerbs
            .Where(h => h.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                       (h.PinYinCode?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false))
            .Take(5)  // 限制最多5个结果
            .ToList();

        foreach (var herb in filtered)
        {
            FilteredHerbs.Add(herb);
        }
    }

    /// <summary>
    /// 重新计算价格
    /// </summary>
    private void RecalculatePrice()
    {
        CalculationResult = _calculator.CalculatePrescriptionPrice(
            PrescriptionItems,
            DosageCount,
            Discount);

        // 通知价格相关属性变更
        RaisePropertyChanged(nameof(TotalPrice));
        RaisePropertyChanged(nameof(DiscountedPrice));
        RaisePropertyChanged(nameof(ItemCount));
    }

    /// <summary>
    /// 刷新8列布局行 (Issue #1360)
    /// </summary>
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

    /// <summary>
    /// 从历史处方复制 (Issue #1374 ENTRY-16)
    /// </summary>
    private void ExecuteCopyFromHistory(PrescriptionSearchResultDto prescription)
    {
        if (prescription == null) return;

        // 清空当前处方项
        _dataManager.Clear();

        // 复制处方项
        foreach (var item in prescription.Items)
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

        // 重新计算价格
        RecalculatePrice();

        // 刷新ItemRows
        RefreshItemRows();

        ShowInfoMessage($"已从历史处方复制 {prescription.Items.Count} 味药材");
    }
}
```

### 4.2 ViewModel职责矩阵

| ViewModel | 职责 | 状态 | 说明 |
|-----------|------|------|------|
| **PrescriptionViewModel** | 处方编写 | ✅ 可用 | 完整实现,985行代码 |
| **PrescriptionsMainViewModel** | 主页统计 | ⚠️ 待重构 | Issue #1608,临时注释DI |
| **PrescriptionManagementViewModel** | 处方管理 | ⚠️ 待重构 | Issue #1608,临时注释DI |
| **PrescriptionEditorDialogViewModel** | 处方编辑对话框 | ⚠️ 待重构 | Issue #1608,临时注释DI |
| **HerbSelectionDialogViewModel** | 药材选择对话框 | ✅ 可用 | 支持拼音码搜索 |
| **SelectFormulaDialogViewModel** | 验方选择对话框 | ✅ 可用 | 支持分类浏览 |
| **FormulaTemplateDialogViewModel** | 验方模板对话框 | ✅ 可用 | 支持模板管理 |
| **PrescriptionItemViewModel** | 处方项 | ✅ 可用 | 单个药材项 |
| **PrescriptionItemRow** | 8列布局行 | ✅ 可用 | 4个项目/行 |

---

## 5. Component组件设计

### 5.1 组件架构

```
┌─────────────────────────────────────────────────────────┐
│           PrescriptionViewModel (主ViewModel)            │
└───────────┬──────────────────────────────────────┬──────┘
            │                                      │
    ┌───────▼────────┐                    ┌───────▼──────┐
    │ 5个Component组件│                    │ 外部依赖接口  │
    └───────┬────────┘                    └───────┬──────┘
            │                                      │
┌───────────┴─────────────────────┐               │
│                                 │               │
▼                                 ▼               ▼
┌──────────────────┐  ┌──────────────────┐  ┌────────────┐
│ DataManager      │  │ Calculator       │  │ Validator  │
│ (数据管理)        │  │ (价格计算)        │  │ (数据验证)  │
│                  │  │                  │  │            │
│ • SaveAsync      │  │ • Calculate      │  │ • Validate │
│ • LoadAsync      │  │ • Analyze        │  │ • Check    │
│ • Clear          │  │                  │  │            │
└──────────────────┘  └──────────────────┘  └────────────┘
         │                      │                   │
         └──────────────────────┴───────────────────┘
                                │
                    ┌───────────┴───────────┐
                    ▼                       ▼
        ┌──────────────────┐    ┌──────────────────┐
        │ CommandHandler   │    │ EventCoordinator │
        │ (命令处理)        │    │ (事件协调)        │
        │                  │    │                  │
        │ • 13个Command    │    │ • Subscribe      │
        │ • CanExecute     │    │ • Publish        │
        └──────────────────┘    └──────────────────┘
```

### 5.2 PrescriptionDataManager (数据管理组件)

**职责**:处方数据的CRUD操作和状态管理

```csharp
/// <summary>
/// 处方数据管理器 - UltraThink专门化组件
/// 职责单一: 专注处方数据的CRUD操作和状态管理
/// 代码干净: 清晰的数据管理接口
/// 性能出色: 优化的数据加载和缓存策略
/// </summary>
public class PrescriptionDataManager
{
    private readonly IPrescriptionApi _prescriptionApi;
    private readonly IMedicalCaseRepository _medicalCaseRepository;

    // ========== 核心数据属性 ==========

    public Guid MedicalCaseId { get; private set; }
    public Guid PrescriptionId { get; private set; }
    public PrescriptionDto? CurrentPrescription { get; private set; }
    public bool IsNewPrescription { get; private set; } = true;
    public string? PrescriptionNumber { get; private set; }    // Server生成(RX-YYYYMMDD-NNNN)
    public string PrescriptionNo { get; set; }                 // Client生成(临时)

    public ObservableCollection<PrescriptionItemViewModel> PrescriptionItems { get; }
    public PrescriptionItemViewModel? SelectedItem { get; set; }
    public int DosageCount { get; set; } = 7;
    public string Usage { get; set; } = "水煎服,一日三次,饭后服用";
    public string MedicalAdvice { get; set; }
    public string Remark { get; set; }
    public decimal Discount { get; set; } = 1.0m;

    public bool IsLoading { get; private set; }
    public bool HasChanges { get; private set; }

    // ========== 数据初始化 ==========

    /// <summary>
    /// 初始化处方数据
    /// </summary>
    public async Task InitializeAsync(Guid medicalCaseId)
    {
        IsLoading = true;
        MedicalCaseId = medicalCaseId;

        // 生成临时处方编号
        GeneratePrescriptionNo();

        // 加载现有数据
        await LoadExistingDataAsync();

        HasChanges = false;
        IsLoading = false;
    }

    /// <summary>
    /// 加载现有处方数据
    /// Issue #1608: 使用IPrescriptionApi替代IPrescriptionRepository
    /// </summary>
    private async Task LoadExistingDataAsync()
    {
        // 调用API获取处方数据
        var response = await _prescriptionApi.GetPrescriptionsByMedicalCaseIdAsync(MedicalCaseId);
        var prescriptions = response.Data ?? new List<PrescriptionDto>();

        if (prescriptions != null && prescriptions.Any())
        {
            var existingPrescription = prescriptions.First();
            CurrentPrescription = existingPrescription;
            PrescriptionId = existingPrescription.Id;
            IsNewPrescription = false;

            // 加载基础信息
            DosageCount = existingPrescription.DosageCount;
            Usage = "水煎服,一日三次,饭后服用"; // 默认值
            MedicalAdvice = existingPrescription.Advice ?? string.Empty;
            Remark = existingPrescription.Remark ?? string.Empty;
            Discount = existingPrescription.Discount;

            // Issue #1551: 加载服务端生成的处方编号
            PrescriptionNumber = existingPrescription.PrescriptionNumber;

            // 加载处方项
            PrescriptionItems.Clear();
            foreach (var item in existingPrescription.Items)
            {
                var viewModel = new PrescriptionItemViewModel(...)
                {
                    HerbId = item.HerbId,
                    HerbName = item.HerbName,
                    Quantity = item.Quantity,
                    Unit = item.Unit,
                    UnitPrice = item.UnitPrice,
                    Remark = item.Remark ?? string.Empty
                };
                PrescriptionItems.Add(viewModel);
            }
        }
        else
        {
            ResetToDefault();
        }
    }

    // ========== 数据操作 ==========

    /// <summary>
    /// 保存处方数据
    /// Issue #1608: 使用IMedicalCaseRepository.CreatePrescriptionAsync替代IPrescriptionRepository
    /// </summary>
    public async Task<bool> SaveAsync()
    {
        if (PrescriptionItems.Count == 0)
        {
            _logger.LogWarning("处方项为空,无法保存");
            return false;
        }

        IsLoading = true;

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

            return true;
        }

        IsLoading = false;
        return false;
    }

    /// <summary>清空数据</summary>
    public void Clear()
    {
        PrescriptionItems.Clear();
        ResetToDefault();
        HasChanges = true;
    }

    /// <summary>添加处方项</summary>
    public void AddPrescriptionItem(PrescriptionItemViewModel item)
    {
        ArgumentNullException.ThrowIfNull(item);
        PrescriptionItems.Add(item);
        HasChanges = true;
    }

    /// <summary>移除处方项</summary>
    public void RemovePrescriptionItem(PrescriptionItemViewModel? item)
    {
        if (item != null && PrescriptionItems.Contains(item))
        {
            PrescriptionItems.Remove(item);
            HasChanges = true;
        }
    }

    /// <summary>标记数据已变更</summary>
    public void MarkAsChanged()
    {
        HasChanges = true;
    }

    // ========== 私有辅助方法 ==========

    private void ResetToDefault()
    {
        Usage = "水煎服,一日三次,饭后服用";
        MedicalAdvice = string.Empty;
        Remark = string.Empty;
        DosageCount = 7;
        Discount = 1.0m;
        GeneratePrescriptionNo();
    }

    public void GeneratePrescriptionNo()
    {
        PrescriptionNo = $"CF{DateTime.Now:yyyyMMddHHmmss}";
        HasChanges = true;
    }
}
```

### 5.3 PrescriptionCalculator (价格计算组件)

**职责**:处方价格计算和用量分析

```csharp
/// <summary>
/// 处方计算器 - UltraThink架构实现
/// Issue #1153: 继承HerbCalculatorBase共享基类
/// </summary>
public class PrescriptionCalculator : HerbCalculatorBase<PrescriptionItemViewModel>
{
    // ========== 继承自基类的方法 ==========
    // • CalculateTotalDosage - 计算总剂量
    // • CalculateTotalWeight - 计算总重量
    // • CalculateItemRatio - 计算项目比例
    // • CalculateEstimatedTotalPrice - 估算总价
    // • ValidateDosageReasonableness - 验证剂量合理性
    // • CalculateStandardDeviation - 计算标准差

    // ========== 处方特有方法 ==========

    /// <summary>
    /// 计算处方价格详情
    /// </summary>
    public CalculationResult CalculatePrescriptionPrice(
        IEnumerable<PrescriptionItemViewModel> items,
        int dosageCount = 1,
        decimal discount = 1.0m)
    {
        if (items == null || !items.Any())
        {
            return new CalculationResult
            {
                IsValid = false,
                ErrorMessage = "处方项目为空"
            };
        }

        var itemList = items.ToList();
        var singleDosagePrice = itemList.Sum(item => item.Quantity * item.UnitPrice);
        var totalPrice = singleDosagePrice * dosageCount;
        var discountedPrice = totalPrice * discount;
        var totalSaved = totalPrice - discountedPrice;

        return new CalculationResult
        {
            SingleDosagePrice = singleDosagePrice,      // 单剂价格
            TotalPrice = totalPrice,                    // 总价格
            DiscountedPrice = discountedPrice,          // 折扣后价格
            TotalSaved = totalSaved,                    // 节省金额
            ItemCount = itemList.Count,
            IsValid = true,
            CalculatedAt = DateTime.Now
        };
    }

    /// <summary>
    /// 分析处方用量分布
    /// </summary>
    public PrescriptionDosageAnalysis AnalyzeDosageDistribution(IEnumerable<PrescriptionItemViewModel> items)
    {
        if (items == null || !items.Any())
        {
            return new PrescriptionDosageAnalysis();
        }

        var dosages = items.Select(i => i.Dosage).ToList();

        return new PrescriptionDosageAnalysis
        {
            TotalItems = dosages.Count,
            MinDosage = dosages.Min(),
            MaxDosage = dosages.Max(),
            AverageDosage = dosages.Average(),
            TotalDosage = dosages.Sum(),
            StandardDeviation = CalculateStandardDeviation(dosages)  // 调用基类protected方法
        };
    }

    // ========== 结果类 ==========

    public class CalculationResult
    {
        public decimal SingleDosagePrice { get; set; }   // 单剂价格
        public decimal TotalPrice { get; set; }          // 总价格
        public decimal DiscountedPrice { get; set; }     // 折扣后价格
        public decimal TotalSaved { get; set; }          // 节省金额
        public int ItemCount { get; set; }
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime CalculatedAt { get; set; } = DateTime.Now;
    }
}
```

### 5.4 PrescriptionValidator (数据验证组件)

**职责**:处方数据验证和配伍禁忌检查

```csharp
/// <summary>
/// 处方验证器 - UltraThink架构实现
/// Issue #1153: 继承HerbValidatorBase共享基类
/// </summary>
public class PrescriptionValidator : HerbValidatorBase<PrescriptionItemViewModel>
{
    // ========== 继承自基类的方法 ==========
    // • ValidateHerbList - 验证药材列表(重复检测 + 必填项)
    // • ValidateRequiredFields - 验证必填字段
    // • GetDosageWarning - 获取剂量警告

    // ========== 基础验证 ==========

    /// <summary>
    /// 验证处方基本信息
    /// </summary>
    public ValidationResult ValidateBasicInfo(string prescriptionNumber, Guid patientId, string doctorName)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(prescriptionNumber))
        {
            result.AddError("处方编号不能为空");
        }

        if (patientId == Guid.Empty)
        {
            result.AddError("患者信息不能为空");
        }

        if (string.IsNullOrWhiteSpace(doctorName))
        {
            result.AddError("医生信息不能为空");
        }

        return result;
    }

    /// <summary>
    /// 验证处方项目列表
    /// </summary>
    public ValidationResult ValidatePrescriptionItems(IEnumerable<PrescriptionItemViewModel> items)
    {
        // 使用基类的ValidateHerbList方法(包含重复检测和必填项验证)
        return ValidateHerbList(items, "处方");
    }

    /// <summary>
    /// 验证单个处方项目
    /// </summary>
    public ValidationResult ValidatePrescriptionItem(PrescriptionItemViewModel item)
    {
        // 使用基类的ValidateRequiredFields方法
        var result = ValidateRequiredFields(item);

        // 添加剂量警告(使用基类方法)
        var warning = GetDosageWarning(item, 0.1m, 500m);
        if (!string.IsNullOrWhiteSpace(warning))
        {
            result.AddWarning(warning);
        }

        return result;
    }

    // ========== 药材相互作用验证 ==========

    /// <summary>
    /// 验证药材相互作用
    /// </summary>
    public ValidationResult ValidateHerbInteractions(IEnumerable<PrescriptionItemViewModel> items)
    {
        var result = new ValidationResult();

        if (items == null || !items.Any())
        {
            return result;
        }

        var herbNames = items.Select(i => i.HerbName).ToList();

        // 简化的配伍禁忌检查(实际应该基于药材数据库)
        var knownContraindications = GetKnownContraindications();

        foreach (var contraindication in knownContraindications)
        {
            if (herbNames.Contains(contraindication.Herb1) && herbNames.Contains(contraindication.Herb2))
            {
                result.AddWarning($"注意: {contraindication.Herb1} 与 {contraindication.Herb2} 可能存在配伍禁忌");
            }
        }

        return result;
    }

    // ========== 用量安全验证 ==========

    /// <summary>
    /// 验证用量安全性
    /// </summary>
    public ValidationResult ValidateDosageSafety(IEnumerable<PrescriptionItemViewModel> items)
    {
        var result = new ValidationResult();

        if (items == null || !items.Any())
        {
            return result;
        }

        var calculator = new PrescriptionCalculator();
        var analysis = calculator.AnalyzeDosageDistribution(items);
        var warnings = calculator.ValidateDosageReasonableness(items);

        foreach (var warning in warnings)
        {
            result.AddWarning(warning);
        }

        // 检查处方总剂数
        if (analysis.TotalItems > 20)
        {
            result.AddWarning($"处方药味较多({analysis.TotalItems}味),请确认是否合理");
        }

        // 检查用量分布
        if (analysis.StandardDeviation > 50)
        {
            result.AddWarning("处方各味药用量差异较大,请确认配比是否合理");
        }

        return result;
    }

    // ========== 私有方法 ==========

    /// <summary>
    /// 获取已知的配伍禁忌
    /// </summary>
    private List<HerbContraindication> GetKnownContraindications()
    {
        // 简化实现,实际应该从数据库或配置文件读取
        return new List<HerbContraindication>
        {
            new("甘草", "甘遂"),
            new("甘草", "大戟"),
            new("甘草", "芫花"),
            new("乌头", "半夏"),
            new("乌头", "瓜蒌"),
            new("藜芦", "人参"),
            new("藜芦", "沙参")
        };
    }
}

/// <summary>
/// 药材配伍禁忌
/// </summary>
public record HerbContraindication(string Herb1, string Herb2);
```

---

## 6. Services层设计

### 6.1 IPrescriptionPrintService (打印服务接口)

**设计目标**:提供基本的处方打印功能,遵循"适度设计、拒绝过度工程"原则

```csharp
/// <summary>
/// 处方打印服务接口 - 简化版本
/// 遵循"适度设计、拒绝过度工程"原则,提供基本的处方打印功能
/// </summary>
public interface IPrescriptionPrintService
{
    /// <summary>打印处方</summary>
    Task<bool> PrintPrescriptionAsync(PrescriptionDto prescription);

    /// <summary>预览处方</summary>
    Task PreviewPrescriptionAsync(PrescriptionDto prescription);

    /// <summary>批量打印处方</summary>
    Task<int> BatchPrintAsync(PrescriptionDto[] prescriptions);

    /// <summary>导出处方为PDF</summary>
    Task<bool> ExportToPdfAsync(PrescriptionDto prescription, string filePath);

    /// <summary>获取可用的打印机列表</summary>
    string[] GetAvailablePrinters();

    /// <summary>设置默认打印机</summary>
    void SetDefaultPrinter(string printerName);

    /// <summary>获取当前默认打印机</summary>
    string? GetDefaultPrinter();
}

/// <summary>打印选项</summary>
public class PrintOptions
{
    public string? PrinterName { get; set; }
    public int Copies { get; set; } = 1;
    public bool DuplexPrinting { get; set; } = false;
    public PaperSize PaperSize { get; set; } = PaperSize.A4;
    public PrintOrientation Orientation { get; set; } = PrintOrientation.Portrait;
}

public enum PaperSize { A4, A5, Letter, Legal }
public enum PrintOrientation { Portrait, Landscape }
```

### 6.2 PrescriptionPrintService (打印服务实现)

```csharp
/// <summary>
/// 处方打印服务实现
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

    /// <summary>
    /// 打印处方
    /// </summary>
    public async Task<bool> PrintPrescriptionAsync(PrescriptionDto prescription)
    {
        try
        {
            // 构建FlowDocument
            var document = await _documentBuilder.BuildAsync(prescription);

            // 创建PrintDialog
            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                // 执行打印
                printDialog.PrintDocument(
                    ((IDocumentPaginatorSource)document).DocumentPaginator,
                    $"处方_{prescription.PrescriptionNumber}");

                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "打印处方失败");
            return false;
        }
    }

    /// <summary>
    /// 预览处方
    /// </summary>
    public async Task PreviewPrescriptionAsync(PrescriptionDto prescription)
    {
        try
        {
            var document = await _documentBuilder.BuildAsync(prescription);

            // 创建预览窗口
            var previewWindow = new Window
            {
                Title = $"处方预览 - {prescription.PrescriptionNumber}",
                Width = 800,
                Height = 600,
                Content = new DocumentViewer
                {
                    Document = document
                }
            };

            previewWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "预览处方失败");
        }
    }

    /// <summary>
    /// 导出为PDF
    /// </summary>
    public async Task<bool> ExportToPdfAsync(PrescriptionDto prescription, string filePath)
    {
        try
        {
            var document = await _documentBuilder.BuildAsync(prescription);

            // 使用XpsDocument导出为PDF(需要引用System.Printing)
            using var stream = new FileStream(filePath, FileMode.Create);
            using var xpsDocument = new XpsDocument(stream, FileAccess.Write);

            var writer = XpsDocument.CreateXpsDocumentWriter(xpsDocument);
            writer.Write(((IDocumentPaginatorSource)document).DocumentPaginator);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出PDF失败");
            return false;
        }
    }
}
```

### 6.3 PrescriptionFlowDocumentBuilder (文档构建器)

```csharp
/// <summary>
/// 处方FlowDocument构建器
/// </summary>
public class PrescriptionFlowDocumentBuilder
{
    /// <summary>
    /// 构建处方FlowDocument
    /// </summary>
    public async Task<FlowDocument> BuildAsync(PrescriptionDto prescription)
    {
        var document = new FlowDocument
        {
            PagePadding = new Thickness(50),
            ColumnWidth = double.PositiveInfinity
        };

        // 标题
        var title = new Paragraph(new Run("中医处方"))
        {
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20)
        };
        document.Blocks.Add(title);

        // 患者信息
        var patientInfo = new Paragraph
        {
            FontSize = 12,
            Margin = new Thickness(0, 0, 0, 10)
        };
        patientInfo.Inlines.Add(new Run($"处方编号: {prescription.PrescriptionNumber}"));
        patientInfo.Inlines.Add(new LineBreak());
        patientInfo.Inlines.Add(new Run($"日期: {prescription.CreatedAt:yyyy-MM-dd}"));
        document.Blocks.Add(patientInfo);

        // 药材列表
        var table = new Table
        {
            CellSpacing = 0,
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(1)
        };

        // 表格列定义
        table.Columns.Add(new TableColumn { Width = GridLength.Auto });  // 序号
        table.Columns.Add(new TableColumn { Width = new GridLength(200) });  // 药材名称
        table.Columns.Add(new TableColumn { Width = GridLength.Auto });  // 用量
        table.Columns.Add(new TableColumn { Width = GridLength.Auto });  // 单位
        table.Columns.Add(new TableColumn { Width = new GridLength(150) });  // 备注

        // 表头
        var headerRow = new TableRow();
        headerRow.Cells.Add(new TableCell(new Paragraph(new Run("序号"))));
        headerRow.Cells.Add(new TableCell(new Paragraph(new Run("药材名称"))));
        headerRow.Cells.Add(new TableCell(new Paragraph(new Run("用量"))));
        headerRow.Cells.Add(new TableCell(new Paragraph(new Run("单位"))));
        headerRow.Cells.Add(new TableCell(new Paragraph(new Run("备注"))));

        var rowGroup = new TableRowGroup();
        rowGroup.Rows.Add(headerRow);

        // 药材行
        int index = 1;
        foreach (var item in prescription.Items)
        {
            var row = new TableRow();
            row.Cells.Add(new TableCell(new Paragraph(new Run(index.ToString()))));
            row.Cells.Add(new TableCell(new Paragraph(new Run(item.HerbName))));
            row.Cells.Add(new TableCell(new Paragraph(new Run(item.Quantity.ToString("F1")))));
            row.Cells.Add(new TableCell(new Paragraph(new Run(item.Unit))));
            row.Cells.Add(new TableCell(new Paragraph(new Run(item.Remark ?? ""))));

            rowGroup.Rows.Add(row);
            index++;
        }

        table.RowGroups.Add(rowGroup);
        document.Blocks.Add(table);

        // 用法用量
        var usage = new Paragraph
        {
            FontSize = 12,
            Margin = new Thickness(0, 20, 0, 10)
        };
        usage.Inlines.Add(new Run($"剂数: {prescription.DosageCount}剂"));
        usage.Inlines.Add(new LineBreak());
        usage.Inlines.Add(new Run($"用法: {prescription.Usage}"));
        if (!string.IsNullOrWhiteSpace(prescription.Advice))
        {
            usage.Inlines.Add(new LineBreak());
            usage.Inlines.Add(new Run($"医嘱: {prescription.Advice}"));
        }
        document.Blocks.Add(usage);

        return await Task.FromResult(document);
    }
}
```

---

## 7. Constants层设计

### 7.1 PrescriptionConstants (常量定义)

```csharp
/// <summary>
/// 处方相关常量定义
/// </summary>
public static class PrescriptionConstants
{
    // ========== 用法用量常量 ==========

    /// <summary>常用剂数选项</summary>
    public static readonly ReadOnlyCollection<int> CommonDosageCounts = new(
        new int[] { 3, 5, 7, 10, 14, 21, 30 });

    /// <summary>常用用法模板 (8种)</summary>
    public static readonly ReadOnlyCollection<string> CommonUsages = new(
        new string[]
        {
            "每日1剂,水煎服,分早晚两次温服",
            "每日1剂,水煎服,分三次温服",
            "每日2剂,水煎服,分四次温服",
            "每日1剂,水煎服,早晚饭后温服",
            "每日1剂,水煎服,睡前温服",
            "每日1剂,开水泡服,代茶饮",
            "研末冲服,每次3g,每日3次",
            "每日1剂,水煎服,分2次温服,饭前服"
        });

    // ========== 输入提示常量 ==========

    public const string UsageHint = "请输入用法用量,如:每日1剂,水煎服...";
    public const string MedicalAdviceHint = "(可选)输入医嘱,如:忌生冷、注意休息等...";
    public const string RemarkHint = "(可选)补充说明...";

    // ========== 默认值常量 ==========

    public const int DefaultDosageCount = 7;
    public const string DefaultUsage = "每日1剂,水煎服,分早晚两次温服";
    public const decimal DefaultDiscount = 1.0m;
    public const string PrescriptionNumberPrefix = "RX";

    // ========== 验证常量 ==========

    public const int MaxDosageCount = 90;          // 最大剂数
    public const int MinDosageCount = 1;           // 最小剂数
    public const decimal MaxDiscount = 1.0m;       // 最大折扣率
    public const decimal MinDiscount = 0.1m;       // 最小折扣率
    public const int MaxPrescriptionItems = 30;    // 最大处方项目数量

    // ========== 格式化常量 ==========

    public const string PrescriptionNumberFormat = "RX{0:yyyyMMdd}{1:D3}";
    public const string PriceFormat = "F2";
    public const string DosageFormat = "F1";
}
```

---

## 8. 核心设计原则

### 8.1 聚合根约束原则 (Issue #1606 Phase 3)

**原则**:所有Prescription的Write操作必须通过MedicalCase聚合根

**设计原因**:
1. **数据一致性**:确保Prescription作为MedicalCase的一部分,维护聚合根边界
2. **业务逻辑完整性**:避免直接操作Prescription绕过MedicalCase的验证逻辑
3. **职责清晰**:明确Write职责归属,防止职责分散

**实施方式**:

```csharp
// ❌ 错误方式 (Issue #1606前)
public class PrescriptionDataManager
{
    private readonly IPrescriptionRepository _prescriptionRepository;

    public async Task<bool> SaveAsync()
    {
        // 直接通过IPrescriptionRepository创建处方
        var result = await _prescriptionRepository.CreateAsync(prescriptionCreateDto);
        return result != null;
    }
}

// ✅ 正确方式 (Issue #1606 Phase 3)
public class PrescriptionDataManager
{
    private readonly IMedicalCaseRepository _medicalCaseRepository;

    public async Task<bool> SaveAsync()
    {
        // 通过MedicalCase聚合根创建处方
        var result = await _medicalCaseRepository.CreatePrescriptionAsync(
            MedicalCaseId,
            prescriptionCreateDto);
        return result != null;
    }
}
```

**约束范围**:

| 操作类型 | 通过何处执行 | 说明 |
|---------|-------------|------|
| **Create** | IMedicalCaseRepository.CreatePrescriptionAsync | ✅ 聚合根 |
| **Update** | IMedicalCaseRepository.UpdatePrescriptionAsync | ✅ 聚合根 |
| **Delete** | IMedicalCaseRepository.DeletePrescriptionAsync | ✅ 聚合根 |
| **Read** | IPrescriptionApi.GetPrescriptionsByMedicalCaseIdAsync | ✅ API直接调用 |

### 8.2 Component组件化原则

**原则**:将ViewModel的复杂逻辑拆分为5个专门化Component组件

**5个Component**:
1. **PrescriptionDataManager** - 数据CRUD和状态管理
2. **PrescriptionCalculator** - 价格计算和用量分析
3. **PrescriptionValidator** - 数据验证和配伍禁忌检查
4. **PrescriptionCommandHandler** - 13个命令的处理逻辑
5. **PrescriptionEventCoordinator** - 事件协调和订阅管理

**设计优势**:
- ✅ **职责单一**:每个Component专注一个职责
- ✅ **易于测试**:可独立单元测试每个Component
- ✅ **代码复用**:Calculator和Validator继承共享基类(HerbCalculatorBase, HerbValidatorBase)
- ✅ **清晰的依赖**:ViewModel只依赖5个Component,不直接依赖底层Repository

### 8.3 8列DataGrid布局原则

**原则**:将处方项转换为8列布局行,提升药材显示密度

**设计细节**:

```csharp
// 数据结构
public class PrescriptionItemRow
{
    public PrescriptionItemViewModel? Item1 { get; set; }  // 列1-2
    public PrescriptionItemViewModel? Item2 { get; set; }  // 列3-4
    public PrescriptionItemViewModel? Item3 { get; set; }  // 列5-6
    public PrescriptionItemViewModel? Item4 { get; set; }  // 列7-8
}

// 转换逻辑 (Issue #1360)
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

**XAML绑定**:

```xml
<DataGrid ItemsSource="{Binding ItemRows}" AutoGenerateColumns="False">
    <DataGrid.Columns>
        <!-- 列1-2: Item1 -->
        <DataGridTextColumn Header="药材" Binding="{Binding Item1.HerbName}" />
        <DataGridTextColumn Header="用量" Binding="{Binding Item1.Dosage}" />

        <!-- 列3-4: Item2 -->
        <DataGridTextColumn Header="药材" Binding="{Binding Item2.HerbName}" />
        <DataGridTextColumn Header="用量" Binding="{Binding Item2.Dosage}" />

        <!-- 列5-6: Item3 -->
        <DataGridTextColumn Header="药材" Binding="{Binding Item3.HerbName}" />
        <DataGridTextColumn Header="用量" Binding="{Binding Item3.Dosage}" />

        <!-- 列7-8: Item4 -->
        <DataGridTextColumn Header="药材" Binding="{Binding Item4.HerbName}" />
        <DataGridTextColumn Header="用量" Binding="{Binding Item4.Dosage}" />
    </DataGrid.Columns>
</DataGrid>
```

### 8.4 历史处方复制原则 (Issue #1374 ENTRY-16)

**原则**:加载患者最近5条处方,支持一键复制药材项

**实现流程**:

```csharp
// Step 1: 加载患者最近处方
private async Task LoadRecentPrescriptionsAsync()
{
    if (CurrentMedicalCase?.PatientId == null)
        return;

    var response = await _prescriptionApi.GetPatientRecentPrescriptionsAsync(
        CurrentMedicalCase.PatientId,
        count: 5);  // 最近5条

    RecentPrescriptions.Clear();
    foreach (var prescription in response.Data ?? new())
    {
        RecentPrescriptions.Add(prescription);
    }
}

// Step 2: 一键复制
private void ExecuteCopyFromHistory(PrescriptionSearchResultDto prescription)
{
    // 清空当前处方项
    _dataManager.Clear();

    // 复制处方项
    foreach (var item in prescription.Items)
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

    // 重新计算价格
    RecalculatePrice();

    // 刷新8列布局
    RefreshItemRows();

    ShowInfoMessage($"已从历史处方复制 {prescription.Items.Count} 味药材");
}
```

**UI交互**:

```xml
<ComboBox ItemsSource="{Binding RecentPrescriptions}"
          SelectedItem="{Binding SelectedRecentPrescription}"
          DisplayMemberPath="DisplayText">
    <!-- 选中后自动触发CopyFromHistoryCommand -->
</ComboBox>
```

### 8.5 ComboBox拼音码过滤原则 (Issue #1362 ENTRY-4)

**原则**:药材选择支持拼音码快速过滤,限制最多显示5个结果

**实现流程**:

```csharp
// Step 1: 初始化时加载所有药材
private async Task LoadAllHerbsAsync()
{
    var herbs = await _herbRepository.SearchAsync(string.Empty);
    AllHerbs = herbs ?? new List<HerbDto>();
}

// Step 2: 用户输入时过滤
public void FilterHerbs(string searchText)
{
    FilteredHerbs.Clear();

    if (string.IsNullOrWhiteSpace(searchText))
        return;

    // 匹配药材名称或拼音码(不区分大小写)
    var filtered = AllHerbs
        .Where(h => h.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                   (h.PinYinCode?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false))
        .Take(5)  // ⚡限制最多5个结果
        .ToList();

    foreach (var herb in filtered)
    {
        FilteredHerbs.Add(herb);
    }
}
```

**XAML绑定**:

```xml
<ComboBox ItemsSource="{Binding FilteredHerbs}"
          DisplayMemberPath="Name"
          IsEditable="True"
          TextChanged="ComboBox_TextChanged">
    <!-- TextChanged事件触发FilterHerbs方法 -->
</ComboBox>
```

---

## 9. UI层设计

### 9.1 Views结构

**主View**:
- **PrescriptionView.xaml** - 处方编写视图(8列DataGrid布局)
- **PrescriptionsMainView.xaml** - 主页视图(统计数据展示)
- **PrescriptionManagementView.xaml** - 管理视图(处方列表、搜索、删除)

**Dialog**:
- **HerbSelectionDialog.xaml** - 药材选择对话框(支持拼音码搜索)
- **SelectFormulaDialog.xaml** - 验方选择对话框(分类浏览)
- **FormulaTemplateDialog.xaml** - 验方模板对话框(模板管理)
- **PrescriptionEditorDialog.xaml** - 处方编辑对话框(快速编辑)

### 9.2 PrescriptionView.xaml (处方编写)

**布局结构**:

```xml
<UserControl x:Class="LYBT.Desktop.Prescriptions.Views.PrescriptionView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:prism="http://prismlibrary.com/">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>    <!-- 患者信息栏 -->
            <RowDefinition Height="*"/>       <!-- 处方内容区 -->
            <RowDefinition Height="Auto"/>    <!-- 价格汇总栏 -->
            <RowDefinition Height="Auto"/>    <!-- 操作按钮栏 -->
        </Grid.RowDefinitions>

        <!-- Row 0: 患者信息栏 -->
        <StackPanel Grid.Row="0" Orientation="Horizontal" Background="#F5F5F5" Padding="10">
            <TextBlock Text="{Binding PatientInfo}" FontWeight="Bold" Margin="0,0,20,0"/>
            <TextBlock Text="{Binding DoctorInfo}" Foreground="#666"/>
        </StackPanel>

        <!-- Row 1: 处方内容区 -->
        <Grid Grid.Row="1" Margin="10">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>    <!-- 药材选择栏 -->
                <RowDefinition Height="*"/>       <!-- 8列DataGrid -->
                <RowDefinition Height="Auto"/>    <!-- 历史处方栏 -->
                <RowDefinition Height="Auto"/>    <!-- 用法用量栏 -->
            </Grid.RowDefinitions>

            <!-- 药材选择栏 -->
            <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="0,0,0,10">
                <TextBlock Text="选择药材:" VerticalAlignment="Center" Margin="0,0,10,0"/>
                <ComboBox ItemsSource="{Binding FilteredHerbs}"
                          DisplayMemberPath="Name"
                          IsEditable="True"
                          Width="200"
                          TextChanged="HerbComboBox_TextChanged"/>
                <Button Content="添加" Command="{Binding AddHerbCommand}" Margin="10,0,0,0"/>
                <Button Content="导入验方" Command="{Binding ImportFormulaCommand}" Margin="10,0,0,0"/>
            </StackPanel>

            <!-- 8列DataGrid (Issue #1360) -->
            <DataGrid Grid.Row="1"
                      ItemsSource="{Binding ItemRows}"
                      AutoGenerateColumns="False"
                      CanUserAddRows="False">
                <DataGrid.Columns>
                    <!-- 列1-2: Item1 -->
                    <DataGridTextColumn Header="药材" Binding="{Binding Item1.HerbName}" Width="100"/>
                    <DataGridTextColumn Header="用量" Binding="{Binding Item1.Dosage}" Width="60"/>

                    <!-- 列3-4: Item2 -->
                    <DataGridTextColumn Header="药材" Binding="{Binding Item2.HerbName}" Width="100"/>
                    <DataGridTextColumn Header="用量" Binding="{Binding Item2.Dosage}" Width="60"/>

                    <!-- 列5-6: Item3 -->
                    <DataGridTextColumn Header="药材" Binding="{Binding Item3.HerbName}" Width="100"/>
                    <DataGridTextColumn Header="用量" Binding="{Binding Item3.Dosage}" Width="60"/>

                    <!-- 列7-8: Item4 -->
                    <DataGridTextColumn Header="药材" Binding="{Binding Item4.HerbName}" Width="100"/>
                    <DataGridTextColumn Header="用量" Binding="{Binding Item4.Dosage}" Width="60"/>
                </DataGrid.Columns>
            </DataGrid>

            <!-- 历史处方栏 (Issue #1374 ENTRY-16) -->
            <StackPanel Grid.Row="2" Orientation="Horizontal" Margin="0,10,0,0">
                <TextBlock Text="历史处方:" VerticalAlignment="Center" Margin="0,0,10,0"/>
                <ComboBox ItemsSource="{Binding RecentPrescriptions}"
                          SelectedItem="{Binding SelectedRecentPrescription}"
                          DisplayMemberPath="DisplayText"
                          Width="300"/>
                <TextBlock Text="(选中后自动复制)" Foreground="#999" Margin="10,0,0,0" VerticalAlignment="Center"/>
            </StackPanel>

            <!-- 用法用量栏 -->
            <Grid Grid.Row="3" Margin="0,10,0,0">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="Auto"/>
                </Grid.RowDefinitions>

                <!-- 剂数 -->
                <TextBlock Grid.Row="0" Grid.Column="0" Text="剂数:" Margin="0,0,10,5"/>
                <StackPanel Grid.Row="0" Grid.Column="1" Orientation="Horizontal">
                    <TextBox Text="{Binding DosageCount}" Width="100"/>
                    <TextBlock Text="剂" Margin="5,0,20,0" VerticalAlignment="Center"/>
                    <TextBlock Text="常用:" Margin="0,0,5,0" VerticalAlignment="Center"/>
                    <ItemsControl ItemsSource="{x:Static local:PrescriptionConstants.CommonDosageCounts}">
                        <ItemsControl.ItemsPanel>
                            <ItemsPanelTemplate>
                                <StackPanel Orientation="Horizontal"/>
                            </ItemsPanelTemplate>
                        </ItemsControl.ItemsPanel>
                        <ItemsControl.ItemTemplate>
                            <DataTemplate>
                                <Button Content="{Binding}"
                                        Command="{Binding DataContext.SetDosageCountCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                        CommandParameter="{Binding}"
                                        Margin="2"/>
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>
                </StackPanel>

                <!-- 用法 -->
                <TextBlock Grid.Row="1" Grid.Column="0" Text="用法:" Margin="0,5,10,5"/>
                <ComboBox Grid.Row="1" Grid.Column="1"
                          ItemsSource="{x:Static local:PrescriptionConstants.CommonUsages}"
                          Text="{Binding Usage}"
                          IsEditable="True"/>

                <!-- 医嘱 -->
                <TextBlock Grid.Row="2" Grid.Column="0" Text="医嘱:" Margin="0,5,10,5"/>
                <TextBox Grid.Row="2" Grid.Column="1"
                         Text="{Binding MedicalAdvice}"
                         Height="60"
                         TextWrapping="Wrap"
                         AcceptsReturn="True"/>
            </Grid>
        </Grid>

        <!-- Row 2: 价格汇总栏 -->
        <Border Grid.Row="2" Background="#F0F8FF" Padding="10" BorderBrush="#CCC" BorderThickness="0,1,0,0">
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="药材数量:" Margin="0,0,10,0"/>
                <TextBlock Text="{Binding ItemCount}" FontWeight="Bold" Margin="0,0,20,0"/>

                <TextBlock Text="单剂价格:" Margin="0,0,10,0"/>
                <TextBlock Text="{Binding SingleDosagePrice, StringFormat=¥{0:F2}}" FontWeight="Bold" Margin="0,0,20,0"/>

                <TextBlock Text="总价:" Margin="0,0,10,0"/>
                <TextBlock Text="{Binding TotalPrice, StringFormat=¥{0:F2}}" FontWeight="Bold" Margin="0,0,20,0"/>

                <TextBlock Text="折扣后:" Margin="0,0,10,0"/>
                <TextBlock Text="{Binding DiscountedPrice, StringFormat=¥{0:F2}}" FontWeight="Bold" Foreground="#FF6B6B" Margin="0,0,20,0"/>

                <TextBlock Text="处方编号:" Margin="0,0,10,0"/>
                <TextBlock Text="{Binding PrescriptionNumber}" FontWeight="Bold" Foreground="#4CAF50"/>
            </StackPanel>
        </Border>

        <!-- Row 3: 操作按钮栏 -->
        <StackPanel Grid.Row="3" Orientation="Horizontal" HorizontalAlignment="Right" Margin="10">
            <Button Content="验证" Command="{Binding ValidateCommand}" Width="80" Margin="5"/>
            <Button Content="重算" Command="{Binding RecalculateCommand}" Width="80" Margin="5"/>
            <Button Content="清空" Command="{Binding ClearCommand}" Width="80" Margin="5"/>
            <Button Content="保存" Command="{Binding SaveCommand}" Width="80" Margin="5" Style="{StaticResource PrimaryButton}"/>
            <Button Content="打印预览" Command="{Binding PrintPreviewCommand}" Width="100" Margin="5"/>
            <Button Content="返回" Command="{Binding BackCommand}" Width="80" Margin="5"/>
        </StackPanel>
    </Grid>
</UserControl>
```

---

## 10. 模块集成与使用

### 10.1 Prism模块注册

```csharp
// App.xaml.cs
protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
{
    // 注册Prescriptions模块
    moduleCatalog.AddModule<PrescriptionsModule>(InitializationMode.WhenAvailable);
}
```

### 10.2 导航到处方编写页面

```csharp
// 从ConsultationView导航到PrescriptionView
public class ConsultationViewModel : UnifiedViewModelBase
{
    private void GoToPrescription()
    {
        var parameters = new NavigationParameters
        {
            { "MedicalCaseId", CurrentMedicalCase.Id }
        };

        NavigateTo("MainRegion", "PrescriptionView", parameters);
    }
}
```

### 10.3 调用打印服务

```csharp
public class PrescriptionViewModel : UnifiedViewModelBase
{
    private readonly IPrescriptionPrintService _printService;

    private async Task PrintPreview()
    {
        if (CurrentPrescription == null)
        {
            ShowErrorMessage("请先保存处方");
            return;
        }

        await _printService.PreviewPrescriptionAsync(CurrentPrescription);
    }
}
```

---

## 11. 测试策略

### 11.1 单元测试 (Component层)

**测试重点**:5个Component组件的独立测试

```csharp
public class PrescriptionCalculatorTests
{
    private PrescriptionCalculator _calculator;

    [SetUp]
    public void Setup()
    {
        _calculator = new PrescriptionCalculator();
    }

    [Test]
    public void CalculatePrescriptionPrice_应返回正确价格_当处方项有效时()
    {
        // Arrange
        var items = new List<PrescriptionItemViewModel>
        {
            new() { HerbName = "黄芪", Quantity = 30m, UnitPrice = 0.5m },
            new() { HerbName = "党参", Quantity = 20m, UnitPrice = 0.8m }
        };
        var dosageCount = 7;
        var discount = 0.9m;

        // Act
        var result = _calculator.CalculatePrescriptionPrice(items, dosageCount, discount);

        // Assert
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.SingleDosagePrice, Is.EqualTo(31m));  // 30*0.5 + 20*0.8
        Assert.That(result.TotalPrice, Is.EqualTo(217m));        // 31*7
        Assert.That(result.DiscountedPrice, Is.EqualTo(195.3m)); // 217*0.9
    }

    [Test]
    public void AnalyzeDosageDistribution_应返回正确统计_当处方项有效时()
    {
        // Arrange
        var items = new List<PrescriptionItemViewModel>
        {
            new() { Dosage = 10m },
            new() { Dosage = 20m },
            new() { Dosage = 30m }
        };

        // Act
        var result = _calculator.AnalyzeDosageDistribution(items);

        // Assert
        Assert.That(result.TotalItems, Is.EqualTo(3));
        Assert.That(result.MinDosage, Is.EqualTo(10m));
        Assert.That(result.MaxDosage, Is.EqualTo(30m));
        Assert.That(result.AverageDosage, Is.EqualTo(20m));
        Assert.That(result.TotalDosage, Is.EqualTo(60m));
    }
}
```

```csharp
public class PrescriptionValidatorTests
{
    private PrescriptionValidator _validator;

    [SetUp]
    public void Setup()
    {
        _validator = new PrescriptionValidator();
    }

    [Test]
    public void ValidateBasicInfo_应返回错误_当处方编号为空时()
    {
        // Act
        var result = _validator.ValidateBasicInfo("", Guid.NewGuid(), "张医生");

        // Assert
        Assert.That(result.HasErrors, Is.True);
        Assert.That(result.Errors, Contains.Item("处方编号不能为空"));
    }

    [Test]
    public void ValidateHerbInteractions_应返回警告_当存在配伍禁忌时()
    {
        // Arrange
        var items = new List<PrescriptionItemViewModel>
        {
            new() { HerbName = "甘草" },
            new() { HerbName = "甘遂" }  // 与甘草配伍禁忌
        };

        // Act
        var result = _validator.ValidateHerbInteractions(items);

        // Assert
        Assert.That(result.HasWarnings, Is.True);
        Assert.That(result.Warnings.Any(w => w.Contains("甘草") && w.Contains("甘遂")), Is.True);
    }
}
```

### 11.2 集成测试 (ViewModel层)

```csharp
[TestFixture]
public class PrescriptionViewModelTests
{
    private Mock<IPrescriptionApi> _mockPrescriptionApi;
    private Mock<IMedicalCaseRepository> _mockMedicalCaseRepository;
    private Mock<IHerbRepository> _mockHerbRepository;
    private PrescriptionViewModel _viewModel;

    [SetUp]
    public void Setup()
    {
        _mockPrescriptionApi = new Mock<IPrescriptionApi>();
        _mockMedicalCaseRepository = new Mock<IMedicalCaseRepository>();
        _mockHerbRepository = new Mock<IHerbRepository>();

        var dataManager = new PrescriptionDataManager(
            _mockPrescriptionApi.Object,
            _mockMedicalCaseRepository.Object,
            ...);

        var calculator = new PrescriptionCalculator();
        var validator = new PrescriptionValidator();
        var commandHandler = new PrescriptionCommandHandler();
        var eventCoordinator = new PrescriptionEventCoordinator();

        _viewModel = new PrescriptionViewModel(
            _mockPrescriptionApi.Object,
            _mockMedicalCaseRepository.Object,
            _mockHerbRepository.Object,
            ...,
            dataManager,
            calculator,
            validator,
            commandHandler,
            eventCoordinator);
    }

    [Test]
    public async Task InitializeAsync_应加载处方数据_当MedicalCaseId有效时()
    {
        // Arrange
        var medicalCaseId = Guid.NewGuid();
        var parameters = new NavigationParameters
        {
            { "MedicalCaseId", medicalCaseId }
        };

        _mockPrescriptionApi
            .Setup(api => api.GetPrescriptionsByMedicalCaseIdAsync(medicalCaseId))
            .ReturnsAsync(new ApiResponse<List<PrescriptionDto>>
            {
                Data = new List<PrescriptionDto>
                {
                    new PrescriptionDto { Id = Guid.NewGuid(), DosageCount = 7 }
                }
            });

        // Act
        await _viewModel.InitializeAsync(parameters);

        // Assert
        Assert.That(_viewModel.MedicalCaseId, Is.EqualTo(medicalCaseId));
        Assert.That(_viewModel.IsBusy, Is.False);
        _mockPrescriptionApi.Verify(api => api.GetPrescriptionsByMedicalCaseIdAsync(medicalCaseId), Times.Once);
    }

    [Test]
    public void RecalculatePrice_应更新价格属性_当处方项变化时()
    {
        // Arrange
        _viewModel.PrescriptionItems.Add(new PrescriptionItemViewModel
        {
            HerbName = "黄芪",
            Quantity = 30m,
            UnitPrice = 0.5m
        });
        _viewModel.DosageCount = 7;
        _viewModel.Discount = 0.9m;

        // Act
        _viewModel.RecalculateCommand.Execute();

        // Assert
        Assert.That(_viewModel.SingleDosagePrice, Is.EqualTo(15m));    // 30*0.5
        Assert.That(_viewModel.TotalPrice, Is.EqualTo(105m));          // 15*7
        Assert.That(_viewModel.DiscountedPrice, Is.EqualTo(94.5m));    // 105*0.9
    }
}
```

---

## 12. 性能优化

### 12.1 数据加载优化

```csharp
// ✅ 并行加载多个数据源
private async Task LoadPrescriptionDataAsync()
{
    SetIsBusy(true, "正在初始化处方数据...");

    // 并行加载3个独立数据源
    var medicalCaseTask = LoadMedicalCaseAsync();
    var herbsTask = LoadAllHerbsAsync();
    var prescriptionsTask = LoadRecentPrescriptionsAsync();

    await Task.WhenAll(medicalCaseTask, herbsTask, prescriptionsTask);

    // 串行执行依赖DataManager的操作
    await _dataManager.InitializeAsync(MedicalCaseId);
    RecalculatePrice();
    RefreshItemRows();

    SetIsBusy(false);
}
```

### 12.2 UI刷新优化

```csharp
// ✅ 批量更新后统一刷新UI
private void ExecuteCopyFromHistory(PrescriptionSearchResultDto prescription)
{
    // 清空当前处方项
    _dataManager.Clear();

    // 批量添加处方项(不触发CollectionChanged)
    using (_dataManager.PrescriptionItems.DeferRefresh())
    {
        foreach (var item in prescription.Items)
        {
            var newItem = new PrescriptionItemViewModel(...);
            _dataManager.PrescriptionItems.Add(newItem);
        }
    }  // 离开using块时统一触发CollectionChanged

    // 统一刷新UI
    RecalculatePrice();
    RefreshItemRows();
}
```

### 12.3 ComboBox过滤优化

```csharp
// ✅ 限制最多5个结果,避免UI渲染延迟
public void FilterHerbs(string searchText)
{
    FilteredHerbs.Clear();

    if (string.IsNullOrWhiteSpace(searchText))
        return;

    // ⚡ Take(5)限制最多5个结果
    var filtered = AllHerbs
        .Where(h => h.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                   (h.PinYinCode?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false))
        .Take(5)  // 限制最多5个结果
        .ToList();

    foreach (var herb in filtered)
    {
        FilteredHerbs.Add(herb);
    }
}
```

---

## 13. 安全性考虑

### 13.1 输入验证

```csharp
// ✅ 使用PrescriptionValidator验证所有输入
private async Task<bool> ValidateBeforeSave()
{
    var result = new ValidationResult();

    // Step 1: 验证基本信息
    result.Merge(_validator.ValidateBasicInfo(
        PrescriptionNo,
        CurrentMedicalCase?.PatientId ?? Guid.Empty,
        SessionManager?.CurrentUser?.RealName ?? ""));

    // Step 2: 验证处方项列表
    result.Merge(_validator.ValidatePrescriptionItems(PrescriptionItems));

    // Step 3: 验证药材相互作用
    result.Merge(_validator.ValidateHerbInteractions(PrescriptionItems));

    // Step 4: 验证用量安全性
    result.Merge(_validator.ValidateDosageSafety(PrescriptionItems));

    // 显示错误
    if (result.HasErrors)
    {
        ShowErrorMessage($"数据验证失败:\n{string.Join("\n", result.Errors)}");
        return false;
    }

    // 显示警告(不阻止保存)
    if (result.HasWarnings)
    {
        ShowWarningMessage($"警告:\n{string.Join("\n", result.Warnings)}");
    }

    return true;
}
```

### 13.2 权限控制

```csharp
// ✅ 命令的CanExecute检查权限
public class PrescriptionCommandHandler
{
    private bool CanSave()
    {
        // Step 1: 检查是否有处方项
        if (_dataManager.PrescriptionItems.Count == 0)
            return false;

        // Step 2: 检查是否正在加载
        if (_dataManager.IsLoading)
            return false;

        // Step 3: 检查是否有权限(从SessionManager获取)
        var user = _sessionManager?.CurrentUser;
        if (user == null || user.Role != "医生")
            return false;

        return true;
    }

    public DelegateCommand SaveCommand => new(ExecuteSave, CanSave);
}
```

---

## 14. 未来扩展

### 14.1 处方模板功能

**需求**:支持将常用处方保存为模板,快速应用

**设计方案**:
```csharp
// DTO定义
public class PrescriptionTemplateDto
{
    public Guid Id { get; set; }
    public string TemplateName { get; set; }
    public string? Indication { get; set; }
    public List<PrescriptionItemDto> Items { get; set; }
    public string? Usage { get; set; }
}

// Service接口
public interface IPrescriptionTemplateService
{
    Task<List<PrescriptionTemplateDto>> GetTemplatesAsync();
    Task<PrescriptionTemplateDto> SaveAsTemplateAsync(PrescriptionDto prescription, string templateName);
    Task ApplyTemplateAsync(Guid templateId);
}
```

### 14.2 药材相互作用数据库

**需求**:基于药材数据库实现完整的配伍禁忌检查

**设计方案**:
```csharp
// 数据库表设计
public class HerbInteraction
{
    public Guid Id { get; set; }
    public Guid Herb1Id { get; set; }
    public Guid Herb2Id { get; set; }
    public InteractionType Type { get; set; }  // Contraindication, Incompatibility, Synergy
    public string Description { get; set; }
    public string Severity { get; set; }       // Severe, Moderate, Mild
}

// Validator增强
public class PrescriptionValidator
{
    private readonly IHerbInteractionRepository _interactionRepository;

    public async Task<ValidationResult> ValidateHerbInteractionsAsync(IEnumerable<PrescriptionItemViewModel> items)
    {
        var herbIds = items.Select(i => i.HerbId).ToList();
        var interactions = await _interactionRepository.GetInteractionsAsync(herbIds);

        var result = new ValidationResult();
        foreach (var interaction in interactions)
        {
            if (interaction.Type == InteractionType.Contraindication)
            {
                result.AddError($"禁忌: {interaction.Description}");
            }
            else
            {
                result.AddWarning($"注意: {interaction.Description}");
            }
        }

        return result;
    }
}
```

### 14.3 智能用量推荐

**需求**:基于历史数据和AI模型推荐药材用量

**设计方案**:
```csharp
// Service接口
public interface IPrescriptionRecommendationService
{
    /// <summary>推荐药材用量</summary>
    Task<decimal> RecommendDosageAsync(Guid herbId, string indication);

    /// <summary>推荐验方</summary>
    Task<List<FormulaDto>> RecommendFormulasAsync(string indication);

    /// <summary>推荐加味药材</summary>
    Task<List<HerbDto>> RecommendAdditionalHerbsAsync(List<Guid> currentHerbIds, string indication);
}
```

---

## 15. 总结

### 15.1 核心优势

1. **聚合根约束 (Issue #1606 Phase 3)**:
   - ✅ 所有Write操作通过MedicalCaseRepository聚合根,确保数据一致性
   - ✅ 职责清晰,防止职责分散
   - ✅ 架构合规,符合DDD原则

2. **Component组件化**:
   - ✅ 5个专门化组件(DataManager, Calculator, Validator, CommandHandler, EventCoordinator)
   - ✅ 职责单一,易于测试和维护
   - ✅ 代码复用(继承共享基类 HerbCalculatorBase, HerbValidatorBase)

3. **8列DataGrid布局**:
   - ✅ 提升药材显示密度,减少滚动操作
   - ✅ 清晰的Items → ItemRows转换逻辑

4. **完善的功能特性**:
   - ✅ ComboBox拼音码过滤(限制最多5个结果)
   - ✅ 历史处方一键复制(最近5条)
   - ✅ 验方模板导入
   - ✅ 价格计算和用量分析
   - ✅ 数据验证和配伍禁忌检查
   - ✅ 打印预览和PDF导出

5. **处方自动编号 (Issue #1551)**:
   - ✅ Client端临时编号:CF{yyyyMMddHHmmss}
   - ✅ Server端正式编号:RX-YYYYMMDD-NNNN(保存后获取)

### 15.2 关键技术

| 技术点 | 实现方式 | 说明 |
|-------|---------|------|
| **MVVM模式** | Prism框架 | UnifiedViewModelBase基类,DelegateCommand命令 |
| **依赖注入** | Prism IContainerRegistry | 注册Services/ViewModels/Dialogs |
| **组件化** | 5个Component组件 | DataManager, Calculator, Validator, CommandHandler, EventCoordinator |
| **数据验证** | PrescriptionValidator | 继承HerbValidatorBase,配伍禁忌检查 |
| **价格计算** | PrescriptionCalculator | 继承HerbCalculatorBase,用量分析 |
| **打印服务** | IPrescriptionPrintService | FlowDocument构建,PDF导出 |
| **8列布局** | PrescriptionItemRow | 4个项目/行,Items → ItemRows转换 |
| **拼音码过滤** | ComboBox IsEditable + TextChanged | 限制最多5个结果 |
| **历史复制** | RecentPrescriptions + CopyFromHistoryCommand | 最近5条处方 |

### 15.3 文档维护

**维护责任**:当Client端Prescriptions模块发生以下变更时,必须同步更新本文档:
- ✅ **架构调整**:Repository删除、聚合根迁移、组件重构
- ✅ **核心功能变更**:8列布局逻辑修改、价格计算公式变更
- ✅ **ViewModel重构**:待Issue #1608重构完成后更新文档
- ✅ **新增功能**:处方模板、AI推荐等功能上线后补充文档

**文档版本**:
- 当前版本:v1.0.0 (2025-10-30)
- 下次更新:Issue #1608完成后(PrescriptionsMainViewModel/PrescriptionManagementViewModel重构)

---

**文档结束**
