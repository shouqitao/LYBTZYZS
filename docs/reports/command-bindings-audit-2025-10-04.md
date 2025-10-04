# XAML Command 绑定检查报告

**生成时间**: 2025-10-04 10:21:02
**相关 Issue**: #884
**检查范围**: Desktop 所有模块

## 概述

| 指标 | 数量 |
|------|------|
| 总 View 数 | 36 |
| 总绑定数 | 242 |
| ✅ 正常绑定 | 213 |
| ❌ 缺失绑定 | 29 |
| ⚠️ 有问题的 View | 8 |

## 检查结果

### 模块: Modules

#### ✅ ChangePasswordDialog

| 命令 | 状态 |
|------|------|
| `CancelCommand` | ✅ 存在 |
| `ConfirmCommand` | ✅ 存在 |

#### ✅ ConsultationMainView

| 命令 | 状态 |
|------|------|
| `DecreaseQuantityCommand` | ✅ 存在 |
| `IncreaseQuantityCommand` | ✅ 存在 |
| `NewConsultationCommand` | ✅ 存在 |
| `PrintPrescriptionCommand` | ✅ 存在 |
| `RefreshCommand` | ✅ 存在 |
| `RemovePrescriptionItemCommand` | ✅ 存在 |
| `SaveConsultationCommand` | ✅ 存在 |
| `ShowTemplateMenuCommand` | ✅ 存在 |
| `ViewPatientHistoryCommand` | ✅ 存在 |

#### ✅ ConsultationManagementView

| 命令 | 状态 |
|------|------|
| `CopyRecordCommand` | ✅ 存在 |
| `FirstPageCommand` | ✅ 存在 |
| `LastPageCommand` | ✅ 存在 |
| `NextPageCommand` | ✅ 存在 |
| `PreviousPageCommand` | ✅ 存在 |
| `PrintCommand` | ✅ 存在 |
| `RefreshCommand` | ✅ 存在 |
| `SearchCommand` | ✅ 存在 |
| `SearchCommand` | ✅ 存在 |
| `StatisticsCommand` | ✅ 存在 |
| `ViewDetailsCommand` | ✅ 存在 |
| `ViewPrescriptionCommand` | ✅ 存在 |

#### ✅ CreateMedicalCaseDialog

| 命令 | 状态 |
|------|------|
| `CancelCommand` | ✅ 存在 |
| `SaveCommand` | ✅ 存在 |

#### ❌ EditFormulaDialog

| 命令 | 状态 |
|------|------|
| `AddHerbCommand` | ❌ 缺失 |
| `CancelCommand` | ✅ 存在 |
| `EditHerbCommand` | ❌ 缺失 |
| `RemoveHerbCommand` | ❌ 缺失 |
| `SaveCommand` | ✅ 存在 |

#### ✅ FormulaDetailView

| 命令 | 状态 |
|------|------|
| `BackCommand` | ✅ 存在 |
| `CancelEditCommand` | ✅ 存在 |
| `CopyFormulaCommand` | ✅ 存在 |
| `EditCommand` | ✅ 存在 |
| `PrintCommand` | ✅ 存在 |
| `SaveCommand` | ✅ 存在 |
| `ViewUsageHistoryCommand` | ✅ 存在 |

#### ✅ FormulaManagementView

| 命令 | 状态 |
|------|------|
| `AddFormulaCommand` | ✅ 存在 |
| `ClearFiltersCommand` | ✅ 存在 |
| `CopyCommand` | ✅ 存在 |
| `DeleteCommand` | ✅ 存在 |
| `EditCommand` | ✅ 存在 |
| `ExportFormulasCommand` | ✅ 存在 |
| `ExportTemplateCommand` | ✅ 存在 |
| `FirstPageCommand` | ✅ 存在 |
| `ImportFormulasCommand` | ✅ 存在 |
| `LastPageCommand` | ✅ 存在 |
| `NextPageCommand` | ✅ 存在 |
| `PreviousPageCommand` | ✅ 存在 |
| `RefreshCommand` | ✅ 存在 |
| `SearchCommand` | ✅ 存在 |
| `ViewDetailsCommand` | ✅ 存在 |

#### ❌ FormulaTemplateDialog

| 命令 | 状态 |
|------|------|
| `CancelCommand` | ✅ 存在 |
| `RefreshCommand` | ✅ 存在 |
| `SelectCommand` | ❌ 缺失 |
| `ViewDetailsCommand` | ❌ 缺失 |

#### ✅ HerbDetailView

| 命令 | 状态 |
|------|------|
| `BackCommand` | ✅ 存在 |
| `CancelEditCommand` | ✅ 存在 |
| `EditCommand` | ✅ 存在 |
| `PrintCommand` | ✅ 存在 |
| `SaveCommand` | ✅ 存在 |
| `ViewUsageHistoryCommand` | ✅ 存在 |

#### ✅ HerbManagementView

| 命令 | 状态 |
|------|------|
| `AddCommand` | ✅ 存在 |
| `DeleteCommand` | ✅ 存在 |
| `EditCommand` | ✅ 存在 |
| `ExportHerbsCommand` | ✅ 存在 |
| `ExportTemplateCommand` | ✅ 存在 |
| `FirstPageCommand` | ✅ 存在 |
| `ImportHerbsCommand` | ✅ 存在 |
| `LastPageCommand` | ✅ 存在 |
| `NextPageCommand` | ✅ 存在 |
| `PreviousPageCommand` | ✅ 存在 |
| `RefreshCommand` | ✅ 存在 |
| `SearchCommand` | ✅ 存在 |
| `SearchCommand` | ✅ 存在 |
| `ToggleStatusCommand` | ✅ 存在 |

#### ✅ HerbSelectionDialog

| 命令 | 状态 |
|------|------|
| `CancelCommand` | ✅ 存在 |
| `ConfirmCommand` | ✅ 存在 |
| `SearchCommand` | ✅ 存在 |
| `SearchCommand` | ✅ 存在 |

#### ✅ LoginView

| 命令 | 状态 |
|------|------|
| `LoginCommand` | ✅ 存在 |
| `LoginCommand` | ✅ 存在 |

#### ❌ LoginWindow

**⚠️ ViewModel 不存在**: `D:\source\repos\LYBTZYZS\src\Client\Desktop\Modules\LYBT.Desktop.Auth\ViewModels\LoginWindowViewModel.cs`

| 命令 | 状态 |
|------|------|
| `LoginCommand` | ❌ 缺失 |
| `LoginCommand` | ❌ 缺失 |
| `LoginCommand` | ❌ 缺失 |

#### ✅ MedicalCaseDetailView

| 命令 | 状态 |
|------|------|
| `BackCommand` | ✅ 存在 |
| `CloseCommand` | ✅ 存在 |
| `EditCommand` | ✅ 存在 |
| `PrintCommand` | ✅ 存在 |
| `PrintPrescriptionCommand` | ✅ 存在 |
| `RefreshCommand` | ✅ 存在 |
| `StartConsultationCommand` | ✅ 存在 |

#### ✅ MedicalCaseListView

| 命令 | 状态 |
|------|------|
| `AddCommand` | ✅ 存在 |
| `DeleteCommand` | ✅ 存在 |
| `EditCommand` | ✅ 存在 |
| `NextPageCommand` | ✅ 存在 |
| `PreviousPageCommand` | ✅ 存在 |
| `RefreshCommand` | ✅ 存在 |
| `SearchCommand` | ✅ 存在 |
| `StartConsultationCommand` | ✅ 存在 |
| `ViewDetailCommand` | ✅ 存在 |

#### ✅ MedicalCaseManagementView

| 命令 | 状态 |
|------|------|
| `AddCommand` | ✅ 存在 |
| `CreatePrescriptionCommand` | ✅ 存在 |
| `DeleteCommand` | ✅ 存在 |
| `EditCommand` | ✅ 存在 |
| `FirstPageCommand` | ✅ 存在 |
| `LastPageCommand` | ✅ 存在 |
| `NextPageCommand` | ✅ 存在 |
| `PreviousPageCommand` | ✅ 存在 |
| `PrintCommand` | ✅ 存在 |
| `RefreshCommand` | ✅ 存在 |
| `SearchCommand` | ✅ 存在 |
| `SearchCommand` | ✅ 存在 |
| `ViewConsultationCommand` | ✅ 存在 |
| `ViewDetailsCommand` | ✅ 存在 |

#### ✅ PatientDetailView

| 命令 | 状态 |
|------|------|
| `BackCommand` | ✅ 存在 |
| `CancelEditCommand` | ✅ 存在 |
| `EditCommand` | ✅ 存在 |
| `PrintCommand` | ✅ 存在 |
| `SaveCommand` | ✅ 存在 |
| `ViewMedicalHistoryCommand` | ✅ 存在 |
| `ViewMedicalHistoryCommand` | ✅ 存在 |

#### ✅ PatientImportWizardView

| 命令 | 状态 |
|------|------|
| `CancelCommand` | ✅ 存在 |
| `NextCommand` | ✅ 存在 |
| `PreviousCommand` | ✅ 存在 |

#### ✅ PrescriptionComposerView

| 命令 | 状态 |
|------|------|
| `AddHerbCommand` | ✅ 存在 |
| `ClearAllCommand` | ✅ 存在 |
| `CloseCommand` | ✅ 存在 |
| `EditHerbCommand` | ✅ 存在 |
| `ImportFormulaCommand` | ✅ 存在 |
| `RemoveHerbCommand` | ✅ 存在 |
| `SaveDraftCommand` | ✅ 存在 |
| `SavePrescriptionCommand` | ✅ 存在 |

#### ❌ PrescriptionEditorDialog

| 命令 | 状态 |
|------|------|
| `AddHerbCommand` | ❌ 缺失 |
| `CancelCommand` | ✅ 存在 |
| `EditHerbCommand` | ❌ 缺失 |
| `LoadFormulaTemplateCommand` | ❌ 缺失 |
| `PreviewCommand` | ❌ 缺失 |
| `RemoveHerbCommand` | ❌ 缺失 |
| `SaveCommand` | ✅ 存在 |

#### ✅ PrescriptionManagementView

| 命令 | 状态 |
|------|------|
| `AddPrescriptionCommand` | ✅ 存在 |
| `ClearFiltersCommand` | ✅ 存在 |
| `CopyPrescriptionCommand` | ✅ 存在 |
| `DeletePrescriptionCommand` | ✅ 存在 |
| `EditPrescriptionCommand` | ✅ 存在 |
| `ExportPrescriptionsCommand` | ✅ 存在 |
| `PrintCommand` | ✅ 存在 |
| `RefreshCommand` | ✅ 存在 |
| `ViewPatientHistoryCommand` | ✅ 存在 |
| `ViewPrescriptionCommand` | ✅ 存在 |

#### ✅ PrescriptionsMainView

| 命令 | 状态 |
|------|------|
| `CreateNewPrescriptionCommand` | ✅ 存在 |
| `ReturnToSourceCommand` | ✅ 存在 |
| `SwitchToManagementCommand` | ✅ 存在 |
| `SwitchToManagementCommand` | ✅ 存在 |

#### ❌ PrescriptionView

**⚠️ ViewModel 不存在**: `D:\source\repos\LYBTZYZS\src\Client\Desktop\Modules\LYBT.Desktop.Prescriptions\ViewModels\PrescriptionViewModel.cs`

| 命令 | 状态 |
|------|------|
| `AddHerbCommand` | ❌ 缺失 |
| `ClearCommand` | ❌ 缺失 |
| `ImportFormulaCommand` | ❌ 缺失 |
| `ImportHistoryCommand` | ❌ 缺失 |
| `PrintPreviewCommand` | ❌ 缺失 |
| `RemoveHerbCommand` | ❌ 缺失 |
| `SaveCommand` | ❌ 缺失 |
| `SetDiscountCommand` | ❌ 缺失 |
| `SetDiscountCommand` | ❌ 缺失 |
| `SetDosageCommand` | ❌ 缺失 |

#### ✅ ResetPasswordDialog

| 命令 | 状态 |
|------|------|
| `CancelCommand` | ✅ 存在 |
| `ConfirmCommand` | ✅ 存在 |
| `GeneratePasswordCommand` | ✅ 存在 |

#### ✅ SelectFormulaDialog

| 命令 | 状态 |
|------|------|
| `CancelCommand` | ✅ 存在 |
| `ConfirmCommand` | ✅ 存在 |
| `ConfirmCommand` | ✅ 存在 |
| `RefreshCommand` | ✅ 存在 |
| `SearchCommand` | ✅ 存在 |
| `ViewDetailsCommand` | ✅ 存在 |

#### ❌ UserDetailView

**⚠️ ViewModel 不存在**: `D:\source\repos\LYBTZYZS\src\Client\Desktop\Modules\LYBT.Desktop.Users\ViewModels\UserDetailViewModel.cs`

| 命令 | 状态 |
|------|------|
| `EditUserCommand` | ❌ 缺失 |
| `GoBackCommand` | ❌ 缺失 |
| `ResetPasswordCommand` | ❌ 缺失 |

#### ✅ UserManagementView

| 命令 | 状态 |
|------|------|
| `AddCommand` | ✅ 存在 |
| `DeleteCommand` | ✅ 存在 |
| `EditCommand` | ✅ 存在 |
| `FirstPageCommand` | ✅ 存在 |
| `LastPageCommand` | ✅ 存在 |
| `NextPageCommand` | ✅ 存在 |
| `PreviousPageCommand` | ✅ 存在 |
| `RefreshCommand` | ✅ 存在 |
| `SearchCommand` | ✅ 存在 |
| `SearchCommand` | ✅ 存在 |
| `ViewDetailsCommand` | ✅ 存在 |

#### ✅ UserProfileDialog

| 命令 | 状态 |
|------|------|
| `CancelCommand` | ✅ 存在 |
| `RemoveAvatarCommand` | ✅ 存在 |
| `SaveCommand` | ✅ 存在 |
| `SelectAvatarCommand` | ✅ 存在 |

#### ✅ ViewFormulaDialog

| 命令 | 状态 |
|------|------|
| `CloseCommand` | ✅ 存在 |
| `ExportCommand` | ✅ 存在 |
| `PrintCommand` | ✅ 存在 |


### 模块: Shell

#### ❌ ConfirmationDialog

| 命令 | 状态 |
|------|------|
| `NoCommand` | ❌ 缺失 |
| `YesCommand` | ❌ 缺失 |

#### ✅ ErrorDetailsDialog

| 命令 | 状态 |
|------|------|
| `CloseCommand` | ✅ 存在 |
| `CopyErrorCommand` | ✅ 存在 |
| `RetryCommand` | ✅ 存在 |

#### ✅ HomeView

| 命令 | 状态 |
|------|------|
| `EnterSystemManagementCommand` | ✅ 存在 |
| `LogoutCommand` | ✅ 存在 |
| `NavigateToDataBackupCommand` | ✅ 存在 |
| `NavigateToFormulaManagementCommand` | ✅ 存在 |
| `NavigateToFormulasCommand` | ✅ 存在 |
| `NavigateToHerbManagementCommand` | ✅ 存在 |
| `NavigateToHerbsCommand` | ✅ 存在 |
| `NavigateToMedicalCaseCommand` | ✅ 存在 |
| `NavigateToPatientManagementCommand` | ✅ 存在 |
| `NavigateToPatientManagementCommand` | ✅ 存在 |
| `NavigateToPatientReceptionCommand` | ✅ 存在 |
| `NavigateToPrescriptionQueryCommand` | ✅ 存在 |
| `NavigateToSystemSettingsCommand` | ✅ 存在 |
| `NavigateToUserManagementCommand` | ✅ 存在 |
| `RefreshTodayPatientsCommand` | ✅ 存在 |
| `StartConsultationCommand` | ✅ 存在 |
| `StartConsultationForPatientCommand` | ✅ 存在 |
| `StartConsultationForPatientCommand` | ✅ 存在 |
| `ViewPatientDetailsCommand` | ✅ 存在 |

#### ❌ InformationDialog

| 命令 | 状态 |
|------|------|
| `OkCommand` | ❌ 缺失 |

#### ✅ MainWindow

| 命令 | 状态 |
|------|------|
| `LogoutCommand` | ✅ 存在 |
| `QuickAddPatientCommand` | ✅ 存在 |
| `QuickStartConsultationCommand` | ✅ 存在 |
| `ShowHelpCommand` | ✅ 存在 |
| `ShowSettingsCommand` | ✅ 存在 |
| `TestApiCommand` | ✅ 存在 |
| `ToggleThemeCommand` | ✅ 存在 |


### 模块: Workstations

#### ✅ AdminWorkstationView

| 命令 | 状态 |
|------|------|
| `LogoutCommand` | ✅ 存在 |
| `NavigateCommand` | ✅ 存在 |
| `NavigateCommand` | ✅ 存在 |
| `NavigateCommand` | ✅ 存在 |
| `NavigateCommand` | ✅ 存在 |
| `NavigateCommand` | ✅ 存在 |
| `NavigateCommand` | ✅ 存在 |

#### ✅ ClinicalWorkstationView

| 命令 | 状态 |
|------|------|
| `ClearPrescriptionCommand` | ✅ 存在 |
| `ImportDiagnosisCommand` | ✅ 存在 |
| `ImportFormulaCommand` | ✅ 存在 |
| `LogoutCommand` | ✅ 存在 |
| `PrintPrescriptionCommand` | ✅ 存在 |
| `SavePrescriptionCommand` | ✅ 存在 |
| `SearchHerbCommand` | ✅ 存在 |
| `SelectPatientCommand` | ✅ 存在 |
| `ShowHistoryCommand` | ✅ 存在 |


## 需要修复的问题

### ConfirmationDialog

**ViewModel**: `ConfirmationDialogViewModel`

**缺失命令**:

- [ ] `NoCommand`
- [ ] `YesCommand`

### EditFormulaDialog

**ViewModel**: `EditFormulaDialogViewModel`

**缺失命令**:

- [ ] `AddHerbCommand`
- [ ] `EditHerbCommand`
- [ ] `RemoveHerbCommand`

### FormulaTemplateDialog

**ViewModel**: `FormulaTemplateDialogViewModel`

**缺失命令**:

- [ ] `SelectCommand`
- [ ] `ViewDetailsCommand`

### InformationDialog

**ViewModel**: `InformationDialogViewModel`

**缺失命令**:

- [ ] `OkCommand`

### LoginWindow

**ViewModel**: `LoginWindowViewModel`

**缺失命令**:

- [ ] `LoginCommand`
- [ ] `LoginCommand`
- [ ] `LoginCommand`

### PrescriptionEditorDialog

**ViewModel**: `PrescriptionEditorDialogViewModel`

**缺失命令**:

- [ ] `AddHerbCommand`
- [ ] `EditHerbCommand`
- [ ] `LoadFormulaTemplateCommand`
- [ ] `PreviewCommand`
- [ ] `RemoveHerbCommand`

### PrescriptionView

**ViewModel**: `PrescriptionViewModel`

**缺失命令**:

- [ ] `AddHerbCommand`
- [ ] `ClearCommand`
- [ ] `ImportFormulaCommand`
- [ ] `ImportHistoryCommand`
- [ ] `PrintPreviewCommand`
- [ ] `RemoveHerbCommand`
- [ ] `SaveCommand`
- [ ] `SetDiscountCommand`
- [ ] `SetDiscountCommand`
- [ ] `SetDosageCommand`

### UserDetailView

**ViewModel**: `UserDetailViewModel`

**缺失命令**:

- [ ] `EditUserCommand`
- [ ] `GoBackCommand`
- [ ] `ResetPasswordCommand`


## 结论

❌ **发现 29 个缺失的命令绑定，需要立即修复。**

建议为每个有问题的模块创建独立的修复 Issue。

## 下一步行动

1. 为每个有问题的模块创建修复 Issue
2. 实现缺失的命令
3. 手动测试所有修复的绑定
4. 回归测试确保无副作用

---
*此报告由自动化脚本生成：`scripts/analysis/check-command-bindings.ps1`*

