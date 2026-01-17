# OpenSpec Proposal: 统一控件数据绑定架构

## 元信息

| 属性 | 值 |
|------|-----|
| 提案ID | unify-control-data-binding |
| 状态 | 待审批 |
| 创建日期 | 2026-01-16 |
| 优先级 | 高 |
| 预估工作量 | 10-15天 |

## 问题陈述

### 现状分析

项目前端存在**严重的DependencyProperty过度定义问题**：

| 指标 | 数值 |
|------|------|
| DependencyProperty总数 | 293个 |
| 涉及控件文件数 | 32个 |
| 单个控件最大属性数 | 26个 (MedicalCaseEditControl) |
| 属性超过15个的控件 | 8个 |

### 典型问题控件

| 控件 | 属性数 | 问题描述 |
|------|--------|----------|
| MedicalCaseEditControl | 26 | 患者信息4个+诊断4个+处方6个+模式3个+命令3个+系统2个+验证1个+兼容2个 |
| PatientViewControl | 23 | 23个业务字段各自定义为独立属性 |
| BaseMasterDataListView | 19 | 分页6个+数据3个+搜索2个+命令4个+状态2个+内容2个 |
| MedicalCaseViewControl | 17 | 与Edit类似的分散属性 |
| PatientSearchControl | 15 | 搜索+分页+命令各自分散 |
| PendingQueueControl | 12 | 队列状态+选项+命令分散 |

### 问题影响

1. **代码膨胀**: 每个属性需要8-10行代码（静态字段+CLR包装）
2. **维护困难**: 属性分散，难以理解业务逻辑分组
3. **重复定义**: 分页、加载状态等通用模式在多个控件重复
4. **类型不安全**: 分散属性难以进行类型验证
5. **测试困难**: 需要分别设置多个属性才能测试

## 解决方案

### 核心思想：对象化数据传递

用**聚合对象**替代**分散属性**，将相关属性封装为有意义的业务对象。

### 四类标准对象

#### 1. DisplayModel - 展示数据模型

**用途**: 封装只读展示数据（View控件使用）

**特征**:
- 纯POCO类，不继承ObservableObject
- 从DTO映射而来
- 包含计算属性用于UI格式化

**示例** (已有):
```csharp
public class PatientDisplayModel
{
    public string Name { get; set; } = string.Empty;
    public int? Age { get; set; }
    public string Gender { get; set; } = string.Empty;

    // 计算属性
    public string AgeDisplay => Age.HasValue ? $"{Age}岁" : "未知";
    public string BasicInfoSummary => $"{Name} {Gender} {AgeDisplay}";
}
```

#### 2. EditModel - 编辑数据模型

**用途**: 封装可编辑业务数据（Edit控件使用）

**特征**:
- 继承ObservableObject
- 使用[ObservableProperty]源生成属性
- 支持TwoWay绑定
- 包含验证逻辑

**示例**:
```csharp
public partial class ConsultationEditModel : ObservableObject
{
    [ObservableProperty] private string? _presentIllness;
    [ObservableProperty] private string? _tongueDiagnosis;
    [ObservableProperty] private string? _pulseDiagnosis;
    [ObservableProperty] private string? _tcmDiagnosis;

    public bool IsValid => !string.IsNullOrEmpty(TcmDiagnosis);

    public void Reset()
    {
        PresentIllness = null;
        TongueDiagnosis = null;
        PulseDiagnosis = null;
        TcmDiagnosis = null;
    }
}
```

#### 3. ViewState - 视图状态对象

**用途**: 封装UI状态（加载、分页、选择等）

**特征**:
- 继承ObservableObject
- 提供状态更新方法
- 可跨控件复用

**已有示例**: WorkspaceState, PatientViewState

**通用组件设计**:
```csharp
// 可复用的分页状态
public partial class PaginationState : ObservableObject
{
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalCount;

    public int TotalPages => PageSize > 0 ? (TotalCount + PageSize - 1) / PageSize : 1;
    public bool HasPrevious => CurrentPage > 1;
    public bool HasNext => CurrentPage < TotalPages;

    public void GoToPage(int page) => CurrentPage = Math.Clamp(page, 1, TotalPages);
    public void Reset() { CurrentPage = 1; TotalCount = 0; }
}

// 可复用的加载状态
public partial class LoadingState : ObservableObject
{
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _message;

    public void Start(string? message = "加载中...") { IsLoading = true; Message = message; }
    public void Stop() { IsLoading = false; Message = null; }
}
```

#### 4. ControlOptions - 控件配置选项

**用途**: 控件显示/行为配置

**特征**:
- 使用record类型
- 简洁不可变
- 提供默认值

**示例**:
```csharp
public record DisplayOptions(
    bool IsCompactMode = false,
    bool ShowHeader = true,
    bool ShowFooter = true
);

public record PaginationOptions(
    bool ShowPageSize = true,
    bool ShowTotalCount = true,
    int[] PageSizeOptions = null
)
{
    public int[] PageSizeOptions { get; init; } = PageSizeOptions ?? new[] { 10, 20, 50, 100 };
}
```

## 架构设计

### 目录结构

```
Infrastructure/
├── Models/
│   ├── Display/                    # 通用DisplayModel
│   │   ├── PatientDisplayModel.cs  # 已有
│   │   └── ...
│   ├── State/                      # 通用ViewState
│   │   ├── PaginationState.cs      # 新增
│   │   ├── LoadingState.cs         # 新增
│   │   ├── SearchState.cs          # 新增
│   │   └── SelectionState.cs       # 新增
│   └── Options/                    # 通用ControlOptions
│       ├── DisplayOptions.cs       # 新增
│       ├── PaginationOptions.cs    # 新增
│       └── ToolbarOptions.cs       # 新增

Modules/MedicalCase/
├── Models/
│   ├── Display/
│   │   └── MedicalCaseDisplayModel.cs
│   └── Edit/
│       ├── ConsultationEditModel.cs
│       └── PrescriptionEditModel.cs

Modules/Patients/
├── Models/
│   ├── Display/
│   │   └── PatientDetailDisplayModel.cs  # PatientViewControl使用
│   └── State/
│       └── PatientViewState.cs           # 已有
```

### 重构前后对比

#### MedicalCaseEditControl 重构示例

**Before (26个属性)**:
```csharp
public partial class MedicalCaseEditControl : UserControl
{
    // 显示模式 (1)
    public static readonly DependencyProperty IsCompactModeProperty = ...;

    // 患者信息 (4)
    public static readonly DependencyProperty PatientNameProperty = ...;
    public static readonly DependencyProperty ConsultationDateProperty = ...;
    public static readonly DependencyProperty DoctorNameProperty = ...;
    public static readonly DependencyProperty StatusProperty = ...;

    // 诊断信息 (4)
    public static readonly DependencyProperty PresentIllnessProperty = ...;
    public static readonly DependencyProperty TongueDiagnosisProperty = ...;
    public static readonly DependencyProperty PulseDiagnosisProperty = ...;
    public static readonly DependencyProperty TcmDiagnosisProperty = ...;

    // 处方信息 (6)
    public static readonly DependencyProperty HerbCountProperty = ...;
    public static readonly DependencyProperty DoseCountProperty = ...;
    public static readonly DependencyProperty FormulaSourceProperty = ...;
    public static readonly DependencyProperty AllHerbsProperty = ...;
    public static readonly DependencyProperty HerbItemsProperty = ...;
    public static readonly DependencyProperty UsageProperty = ...;

    // ... 更多属性
}
```

**After (6个属性)**:
```csharp
public partial class MedicalCaseEditControl : UserControl
{
    // 数据模型 (2)
    public static readonly DependencyProperty ConsultationProperty =
        DependencyProperty.Register(nameof(Consultation),
            typeof(ConsultationEditModel), typeof(MedicalCaseEditControl));

    public static readonly DependencyProperty PrescriptionProperty =
        DependencyProperty.Register(nameof(Prescription),
            typeof(PrescriptionEditModel), typeof(MedicalCaseEditControl));

    // 上下文数据 (2)
    public static readonly DependencyProperty PatientProperty =
        DependencyProperty.Register(nameof(Patient),
            typeof(PatientDisplayModel), typeof(MedicalCaseEditControl));

    public static readonly DependencyProperty AllHerbsProperty =
        DependencyProperty.Register(nameof(AllHerbs),
            typeof(IEnumerable), typeof(MedicalCaseEditControl));

    // 配置选项 (1)
    public static readonly DependencyProperty OptionsProperty =
        DependencyProperty.Register(nameof(Options),
            typeof(DisplayOptions), typeof(MedicalCaseEditControl),
            new PropertyMetadata(new DisplayOptions()));

    // 命令 (1) - 仅保留必要的
    public static readonly DependencyProperty CommandsProperty =
        DependencyProperty.Register(nameof(Commands),
            typeof(MedicalCaseCommands), typeof(MedicalCaseEditControl));
}
```

**XAML绑定变化**:
```xml
<!-- Before -->
<TextBlock Text="{Binding PatientName}"/>
<TextBox Text="{Binding PresentIllness, Mode=TwoWay}"/>

<!-- After -->
<TextBlock Text="{Binding Patient.Name}"/>
<TextBox Text="{Binding Consultation.PresentIllness, Mode=TwoWay}"/>
```

### 预期收益

| 指标 | Before | After | 改进 |
|------|--------|-------|------|
| DependencyProperty总数 | 293 | ~100 | -66% |
| 平均每控件属性数 | 9.2 | 3-4 | -60% |
| 代码行数估算 | ~2500行 | ~1000行 | -60% |
| 重复模式 | 分散在各控件 | 统一State/Options | 可复用 |

## 迁移策略

### 渐进式重构

采用**模块独立、逐步迁移**策略，确保系统稳定性。

### Phase A: 基础设施 (2天)

1. 创建通用State类 (PaginationState, LoadingState, SearchState)
2. 创建通用Options类 (DisplayOptions, PaginationOptions)
3. 更新CLAUDE.md文档规范

### Phase B: 高优先级控件 (5天)

按属性数量降序处理:

| 任务 | 控件 | 属性数 | 预估 |
|------|------|--------|------|
| B.1 | MedicalCaseEditControl | 26 | 1天 |
| B.2 | PatientViewControl | 23 | 1天 |
| B.3 | BaseMasterDataListView | 19 | 1天 |
| B.4 | MedicalCaseViewControl | 17 | 0.5天 |
| B.5 | HerbViewControl + HerbEditControl | 16+15 | 1天 |

### Phase C: 中优先级控件 (4天)

| 任务 | 控件 | 属性数 | 预估 |
|------|------|--------|------|
| C.1 | BaseDetailContainer | 15 | 0.5天 |
| C.2 | PatientSearchControl | 15 | 0.5天 |
| C.3 | PatientEditControl | 13 | 0.5天 |
| C.4 | FormulaEditControl | 12 | 0.5天 |
| C.5 | PendingQueueControl | 12 | 0.5天 |
| C.6 | UserEditControl + UserViewControl | 12+11 | 1天 |

### Phase D: 低优先级控件 (2天)

剩余控件按统一模式迁移。

## 验收标准

1. [ ] DependencyProperty总数降至100以下
2. [ ] 所有通用State/Options类创建完成
3. [ ] 高优先级控件(8个)全部重构完成
4. [ ] 编译通过，0错误0警告
5. [ ] 现有功能无回归
6. [ ] CLAUDE.md文档更新

## 风险与缓解

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|----------|
| XAML绑定路径变更导致运行时错误 | 高 | 中 | 每个控件重构后立即测试 |
| EditModel双向绑定失效 | 中 | 高 | 使用FrameworkPropertyMetadataOptions.BindsTwoWayByDefault |
| 设计时预览失效 | 低 | 低 | 保持DesignerProperties检查模式 |

## 相关OpenSpec

- `slim-workspace-viewmodel` - WorkspaceState设计参考
- `standardize-viewmodel-framework` - CommunityToolkit.Mvvm迁移
