# Proposal: 删除Panel ViewModel - 简化医案数据模型

**Change ID**: `consolidate-panel-viewmodels`
**Type**: Refactoring / Code Deletion
**Priority**: P1 (Current Sprint)
**Status**: Draft
**Author**: Claude Code
**Created**: 2026-01-05
**Target Version**: v1.1.0

---

## 1. Executive Summary

### 1.1 核心洞察

**Desktop层Item模式已标准化**，各模块均已实现OOP数据转换：

```
Entity (服务端) → DTO (Shared层) → Item (Desktop层) → ViewModel
```

**审计结果**（2026-01-05）：

| 模块 | Item类 | 转换方法 | 状态 |
|------|--------|----------|------|
| Consultation | `ConsultationItem` | FromDto/ToDto/ToInputDto | 已标准化 |
| Patients | `PatientItem` | FromDto/ToDto/UpdateFromDto | 已标准化 |
| Formula | `FormulaItem` | FromDto/ToDto | 已标准化 |
| Users | `UserItem` | FromDto/ToDto/UpdateFromDto | 已标准化 |
| Herbs | `HerbItemDto` | Clone/CreateEmpty | 已标准化 |
| **MedicalCase** | PanelViewModel | **过度设计** | 需重构 |

### 1.2 问题陈述

MedicalCase模块使用Panel ViewModel模式，与其他模块不一致：

```
其他模块: ViewModel → Item (简单数据容器)
MedicalCase: ViewModel → PanelViewModel → 散落属性 (过度设计)
```

**Panel ViewModel问题**:
- ConsultationPanelViewModel (379行) - 仅4个诊断字段
- PrescriptionPanelViewModel (831行) - 仅药材列表+元数据
- 绑定路径复杂: `{Binding PrescriptionPanelViewModel.HerbItems}`

### 1.3 提案目标

1. **删除Panel ViewModel**: 移除过度设计的中间层
2. **创建Item模型**: PrescriptionEditItem封装处方数据
3. **复用现有Item**: ConsultationItem已存在，直接复用
4. **简化绑定**: `{Binding Prescription.HerbItems}`

### 1.4 预期收益

| 收益 | 量化指标 |
|------|----------|
| 代码删除 | -1200行 (Panel ViewModel) |
| 新增代码 | +100行 (PrescriptionEditItem) |
| 净减少 | -1100行 |
| 绑定简化 | 1级路径 (原2级) |
| 模式统一 | MedicalCase与其他模块一致 |

---

## 1A. Panel ViewModel详细分析

### ConsultationPanelViewModel (379行)

**核心职责**:
- 4个诊断属性: PresentIllness, TongueDiagnosis, PulseDiagnosis, TcmDiagnosis
- 状态管理: Status (NotStarted/InProgress/Completed)
- 验证逻辑: Validate() - 检查TcmDiagnosis是否已填
- 数据提供: GetConsultationData() -> ConsultationInputDto
- 初始化: Initialize(medicalCaseId, existingConsultation)

**依赖分析**:
| 依赖 | WorkspaceVM已有 | 处理方式 |
|------|-----------------|----------|
| IMedicalCaseRepository | 是 | 复用 |
| IEventAggregator | 是 | 复用 |
| ILoggerFactory | 是 | 复用 |
| IRegionManager | 是 | 复用 |

**重构评估**:
- 代码量: 约100行有效代码（排除注释和空行）
- 复杂度: 低（纯属性+简单验证）
- **建议**: 完全内联到WorkspaceViewModel

---

### PrescriptionPanelViewModel (831行)

**核心职责**:
1. 处方属性: DosageCount, Usage, TreatmentPrinciple, ReferencedFormulas, Remark
2. 价格计算: SingleDosagePrice, TotalPrice
3. 药材管理: HerbItemsToLoad, PendingAddHerbs, AllHerbs
4. 状态管理: Status, HasUnsavedChanges, IsReadOnly
5. 重复警告: IsDuplicateHerbsWarningVisible, DuplicateHerbsWarningText
6. 数据提供: GetPrescriptionData() -> PrescriptionInputDto
7. 对话框处理: 经验方导入、历史处方复制

**组件依赖** (已拆分的Component):
| 组件 | 职责 | 保留原因 |
|------|------|----------|
| PrescriptionCalculator | 价格计算 | 有测试覆盖，逻辑复杂 |
| PrescriptionValidator | 处方验证 | 有测试覆盖 |
| PrescriptionSaveHandler | 保存处理 | 聚合保存逻辑 |
| PrescriptionImportHandler | 导入转换 | 验方/历史转换 |
| PrescriptionDataLoader | 数据加载 | 异步加载药材库 |

**重构评估**:
- 代码量: 约500行有效代码
- 复杂度: 高（多个组件协调、对话框处理）
- **建议**: 属性提升 + 保留组件委托

---

## 2. Current State Analysis

### 2.1 当前Panel ViewModel架构

```
MedicalCaseWorkspaceViewModel
├── ConsultationPanelViewModel (子ViewModel)
│   ├── PresentIllness, TongueDiagnosis, PulseDiagnosis, TcmDiagnosis
│   ├── Status, Validate(), GetConsultationData()
│   └── IDataProvider, IValidatable接口
│
└── PrescriptionPanelViewModel (子ViewModel)
    ├── HerbItems, AllHerbs, DosageCount, Usage
    ├── SingleDosagePrice, ItemCount, UsageOptions
    ├── OpenFormulaImportDialogCommand, OpenHistoryCopyDialogCommand
    ├── ClearHerbItemsCommand
    ├── Status, Validate(), GetPrescriptionData()
    └── IDataProvider, IValidatable接口
```

### 2.2 当前绑定方式

```xml
<!-- MedicalCaseWorkspaceView.xaml -->
<controls:MedicalCaseEditControl
    PresentIllness="{Binding ConsultationPanelViewModel.PresentIllness, Mode=TwoWay}"
    HerbItems="{Binding PrescriptionPanelViewModel.HerbItems, Mode=TwoWay}"
    TotalPrice="{Binding PrescriptionPanelViewModel.SingleDosagePrice}"
    ImportFormulaCommand="{Binding PrescriptionPanelViewModel.OpenFormulaImportDialogCommand}"
    ... />
```

### 2.3 价格计算现状

- **PrescriptionCalculator**: 计算单剂价格
- **调用时机**: HerbItems变化时
- **问题**: 不考虑剂数、无折扣支持、与保存分离

### 2.4 处方笺打印现状

- **PrescriptionPrintService**: 生成FixedDocument
- **PrescriptionPrintModel**: 打印数据模型
- **问题**: 需手动组装数据，无法直接访问医案上下文

---

## 3. Proposed Architecture

### 3.1 内联后的ViewModel结构

```
MedicalCaseWorkspaceViewModel
├── [诊断属性] (原ConsultationPanelViewModel)
│   ├── PresentIllness, TongueDiagnosis, PulseDiagnosis, TcmDiagnosis
│   └── ConsultationStatus (派生属性)
│
├── [处方属性] (原PrescriptionPanelViewModel)
│   ├── HerbItems, AllHerbs, DosageCount, Usage
│   ├── SingleDosagePrice, TotalPrice (新: 单剂×剂数)
│   └── PrescriptionStatus (派生属性)
│
├── [命令] (提升到顶层)
│   ├── OpenFormulaImportDialogCommand
│   ├── OpenHistoryCopyDialogCommand
│   ├── ClearHerbItemsCommand
│   └── PrintPrescriptionCommand (新)
│
└── [组件] (保留)
    ├── PrescriptionCalculator
    ├── PrescriptionValidator
    ├── PrescriptionSaveHandler
    └── PrescriptionDataLoader
```

### 3.2 简化后的绑定

```xml
<!-- MedicalCaseWorkspaceView.xaml (简化后) -->
<controls:MedicalCaseEditControl
    PresentIllness="{Binding PresentIllness, Mode=TwoWay}"
    HerbItems="{Binding HerbItems, Mode=TwoWay}"
    TotalPrice="{Binding SingleDosagePrice}"
    ImportFormulaCommand="{Binding OpenFormulaImportDialogCommand}"
    ... />
```

### 3.3 价格计算重设计

```csharp
/// <summary>
/// 医案价格计算（从完整逻辑出发）
/// </summary>
public class MedicalCasePriceCalculator
{
    /// <summary>
    /// 计算完整价格
    /// </summary>
    /// <param name="herbItems">药材列表</param>
    /// <param name="doseCount">剂数</param>
    /// <param name="discountRate">折扣率(可选)</param>
    /// <returns>价格明细</returns>
    public PriceBreakdown Calculate(
        IEnumerable<HerbItemDto> herbItems,
        int doseCount,
        decimal? discountRate = null)
    {
        var singleDosePrice = herbItems.Sum(h => h.Dosage * h.UnitPrice);
        var totalBeforeDiscount = singleDosePrice * doseCount;
        var discount = discountRate.HasValue ? totalBeforeDiscount * discountRate.Value : 0;
        var finalPrice = totalBeforeDiscount - discount;

        return new PriceBreakdown
        {
            SingleDosePrice = singleDosePrice,
            DoseCount = doseCount,
            TotalBeforeDiscount = totalBeforeDiscount,
            Discount = discount,
            FinalPrice = finalPrice
        };
    }
}
```

### 3.4 处方笺打印重设计

```csharp
/// <summary>
/// 医案打印服务（从完整上下文出发）
/// </summary>
public interface IMedicalCasePrintService
{
    /// <summary>
    /// 打印处方笺
    /// </summary>
    /// <param name="context">医案完整上下文</param>
    Task PrintPrescriptionAsync(MedicalCasePrintContext context);
}

/// <summary>
/// 医案打印上下文
/// </summary>
public class MedicalCasePrintContext
{
    // 患者信息
    public PatientInfo Patient { get; set; }

    // 诊断信息
    public string PresentIllness { get; set; }
    public string TcmDiagnosis { get; set; }

    // 处方信息
    public IReadOnlyList<HerbItemDto> HerbItems { get; set; }
    public int DoseCount { get; set; }
    public string Usage { get; set; }

    // 价格信息
    public PriceBreakdown Price { get; set; }

    // 元数据
    public DateTime PrintTime { get; set; }
    public string DoctorName { get; set; }
}
```

---

## 4. Implementation Plan (简化版)

### Phase 1: 删除ConsultationPanelViewModel (0.5天)

**数据模型变化**:
```
之前: WorkspaceVM -> ConsultationPanelViewModel -> 4个属性
之后: WorkspaceVM -> ConsultationItem实例 (复用现有)
```

**关键发现**: `ConsultationItem`已存在于`LYBT.Desktop.Consultation.Models.Items`，包含：
- 4个诊断字段: PresentIllness, TongueDiagnosis, PulseDiagnosis, TCMDiagnosis
- FromDto/ToDto/ToInputDto完整转换方法
- IsDiagnosisComplete验证属性

**步骤**:
1. 在WorkspaceViewModel中持有ConsultationItem实例
2. 更新XAML绑定路径（`ConsultationPanelViewModel.` → `Consultation.`）
3. 删除ConsultationPanelViewModel.cs
4. 更新MedicalCaseModule.cs的DI注册

**WorkspaceViewModel新增代码** (~10行):
```csharp
#region 诊断数据

/// <summary>诊断编辑项 - 复用现有ConsultationItem</summary>
public ConsultationItem Consultation { get; } = new();

#endregion
```

---

### Phase 2: 创建PrescriptionEditItem (0.5天)

**OOP数据层次**:
```
Entity (服务端) → DTO (Shared层) → Item (Desktop层) → ViewModel
```

**新建: Models/Items/PrescriptionEditItem.cs**
```csharp
using LYBT.Desktop.Herbs.Models;
using Prism.Mvvm;
using System.Collections.ObjectModel;

namespace LYBT.Desktop.MedicalCase.Models.Items;

/// <summary>
/// 处方编辑项 - Desktop层UI模型
/// OpenSpec: consolidate-panel-viewmodels
/// </summary>
public class PrescriptionEditItem : BindableBase
{
    #region 核心属性

    private ObservableCollection<HerbItemDto> _herbItems = new();
    /// <summary>药材列表</summary>
    public ObservableCollection<HerbItemDto> HerbItems
    {
        get => _herbItems;
        set => SetProperty(ref _herbItems, value);
    }

    private int _dosageCount = 7;
    /// <summary>剂数</summary>
    public int DosageCount
    {
        get => _dosageCount;
        set
        {
            if (SetProperty(ref _dosageCount, value))
            {
                RaisePropertyChanged(nameof(TotalPrice));
            }
        }
    }

    private string _usage = "水煎服，一日一剂，分早晚两次温服";
    /// <summary>用法</summary>
    public string Usage
    {
        get => _usage;
        set => SetProperty(ref _usage, value);
    }

    #endregion

    #region 派生属性

    /// <summary>有效药材数量</summary>
    public int HerbCount => HerbItems.Count(h => h.IsValid);

    /// <summary>单剂价格</summary>
    public decimal SingleDosagePrice => HerbItems.Where(h => h.IsValid).Sum(h => h.CalculatePrice());

    /// <summary>总价</summary>
    public decimal TotalPrice => SingleDosagePrice * DosageCount;

    /// <summary>是否有药材</summary>
    public bool HasHerbs => HerbCount > 0;

    /// <summary>是否有效（至少一味药材）</summary>
    public bool IsValid => HasHerbs;

    #endregion

    #region 转换方法

    /// <summary>从DTO加载</summary>
    public void LoadFromDto(PrescriptionDetailDto dto)
    {
        DosageCount = dto.DosageCount;
        Usage = dto.Usage ?? _usage;
        HerbItems.Clear();
        if (dto.Items != null)
        {
            foreach (var item in dto.Items)
            {
                HerbItems.Add(new HerbItemDto
                {
                    HerbId = item.HerbId,
                    HerbName = item.HerbName ?? string.Empty,
                    Dosage = item.Dosage,
                    UnitPrice = item.UnitPrice,
                    DecocteMethod = item.DecocteMethod
                });
            }
        }
    }

    /// <summary>转换为InputDto</summary>
    public PrescriptionInputDto ToInputDto()
    {
        return new PrescriptionInputDto
        {
            DosageCount = DosageCount,
            Usage = Usage,
            Items = HerbItems.Where(h => h.IsValid).Select(h => new PrescriptionItemInputDto
            {
                HerbId = h.HerbId,
                HerbName = h.HerbName,
                Dosage = h.Dosage,
                UnitPrice = h.UnitPrice,
                DecocteMethod = h.DecocteMethod
            }).ToList()
        };
    }

    /// <summary>清空处方</summary>
    public void Clear()
    {
        HerbItems.Clear();
        DosageCount = 7;
        Usage = "水煎服，一日一剂，分早晚两次温服";
    }

    #endregion
}
```

---

### Phase 3: 删除PrescriptionPanelViewModel (1天)

**数据模型变化**:
```
之前: WorkspaceVM -> PrescriptionPanelViewModel -> HerbItems + 元数据
之后: WorkspaceVM -> PrescriptionEditItem (OOP封装)
```

**步骤**:
1. 在WorkspaceViewModel中持有PrescriptionEditItem实例
2. 保留现有服务引用（ImportService, PrintService等）
3. 将命令移到WorkspaceViewModel
4. 更新XAML绑定路径（`PrescriptionPanelViewModel.HerbItems` → `Prescription.HerbItems`）
5. 删除PrescriptionPanelViewModel.cs
6. 更新MedicalCaseModule.cs的DI注册

**WorkspaceViewModel新增代码** (~40行):
```csharp
#region 诊断和处方实例

/// <summary>诊断编辑项</summary>
public ConsultationItem Consultation { get; } = new();

/// <summary>处方编辑项</summary>
public PrescriptionEditItem Prescription { get; } = new();

// 药材库（用于自动补全，供HerbListControl使用）
private ObservableCollection<HerbListDto> _allHerbs = new();
public ObservableCollection<HerbListDto> AllHerbs => _allHerbs;

#endregion

#region 命令

public DelegateCommand OpenFormulaImportDialogCommand { get; }
public DelegateCommand OpenHistoryCopyDialogCommand { get; }
public DelegateCommand ClearHerbItemsCommand { get; }
public DelegateCommand PrintPrescriptionCommand { get; }

#endregion
```

**XAML绑定变化**:
```xml
<!-- 之前 -->
<controls:MedicalCaseEditControl
    PresentIllness="{Binding ConsultationPanelViewModel.PresentIllness, Mode=TwoWay}"
    HerbItems="{Binding PrescriptionPanelViewModel.HerbItems, Mode=TwoWay}" />

<!-- 之后 (OOP Item模式) -->
<controls:MedicalCaseEditControl
    PresentIllness="{Binding Consultation.PresentIllness, Mode=TwoWay}"
    HerbItems="{Binding Prescription.HerbItems, Mode=TwoWay}" />
```

---

### Phase 4: 服务层整理 (0.5天)

**核心原则**: 所有复杂逻辑都以服务形式注入，ViewModel只持有数据和调用服务

**服务清单**:
| 服务 | 接口 | 职责 |
|------|------|------|
| **导入服务** | `IPrescriptionImportService` | 验方导入、历史处方复制 |
| **价格计算** | `IPriceCalculator` | 计算单剂/总价 |
| **打印服务** | `IPrescriptionPrintService` | 处方笺打印 |
| **数据加载** | `IPrescriptionDataLoader` | 加载药材库 |

**导入服务设计**:
```csharp
public interface IPrescriptionImportService
{
    /// <summary>
    /// 从验方导入药材
    /// </summary>
    Task<IReadOnlyList<HerbItemDto>> ImportFromFormulaAsync(FormulaDetailDto formula, IEnumerable<FormulaHerbItemDto> herbs);

    /// <summary>
    /// 从历史处方复制药材
    /// </summary>
    Task<IReadOnlyList<HerbItemDto>> ImportFromHistoryAsync(IEnumerable<PrescriptionItemDto> items);
}

// 实现: PrescriptionImportHandler改造为服务
public class PrescriptionImportService : IPrescriptionImportService
{
    public Task<IReadOnlyList<HerbItemDto>> ImportFromFormulaAsync(...)
    {
        // 现有PrescriptionImportHandler.ToHerbItemDtos逻辑
    }
}
```

**WorkspaceViewModel使用服务**:
```csharp
// 注入服务
private readonly IPrescriptionImportService _importService;
private readonly IPriceCalculator _priceCalculator;
private readonly IPrescriptionPrintService _printService;
private readonly IPrescriptionDataLoader _dataLoader;

// 导入命令调用服务
private async void ExecuteOpenFormulaImportDialog()
{
    _dialogService.ShowDialog(nameof(FormulaImportDialog), null, async r =>
    {
        if (r.Result != ButtonResult.OK) return;

        var formula = r.Parameters.GetValue<FormulaDetailDto>("SelectedFormula");
        var herbs = r.Parameters.GetValue<List<FormulaHerbItemDto>>("SelectedHerbs");

        // 调用服务导入
        var imported = await _importService.ImportFromFormulaAsync(formula, herbs);

        // 合并到当前列表
        AddHerbsToList(imported);
    });
}
```

**打印服务扩展**:
```csharp
// 现有接口扩展
public interface IPrescriptionPrintService
{
    // 现有方法保留
    Task PrintAsync(PrescriptionPrintModel model);

    // 新增：基于医案上下文打印
    Task PrintFromContextAsync(MedicalCasePrintContext context);
}

public class MedicalCasePrintContext
{
    public string PatientName { get; set; }
    public string TcmDiagnosis { get; set; }
    public IReadOnlyList<HerbItemDto> HerbItems { get; set; }
    public int DoseCount { get; set; }
    public string Usage { get; set; }
    public decimal TotalPrice { get; set; }
}
```

---

### 总工期: 2天

| Phase | 工期 | 删除代码 | 新增代码 |
|-------|------|----------|----------|
| Phase 1 | 0.5天 | 379行 | ~30行 |
| Phase 2 | 1天 | 831行 | ~80行 |
| Phase 3 | 0.5天 | 0行 | ~20行 |
| **合计** | **2天** | **1210行** | **~130行** |

**净减少: ~1080行**

---

## 5. Risk Assessment

| 风险 | 可能性 | 影响 | 缓解措施 |
|------|--------|------|----------|
| 绑定迁移遗漏 | 中 | 高 | 逐个属性迁移，每次验证 |
| 功能回归 | 中 | 高 | 保留现有逻辑，分步重构 |
| 价格计算不准确 | 低 | 高 | 单元测试覆盖 |

---

## 6. Affected Files

### 需修改

| 文件 | 变更类型 | 说明 |
|------|----------|------|
| `MedicalCaseWorkspaceViewModel.cs` | MAJOR | 内联Panel属性和命令 |
| `MedicalCaseWorkspaceView.xaml` | MAJOR | 更新所有绑定路径 |
| `PrescriptionCalculator.cs` | MODIFY | 重构为MedicalCasePriceCalculator |
| `PrescriptionPrintService.cs` | MODIFY | 重构为IMedicalCasePrintService |
| `MedicalCaseModule.cs` | MODIFY | 更新DI注册 |

### 需删除

| 文件 | 删除原因 |
|------|----------|
| `ConsultationPanelViewModel.cs` | 属性已内联 |
| `PrescriptionPanelViewModel.cs` | 属性已内联 |

### 需新建

| 文件 | 说明 |
|------|------|
| `Models/PriceBreakdown.cs` | 价格明细数据模型 |
| `Models/MedicalCasePrintContext.cs` | 打印上下文数据模型 |
| `Services/IMedicalCasePrintService.cs` | 打印服务接口 |

---

## 7. Dependencies

- **前置**: `refactor-medicalcase-workspace` Phase 5完成（Panel控件删除）
- **前置**: `simplify-workspace-event-architecture` (可选，事件简化)

---

## 8. Success Criteria

- [ ] Panel ViewModel类已删除
- [ ] 绑定路径简化为1级
- [ ] 价格计算支持剂数和折扣
- [ ] 打印可访问完整医案上下文
- [ ] 编译通过，功能正常

---

## Appendix A: Desktop层Item标准规范

### A.1 标准目录结构

```
LYBT.Desktop.{Module}/
├── Models/
│   ├── Items/                    # Item类存放位置（规范）
│   │   ├── {Module}Item.cs       # 主实体Item
│   │   └── {Sub}Item.cs          # 子实体Item
│   ├── {Module}DetailModel.cs    # 详情模型（用于复杂视图）
│   └── {Enum}.cs                 # 枚举定义
└── ViewModels/
```

### A.2 Item类命名规范

| 用途 | 命名模式 | 示例 |
|------|----------|------|
| 列表项 | `{Entity}Item` | `PatientItem`, `FormulaItem` |
| 编辑项 | `{Entity}EditItem` | `PrescriptionEditItem` |
| 子项 | `{Parent}{Child}Item` | `FormulaHerbItem`, `PrescriptionHerbItem` |
| 输出DTO | `{Entity}Dto` | **应避免，使用Item** |

### A.3 Item类标准结构

```csharp
/// <summary>
/// {Entity}列表项UI模型 - 用于DataGrid/ListView显示
/// 替代直接使用{Entity}Dto，实现Desktop层与Shared层的解耦
/// </summary>
public class {Entity}Item : BindableBase
{
    // 核心属性（private backing field + public property）
    private Guid _id;
    public Guid Id { get => _id; set => SetProperty(ref _id, value); }

    // UI状态属性
    private bool _isSelected;
    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }

    // 转换方法（必需）
    public static {Entity}Item FromDto({Entity}DetailDto dto) { ... }
    public {Entity}DetailDto ToDto() { ... }
    public {Entity}InputDto ToInputDto() { ... }  // 可选
    public void UpdateFromDto({Entity}DetailDto dto) { ... }  // 可选

    // 派生属性（只读计算）
    public string DisplayText => $"...";
    public bool IsValid => ...;
}
```

### A.4 审计结果 (2026-01-05)

| 模块 | Item类 | 位置 | 符合规范 | 备注 |
|------|--------|------|----------|------|
| Consultation | ConsultationItem | Models/Items/ | 是 | - |
| Patients | PatientItem | Models/Items/ | 是 | - |
| Users | UserItem | Models/Items/ | 是 | - |
| Formula | FormulaItem | Models/Items/ | 是 | - |
| Formula | FormulaHerbItem | Models/Items/ | 是 | - |
| MedicalCase | MedicalCaseItem | Models/Items/ | 是 | - |
| MedicalCase | PrescriptionHerbItem | Models/Items/ | 是 | 继承HerbItemViewModelBase |
| **Herbs** | **HerbItemDto** | **Models/** | **否** | **应移至Items/** |
| **MedicalCase** | **PrescriptionEditItem** | **待创建** | - | **本提案** |

### A.5 待修复项

**HerbItemDto位置问题**：
- 当前: `LYBT.Desktop.Herbs/Models/HerbItemDto.cs`
- 应为: `LYBT.Desktop.Herbs/Models/Items/HerbItemDto.cs`
- 建议: 移动文件并重命名为`HerbItem.cs`（符合命名规范）

**待创建项**：
- `LYBT.Desktop.MedicalCase/Models/Items/PrescriptionEditItem.cs`

---

**文档版本**: v1.1
**最后更新**: 2026-01-05
