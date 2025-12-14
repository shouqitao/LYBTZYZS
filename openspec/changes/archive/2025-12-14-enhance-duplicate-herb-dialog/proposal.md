# OpenSpec Proposal: enhance-duplicate-herb-dialog

## Summary

优化处方导入时重复药材的提醒机制：将批量提醒改为逐个确认对话框，医生必须逐一确认每个重复药材。

## Motivation

### 现状问题

1. **批量提醒不够精细**：当前发现重复药材时，系统弹出一个对话框显示所有重复药材列表，医生只需点击一次确认
2. **确认流程不够严谨**：医生可能未仔细查看每个重复药材就点击确认，容易遗漏问题

### 业务需求

- 医生需要针对每个重复药材逐一确认，确保不会遗漏
- 每个重复药材都需要单独弹窗，医生必须逐个点击"确定"才能继续

## Requirements

### 功能需求

#### REQ-001: 逐个确认对话框

- 当验方导入或历史处方复制检测到N个重复药材时，依次弹出N个对话框
- 每个对话框显示：
  - 药材名称
  - 提示信息："[药材名称] 重复"
- 医生点击"确定"关闭当前对话框后，继续弹出下一个
- 所有重复项确认完成后，继续执行导入/合并操作

#### REQ-002: 剂量合并策略

- 保持现有的剂量合并逻辑（取最大值）
- 对话框仅作为提醒，不提供合并选项

### 非功能需求

- NFR-001: 对话框响应时间 < 100ms
- NFR-002: 向后兼容 - 剂量合并行为与当前版本一致（取最大值）

## Affected Components

| 层 | 组件 | 影响程度 |
|----|------|----------|
| Desktop/MedicalCase | `DuplicateHerbAlertDialog.xaml` | Major - 重构为单药材对话框 |
| Desktop/MedicalCase | `DuplicateHerbAlertDialogViewModel.cs` | Major - 简化为单药材确认 |
| Desktop/MedicalCase | `PrescriptionImportHandler.cs` | Moderate - 调整调用逻辑 |
| Desktop/MedicalCase | `PrescriptionPanelViewModel.cs` | Moderate - 循环调用对话框 |

## Acceptance Criteria

- [ ] AC-001: 导入包含3个重复药材的验方时，依次弹出3个对话框，每个显示单个药材名称
- [ ] AC-002: 每个对话框只有"确定"按钮，点击后关闭当前对话框
- [ ] AC-003: 所有对话框确认完成后，继续执行导入操作，剂量按最大值合并
- [ ] AC-004: 如果没有重复药材，不弹出任何对话框

## Out of Scope

- 可配置的剂量合并策略
- 取消导入功能
- 用户选择合并方式
- 服务端处方数据的修改

## Risks

| 风险 | 可能性 | 影响 | 缓解措施 |
|------|--------|------|----------|
| 多个对话框影响用户体验 | 中 | 低 | 对话框简洁，只需一键确认 |
| 重复药材数量过多时繁琐 | 低 | 中 | 实际场景中重复药材通常1-3个 |

## References

- Epic #2175 BF-002 Task 3.10: 重复药材聚合提醒对话框（当前实现）
- `DuplicateHerbAlertDialog.xaml`: 现有对话框实现
- `PrescriptionImportHandler.cs`: 现有导入处理逻辑
