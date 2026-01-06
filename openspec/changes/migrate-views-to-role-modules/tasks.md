# migrate-views-to-role-modules Tasks

## Phase 1: 删除无调用的View

- [x] 1.1 删除 `Consultation/Views/ConsultationFormView.xaml(.cs)`
- [x] 1.2 更新 ConsultationModule.cs 移除相关注册
- [x] 1.3 删除 `Formula/Views/FormulaDetailView.xaml(.cs)`
- [x] 1.4 删除 `Formula/Views/FormulaValidationView.xaml(.cs)`
- [x] 1.5 更新 FormulaModule.cs 移除相关注册
- [x] 1.6 删除 `Herbs/Views/HerbDetailView.xaml(.cs)`
- [x] 1.7 更新 HerbsModule.cs 移除相关注册
- [x] 1.8 删除 `MedicalCase/Views/MedicalCaseDetailView.xaml(.cs)`
- [x] 1.9 删除 `MedicalCase/Views/MedicalCaseWorkspaceView.xaml(.cs)`
- [x] 1.10 更新 MedicalCaseModule.cs 移除相关注册
- [x] 1.11 删除 `Users/Views/UserDetailView.xaml(.cs)`
- [x] 1.12 更新 UsersModule.cs 移除相关注册
- [x] 1.13 编译验证 Phase 1

### Phase 1 额外清理 (未调用的Dialog)

- [x] 1.14 删除 `Formula/Views/EditFormulaDialog.xaml(.cs)` + ViewModel
- [x] 1.15 删除 `MedicalCase/Views/HistoryPrescriptionSelectionDialog.xaml(.cs)` + ViewModel
- [x] 1.16 删除 `MedicalCase/Views/DuplicateHerbAlertDialog.xaml(.cs)` + ViewModel
- [x] 1.17 删除 `Patients/Views/QuickCreatePatientDialog.xaml(.cs)` + ViewModel
- [x] 1.18 更新相关Module移除RegisterDialog注册

## Phase 2: PatientDetailView 迁移

**决策: 删除并改用PatientManagementView**
- PatientDetailView是历史遗留代码
- 两个调用点已改为导航到PatientManagementView（内嵌PatientMasterDetailControl）
- 删除PatientDetailView和PatientDetailViewModel

- [x] 2.1 分析PatientDetailView调用情况 -> 两个调用点可改用PatientManagementView
- [x] 2.2 删除 `Patients/Views/PatientDetailView.xaml(.cs)`
- [x] 2.3 删除 `Patients/ViewModels/PatientDetailViewModel.cs`
- [x] 2.4 更新 PatientsModule.cs 移除注册
- [x] 2.5 修改 PatientSelectionViewModel.ExecuteNewPatient() -> PatientManagementView
- [x] 2.6 修改 MedicalCaseWorkspaceViewModel.ExecuteViewPatientHistory() -> PatientManagementView
- [x] 2.7 删除空的 Patients/Views/ 文件夹

## Phase 3: ChangePasswordView 迁移

**决策: 合并到AccountSettingsControl**
- ChangePasswordView和UserProfileView功能合并
- 创建Shell/Controls/AccountSettingsControl统一处理
- 通过SidebarControl的"账户设置"按钮触发

- [x] 3.1 创建 `Shell/Controls/AccountSettingsControl.xaml(.cs)`
- [x] 3.2 合并密码修改和个人资料功能
- [x] 3.3 删除 `Users/Views/ChangePasswordView.xaml(.cs)` + ViewModel
- [x] 3.4 删除 `Users/Views/UserProfileView.xaml(.cs)` + ViewModel
- [x] 3.5 更新UsersModule移除相关注册

## Phase 4: UserProfileView 迁移

**决策: 已合并到Phase 3的AccountSettingsControl**

- [x] 4.1 与ChangePasswordView合并为AccountSettingsControl

## Phase 5: 清理与验证

- [x] 5.1 检查并删除业务模块中空的 Views 文件夹
  - 已删除: Formula/Views, MedicalCase/Views, Patients/Views
  - 保留: Auth/Views (包含LoginView/LoginWindow)
- [x] 5.2 全量编译验证 -> 0 错误
- [ ] 5.3 运行时导航测试
- [ ] 5.4 更新架构文档

## Phase 6: 遗留ViewModel清理

**决策: 删除已无View引用的DetailViewModel**
- 这些ViewModel是历史遗留代码，对应的View已在Phase 1删除
- 现已统一使用MasterDetailViewModel模式

- [x] 6.1 分析6个遗留ViewModel引用情况
- [x] 6.2 删除 FormulaDetailViewModel.cs
- [x] 6.3 删除 FormulaValidationViewModel.cs
- [x] 6.4 删除 ConsultationFormViewModel.cs
- [x] 6.5 删除 HerbDetailViewModel.cs
- [x] 6.6 删除 MedicalCaseDetailViewModel.cs
- [x] 6.7 删除 UserDetailViewModel.cs
- [x] 6.8 更新各Module移除注册
- [x] 6.9 删除 UserDetailViewModelTests.cs (测试文件)
- [x] 6.10 编译验证 -> 0 错误 0 警告

## 完成标准

- [x] 业务模块 Views 文件夹仅保留必要View (LoginView/LoginWindow)
- [x] ChangePasswordView和UserProfileView合并为AccountSettingsControl
- [x] 12个无调用的View/Dialog已删除（含PatientDetailView）
- [ ] 所有角色台 View 正常导航
- [x] 编译 0 错误

## 清理统计

**已删除文件 (View):**
1. ConsultationFormView.xaml(.cs)
2. FormulaDetailView.xaml(.cs)
3. FormulaValidationView.xaml(.cs)
4. HerbDetailView.xaml(.cs)
5. MedicalCaseDetailView.xaml(.cs)
6. MedicalCaseWorkspaceView.xaml(.cs)
7. UserDetailView.xaml(.cs)
8. ChangePasswordView.xaml(.cs)
9. UserProfileView.xaml(.cs)
10. PatientDetailView.xaml(.cs)

**已删除文件 (Dialog):**
11. EditFormulaDialog.xaml(.cs) + EditFormulaDialogViewModel.cs
12. HistoryPrescriptionSelectionDialog.xaml(.cs) + HistoryPrescriptionSelectionDialogViewModel.cs
13. DuplicateHerbAlertDialog.xaml(.cs) + DuplicateHerbAlertDialogViewModel.cs
14. QuickCreatePatientDialog.xaml(.cs) + QuickCreatePatientDialogViewModel.cs

**已删除ViewModel (Phase 2-4):**
- ChangePasswordViewModel.cs
- UserProfileViewModel.cs
- PatientDetailViewModel.cs

**已删除ViewModel (Phase 6 - 遗留DetailViewModel清理):**
- FormulaDetailViewModel.cs
- FormulaValidationViewModel.cs
- ConsultationFormViewModel.cs
- HerbDetailViewModel.cs
- MedicalCaseDetailViewModel.cs
- UserDetailViewModel.cs

**已删除测试文件:**
- UserDetailViewModelTests.cs

**已删除空文件夹:**
- Formula/Views/
- MedicalCase/Views/
- Patients/Views/

**已修改调用点:**
- PatientSelectionViewModel.ExecuteNewPatient() -> PatientManagementView
- MedicalCaseWorkspaceViewModel.ExecuteViewPatientHistory() -> PatientManagementView (2处)

## Phase 7: 审计Dialog清理

**决策: 完整移除审计功能，后续单独规划**
- 审计功能将来单独创建project实现
- 删除所有审计相关Dialog和调用

- [x] 7.1 分析MedicalCase/Dialogs目录5个Dialog使用情况
  - AuditLogDialog: 未调用（Shell有EntityAuditLogDialog替代）
  - AuditReasonDialog: MedicalCaseWorkspaceViewModel调用
  - FormulaImportDialog: 保留（PrescriptionPanelViewModel调用）
  - HistoryCopyDialog: 保留（PrescriptionPanelViewModel调用）
  - UnsavedChangesDialog: 保留（MedicalCaseNavigationHandler调用）
- [x] 7.2 删除 AuditLogDialog.xaml(.cs) + AuditLogDialogViewModel.cs
- [x] 7.3 删除 AuditReasonDialog.xaml(.cs) + AuditReasonDialogViewModel.cs
- [x] 7.4 更新 MedicalCaseModule.cs 移除审计Dialog注册
- [x] 7.5 更新 MedicalCaseWorkspaceViewModel (2处) 移除审计调用
- [x] 7.6 编译验证 -> 0 错误 5 警告(既有)

**已删除文件 (Phase 7 - 审计Dialog):**
- AuditLogDialog.xaml(.cs) + AuditLogDialogViewModel.cs
- AuditReasonDialog.xaml(.cs) + AuditReasonDialogViewModel.cs
