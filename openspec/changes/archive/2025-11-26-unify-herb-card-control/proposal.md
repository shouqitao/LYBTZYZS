# Proposal: unify-herb-card-control

## Summary

统一药材卡片控件（HerbCardControl），复用经验方模块的设计模式到处方编辑场景。主要区别：经验方价格固定为0，处方使用药材库实际价格。

## Motivation

当前存在两套类似但不同的药材编辑实现：
1. **经验方模块** (`LYBT.Desktop.Formula`): 使用 `HerbCardControl` + `ItemsControl` + `UniformGrid(4列)` 布局，无价格显示
2. **处方模块** (`LYBT.Desktop.MedicalCase`): 旧版使用 8列DataGrid，新版 `PrescriptionEditorPanel` 已采用卡片布局但缺少价格功能

用户希望：
- 统一两套药材编辑体验
- 处方编辑时显示药材单价（从药材库获取）
- 经验方编辑时价格固定为0

## Scope

### In Scope
- 提取共享的 `HerbCardControl` 到共享层
- 添加可选的价格显示功能（通过属性控制）
- 统一 `HerbItemViewModel` 基类
- 重构处方编辑使用新控件
- 保持经验方编辑现有行为不变

### Out of Scope
- 修改处方业务逻辑
- 修改价格计算算法
- 后端API变更

## Design

参见 [design.md](./design.md)

## Tasks

参见 [tasks.md](./tasks.md)

## Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| 经验方模块回归 | Low | Medium | 单元测试覆盖，控件属性隔离 |
| 价格显示UI空间不足 | Low | Low | 调整卡片布局，价格显示为可选 |
| ViewModel依赖冲突 | Medium | Medium | 定义清晰的接口 `IHerbItem` |

## Acceptance Criteria

1. 处方编辑使用卡片布局（与经验方一致）
2. 处方编辑卡片显示药材单价
3. 经验方编辑卡片不显示价格
4. 拼音码自动完成功能正常
5. 键盘导航（Enter跳转、Delete删除）功能正常
6. 总价格自动计算正确

## References

- 经验方控件: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Controls/HerbCardControl.xaml`
- 处方面板: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/PrescriptionEditorPanel.xaml`
- 药材项ViewModel: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/ViewModels/FormulaHerbItemViewModel.cs`
