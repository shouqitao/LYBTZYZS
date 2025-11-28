# medicalcase-management-ui-refactor

## Why

当前医案工作区界面存在以下问题：

1. **按钮布局不合理**：Management模式下"编辑医案"按钮在底部，用户需要先看完整个界面才能发现编辑入口
2. **保存按钮位置**：底部操作栏"打印处方笺"在最右侧，但"保存"操作更常用，应该在最右侧
3. **缺少未保存提示**：编辑状态下点击返回，没有提示用户保存修改
4. **审计集成不完善**：Management模式下修改历史医案没有强制要求填写修改原因

## What Changes

### UI布局调整
- 右上角增加"编辑医案"按钮（Management只读模式显示）
- 底部操作栏调整：[打印处方笺] [保存医案] → 保存在最右侧

### 交互流程优化
- Management模式默认进入只读状态（当前是编辑状态）
- 编辑状态返回时显示三选项对话框：保存修改/放弃修改/取消
- 只读状态返回时直接返回列表

### 审计集成
- 智能审计判断：根据医案状态、修改人、时间间隔决定是否需要审计理由
- Management模式保存时强制弹出审计理由对话框

## Scope

### In Scope
- MedicalCaseWorkspaceView.xaml 布局调整
- MedicalCaseWorkspaceViewModel.cs 状态管理优化
- 审计理由对话框 (AuditReasonDialog)
- 审计判断逻辑 (IAuditRequirementChecker)

### Out of Scope
- 审计日志持久化（已由global-audit规范定义）
- Clinical模式行为变更（保持现有逻辑）
- 后端API变更

## Related

- **Modifies**: `medicalcase-edit-modes` spec (EDITMODE-002, EDITMODE-004)
- **Integrates**: `global-audit` spec
