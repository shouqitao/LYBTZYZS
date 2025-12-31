# 技术设计: 简化医案工作区事件架构

## Context

### 背景
医案工作区是系统核心功能模块，负责诊断录入和处方开具。当前实现存在以下技术债务：

1. **事件滥用**: 在同一ViewModel树内使用跨模块事件机制进行父子通信
2. **过度拆分**: 部分组件过小（<150行），增加不必要的间接层
3. **死代码**: PrescriptionDataChangedEvent定义但无订阅者
4. **状态冗余**: 诊断/处方状态在多处重复定义和追踪

### 状态冗余详情

**当前状态属性分布**:
```
MedicalCaseWorkspaceViewModel (1201行)
│
├── 处方状态 (6个属性 - 冗余)
│   ├── ShowPrescriptionStatus
│   ├── PrescriptionStatusText = "待诊断"
│   ├── PrescriptionStatusBackground
│   ├── PrescriptionStatusSummary = "待开方"
│   ├── PrescriptionStatusSummaryColor
│   └── IsPrescriptionEnabled
│
├── 诊断状态 (2个属性 - 冗余)
│   ├── ConsultationStatusText = "未完成"
│   └── ConsultationStatusColor
│
└── NeedsPrescription (重复定义)
    └── 与ConsultationPanelVM重复

ConsultationPanelViewModel (337行)
└── NeedsPrescription (原始定义)
    └── NoPrescription派生属性
```

### 当前架构状态

**WorkspaceViewModel组件依赖分析**:
```
MedicalCaseWorkspaceViewModel (1201行, 15+构造函数参数)
│
├── 通过DI注入 (合理)
│   ├── MedicalCaseWorkspaceCoordinator (273行) - 聚合操作
│   ├── MedicalCaseDataLoader (193行) - 被3处使用
│   ├── MedicalCaseLifecycleHandler (363行) - 生命周期
│   ├── MedicalCaseNavigationHandler (229行) - 有测试
│   ├── EditModeStateMachine (367行) - 状态机
│   └── PrescriptionPrintHandler (155行) - 打印
│
├── 直接new()创建 (可内联)
│   ├── WorkspaceStatusDisplay (129行) ← 可内联
│   └── WorkspacePendingQueueHandler (375行) ← 保留(太大)
│
└── 子ViewModel
    ├── ConsultationPanelViewModel
    └── PrescriptionPanelViewModel
```

### 约束
- 必须保持现有UI交互行为不变
- 必须兼容Prism MVVM框架
- 重构过程中系统必须可用
- **KISS原则**: 最小化改动，只整合明确不合理的组件

## Goals / Non-Goals

### Goals
1. **移除内部事件** - 删除3个模块内部事件，改用直接调用/回调
2. **内联过小组件** - WorkspaceStatusDisplay内联到父VM
3. **简化通信** - 父→子用直接方法调用，子→父用Action回调
4. **简化状态** - 移除冗余状态属性，使用派生属性和Converter
5. **减少间接层** - 构造函数参数减少1-2个

### Non-Goals
- 不大规模拆分ViewModel（现有拆分已足够）
- 不创建新的接口或组件（除Converter外）
- 不改变业务逻辑
- 不整合有测试覆盖的组件（保护测试投资）

## Decisions

### Decision 1: 状态枚举化

**定义统一的面板状态枚举**:
```csharp
/// <summary>
/// 面板状态枚举
/// </summary>
public enum PanelStatus
{
    /// <summary>未开始</summary>
    NotStarted,
    /// <summary>进行中</summary>
    InProgress,
    /// <summary>已完成</summary>
    Completed
}
```

**子ViewModel各自维护状态**:
```csharp
// ConsultationPanelViewModel
public PanelStatus Status { get; private set; } = PanelStatus.NotStarted;

// PrescriptionPanelViewModel  
public PanelStatus Status { get; private set; } = PanelStatus.NotStarted;
```

### Decision 2: 移除重复的NeedsPrescription

**当前** (重复):
```csharp
// WorkspaceViewModel
private bool _needsPrescription = true;
public bool NeedsPrescription { get => _needsPrescription; set => ... }

// ConsultationPanelViewModel
private bool _needsPrescription = true;
public bool NeedsPrescription { get => _needsPrescription; set => ... }
```

**简化后** (单一来源):
```csharp
// WorkspaceViewModel - 直接从子VM读取
public bool NeedsPrescription => ConsultationPanelViewModel.NeedsPrescription;

// ConsultationPanelViewModel - 保持原样，作为数据源
public bool NeedsPrescription { get => _needsPrescription; set => ... }
```

### Decision 3: 简化状态UI属性

**当前** (8个属性):
```csharp
// 需要手动更新的6个处方状态属性
public bool ShowPrescriptionStatus { get; set; }
public string PrescriptionStatusText { get; set; } = "待诊断";
public Brush PrescriptionStatusBackground { get; set; }
public string PrescriptionStatusSummary { get; set; } = "待开方";
public Brush PrescriptionStatusSummaryColor { get; set; }
public bool IsPrescriptionEnabled { get; set; }

// 需要手动更新的2个诊断状态属性
public string ConsultationStatusText { get; set; } = "未完成";
public Brush ConsultationStatusColor { get; set; }
```

**简化后** (派生属性+Converter):
```csharp
// WorkspaceViewModel - 派生属性
public PanelStatus ConsultationStatus => ConsultationPanelViewModel?.Status ?? PanelStatus.NotStarted;
public PanelStatus PrescriptionStatus => PrescriptionPanelViewModel?.Status ?? PanelStatus.NotStarted;

// CanComplete 自动计算
public bool CanComplete => ConsultationStatus == PanelStatus.Completed 
    && (!NeedsPrescription || PrescriptionStatus == PanelStatus.Completed);
```

**UI绑定使用Converter**:
```xml
<TextBlock Text="{Binding ConsultationStatus, Converter={StaticResource PanelStatusToTextConverter}}"
           Foreground="{Binding ConsultationStatus, Converter={StaticResource PanelStatusToColorConverter}}"/>
```

### Decision 4: 组件整合策略

**整合原则**:
| 条件 | 决策 |
|-----|------|
| 行数<150 且 单一使用者 | 内联到使用者 |
| 有单元测试覆盖 | 保留独立 |
| 被多个组件使用 | 保留独立 |
| 行数>300 | 保留独立 |

**整合决策表**:
| 组件 | 行数 | 使用者 | 测试 | 决策 | 原因 |
|-----|------|--------|------|------|------|
| WorkspaceStatusDisplay | 129 | 1 | 无 | **内联** | 太小，无复用 |
| WorkspacePendingQueueHandler | 375 | 1 | 无 | 保留 | 代码量大 |
| MedicalCaseNavigationHandler | 229 | 1 | **有** | 保留 | 有测试覆盖 |
| MedicalCaseDataLoader | 193 | 3 | 无 | 保留 | 多处使用 |

### Decision 5: 移除SaveAllRequestedEvent，改为直接方法调用

**当前方式** (复杂):
```
WorkspaceVM ──[Publish SaveAllRequestedEvent]──► PrescriptionPanelVM
                                                    │
                                               OnSaveAllRequested()
```

**简化后** (直接调用):
```csharp
// WorkspaceViewModel中
private async Task ExecuteSaveAsync()
{
    var prescriptionResult = await _prescriptionPanelViewModel.SaveAsync();
}
```

### Decision 6: 移除PrescriptionSavedEvent，改为Action回调

**简化后** (回调注入):
```csharp
// PrescriptionPanelViewModel构造函数
public PrescriptionPanelViewModel(..., Action<PrescriptionSaveResult>? onSaved = null)
{
    _onSaved = onSaved;
}

// 保存完成后
private async Task ExecuteSaveAsync()
{
    var result = await _saveHandler.SaveAsync(...);
    _onSaved?.Invoke(result); // 直接回调
}
```

### Decision 7: 内联WorkspaceStatusDisplay

**当前**:
```csharp
// 独立文件: WorkspaceStatusDisplay.cs (129行)
public class WorkspaceStatusDisplay
{
    public void UpdateConsultationStatus(...) { ... }
    public void UpdatePrescriptionStatus(...) { ... }
}

// WorkspaceViewModel中
private readonly WorkspaceStatusDisplay _statusDisplay = new();
```

**简化后**:
```csharp
// 直接在WorkspaceViewModel中
#region Status Display Methods
private void UpdateConsultationStatus(...) { ... }
private void UpdatePrescriptionStatus(...) { ... }
#endregion
```

**原因**: 129行代码分离成独立文件增加了不必要的间接层，且无测试、无复用。

### Decision 8: 保留跨模块事件

**保留的事件**:
| 事件 | 用途 | 订阅者 |
|-----|------|--------|
| `CaseEvents.ConsultationCompletedEvent` | 诊断完成通知 | 待诊队列、其他模块 |
| `CaseEvents.PrescriptionCompletedEvent` | 处方完成通知 | 其他模块 |

## 简化后架构图

```
┌─────────────────────────────────────────────────────────────┐
│           MedicalCaseWorkspaceViewModel                     │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ 简化后的状态属性                                     │   │
│  │ - ConsultationStatus (派生自子VM)                   │   │
│  │ - PrescriptionStatus (派生自子VM)                   │   │
│  │ - NeedsPrescription (派生自ConsultationVM)          │   │
│  │ - CanComplete (派生属性，自动计算)                   │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ 内联的组件                                           │   │
│  │ - StatusDisplay方法 (原129行独立文件)                │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ 保留的DI组件 (有测试或多处使用)                       │   │
│  │ - Coordinator, DataLoader, LifecycleHandler         │   │
│  │ - NavigationHandler, StateMachine, PrintHandler     │   │
│  │ - PendingQueueHandler                               │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ 子ViewModel通信（简化后）                            │   │
│  │                                                     │   │
│  │  父→子: 直接方法调用 SaveAsync()                     │   │
│  │  子→父: Action回调 _onSaved?.Invoke()               │   │
│  │  跨模块: 保持PubSubEvent                            │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│  子ViewModel (各自维护状态)                                 │
│                                                             │
│  ConsultationPanelViewModel          PrescriptionPanelVM    │
│  ├── Status: PanelStatus             ├── Status: PanelStatus│
│  ├── NeedsPrescription (源)          └── SaveAsync()        │
│  └── ...                                                    │
└─────────────────────────────────────────────────────────────┘
```

## Risks / Trade-offs

| 风险 | 概率 | 影响 | 缓解措施 |
|-----|------|------|---------| 
| 内联增加VM行数 | 确定 | 低 | 仅+129行，用region组织 |
| 回调参数增加耦合 | 低 | 低 | 使用可选参数 |
| 派生属性性能 | 低 | 低 | 属性访问很快，无需缓存 |
| UI绑定变更 | 确定 | 中 | 统一使用Converter |

**Trade-off分析**:
- 选择简单性而非过度抽象
- 接受VM略增行数换取减少间接层
- 保护有测试覆盖的组件

## Migration Plan

### Step 1: 简化状态模型
1. 定义 `PanelStatus` 枚举
2. 创建 `PanelStatusToTextConverter` 和 `PanelStatusToColorConverter`
3. 子VM添加 `Status` 属性
4. WorkspaceVM 改用派生属性
5. 更新UI绑定使用Converter

### Step 2: 内联WorkspaceStatusDisplay
1. 将129行代码移动到WorkspaceViewModel
2. 使用 `#region Status Display` 组织
3. 删除独立文件
4. 更新相关引用

### Step 3: 移除SaveAllRequestedEvent
1. PrescriptionPanelViewModel添加public SaveAsync()
2. WorkspaceViewModel改为直接调用
3. 移除事件定义

### Step 4: 移除PrescriptionSavedEvent
1. PrescriptionPanelViewModel添加回调参数
2. 保存完成后调用回调
3. 移除事件定义

### Step 5: 清理
1. 删除WorkspaceEvents.cs
2. 删除WorkspaceStatusDisplay.cs
3. 移除未使用的using引用

## File Changes Summary

| 操作 | 文件 | 说明 |
|-----|------|------|
| CREATE | `Enums/PanelStatus.cs` | 面板状态枚举 |
| CREATE | `Converters/PanelStatusToTextConverter.cs` | 状态转文本 |
| CREATE | `Converters/PanelStatusToColorConverter.cs` | 状态转颜色 |
| DELETE | `Events/WorkspaceEvents.cs` | 移除所有内部事件 |
| DELETE | `Components/WorkspaceStatusDisplay.cs` | 内联到VM |
| MODIFY | `ViewModels/ConsultationPanelViewModel.cs` | 添加Status属性 |
| MODIFY | `ViewModels/PrescriptionPanelViewModel.cs` | 添加Status属性, SaveAsync, 回调参数 |
| MODIFY | `ViewModels/MedicalCaseWorkspaceViewModel.cs` | 内联StatusDisplay，改用派生属性 |
| MODIFY | `Views/MedicalCaseWorkspaceView.xaml` | 更新状态绑定使用Converter |

## Success Metrics

| 指标 | 当前 | 目标 |
|------|------|------|
| 模块内部事件数 | 3 | 0 |
| 独立组件文件数 | 11 | 10 (-1) |
| 状态UI属性数 | 8 | 2 (派生) |
| 重复定义属性 | 1 (NeedsPrescription) | 0 |
| WorkspaceVM行数 | 1201 | ~1200 (+内联-状态属性-事件代码) |
| 构造函数参数数 | 15+ | 14+ (-1) |
| 预计工时 | - | 2.5天 |
