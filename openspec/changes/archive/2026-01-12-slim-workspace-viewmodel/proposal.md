# slim-workspace-viewmodel

## 提案概述

**目标**: 将`MedicalCaseWorkspaceViewModel`从1491行精简到500行以下，遵循MVVM最佳实践。

**问题陈述**:
尽管经历了多次重构（`refactor-oversized-viewmodels`、`consolidate-panel-viewmodels`等），`MedicalCaseWorkspaceViewModel`仍有1491行代码，远超500行目标。根本原因是Handler委托不彻底——ViewModel中仍保留了大量应该由Handler处理的逻辑。

## 现状分析

### 代码分布（1491行）

| 区域 | 行数 | 占比 | 问题 |
|------|------|------|------|
| Properties | ~240 | 16% | 可用State对象聚合 |
| 待诊队列操作 | ~216 | 14% | 与Handler重复 |
| 处方编辑命令 | ~240 | 16% | 逻辑应委托Handler |
| INavigationAware | ~205 | 14% | 过于复杂 |
| 适配器类 | ~104 | 7% | 可以内联或提取 |
| 初始化/构造 | ~150 | 10% | 合理 |
| 诊断/基础 | ~336 | 23% | 部分可委托 |

### 已有Handler组件（共1440行）

| Handler | 行数 | 职责 |
|---------|------|------|
| WorkspacePendingQueueHandler | 363 | 待诊队列管理 |
| MedicalCaseEditModeStateMachine | 360 | 编辑模式状态机 |
| MedicalCaseWorkspaceCoordinator | 256 | 工作流协调 |
| PrescriptionPrintHandler | 232 | 处方打印 |
| WorkspaceStatusDisplay | 129 | 状态显示 |
| PrescriptionImportHandler | 100 | 处方导入 |

**关键发现**: Handler已存在但ViewModel仍有重复逻辑，委托不彻底。

## 设计模式研究

### 1. View Composition（视图组合）

MVVM的核心是组合。一个包含多个功能的屏幕应该由多个ViewModel组成：

```
MedicalCaseWorkspaceViewModel (聚合ViewModel)
├── PatientInfoViewModel (患者信息)
├── ConsultationViewModel (诊断编辑)
├── PrescriptionViewModel (处方编辑)
└── PendingQueueViewModel (待诊队列)
```

### 2. Handler完全委托

ViewModel职责应限于：
- UI状态绑定
- Command定义（实现委托给Handler）
- 子ViewModel协调

Handler职责：
- 业务逻辑
- 服务调用
- 数据转换

### 3. State对象聚合

将相关属性聚合到State类：

```csharp
// 替代20+个独立属性
public WorkspaceState State { get; }

public class WorkspaceState : BindableBase
{
    public bool IsBusy { get; set; }
    public bool IsReadOnly { get; set; }
    public string StatusMessage { get; set; }
    public PatientInfo PatientInfo { get; set; }
    // ...
}
```

## 重构方案

### Phase 1: 完善Handler委托（目标: 减少300行）

1. 将待诊队列操作完全委托给`WorkspacePendingQueueHandler`
2. 将处方编辑命令委托给新的`PrescriptionEditHandler`
3. 移除ViewModel中的重复逻辑

### Phase 2: State对象重构（目标: 减少150行）

1. 创建`WorkspaceState`类聚合UI状态属性
2. 创建`PatientDisplayInfo`聚合患者显示信息
3. 简化属性定义

### Phase 3: 简化导航生命周期（目标: 减少100行）

1. 提取导航逻辑到`WorkspaceNavigationHandler`
2. 简化`OnNavigatedTo`/`OnNavigatedFrom`实现
3. 移除内嵌适配器类

### Phase 4: 命令整合（目标: 减少100行）

1. 使用CompositeCommand聚合相关命令
2. 统一命令CanExecute逻辑
3. 移除冗余命令定义

## 预期成果

| 指标 | 重构前 | 重构后 |
|------|--------|--------|
| ViewModel行数 | 1491 | <500 |
| Handler行数 | 1440 | ~1800 |
| 总代码行数 | 2931 | ~2300 |
| 单一职责 | 违反 | 符合 |

## 风险评估

| 风险 | 概率 | 缓解措施 |
|------|------|----------|
| 绑定路径变更 | 高 | 使用属性包装器保持兼容 |
| 命令引用断裂 | 中 | 渐进式迁移 |
| Handler间耦合 | 低 | 使用EventAggregator通信 |

## 验收标准

1. [ ] ViewModel代码行数 < 500
2. [ ] 编译通过（0错误0警告）
3. [ ] 所有现有功能正常
4. [ ] XAML绑定无断裂
5. [ ] 无性能退化

---

**提案者**: Claude Code
**日期**: 2026-01-12
**状态**: Draft
