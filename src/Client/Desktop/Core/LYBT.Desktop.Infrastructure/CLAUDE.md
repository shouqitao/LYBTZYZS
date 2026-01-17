# LYBT.Desktop.Infrastructure 模块说明

## XAML资源加载顺序规则

**重要**: WPF资源字典中的样式定义顺序敏感，`BasedOn`引用的样式必须在被引用之前定义。

### 已知问题及解决方案

1. **BaseDataGridCell 前向引用问题** (2026-01-04修复)
   - 问题: `MasterDetailDataGridCellStyle` 使用 `BasedOn="{StaticResource BaseDataGridCell}"`，但 `BaseDataGridCell` 定义在后面
   - 解决: 将 `BaseDataGridCell` 移到 `MasterDetailDataGridCellStyle` 之前

2. **跨文件资源引用问题** (2026-01-04修复)
   - 问题: `ValidationStyles.xaml` 中的 `ValidatingTextBoxStyle` 继承自 `EditableTextBoxStyle`，但 `ValidationStyles.xaml` 在 `UnifiedComponents.xaml` 开头被合并
   - 解决: 将 `ValidatingTextBoxStyle` 迁移到 `UnifiedComponents.xaml`，放在 `EditableTextBoxStyle` 之后

### 资源文件结构

```
Themes/
├── UnifiedComponents.xaml  # 主资源字典，合并其他资源
│   ├── 合并 ValidationStyles.xaml (开头)
│   ├── EditableTextBoxStyle (第412行)
│   ├── ValidatingTextBoxStyle (第478行，继承EditableTextBoxStyle)
│   ├── BaseDataGridCell (第633行)
│   └── MasterDetailDataGridCellStyle (第658行，继承BaseDataGridCell)
└── ValidationStyles.xaml   # 验证相关样式（基础样式，无继承依赖）
```

### 添加新样式的规则

1. 如果新样式使用 `BasedOn` 继承，确保基类样式已在前面定义
2. 跨文件继承时，检查资源字典合并顺序
3. 优先在 `UnifiedComponents.xaml` 中定义有继承关系的样式

---

## Mapperly 直接映射架构 (OpenSpec: standardize-api-architecture)

### 架构演进 (2026-01-07)

**已删除**: `IMappingService<TDto, TInputDto, TItem>` 接口和 `MappingServiceBase` 基类
- 原因: MappingService是Mapper的薄包装层，增加了不必要的间接性
- 方案: ViewModel直接实例化Mapper，无需DI注入

### 当前模式

**直接Mapper实例化 (唯一推荐模式)**
```csharp
public class XXXMasterDetailViewModel
{
    // 直接实例化，无需DI
    private readonly XXXMapper _mapper = new();

    // 加载时
    var item = _mapper.ToItem(dto);

    // 保存时
    var inputDto = _mapper.ToInputDto(item);
}
```

### 各模块 Mapper 位置

| 模块 | Mapper 类 | 位置 |
|------|-----------|------|
| Herbs | HerbMapper | `Mappers/HerbMapper.cs` |
| Formula | FormulaMapper, FormulaDetailModelMapper | `Mappers/` |
| MedicalCase | MedicalCaseDetailModelMapper | `Mappers/` |
| Patients | PatientMapper | `Mappers/PatientMapper.cs` |
| Users | UserMapper | `Mappers/UserMapper.cs` |

### 已废弃的 FromDto/ToDto 方法

所有 Item 类中的静态 `FromDto()` 和实例 `ToDto()` 方法已标记 `[Obsolete]`：
- 请使用对应模块的 `XXXMapper.ToItem()` / `ToDto()` / `ToInputDto()` 替代
- 这些方法将在后续版本移除

### Mapperly + CommunityToolkit.Mvvm 源生成器兼容性

**重要**: Mapperly源生成器与CommunityToolkit.Mvvm的`[ObservableProperty]`存在编译顺序冲突。

**问题**: Mapperly在编译时验证属性存在性，但`[ObservableProperty]`生成的属性尚未生成，导致RMG005/RMG006错误。

**解决方案**: 对于源生成属性，使用`[MapperIgnore*]`忽略，在包装方法中手动映射：

```csharp
// 错误模式（编译失败）
[MapProperty(nameof(Dto.CaseStatus), "CaseStatus")]
public partial Item ToItemCore(Dto dto);

// 正确模式
[MapperIgnoreTarget("CaseStatus")]  // 字符串字面量
[MapperIgnoreSource(nameof(Dto.CaseStatus))]
public partial Item ToItemCore(Dto dto);

public Item ToItem(Dto dto)
{
    var item = ToItemCore(dto);
    item.CaseStatus = dto.CaseStatus;  // 手动映射
    return item;
}
```

**详细说明**: 参见 `MedicalCase/CLAUDE.md` 的"Mapperly与CommunityToolkit.Mvvm源生成器兼容性"章节

---

## XAML 绑定最佳实践 (OpenSpec: fix-elementname-binding-architecture)

### WPF NameScope 机制

**关键概念**: WPF 的 `ContentPresenter` 会创建独立的 NameScope，导致其内部的 `ElementName` 绑定无法解析父级控件。

**失败场景**:
```xml
<UserControl x:Name="Root">  <!-- NameScope #1 -->
  <MasterDetailLayout>
    <MasterDetailLayout.DetailContent>
      <!-- ContentPresenter 创建 NameScope #2 -->
      <SomeControl Prop="{Binding X, ElementName=Root}"/>  <!-- 失败! Root 在 NameScope #1 中 -->
    </MasterDetailLayout.DetailContent>
  </MasterDetailLayout>
</UserControl>
```

### 三种绑定模式使用场景

| 模式 | 适用场景 | 示例 |
|------|----------|------|
| **DataContext 绑定** | 绑定 ViewModel 属性（推荐默认模式） | `{Binding PropertyName}` |
| **ElementName 绑定** | 同一 NameScope 内控件间绑定 | `{Binding Width, ElementName=OtherControl}` |
| **RelativeSource 绑定** | 需要向上查找祖先元素 | `{Binding DataContext.Prop, RelativeSource={RelativeSource AncestorType=UserControl}}` |

### MasterDetailLayout 内的正确绑定模式

**ContentPresenter 内容区域** (MasterContent/DetailContent/EmptyContent):
- ✅ **使用 DataContext 绑定**: `{Binding ViewModel.Property}`
- ❌ **禁止 ElementName=Root 绑定**: 因 NameScope 隔离会失败

**控件根级别属性**:
- ✅ **使用 ElementName 绑定**: 同一 NameScope，可正常工作
- 示例: SidebarControl 使用 `ElementName=Root` 绑定自身 DependencyProperty

### DataContext 透传模式

**原理**: 子控件自动继承父控件的 DataContext，无需显式传递。

**实现**:
```xml
<!-- PatientSelectionView.xaml -->
<!-- DataContext 由 Prism 自动注入 PatientSelectionViewModel -->
<patientControls:PatientSelectionControl
    Grid.Row="1"
    Margin="20"
    PatientDoubleClicked="..."/>
<!-- 无需显式绑定属性，控件内部直接绑定 DataContext -->
```

```xml
<!-- PatientSelectionControl.xaml 内部 -->
<DataGrid ItemsSource="{Binding Patients}"/>  <!-- 直接绑定 ViewModel 属性 -->
```

### 参考实现

- **正确模式**: `PatientMasterDetailControl.xaml` - 直接 DataContext 绑定
- **正确模式**: `SidebarControl.xaml` - 根级 ElementName 绑定（不跨 ContentPresenter）
- **已修复**: `PatientSelectionControl.xaml` - 原 ElementName=Root 已改为 DataContext 绑定

---

## 对象化数据绑定规范 (OpenSpec: unify-control-data-binding)

### 核心理念

用**聚合对象**替代**分散DependencyProperty**，将相关属性封装为有意义的业务对象。

**目标**: 将293个DependencyProperty减少至约100个（-66%）

### 四种标准对象类型

| 类型 | 用途 | 继承 | 特征 |
|------|------|------|------|
| **DisplayModel** | 只读展示数据 | 无（POCO） | 从DTO映射，包含计算属性用于格式化 |
| **EditModel** | 可编辑业务数据 | ObservableObject | 使用[ObservableProperty]，支持TwoWay绑定 |
| **ViewState** | UI状态管理 | ObservableObject | 可跨控件复用（分页、加载、搜索状态） |
| **ControlOptions** | 控件配置选项 | record类型 | 不可变，提供默认值 |

### 目录结构

```
Infrastructure/Models/
├── Display/                    # 通用DisplayModel
│   └── PatientDisplayModel.cs  # 已存在
├── State/                      # 通用ViewState（待创建）
│   ├── PaginationState.cs      # 分页状态
│   ├── LoadingState.cs         # 加载状态
│   └── SearchState.cs          # 搜索状态
└── Options/                    # 通用ControlOptions（待创建）
    ├── DisplayOptions.cs       # 显示选项
    └── PaginationOptions.cs    # 分页选项

Modules/<ModuleName>/Models/
├── Display/                    # 模块特定DisplayModel
│   └── XXXDisplayModel.cs
└── Edit/                       # 模块特定EditModel
    └── XXXEditModel.cs
```

### 代码示例

**DisplayModel（只读展示）**:
```csharp
public class PatientDisplayModel
{
    public string Name { get; set; } = string.Empty;
    public int? Age { get; set; }

    // 计算属性用于UI格式化
    public string AgeDisplay => Age.HasValue ? $"{Age}岁" : "未知";
}
```

**EditModel（可编辑）**:
```csharp
public partial class ConsultationEditModel : ObservableObject
{
    [ObservableProperty] private string? _presentIllness;
    [ObservableProperty] private string? _tcmDiagnosis;

    public bool IsValid => !string.IsNullOrEmpty(TcmDiagnosis);
    public void Reset() { PresentIllness = null; TcmDiagnosis = null; }
}
```

**ViewState（UI状态）**:
```csharp
public partial class PaginationState : ObservableObject
{
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalCount;

    public int TotalPages => PageSize > 0 ? (TotalCount + PageSize - 1) / PageSize : 1;
    public bool HasPrevious => CurrentPage > 1;
    public bool HasNext => CurrentPage < TotalPages;
}
```

**ControlOptions（配置选项）**:
```csharp
public record DisplayOptions(
    bool IsCompactMode = false,
    bool ShowHeader = true,
    bool ShowFooter = true
);
```

### XAML绑定迁移

**Before（分散属性）**:
```xml
<local:MedicalCaseEditControl
    PatientName="{Binding PatientName}"
    PresentIllness="{Binding PresentIllness, Mode=TwoWay}"
    TcmDiagnosis="{Binding TcmDiagnosis, Mode=TwoWay}"/>
```

**After（对象化绑定）**:
```xml
<local:MedicalCaseEditControl
    Patient="{Binding Patient}"
    Consultation="{Binding Consultation}"/>

<!-- 控件内部绑定 -->
<TextBlock Text="{Binding Patient.Name}"/>
<TextBox Text="{Binding Consultation.PresentIllness, Mode=TwoWay}"/>
```

### 参考实现

- **WorkspaceState** (`MedicalCase/ViewModels/Components/WorkspaceState.cs`) - ViewState模式
- **PatientDisplayModel** (`Infrastructure/Models/Display/PatientDisplayModel.cs`) - DisplayModel模式
- **PatientViewState** (`Patients/ViewModels/Components/PatientViewState.cs`) - ViewState模式

### 详细设计文档

完整的架构设计和任务分解见 OpenSpec:
- `openspec/changes/unify-control-data-binding/proposal.md` - 完整提案
- `openspec/changes/unify-control-data-binding/design.md` - 详细设计
- `openspec/changes/unify-control-data-binding/tasks.md` - 任务分解
