# Tasks: 统一药材列表编辑控件

## Phase 1: Bug修复与新控件

- [x] 修复 HerbItemViewModelBase._unit 默认值为空字符串
- [x] 创建 HerbListView.xaml 只读预览控件
- [x] 创建 HerbListView.xaml.cs 代码后端

## Phase 2: 控件统一

- [x] 重构 EditFormulaDialog.xaml 使用 HerbListEditor
- [x] 重构 MedicalCaseWorkspaceView.xaml 药材预览使用 HerbListView
- [x] 检查 MedicalCaseViewControl.xaml - 保持现有Tag样式（直接绑定DTO，与HerbListView设计不兼容）

## Phase 2.5: 医案管理模块处方编辑

- [x] 更新 MedicalCaseEditControl.xaml 使用 HerbListEditor
- [x] 更新 MedicalCaseEditControl.xaml.cs 添加 HerbItems 和命令属性
- [x] 更新 MedicalCaseMasterDetailView.xaml 绑定处方编辑属性
- [x] 更新 MedicalCaseMasterDetailViewModel 添加处方编辑功能
  - 添加 AllHerbs, HerbItems, HerbCount 属性
  - 实现 DeleteHerbCommand, DosageCompletedCommand, AddNewRowCommand
  - 使用 SaveAggregateAsync 一次性保存诊断和处方

## Phase 3: 验证

- [x] 全量编译测试 (0 errors, 0 warnings)
- [ ] 验证处方编辑功能 (手动测试)
- [ ] 验证经验方编辑功能 (手动测试)
- [ ] 验证预览显示功能 (手动测试)
