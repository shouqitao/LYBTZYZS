# Implementation Tasks: 简化医案工作区事件架构

## 设计原则

- **KISS**: 最小化改动，只整合明确不合理的组件
- **YAGNI**: 只做必要的事件移除和组件整合
- **保护测试**: 不整合有测试覆盖的组件

## Phase 1: 简化状态模型 (0.75天)

### 1.1 定义状态枚举和Converter
- [ ] 1.1.1 创建 `Enums/PanelStatus.cs` 定义枚举 (NotStarted/InProgress/Completed)
- [ ] 1.1.2 创建 `Converters/PanelStatusToTextConverter.cs` (状态→文本)
- [ ] 1.1.3 创建 `Converters/PanelStatusToColorConverter.cs` (状态→颜色)
- [ ] 1.1.4 在 `App.xaml` 或资源字典中注册Converter

### 1.2 子ViewModel添加Status属性
- [ ] 1.2.1 `ConsultationPanelViewModel` 添加 `Status` 属性
- [ ] 1.2.2 在诊断完成时更新 `Status = PanelStatus.Completed`
- [ ] 1.2.3 `PrescriptionPanelViewModel` 添加 `Status` 属性
- [ ] 1.2.4 在处方保存时更新 `Status = PanelStatus.Completed`

### 1.3 WorkspaceViewModel简化
- [ ] 1.3.1 移除 `_needsPrescription` 字段，改为派生属性
- [ ] 1.3.2 添加派生属性 `ConsultationStatus => ConsultationPanelVM?.Status`
- [ ] 1.3.3 添加派生属性 `PrescriptionStatus => PrescriptionPanelVM?.Status`
- [ ] 1.3.4 移除8个状态UI属性
- [ ] 1.3.5 改用派生属性 `CanComplete` 自动计算

### 1.4 更新UI绑定
- [ ] 1.4.1 更新 `MedicalCaseWorkspaceView.xaml` 状态绑定使用Converter
- [ ] 1.4.2 验证状态显示正常

## Phase 2: 内联WorkspaceStatusDisplay (0.5天)

### 2.1 分析并迁移代码
- [ ] 2.1.1 读取 `WorkspaceStatusDisplay.cs` 全部内容 (129行)
- [ ] 2.1.2 在 `MedicalCaseWorkspaceViewModel.cs` 中添加 `#region Status Display`
- [ ] 2.1.3 将方法迁移到该region
- [ ] 2.1.4 更新字段 `_statusDisplay` 的引用为直接方法调用

### 2.2 清理
- [ ] 2.2.1 删除 `WorkspaceStatusDisplay.cs` 文件
- [ ] 2.2.2 移除 `MedicalCaseModule.cs` 中的注册（如有）
- [ ] 2.2.3 验证编译通过

## Phase 3: 移除SaveAllRequestedEvent (0.5天)

### 3.1 PrescriptionPanelViewModel添加公共保存方法
- [ ] 3.1.1 添加 `public async Task<PrescriptionSaveResult> SaveAsync()` 方法
- [ ] 3.1.2 将 `OnSaveAllRequested` 逻辑移动到 `SaveAsync`
- [ ] 3.1.3 移除 `SaveAllRequestedEvent` 订阅代码

### 3.2 WorkspaceViewModel改为直接调用
- [ ] 3.2.1 移除 `SaveAllRequestedEvent.Publish` 调用
- [ ] 3.2.2 改为 `await _prescriptionPanelViewModel.SaveAsync()`
- [ ] 3.2.3 处理SaveAsync返回结果

### 3.3 验证
- [ ] 3.3.1 验证编译通过
- [ ] 3.3.2 测试保存流程正常

## Phase 4: 移除PrescriptionSavedEvent (0.5天)

### 4.1 PrescriptionPanelViewModel添加回调参数
- [ ] 4.1.1 构造函数添加 `Action<PrescriptionSaveResult>? onSaved = null`
- [ ] 4.1.2 保存完成后调用 `_onSaved?.Invoke(result)`
- [ ] 4.1.3 移除 `PrescriptionSavedEvent.Publish` 调用

### 4.2 WorkspaceViewModel提供回调
- [ ] 4.2.1 创建 `OnPrescriptionSavedCallback` 方法
- [ ] 4.2.2 在初始化子VM时注入回调
- [ ] 4.2.3 移除 `PrescriptionSavedEvent` 订阅

### 4.3 验证
- [ ] 4.3.1 验证编译通过
- [ ] 4.3.2 测试RowVersion同步正常

## Phase 5: 清理死代码 (0.25天)

### 5.1 移除PrescriptionDataChangedEvent
- [ ] 5.1.1 确认无订阅者
- [ ] 5.1.2 移除事件发布代码 (`PrescriptionPanelViewModel.cs:640`)
- [ ] 5.1.3 从WorkspaceEvents.cs移除定义

### 5.2 删除WorkspaceEvents.cs
- [ ] 5.2.1 确认所有事件已移除
- [ ] 5.2.2 删除整个文件
- [ ] 5.2.3 移除相关using引用

## Phase 6: 验证与测试 (0.25天)

### 6.1 编译验证
- [ ] 6.1.1 解决所有编译错误
- [ ] 6.1.2 消除编译警告

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

- [ ] 所有Phase任务完成
- [ ] WorkspaceEvents.cs已删除
- [ ] WorkspaceStatusDisplay.cs已删除
- [ ] 模块内部事件数从3减至0
- [ ] 状态UI属性数从8减至2（派生属性）
- [ ] NeedsPrescription重复定义已消除
- [ ] 所有测试通过
- [ ] 无编译警告
- [ ] 功能行为与重构前一致

## 变更文件清单

| 文件 | 操作 | 说明 |
|-----|------|------|
| `Enums/PanelStatus.cs` | CREATE | 面板状态枚举 |
| `Converters/PanelStatusToTextConverter.cs` | CREATE | 状态转文本 |
| `Converters/PanelStatusToColorConverter.cs` | CREATE | 状态转颜色 |
| `Events/WorkspaceEvents.cs` | DELETE | 移除所有内部事件 |
| `Components/WorkspaceStatusDisplay.cs` | DELETE | 内联到VM |
| `ViewModels/ConsultationPanelViewModel.cs` | MODIFY | 添加Status属性 |
| `ViewModels/PrescriptionPanelViewModel.cs` | MODIFY | 添加Status属性, SaveAsync, 回调参数 |
| `ViewModels/MedicalCaseWorkspaceViewModel.cs` | MODIFY | 内联StatusDisplay，移除冗余状态属性 |
| `Views/MedicalCaseWorkspaceView.xaml` | MODIFY | 更新状态绑定使用Converter |

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
