# Change: 重构DetailView为容器化架构

## Why

当前DetailView使用IsReadOnly属性切换查看/编辑模式，导致：
1. 查看模式下TextBox边框仍然存在，视觉效果不够美观
2. 查看和编辑模式共用相同控件，限制了UI设计的灵活性
3. 无法为不同模式提供差异化的布局和交互体验

## What Changes

- **新增** BaseDetailContainer容器控件，支持ViewContent/EditContent独立定义
- **重构** PatientDetailView使用容器模式（试点）
- **重构** HerbDetailView使用容器模式
- **重构** UserDetailView使用容器模式
- **重构** FormulaDetailView使用容器模式
- **重构** MedicalCaseDetailView使用容器模式
- **新增** 查看模式专用样式（InfoCard、ValueText等）
- **新增** 编辑模式专用样式（FormField、FormSection等）

## Impact

- Affected specs: `desktop-detail-views`
- Affected code:
  - `LYBT.Desktop.Infrastructure/Views/BaseDetailContainer.xaml`
  - `LYBT.Desktop.Infrastructure/Controls/InfoCard.xaml`
  - `LYBT.Desktop.Infrastructure/Controls/FormField.xaml`
  - `LYBT.Desktop.Patients/Views/PatientDetailView.xaml`
  - `LYBT.Desktop.Herbs/Views/HerbDetailView.xaml`
  - `LYBT.Desktop.Users/Views/UserDetailView.xaml`
  - `LYBT.Desktop.Formula/Views/FormulaDetailView.xaml`
  - `LYBT.Desktop.MedicalCase/Views/MedicalCaseDetailView.xaml`

## Dependencies

- 依赖 `unify-detail-view-style` 提案完成后执行（当前已完成Phase 1-3）
