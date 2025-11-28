# Tasks

## Phase 1: UI布局调整

- [x] TASK-001: 右上角添加"编辑医案"按钮
  - 修改 MedicalCaseWorkspaceView.xaml Row 0 Grid
  - 添加按钮到 Grid.Column="2" 最右侧
  - 绑定 Visibility 到 ShowEditButtonTopRight 属性
  - 绑定 Command 到 EnterEditModeCommand

- [x] TASK-002: 底部操作栏按钮位置调整
  - 修改 MedicalCaseWorkspaceView.xaml Row 2 StackPanel
  - Management编辑模式：[打印处方笺] [保存医案]
  - 保存按钮在最右侧
  - 添加 SaveAndStayCommand 绑定（复用现有命令，重命名显示文本）

- [x] TASK-003: ViewModel按钮可见性属性
  - 添加 ShowEditButtonTopRight 属性（Management只读模式显示）
  - 调整 ShowEditButton 属性（底部不再显示）
  - 更新属性变更通知链

## Phase 2: Management模式默认只读

- [x] TASK-004: 修改DetermineEditModeAsync逻辑
  - Management模式 + ForManagementView → 默认只读
  - Management模式 + ForManagementEdit → 默认编辑
  - 保持Clinical模式现有逻辑

- [x] TASK-005: 调整MedicalCaseNavigationParameters.ForManagementEdit
  - 验证现有实现是否设置正确的InitialEditState
  - 确保从管理界面点击"编辑"进入编辑状态

## Phase 3: 返回确认对话框

- [x] TASK-006: 创建编辑返回确认对话框
  - 创建 UnsavedChangesDialog.xaml
  - 三选项：保存修改 / 放弃修改 / 取消
  - 注册到 MedicalCaseModule

- [x] TASK-007: 修改ExecuteBackAsync逻辑
  - Management只读模式：直接返回
  - Management编辑模式：显示UnsavedChangesDialog
  - 根据用户选择执行保存/放弃/取消

## Phase 4: 审计判断逻辑

- [x] TASK-008: 创建IAuditRequirementChecker接口
  - 定义 IsAuditRequired(MedicalCaseDto, Guid currentUserId) 方法
  - 放置于 LYBT.Desktop.MedicalCase.Interfaces

- [x] TASK-009: 实现AuditRequirementChecker
  - 实现三条规则：Completed状态、非本人、隔天
  - 放置于 LYBT.Desktop.MedicalCase.Services
  - 注册为Singleton

## Phase 5: 审计理由对话框

- [x] TASK-010: 创建AuditReasonDialog.xaml
  - 多行文本框（修改原因）
  - 常用原因RadioButton组
  - 确认/取消按钮
  - 确认按钮CanExecute绑定到原因非空

- [x] TASK-011: 创建AuditReasonDialogViewModel
  - Reason属性（文本框绑定）
  - SelectedCommonReason属性（RadioButton绑定）
  - ConfirmCommand / CancelCommand
  - 常用原因选择时自动填充Reason

- [x] TASK-012: 注册对话框到MedicalCaseModule
  - RegisterDialog<AuditReasonDialog, AuditReasonDialogViewModel>

## Phase 6: 集成审计到保存流程

- [x] TASK-013: 修改SaveAndStay命令实现
  - 注入IAuditRequirementChecker
  - 保存前检查是否需要审计
  - 需要时弹出AuditReasonDialog
  - 不需要时直接保存

- [x] TASK-014: 修改UnsavedChangesDialog保存选项
  - "保存修改"选项需要先检查审计
  - 需要审计时弹出AuditReasonDialog
  - 审计完成后再执行保存和返回

## Phase 7: 测试与验证

- [x] TASK-015: 手动测试场景
  - Management模式：查看 → 编辑 → 保存 → 只读
  - Management模式：编辑 → 返回 → 保存修改
  - Management模式：编辑 → 返回 → 放弃修改
  - Clinical模式：验证现有功能不受影响
  - 审计场景：验证各种情况下的审计判断

## Dependencies

```
TASK-001 ─┬─→ TASK-003
TASK-002 ─┘

TASK-004 ─→ TASK-005

TASK-006 ─→ TASK-007

TASK-008 ─→ TASK-009 ─→ TASK-013

TASK-010 ─→ TASK-011 ─→ TASK-012 ─→ TASK-013

TASK-013 ─→ TASK-014

All ─→ TASK-015
```

## Parallelizable

- Phase 1 (UI布局) 和 Phase 2 (默认只读) 可并行
- Phase 3 (返回确认) 和 Phase 4+5 (审计) 可并行
- Phase 6 依赖 Phase 3-5 完成
