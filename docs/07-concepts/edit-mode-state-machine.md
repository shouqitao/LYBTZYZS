---
type: concept
title: 医案编辑状态机 (EditModeStateMachine)
tags: [state-machine, medical-case, mvvm]
related: [medical-case-module, workspace-modes]
created: 2026-06-10
updated: 2026-06-10
sources: ["docs/03-architecture/desktop.md"]
---
# 医案编辑状态机 (EditModeStateMachine)

**医案编辑状态机**是 Desktop 端医案模块的核心交互逻辑组件，用于管理医案工作区从只读到编辑、保存及离开确认的完整生命周期。它通过状态转换表驱动 UI 行为，确保了数据一致性和用户操作的原子性。

## 状态定义

状态机包含以下 6 个核心状态 (`WorkspaceEditState`)：

1.  **ReadOnly**：初始状态，数据只读，编辑按钮可用。
2.  **Editing**：用户点击编辑后进入，允许修改数据。
3.  **DirtyEditing**：数据发生变更后进入，标记为“脏”状态，触发未保存提示逻辑。
4.  **Saving**：保存请求发出后的中间状态，禁用所有输入控件，显示加载指示器。
5.  **LeavingConfirming**：用户在有未保存修改时尝试离开，弹出确认对话框。
6.  **TransitionBlocked**：重入保护状态，防止在保存或确认过程中重复触发事件。

## 事件与转换

状态流转由 10 个事件驱动：

*   **BeginEdit**: `ReadOnly` -> `Editing`
*   **MakeChange**: `Editing` -> `DirtyEditing`
*   **SaveRequest**: `Editing`/`DirtyEditing` -> `Saving`
*   **SaveComplete**: `Saving` -> `ReadOnly`
*   **SaveFailed**: `Saving` -> `DirtyEditing`
*   **LeaveRequest**: `DirtyEditing` -> `LeavingConfirming`
*   **LeaveConfirmed**: `LeavingConfirming` -> `ReadOnly` (丢弃或挂起后)
*   **LeaveCancelled**: `LeavingConfirming` -> `DirtyEditing`
*   **ForceLeave**: 任意状态 -> `ReadOnly` (用于强制重置)
*   **Reset**: 清除状态

## 实现细节

*   **转换表驱动**：使用 `Dictionary<(State, Event), State>` 定义合法转换，类似认证状态机。
*   **线程安全**：内部使用 `lock` 保证状态变更的原子性，`StateChanged` 事件在锁外触发以更新 UI。
*   **回退机制**：`_returnState` 私有字段记录 `LeavingConfirming` 前的状态，以便取消离开时恢复。
*   **UI 绑定**：`MedicalCaseWorkspaceViewModel` 订阅 `StateChanged` 事件，动态控制保存/取消按钮的可见性及顶部 Banner 提示。

## 业务价值

该状态机解决了传统 MVVM 中通过多个布尔标志位（如 `IsEditing`, `HasChanges`, `IsSaving`）管理复杂交互时的状态不一致问题，确保了医案数据在编辑、保存和导航过程中的完整性。
