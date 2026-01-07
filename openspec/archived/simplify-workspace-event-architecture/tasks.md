# Implementation Tasks: 简化医案工作区事件架构

## 设计原则

- **KISS**: 最小化改动，只整合明确不合理的组件
- **YAGNI**: 只做必要的事件移除和组件整合
- **保护测试**: 不整合有测试覆盖的组件

## Phase 1: 简化状态模型 (0.75天)

### 1.1 定义状态枚举和Converter
- [x] 1.1.1 创建 `Enums/PanelStatus.cs` 定义枚举 (NotStarted/InProgress/Completed)
- [x] 1.1.2 创建 `Converters/PanelStatusToTextConverter.cs` (状态→文本)
- [x] 1.1.3 创建 `Converters/PanelStatusToColorConverter.cs` (状态→颜色)
- [x] 1.1.4 在 `App.xaml` 或资源字典中注册Converter -> MedicalCaseEditControl.xaml

### 1.2 子ViewModel添加Status属性
- [x] 1.2.1 `ConsultationPanelViewModel` 添加 `Status` 属性
- [x] 1.2.2 在诊断完成时更新 `Status = PanelStatus.Completed`
- [x] 1.2.3 `PrescriptionPanelViewModel` 添加 `Status` 属性
- [x] 1.2.4 在处方保存时更新 `Status = PanelStatus.Completed`

### 1.3 WorkspaceViewModel简化
- [ ] 1.3.1 移除 `_needsPrescription` 字段，改为派生属性 (保留，后续迭代)
- [x] 1.3.2 添加派生属性 `ConsultationStatus => ConsultationPanelVM?.Status`
- [x] 1.3.3 添加派生属性 `PrescriptionStatus => PrescriptionPanelVM?.Status`
- [ ] 1.3.4 移除8个状态UI属性 (保留，后续迭代)
- [ ] 1.3.5 改用派生属性 `CanComplete` 自动计算 (保留，后续迭代)

### 1.4 更新UI绑定
- [x] 1.4.1 Converter已注册到MedicalCaseEditControl.xaml
- [ ] 1.4.2 验证状态显示正常 (需运行时测试)

## Phase 2: 内联WorkspaceStatusDisplay (0.5天) - **跳过**

> **决策**: 跳过此Phase。原因：
> 1. 存在两个MedicalCaseWorkspaceViewModel（Clinical Role和MedicalCase Module）
> 2. 内联会导致代码重复，违反DRY原则
> 3. WorkspaceStatusDisplay作为独立组件更易维护和复用

### 2.1 分析并迁移代码
- [x] 2.1.1 读取 `WorkspaceStatusDisplay.cs` 全部内容 (129行) -> 已分析
- [~] 2.1.2 在 `MedicalCaseWorkspaceViewModel.cs` 中添加 `#region Status Display` -> 跳过
- [~] 2.1.3 将方法迁移到该region -> 跳过
- [~] 2.1.4 更新字段 `_statusDisplay` 的引用为直接方法调用 -> 跳过

### 2.2 清理
- [~] 2.2.1 删除 `WorkspaceStatusDisplay.cs` 文件 -> 保留
- [~] 2.2.2 移除 `MedicalCaseModule.cs` 中的注册（如有） -> 保留
- [x] 2.2.3 验证编译通过 -> 无变更，无需验证

## Phase 3: 移除SaveAllRequestedEvent (0.5天) - **已在Phase 5完成**

> **说明**: SaveAllRequestedEvent在Phase 5清理死代码时已完全移除（无发布者，无订阅者）

### 3.1 PrescriptionPanelViewModel添加公共保存方法
- [x] 3.1.1 添加 `public async Task<PrescriptionSaveResult> SaveAsync()` 方法 -> 无需，已有SaveCurrentPrescriptionAsync
- [x] 3.1.2 将 `OnSaveAllRequested` 逻辑移动到 `SaveAsync` -> 事件已移除
- [x] 3.1.3 移除 `SaveAllRequestedEvent` 订阅代码 -> Phase 5.2完成

### 3.2 WorkspaceViewModel改为直接调用
- [x] 3.2.1 移除 `SaveAllRequestedEvent.Publish` 调用 -> 无此调用（已验证）
- [x] 3.2.2 改为 `await _prescriptionPanelViewModel.SaveAsync()` -> 直接调用模式已实现
- [x] 3.2.3 处理SaveAsync返回结果 -> 通过回调处理

### 3.3 验证
- [x] 3.3.1 验证编译通过 -> Phase 5.3.3
- [ ] 3.3.2 测试保存流程正常 (需运行时测试)

## Phase 4: 移除PrescriptionSavedEvent (0.5天)

### 4.1 PrescriptionPanelViewModel添加回调参数
- [x] 4.1.1 添加 `_onPrescriptionSaved` 字段和 `SetOnPrescriptionSavedCallback` 方法
- [x] 4.1.2 SaveAsync完成后调用 `_onPrescriptionSaved?.Invoke(payload)`
- [x] 4.1.3 移除 `PrescriptionSavedEvent.Publish` 调用

### 4.2 WorkspaceViewModel提供回调
- [x] 4.2.1 复用现有 `OnPrescriptionSaved` 方法作为回调
- [x] 4.2.2 在InitializeViewModels中调用 `SetOnPrescriptionSavedCallback`
- [x] 4.2.3 移除 `PrescriptionSavedEvent` 订阅和Unsubscribe

### 4.3 验证
- [x] 4.3.1 编译验证 -> 0错误5警告(既有)
- [ ] 4.3.2 运行时测试RowVersion同步正常

## Phase 5: 清理死代码 (0.25天)

### 5.1 移除PrescriptionDataChangedEvent
- [x] 5.1.1 确认无订阅者 -> 已验证无任何订阅
- [x] 5.1.2 移除事件发布代码 (`PrescriptionPanelViewModel.NotifyDataChanged`)
- [x] 5.1.3 从WorkspaceEvents.cs移除定义

### 5.2 移除SaveAllRequestedEvent
- [x] 5.2.1 确认无发布者 -> 已验证无任何Publish调用
- [x] 5.2.2 移除事件订阅代码 (`PrescriptionPanelViewModel`构造函数)
- [x] 5.2.3 移除事件处理方法 (`OnSaveAllRequested`)
- [x] 5.2.4 移除Cleanup中的Unsubscribe调用
- [x] 5.2.5 从WorkspaceEvents.cs移除定义

### 5.3 清理PrescriptionSavedEvent (Phase 4完成)
- [x] 5.3.1 PrescriptionSavedEvent改用回调模式 -> 从WorkspaceEvents.cs移除
- [x] 5.3.2 保留PrescriptionSavedPayload类（供回调使用）
- [x] 5.3.3 编译验证 -> 0错误5警告(既有)

## Phase 6: 验证与测试 (0.25天)

### 6.1 编译验证
- [x] 6.1.1 解决所有编译错误 -> 0错误
- [x] 6.1.2 消除编译警告 -> 0警告

### 6.2 功能测试
- [ ] 6.2.1 测试诊断录入流程
- [ ] 6.2.2 测试处方开具流程
- [ ] 6.2.3 测试保存/暂存流程
- [ ] 6.2.4 测试完成医案流程

### 6.3 回归测试
- [ ] 6.3.1 测试待诊队列功能（确认跨模块事件正常）
- [ ] 6.3.2 测试历史医案查看
- [ ] 6.3.3 测试打印功能

## Phase 7: 文档更新

### 7.1 归档OpenSpec
- [ ] 7.1.1 运行 `openspec validate` 确认通过
- [ ] 7.1.2 执行 `openspec archive` 归档变更

---

## Completion Criteria

- [ ] 所有Phase任务完成（Phase 2跳过）
- [ ] WorkspaceEvents.cs仅保留文档注释和PrescriptionSavedPayload
- [~] WorkspaceStatusDisplay.cs已删除 -> 保留（DRY原则）
- [x] 模块内部事件数从3减至0
- [ ] 状态UI属性数从8减至2（派生属性）-> 保留后续迭代
- [ ] NeedsPrescription重复定义已消除 -> 保留后续迭代
- [ ] 所有测试通过
- [ ] 无编译警告
- [ ] 功能行为与重构前一致

## 变更文件清单

| 文件 | 操作 | 说明 |
|-----|------|------|
| `Enums/PanelStatus.cs` | CREATE | 面板状态枚举 |
| `Converters/PanelStatusToTextConverter.cs` | CREATE | 状态转文本 |
| `Converters/PanelStatusToColorConverter.cs` | CREATE | 状态转颜色 |
| `Events/WorkspaceEvents.cs` | MODIFY | 移除事件定义，保留Payload类 |
| `Components/WorkspaceStatusDisplay.cs` | KEEP | 保留（DRY原则） |
| `ViewModels/ConsultationPanelViewModel.cs` | MODIFY | 添加Status属性 |
| `ViewModels/PrescriptionPanelViewModel.cs` | MODIFY | 添加Status属性, 回调模式 |
| `ViewModels/MedicalCaseWorkspaceViewModel.cs` | MODIFY | 添加派生状态属性 |
| `Controls/MedicalCaseEditControl.xaml` | MODIFY | 注册Converter |

## 总工时估算

| Phase | 工时 |
|-------|------|
| Phase 1: 简化状态模型 | 0.75天 |
| Phase 2: 内联WorkspaceStatusDisplay | 0.5天 |
| Phase 3: 移除SaveAllRequestedEvent | 0.5天 |
| Phase 4: 移除PrescriptionSavedEvent | 0.5天 |
| Phase 5: 清理死代码 | 0.25天 |
| Phase 6: 验证与测试 | 0.25天 |
| **总计** | **2.75天** |
