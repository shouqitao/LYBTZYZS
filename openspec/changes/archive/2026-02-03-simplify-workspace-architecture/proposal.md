# simplify-workspace-architecture

## 概述

简化MedicalCaseWorkspace架构设计，消除过度设计，将11个类精简为5个核心类。

## 背景

### 前序提案
- `slim-workspace-viewmodel` (2026-01-12归档) - 尝试通过State对象和Handler委托减少行数
- 结果：仅减少104行(7%)，未达预期目标

### 核心问题

当前MedicalCaseWorkspace包含11个类，总计~3000行代码：

| 类 | 行数 | 问题 |
|----|------|------|
| MedicalCaseWorkspaceViewModel | 1393 | 过度膨胀 |
| WorkspaceState | 217 | 必要但可精简 |
| WorkspaceStatusDisplay | 130 | 可合并 |
| MedicalCaseEditModeStateMachine | 361 | 必要 |
| MedicalCaseWorkspaceCoordinator | 257 | 必要 |
| WorkspacePendingQueueHandler | 363 | **过度设计** |
| MedicalCaseNavigationHandler | 230 | **过度设计** |
| MedicalCaseDataLoader | 195 | 可合并 |
| PrescriptionPrintHandler | 233 | 必要 |
| PrescriptionImportHandler | 101 | **过于简单** |
| DataProviderAdapters | 112 | **过度设计** |

### 过度设计分析

1. **回调委托模式滥用**
   - WorkspacePendingQueueHandler使用6个回调委托
   - MedicalCaseNavigationHandler使用5个回调委托
   - 增加间接层，降低可读性

2. **适配器模式过度抽象**
   - 4个DataProviderAdapter类只是简单包装
   - ConsultationItem/PrescriptionItem可直接实现接口

3. **类职责过于细碎**
   - PrescriptionImportHandler只有2个方法
   - WorkspaceStatusDisplay只计算颜色

## 目标

| 指标 | 当前 | 目标 |
|------|------|------|
| 类数量 | 11个 | 5个 |
| 总代码行数 | ~3000行 | ~1600行 |
| ViewModel行数 | 1393行 | <500行 |
| 回调委托数 | 11个 | 0个 |

## 解决方案

### 目标架构

```
MedicalCaseWorkspaceViewModel (<500行)
├── WorkspaceState (~250行) [合并StatusDisplay]
├── MedicalCaseEditModeStateMachine (~300行)
├── MedicalCaseCoordinator (~350行) [合并DataLoader]
└── PrescriptionPrintHandler (~230行)
```

### 删除/合并策略

| 原类 | 处理方式 | 理由 |
|------|----------|------|
| WorkspacePendingQueueHandler | 回归ViewModel | 待诊队列是UI交互，ViewModel职责 |
| MedicalCaseNavigationHandler | 回归ViewModel | 导航是ViewModel核心职责 |
| MedicalCaseDataLoader | 合并到Coordinator | 数据加载与业务协调紧密相关 |
| WorkspaceStatusDisplay | 合并到WorkspaceState | 状态显示是状态的一部分 |
| PrescriptionImportHandler | 扩展方法 | 只有2个DTO转换方法 |
| DataProviderAdapters | Item直接实现接口 | 消除适配器间接层 |

### 关键设计变更

1. **ConsultationItem/PrescriptionItem直接实现IDataProvider+IValidatable**
   - 消除4个适配器类
   - 简化数据收集流程

2. **待诊队列逻辑回归ViewModel**
   - 消除回调委托
   - 直接操作UI状态

3. **导航逻辑回归ViewModel**
   - BackCommand直接处理
   - 对话框逻辑内联

4. **状态显示合并到WorkspaceState**
   - 计算属性统一管理
   - 减少组件数量

## 影响范围

### 修改文件
- `MedicalCaseWorkspaceViewModel.cs` - 重构
- `WorkspaceState.cs` - 合并StatusDisplay
- `MedicalCaseWorkspaceCoordinator.cs` - 合并DataLoader
- `ConsultationItem.cs` - 实现IDataProvider
- `PrescriptionItem.cs` - 实现IDataProvider

### 删除文件
- `WorkspacePendingQueueHandler.cs`
- `MedicalCaseNavigationHandler.cs`
- `MedicalCaseDataLoader.cs`
- `WorkspaceStatusDisplay.cs`
- `PrescriptionImportHandler.cs`
- `DataProviderAdapters.cs`

### 保持不变
- `MedicalCaseEditModeStateMachine.cs`
- `PrescriptionPrintHandler.cs`

## 验收标准

- [ ] 类数量从11个减少到5个
- [ ] ViewModel行数 < 500行
- [ ] 消除所有回调委托
- [ ] 编译通过
- [ ] 现有功能不受影响

## 风险

| 风险 | 概率 | 缓解措施 |
|------|------|----------|
| 功能回归 | 中 | 逐步重构，每步验证 |
| Item实现接口影响其他模块 | 低 | 接口在MedicalCase模块内部 |
| ViewModel职责过重 | 低 | 保留核心委托(Coordinator/StateMachine) |

## 时间估算

| Phase | 时间 |
|-------|------|
| Phase 1: Item实现接口 | 1小时 |
| Phase 2: 合并DataLoader到Coordinator | 1小时 |
| Phase 3: 合并StatusDisplay到State | 30分钟 |
| Phase 4: 回归待诊队列逻辑 | 1.5小时 |
| Phase 5: 回归导航逻辑 | 1小时 |
| Phase 6: 清理和验证 | 30分钟 |
| **总计** | **5.5小时** |
