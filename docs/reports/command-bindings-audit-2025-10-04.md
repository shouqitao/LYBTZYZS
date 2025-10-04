# 命令绑定审计报告 v2

**生成时间**: 2025-10-04 16:18:39
**扫描路径**: `D:\source\repos\LYBTZYZS\src\Client\Desktop`
**相关Issue**: #884

## 📊 审计摘要

| 指标 | 数值 |
|------|------|
| 扫描的XAML文件数 | 51 |
| 检查的命令总数 | 162 |
| 缺失的命令数 | **0** |
| 警告数 | 22 |

## 🔴 缺失的命令

✅ **未发现缺失的命令绑定！**

## ⚠️ 警告

| View | 问题 |
|------|------|
| VirtualizedDataGrid.xaml | ⚠️ ViewModel不存在 |
| LoginStatusControl.xaml | ⚠️ ViewModel不存在 |
| LoginControl.xaml | ⚠️ ViewModel不存在 |
| ErrorNotificationControl.xaml | ⚠️ ViewModel不存在 |
| FormulaTemplateListItemControl.xaml | ⚠️ ViewModel不存在 |
| LoginWindow.xaml | ⚠️ ViewModel不存在 |
| EditFormulaDialog.xaml | ⚠️ ViewModel不存在 |
| ViewFormulaDialog.xaml | ⚠️ ViewModel不存在 |
| CreateMedicalCaseDialog.xaml | ⚠️ ViewModel不存在 |
| FormulaTemplateDialog.xaml | ⚠️ ViewModel不存在 |
| HerbSelectionDialog.xaml | ⚠️ ViewModel不存在 |
| PrescriptionEditorDialog.xaml | ⚠️ ViewModel不存在 |
| SelectFormulaDialog.xaml | ⚠️ ViewModel不存在 |
| ChangePasswordDialog.xaml | ⚠️ ViewModel不存在 |
| ResetPasswordDialog.xaml | ⚠️ ViewModel不存在 |
| UserProfileDialog.xaml | ⚠️ ViewModel不存在 |
| ConfirmationDialog.xaml | ⚠️ ViewModel不存在 |
| ErrorDetailsDialog.xaml | ⚠️ ViewModel不存在 |
| InformationDialog.xaml | ⚠️ ViewModel不存在 |
| MainWindow.xaml | ⚠️ ViewModel不存在 |
| DataManagementView.xaml | ⚠️ ViewModel不存在 |
| ClinicalWorkflowView.xaml | ⚠️ ViewModel不存在 |

## ✅ 正常的命令绑定

<details>
<summary>点击展开查看所有正常的命令绑定（162 个）</summary>

| View | ViewModel | 命令 |
|------|-----------|------|
| LoginView.xaml | LoginViewModel.cs | `LoginCommand` |
| ConsultationMainView.xaml | ConsultationMainViewModel.cs | `RefreshCommand` |
| ConsultationMainView.xaml | ConsultationMainViewModel.cs | `ViewPatientHistoryCommand` |
| ConsultationMainView.xaml | ConsultationMainViewModel.cs | `NewConsultationCommand` |
| ConsultationMainView.xaml | ConsultationMainViewModel.cs | `ShowTemplateMenuCommand` |
| ConsultationMainView.xaml | ConsultationMainViewModel.cs | `SaveConsultationCommand` |
| ConsultationMainView.xaml | ConsultationMainViewModel.cs | `PrintPrescriptionCommand` |
| ConsultationMainView.xaml | ConsultationMainViewModel.cs | `DecreaseQuantityCommand` |
| ConsultationMainView.xaml | ConsultationMainViewModel.cs | `IncreaseQuantityCommand` |
| ConsultationMainView.xaml | ConsultationMainViewModel.cs | `RemovePrescriptionItemCommand` |
| ConsultationManagementView.xaml | ConsultationManagementViewModel.cs | `SearchCommand` |
| ConsultationManagementView.xaml | ConsultationManagementViewModel.cs | `StatisticsCommand` |
| ConsultationManagementView.xaml | ConsultationManagementViewModel.cs | `RefreshCommand` |
| ConsultationManagementView.xaml | ConsultationManagementViewModel.cs | `ViewDetailsCommand` |
| ConsultationManagementView.xaml | ConsultationManagementViewModel.cs | `ViewPrescriptionCommand` |
| ConsultationManagementView.xaml | ConsultationManagementViewModel.cs | `PrintCommand` |
| ConsultationManagementView.xaml | ConsultationManagementViewModel.cs | `CopyRecordCommand` |
| ConsultationManagementView.xaml | ConsultationManagementViewModel.cs | `FirstPageCommand` |
| ConsultationManagementView.xaml | ConsultationManagementViewModel.cs | `PreviousPageCommand` |
| ConsultationManagementView.xaml | ConsultationManagementViewModel.cs | `NextPageCommand` |
| ConsultationManagementView.xaml | ConsultationManagementViewModel.cs | `LastPageCommand` |
| FormulaDetailView.xaml | FormulaDetailViewModel.cs | `BackCommand` |
| FormulaDetailView.xaml | FormulaDetailViewModel.cs | `EditCommand` |
| FormulaDetailView.xaml | FormulaDetailViewModel.cs | `SaveCommand` |
| FormulaDetailView.xaml | FormulaDetailViewModel.cs | `CancelEditCommand` |
| FormulaDetailView.xaml | FormulaDetailViewModel.cs | `CopyFormulaCommand` |
| FormulaDetailView.xaml | FormulaDetailViewModel.cs | `ViewUsageHistoryCommand` |
| FormulaDetailView.xaml | FormulaDetailViewModel.cs | `PrintCommand` |
| FormulaManagementView.xaml | FormulaManagementViewModel.cs | `SearchCommand` |
| FormulaManagementView.xaml | FormulaManagementViewModel.cs | `ClearFiltersCommand` |
| FormulaManagementView.xaml | FormulaManagementViewModel.cs | `ImportFormulasCommand` |
| FormulaManagementView.xaml | FormulaManagementViewModel.cs | `ExportTemplateCommand` |
| FormulaManagementView.xaml | FormulaManagementViewModel.cs | `ExportFormulasCommand` |
| FormulaManagementView.xaml | FormulaManagementViewModel.cs | `AddFormulaCommand` |
| FormulaManagementView.xaml | FormulaManagementViewModel.cs | `RefreshCommand` |
| FormulaManagementView.xaml | FormulaManagementViewModel.cs | `ViewDetailsCommand` |
| FormulaManagementView.xaml | FormulaManagementViewModel.cs | `EditCommand` |
| FormulaManagementView.xaml | FormulaManagementViewModel.cs | `CopyCommand` |
| FormulaManagementView.xaml | FormulaManagementViewModel.cs | `DeleteCommand` |
| FormulaManagementView.xaml | FormulaManagementViewModel.cs | `FirstPageCommand` |
| FormulaManagementView.xaml | FormulaManagementViewModel.cs | `PreviousPageCommand` |
| FormulaManagementView.xaml | FormulaManagementViewModel.cs | `NextPageCommand` |
| FormulaManagementView.xaml | FormulaManagementViewModel.cs | `LastPageCommand` |
| HerbDetailView.xaml | HerbDetailViewModel.cs | `BackCommand` |
| HerbDetailView.xaml | HerbDetailViewModel.cs | `EditCommand` |
| HerbDetailView.xaml | HerbDetailViewModel.cs | `SaveCommand` |
| HerbDetailView.xaml | HerbDetailViewModel.cs | `CancelEditCommand` |
| HerbDetailView.xaml | HerbDetailViewModel.cs | `ViewUsageHistoryCommand` |
| HerbDetailView.xaml | HerbDetailViewModel.cs | `PrintCommand` |
| HerbManagementView.xaml | HerbManagementViewModel.cs | `SearchCommand` |
| HerbManagementView.xaml | HerbManagementViewModel.cs | `ImportHerbsCommand` |
| HerbManagementView.xaml | HerbManagementViewModel.cs | `ExportTemplateCommand` |
| HerbManagementView.xaml | HerbManagementViewModel.cs | `ExportHerbsCommand` |
| HerbManagementView.xaml | HerbManagementViewModel.cs | `AddCommand` |
| HerbManagementView.xaml | HerbManagementViewModel.cs | `RefreshCommand` |
| HerbManagementView.xaml | HerbManagementViewModel.cs | `EditCommand` |
| HerbManagementView.xaml | HerbManagementViewModel.cs | `ToggleStatusCommand` |
| HerbManagementView.xaml | HerbManagementViewModel.cs | `DeleteCommand` |
| HerbManagementView.xaml | HerbManagementViewModel.cs | `FirstPageCommand` |
| HerbManagementView.xaml | HerbManagementViewModel.cs | `PreviousPageCommand` |
| HerbManagementView.xaml | HerbManagementViewModel.cs | `NextPageCommand` |
| HerbManagementView.xaml | HerbManagementViewModel.cs | `LastPageCommand` |
| MedicalCaseDetailView.xaml | MedicalCaseDetailViewModel.cs | `BackCommand` |
| MedicalCaseDetailView.xaml | MedicalCaseDetailViewModel.cs | `StartConsultationCommand` |
| MedicalCaseDetailView.xaml | MedicalCaseDetailViewModel.cs | `PrintCommand` |
| MedicalCaseDetailView.xaml | MedicalCaseDetailViewModel.cs | `RefreshCommand` |
| MedicalCaseDetailView.xaml | MedicalCaseDetailViewModel.cs | `EditCommand` |
| MedicalCaseDetailView.xaml | MedicalCaseDetailViewModel.cs | `PrintPrescriptionCommand` |
| MedicalCaseDetailView.xaml | MedicalCaseDetailViewModel.cs | `CloseCommand` |
| MedicalCaseListView.xaml | MedicalCaseListViewModel.cs | `SearchCommand` |
| MedicalCaseListView.xaml | MedicalCaseListViewModel.cs | `AddCommand` |
| MedicalCaseListView.xaml | MedicalCaseListViewModel.cs | `RefreshCommand` |
| MedicalCaseListView.xaml | MedicalCaseListViewModel.cs | `ViewDetailCommand` |
| MedicalCaseListView.xaml | MedicalCaseListViewModel.cs | `StartConsultationCommand` |
| MedicalCaseListView.xaml | MedicalCaseListViewModel.cs | `EditCommand` |
| MedicalCaseListView.xaml | MedicalCaseListViewModel.cs | `DeleteCommand` |
| MedicalCaseListView.xaml | MedicalCaseListViewModel.cs | `PreviousPageCommand` |
| MedicalCaseListView.xaml | MedicalCaseListViewModel.cs | `NextPageCommand` |
| MedicalCaseManagementView.xaml | MedicalCaseManagementViewModel.cs | `SearchCommand` |
| MedicalCaseManagementView.xaml | MedicalCaseManagementViewModel.cs | `AddCommand` |
| MedicalCaseManagementView.xaml | MedicalCaseManagementViewModel.cs | `RefreshCommand` |
| MedicalCaseManagementView.xaml | MedicalCaseManagementViewModel.cs | `ViewDetailsCommand` |
| MedicalCaseManagementView.xaml | MedicalCaseManagementViewModel.cs | `EditCommand` |
| MedicalCaseManagementView.xaml | MedicalCaseManagementViewModel.cs | `ViewConsultationCommand` |
| MedicalCaseManagementView.xaml | MedicalCaseManagementViewModel.cs | `CreatePrescriptionCommand` |
| MedicalCaseManagementView.xaml | MedicalCaseManagementViewModel.cs | `PrintCommand` |
| MedicalCaseManagementView.xaml | MedicalCaseManagementViewModel.cs | `DeleteCommand` |
| MedicalCaseManagementView.xaml | MedicalCaseManagementViewModel.cs | `FirstPageCommand` |
| MedicalCaseManagementView.xaml | MedicalCaseManagementViewModel.cs | `PreviousPageCommand` |
| MedicalCaseManagementView.xaml | MedicalCaseManagementViewModel.cs | `NextPageCommand` |
| MedicalCaseManagementView.xaml | MedicalCaseManagementViewModel.cs | `LastPageCommand` |
| PatientDetailView.xaml | PatientDetailViewModel.cs | `BackCommand` |
| PatientDetailView.xaml | PatientDetailViewModel.cs | `EditCommand` |
| PatientDetailView.xaml | PatientDetailViewModel.cs | `SaveCommand` |
| PatientDetailView.xaml | PatientDetailViewModel.cs | `CancelEditCommand` |
| PatientDetailView.xaml | PatientDetailViewModel.cs | `ViewMedicalHistoryCommand` |
| PatientDetailView.xaml | PatientDetailViewModel.cs | `PrintCommand` |
| PatientImportWizardView.xaml | PatientImportWizardViewModel.cs | `CancelCommand` |
| PatientImportWizardView.xaml | PatientImportWizardViewModel.cs | `PreviousCommand` |
| PatientImportWizardView.xaml | PatientImportWizardViewModel.cs | `NextCommand` |
| PrescriptionComposerView.xaml | PrescriptionComposerViewModel.cs | `AddHerbCommand` |
| PrescriptionComposerView.xaml | PrescriptionComposerViewModel.cs | `ImportFormulaCommand` |
| PrescriptionComposerView.xaml | PrescriptionComposerViewModel.cs | `ClearAllCommand` |
| PrescriptionComposerView.xaml | PrescriptionComposerViewModel.cs | `EditHerbCommand` |
| PrescriptionComposerView.xaml | PrescriptionComposerViewModel.cs | `RemoveHerbCommand` |
| PrescriptionComposerView.xaml | PrescriptionComposerViewModel.cs | `SaveDraftCommand` |
| PrescriptionComposerView.xaml | PrescriptionComposerViewModel.cs | `SavePrescriptionCommand` |
| PrescriptionComposerView.xaml | PrescriptionComposerViewModel.cs | `CloseCommand` |
| PrescriptionManagementView.xaml | PrescriptionManagementViewModel.cs | `AddPrescriptionCommand` |
| PrescriptionManagementView.xaml | PrescriptionManagementViewModel.cs | `ExportPrescriptionsCommand` |
| PrescriptionManagementView.xaml | PrescriptionManagementViewModel.cs | `RefreshCommand` |
| PrescriptionManagementView.xaml | PrescriptionManagementViewModel.cs | `ClearFiltersCommand` |
| PrescriptionManagementView.xaml | PrescriptionManagementViewModel.cs | `ViewPrescriptionCommand` |
| PrescriptionManagementView.xaml | PrescriptionManagementViewModel.cs | `EditPrescriptionCommand` |
| PrescriptionManagementView.xaml | PrescriptionManagementViewModel.cs | `ViewPatientHistoryCommand` |
| PrescriptionManagementView.xaml | PrescriptionManagementViewModel.cs | `CopyPrescriptionCommand` |
| PrescriptionManagementView.xaml | PrescriptionManagementViewModel.cs | `PrintCommand` |
| PrescriptionManagementView.xaml | PrescriptionManagementViewModel.cs | `DeletePrescriptionCommand` |
| PrescriptionsMainView.xaml | PrescriptionsMainViewModel.cs | `SwitchToManagementCommand` |
| PrescriptionsMainView.xaml | PrescriptionsMainViewModel.cs | `ReturnToSourceCommand` |
| PrescriptionsMainView.xaml | PrescriptionsMainViewModel.cs | `CreateNewPrescriptionCommand` |
| PrescriptionView.xaml | PrescriptionViewModel.cs | `AddHerbCommand` |
| PrescriptionView.xaml | PrescriptionViewModel.cs | `ImportFormulaCommand` |
| PrescriptionView.xaml | PrescriptionViewModel.cs | `RemoveHerbCommand` |
| PrescriptionView.xaml | PrescriptionViewModel.cs | `SetDosageCommand` |
| PrescriptionView.xaml | PrescriptionViewModel.cs | `SetDiscountCommand` |
| PrescriptionView.xaml | PrescriptionViewModel.cs | `ImportHistoryCommand` |
| PrescriptionView.xaml | PrescriptionViewModel.cs | `PrintPreviewCommand` |
| PrescriptionView.xaml | PrescriptionViewModel.cs | `ClearCommand` |
| PrescriptionView.xaml | PrescriptionViewModel.cs | `SaveCommand` |
| UserDetailView.xaml | UserDetailViewModel.cs | `GoBackCommand` |
| UserDetailView.xaml | UserDetailViewModel.cs | `EditUserCommand` |
| UserDetailView.xaml | UserDetailViewModel.cs | `ResetPasswordCommand` |
| UserManagementView.xaml | UserManagementViewModel.cs | `SearchCommand` |
| UserManagementView.xaml | UserManagementViewModel.cs | `AddCommand` |
| UserManagementView.xaml | UserManagementViewModel.cs | `RefreshCommand` |
| UserManagementView.xaml | UserManagementViewModel.cs | `ViewDetailsCommand` |
| UserManagementView.xaml | UserManagementViewModel.cs | `EditCommand` |
| UserManagementView.xaml | UserManagementViewModel.cs | `DeleteCommand` |
| UserManagementView.xaml | UserManagementViewModel.cs | `FirstPageCommand` |
| UserManagementView.xaml | UserManagementViewModel.cs | `PreviousPageCommand` |
| UserManagementView.xaml | UserManagementViewModel.cs | `NextPageCommand` |
| UserManagementView.xaml | UserManagementViewModel.cs | `LastPageCommand` |
| HomeView.xaml | HomeViewModel.cs | `LogoutCommand` |
| HomeView.xaml | HomeViewModel.cs | `StartConsultationCommand` |
| HomeView.xaml | HomeViewModel.cs | `RefreshTodayPatientsCommand` |
| HomeView.xaml | HomeViewModel.cs | `StartConsultationForPatientCommand` |
| HomeView.xaml | HomeViewModel.cs | `ViewPatientDetailsCommand` |
| HomeView.xaml | HomeViewModel.cs | `NavigateToPatientReceptionCommand` |
| HomeView.xaml | HomeViewModel.cs | `NavigateToMedicalCaseCommand` |
| HomeView.xaml | HomeViewModel.cs | `NavigateToPrescriptionQueryCommand` |
| HomeView.xaml | HomeViewModel.cs | `NavigateToPatientManagementCommand` |
| HomeView.xaml | HomeViewModel.cs | `NavigateToHerbsCommand` |
| HomeView.xaml | HomeViewModel.cs | `NavigateToFormulasCommand` |
| HomeView.xaml | HomeViewModel.cs | `EnterSystemManagementCommand` |
| HomeView.xaml | HomeViewModel.cs | `NavigateToUserManagementCommand` |
| HomeView.xaml | HomeViewModel.cs | `NavigateToHerbManagementCommand` |
| HomeView.xaml | HomeViewModel.cs | `NavigateToFormulaManagementCommand` |
| HomeView.xaml | HomeViewModel.cs | `NavigateToSystemSettingsCommand` |
| HomeView.xaml | HomeViewModel.cs | `NavigateToDataBackupCommand` |
| AdminWorkstationView.xaml | AdminWorkstationViewModel.cs | `NavigateCommand` |
| ClinicalWorkstationView.xaml | ClinicalWorkstationViewModel.cs | `NavigateCommand` |

</details>

## 📋 后续行动
✅ **所有命令绑定检查通过！无需修复。**

## 🔗 相关资源

- Issue #884: 全面检查所有模块的事件绑定
- 脚本位置: `scripts/analysis/check-command-bindings.ps1`

---
*此报告由自动化脚本生成*
