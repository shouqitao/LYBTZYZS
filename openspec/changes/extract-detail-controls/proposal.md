# Change: 提取DetailView预览/编辑控件为独立UserControl

## Why
当前所有DetailView的ViewContent和EditContent内容直接嵌入在BaseDetailContainer中，无法在其他场景复用。例如FormulaImportDialog需要显示验方预览，但无法复用FormulaDetailView的ViewContent，导致重复实现预览UI。

## What Changes
- 将所有模块的ViewContent提取为独立的预览控件（XxxViewControl）
- 将所有模块的EditContent提取为独立的编辑控件（XxxEditControl）
- 在BaseDetailContainer中引用这些独立控件
- FormulaImportDialog复用FormulaViewControl实现详情预览

## Impact
- Affected specs: desktop-detail-views
- Affected modules:
  - LYBT.Desktop.Formula（FormulaViewControl、FormulaEditControl）
  - LYBT.Desktop.Herbs（HerbViewControl、HerbEditControl）
  - LYBT.Desktop.Patients（PatientViewControl、PatientEditControl）
  - LYBT.Desktop.Users（UserViewControl、UserEditControl）
  - LYBT.Desktop.MedicalCase（MedicalCaseViewControl、MedicalCaseEditControl）
  - LYBT.Desktop.MedicalCase/Dialogs（FormulaImportDialog复用FormulaViewControl）

## Design Notes
每个预览/编辑控件：
- 位置：`Controls/` 目录
- 命名：`{Entity}ViewControl.xaml` / `{Entity}EditControl.xaml`
- 数据绑定：通过DependencyProperty接收DTO对象
- 无ViewModel：纯展示控件，数据由外部提供
