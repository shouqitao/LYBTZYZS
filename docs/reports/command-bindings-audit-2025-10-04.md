# XAML Command 绑定检查报告

**生成时间**: 2025-10-04 08:19:44
**相关 Issue**: #884
**检查范围**: Desktop 所有模块

## 概述

| 指标 | 数量 |
|------|------|
| 总 View 数 | 36 |
| 总绑定数 | 242 |
| ✅ 正常绑定 | 53 |
| ❌ 缺失绑定 | 189 |
| ⚠️ 有问题的 View | 32 |

## 检查结果

### 模块: Modules

#### ❌ ChangePasswordDialog

**⚠️ ViewModel 不存在**: `D:\source\repos\LYBTZYZS\src\Client\Desktop\Modules\LYBT.Desktop.Users\ViewModels\ChangePasswordDialog.cs`

| 命令 | 状态 |
|------|------|
| `CancelCommand` | ❌ 缺失 |
| `ConfirmCommand` | ❌ 缺失 |

#### ❌ ConsultationMainView

| 命令 | 状态 |
|------|------|
| `DataContext.DecreaseQuantityCommand` | ❌ 缺失 |
| `DataContext.IncreaseQuantityCommand` | ❌ 缺失 |
| `DataContext.RemovePrescriptionItemCommand` | ❌ 缺失 |
| `NewConsultationCommand` | ❌ 缺失 |
| `PrintPrescriptionCommand` | ❌ 缺失 |
| `RefreshCommand` | ❌ 缺失 |
| `SaveConsultationCommand` | ✅ 存在 |
| `ShowTemplateMenuCommand` | ✅ 存在 |
| `ViewPatientHistoryCommand` | ✅ 存在 |

#### ❌ ConsultationManagementView

| 命令 | 状态 |
|------|------|
| `DataContext.CopyRecordCommand` | ❌ 缺失 |
| `DataContext.PrintCommand` | ❌ 缺失 |
| `DataContext.ViewDetailsCommand` | ❌ 缺失 |
| `DataContext.ViewPrescriptionCommand` | ❌ 缺失 |
| `FirstPageCommand` | ❌ 缺失 |
| `LastPageCommand` | ❌ 缺失 |
| `NextPageCommand` | ❌ 缺失 |
| `PreviousPageCommand` | ❌ 缺失 |
| `RefreshCommand` | ✅ 存在 |
| `SearchCommand` | ✅ 存在 |
| `SearchCommand` | ✅ 存在 |
| `StatisticsCommand` | ❌ 缺失 |

#### ❌ CreateMedicalCaseDialog

**⚠️ ViewModel 不存在**: `D:\source\repos\LYBTZYZS\src\Client\Desktop\Modules\LYBT.Desktop.MedicalCase\ViewModels\CreateMedicalCaseDialog.cs`

| 命令 | 状态 |
|------|------|
| `CancelCommand` | ❌ 缺失 |
| `SaveCommand` | ❌ 缺失 |

#### ❌ EditFormulaDialog

**⚠️ ViewModel 不存在**: `D:\source\repos\LYBTZYZS\src\Client\Desktop\Modules\LYBT.Desktop.Formula\ViewModels\EditFormulaDialog.cs`

| 命令 | 状态 |
|------|------|
| `AddHerbCommand` | ❌ 缺失 |
| `CancelCommand` | ❌ 缺失 |
| `DataContext.EditHerbCommand` | ❌ 缺失 |
| `DataContext.RemoveHerbCommand` | ❌ 缺失 |
| `SaveCommand` | ❌ 缺失 |

#### ❌ FormulaDetailView

| 命令 | 状态 |
|------|------|
| `BackCommand` | ✅ 存在 |
| `CancelEditCommand` | ✅ 存在 |
| `CopyFormulaCommand` | ✅ 存在 |
| `EditCommand` | ✅ 存在 |
| `PrintCommand` | ❌ 缺失 |
| `SaveCommand` | ✅ 存在 |
| `ViewUsageHistoryCommand` | ❌ 缺失 |

#### ❌ FormulaManagementView

| 命令 | 状态 |
|------|------|
| `AddFormulaCommand` | ❌ 缺失 |
| `ClearFiltersCommand` | ❌ 缺失 |
| `DataContext.CopyCommand` | ❌ 缺失 |
| `DataContext.DeleteCommand` | ❌ 缺失 |
| `DataContext.EditCommand` | ❌ 缺失 |
| `DataContext.ViewDetailsCommand` | ❌ 缺失 |
| `ExportFormulasCommand` | ❌ 缺失 |
| `ExportTemplateCommand` | ❌ 缺失 |
| `FirstPageCommand` | ❌ 缺失 |
| `ImportFormulasCommand` | ❌ 缺失 |
| `LastPageCommand` | ❌ 缺失 |
| `NextPageCommand` | ❌ 缺失 |
| `PreviousPageCommand` | ❌ 缺失 |
| `RefreshCommand` | ❌ 缺失 |
| `SearchCommand` | ❌ 缺失 |

#### ❌ FormulaTemplateDialog

**⚠️ ViewModel 不存在**: `D:\source\repos\LYBTZYZS\src\Client\Desktop\Modules\LYBT.Desktop.Prescriptions\ViewModels\FormulaTemplateDialog.cs`

| 命令 | 状态 |
|------|------|
| `CancelCommand` | ❌ 缺失 |
| `DataContext.ViewDetailsCommand` | ❌ 缺失 |
| `RefreshCommand` | ❌ 缺失 |
| `SelectCommand` | ❌ 缺失 |

#### ❌ HerbDetailView

| 命令 | 状态 |
|------|------|
| `BackCommand` | ❌ 缺失 |
| `CancelEditCommand` | ❌ 缺失 |
| `EditCommand` | ❌ 缺失 |
| `PrintCommand` | ❌ 缺失 |
| `SaveCommand` | ✅ 存在 |
| `ViewUsageHistoryCommand` | ❌ 缺失 |

#### ❌ HerbManagementView

| 命令 | 状态 |
|------|------|
| `AddCommand` | ❌ 缺失 |
| `DataContext.DeleteCommand` | ❌ 缺失 |
| `DataContext.EditCommand` | ❌ 缺失 |
| `DataContext.ToggleStatusCommand` | ❌ 缺失 |
| `ExportHerbsCommand` | ✅ 存在 |
| `ExportTemplateCommand` | ✅ 存在 |
| `FirstPageCommand` | ✅ 存在 |
| `ImportHerbsCommand` | ✅ 存在 |
| `LastPageCommand` | ✅ 存在 |
| `NextPageCommand` | ❌ 缺失 |
| `PreviousPageCommand` | ❌ 缺失 |
| `RefreshCommand` | ❌ 缺失 |
| `SearchCommand` | ❌ 缺失 |
| `SearchCommand` | ❌ 缺失 |

#### ❌ HerbSelectionDialog

**⚠️ ViewModel 不存在**: `D:\source\repos\LYBTZYZS\src\Client\Desktop\Modules\LYBT.Desktop.Prescriptions\ViewModels\HerbSelectionDialog.cs`

| 命令 | 状态 |
|------|------|
| `CancelCommand` | ❌ 缺失 |
| `ConfirmCommand` | ❌ 缺失 |
| `SearchCommand` | ❌ 缺失 |
| `SearchCommand` | ❌ 缺失 |

#### ✅ LoginView

| 命令 | 状态 |
|------|------|
| `LoginCommand` | ✅ 存在 |
| `LoginCommand` | ✅ 存在 |

#### ❌ LoginWindow

**⚠️ ViewModel 不存在**: `D:\source\repos\LYBTZYZS\src\Client\Desktop\Modules\LYBT.Desktop.Auth\ViewModels\LoginWindow.cs`

| 命令 | 状态 |
|------|------|
| `LoginCommand` | ❌ 缺失 |
| `LoginCommand` | ❌ 缺失 |
| `LoginCommand` | ❌ 缺失 |

#### ❌ MedicalCaseDetailView

| 命令 | 状态 |
|------|------|
| `BackCommand` | ❌ 缺失 |
| `CloseCommand` | ❌ 缺失 |
| `EditCommand` | ❌ 缺失 |
| `PrintCommand` | ❌ 缺失 |
| `PrintPrescriptionCommand` | ❌ 缺失 |
| `RefreshCommand` | ❌ 缺失 |
| `StartConsultationCommand` | ❌ 缺失 |

#### ❌ MedicalCaseListView

| 命令 | 状态 |
|------|------|
| `AddCommand` | ❌ 缺失 |
| `DataContext.DeleteCommand` | ❌ 缺失 |
| `DataContext.EditCommand` | ❌ 缺失 |
| `DataContext.StartConsultationCommand` | ❌ 缺失 |
| `DataContext.ViewDetailCommand` | ❌ 缺失 |
| `NextPageCommand` | ✅ 存在 |
| `PreviousPageCommand` | ✅ 存在 |
| `RefreshCommand` | ❌ 缺失 |
| `SearchCommand` | ✅ 存在 |

#### ❌ MedicalCaseManagementView

| 命令 | 状态 |
|------|------|
| `AddCommand` | ❌ 缺失 |
| `DataContext.CreatePrescriptionCommand` | ❌ 缺失 |
| `DataContext.DeleteCommand` | ❌ 缺失 |
| `DataContext.EditCommand` | ❌ 缺失 |
| `DataContext.PrintCommand` | ❌ 缺失 |
| `DataContext.ViewConsultationCommand` | ❌ 缺失 |
| `DataContext.ViewDetailsCommand` | ❌ 缺失 |
| `FirstPageCommand` | ❌ 缺失 |
| `LastPageCommand` | ❌ 缺失 |
| `NextPageCommand` | ❌ 缺失 |
| `PreviousPageCommand` | ❌ 缺失 |
| `RefreshCommand` | ✅ 存在 |
| `SearchCommand` | ❌ 缺失 |
| `SearchCommand` | ❌ 缺失 |

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

#### ❌ PrescriptionComposerView

| 命令 | 状态 |
|------|------|
| `AddHerbCommand` | ✅ 存在 |
| `ClearAllCommand` | ❌ 缺失 |
| `CloseCommand` | ❌ 缺失 |
| `DataContext.EditHerbCommand` | ❌ 缺失 |
| `DataContext.RemoveHerbCommand` | ❌ 缺失 |
| `ImportFormulaCommand` | ✅ 存在 |
| `SaveDraftCommand` | ❌ 缺失 |
| `SavePrescriptionCommand` | ❌ 缺失 |

#### ❌ PrescriptionEditorDialog

**⚠️ ViewModel 不存在**: `D:\source\repos\LYBTZYZS\src\Client\Desktop\Modules\LYBT.Desktop.Prescriptions\ViewModels\PrescriptionEditorDialog.cs`

| 命令 | 状态 |
|------|------|
| `AddHerbCommand` | ❌ 缺失 |
| `CancelCommand` | ❌ 缺失 |
| `DataContext.EditHerbCommand` | ❌ 缺失 |
| `DataContext.RemoveHerbCommand` | ❌ 缺失 |
| `LoadFormulaTemplateCommand` | ❌ 缺失 |
| `PreviewCommand` | ❌ 缺失 |
| `SaveCommand` | ❌ 缺失 |

#### ❌ PrescriptionManagementView

| 命令 | 状态 |
|------|------|
| `AddPrescriptionCommand` | ❌ 缺失 |
| `ClearFiltersCommand` | ❌ 缺失 |
| `DataContext.CopyPrescriptionCommand` | ❌ 缺失 |
| `DataContext.DeletePrescriptionCommand` | ❌ 缺失 |
| `DataContext.EditPrescriptionCommand` | ❌ 缺失 |
| `DataContext.PrintCommand` | ❌ 缺失 |
| `DataContext.ViewPatientHistoryCommand` | ❌ 缺失 |
| `DataContext.ViewPrescriptionCommand` | ❌ 缺失 |
| `ExportPrescriptionsCommand` | ❌ 缺失 |
| `RefreshCommand` | ✅ 存在 |

#### ❌ PrescriptionsMainView

| 命令 | 状态 |
|------|------|
| `CreateNewPrescriptionCommand` | ❌ 缺失 |
| `ReturnToSourceCommand` | ❌ 缺失 |
| `SwitchToManagementCommand` | ❌ 缺失 |
| `SwitchToManagementCommand` | ❌ 缺失 |

#### ❌ PrescriptionView

**⚠️ ViewModel 不存在**: `D:\source\repos\LYBTZYZS\src\Client\Desktop\Modules\LYBT.Desktop.Prescriptions\ViewModels\PrescriptionViewModel.cs`

| 命令 | 状态 |
|------|------|
| `AddHerbCommand` | ❌ 缺失 |
| `ClearCommand` | ❌ 缺失 |
| `DataContext.RemoveHerbCommand` | ❌ 缺失 |
| `DataContext.SetDosageCommand` | ❌ 缺失 |
| `ImportFormulaCommand` | ❌ 缺失 |
| `ImportHistoryCommand` | ❌ 缺失 |
| `PrintPreviewCommand` | ❌ 缺失 |
| `SaveCommand` | ❌ 缺失 |
| `SetDiscountCommand` | ❌ 缺失 |
| `SetDiscountCommand` | ❌ 缺失 |

#### ❌ ResetPasswordDialog

**⚠️ ViewModel 不存在**: `D:\source\repos\LYBTZYZS\src\Client\Desktop\Modules\LYBT.Desktop.Users\ViewModels\ResetPasswordDialog.cs`

| 命令 | 状态 |
|------|------|
| `CancelCommand` | ❌ 缺失 |
| `ConfirmCommand` | ❌ 缺失 |
| `GeneratePasswordCommand` | ❌ 缺失 |

#### ❌ SelectFormulaDialog

**⚠️ ViewModel 不存在**: `D:\source\repos\LYBTZYZS\src\Client\Desktop\Modules\LYBT.Desktop.Prescriptions\ViewModels\SelectFormulaDialog.cs`

| 命令 | 状态 |
|------|------|
| `CancelCommand` | ❌ 缺失 |
| `ConfirmCommand` | ❌ 缺失 |
| `ConfirmCommand` | ❌ 缺失 |
| `DataContext.ViewDetailsCommand` | ❌ 缺失 |
| `RefreshCommand` | ❌ 缺失 |
| `SearchCommand` | ❌ 缺失 |

#### ❌ UserDetailView

**⚠️ ViewModel 不存在**: `D:\source\repos\LYBTZYZS\src\Client\Desktop\Modules\LYBT.Desktop.Users\ViewModels\UserDetailViewModel.cs`

| 命令 | 状态 |
|------|------|
| `EditUserCommand` | ❌ 缺失 |
| `GoBackCommand` | ❌ 缺失 |
| `ResetPasswordCommand` | ❌ 缺失 |

#### ❌ UserManagementView

| 命令 | 状态 |
|------|------|
| `AddCommand` | ❌ 缺失 |
| `DataContext.DeleteCommand` | ❌ 缺失 |
| `DataContext.EditCommand` | ❌ 缺失 |
| `DataContext.ViewDetailsCommand` | ❌ 缺失 |
| `FirstPageCommand` | ❌ 缺失 |
| `LastPageCommand` | ❌ 缺失 |
| `NextPageCommand` | ❌ 缺失 |
| `PreviousPageCommand` | ❌ 缺失 |
| `RefreshCommand` | ❌ 缺失 |
| `SearchCommand` | ❌ 缺失 |
| `SearchCommand` | ❌ 缺失 |

#### ❌ UserProfileDialog

**⚠️ ViewModel 不存在**: `D:\source\repos\LYBTZYZS\src\Client\Desktop\Modules\LYBT.Desktop.Users\ViewModels\UserProfileDialog.cs`

| 命令 | 状态 |
|------|------|
| `CancelCommand` | ❌ 缺失 |
| `RemoveAvatarCommand` | ❌ 缺失 |
| `SaveCommand` | ❌ 缺失 |
| `SelectAvatarCommand` | ❌ 缺失 |

#### ❌ ViewFormulaDialog

**⚠️ ViewModel 不存在**: `D:\source\repos\LYBTZYZS\src\Client\Desktop\Modules\LYBT.Desktop.Formula\ViewModels\ViewFormulaDialog.cs`

| 命令 | 状态 |
|------|------|
| `CloseCommand` | ❌ 缺失 |
| `ExportCommand` | ❌ 缺失 |
| `PrintCommand` | ❌ 缺失 |


### 模块: Shell

#### ❌ ConfirmationDialog

**⚠️ ViewModel 不存在**: `D:\source\repos\LYBTZYZS\src\Client\Desktop\Shell\Dialogs\ViewModels\ConfirmationDialog.cs`

| 命令 | 状态 |
|------|------|
| `NoCommand` | ❌ 缺失 |
| `YesCommand` | ❌ 缺失 |

#### ❌ ErrorDetailsDialog

**⚠️ ViewModel 不存在**: `D:\source\repos\LYBTZYZS\src\Client\Desktop\Shell\Dialogs\ViewModels\ErrorDetailsDialog.cs`

| 命令 | 状态 |
|------|------|
| `CloseCommand` | ❌ 缺失 |
| `CopyErrorCommand` | ❌ 缺失 |
| `RetryCommand` | ❌ 缺失 |

#### ❌ HomeView

| 命令 | 状态 |
|------|------|
| `DataContext.StartConsultationForPatientCommand` | ❌ 缺失 |
| `DataContext.StartConsultationForPatientCommand` | ❌ 缺失 |
| `DataContext.ViewPatientDetailsCommand` | ❌ 缺失 |
| `EnterSystemManagementCommand` | ❌ 缺失 |
| `LogoutCommand` | ❌ 缺失 |
| `NavigateToDataBackupCommand` | ❌ 缺失 |
| `NavigateToFormulaManagementCommand` | ❌ 缺失 |
| `NavigateToFormulasCommand` | ❌ 缺失 |
| `NavigateToHerbManagementCommand` | ❌ 缺失 |
| `NavigateToHerbsCommand` | ❌ 缺失 |
| `NavigateToMedicalCaseCommand` | ❌ 缺失 |
| `NavigateToPatientManagementCommand` | ✅ 存在 |
| `NavigateToPatientManagementCommand` | ✅ 存在 |
| `NavigateToPatientReceptionCommand` | ❌ 缺失 |
| `NavigateToPrescriptionQueryCommand` | ❌ 缺失 |
| `NavigateToSystemSettingsCommand` | ❌ 缺失 |
| `NavigateToUserManagementCommand` | ❌ 缺失 |
| `RefreshTodayPatientsCommand` | ❌ 缺失 |
| `StartConsultationCommand` | ❌ 缺失 |

#### ❌ InformationDialog

**⚠️ ViewModel 不存在**: `D:\source\repos\LYBTZYZS\src\Client\Desktop\Shell\Dialogs\ViewModels\InformationDialog.cs`

| 命令 | 状态 |
|------|------|
| `OkCommand` | ❌ 缺失 |

#### ❌ MainWindow

**⚠️ ViewModel 不存在**: `D:\source\repos\LYBTZYZS\src\Client\Desktop\Shell\ViewModels\MainWindow.cs`

| 命令 | 状态 |
|------|------|
| `LogoutCommand` | ❌ 缺失 |
| `QuickAddPatientCommand` | ❌ 缺失 |
| `QuickStartConsultationCommand` | ❌ 缺失 |
| `ShowHelpCommand` | ❌ 缺失 |
| `ShowSettingsCommand` | ❌ 缺失 |
| `TestApiCommand` | ❌ 缺失 |
| `ToggleThemeCommand` | ❌ 缺失 |


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

#### ❌ ClinicalWorkstationView

| 命令 | 状态 |
|------|------|
| `ClearPrescriptionCommand` | ✅ 存在 |
| `DataContext.ImportDiagnosisCommand` | ❌ 缺失 |
| `ImportFormulaCommand` | ✅ 存在 |
| `LogoutCommand` | ✅ 存在 |
| `PrintPrescriptionCommand` | ✅ 存在 |
| `SavePrescriptionCommand` | ✅ 存在 |
| `SearchHerbCommand` | ✅ 存在 |
| `SelectPatientCommand` | ✅ 存在 |
| `ShowHistoryCommand` | ✅ 存在 |


## 需要修复的问题

### ChangePasswordDialog

**ViewModel**: `ChangePasswordDialog`

**缺失命令**:

- [ ] `CancelCommand`
- [ ] `ConfirmCommand`

### ClinicalWorkstationView

**ViewModel**: `ClinicalWorkstationViewModel`

**缺失命令**:

- [ ] `DataContext.ImportDiagnosisCommand`

### ConfirmationDialog

**ViewModel**: `ConfirmationDialog`

**缺失命令**:

- [ ] `NoCommand`
- [ ] `YesCommand`

### ConsultationMainView

**ViewModel**: `ConsultationMainViewModel`

**缺失命令**:

- [ ] `DataContext.DecreaseQuantityCommand`
- [ ] `DataContext.IncreaseQuantityCommand`
- [ ] `DataContext.RemovePrescriptionItemCommand`
- [ ] `NewConsultationCommand`
- [ ] `PrintPrescriptionCommand`
- [ ] `RefreshCommand`

### ConsultationManagementView

**ViewModel**: `ConsultationManagementViewModel`

**缺失命令**:

- [ ] `DataContext.CopyRecordCommand`
- [ ] `DataContext.PrintCommand`
- [ ] `DataContext.ViewDetailsCommand`
- [ ] `DataContext.ViewPrescriptionCommand`
- [ ] `FirstPageCommand`
- [ ] `LastPageCommand`
- [ ] `NextPageCommand`
- [ ] `PreviousPageCommand`
- [ ] `StatisticsCommand`

### CreateMedicalCaseDialog

**ViewModel**: `CreateMedicalCaseDialog`

**缺失命令**:

- [ ] `CancelCommand`
- [ ] `SaveCommand`

### EditFormulaDialog

**ViewModel**: `EditFormulaDialog`

**缺失命令**:

- [ ] `AddHerbCommand`
- [ ] `CancelCommand`
- [ ] `DataContext.EditHerbCommand`
- [ ] `DataContext.RemoveHerbCommand`
- [ ] `SaveCommand`

### ErrorDetailsDialog

**ViewModel**: `ErrorDetailsDialog`

**缺失命令**:

- [ ] `CloseCommand`
- [ ] `CopyErrorCommand`
- [ ] `RetryCommand`

### FormulaDetailView

**ViewModel**: `FormulaDetailViewModel`

**缺失命令**:

- [ ] `PrintCommand`
- [ ] `ViewUsageHistoryCommand`

### FormulaManagementView

**ViewModel**: `FormulaManagementViewModel`

**缺失命令**:

- [ ] `AddFormulaCommand`
- [ ] `ClearFiltersCommand`
- [ ] `DataContext.CopyCommand`
- [ ] `DataContext.DeleteCommand`
- [ ] `DataContext.EditCommand`
- [ ] `DataContext.ViewDetailsCommand`
- [ ] `ExportFormulasCommand`
- [ ] `ExportTemplateCommand`
- [ ] `FirstPageCommand`
- [ ] `ImportFormulasCommand`
- [ ] `LastPageCommand`
- [ ] `NextPageCommand`
- [ ] `PreviousPageCommand`
- [ ] `RefreshCommand`
- [ ] `SearchCommand`

### FormulaTemplateDialog

**ViewModel**: `FormulaTemplateDialog`

**缺失命令**:

- [ ] `CancelCommand`
- [ ] `DataContext.ViewDetailsCommand`
- [ ] `RefreshCommand`
- [ ] `SelectCommand`

### HerbDetailView

**ViewModel**: `HerbDetailViewModel`

**缺失命令**:

- [ ] `BackCommand`
- [ ] `CancelEditCommand`
- [ ] `EditCommand`
- [ ] `PrintCommand`
- [ ] `ViewUsageHistoryCommand`

### HerbManagementView

**ViewModel**: `HerbManagementViewModel`

**缺失命令**:

- [ ] `AddCommand`
- [ ] `DataContext.DeleteCommand`
- [ ] `DataContext.EditCommand`
- [ ] `DataContext.ToggleStatusCommand`
- [ ] `NextPageCommand`
- [ ] `PreviousPageCommand`
- [ ] `RefreshCommand`
- [ ] `SearchCommand`
- [ ] `SearchCommand`

### HerbSelectionDialog

**ViewModel**: `HerbSelectionDialog`

**缺失命令**:

- [ ] `CancelCommand`
- [ ] `ConfirmCommand`
- [ ] `SearchCommand`
- [ ] `SearchCommand`

### HomeView

**ViewModel**: `HomeViewModel`

**缺失命令**:

- [ ] `DataContext.StartConsultationForPatientCommand`
- [ ] `DataContext.StartConsultationForPatientCommand`
- [ ] `DataContext.ViewPatientDetailsCommand`
- [ ] `EnterSystemManagementCommand`
- [ ] `LogoutCommand`
- [ ] `NavigateToDataBackupCommand`
- [ ] `NavigateToFormulaManagementCommand`
- [ ] `NavigateToFormulasCommand`
- [ ] `NavigateToHerbManagementCommand`
- [ ] `NavigateToHerbsCommand`
- [ ] `NavigateToMedicalCaseCommand`
- [ ] `NavigateToPatientReceptionCommand`
- [ ] `NavigateToPrescriptionQueryCommand`
- [ ] `NavigateToSystemSettingsCommand`
- [ ] `NavigateToUserManagementCommand`
- [ ] `RefreshTodayPatientsCommand`
- [ ] `StartConsultationCommand`

### InformationDialog

**ViewModel**: `InformationDialog`

**缺失命令**:

- [ ] `OkCommand`

### LoginWindow

**ViewModel**: `LoginWindow`

**缺失命令**:

- [ ] `LoginCommand`
- [ ] `LoginCommand`
- [ ] `LoginCommand`

### MainWindow

**ViewModel**: `MainWindow`

**缺失命令**:

- [ ] `LogoutCommand`
- [ ] `QuickAddPatientCommand`
- [ ] `QuickStartConsultationCommand`
- [ ] `ShowHelpCommand`
- [ ] `ShowSettingsCommand`
- [ ] `TestApiCommand`
- [ ] `ToggleThemeCommand`

### MedicalCaseDetailView

**ViewModel**: `MedicalCaseDetailViewModel`

**缺失命令**:

- [ ] `BackCommand`
- [ ] `CloseCommand`
- [ ] `EditCommand`
- [ ] `PrintCommand`
- [ ] `PrintPrescriptionCommand`
- [ ] `RefreshCommand`
- [ ] `StartConsultationCommand`

### MedicalCaseListView

**ViewModel**: `MedicalCaseListViewModel`

**缺失命令**:

- [ ] `AddCommand`
- [ ] `DataContext.DeleteCommand`
- [ ] `DataContext.EditCommand`
- [ ] `DataContext.StartConsultationCommand`
- [ ] `DataContext.ViewDetailCommand`
- [ ] `RefreshCommand`

### MedicalCaseManagementView

**ViewModel**: `MedicalCaseManagementViewModel`

**缺失命令**:

- [ ] `AddCommand`
- [ ] `DataContext.CreatePrescriptionCommand`
- [ ] `DataContext.DeleteCommand`
- [ ] `DataContext.EditCommand`
- [ ] `DataContext.PrintCommand`
- [ ] `DataContext.ViewConsultationCommand`
- [ ] `DataContext.ViewDetailsCommand`
- [ ] `FirstPageCommand`
- [ ] `LastPageCommand`
- [ ] `NextPageCommand`
- [ ] `PreviousPageCommand`
- [ ] `SearchCommand`
- [ ] `SearchCommand`

### PrescriptionComposerView

**ViewModel**: `PrescriptionComposerViewModel`

**缺失命令**:

- [ ] `ClearAllCommand`
- [ ] `CloseCommand`
- [ ] `DataContext.EditHerbCommand`
- [ ] `DataContext.RemoveHerbCommand`
- [ ] `SaveDraftCommand`
- [ ] `SavePrescriptionCommand`

### PrescriptionEditorDialog

**ViewModel**: `PrescriptionEditorDialog`

**缺失命令**:

- [ ] `AddHerbCommand`
- [ ] `CancelCommand`
- [ ] `DataContext.EditHerbCommand`
- [ ] `DataContext.RemoveHerbCommand`
- [ ] `LoadFormulaTemplateCommand`
- [ ] `PreviewCommand`
- [ ] `SaveCommand`

### PrescriptionManagementView

**ViewModel**: `PrescriptionManagementViewModel`

**缺失命令**:

- [ ] `AddPrescriptionCommand`
- [ ] `ClearFiltersCommand`
- [ ] `DataContext.CopyPrescriptionCommand`
- [ ] `DataContext.DeletePrescriptionCommand`
- [ ] `DataContext.EditPrescriptionCommand`
- [ ] `DataContext.PrintCommand`
- [ ] `DataContext.ViewPatientHistoryCommand`
- [ ] `DataContext.ViewPrescriptionCommand`
- [ ] `ExportPrescriptionsCommand`

### PrescriptionsMainView

**ViewModel**: `PrescriptionsMainViewModel`

**缺失命令**:

- [ ] `CreateNewPrescriptionCommand`
- [ ] `ReturnToSourceCommand`
- [ ] `SwitchToManagementCommand`
- [ ] `SwitchToManagementCommand`

### PrescriptionView

**ViewModel**: `PrescriptionViewModel`

**缺失命令**:

- [ ] `AddHerbCommand`
- [ ] `ClearCommand`
- [ ] `DataContext.RemoveHerbCommand`
- [ ] `DataContext.SetDosageCommand`
- [ ] `ImportFormulaCommand`
- [ ] `ImportHistoryCommand`
- [ ] `PrintPreviewCommand`
- [ ] `SaveCommand`
- [ ] `SetDiscountCommand`
- [ ] `SetDiscountCommand`

### ResetPasswordDialog

**ViewModel**: `ResetPasswordDialog`

**缺失命令**:

- [ ] `CancelCommand`
- [ ] `ConfirmCommand`
- [ ] `GeneratePasswordCommand`

### SelectFormulaDialog

**ViewModel**: `SelectFormulaDialog`

**缺失命令**:

- [ ] `CancelCommand`
- [ ] `ConfirmCommand`
- [ ] `ConfirmCommand`
- [ ] `DataContext.ViewDetailsCommand`
- [ ] `RefreshCommand`
- [ ] `SearchCommand`

### UserDetailView

**ViewModel**: `UserDetailViewModel`

**缺失命令**:

- [ ] `EditUserCommand`
- [ ] `GoBackCommand`
- [ ] `ResetPasswordCommand`

### UserManagementView

**ViewModel**: `UserManagementViewModel`

**缺失命令**:

- [ ] `AddCommand`
- [ ] `DataContext.DeleteCommand`
- [ ] `DataContext.EditCommand`
- [ ] `DataContext.ViewDetailsCommand`
- [ ] `FirstPageCommand`
- [ ] `LastPageCommand`
- [ ] `NextPageCommand`
- [ ] `PreviousPageCommand`
- [ ] `RefreshCommand`
- [ ] `SearchCommand`
- [ ] `SearchCommand`

### UserProfileDialog

**ViewModel**: `UserProfileDialog`

**缺失命令**:

- [ ] `CancelCommand`
- [ ] `RemoveAvatarCommand`
- [ ] `SaveCommand`
- [ ] `SelectAvatarCommand`

### ViewFormulaDialog

**ViewModel**: `ViewFormulaDialog`

**缺失命令**:

- [ ] `CloseCommand`
- [ ] `ExportCommand`
- [ ] `PrintCommand`


## 结论

❌ **发现 189 个缺失的命令绑定，需要立即修复。**

建议为每个有问题的模块创建独立的修复 Issue。

## 下一步行动

1. 为每个有问题的模块创建修复 Issue
2. 实现缺失的命令
3. 手动测试所有修复的绑定
4. 回归测试确保无副作用

---
*此报告由自动化脚本生成：`scripts/analysis/check-command-bindings.ps1`*

