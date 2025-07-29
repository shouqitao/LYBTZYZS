# LYBT中医诊疗系统WPF完整项目结构

## 解决方案结构

```
LYBT.WPF.Client/
│
├── LYBT.WPF.Client.Shell/                          # 主壳程序
│   ├── Views/
│   │   ├── MainWindow.xaml
│   │   ├── MainWindow.xaml.cs
│   │   ├── ShellView.xaml
│   │   └── ShellView.xaml.cs
│   ├── ViewModels/
│   │   ├── MainWindowViewModel.cs
│   │   └── ShellViewModel.cs
│   ├── Services/
│   │   ├── WindowManager.cs
│   │   └── ThemeManager.cs
│   ├── App.xaml
│   ├── App.xaml.cs
│   ├── Bootstrapper.cs
│   └── LYBT.WPF.Client.Shell.csproj
│
├── LYBT.WPF.Client.Core/                           # 核心基础设施
│   ├── Models/
│   │   ├── Common/
│   │   │   ├── ApiResponse.cs
│   │   │   ├── PaginatedResult.cs
│   │   │   ├── BaseModel.cs
│   │   │   └── AuditableModel.cs
│   │   ├── Authentication/
│   │   │   ├── LoginRequest.cs
│   │   │   ├── LoginResponse.cs
│   │   │   ├── UserInfo.cs
│   │   │   └── ChangePasswordRequest.cs
│   │   ├── Enums/
│   │   │   ├── UserRole.cs
│   │   │   ├── PatientStatus.cs
│   │   │   ├── ConsultationStatus.cs
│   │   │   ├── BillingStatus.cs
│   │   │   ├── PharmacyStatus.cs
│   │   │   └── PhysiotherapyStatus.cs
│   │   └── Configuration/
│   │       ├── SystemSettings.cs
│   │       └── UserPreferences.cs
│   ├── Services/
│   │   ├── IApiService.cs
│   │   ├── IAuthenticationService.cs
│   │   ├── IDialogService.cs
│   │   ├── INotificationService.cs
│   │   ├── ICacheService.cs
│   │   ├── ILoggingService.cs
│   │   ├── IPrintService.cs
│   │   └── IExportService.cs
│   ├── Constants/
│   │   ├── ApiEndpoints.cs
│   │   ├── UserRoles.cs
│   │   ├── SystemMessages.cs
│   │   ├── PrintTemplates.cs
│   │   └── ValidationRules.cs
│   ├── Extensions/
│   │   ├── ObservableCollectionExtensions.cs
│   │   ├── StringExtensions.cs
│   │   ├── DateTimeExtensions.cs
│   │   └── ValidationExtensions.cs
│   ├── Attributes/
│   │   ├── RoleAuthorizeAttribute.cs
│   │   └── AuditLogAttribute.cs
│   └── LYBT.WPF.Client.Core.csproj
│
├── LYBT.WPF.Client.Services/                       # 服务层
│   ├── Authentication/
│   │   ├── IAuthenticationService.cs
│   │   ├── AuthenticationService.cs
│   │   ├── IPasswordService.cs
│   │   └── PasswordService.cs
│   ├── SystemManagement/                           # 系统管理服务
│   │   ├── Users/
│   │   │   ├── IUsersService.cs
│   │   │   └── UsersService.cs
│   │   ├── Patients/
│   │   │   ├── IPatientsService.cs
│   │   │   └── PatientsService.cs
│   │   ├── Herbs/
│   │   │   ├── IHerbsService.cs
│   │   │   └── HerbsService.cs
│   │   ├── PrescriptionTemplates/
│   │   │   ├── IPrescriptionTemplatesService.cs
│   │   │   └── PrescriptionTemplatesService.cs
│   │   ├── Pharmacy/
│   │   │   ├── IPharmacyInventoryService.cs
│   │   │   └── PharmacyInventoryService.cs
│   │   ├── Config/
│   │   │   ├── ISystemConfigService.cs
│   │   │   └── SystemConfigService.cs
│   │   ├── Logs/
│   │   │   ├── IAuditLogService.cs
│   │   │   └── AuditLogService.cs
│   │   ├── DataManagement/
│   │   │   ├── IImportExportService.cs
│   │   │   ├── ImportExportService.cs
│   │   │   ├── IBackupService.cs
│   │   │   └── BackupService.cs
│   │   └── Reports/
│   │       ├── ISystemReportService.cs
│   │       └── SystemReportService.cs
│   ├── Workflow/                                   # 业务流程服务
│   │   ├── Registration/
│   │   │   ├── IRegistrationService.cs
│   │   │   ├── RegistrationService.cs
│   │   │   ├── IQueueService.cs
│   │   │   └── QueueService.cs
│   │   ├── Consultation/
│   │   │   ├── IConsultationService.cs
│   │   │   ├── ConsultationService.cs
│   │   │   ├── IDiagnosisService.cs
│   │   │   ├── DiagnosisService.cs
│   │   │   ├── IPrescriptionService.cs
│   │   │   ├── PrescriptionService.cs
│   │   │   ├── IPhysiotherapyService.cs
│   │   │   └── PhysiotherapyService.cs
│   │   ├── MedicalRecords/
│   │   │   ├── IMedicalRecordService.cs
│   │   │   └── MedicalRecordService.cs
│   │   ├── Billing/
│   │   │   ├── IBillingService.cs
│   │   │   └── BillingService.cs
│   │   ├── Pharmacy/
│   │   │   ├── IPharmacyDispenseService.cs
│   │   │   └── PharmacyDispenseService.cs
│   │   └── Physiotherapy/
│   │       ├── IPhysiotherapyTreatmentService.cs
│   │       └── PhysiotherapyTreatmentService.cs
│   ├── Infrastructure/
│   │   ├── IHttpService.cs
│   │   ├── HttpService.cs
│   │   ├── ICacheService.cs
│   │   ├── CacheService.cs
│   │   ├── ILoggingService.cs
│   │   ├── LoggingService.cs
│   │   ├── IPrintService.cs
│   │   ├── PrintService.cs
│   │   ├── IExportService.cs
│   │   ├── ExportService.cs
│   │   ├── INotificationService.cs
│   │   └── NotificationService.cs
│   └── LYBT.WPF.Client.Services.csproj
│
├── LYBT.WPF.Client.Modules/                        # 业务模块
│   │
│   ├── Authentication/                             # 认证模块
│   │   ├── Views/
│   │   │   ├── LoginView.xaml
│   │   │   ├── LoginView.xaml.cs
│   │   │   ├── ChangePasswordView.xaml
│   │   │   ├── ChangePasswordView.xaml.cs
│   │   │   ├── FirstTimeSetupView.xaml             # 首次运行设置
│   │   │   └── FirstTimeSetupView.xaml.cs
│   │   ├── ViewModels/
│   │   │   ├── LoginViewModel.cs
│   │   │   ├── ChangePasswordViewModel.cs
│   │   │   └── FirstTimeSetupViewModel.cs
│   │   ├── Models/
│   │   │   ├── LoginModel.cs
│   │   │   ├── ChangePasswordModel.cs
│   │   │   └── FirstTimeSetupModel.cs
│   │   ├── Services/
│   │   │   ├── ISecurityService.cs
│   │   │   └── SecurityService.cs
│   │   ├── AuthenticationModule.cs
│   │   └── LYBT.WPF.Client.Modules.Authentication.csproj
│   │
│   ├── SystemManagement/                           # 系统管理模块 (超级管理员/管理员)
│   │   │
│   │   ├── Users/                                  # 用户管理
│   │   │   ├── Views/
│   │   │   │   ├── UsersListView.xaml
│   │   │   │   ├── UsersListView.xaml.cs
│   │   │   │   ├── UserDetailView.xaml
│   │   │   │   ├── UserDetailView.xaml.cs
│   │   │   │   ├── UserEditView.xaml
│   │   │   │   ├── UserEditView.xaml.cs
│   │   │   │   ├── CreateUserView.xaml
│   │   │   │   ├── CreateUserView.xaml.cs
│   │   │   │   ├── UserRoleAssignView.xaml         # 角色分配
│   │   │   │   ├── UserRoleAssignView.xaml.cs
│   │   │   │   ├── UserStatusManageView.xaml       # 用户状态管理
│   │   │   │   ├── UserStatusManageView.xaml.cs
│   │   │   │   ├── PasswordPolicyView.xaml         # 密码策略设置
│   │   │   │   └── PasswordPolicyView.xaml.cs
│   │   │   ├── ViewModels/
│   │   │   │   ├── UsersListViewModel.cs
│   │   │   │   ├── UserDetailViewModel.cs
│   │   │   │   ├── UserEditViewModel.cs
│   │   │   │   ├── CreateUserViewModel.cs
│   │   │   │   ├── UserRoleAssignViewModel.cs
│   │   │   │   ├── UserStatusManageViewModel.cs
│   │   │   │   └── PasswordPolicyViewModel.cs
│   │   │   ├── Models/
│   │   │   │   ├── UserModel.cs
│   │   │   │   ├── UserRoleModel.cs
│   │   │   │   ├── UserStatusModel.cs
│   │   │   │   └── PasswordPolicyModel.cs
│   │   │   └── UsersModule.cs
│   │   │
│   │   ├── Patients/                               # 患者管理
│   │   │   ├── Views/
│   │   │   │   ├── PatientsListView.xaml
│   │   │   │   ├── PatientsListView.xaml.cs
│   │   │   │   ├── PatientDetailView.xaml
│   │   │   │   ├── PatientDetailView.xaml.cs
│   │   │   │   ├── PatientEditView.xaml
│   │   │   │   ├── PatientEditView.xaml.cs
│   │   │   │   ├── PatientSearchView.xaml
│   │   │   │   ├── PatientSearchView.xaml.cs
│   │   │   │   ├── PatientImportView.xaml          # 患者批量导入
│   │   │   │   ├── PatientImportView.xaml.cs
│   │   │   │   ├── PatientStatisticsView.xaml      # 患者统计
│   │   │   │   └── PatientStatisticsView.xaml.cs
│   │   │   ├── ViewModels/
│   │   │   │   ├── PatientsListViewModel.cs
│   │   │   │   ├── PatientDetailViewModel.cs
│   │   │   │   ├── PatientEditViewModel.cs
│   │   │   │   ├── PatientSearchViewModel.cs
│   │   │   │   ├── PatientImportViewModel.cs
│   │   │   │   └── PatientStatisticsViewModel.cs
│   │   │   ├── Models/
│   │   │   │   ├── PatientModel.cs
│   │   │   │   ├── PatientSearchCriteria.cs
│   │   │   │   └── PatientStatisticsModel.cs
│   │   │   └── PatientsModule.cs
│   │   │
│   │   ├── Herbs/                                  # 药材管理
│   │   │   ├── Views/
│   │   │   │   ├── HerbsListView.xaml
│   │   │   │   ├── HerbsListView.xaml.cs
│   │   │   │   ├── HerbDetailView.xaml
│   │   │   │   ├── HerbDetailView.xaml.cs
│   │   │   │   ├── HerbEditView.xaml
│   │   │   │   ├── HerbEditView.xaml.cs
│   │   │   │   ├── HerbSearchView.xaml
│   │   │   │   ├── HerbSearchView.xaml.cs
│   │   │   │   ├── HerbCategoryView.xaml
│   │   │   │   ├── HerbCategoryView.xaml.cs
│   │   │   │   ├── HerbImportView.xaml             # 药材批量导入
│   │   │   │   ├── HerbImportView.xaml.cs
│   │   │   │   ├── HerbPriceManageView.xaml        # 药材价格管理
│   │   │   │   └── HerbPriceManageView.xaml.cs
│   │   │   ├── ViewModels/
│   │   │   │   ├── HerbsListViewModel.cs
│   │   │   │   ├── HerbDetailViewModel.cs
│   │   │   │   ├── HerbEditViewModel.cs
│   │   │   │   ├── HerbSearchViewModel.cs
│   │   │   │   ├── HerbCategoryViewModel.cs
│   │   │   │   ├── HerbImportViewModel.cs
│   │   │   │   └── HerbPriceManageViewModel.cs
│   │   │   ├── Models/
│   │   │   │   ├── HerbModel.cs
│   │   │   │   ├── HerbCategoryModel.cs
│   │   │   │   ├── HerbSearchCriteria.cs
│   │   │   │   └── HerbPriceModel.cs
│   │   │   └── HerbsModule.cs
│   │   │
│   │   ├── PrescriptionTemplates/                  # 经方管理
│   │   │   ├── Views/
│   │   │   │   ├── TemplatesListView.xaml
│   │   │   │   ├── TemplatesListView.xaml.cs
│   │   │   │   ├── TemplateDetailView.xaml
│   │   │   │   ├── TemplateDetailView.xaml.cs
│   │   │   │   ├── TemplateEditView.xaml           # 经方编辑(一味一味药材组成)
│   │   │   │   ├── TemplateEditView.xaml.cs
│   │   │   │   ├── TemplateSearchView.xaml
│   │   │   │   ├── TemplateSearchView.xaml.cs
│   │   │   │   ├── HerbSelectionView.xaml          # 药材选择器
│   │   │   │   ├── HerbSelectionView.xaml.cs
│   │   │   │   ├── TemplateCategoryView.xaml       # 经方分类
│   │   │   │   ├── TemplateCategoryView.xaml.cs
│   │   │   │   ├── TemplateImportView.xaml         # 经方导入
│   │   │   │   └── TemplateImportView.xaml.cs
│   │   │   ├── ViewModels/
│   │   │   │   ├── TemplatesListViewModel.cs
│   │   │   │   ├── TemplateDetailViewModel.cs
│   │   │   │   ├── TemplateEditViewModel.cs
│   │   │   │   ├── TemplateSearchViewModel.cs
│   │   │   │   ├── HerbSelectionViewModel.cs
│   │   │   │   ├── TemplateCategoryViewModel.cs
│   │   │   │   └── TemplateImportViewModel.cs
│   │   │   ├── Models/
│   │   │   │   ├── PrescriptionTemplateModel.cs
│   │   │   │   ├── TemplateHerbItemModel.cs
│   │   │   │   ├── TemplateSearchCriteria.cs
│   │   │   │   └── TemplateCategoryModel.cs
│   │   │   └── PrescriptionTemplatesModule.cs
│   │   │
│   │   ├── PharmacyInventory/                      # 药房管理
│   │   │   ├── Views/
│   │   │   │   ├── InventoryListView.xaml
│   │   │   │   ├── InventoryListView.xaml.cs
│   │   │   │   ├── StockInView.xaml                # 入库管理
│   │   │   │   ├── StockInView.xaml.cs
│   │   │   │   ├── StockOutView.xaml               # 出库管理
│   │   │   │   ├── StockOutView.xaml.cs
│   │   │   │   ├── StockTransferView.xaml          # 库存调拨
│   │   │   │   ├── StockTransferView.xaml.cs
│   │   │   │   ├── StockTakeView.xaml              # 盘点管理
│   │   │   │   ├── StockTakeView.xaml.cs
│   │   │   │   ├── StockAlertView.xaml             # 库存预警
│   │   │   │   ├── StockAlertView.xaml.cs
│   │   │   │   ├── SupplierManageView.xaml         # 供应商管理
│   │   │   │   └── SupplierManageView.xaml.cs
│   │   │   ├── ViewModels/
│   │   │   │   ├── InventoryListViewModel.cs
│   │   │   │   ├── StockInViewModel.cs
│   │   │   │   ├── StockOutViewModel.cs
│   │   │   │   ├── StockTransferViewModel.cs
│   │   │   │   ├── StockTakeViewModel.cs
│   │   │   │   ├── StockAlertViewModel.cs
│   │   │   │   └── SupplierManageViewModel.cs
│   │   │   ├── Models/
│   │   │   │   ├── InventoryModel.cs
│   │   │   │   ├── StockInModel.cs
│   │   │   │   ├── StockOutModel.cs
│   │   │   │   ├── StockTransferModel.cs
│   │   │   │   ├── StockTakeModel.cs
│   │   │   │   ├── StockAlertModel.cs
│   │   │   │   └── SupplierModel.cs
│   │   │   └── PharmacyInventoryModule.cs
│   │   │
│   │   ├── SystemConfig/                           # 系统配置
│   │   │   ├── Views/
│   │   │   │   ├── SystemParametersView.xaml       # 系统参数
│   │   │   │   ├── SystemParametersView.xaml.cs
│   │   │   │   ├── RegistrationFeeView.xaml        # 挂号费设置
│   │   │   │   ├── RegistrationFeeView.xaml.cs
│   │   │   │   ├── PrintTemplateView.xaml          # 打印模板
│   │   │   │   ├── PrintTemplateView.xaml.cs
│   │   │   │   ├── ThemeSettingsView.xaml          # 主题设置
│   │   │   │   ├── ThemeSettingsView.xaml.cs
│   │   │   │   ├── SecuritySettingsView.xaml       # 安全设置
│   │   │   │   └── SecuritySettingsView.xaml.cs
│   │   │   ├── ViewModels/
│   │   │   │   ├── SystemParametersViewModel.cs
│   │   │   │   ├── RegistrationFeeViewModel.cs
│   │   │   │   ├── PrintTemplateViewModel.cs
│   │   │   │   ├── ThemeSettingsViewModel.cs
│   │   │   │   └── SecuritySettingsViewModel.cs
│   │   │   ├── Models/
│   │   │   │   ├── SystemParameterModel.cs
│   │   │   │   ├── RegistrationFeeModel.cs
│   │   │   │   ├── PrintTemplateModel.cs
│   │   │   │   ├── ThemeSettingsModel.cs
│   │   │   │   └── SecuritySettingsModel.cs
│   │   │   └── SystemConfigModule.cs
│   │   │
│   │   ├── AuditLogs/                              # 操作日志
│   │   │   ├── Views/
│   │   │   │   ├── AuditLogsListView.xaml
│   │   │   │   ├── AuditLogsListView.xaml.cs
│   │   │   │   ├── LogDetailView.xaml
│   │   │   │   ├── LogDetailView.xaml.cs
│   │   │   │   ├── LogSearchView.xaml
│   │   │   │   ├── LogSearchView.xaml.cs
│   │   │   │   ├── LogStatisticsView.xaml          # 日志统计
│   │   │   │   └── LogStatisticsView.xaml.cs
│   │   │   ├── ViewModels/
│   │   │   │   ├── AuditLogsListViewModel.cs
│   │   │   │   ├── LogDetailViewModel.cs
│   │   │   │   ├── LogSearchViewModel.cs
│   │   │   │   └── LogStatisticsViewModel.cs
│   │   │   ├── Models/
│   │   │   │   ├── AuditLogModel.cs
│   │   │   │   ├── LogSearchCriteria.cs
│   │   │   │   └── LogStatisticsModel.cs
│   │   │   └── AuditLogsModule.cs
│   │   │
│   │   ├── DataManagement/                         # 数据管理
│   │   │   ├── Views/
│   │   │   │   ├── ImportDataView.xaml             # 数据导入
│   │   │   │   ├── ImportDataView.xaml.cs
│   │   │   │   ├── ExportDataView.xaml             # 数据导出
│   │   │   │   ├── ExportDataView.xaml.cs
│   │   │   │   ├── BackupView.xaml                 # 数据备份
│   │   │   │   ├── BackupView.xaml.cs
│   │   │   │   ├── RestoreView.xaml                # 数据恢复
│   │   │   │   ├── RestoreView.xaml.cs
│   │   │   │   ├── DataIntegrityView.xaml          # 数据完整性检查
│   │   │   │   └── DataIntegrityView.xaml.cs
│   │   │   ├── ViewModels/
│   │   │   │   ├── ImportDataViewModel.cs
│   │   │   │   ├── ExportDataViewModel.cs
│   │   │   │   ├── BackupViewModel.cs
│   │   │   │   ├── RestoreViewModel.cs
│   │   │   │   └── DataIntegrityViewModel.cs
│   │   │   ├── Models/
│   │   │   │   ├── ImportDataModel.cs
│   │   │   │   ├── ExportDataModel.cs
│   │   │   │   ├── BackupModel.cs
│   │   │   │   ├── RestoreModel.cs
│   │   │   │   └── DataIntegrityModel.cs
│   │   │   └── DataManagementModule.cs
│   │   │
│   │   ├── SystemReports/                          # 系统报表
│   │   │   ├── Views/
│   │   │   │   ├── ReportDashboardView.xaml        # 报表仪表板
│   │   │   │   ├── ReportDashboardView.xaml.cs
│   │   │   │   ├── UserActivityReportView.xaml     # 用户活动报表
│   │   │   │   ├── UserActivityReportView.xaml.cs
│   │   │   │   ├── SystemUsageReportView.xaml      # 系统使用报表
│   │   │   │   ├── SystemUsageReportView.xaml.cs
│   │   │   │   ├── DataStatisticsView.xaml         # 数据统计
│   │   │   │   └── DataStatisticsView.xaml.cs
│   │   │   ├── ViewModels/
│   │   │   │   ├── ReportDashboardViewModel.cs
│   │   │   │   ├── UserActivityReportViewModel.cs
│   │   │   │   ├── SystemUsageReportViewModel.cs
│   │   │   │   └── DataStatisticsViewModel.cs
│   │   │   ├── Models/
│   │   │   │   ├── ReportDashboardModel.cs
│   │   │   │   ├── UserActivityReportModel.cs
│   │   │   │   ├── SystemUsageReportModel.cs
│   │   │   │   └── DataStatisticsModel.cs
│   │   │   └── SystemReportsModule.cs
│   │   │
│   │   ├── SystemManagementModule.cs
│   │   └── LYBT.WPF.Client.Modules.SystemManagement.csproj
│   │
│   ├── FrontDesk/                                  # 前台模块 (前台用户)
│   │   ├── Views/
│   │   │   ├── FrontDeskWorkbenchView.xaml         # 前台工作台
│   │   │   ├── FrontDeskWorkbenchView.xaml.cs
│   │   │   ├── RegistrationView.xaml               # 挂号登记
│   │   │   ├── RegistrationView.xaml.cs
│   │   │   ├── PatientSearchView.xaml              # 患者查找
│   │   │   ├── PatientSearchView.xaml.cs
│   │   │   ├── PatientCreateView.xaml              # 新建患者
│   │   │   ├── PatientCreateView.xaml.cs
│   │   │   ├── RegistrationListView.xaml           # 挂号列表
│   │   │   ├── RegistrationListView.xaml.cs
│   │   │   ├── QueueManagementView.xaml            # 排队管理
│   │   │   ├── QueueManagementView.xaml.cs
│   │   │   ├── QueueDisplayView.xaml               # 排队显示屏
│   │   │   ├── QueueDisplayView.xaml.cs
│   │   │   ├── RegistrationCancelView.xaml         # 挂号取消
│   │   │   ├── RegistrationCancelView.xaml.cs
│   │   │   ├── FrontDeskStatisticsView.xaml        # 前台统计
│   │   │   └── FrontDeskStatisticsView.xaml.cs
│   │   ├── ViewModels/
│   │   │   ├── FrontDeskWorkbenchViewModel.cs
│   │   │   ├── RegistrationViewModel.cs
│   │   │   ├── PatientSearchViewModel.cs
│   │   │   ├── PatientCreateViewModel.cs
│   │   │   ├── RegistrationListViewModel.cs
│   │   │   ├── QueueManagementViewModel.cs
│   │   │   ├── QueueDisplayViewModel.cs
│   │   │   ├── RegistrationCancelViewModel.cs
│   │   │   └── FrontDeskStatisticsViewModel.cs
│   │   ├── Models/
│   │   │   ├── RegistrationModel.cs
│   │   │   ├── QueueModel.cs
│   │   │   ├── QueueStatus.cs
│   │   │   ├── RegistrationStatisticsModel.cs
│   │   │   └── FrontDeskWorkbenchModel.cs
│   │   ├── Services/
│   │   │   ├── IQueueNotificationService.cs
│   │   │   └── QueueNotificationService.cs
│   │   ├── FrontDeskModule.cs
│   │   └── LYBT.WPF.Client.Modules.FrontDesk.csproj
│   │
│   ├── Doctor/                                     # 医生模块 (医生用户)
│   │   ├── Views/
│   │   │   ├── DoctorWorkbenchView.xaml            # 医生工作台
│   │   │   ├── DoctorWorkbenchView.xaml.cs
│   │   │   ├── WaitingPatientsView.xaml            # 待看诊列表
│   │   │   ├── WaitingPatientsView.xaml.cs
│   │   │   ├── PatientCallView.xaml                # 患者叫号
│   │   │   ├── PatientCallView.xaml.cs
│   │   │   ├── ConsultationView.xaml               # 看诊界面
│   │   │   ├── ConsultationView.xaml.cs
│   │   │   ├── MedicalRecordEditView.xaml          # 病历录入
│   │   │   ├── MedicalRecordEditView.xaml.cs
│   │   │   ├── DiagnosisView.xaml                  # 诊断录入
│   │   │   ├── DiagnosisView.xaml.cs
│   │   │   ├── PrescriptionEditView.xaml           # 处方开具
│   │   │   ├── PrescriptionEditView.xaml.cs
│   │   │   ├── PhysiotherapyPlanView.xaml          # 理疗方案
│   │   │   ├── PhysiotherapyPlanView.xaml.cs
│   │   │   ├── TemplateSelectionView.xaml          # 经方选择
│   │   │   ├── TemplateSelectionView.xaml.cs
│   │   │   ├── HerbDosageView.xaml                 # 药材剂量调整
│   │   │   ├── HerbDosageView.xaml.cs
│   │   │   ├── SaveAsTemplateView.xaml             # 处方保存为经方
│   │   │   ├── SaveAsTemplateView.xaml.cs
│   │   │   ├── MedicalRecordQueryView.xaml         # 病历查询
│   │   │   ├── MedicalRecordQueryView.xaml.cs
│   │   │   ├── MedicalRecordDetailView.xaml        # 病历详情
│   │   │   ├── MedicalRecordDetailView.xaml.cs
│   │   │   ├── MedicalRecordShareView.xaml         # 病历共享设置
│   │   │   ├── MedicalRecordShareView.xaml.cs
│   │   │   ├── DoctorStatisticsView.xaml           # 医生统计
│   │   │   └── DoctorStatisticsView.xaml.cs
│   │   ├── ViewModels/
│   │   │   ├── DoctorWorkbenchViewModel.cs
│   │   │   ├── WaitingPatientsViewModel.cs
│   │   │   ├── PatientCallViewModel.cs
│   │   │   ├── ConsultationViewModel.cs
│   │   │   ├── MedicalRecordEditViewModel.cs
│   │   │   ├── DiagnosisViewModel.cs
│   │   │   ├── PrescriptionEditViewModel.cs
│   │   │   ├── PhysiotherapyPlanViewModel.cs
│   │   │   ├── TemplateSelectionViewModel.cs
│   │   │   ├── HerbDosageViewModel.cs
│   │   │   ├── SaveAsTemplateViewModel.cs
│   │   │   ├── MedicalRecordQueryViewModel.cs
│   │   │   ├── MedicalRecordDetailViewModel.cs
│   │   │   ├── MedicalRecordShareViewModel.cs
│   │   │   └── DoctorStatisticsViewModel.cs
│   │   ├── Models/
│   │   │   ├── ConsultationModel.cs
│   │   │   ├── MedicalRecordModel.cs               # 完整病历(基本信息+诊断+处方+理疗)
│   │   │   ├── DiagnosisModel.cs
│   │   │   ├── PrescriptionModel.cs
│   │   │   ├── PrescriptionItemModel.cs
│   │   │   ├── PhysiotherapyPlanModel.cs
│   │   │   ├── TCMExaminationModel.cs              # 中医四诊
│   │   │   ├── MedicalRecordQueryCriteria.cs
│   │   │   ├── MedicalRecordShareModel.cs
│   │   │   └── DoctorStatisticsModel.cs
│   │   ├── Services/
│   │   │   ├── ITCMDiagnosisService.cs
│   │   │   ├── TCMDiagnosisService.cs
│   │   │   ├── IPrescriptionCalculatorService.cs
│   │   │   └── PrescriptionCalculatorService.cs
│   │   ├── DoctorModule.cs
│   │   └── LYBT.WPF.Client.Modules.Doctor.csproj
│   │
│   ├── Cashier/                                    # 收银员模块 (收银员用户)
│   │   ├── Views/
│   │   │   ├── CashierWorkbenchView.xaml           # 收银员工作台
│   │   │   ├── CashierWorkbenchView.xaml.cs
│   │   │   ├── PendingBillsView.xaml               # 待收费列表
│   │   │   ├── PendingBillsView.xaml.cs
│   │   │   ├── BillingView.xaml                    # 收费界面
│   │   │   ├── BillingView.xaml.cs
│   │   │   ├── FeeCalculationView.xaml             # 费用计算
│   │   │   ├── FeeCalculationView.xaml.cs
│   │   │   ├── PaymentView.xaml                    # 付款处理
│   │   │   ├── PaymentView.xaml.cs
│   │   │   ├── InvoicePrintView.xaml               # 发票打印
│   │   │   ├── InvoicePrintView.xaml.cs
│   │   │   ├── RefundView.xaml                     # 退费处理
│   │   │   ├── RefundView.xaml.cs
│   │   │   ├── PaymentHistoryView.xaml             # 收费历史
│   │   │   ├── PaymentHistoryView.xaml.cs
│   │   │   ├── CashierStatisticsView.xaml          # 收银统计
│   │   │   └── CashierStatisticsView.xaml.cs
│   │   ├── ViewModels/
│   │   │   ├── CashierWorkbenchViewModel.cs
│   │   │   ├── PendingBillsViewModel.cs
│   │   │   ├── BillingViewModel.cs
│   │   │   ├── FeeCalculationViewModel.cs
│   │   │   ├── PaymentViewModel.cs
│   │   │   ├── InvoicePrintViewModel.cs
│   │   │   ├── RefundViewModel.cs
│   │   │   ├── PaymentHistoryViewModel.cs
│   │   │   └── CashierStatisticsViewModel.cs
│   │   ├── Models/
│   │   │   ├── BillingModel.cs
│   │   │   ├── PaymentModel.cs
│   │   │   ├── InvoiceModel.cs
│   │   │   ├── RefundModel.cs
│   │   │   ├── FeeItemModel.cs
│   │   │   ├── PaymentMethodModel.cs
│   │   │   └── CashierStatisticsModel.cs
│   │   ├── Services/
│   │   │   ├── IFeeCalculationService.cs
│   │   │   ├── FeeCalculationService.cs
│   │   │   ├── IInvoiceService.cs
│   │   │   └── InvoiceService.cs
│   │   ├── CashierModule.cs
│   │   └── LYBT.WPF.Client.Modules.Cashier.csproj
│   │
│   ├── Pharmacist/                                 # 药剂师模块 (药剂师用户)
│   │   ├── Views/
│   │   │   ├── PharmacistWorkbenchView.xaml        # 药剂师工作台
│   │   │   ├── PharmacistWorkbenchView.xaml.cs
│   │   │   ├── DispenseQueueView.xaml              # 待抓药列表
│   │   │   ├── DispenseQueueView.xaml.cs
│   │   │   ├── PrescriptionDetailView.xaml         # 处方详情
│   │   │   ├── PrescriptionDetailView.xaml.cs
│   │   │   ├── DispenseView.xaml                   # 药品调剂
│   │   │   ├── DispenseView.xaml.cs
│   │   │   ├── HerbWeighingView.xaml               # 药材称量
│   │   │   ├── HerbWeighingView.xaml.cs
│   │   │   ├── DispenseReviewView.xaml             # 调剂复核
│   │   │   ├── DispenseReviewView.xaml.cs
│   │   │   ├── DispenseCompletionView.xaml         # 发药完成
│   │   │   ├── DispenseCompletionView.xaml.cs
│   │   │   ├── StockCheckView.xaml                 # 库存检查
│   │   │   ├── StockCheckView.xaml.cs
│   │   │   ├── DispenseHistoryView.xaml            # 调剂历史
│   │   │   ├── DispenseHistoryView.xaml.cs
│   │   │   ├── PharmacistStatisticsView.xaml       # 药房统计
│   │   │   └── PharmacistStatisticsView.xaml.cs
│   │   ├── ViewModels/
│   │   │   ├── PharmacistWorkbenchViewModel.cs
│   │   │   ├── DispenseQueueViewModel.cs
│   │   │   ├── PrescriptionDetailViewModel.cs
│   │   │   ├── DispenseViewModel.cs
│   │   │   ├── HerbWeighingViewModel.cs
│   │   │   ├── DispenseReviewViewModel.cs
│   │   │   ├── DispenseCompletionViewModel.cs
│   │   │   ├── StockCheckViewModel.cs
│   │   │   ├── DispenseHistoryViewModel.cs
│   │   │   └── PharmacistStatisticsViewModel.cs
│   │   ├── Models/
│   │   │   ├── DispenseTaskModel.cs
│   │   │   ├── DispenseItemModel.cs
│   │   │   ├── HerbWeighingModel.cs
│   │   │   ├── DispenseReviewModel.cs
│   │   │   ├── StockCheckModel.cs
│   │   │   ├── DispenseHistoryModel.cs
│   │   │   └── PharmacistStatisticsModel.cs
│   │   ├── Services/
│   │   │   ├── IDispenseCalculationService.cs
│   │   │   ├── DispenseCalculationService.cs
│   │   │   ├── IStockUpdateService.cs
│   │   │   └── StockUpdateService.cs
│   │   ├── PharmacistModule.cs
│   │   └── LYBT.WPF.Client.Modules.Pharmacist.csproj
│   │
│   ├── Physiotherapist/                            # 理疗师模块 (理疗师用户)
│   │   ├── Views/
│   │   │   ├── PhysiotherapistWorkbenchView.xaml   # 理疗师工作台
│   │   │   ├── PhysiotherapistWorkbenchView.xaml.cs
│   │   │   ├── TreatmentQueueView.xaml             # 待理疗列表
│   │   │   ├── TreatmentQueueView.xaml.cs
│   │   │   ├── TreatmentPlanDetailView.xaml        # 理疗方案详情
│   │   │   ├── TreatmentPlanDetailView.xaml.cs
│   │   │   ├── TreatmentExecutionView.xaml         # 理疗执行
│   │   │   ├── TreatmentExecutionView.xaml.cs
│   │   │   ├── TreatmentRecordView.xaml            # 理疗记录
│   │   │   ├── TreatmentRecordView.xaml.cs
│   │   │   ├── EquipmentManageView.xaml            # 设备管理
│   │   │   ├── EquipmentManageView.xaml.cs
│   │   │   ├── TreatmentCompletionView.xaml        # 理疗完成
│   │   │   ├── TreatmentCompletionView.xaml.cs
│   │   │   ├── TreatmentHistoryView.xaml           # 理疗历史
│   │   │   ├── TreatmentHistoryView.xaml.cs
│   │   │   ├── PhysiotherapyStatisticsView.xaml    # 理疗统计
│   │   │   └── PhysiotherapyStatisticsView.xaml.cs
│   │   ├── ViewModels/
│   │   │   ├── PhysiotherapistWorkbenchViewModel.cs
│   │   │   ├── TreatmentQueueViewModel.cs
│   │   │   ├── TreatmentPlanDetailViewModel.cs
│   │   │   ├── TreatmentExecutionViewModel.cs
│   │   │   ├── TreatmentRecordViewModel.cs
│   │   │   ├── EquipmentManageViewModel.cs
│   │   │   ├── TreatmentCompletionViewModel.cs
│   │   │   ├── TreatmentHistoryViewModel.cs
│   │   │   └── PhysiotherapyStatisticsViewModel.cs
│   │   ├── Models/
│   │   │   ├── TreatmentTaskModel.cs
│   │   │   ├── TreatmentPlanModel.cs
│   │   │   ├── TreatmentItemModel.cs
│   │   │   ├── TreatmentRecordModel.cs
│   │   │   ├── EquipmentModel.cs
│   │   │   ├── TreatmentHistoryModel.cs
│   │   │   └── PhysiotherapyStatisticsModel.cs
│   │   ├── Services/
│   │   │   ├── ITreatmentScheduleService.cs
│   │   │   ├── TreatmentScheduleService.cs
│   │   │   ├── IEquipmentService.cs
│   │   │   └── EquipmentService.cs
│   │   ├── PhysiotherapistModule.cs
│   │   └── LYBT.WPF.Client.Modules.Physiotherapist.csproj
│   │
│   └── Common/                                     # 通用模块
│       ├── Views/
│       │   ├── ConfirmationDialogView.xaml         # 确认对话框
│       │   ├── ConfirmationDialogView.xaml.cs
│       │   ├── MessageBoxView.xaml                 # 消息框
│       │   ├── MessageBoxView.xaml.cs
│       │   ├── ProgressDialogView.xaml             # 进度对话框
│       │   ├── ProgressDialogView.xaml.cs
│       │   ├── SearchDialogView.xaml               # 搜索对话框
│       │   ├── SearchDialogView.xaml.cs
│       │   ├── PrintPreviewView.xaml               # 打印预览
│       │   └── PrintPreviewView.xaml.cs
│       ├── ViewModels/
│       │   ├── ConfirmationDialogViewModel.cs
│       │   ├── MessageBoxViewModel.cs
│       │   ├── ProgressDialogViewModel.cs
│       │   ├── SearchDialogViewModel.cs
│       │   └── PrintPreviewViewModel.cs
│       ├── Models/
│       │   ├── DialogResultModel.cs
│       │   ├── ProgressModel.cs
│       │   └── SearchCriteriaModel.cs
│       ├── CommonModule.cs
│       └── LYBT.WPF.Client.Modules.Common.csproj
│
├── LYBT.WPF.Client.Infrastructure/                 # 基础设施
│   ├── Behaviors/
│   │   ├── TextBoxBehavior.cs
│   │   ├── DataGridBehavior.cs
│   │   ├── PasswordBoxBehavior.cs
│   │   ├── WindowBehavior.cs
│   │   └── KeyboardShortcutBehavior.cs
│   ├── Converters/
│   │   ├── BoolToVisibilityConverter.cs
│   │   ├── DateTimeConverter.cs
│   │   ├── StatusToColorConverter.cs
│   │   ├── EnumToStringConverter.cs
│   │   ├── NullToVisibilityConverter.cs
│   │   └── InverseBooleanConverter.cs
│   ├── Controls/
│   │   ├── CustomDataGrid.cs
│   │   ├── SearchTextBox.cs
│   │   ├── LoadingControl.cs
│   │   ├── NumericUpDown.cs
│   │   ├── DateTimePicker.cs
│   │   ├── AutoCompleteTextBox.cs
│   │   ├── StatusIndicator.cs
│   │   └── PrintableControl.cs
│   ├── Helpers/
│   │   ├── ValidationHelper.cs
│   │   ├── PrintHelper.cs
│   │   ├── ExportHelper.cs
│   │   ├── SecurityHelper.cs
│   │   ├── BackupHelper.cs
│   │   ├── ConfigHelper.cs
│   │   └── LoggingHelper.cs
│   ├── Themes/
│   │   ├── Generic.xaml
│   │   ├── Colors.xaml
│   │   ├── Styles.xaml
│   │   ├── LightTheme.xaml
│   │   ├── DarkTheme.xaml
│   │   └── MedicalTheme.xaml
│   ├── Templates/
│   │   ├── PrintTemplates/
│   │   │   ├── RegistrationTicketTemplate.xaml     # 挂号单模板
│   │   │   ├── PrescriptionTemplate.xaml           # 处方单模板
│   │   │   ├── InvoiceTemplate.xaml                # 发票模板
│   │   │   └── MedicalRecordTemplate.xaml          # 病历模板
│   │   └── ReportTemplates/
│   │       ├── StatisticsReportTemplate.xaml       # 统计报表模板
│   │       └── ChartTemplate.xaml                  # 图表模板
│   ├── Validations/
│   │   ├── Rules/
│   │   │   ├── RequiredFieldRule.cs
│   │   │   ├── EmailValidationRule.cs
│   │   │   ├── PhoneValidationRule.cs
│   │   │   ├── IdCardValidationRule.cs
│   │   │   ├── PasswordValidationRule.cs
│   │   │   └── NumericRangeRule.cs
│   │   └── Attributes/
│   │       ├── RequiredAttribute.cs
│   │       ├── RangeAttribute.cs
│   │       └── RegexAttribute.cs
│   ├── Security/
│   │   ├── Encryption/
│   │   │   ├── IEncryptionService.cs
│   │   │   └── EncryptionService.cs
│   │   ├── Authorization/
│   │   │   ├── IAuthorizationService.cs
│   │   │   └── AuthorizationService.cs
│   │   └── Audit/
│   │       ├── IAuditService.cs
│   │       └── AuditService.cs
│   └── LYBT.WPF.Client.Infrastructure.csproj
│
├── Tests/                                          # 测试项目
│   ├── LYBT.WPF.Client.Tests.Unit/
│   │   ├── Services/
│   │   │   ├── AuthenticationServiceTests.cs
│   │   │   ├── ConsultationServiceTests.cs
│   │   │   ├── BillingServiceTests.cs
│   │   │   └── PharmacyServiceTests.cs
│   │   ├── ViewModels/
│   │   │   ├── LoginViewModelTests.cs
│   │   │   ├── ConsultationViewModelTests.cs
│   │   │   └── BillingViewModelTests.cs
│   │   ├── Helpers/
│   │   │   ├── ValidationHelperTests.cs
│   │   │   └── SecurityHelperTests.cs
│   │   └── LYBT.WPF.Client.Tests.Unit.csproj
│   ├── LYBT.WPF.Client.Tests.Integration/
│   │   ├── ApiTests/
│   │   │   ├── AuthenticationApiTests.cs
│   │   │   ├── ConsultationApiTests.cs
│   │   │   └── BillingApiTests.cs
│   │   ├── WorkflowTests/
│   │   │   ├── RegistrationWorkflowTests.cs
│   │   │   ├── ConsultationWorkflowTests.cs
│   │   │   └── BillingWorkflowTests.cs
│   │   └── LYBT.WPF.Client.Tests.Integration.csproj
│   └── LYBT.WPF.Client.Tests.UI/
│       ├── UITests/
│       │   ├── LoginUITests.cs
│       │   ├── ConsultationUITests.cs
│       │   └── BillingUITests.cs
│       └── LYBT.WPF.Client.Tests.UI.csproj
│
├── Documentation/                                  # 文档
│   ├── Requirements/
│   │   ├── 功能需求说明书.md
│   │   ├── 技术架构设计.md
│   │   └── 用户角色权限说明.md
│   ├── Development/
│   │   ├── 开发环境搭建.md
│   │   ├── 编码规范.md
│   │   └── 模块开发指南.md
│   ├── Deployment/
│   │   ├── 部署指南.md
│   │   ├── 配置说明.md
│   │   └── 升级指南.md
│   └── UserManuals/
│       ├── 系统管理员手册.md
│       ├── 医生使用手册.md
│       ├── 前台使用手册.md
│       ├── 收银员使用手册.md
│       ├── 药剂师使用手册.md
│       └── 理疗师使用手册.md
│
├── Scripts/                                        # 脚本文件
│   ├── Build/
│   │   ├── build.bat
│   │   └── build.ps1
│   ├── Deploy/
│   │   ├── deploy.bat
│   │   └── deploy.ps1
│   └── Database/
│       ├── init.sql
│       └── migrate.sql
│
├── Resources/                                      # 资源文件
│   ├── Images/
│   │   ├── Icons/
│   │   ├── Logos/
│   │   └── UI/
│   ├── Fonts/
│   ├── Templates/
│   │   ├── Excel/
│   │   └── Word/
│   └── Configuration/
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       └── appsettings.Production.json
│
├── Tools/                                          # 工具
│   ├── DatabaseMigration/
│   ├── DataImport/
│   └── CodeGeneration/
│
├── LYBT.WPF.Client.sln                            # 解决方案文件
├── Directory.Build.props                          # 全局项目属性
├── README.md                                       # 项目说明
├── LICENSE                                         # 许可证
└── .gitignore                                      # Git忽略文件
```

## 技术栈说明

### 核心框架
- **.NET 8.0**: 目标框架
- **WPF**: UI框架  
- **Prism 8.1+**: MVVM框架和模块化
- **Unity Container**: 依赖注入容器
- **MaterialDesignInXAML**: 现代化UI设计
- **Mahapps.Metro**: 增强UI控件

### 通信与数据
- **HttpClient**: HTTP API调用
- **Newtonsoft.Json**: JSON序列化
- **Refit**: 类型安全的HTTP客户端
- **AutoMapper**: 对象映射

### 辅助库
- **Serilog**: 结构化日志
- **FluentValidation**: 数据验证
- **LiveCharts**: 图表展示
- **NPOI**: Excel操作
- **iTextSharp**: PDF生成

## 关键特性

### 1. 模块化架构
- 基于Prism的模块化设计
- 角色驱动的模块加载
- 独立的模块生命周期管理

### 2. 安全性
- 基于角色的访问控制(RBAC)
- 操作审计日志
- 数据加密存储
- 会话超时管理

### 3. 用户体验
- 现代化Material Design界面
- 主题切换支持
- 快捷键操作
- 智能提示和自动完成

### 4. 数据管理
- 实时状态同步
- 数据备份恢复
- 批量导入导出
- 数据完整性检查

### 5. 业务流程
- 状态驱动的工作流
- 流程撤销机制
- 实时队列管理
- 跨模块数据流转

### 6. 报表统计
- 可视化图表展示
- 多维度统计分析
- 自定义报表模板
- 数据导出功能

### 7. 打印功能
- 自定义打印模板
- 批量打印支持
- 打印预览
- 多种纸张格式

### 8. 系统管理
- 参数配置管理
- 用户权限管理
- 系统监控
- 日志管理

## 部署架构

### 客户端部署
- **单机版**: 直接安装运行
- **网络版**: 通过网络访问后端API
- **便携版**: 免安装绿色版本

### 数据存储
- **本地数据库**: SQLite(单机版)
- **网络数据库**: SQL Server/MySQL(网络版)
- **配置存储**: 本地配置文件

### 系统要求
- **操作系统**: Windows 10/11
- **运行时**: .NET 8.0 Runtime
- **内存**: 最低4GB，推荐8GB
- **硬盘**: 最低2GB可用空间

这个完整的项目结构覆盖了所有需求功能，采用现代化的技术栈和架构设计，确保系统的可维护性、可扩展性和用户体验。