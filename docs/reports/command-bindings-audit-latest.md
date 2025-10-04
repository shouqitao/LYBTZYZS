=== 开始检查 XAML Command 绑定 ===
Desktop 路径: D:\source\repos\LYBTZYZS\src\Client\Desktop
输出报告: D:\source\repos\LYBTZYZS\docs\reports\command-bindings-audit-2025-10-04.md

找到 36 个 View 文件

检查: LoginView
  ViewModel: LoginViewModel
    ? LoginCommand
    ? LoginCommand

检查: LoginWindow
  ??  未找到 ViewModel: D:\source\repos\LYBTZYZS\src\Client\Desktop\Modules\LYBT.Desktop.Auth\ViewModels\LoginWindow.cs
    ??  LoginCommand (ViewModel 不存在)
    ??  LoginCommand (ViewModel 不存在)
    ??  LoginCommand (ViewModel 不存在)

检查: ConsultationMainView
  ViewModel: ConsultationMainViewModel
    ? RefreshCommand
    ? ViewPatientHistoryCommand
    ? NewConsultationCommand
    ? ShowTemplateMenuCommand
    ? SaveConsultationCommand
    ? PrintPrescriptionCommand
    ? 缺失: DataContext.DecreaseQuantityCommand
    ? 缺失: DataContext.IncreaseQuantityCommand
    ? 缺失: DataContext.RemovePrescriptionItemCommand

检查: ConsultationManagementView
  ViewModel: ConsultationManagementViewModel
    ? SearchCommand
    ? SearchCommand
    ? StatisticsCommand
    ? RefreshCommand
    ? 缺失: DataContext.ViewDetailsCommand
    ? 缺失: DataContext.ViewPrescriptionCommand
    ? 缺失: DataContext.PrintCommand
    ? 缺失: DataContext.CopyRecordCommand
    ? FirstPageCommand
    ? PreviousPageCommand
    ? NextPageCommand
    ? LastPageCommand

检查: EditFormulaDialog
  ??  未找到 ViewModel: D:\source\repos\LYBTZYZS\src\Client\Desktop\Modules\LYBT.Desktop.Formula\ViewModels\EditFormulaDialog.cs
    ??  AddHerbCommand (ViewModel 不存在)
    ??  DataContext.EditHerbCommand (ViewModel 不存在)
    ??  DataContext.RemoveHerbCommand (ViewModel 不存在)
    ??  SaveCommand (ViewModel 不存在)
    ??  CancelCommand (ViewModel 不存在)

检查: FormulaDetailView
  ViewModel: FormulaDetailViewModel
    ? BackCommand
    ? EditCommand
    ? SaveCommand
    ? CancelEditCommand
    ? CopyFormulaCommand
    ? ViewUsageHistoryCommand
    ? PrintCommand

检查: FormulaManagementView
  ViewModel: FormulaManagementViewModel
    ? 缺失: SearchCommand
    ? ClearFiltersCommand
    ? ImportFormulasCommand
    ? ExportTemplateCommand
    ? ExportFormulasCommand
    ? 缺失: AddFormulaCommand
    ? 缺失: RefreshCommand
    ? 缺失: DataContext.ViewDetailsCommand
    ? 缺失: DataContext.EditCommand
    ? 缺失: DataContext.CopyCommand
    ? 缺失: DataContext.DeleteCommand
    ? FirstPageCommand
    ? 缺失: PreviousPageCommand
    ? 缺失: NextPageCommand
    ? LastPageCommand

检查: ViewFormulaDialog
  ??  未找到 ViewModel: D:\source\repos\LYBTZYZS\src\Client\Desktop\Modules\LYBT.Desktop.Formula\ViewModels\ViewFormulaDialog.cs
    ??  PrintCommand (ViewModel 不存在)
    ??  ExportCommand (ViewModel 不存在)
    ??  CloseCommand (ViewModel 不存在)

检查: HerbDetailView
  ViewModel: HerbDetailViewModel
    ? BackCommand
    ? EditCommand
    ? SaveCommand
    ? CancelEditCommand
    ? ViewUsageHistoryCommand
    ? PrintCommand

检查: HerbManagementView
  ViewModel: HerbManagementViewModel
    ? 缺失: SearchCommand
    ? 缺失: SearchCommand
    ? 缺失: ImportHerbsCommand
    ? 缺失: ExportTemplateCommand
    ? 缺失: ExportHerbsCommand
    ? 缺失: AddCommand
    ? 缺失: RefreshCommand
    ? 缺失: DataContext.EditCommand
    ? 缺失: DataContext.ToggleStatusCommand
    ? 缺失: DataContext.DeleteCommand
    ? 缺失: FirstPageCommand
    ? 缺失: PreviousPageCommand
    ? 缺失: NextPageCommand
    ? 缺失: LastPageCommand

检查: CreateMedicalCaseDialog
  ??  未找到 ViewModel: D:\source\repos\LYBTZYZS\src\Client\Desktop\Modules\LYBT.Desktop.MedicalCase\ViewModels\CreateMedicalCaseDialog.cs
    ??  SaveCommand (ViewModel 不存在)
    ??  CancelCommand (ViewModel 不存在)

检查: MedicalCaseDetailView
  ViewModel: MedicalCaseDetailViewModel
    ? BackCommand
    ? StartConsultationCommand
    ? PrintCommand
    ? RefreshCommand
    ? EditCommand
    ? PrintPrescriptionCommand
    ? CloseCommand

检查: MedicalCaseListView
  ViewModel: MedicalCaseListViewModel
    ? SearchCommand
    ? AddCommand
    ? RefreshCommand
    ? 缺失: DataContext.ViewDetailCommand
    ? 缺失: DataContext.StartConsultationCommand
    ? 缺失: DataContext.EditCommand
    ? 缺失: DataContext.DeleteCommand
    ? PreviousPageCommand
    ? NextPageCommand

检查: MedicalCaseManagementView
  ViewModel: MedicalCaseManagementViewModel
    ? SearchCommand
    ? SearchCommand
    ? AddCommand
    ? RefreshCommand
    ? 缺失: DataContext.ViewDetailsCommand
    ? 缺失: DataContext.EditCommand
    ? 缺失: DataContext.ViewConsultationCommand
    ? 缺失: DataContext.CreatePrescriptionCommand
    ? 缺失: DataContext.PrintCommand
    ? 缺失: DataContext.DeleteCommand
    ? FirstPageCommand
    ? PreviousPageCommand
    ? NextPageCommand
    ? LastPageCommand

检查: PatientDetailView
  ViewModel: PatientDetailViewModel
    ? BackCommand
    ? EditCommand
    ? SaveCommand
    ? CancelEditCommand
    ? ViewMedicalHistoryCommand
    ? PrintCommand
    ? ViewMedicalHistoryCommand

检查: PatientImportWizardView
  ViewModel: PatientImportWizardViewModel
    ? CancelCommand
    ? PreviousCommand
    ? NextCommand

检查: FormulaTemplateDialog
  ??  未找到 ViewModel: D:\source\repos\LYBTZYZS\src\Client\Desktop\Modules\LYBT.Desktop.Prescriptions\ViewModels\FormulaTemplateDialog.cs
    ??  RefreshCommand (ViewModel 不存在)
    ??  DataContext.ViewDetailsCommand (ViewModel 不存在)
    ??  SelectCommand (ViewModel 不存在)
    ??  CancelCommand (ViewModel 不存在)

检查: HerbSelectionDialog
  ??  未找到 ViewModel: D:\source\repos\LYBTZYZS\src\Client\Desktop\Modules\LYBT.Desktop.Prescriptions\ViewModels\HerbSelectionDialog.cs
    ??  SearchCommand (ViewModel 不存在)
    ??  SearchCommand (ViewModel 不存在)
    ??  ConfirmCommand (ViewModel 不存在)
    ??  CancelCommand (ViewModel 不存在)

检查: PrescriptionComposerView
  ViewModel: PrescriptionComposerViewModel
    ? AddHerbCommand
    ? ImportFormulaCommand
    ? ClearAllCommand
    ? 缺失: DataContext.EditHerbCommand
    ? 缺失: DataContext.RemoveHerbCommand
    ? SaveDraftCommand
    ? SavePrescriptionCommand
    ? CloseCommand

检查: PrescriptionEditorDialog
  ??  未找到 ViewModel: D:\source\repos\LYBTZYZS\src\Client\Desktop\Modules\LYBT.Desktop.Prescriptions\ViewModels\PrescriptionEditorDialog.cs
    ??  AddHerbCommand (ViewModel 不存在)
    ??  LoadFormulaTemplateCommand (ViewModel 不存在)
    ??  DataContext.EditHerbCommand (ViewModel 不存在)
    ??  DataContext.RemoveHerbCommand (ViewModel 不存在)
    ??  PreviewCommand (ViewModel 不存在)
    ??  SaveCommand (ViewModel 不存在)
    ??  CancelCommand (ViewModel 不存在)

检查: PrescriptionManagementView
  ViewModel: PrescriptionManagementViewModel
    ? AddPrescriptionCommand
    ? ExportPrescriptionsCommand
    ? RefreshCommand
    ? ClearFiltersCommand
    ? 缺失: DataContext.ViewPrescriptionCommand
    ? 缺失: DataContext.EditPrescriptionCommand
    ? 缺失: DataContext.ViewPatientHistoryCommand
    ? 缺失: DataContext.CopyPrescriptionCommand
    ? 缺失: DataContext.PrintCommand
    ? 缺失: DataContext.DeletePrescriptionCommand

检查: PrescriptionsMainView
  ViewModel: PrescriptionsMainViewModel
    ? SwitchToManagementCommand
    ? ReturnToSourceCommand
    ? SwitchToManagementCommand
    ? CreateNewPrescriptionCommand

检查: PrescriptionView
  ??  未找到 ViewModel: D:\source\repos\LYBTZYZS\src\Client\Desktop\Modules\LYBT.Desktop.Prescriptions\ViewModels\PrescriptionViewModel.cs
    ??  AddHerbCommand (ViewModel 不存在)
    ??  ImportFormulaCommand (ViewModel 不存在)
    ??  DataContext.RemoveHerbCommand (ViewModel 不存在)
    ??  DataContext.SetDosageCommand (ViewModel 不存在)
    ??  SetDiscountCommand (ViewModel 不存在)
    ??  SetDiscountCommand (ViewModel 不存在)
    ??  ImportHistoryCommand (ViewModel 不存在)
    ??  PrintPreviewCommand (ViewModel 不存在)
    ??  ClearCommand (ViewModel 不存在)
    ??  SaveCommand (ViewModel 不存在)

检查: SelectFormulaDialog
  ??  未找到 ViewModel: D:\source\repos\LYBTZYZS\src\Client\Desktop\Modules\LYBT.Desktop.Prescriptions\ViewModels\SelectFormulaDialog.cs
    ??  SearchCommand (ViewModel 不存在)
    ??  RefreshCommand (ViewModel 不存在)
    ??  ConfirmCommand (ViewModel 不存在)
    ??  DataContext.ViewDetailsCommand (ViewModel 不存在)
    ??  ConfirmCommand (ViewModel 不存在)
    ??  CancelCommand (ViewModel 不存在)

检查: ChangePasswordDialog
  ??  未找到 ViewModel: D:\source\repos\LYBTZYZS\src\Client\Desktop\Modules\LYBT.Desktop.Users\ViewModels\ChangePasswordDialog.cs
    ??  ConfirmCommand (ViewModel 不存在)
    ??  CancelCommand (ViewModel 不存在)

检查: ResetPasswordDialog
  ??  未找到 ViewModel: D:\source\repos\LYBTZYZS\src\Client\Desktop\Modules\LYBT.Desktop.Users\ViewModels\ResetPasswordDialog.cs
    ??  GeneratePasswordCommand (ViewModel 不存在)
    ??  ConfirmCommand (ViewModel 不存在)
    ??  CancelCommand (ViewModel 不存在)

检查: UserDetailView
  ??  未找到 ViewModel: D:\source\repos\LYBTZYZS\src\Client\Desktop\Modules\LYBT.Desktop.Users\ViewModels\UserDetailViewModel.cs
    ??  GoBackCommand (ViewModel 不存在)
    ??  EditUserCommand (ViewModel 不存在)
    ??  ResetPasswordCommand (ViewModel 不存在)

检查: UserManagementView
  ViewModel: UserManagementViewModel
    ? 缺失: SearchCommand
    ? 缺失: SearchCommand
    ? 缺失: AddCommand
    ? 缺失: RefreshCommand
    ? 缺失: DataContext.ViewDetailsCommand
    ? 缺失: DataContext.EditCommand
    ? 缺失: DataContext.DeleteCommand
    ? FirstPageCommand
    ? 缺失: PreviousPageCommand
    ? 缺失: NextPageCommand
    ? LastPageCommand

检查: UserProfileDialog
  ??  未找到 ViewModel: D:\source\repos\LYBTZYZS\src\Client\Desktop\Modules\LYBT.Desktop.Users\ViewModels\UserProfileDialog.cs
    ??  SelectAvatarCommand (ViewModel 不存在)
    ??  RemoveAvatarCommand (ViewModel 不存在)
    ??  SaveCommand (ViewModel 不存在)
    ??  CancelCommand (ViewModel 不存在)

检查: ConfirmationDialog
  ??  未找到 ViewModel: D:\source\repos\LYBTZYZS\src\Client\Desktop\Shell\Dialogs\ViewModels\ConfirmationDialog.cs
    ??  YesCommand (ViewModel 不存在)
    ??  NoCommand (ViewModel 不存在)

检查: ErrorDetailsDialog
  ??  未找到 ViewModel: D:\source\repos\LYBTZYZS\src\Client\Desktop\Shell\Dialogs\ViewModels\ErrorDetailsDialog.cs
    ??  CopyErrorCommand (ViewModel 不存在)
    ??  RetryCommand (ViewModel 不存在)
    ??  CloseCommand (ViewModel 不存在)

检查: InformationDialog
  ??  未找到 ViewModel: D:\source\repos\LYBTZYZS\src\Client\Desktop\Shell\Dialogs\ViewModels\InformationDialog.cs
    ??  OkCommand (ViewModel 不存在)

检查: HomeView
  ViewModel: HomeViewModel
    ? LogoutCommand
    ? StartConsultationCommand
    ? RefreshTodayPatientsCommand
    ? 缺失: DataContext.StartConsultationForPatientCommand
    ? 缺失: DataContext.StartConsultationForPatientCommand
    ? 缺失: DataContext.ViewPatientDetailsCommand
    ? NavigateToPatientReceptionCommand
    ? NavigateToMedicalCaseCommand
    ? NavigateToPrescriptionQueryCommand
    ? NavigateToPatientManagementCommand
    ? NavigateToHerbsCommand
    ? NavigateToFormulasCommand
    ? EnterSystemManagementCommand
    ? NavigateToUserManagementCommand
    ? NavigateToHerbManagementCommand
    ? NavigateToFormulaManagementCommand
    ? NavigateToPatientManagementCommand
    ? NavigateToSystemSettingsCommand
    ? NavigateToDataBackupCommand

检查: MainWindow
  ??  未找到 ViewModel: D:\source\repos\LYBTZYZS\src\Client\Desktop\Shell\ViewModels\MainWindow.cs
    ??  QuickAddPatientCommand (ViewModel 不存在)
    ??  QuickStartConsultationCommand (ViewModel 不存在)
    ??  ShowHelpCommand (ViewModel 不存在)
    ??  ShowSettingsCommand (ViewModel 不存在)
    ??  ToggleThemeCommand (ViewModel 不存在)
    ??  TestApiCommand (ViewModel 不存在)
    ??  LogoutCommand (ViewModel 不存在)

检查: AdminWorkstationView
  ViewModel: AdminWorkstationViewModel
    ? LogoutCommand
    ? NavigateCommand
    ? NavigateCommand
    ? NavigateCommand
    ? NavigateCommand
    ? NavigateCommand
    ? NavigateCommand

检查: ClinicalWorkstationView
  ViewModel: ClinicalWorkstationViewModel
    ? SelectPatientCommand
    ? LogoutCommand
    ? 缺失: DataContext.ImportDiagnosisCommand
    ? SearchHerbCommand
    ? ImportFormulaCommand
    ? ShowHistoryCommand
    ? ClearPrescriptionCommand
    ? SavePrescriptionCommand
    ? PrintPrescriptionCommand

=== 检查完成 ===

总结:
  总 View 数: 36
  总绑定数: 242
  ? 正常绑定: 112
  ? 缺失绑定: 130
  ?? 有问题的 View: 28

报告已保存: D:\source\repos\LYBTZYZS\docs\reports\command-bindings-audit-2025-10-04.md
