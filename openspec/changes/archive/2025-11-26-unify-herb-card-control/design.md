# Design: unify-herb-card-control

## Overview

统一药材卡片控件设计，使处方编辑复用经验方的 `HerbCardControl` 模式，同时支持可选的价格显示功能。

## Current State

### 经验方模块 (LYBT.Desktop.Formula)

```
HerbCardControl.xaml
├── 药材名称 TextBox + Popup建议列表
├── 剂量输入 TextBox + 单位显示
└── 删除按钮

FormulaHerbItemViewModel.cs
├── IHerbItem 接口实现
├── 拼音码过滤逻辑
└── UnitPrice => 0m (固定返回0)
```

### 处方模块 (LYBT.Desktop.MedicalCase)

```
PrescriptionEditorView.xaml (旧)
├── 8列 DataGrid 布局
└── ComboBox 药材选择

PrescriptionEditorPanel.xaml (新)
├── HerbCardControl (本地复制版)
├── ItemsControl + UniformGrid(4列)
└── 底部价格显示区域
```

## Target State

### 共享控件架构

```
LYBT.Desktop.Presentation (共享层)
└── Controls/
    └── HerbCardControl.xaml           # 统一的药材卡片控件
        ├── ShowPrice 依赖属性        # 是否显示价格
        ├── 药材名称 AutoSuggest
        ├── 剂量输入
        ├── 价格显示 (可选)
        └── 删除按钮

LYBT.Desktop.Infrastructure (共享层)
└── ViewModels/
    └── HerbItemViewModelBase.cs       # 药材项基类
        ├── HerbId, HerbName
        ├── Dosage, Unit
        ├── abstract UnitPrice         # 抽象属性
        └── FilteredHerbs, SelectedHerb
```

### 模块特化实现

```
LYBT.Desktop.Formula
└── FormulaHerbItemViewModel.cs
    └── override UnitPrice => 0m       # 经验方固定为0

LYBT.Desktop.MedicalCase
└── PrescriptionHerbItemViewModel.cs
    └── override UnitPrice => _herb.Price  # 处方使用实际价格
```

## Component Design

### HerbCardControl 控件

**新增依赖属性**:

| 属性名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| ShowPrice | bool | false | 是否显示价格列 |
| IsEditMode | bool | false | 是否可编辑 |
| DeleteCommand | ICommand | null | 删除命令 |
| DosageCompletedCommand | ICommand | null | 剂量完成命令 |
| AddNewRowCommand | ICommand | null | 添加新行命令 |

**布局结构**:

```
┌─────────────────────────────────────────────────────────┐
│ ┌──────────────┐ ┌────────────┐ ┌────────┐ ┌───┐       │
│ │ 药材名称     │ │ 剂量: 10 g │ │ ¥2.50  │ │ ✕ │       │
│ │ [AutoSuggest]│ │ [TextBox]  │ │(可选)  │ │   │       │
│ └──────────────┘ └────────────┘ └────────┘ └───┘       │
└─────────────────────────────────────────────────────────┘
```

**XAML 结构**:

```xaml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*"/>           <!-- 药材名称 -->
        <ColumnDefinition Width="Auto"/>        <!-- 剂量区域 -->
        <ColumnDefinition Width="Auto"/>        <!-- 价格区域(可选) -->
        <ColumnDefinition Width="Auto"/>        <!-- 删除按钮 -->
    </Grid.ColumnDefinitions>

    <!-- Column 0: 药材名称 AutoSuggest -->
    <!-- Column 1: 剂量输入 -->
    <!-- Column 2: 价格显示 (Visibility绑定ShowPrice) -->
    <!-- Column 3: 删除按钮 -->
</Grid>
```

### IHerbItem 接口 (已存在)

```csharp
public interface IHerbItem
{
    Guid HerbId { get; set; }
    string HerbName { get; set; }
    decimal Dosage { get; set; }
    string Unit { get; set; }
    decimal UnitPrice { get; }  // 只读属性
}
```

### HerbItemViewModelBase 基类

```csharp
public abstract class HerbItemViewModelBase : BindableBase, IHerbItem
{
    // 共享属性
    public Guid HerbId { get; set; }
    public string HerbName { get; set; }
    public decimal Dosage { get; set; }
    public string Unit { get; set; } = "g";

    // 抽象属性 - 子类实现
    public abstract decimal UnitPrice { get; }

    // 共享逻辑
    public ObservableCollection<HerbDto>? AllHerbs { get; set; }
    public ObservableCollection<HerbDto> FilteredHerbs { get; }
    public HerbDto? SelectedHerb { get; set; }

    protected void FilterHerbs() { /* 拼音码过滤 */ }
}
```

### 使用示例

**经验方 (不显示价格)**:

```xaml
<controls:HerbCardControl
    ShowPrice="False"
    IsEditMode="True"
    DeleteCommand="{Binding DeleteHerbCommand}"
    ... />
```

**处方 (显示价格)**:

```xaml
<controls:HerbCardControl
    ShowPrice="True"
    IsEditMode="True"
    DeleteCommand="{Binding DeleteHerbCommand}"
    ... />
```

## Data Flow

### 药材选择流程

```
用户输入药材名/拼音码
    ↓
HerbName 属性变更
    ↓
FilterHerbs() 执行拼音码匹配
    ↓
FilteredHerbs 更新 → Popup 显示建议列表
    ↓
用户选择药材 (键盘Enter / 鼠标点击)
    ↓
SelectedHerb 属性变更
    ↓
自动填充: HerbId, HerbName, Unit, UnitPrice
    ↓
焦点跳转到剂量输入框
```

### 价格计算流程 (仅处方)

```
PrescriptionHerbItemViewModel
    ├── UnitPrice = Herb.Price (从药材库获取)
    └── ItemTotal = Dosage * UnitPrice

PrescriptionEditorPanelViewModel
    ├── SingleDosagePrice = ΣItemTotal
    └── TotalPrice = SingleDosagePrice * DosageCount
```

## Migration Strategy

### Phase 1: 提取共享控件
1. 创建 `HerbItemViewModelBase` 基类
2. 复制 `HerbCardControl` 到共享层并添加 `ShowPrice` 属性
3. 经验方模块继续使用本地控件（避免回归）

### Phase 2: 处方模块迁移
1. 创建 `PrescriptionHerbItemViewModel` 继承基类
2. 更新 `PrescriptionEditorPanel` 使用共享控件
3. 实现价格显示逻辑

### Phase 3: 经验方模块迁移 (可选)
1. 更新 `FormulaHerbItemViewModel` 继承基类
2. 更新经验方控件引用共享控件
3. 删除本地控件副本

## Risks & Mitigations

| 风险 | 缓解措施 |
|------|----------|
| 共享控件依赖问题 | 最小化依赖，仅依赖 Infrastructure 层 |
| 经验方回归 | Phase 3 作为可选步骤，优先保证处方功能 |
| 价格精度问题 | 使用 decimal 类型，显示格式化为 F2 |

## References

- 现有 IHerbItem 接口: `LYBT.Desktop.Infrastructure/Interfaces/IHerbItem.cs`
- 经验方控件: `LYBT.Desktop.Formula/Controls/HerbCardControl.xaml`
- 处方面板: `LYBT.Desktop.MedicalCase/Controls/PrescriptionEditorPanel.xaml`
