# 可行性分析报告: 药材编辑控件重构

## 1. 分析背景

### 1.1 目标
将药材编辑列表重构成一个高度集成的控件，实现：
- **高内聚**: 所有药材编辑逻辑封装在单一控件中
- **简单输出**: 控件输出 `List<PrescriptionHerbItem>` 或等效DTO
- **解耦**: 处方模块只需赋值，无需关心编辑细节
- **分层校验**: 数据校验放在该放的层级

### 1.2 分析范围
- 处方模块 (LYBT.Desktop.MedicalCase)
- 药草模块 (LYBT.Desktop.Herbs)
- 方剂模块 (LYBT.Desktop.Formula)
- 基础设施层 (LYBT.Desktop.Infrastructure)

---

## 2. 现状分析

### 2.1 当前代码分布

```
药材编辑相关代码分布 (碎片化)
│
├── Infrastructure/Controls/
│   ├── HerbListEditor.xaml          # UI控件 - 仅渲染
│   └── HerbListEditor.xaml.cs       # 依赖属性定义
│
├── MedicalCase/Controls/
│   └── PrescriptionEditorPanel.xaml  # 面板UI - 组装HerbListEditor
│
├── MedicalCase/ViewModels/
│   └── PrescriptionPanelViewModel.cs # 协调器 (646行) - 处理所有逻辑
│
├── MedicalCase/ViewModels/Components/
│   ├── PrescriptionItemHandler.cs    # 药材项CRUD (308行)
│   ├── PrescriptionCalculator.cs     # 价格计算
│   ├── PrescriptionValidator.cs      # 验证逻辑
│   └── PrescriptionImportHandler.cs  # 方剂/历史导入
│
├── MedicalCase/Services/
│   └── HerbSelectionManager.cs       # 行管理器 (用于方剂?)
│
└── Prescriptions/Models/Items/
    └── PrescriptionHerbItem.cs       # 数据模型 (含部分验证)
```

### 2.2 当前依赖关系

```
PrescriptionPanelViewModel (协调者)
    │
    ├── _itemHandler: PrescriptionItemHandler
    │   └── 创建、删除、紧凑、收集药材项
    │
    ├── _calculator: PrescriptionCalculator
    │   └── 计算单剂价格、总价
    │
    ├── _validator: PrescriptionValidator
    │   └── 校验处方完整性、重复药材
    │
    ├── _importHandler: PrescriptionImportHandler
    │   └── 处理方剂导入、历史复制
    │
    ├── _dataLoader: PrescriptionDataLoader
    │   └── 加载处方数据
    │
    ├── _saveHandler: PrescriptionSaveHandler
    │   └── 保存处方
    │
    └── HerbItems: ObservableCollection<PrescriptionHerbItem>
        └── UI绑定到HerbListEditor
```

### 2.3 当前数据流

```
用户输入药材
    ↓
HerbListEditor (UI控件)
    ↓ [双向绑定]
PrescriptionPanelViewModel.HerbItems
    ↓ [事件监听]
OnHerbItemChanged()
    ↓
PrescriptionItemHandler.EnsureMinimumBlankRows()
PrescriptionCalculator.CalculatePrices()
PrescriptionValidator.CheckDuplicateHerbs()
    ↓ [属性更新]
ItemCount, SingleDosagePrice, TotalPrice, DuplicateWarning
```

### 2.4 问题识别

| 问题 | 影响 | 严重程度 |
|-----|------|----------|
| **职责分散** | 药材编辑逻辑散布在6+文件中 | 高 |
| **过度协调** | PrescriptionPanelVM需协调5个Handler | 高 |
| **重复验证** | PrescriptionHerbItem和PrescriptionValidator都有验证 | 中 |
| **状态同步** | ItemCount、价格等需手动更新 | 中 |
| **测试困难** | 需要Mock多个依赖 | 中 |
| **复用受限** | 方剂模块无法直接复用 | 高 |

---

## 3. 目标架构

### 3.1 高内聚控件设计

```
┌─────────────────────────────────────────────────────────────┐
│                    HerbEditorControl                        │
│         (高内聚的药材编辑控件 - 完全自包含)                   │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ 输入依赖 (DependencyProperty)                        │   │
│  │ - AllHerbs: 可选药材列表                             │   │
│  │ - IsReadOnly: 是否只读                               │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ 内部组件 (封装)                                      │   │
│  │ - HerbItemManager: 药材项CRUD                        │   │
│  │ - PriceCalculator: 价格计算                          │   │
│  │ - DuplicateChecker: 重复检测                         │   │
│  │ - ImportProcessor: 导入处理                          │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ 输出属性 (BindableProperty / Event)                  │   │
│  │ - HerbList: List<HerbItemDto>  ← 核心输出            │   │
│  │ - ItemCount: int                                     │   │
│  │ - SingleDosagePrice: decimal                         │   │
│  │ - TotalPrice: decimal                                │   │
│  │ - HasDuplicates: bool                                │   │
│  │ - DuplicateWarning: string                           │   │
│  │ - IsValid: bool                                      │   │
│  │ - HerbListChanged: event                             │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ 公共方法                                             │   │
│  │ - LoadFromDto(items): 加载数据                       │   │
│  │ - ImportFromFormula(formulaId): 导入方剂             │   │
│  │ - CopyFromHistory(prescriptionId): 复制历史          │   │
│  │ - Clear(): 清空                                      │   │
│  │ - Validate(): 校验                                   │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### 3.2 使用方式对比

**当前方式 (复杂)**:
```csharp
// PrescriptionPanelViewModel - 646行
public class PrescriptionPanelViewModel
{
    private readonly PrescriptionItemHandler _itemHandler;
    private readonly PrescriptionCalculator _calculator;
    private readonly PrescriptionValidator _validator;
    private readonly PrescriptionImportHandler _importHandler;
    
    public ObservableCollection<PrescriptionHerbItem> HerbItems { get; }
    public int ItemCount { get; set; }
    public decimal SingleDosagePrice { get; set; }
    public decimal TotalPrice { get; set; }
    public bool IsDuplicateHerbsWarningVisible { get; set; }
    public string DuplicateHerbsWarningText { get; set; }
    
    // 需要手动协调所有Handler...
    private void OnHerbItemChanged(...)
    {
        _itemHandler.EnsureMinimumBlankRows(...);
        UpdateItemCount();
        CalculatePrices();
        CheckDuplicateHerbs();
    }
}
```

**目标方式 (简洁)**:
```csharp
// PrescriptionPanelViewModel - 大幅减少
public class PrescriptionPanelViewModel
{
    // 控件自动处理所有药材编辑逻辑
    // VM只需要在保存时获取HerbList
    
    public async Task<SaveResult> SaveAsync()
    {
        var herbs = HerbEditorControl.HerbList; // 直接获取结果
        var prescription = new PrescriptionInputDto
        {
            Items = herbs,  // 对象赋值
            DosageCount = DosageCount,
            Usage = Usage
        };
        return await _saveHandler.SaveAsync(prescription);
    }
}
```

### 3.3 校验分层设计

| 校验类型 | 所属层 | 说明 |
|---------|--------|------|
| 单项校验 | HerbItemDto | 剂量范围、必填校验 |
| 列表校验 | HerbEditorControl | 重复药材、最小数量 |
| 业务校验 | PrescriptionPanelVM | 与诊断关联校验 |
| 完整性校验 | SaveHandler | 保存前最终校验 |

---

## 4. 可行性评估

### 4.1 技术可行性

| 方面 | 评估 | 说明 |
|-----|------|------|
| WPF UserControl封装 | **可行** | 标准WPF模式 |
| DependencyProperty绑定 | **可行** | 已有HerbListEditor经验 |
| 内部状态管理 | **可行** | 控件内部维护状态 |
| 事件通知机制 | **可行** | INotifyPropertyChanged + 自定义事件 |
| 跨模块复用 | **可行** | 放在Infrastructure层 |

### 4.2 影响范围分析

**需要修改的文件**:
| 模块 | 文件 | 变更类型 |
|-----|------|----------|
| Infrastructure | HerbEditorControl.xaml/cs | 新建 |
| Infrastructure | HerbItemDto.cs | 新建 |
| MedicalCase | PrescriptionPanelViewModel.cs | 大幅简化 |
| MedicalCase | PrescriptionEditorPanel.xaml | 替换控件 |
| MedicalCase | PrescriptionItemHandler.cs | 迁移到控件 |
| MedicalCase | PrescriptionCalculator.cs | 迁移到控件 |
| MedicalCase | PrescriptionImportHandler.cs | 迁移到控件 |
| Formula | FormulaDetailViewModel.cs | 可复用控件 |

**影响统计**:
- 新建文件: 2-3个
- 修改文件: 5-8个
- 删除文件: 3-4个 (迁移后)

### 4.3 风险评估

| 风险 | 概率 | 影响 | 缓解措施 |
|-----|------|------|----------|
| 重构范围大 | 中 | 高 | 分Phase实施 |
| UI绑定兼容 | 低 | 中 | 保持DependencyProperty模式 |
| 方剂模块影响 | 中 | 中 | 先处方后方剂 |
| 测试覆盖 | 中 | 中 | 增加单元测试 |

### 4.4 收益评估

| 收益 | 量化 |
|-----|------|
| 代码减少 | PrescriptionPanelVM: 646行 → ~200行 |
| 文件减少 | Handler文件: 4个 → 0个 (迁移) |
| 复用提升 | 方剂模块可直接使用 |
| 测试简化 | Mock依赖: 5个 → 1个 |
| 维护成本 | 降低约50% |

---

## 5. 实施建议

### 5.1 推荐方案

**分三阶段实施**:

**Phase 1: 创建核心控件 (2天)**
- 新建 `HerbEditorControl` 控件
- 迁移 `PrescriptionItemHandler` 逻辑
- 迁移 `PrescriptionCalculator` 逻辑
- 定义输入/输出接口

**Phase 2: 集成到处方模块 (1.5天)**
- 替换 `PrescriptionEditorPanel` 中的控件
- 简化 `PrescriptionPanelViewModel`
- 更新保存逻辑使用HerbList

**Phase 3: 扩展复用 (1天)**
- 方剂模块复用控件
- 删除冗余Handler文件
- 完善单元测试

### 5.2 建议调整

与当前 `simplify-workspace-event-architecture` 提案的关系：
- **独立提案**: 药材编辑控件重构应单独创建OpenSpec提案
- **先后顺序**: 可并行或在事件架构重构后进行
- **共享原则**: 两个提案都遵循KISS、高内聚原则

---

## 6. 结论

### 6.1 可行性结论

**推荐实施** - 技术可行，收益明显，风险可控。

### 6.2 下一步行动

1. 创建独立OpenSpec提案: `herb-editor-control-refactoring`
2. 定义 `HerbEditorControl` 接口规范
3. 设计校验分层标准
4. 制定详细任务清单

---

## 附录: 当前文件行数统计

| 文件 | 行数 | 重构后预期 |
|-----|------|-----------|
| PrescriptionPanelViewModel.cs | 646 | ~200 |
| PrescriptionItemHandler.cs | 308 | 0 (迁移) |
| PrescriptionCalculator.cs | ~80 | 0 (迁移) |
| PrescriptionValidator.cs | ~120 | ~50 (部分保留) |
| PrescriptionImportHandler.cs | ~150 | 0 (迁移) |
| HerbListEditor.xaml.cs | ~100 | 0 (替换) |
| **HerbEditorControl (新)** | - | ~400 |
| **总计** | ~1404 | ~650 |

**代码减少**: 约54%
