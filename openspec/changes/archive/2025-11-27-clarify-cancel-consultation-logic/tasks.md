# Tasks: clarify-cancel-consultation-logic

## Overview

| Phase | Description | Est. Time |
|-------|-------------|-----------|
| 1 | 移除独立取消按钮 | 15min |
| 2 | 实现统一离开弹窗 | 45min |
| 3 | 测试验证 | 20min |

**Total Estimated Time**: ~1.5h

---

## Phase 1: 移除独立取消按钮

### Task 1.1: 移除XAML中的取消按钮
**Priority**: P1
**Effort**: 10min
**Status**: Pending

- [ ] 从 `MedicalCaseWorkspaceView.xaml` 移除"取消看诊"按钮
- [ ] 调整底部操作栏布局

### Task 1.2: 移除ViewModel中的取消命令
**Priority**: P1
**Effort**: 5min
**Status**: Pending
**Dependencies**: Task 1.1

- [ ] 从 `MedicalCaseWorkspaceViewModel` 移除 `CancelConsultationCommand`
- [ ] 移除 `ExecuteCancelConsultation` 方法
- [ ] 移除相关私有字段

---

## Phase 2: 实现统一离开弹窗

### Task 2.1: 创建三选项对话框
**Priority**: P1
**Effort**: 20min
**Status**: Pending

- [ ] 创建 `LeaveConsultationDialog` 或使用现有对话框组件
- [ ] 实现三个按钮："暂存医案"、"取消医案"、"取消"
- [ ] 返回枚举值: `SaveDraft`, `CancelCase`, `Stay`

### Task 2.2: 修改返回患者列表逻辑
**Priority**: P1
**Effort**: 15min
**Status**: Pending
**Dependencies**: Task 2.1

- [ ] 修改 `NavigateBackCommand` 或相关导航方法
- [ ] 在导航前弹出离开确认对话框
- [ ] 根据用户选择执行对应操作

### Task 2.3: 修改退出登录逻辑
**Priority**: P1
**Effort**: 10min
**Status**: Pending
**Dependencies**: Task 2.1

- [ ] 在退出登录流程中检查是否有活跃医案
- [ ] 如有，弹出离开确认对话框
- [ ] 根据用户选择执行对应操作

---

## Phase 3: 测试验证

### Task 3.1: 手动测试场景
**Priority**: P1
**Effort**: 20min
**Status**: Pending
**Dependencies**: Phase 1, Phase 2

- [ ] 测试返回患者列表流程
  - [ ] 选择"暂存医案"：数据保存，返回列表
  - [ ] 选择"取消医案"：软删除，返回列表
  - [ ] 选择"取消"：继续停留
- [ ] 测试退出登录流程
  - [ ] 同上三种情况
- [ ] 测试暂存按钮（手动触发）
  - [ ] 验证数据正常保存

---

## Implementation Notes

### 修改的文件
- `MedicalCaseWorkspaceView.xaml` - 移除取消按钮
- `MedicalCaseWorkspaceViewModel.cs` - 移除取消命令，添加离开确认逻辑

### 保留的功能
- 暂存医案按钮（手动触发）
- `MedicalCaseLifecycleHandler.SaveDraftAsync()` - 暂存逻辑
- `MedicalCaseLifecycleHandler.CancelAsync()` - 取消逻辑（由离开弹窗调用）

### 验收标准
1. 取消按钮已移除
2. 返回/退出时弹出三选项对话框
3. 各选项行为正确
4. 暂存按钮正常工作
