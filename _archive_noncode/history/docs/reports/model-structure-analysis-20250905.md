# 模型结构一致性分析报告

**分析时间**: 2025-09-05T10:05:29.438950  
**扫描文件**: 162个模型文件  
**发现类**: 前端106个，后端14个，共享170个

## 📊 分析统计

| 类别 | 数量 |
|------|------|
| 精确匹配 | 0 |
| 不一致问题 | 0 |
| 相似匹配 | 2 |
| 仅前端存在 | 101 |
| 仅后端存在 | 14 |

## ✅ 精确匹配的模型

## 🔍 相似匹配的模型

- **PrescriptionViewModel** (前端) ≈ **PrescriptionItemModel** (后端)
  - 相似度: 90.5%
- **PrescriptionItemViewModel** (前端) ≈ **PrescriptionItemModel** (后端)
  - 相似度: 84.0%
## 📱 仅前端存在的模型

- TherapistMainViewModel
- SystemWorkbenchMainViewModel
- ReceptionistMainViewModel
- PharmacistMainViewModel
- ConsultationWorkbenchMainViewModel
- CashierMainViewModel
- HomeViewModel
- PatientListViewModel
- PatientDetailViewModel
- PrescriptionViewModel
- ConsultationViewModel
- ConfirmationDialogViewModel
- ErrorDetailsDialogViewModel
- InformationDialogViewModel
- UserAddEditDialogViewModel
- RoleItem
- UserManagementViewModel
- FormulaTemplateDialogViewModel
- HerbSelectionDialogViewModel
- PrescriptionComposerViewModel
- PrescriptionEditorDialogViewModel
- PrescriptionItemViewModel
- PrescriptionManagementViewModel
- PrescriptionsMainViewModel
- ModuleNavigationEventArgs
- ModuleNavigationEvent
- PrescriptionViewModelRefactored
- SelectFormulaDialogViewModel
- FormulaCategoryOption
- PatientAddEditDialogViewModel
- PatientImportWizardViewModel
- PatientManagementViewModel
- CreateMedicalCaseViewModel
- MedicalCaseDetailViewModel
- MedicalCaseListViewModel
- MedicalCaseManagementViewModel
- HerbAddEditDialogViewModel
- HerbDetailViewModel
- HerbManagementViewModel
- AddFormulaDialogViewModel
- EditFormulaDialogViewModel
- FormulaDetailViewModel
- FormulaManagementViewModel
- ViewFormulaDialogViewModel
- ConsultationMainViewModel
- ConsultationManagementViewModel
- LoginViewModel
- ErrorNotificationViewModel
- NavigationParameters
- CriticalErrorDialogViewModel
- FormulaSelectionDialogViewModel
- InputDialogViewModel
- FormulaDisplayViewModel
- FormulaStateViewModel
- FormulaThemeViewModel
- FormulaViewModel
- HerbDisplayViewModel
- HerbStateViewModel
- HerbThemeViewModel
- HerbViewModel
- MedicalCaseDisplayViewModel
- MedicalCaseStateViewModel
- MedicalCaseThemeViewModel
- MedicalCaseViewModel
- PatientDisplayViewModel
- PatientStateViewModel
- PatientThemeViewModel
- PatientViewModel
- PrescriptionDisplayViewModel
- PrescriptionStateViewModel
- PrescriptionThemeViewModel
- UserDisplayViewModel
- UserStateViewModel
- UserThemeViewModel
- UserViewModel
- WorkflowStepData
- FourDiagnosisData
- DifferentiationData
- PrescriptionData
- PrescriptionItem
- ConsultationData
- LoginStatusDto
- ApiConnectionStatusDto
- ConnectionLatencyDto
- SessionInfoDto
- SavedCredentialInfoDto
- AuthStatisticsDto
- RecentLoginHistoryDto
- LoginHistoryItemDto
- SecurityStatusDto
- AuthRiskLevelDto
- ChangePasswordDto
- ResetPasswordDto
- PasswordStrengthDto
- SecurityCheckResultDto
- SecurityThreatDto
- LoginExperienceDto
- OfflineModeDto
- AuthDiagnosticsDto
- SessionStatusChangedEventArgs
- SecurityEventArgs

## 🖥️ 仅后端存在的模型

- EnumMappingDto
- SettingsCreateDto
- SettingsEditDto
- AuthSession
- Consultation
- FormulaHerbItem
- Formula
- Herb
- MedicalCase
- Patient
- PrescriptionItemModel
- Prescription
- AdminSecretModel
- User


## 📋 建议行动

1. **修复不一致问题**: 优先处理0个不一致的模型
2. **检查相似匹配**: 验证2个相似模型是否应该统一
3. **清理冗余模型**: 评估单独存在的前端/后端模型是否必要
4. **完善共享模型**: 将通用模型迁移到Shared.Models项目

---

**生成时间**: 2025-09-05 10:05:29  
**工具**: 模型结构对比工具 v1.0
