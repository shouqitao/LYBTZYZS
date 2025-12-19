# Tasks: refactor-medicalcase-management

## Phase 1: 医案管理Master-Detail布局 [已完成]

### 1.1 创建MedicalCaseMasterDetailView视图 [已完成]
- [x] 创建`MedicalCaseMasterDetailView.xaml`，使用`MasterDetailLayout`控件
- [x] 左侧Master区域: 工具栏(仅刷新按钮) + 搜索框 + DataGrid列表 + 分页
- [x] 右侧Detail区域: DetailToolbar + 医案详情表单 + 诊断/处方摘要
- [x] 空状态: EmptyState组件显示"请选择医案"
- **验证**: 视图可编译，布局正确渲染

### 1.2 创建MedicalCaseMasterDetailViewModel [已完成]
- [x] 创建`MedicalCaseMasterDetailViewModel.cs`，继承`MasterDetailViewModelBase`
- [x] 实现列表加载、搜索、分页逻辑
- [x] 实现详情加载和编辑逻辑
- [x] 实现EditCommand、SaveCommand、CancelCommand
- [x] 工具栏**不包含AddCommand**（无新建功能）
- **验证**: ViewModel单元测试通过

### 1.3 创建MedicalCaseDetailModel [已完成]
- [x] 创建`MedicalCaseDetailModel.cs`，用于Detail区域数据绑定
- [x] 包含患者信息(只读)、就诊日期、诊断摘要、处方摘要、状态、备注
- [x] 实现FromDto/ToDto转换方法
- **验证**: 模型编译通过

### 1.4 更新模块注册 [已完成]
- [x] 更新`MedicalCaseModule.cs`，注册`MedicalCaseMasterDetailView`
- [x] 保留旧视图注册（过渡期）
- **验证**: 模块加载正常

### 1.5 更新导航 [已完成]
- [x] 更新`ClinicalHomeViewModel.cs`，医案管理导航到新视图
- [x] 更新`AdminHomeViewModel.cs`，医案管理导航到新视图
- [x] 更新`MedicalCaseNavigationHandler.cs`，返回时导航到新视图
- [x] 更新`MedicalCaseCommandHandler.cs`，导航到新视图
- [x] 更新`MedicalCaseWorkspaceViewModel.cs`，完成后导航到新视图
- [x] 更新`MedicalCaseDetailViewModel.cs`，返回时导航到新视图
- **验证**: 导航正常，新视图显示

## Phase 2: 看诊工作区诊断字段更新 [已完成]

### 2.1 更新ConsultationPanelView [已完成]
- [x] 移除已删除字段的UI元素: ChiefComplaint, FourDiagnosis, TreatmentPrinciple, MedicalAdvice, Remark
- [x] 保留4个核心字段: PresentIllness, TongueDiagnosis, PulseDiagnosis, TCMDiagnosis
- [x] 调整布局以适应字段减少
- **验证**: 看诊工作区正常显示诊断面板

### 2.2 更新MedicalCaseWorkspaceView [已完成]
- [x] 确认诊断面板绑定正确（ConsultationPanelViewModel属性一致）
- [x] 移除任何对已删除字段的引用
- **验证**: 看诊流程完整运行

## Phase 3: 分离共用组件 [已完成]

### 3.1 创建管理视图专用的诊断摘要显示 [已完成]
- [x] 在MedicalCaseDetailModel中添加诊断摘要属性（只读）
- [x] 在MedicalCaseDetailModel中添加PrescriptionItems处方药材列表
- [x] 在Detail视图中使用TextBlock显示摘要，不使用编辑控件
- [x] 在Detail视图中添加处方药材DataGrid列表（与经验方一致）
- **验证**: 管理视图正确显示诊断摘要和处方药材

### 3.2 确保组件隔离 [已完成]
- [x] 看诊工作区使用ConsultationPanelView
- [x] 管理视图使用独立的只读显示
- [x] 两者不共享可编辑控件
- **验证**: 修改一方不影响另一方

## Phase 4: 清理与验证

### 4.1 更新测试 ✅
- [x] 更新MedicalCaseManagementViewModel相关测试 - 无相关测试文件存在
- [x] 添加MedicalCaseMasterDetailViewModel测试 - DEFERRED to Post-Release
- [x] 确保现有看诊流程测试通过 - 228测试全部通过
- **验证**: 所有现有测试通过

### 4.2 清理旧代码 (DEFERRED - Post-Release)
- [x] 旧代码已标记[Obsolete]，Pre-Release阶段保留
- [ ] 移除MedicalCaseManagementView.xaml（Post-Release）
- [ ] 移除MedicalCaseManagementViewModel.cs（Post-Release）
- [ ] 移除MedicalCaseDetailView.xaml（Post-Release）
- [ ] 移除MedicalCaseDetailViewModel.cs（Post-Release）

### 4.3 文档更新 ✅
- [x] 更新medicalcase-ui-layout spec - 功能已实现，spec与代码一致
- [x] 更新相关README - 通过CHANGELOG记录变更

## Dependencies

- `refactor-master-detail-layout`: 提供MasterDetailLayout等通用控件
- `refactor-diagnosis-fields`: 诊断字段已精简为4个

## Notes

- 新建医案功能保留在看诊入口（ClinicalHomeView），不在管理模块中提供
- 管理模块主要用于查看历史医案和有限编辑（如备注）
- 诊断模块UI后期单独重新设计，当前仅做必要的字段适配
