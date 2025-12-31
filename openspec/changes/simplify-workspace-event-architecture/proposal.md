# Change: 简化医案工作区事件架构

## Why

当前医案工作区(MedicalCaseWorkspace)存在以下问题：

1. **事件滥用** - 在同一ViewModel树内使用跨模块事件机制（PubSubEvent）进行父子通信
2. **过度拆分** - 部分组件过小（<150行），增加不必要的间接层和构造函数参数
3. **死代码** - PrescriptionDataChangedEvent定义但无订阅者
4. **状态冗余** - 诊断/处方状态在多处重复定义和追踪

**状态冗余分析**:
- `NeedsPrescription` 在 WorkspaceVM 和 ConsultationVM 重复定义
- WorkspaceVM 有8个状态UI属性（6个处方+2个诊断）维护成本高
- `WorkspaceStatusDisplay` 组件仅做双重存储，无实际价值
- 状态更新通过事件驱动分散在多个方法，难以追踪

**经过分析**:
- WorkspaceViewModel已有充分的组件拆分（Coordinator、DataLoader、LifecycleHandler等）
- 问题核心是事件通信模式、状态冗余和少量过小组件
- 有测试覆盖的组件应保留独立

## What Changes

### 核心原则
- **KISS**: 最小化改动
- **保护测试**: 不整合有测试覆盖的组件
- **务实**: 只整合明确不合理的组件

### Phase 1: 简化状态模型
- [ ] 定义 `PanelStatus` 枚举 (NotStarted/InProgress/Completed)
- [ ] ConsultationPanelVM 和 PrescriptionPanelVM 各自维护自己的 `Status` 属性
- [ ] 移除 WorkspaceVM 中重复的 `NeedsPrescription`，改为从 ConsultationVM 读取
- [ ] 移除8个状态UI属性，改为派生属性+ValueConverter

### Phase 2: 内联过小组件
- [ ] **WorkspaceStatusDisplay** (129行) 内联到WorkspaceVM
- [x] 分析确认其他组件保留（有测试或多处使用）

### Phase 3: 移除SaveAllRequestedEvent
- [ ] PrescriptionPanelViewModel添加公共 `SaveAsync()` 方法
- [ ] WorkspaceViewModel改为直接调用
- [ ] 移除事件定义

### Phase 4: 移除PrescriptionSavedEvent
- [ ] PrescriptionPanelViewModel构造函数添加 `Action<T>` 回调参数
- [ ] 保存完成后调用回调
- [ ] WorkspaceViewModel提供回调实现
- [ ] 移除事件定义

### Phase 5: 清理死代码
- [ ] 移除PrescriptionDataChangedEvent（无订阅者）
- [ ] 删除WorkspaceEvents.cs文件

## Impact

### Affected Specs
- `desktop-medicalcase` - 医案模块通信模式和组件结构

### Affected Code
| 文件 | 变更类型 | 说明 |
|-----|---------|------|
| `Events/WorkspaceEvents.cs` | DELETE | 移除所有内部事件 |
| `Components/WorkspaceStatusDisplay.cs` | DELETE | 内联到VM |
| `ViewModels/PrescriptionPanelViewModel.cs` | MODIFY | 添加Status属性, SaveAsync, 回调参数 |
| `ViewModels/ConsultationPanelViewModel.cs` | MODIFY | 添加Status属性 |
| `ViewModels/MedicalCaseWorkspaceViewModel.cs` | MODIFY | 内联StatusDisplay，移除冗余状态属性，直接调用 |
| `Converters/PanelStatusConverter.cs` | CREATE | 状态枚举到UI属性的转换器 |

### Breaking Changes
- **BREAKING**: `SaveAllRequestedEvent` 将被移除
- **BREAKING**: `PrescriptionSavedEvent` 将被移除
- **BREAKING**: `PrescriptionDataChangedEvent` 将被移除
- **BREAKING**: WorkspaceVM 的8个状态UI属性将被简化

### Migration
1. 事件发布代码改为直接方法调用
2. 事件订阅代码改为回调注入
3. UI绑定改用Converter绑定到子VM的Status属性

## 组件分析结果

| 组件 | 行数 | 使用者 | 测试 | 决策 | 原因 |
|-----|------|--------|------|------|------|
| WorkspaceStatusDisplay | 129 | 1 | 无 | **内联** | 太小，无复用 |
| WorkspacePendingQueueHandler | 375 | 1 | 无 | 保留 | 代码量大 |
| MedicalCaseNavigationHandler | 229 | 1 | **有** | 保留 | 有测试覆盖 |
| MedicalCaseDataLoader | 193 | 3 | 无 | 保留 | 多处使用 |
| MedicalCaseWorkspaceCoordinator | 273 | 1 | 无 | 保留 | 核心协调器 |

## 状态冗余分析结果

| 冗余项 | 当前位置 | 简化方案 |
|--------|----------|----------|
| NeedsPrescription | WorkspaceVM + ConsultationVM | 仅保留ConsultationVM，WorkspaceVM通过引用读取 |
| 8个状态UI属性 | WorkspaceVM | 改为2个派生属性+Converter |
| WorkspaceStatusDisplay | 独立组件 | 删除，逻辑内联 |
| CanComplete手动更新 | WorkspaceVM | 改为派生属性自动计算 |

## Success Metrics

| 指标 | 当前 | 目标 |
|------|------|------|
| 模块内部事件数 | 3 | 0 |
| 独立组件文件数 | 11 | 10 (-1) |
| 状态UI属性数 | 8 | 2 (派生) |
| 重复定义属性 | 1 | 0 |
| 新增组件数 | - | 1 (Converter) |

## Dependencies

无外部依赖。

## Risks

| 风险 | 影响 | 缓解措施 |
|-----|------|---------| 
| 内联增加VM行数 | 低 | 用region组织，仅+129行 |
| 重构引入bug | 中 | 分步实施，每步验证 |

## Timeline Estimate

| Phase | 工时 |
|-------|------|
| Phase 1: 内联WorkspaceStatusDisplay | 0.5天 |
| Phase 2: 移除SaveAllRequestedEvent | 0.5天 |
| Phase 3: 移除PrescriptionSavedEvent | 0.5天 |
| Phase 4: 清理死代码 | 0.25天 |
| 验证测试 | 0.25天 |
| **总计** | **2天** |
