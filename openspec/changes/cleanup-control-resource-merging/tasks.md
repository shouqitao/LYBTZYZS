# Tasks: cleanup-control-resource-merging

## Phase 0: 资源架构统一 (2026-01-22新增)

### Task 0.1: 移除控件级资源合并 (已完成)
- [x] 分析App.xaml应用级资源配置 - 确认已正确配置
- [x] 移除MasterDetailLayout.xaml中的Theme.Light.xaml合并
- [x] 移除BaseDetailContainer.xaml中的UnifiedComponents.xaml合并
- [x] 更新CLAUDE.md文档说明新架构原则
- [x] 编译验证通过

**关键发现**：控件级别重复合并资源字典导致DependencyProperty.UnsetValue崩溃。正确做法是统一依赖Application.Resources。

---

## Phase 1: 基础设施层 (Converter迁移已完成)

### Task 1.1: ConverterInstances.cs (已完成)
- [x] 创建 `Converters/ConverterInstances.cs`
- [x] 实现 `Cvt` 静态类
- [x] 添加所有转换器的静态实例
- [x] 添加XML文档注释

### Task 1.2: MasterDetailLayout.xaml (已完成)
- [x] 添加 `xmlns:converters` 命名空间
- [x] 替换 `BooleanToVisibilityConverter` → `Cvt.BoolToVis`
- [x] 替换 `InverseBooleanToVisibilityConverter` → `Cvt.InverseBoolToVis`
- [x] 保留本地样式定义（如GridSplitter样式）
- [x] 移除控件级资源字典合并 (Phase 0)
- [x] 编译验证

### Task 1.3: SidebarControl.xaml (已完成)
- [x] 添加 `xmlns:converters` 命名空间
- [x] 替换 `BooleanToVisibilityConverter` → `Cvt.BoolToVis`
- [x] 替换 `InverseBooleanToVisibilityConverter` → `Cvt.InverseBoolToVis`
- [x] 替换 `EnumDescriptionConverter` → `Cvt.EnumDesc`
- [x] 替换 `FirstCharacterConverter` → `Cvt.FirstChar`
- [x] 替换 `ApiHealthStatusToColorConverter` → `Cvt.ApiStatusToColor`
- [x] 替换 `ApiHealthStatusToTextConverter` → `Cvt.ApiStatusToText`
- [x] 保留 `BoolToSidebarWidthConverter` 本地定义（带参数）
- [x] 编译验证

### Task 1.4: PatientSearchControl.xaml (已完成)
- [x] 添加 `xmlns:converters` 命名空间
- [x] 替换 `BooleanToVisibilityConverter` → `Cvt.BoolToVis`
- [x] 替换 `EnumDescriptionConverter` → `Cvt.EnumDesc`
- [x] 编译验证

### Task 1.5: 其他Infrastructure Controls (已完成)
- [x] 检查并迁移 DataGridToolbar.xaml
- [x] 检查并迁移 DetailToolbar.xaml
- [x] 检查并迁移 EmptyState.xaml
- [x] 检查并迁移 InfoCard.xaml
- [x] 检查并迁移 StatusBadge.xaml
- [x] 检查并迁移 SearchBox.xaml
- [x] 检查并迁移 PendingQueueControl.xaml
- [x] 检查并迁移 PatientInfoCardControl.xaml
- [x] 检查并迁移 BaseDetailContainer.xaml (含Phase 0资源合并移除)

---

## Phase 2: 模块控件层 (Converter迁移已完成)

### Task 2.1: Herbs模块 (已完成)
- [x] 迁移 HerbMasterDetailControl.xaml
- [x] 迁移 HerbEditControl.xaml
- [x] 迁移 HerbViewControl.xaml
- [ ] 测试：点击药材列表项显示详情

### Task 2.2: Formula模块 (已完成)
- [x] 迁移 FormulaMasterDetailControl.xaml
- [x] 迁移 FormulaEditControl.xaml
- [x] 迁移 FormulaViewControl.xaml
- [ ] 测试：点击方剂列表项显示详情

### Task 2.3: Patients模块 (已完成)
- [x] 迁移 PatientMasterDetailControl.xaml
- [x] 迁移 PatientEditControl.xaml
- [x] 迁移 PatientViewControl.xaml
- [ ] 测试：点击患者列表项显示详情

### Task 2.4: MedicalCase模块 (已完成)
- [x] 迁移 MedicalCaseMasterDetailControl.xaml
- [x] 迁移 MedicalCaseEditControl.xaml
- [x] 迁移 MedicalCaseViewControl.xaml
- [ ] 测试：点击医案列表项显示详情

### Task 2.5: Users模块 (已完成)
- [x] 迁移 UserMasterDetailControl.xaml
- [x] 迁移 UserEditControl.xaml
- [x] 迁移 UserViewControl.xaml
- [ ] 测试：点击用户列表项显示详情

---

## Phase 3: 视图层 (Converter迁移已完成)

### Task 3.1: Shell Views (已完成)
- [x] 检查并迁移 LoginWindow.xaml
- [x] 检查并迁移 AccountSettingsControl.xaml

### Task 3.2: Admin Views (已完成)
- [x] 检查并迁移 AdminHomeView.xaml
- [x] 检查并迁移 SystemSettingsView.xaml

### Task 3.3: Clinical Views (已完成)
- [x] 检查并迁移 ClinicalHomeView.xaml
- [x] 检查并迁移 PatientSelectionView.xaml
- [x] 检查并迁移 MedicalCaseWorkspaceView.xaml

---

## Phase 4: 清理与验证

### Task 4.1: 移除不必要的资源合并 (已完成 - Phase 0)
- [x] 审查所有已迁移控件
- [x] 移除MasterDetailLayout.xaml中的资源合并
- [x] 移除BaseDetailContainer.xaml中的资源合并
- [x] 确认其他控件不需要控件级资源合并

### Task 4.2: 文档更新 (已完成)
- [x] 更新 `LYBT.Desktop.Infrastructure/CLAUDE.md` (资源架构原则)
- [ ] 更新 `XAML-RESOURCE-GUIDE.md` (待后续完善)

### Task 4.3: 全面回归测试
- [ ] 测试所有Master-Detail模块
- [ ] 验证侧边栏功能
- [ ] 验证主题切换（如有）
- [ ] 检查VS Output窗口绑定错误

### Task 4.4: 归档
- [ ] 更新proposal.md状态为Completed
- [ ] 记录到Serena记忆系统
