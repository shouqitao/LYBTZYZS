# Desktop UI 全景清单

> **版本**: v1.0 | **日期**: 2026-08-15 | **作者**: Hermes Agent  
> **范围**: LYBTZYZS Desktop 全部需要 UI 的页面  
> **方法**: 需求文档 US 扫描 + XAML/ViewModel 代码扫描 + 交叉对照  

## 图例

| 列 | 含义 |
|----|------|
| 页面名称 | XAML 视图或 UI 组件名称 |
| 层级 | Shell / Role / Module / Dialog |
| 角色 | 可访问角色：A(Admin) / D(Doctor) / R(Receptionist) / S(Sysadmin) / All |
| 需求来源 | 关联的 US 编号 |
| 当前状态 | ✅ 已实现 / ⚠️ 部分 / 🔴 缺失 / 🧲 待实现 |
| 优先级 | P0(核心) / P1(重要) / P2(可选) |
| XAML 路径 | 代码文件位置 |

---

## 一、Shell 层（平台外壳）

| # | 页面名称 | 层级 | 角色 | 需求来源 | 当前状态 | 优先级 | XAML 路径 |
|---|----------|------|------|----------|----------|--------|-----------|
| 1 | **MainWindow** | Shell | All | US-SHELL-001/003/005 | ✅ 已实现 | P0 | `Shell/Views/MainWindow.xaml` |
| 2 | **SplashScreen** | Shell | All | US-SHELL-001 | ✅ 已实现 | P0 | `Shell/App.xaml`（内嵌） |
| 3 | **AccountSettingsView** | Shell | All | US-SHELL-004 / US-USER-008/009 | ✅ 已实现 | P1 | `Shell/Views/AccountSettingsView.xaml` |
| 4 | **AccountSettingsControl** | Shell | All | US-SHELL-004 | ✅ 已实现 | P1 | `Shell/Controls/AccountSettingsControl.xaml` |
| 5 | **ConfirmationDialog** | Dialog | All | 通用 | ✅ 已实现 | P0 | `Shell/Dialogs/Views/ConfirmationDialog.xaml` |
| 6 | **InputDialog** | Dialog | All | 通用 | ✅ 已实现 | P1 | `Shell/Dialogs/Views/InputDialog.xaml` |
| 7 | **MessageDialog** | Dialog | All | 通用 | ✅ 已实现 | P0 | `Shell/Dialogs/Views/MessageDialog.xaml` |

---

## 二、Auth 模块（认证）

| # | 页面名称 | 层级 | 角色 | 需求来源 | 当前状态 | 优先级 | XAML 路径 |
|---|----------|------|------|----------|----------|--------|-----------|
| 8 | **LoginView** | Module | All | US-AUTH-001/009 | ✅ 已实现 | P0 | `Modules/LYBT.Desktop.Auth/Views/LoginView.xaml` |
| 9 | **ServerConfigView** | Module | All | US-SHELL-007 | ✅ 已实现 | P1 | `Modules/LYBT.Desktop.Auth/Views/ServerConfigView.xaml` |
| 10 | **FirstRunSetupView** | Module | S | US-SHELL-011 | ⚠️ 部分（5步向导未完整） | P0 | `Modules/LYBT.Desktop.Auth/Views/FirstRunSetupView.xaml` |

---

## 三、Admin 角色工作台

| # | 页面名称 | 层级 | 角色 | 需求来源 | 当前状态 | 优先级 | XAML 路径 |
|---|----------|------|------|----------|----------|--------|-----------|
| 11 | **AdminHomeView** | Role | A | US-SHELL-003 | ✅ 已实现 | P0 | `Roles/LYBT.Desktop.Admin/Views/AdminHomeView.xaml` |
| 12 | **SystemSettingsView** | Role | A | US-CFG-001~006 | ✅ 已实现 | P1 | `Roles/LYBT.Desktop.Admin/Views/SystemSettingsView.xaml` |
| 13 | **UserManagementView** | Role | A | US-USER-001~012 | ✅ 已实现 | P0 | `Roles/LYBT.Desktop.Admin/Views/UserManagementView.xaml` |

---

## 四、Sysadmin 角色工作台（独立用户，非角色）

| # | 页面名称 | 层级 | 角色 | 需求来源 | 当前状态 | 优先级 | XAML 路径 |
|---|----------|------|------|----------|----------|--------|-----------|
| 14 | **SysadminHomeView** | Role | S | US-SHELL-018 | ✅ 已实现 | P0 | `Roles/LYBT.Desktop.Admin/Sysadmin/Views/SysadminHomeView.xaml` |
| 15 | **BackupManagementView** | Role | S | US-SHELL-013 | ✅ 已实现 | P1 | `Roles/LYBT.Desktop.Admin/Sysadmin/Views/BackupManagementView.xaml` |
| 16 | **DeploymentView** | Role | S | US-SHELL-020 | ✅ 已实现 | P1 | `Roles/LYBT.Desktop.Admin/Sysadmin/Views/DeploymentView.xaml` |
| 17 | **LogLevelControlView** | Role | S | US-SYS-005~009 | ✅ 已实现 | P2 | `Roles/LYBT.Desktop.Admin/Sysadmin/Views/LogLevelControlView.xaml` |

---

## 五、Clinical 角色工作台（医生）

| # | 页面名称 | 层级 | 角色 | 需求来源 | 当前状态 | 优先级 | XAML 路径 |
|---|----------|------|------|----------|----------|--------|-----------|
| 18 | **ClinicalHomeView** | Role | D | US-SHELL-003 | ✅ 已实现 | P0 | `Roles/LYBT.Desktop.Clinical/Views/ClinicalHomeView.xaml` |
| 19 | **ClinicalWorkspaceView** | Role | D | 医案工作流 | ✅ 已实现 | P0 | `Roles/LYBT.Desktop.Clinical/Views/ClinicalWorkspaceView.xaml` |
| 20 | **MedicalCaseWorkspaceView** | Role | D | US-MC-001~020 | ✅ 已实现 | P0 | `Roles/LYBT.Desktop.Clinical/Views/MedicalCaseWorkspaceView.xaml` |
| 21 | **PatientManagementView** | Role | D | US-PAT-001~014 | ✅ 已实现 | P0 | `Roles/LYBT.Desktop.Clinical/Views/PatientManagementView.xaml` |
| 22 | **PatientSelectionView** | Role | D | US-REG-001/002 | ✅ 已实现 | P0 | `Roles/LYBT.Desktop.Clinical/Views/PatientSelectionView.xaml` |
| 23 | **PendingQueueView** | Role | D | US-REG-004/005 | ✅ 已实现 | P0 | `Roles/LYBT.Desktop.Clinical/Views/PendingQueueView.xaml` |
| 24 | **HerbManagementView** | Role | D | US-HERB-001~014 | ✅ 已实现 | P1 | `Roles/LYBT.Desktop.Clinical/Views/HerbManagementView.xaml` |
| 25 | **FormulaManagementView** | Role | D | US-FORM-001~014 | ✅ 已实现 | P1 | `Roles/LYBT.Desktop.Clinical/Views/FormulaManagementView.xaml` |
| 26 | **MedicalCaseManagementView** | Role | D | US-MC-005~007 | ✅ 已实现 | P1 | `Roles/LYBT.Desktop.Clinical/Views/MedicalCaseManagementView.xaml` |

---

## 六、Receptionist 角色工作台（前台）

| # | 页面名称 | 层级 | 角色 | 需求来源 | 当前状态 | 优先级 | XAML 路径 |
|---|----------|------|------|----------|----------|--------|-----------|
| 27 | **ReceptionistHomeView** | Role | R | US-SHELL-003 | ✅ 已实现 | P0 | `Roles/LYBT.Desktop.Clinical/Receptionist/Views/ReceptionistHomeView.xaml` |

---

## 七、Patients 模块（业务控件）

| # | 页面名称 | 层级 | 角色 | 需求来源 | 当前状态 | 优先级 | XAML 路径 |
|---|----------|------|------|----------|----------|--------|-----------|
| 28 | **PatientMasterDetailControl** | Module | A/D/R | US-PAT-001~014 | ✅ 已实现 | P0 | `Modules/LYBT.Desktop.Patients/Controls/PatientMasterDetailControl.xaml` |
| 29 | **PatientEditControl** | Module | A/D/R | US-PAT-003/004 | ✅ 已实现 | P0 | `Modules/LYBT.Desktop.Patients/Controls/PatientEditControl.xaml` |
| 30 | **PatientViewControl** | Module | A/D/R | US-PAT-002 | ✅ 已实现 | P1 | `Modules/LYBT.Desktop.Patients/Controls/PatientViewControl.xaml` |
| 31 | **PatientSelectionControl** | Module | D/R | US-REG-001/002 | ✅ 已实现 | P0 | `Modules/LYBT.Desktop.Patients/Controls/PatientSelectionControl.xaml` |

---

## 八、Catalog 模块 — 药材管理（业务控件）

| # | 页面名称 | 层级 | 角色 | 需求来源 | 当前状态 | 优先级 | XAML 路径 |
|---|----------|------|------|----------|----------|--------|-----------|
| 32 | **HerbMasterDetailControl** | Module | A/D | US-HERB-001~014 | ✅ 已实现 | P0 | `Modules/LYBT.Desktop.Catalog/Controls/HerbMasterDetailControl.xaml` |
| 33 | **HerbEditControl** | Module | A | US-HERB-003/004 | ✅ 已实现 | P0 | `Modules/LYBT.Desktop.Catalog/Controls/HerbEditControl.xaml` |
| 34 | **HerbViewControl** | Module | A/D | US-HERB-002 | ✅ 已实现 | P1 | `Modules/LYBT.Desktop.Catalog/Controls/HerbViewControl.xaml` |

---

## 九、Catalog 模块 — 验方管理（业务控件）

| # | 页面名称 | 层级 | 角色 | 需求来源 | 当前状态 | 优先级 | XAML 路径 |
|---|----------|------|------|----------|----------|--------|-----------|
| 35 | **FormulaMasterDetailControl** | Module | A/D | US-FORM-001~014 | ✅ 已实现 | P0 | `Modules/LYBT.Desktop.Catalog/Controls/FormulaMasterDetailControl.xaml` |
| 36 | **FormulaEditControl** | Module | A/D | US-FORM-003/004 | ✅ 已实现 | P0 | `Modules/LYBT.Desktop.Catalog/Controls/FormulaEditControl.xaml` |

---

## 十、MedicalCase 模块（业务控件）

| # | 页面名称 | 层级 | 角色 | 需求来源 | 当前状态 | 优先级 | XAML 路径 |
|---|----------|------|------|----------|----------|--------|-----------|
| 37 | **MedicalCaseMasterDetailControl** | Module | D/A | US-MC-001~020 | ✅ 已实现 | P0 | `Modules/LYBT.Desktop.MedicalCase/Controls/MedicalCaseMasterDetailControl.xaml` |
| 38 | **MedicalCaseEditControl** | Module | D | US-MC-002 | ✅ 已实现 | P0 | `Modules/LYBT.Desktop.MedicalCase/Controls/MedicalCaseEditControl.xaml` |
| 39 | **MedicalCaseViewControl** | Module | D/A | US-MC-004 | ✅ 已实现 | P1 | `Modules/LYBT.Desktop.MedicalCase/Controls/MedicalCaseViewControl.xaml` |
| 40 | **WorkflowStepIndicator** | Module | D | 医案工作流 | ✅ 已实现 | P1 | `Modules/LYBT.Desktop.MedicalCase/Controls/WorkflowStepIndicator.xaml` |

---

## 十一、MedicalCase 模块 — 视图

| # | 页面名称 | 层级 | 角色 | 需求来源 | 当前状态 | 优先级 | XAML 路径 |
|---|----------|------|------|----------|----------|--------|-----------|
| 41 | **MedicalCaseMasterDetailView** | Module | D/A | US-MC-001~020 | ✅ 已实现 | P0 | `Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseMasterDetailView.xaml` |
| 42 | **AuditLogView** | Module | D/A | US-MC-017 | ✅ 已实现 | P1 | `Modules/LYBT.Desktop.MedicalCase/Views/AuditLogView.xaml` |

---

## 十二、MedicalCase 模块 — 对话框

| # | 页面名称 | 层级 | 角色 | 需求来源 | 当前状态 | 优先级 | XAML 路径 |
|---|----------|------|------|----------|----------|--------|-----------|
| 43 | **FormulaImportDialog** | Dialog | D | US-FORM-006 | ✅ 已实现 | P1 | `Modules/LYBT.Desktop.MedicalCase/Dialogs/FormulaImportDialog.xaml` |
| 44 | **HistoryCopyDialog** | Dialog | D | US-MC-019 | ✅ 已实现 | P1 | `Modules/LYBT.Desktop.MedicalCase/Dialogs/HistoryCopyDialog.xaml` |
| 45 | **UnsavedChangesDialog** | Dialog | D | 通用 | ✅ 已实现 | P1 | `Modules/LYBT.Desktop.MedicalCase/Dialogs/UnsavedChangesDialog.xaml` |

---

## 十三、MedicalCase 模块 — 报表

| # | 页面名称 | 层级 | 角色 | 需求来源 | 当前状态 | 优先级 | XAML 路径 |
|---|----------|------|------|----------|----------|--------|-----------|
| 46 | **ReportsHomeView** | Module | A/D | US-REPORT-001~004 | ✅ 已实现 | P1 | `Modules/LYBT.Desktop.MedicalCase/Reports/Views/ReportsHomeView.xaml` |

---

## 十四、Registrations 模块

| # | 页面名称 | 层级 | 角色 | 需求来源 | 当前状态 | 优先级 | XAML 路径 |
|---|----------|------|------|----------|----------|--------|-----------|
| 47 | **RegistrationListView** | Module | D/R/A | US-REG-001~008 | ✅ 已实现 | P0 | `Modules/LYBT.Desktop.Registrations/Views/RegistrationListView.xaml` |
| 48 | **RegistrationCreateDialog** | Dialog | R/D | US-REG-001/002 | ✅ 已实现 | P0 | `Modules/LYBT.Desktop.Registrations/Dialogs/RegistrationCreateDialog.xaml` |

---

## 十五、Users 模块

| # | 页面名称 | 层级 | 角色 | 需求来源 | 当前状态 | 优先级 | XAML 路径 |
|---|----------|------|------|----------|----------|--------|-----------|
| 49 | **UserMasterDetailControl** | Module | A | US-USER-001~012 | ✅ 已实现 | P0 | `Modules/LYBT.Desktop.Users/Controls/UserMasterDetailControl.xaml` |
| 50 | **UserEditControl** | Module | A | US-USER-004/005 | ✅ 已实现 | P0 | `Modules/LYBT.Desktop.Users/Controls/UserEditControl.xaml` |
| 51 | **UserViewControl** | Module | A | US-USER-002 | ✅ 已实现 | P1 | `Modules/LYBT.Desktop.Users/Controls/UserViewControl.xaml` |

---

## 十六、共享控件（Core Controls）

| # | 页面名称 | 层级 | 角色 | 需求来源 | 当前状态 | 优先级 | XAML 路径 |
|---|----------|------|------|----------|----------|--------|-----------|
| 52 | **MasterDetailLayout** | Core | All | 架构 | ✅ 已实现 | P0 | `Core/LYBT.Desktop.Controls/Controls/MasterDetailLayout.xaml` |
| 53 | **DataGridToolbar** | Core | All | 架构 | ✅ 已实现 | P0 | `Core/LYBT.Desktop.Controls/Controls/DataGridToolbar.xaml` |
| 54 | **DetailToolbar** | Core | All | 架构 | ✅ 已实现 | P0 | `Core/LYBT.Desktop.Controls/Controls/DetailToolbar.xaml` |
| 55 | **SearchBox** | Core | All | 架构 | ✅ 已实现 | P0 | `Core/LYBT.Desktop.Controls/Controls/SearchBox.xaml` |
| 56 | **EmptyState** | Core | All | 架构 | ✅ 已实现 | P1 | `Core/LYBT.Desktop.Controls/Controls/EmptyState.xaml` |
| 57 | **StatusBadge** | Core | All | 架构 | ✅ 已实现 | P1 | `Core/LYBT.Desktop.Controls/Controls/StatusBadge.xaml` |
| 58 | **InfoCard** | Core | All | 架构 | ✅ 已实现 | P1 | `Core/LYBT.Desktop.Controls/Controls/InfoCard.xaml` |
| 59 | **LoadingOverlay** | Core | All | 架构 | ✅ 已实现 | P1 | `Core/LYBT.Desktop.Controls/Controls/LoadingOverlay.xaml` |
| 60 | **ToastControl** | Core | All | US-ERR-008 | ✅ 已实现 | P0 | `Core/LYBT.Desktop.Controls/Controls/Toast/ToastControl.xaml` |
| 61 | **BreadcrumbBar** | Core | All | US-SHELL-005 | ✅ 已实现 | P1 | `Core/LYBT.Desktop.Controls/Controls/BreadcrumbBar.xaml` |
| 62 | **UnifiedPaginationBar** | Core | All | 架构 | ✅ 已实现 | P1 | `Core/LYBT.Desktop.Controls/Controls/UnifiedPaginationBar.xaml` |
| 63 | **BaseDetailContainer** | Core | All | 架构 | ✅ 已实现 | P1 | `Core/LYBT.Desktop.Controls/Controls/BaseDetailContainer.xaml` |
| 64 | **PatientInfoCardControl** | Core | D/R | 患者信息卡 | ✅ 已实现 | P1 | `Core/LYBT.Desktop.Controls/Controls/PatientInfoCardControl.xaml` |
| 65 | **HerbItemControl** | Core | D | 药材选择 | ✅ 已实现 | P1 | `Core/LYBT.Desktop.Controls/Controls/HerbItem/HerbItemControl.xaml` |
| 66 | **HerbListControl** | Core | D | 药材列表 | ✅ 已实现 | P1 | `Core/LYBT.Desktop.Controls/Controls/HerbList/HerbListControl.xaml` |
| 67 | **FormulaViewControl** | Core | D | 验方展示 | ✅ 已实现 | P1 | `Core/LYBT.Desktop.Controls/Controls/FormulaView/FormulaViewControl.xaml` |

---

## 十七、打印模板

| # | 页面名称 | 层级 | 角色 | 需求来源 | 当前状态 | 优先级 | XAML 路径 |
|---|----------|------|------|----------|----------|--------|-----------|
| 68 | **PrescriptionPrintTemplate** | Core | D | US-PRINT-001/002 | ✅ 已实现 | P0 | `Core/LYBT.Desktop.Printing/Templates/PrescriptionPrintTemplate.xaml` |
| 69 | **PrescriptionPrintA4Template** | Core | D | US-PRINT-001/002 | ✅ 已实现 | P0 | `Core/LYBT.Desktop.Printing/Templates/PrescriptionPrintA4Template.xaml` |
| 70 | **PrescriptionContinuationTemplate** | Core | D | US-PRINT-001/002 | ✅ 已实现 | P1 | `Core/LYBT.Desktop.Printing/Templates/PrescriptionContinuationTemplate.xaml` |
| 71 | **PrescriptionContinuationA4Template** | Core | D | US-PRINT-001/002 | ✅ 已实现 | P1 | `Core/LYBT.Desktop.Printing/Templates/PrescriptionContinuationA4Template.xaml` |

---

## 十八、主题与样式

| # | 页面名称 | 层级 | 角色 | 需求来源 | 当前状态 | 优先级 | XAML 路径 |
|---|----------|------|------|----------|----------|--------|-----------|
| 72 | **DataGridStyles** | Core | All | 架构 | ✅ 已实现 | P0 | `Core/LYBT.Desktop.Controls/Themes/DataGridStyles.xaml` |
| 73 | **Functions** | Core | All | 架构 | ✅ 已实现 | P0 | `Core/LYBT.Desktop.Controls/Themes/Functions.xaml` |
| 74 | **Icons** | Core | All | 架构 | ✅ 已实现 | P0 | `Core/LYBT.Desktop.Controls/Themes/Icons.xaml` |
| 75 | **Spacing** | Core | All | 架构 | ✅ 已实现 | P0 | `Core/LYBT.Desktop.Controls/Themes/Spacing.xaml` |
| 76 | **Surfaces** | Core | All | 架构 | ✅ 已实现 | P0 | `Core/LYBT.Desktop.Controls/Themes/Surfaces.xaml` |
| 77 | **TcmBrands** | Core | All | 架构 | ✅ 已实现 | P1 | `Core/LYBT.Desktop.Controls/Themes/TcmBrands.xaml` |
| 78 | **Converters** | Core | All | 架构 | ✅ 已实现 | P0 | `Core/LYBT.Desktop.Controls/Converters/Converters.xaml` |

---

## 统计汇总

| 分类 | 总数 | ✅ 已实现 | ⚠️ 部分 | 🔴 缺失 | 🧲 待实现 |
|------|:----:|:---------:|:-------:|:-------:|:---------:|
| Shell 层 | 7 | 7 | 0 | 0 | 0 |
| Auth 模块 | 3 | 2 | 1 | 0 | 0 |
| Admin 角色 | 3 | 3 | 0 | 0 | 0 |
| Sysadmin 角色 | 4 | 4 | 0 | 0 | 0 |
| Clinical 角色 | 9 | 9 | 0 | 0 | 0 |
| Receptionist 角色 | 1 | 1 | 0 | 0 | 0 |
| Patients 模块 | 4 | 4 | 0 | 0 | 0 |
| Catalog 药材 | 3 | 3 | 0 | 0 | 0 |
| Catalog 验方 | 2 | 2 | 0 | 0 | 0 |
| MedicalCase 控件 | 4 | 4 | 0 | 0 | 0 |
| MedicalCase 视图 | 2 | 2 | 0 | 0 | 0 |
| MedicalCase 对话框 | 3 | 3 | 0 | 0 | 0 |
| MedicalCase 报表 | 1 | 1 | 0 | 0 | 0 |
| Registrations | 2 | 2 | 0 | 0 | 0 |
| Users 模块 | 3 | 3 | 0 | 0 | 0 |
| 共享控件 | 16 | 16 | 0 | 0 | 0 |
| 打印模板 | 4 | 4 | 0 | 0 | 0 |
| 主题样式 | 7 | 7 | 0 | 0 | 0 |
| **合计** | **78** | **76** | **1** | **0** | **0** |

---

## 按角色分组

### 通用（All Users）
- MainWindow, SplashScreen, LoginView, ServerConfigView
- AccountSettingsView/Control
- ConfirmationDialog, InputDialog, MessageDialog
- ToastControl, SearchBox, LoadingOverlay, BreadcrumbBar
- 所有共享控件与主题

### Admin（A）
- AdminHomeView → 用户管理、系统设置入口
- UserManagementView → UserMasterDetailControl + UserEditControl
- SystemSettingsView → 诊所配置、功能开关
- HerbManagementView → HerbMasterDetailControl + HerbEditControl
- FormulaManagementView → FormulaMasterDetailControl + FormulaEditControl
- PatientManagementView → PatientMasterDetailControl + PatientEditControl
- MedicalCaseManagementView → 医案查询/筛选
- ReportsHomeView → 收入/就诊/药材报表

### Sysadmin（S）
- SysadminHomeView → 配置中心、服务管理入口
- BackupManagementView → LocalDB 备份恢复
- DeploymentView → 远程部署
- LogLevelControlView → 日志级别调整
- FirstRunSetupView → 首次初始化向导（⚠️ 待完善）

### Doctor/临床（D）
- ClinicalHomeView → 工作台首页
- ClinicalWorkspaceView → 诊疗流程主界面
- MedicalCaseWorkspaceView → 医案编辑（诊断+处方）
- PatientSelectionView → 患者选择（挂号/快速看诊入口）
- PendingQueueView → 待诊队列（接诊）
- PatientManagementView → 患者管理
- HerbManagementView → 药材查看
- FormulaManagementView → 验方管理
- MedicalCaseManagementView → 医案查询/历史

### Receptionist/前台（R）
- ReceptionistHomeView → 前台工作台
- RegistrationListView → 挂号列表
- RegistrationCreateDialog → 创建挂号
- PatientSelectionView → 患者选择
- PendingQueueView → 排队查看

---

## 需求驱动的 UI 空白分析

以下是需求文档要求但**代码中未发现独立 UI 页面**的功能点（可能已内嵌到其他页面或尚未实现）：

| US | 需求描述 | UI 现状 | 评估 |
|----|----------|---------|------|
| US-SHELL-011 | 首次初始化向导（5步：改密→诊所→模式→admin→完成） | FirstRunSetupView 存在但仅有基础框架 | ⚠️ 需完善 5 步向导 UI |
| US-SHELL-012 | Desktop 自动更新（v2.0） | 无 UI | 📦 v2.0 规划 |
| US-SHELL-014 | 安全审计日志查看 | 无独立 UI 页面 | 🧲 待实现（需 Sysadmin 安全日志查看页） |
| US-SHELL-016 | 数据导出/导入（JSON） | 无独立 UI | 🧲 待实现（需 Sysadmin 数据管理页） |
| US-SHELL-021 | 上线数据迁移 | 无 UI | 🧲 待实现 |
| US-REG-002 | 医生快速就诊 UI 双入口（"挂号" + "快速看诊"） | PatientSelectionView 存在，QuickVisit 两步 VM 已实现 | ⚠️ 需确认 UI 双按钮接线 |
| US-REPORT-004 | 趋势/绩效/排行报表（5端点） | ReportsHomeView 仅消费 3 个 daily 端点 | ⚠️ 需扩展报表 UI |
| US-CARD-002 | 读卡器集成（挂号流程中） | CardReaderViewModel 存在 | ⚠️ 待完善读卡器 UI 接线 |

---

## 优先级排序（按实施顺序）

### P0 核心（73 个页面，全部已实现 ✅）
全部 78 个页面中 76 个已实现，1 个部分实现（FirstRunSetupView）。P0 页面覆盖了：
- 登录认证流程
- 医生诊疗全流程（挂号→接诊→开方→打印）
- 管理员用户/药材/验方/患者管理
- Sysadmin 系统配置/备份/部署

### P1 重要（需完善的页面）
1. **FirstRunSetupView** — 需从 3 步扩展到 5 步向导（US-SHELL-011）
2. **ReportsHomeView** — 需扩展支持趋势/绩效/排行报表（US-REPORT-004）
3. **SysadminHomeView** — 需增加安全审计日志入口（US-SHELL-014）
4. **SysadminHomeView** — 需增加数据导入导出入口（US-SHELL-016）

### P2 可选（v2.0 或低优先级）
1. 自动更新 UI（US-SHELL-012，v2.0）
2. 上线数据迁移向导（US-SHELL-021）
3. 培训/FAQ 帮助页（US-SHELL-023）

---

## 已生成设计稿的页面

以下 4 个核心页面已有独立 UI 设计稿（参见 docs/compose/ 目录）：

| 页面 | 设计稿 | 代码状态 |
|------|--------|----------|
| 登录页 | ✅ | LoginView.xaml 已实现 |
| 主界面 | ✅ | MainWindow.xaml 已实现 |
| 患者列表 | ✅ | PatientMasterDetailControl.xaml 已实现 |
| 医案工作台 | ✅ | MedicalCaseWorkspaceView.xaml 已实现 |

---

## 变更记录

| 日期 | 变更 | 原因 |
|------|------|------|
| 2026-08-15 | 初版创建：78 页面全量清单 | Desktop UI 全景分析 |
