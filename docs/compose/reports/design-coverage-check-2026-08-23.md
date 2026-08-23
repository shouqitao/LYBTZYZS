# Desktop 设计稿覆盖度深度核查报告

**日期**: 2026-08-23  
**审计人**: Hermes Agent (subagent)  
**触发**: 19 个 .pen 设计稿 vs 详细设计文档 17 页清单对比，发现疑似 5 页缺失  
**结论**: 原始清单偏差 — 真正需要补设计稿的是 **4 页**（不是 5 页），且原始清单**遗漏了最重要的一页**（ClinicalWorkspaceView / 医生主页）

---

## 1. 核心数据

### 1.1 代码中实际存在的 Views（29 个 XAML）

| 模块 | 视图 | 说明 |
|------|------|------|
| Auth | FirstRunSetupView | 首次运行向导 |
| Auth | LoginView | 登录 |
| Auth | ServerConfigView | 服务器配置 |
| MedicalCase/Reports | ReportsHomeView | 报表首页 |
| MedicalCase | AuditLogView | 审计日志 |
| MedicalCase | MedicalCaseMasterDetailView | 医案管理（MasterDetail wrapper） |
| Registrations | RegistrationListView | 挂号列表 |
| Admin/Sysadmin | BackupManagementView | 备份管理 |
| Admin/Sysadmin | DeploymentView | 部署 |
| Admin/Sysadmin | LogLevelControlView | 日志级别 |
| Admin/Sysadmin | SysadminHomeView | 运维首页 |
| Admin | AdminHomeView | 管理员首页 |
| Admin | SystemSettingsView | 系统设置 |
| Admin | UserManagementView | 用户管理 |
| Clinical/Receptionist | ReceptionistHomeView | **前台首页** |
| Clinical | ClinicalHomeView | **临床首页**（半死） |
| Clinical | ClinicalWorkspaceView | **临床工作台**（医生主页） |
| Clinical | FormulaManagementView | 验方管理 |
| Clinical | HerbManagementView | 药材管理 |
| Clinical | MedicalCaseManagementView | 医案管理（wrapper） |
| Clinical | MedicalCaseWorkspaceView | 医案工作台 |
| Clinical | PatientManagementView | 患者管理 |
| Clinical | PatientSelectionView | **患者选择** |
| Clinical | PendingQueueView | **待诊队列**（死代码） |
| Shell/Dialogs | ConfirmationDialog | 通用确认对话框 |
| Shell/Dialogs | InputDialog | 通用输入对话框 |
| Shell/Dialogs | MessageDialog | 通用消息对话框 |
| Shell | AccountSettingsView | 账户设置 |
| Shell | MainWindow | 主窗口 |

### 1.2 已有设计稿（20 个 .pen，含并行会话新增的 1 个）

| # | 设计稿 | 帧名 | 对应 View |
|---|--------|------|-----------|
| 1 | login.pen | 登录界面 | LoginView ✅ |
| 2 | first-run.pen | 初始化向导窗口 | FirstRunSetupView ✅ |
| 3 | server-config.pen | Server Config Page | ServerConfigView ✅ |
| 4 | main-window.pen | NavItem/统计卡片/快捷操作 组件 + 主界面·今日工作台 | MainWindow shell ✅ |
| 5 | patient-list.pen | 患者管理列表 | PatientManagementView ✅ |
| 6 | herb-management.pen | 中药药材管理 | HerbManagementView ✅ |
| 7 | formula-management.pen | 验方管理页面 | FormulaManagementView ✅ |
| 8 | medical-case.pen | 医案工作台 | MedicalCaseWorkspaceView ✅ |
| 9 | medical-case-management.pen | 医案管理页面 | MedicalCaseMasterDetailView / MedicalCaseManagementView ✅ |
| 10 | registration.pen | 挂号管理页面 | RegistrationListView ✅ |
| 11 | reports.pen | 报表首页 | ReportsHomeView ✅ |
| 12 | admin-home.pen | 管理员工作台 | AdminHomeView ✅ |
| 13 | system-settings.pen | System Settings Window | SystemSettingsView ✅ |
| 14 | user-management.pen | 用户管理-列表页/新建用户对话框 | UserManagementView ✅ |
| 15 | sysadmin-home.pen | 系统配置中心主窗口 | SysadminHomeView ✅ |
| 16 | backup-management.pen | Backup Management Page | BackupManagementView ✅ |
| 17 | deployment.pen | Deployment Page | DeploymentView ✅ |
| 18 | log-level.pen | Log Level Control Window | LogLevelControlView ✅ |
| 19 | account-settings.pen | Account Settings Screen | AccountSettingsView ✅ |
| 20 | dialog-registration-create.pen | Dialog Overlay | RegistrationCreateDialog ✅（并行会话新增，未提交） |

---

## 2. 完整覆盖矩阵

| View | 设计稿 | 注册导航 | 导航引用方 | 代码存活状态 | 需求 US | 结论 |
|------|--------|---------|-----------|------------|---------|------|
| LoginView | login.pen ✅ | — | Auth flow | ✅ 活跃 | — | 已覆盖 |
| FirstRunSetupView | first-run.pen ✅ | — | Auth flow | ✅ 活跃 | — | 已覆盖 |
| ServerConfigView | server-config.pen ✅ | — | Auth flow | ✅ 活跃 | — | 已覆盖 |
| MainWindow | main-window.pen ✅ | — | Shell | ✅ 活跃 | — | 已覆盖 |
| AccountSettingsView | account-settings.pen ✅ | AccountSettingsModule | MenuManager | ✅ 活跃 | — | 已覆盖 |
| AdminHomeView | admin-home.pen ✅ | AdminModule | AdminRoleDef | ✅ 活跃 | — | 已覆盖 |
| SystemSettingsView | system-settings.pen ✅ | AdminModule | AdminHomeViewModel | ✅ 活跃 | — | 已覆盖 |
| UserManagementView | user-management.pen ✅ | UsersModule | AdminHome/SysadminHome | ✅ 活跃 | — | 已覆盖 |
| SysadminHomeView | sysadmin-home.pen ✅ | SysadminModule | SuperAdminRoleDef | ✅ 活跃 | — | 已覆盖 |
| BackupManagementView | backup-management.pen ✅ | SysadminModule | SysadminHomeViewModel | ✅ 活跃 | — | 已覆盖 |
| DeploymentView | deployment.pen ✅ | SysadminModule | SysadminHomeViewModel | ✅ 活跃 | — | 已覆盖 |
| LogLevelControlView | log-level.pen ✅ | SysadminModule | SysadminHomeViewModel | ✅ 活跃 | — | 已覆盖 |
| PatientManagementView | patient-list.pen ✅ | PatientsModule | Admin/Clinical/Receptionist HomeVM | ✅ 活跃 | — | 已覆盖 |
| HerbManagementView | herb-management.pen ✅ | CatalogModule | ClinicalHomeViewModel | ✅ 活跃 | — | 已覆盖 |
| FormulaManagementView | formula-management.pen ✅ | CatalogModule | ClinicalHomeViewModel | ✅ 活跃 | — | 已覆盖 |
| MedicalCaseManagementView | medical-case-management.pen ✅ | MedicalCaseModule | Admin/Clinical HomeVM | ✅ 活跃 | — | 已覆盖 |
| MedicalCaseWorkspaceView | medical-case.pen ✅ | MedicalCaseModule | ClinicalHome/WorkspaceNaviHandler | ✅ 活跃 | — | 已覆盖 |
| RegistrationListView | registration.pen ✅ | RegistrationModule | ReceptionistHome/ClinicalHome | ✅ 活跃 | US-REG-001~006 | 已覆盖 |
| ReportsHomeView | reports.pen ✅ | ReportsModule | Admin/Clinical HomeVM | ✅ 活跃 | — | 已覆盖 |
| **PatientSelectionView** | ❌ **无设计稿** | ClinicalModule ✅ | WorkspaceNavigationHandler（工作台返回） | ✅ 活跃 | US-REG-004/005 | **⚠️ 需要设计** |
| **ClinicalWorkspaceView** | ❌ **无设计稿** | ClinicalModule ✅ | DoctorRoleDef.HomeViewName（**医生主页**） | ✅ 活跃 | 临床工作台（US-MC 核心流程入口） | **⚠️ 需要设计（最关键缺口）** |
| **ReceptionistHomeView** | ❌ **无设计稿** | ClinicalModule ✅ | ReceptionistRoleDef.HomeViewName（**前台主页**） | ✅ 活跃 | 叫号/挂号入口 | **⚠️ 需要设计** |
| **AuditLogView** | ❌ **无设计稿** | MedicalCaseModule ✅ | AdminHome/ClinicalHome/MedicalCaseWorkspaceVM | ✅ 活跃 | US-MC-017 (Must) | **⚠️ 需要设计** |
| ClinicalHomeView | ❌ 无设计稿 | ClinicalModule ✅ | **仅 RoleRegistry.DefaultHomeView fallback**（null-user 保底） | ⚠️ 半死 | — | **不需要设计**（仅 fallback，无角色使用） |
| PendingQueueView | ❌ 无设计稿 | **❌ 未注册导航** | 无（PendingQueueViewModel 已内嵌到 PatientSelectionViewModel） | ❌ 死代码 | US-REG-004 队列 UI 已嵌入 PatientSelectionView | **不需要设计**（功能已被吸收） |
| ConfirmationDialog | — | Dialogs（注册对话框） | 多处 | ✅ 活跃 | — | 通用对话框，无需独立设计 |
| InputDialog | — | Dialogs（注册对话框） | 多处 | ✅ 活跃 | — | 通用对话框，无需独立设计 |
| MessageDialog | — | Dialogs（注册对话框） | 多处 | ✅ 活跃 | — | 通用对话框，无需独立设计 |
| RegistrationCreateDialog | dialog-registration-create.pen ✅ | Dialogs | RegistrationListViewModel | ✅ 活跃 | — | 已覆盖（并行会话新增） |

---

## 3. 原始「缺失 5 页」逐项核查

### 3.1 patient-selection（PatientSelectionView）→ ✅ 需要设计

- **代码状态**: XAML 存在，ViewModel 完整（CardReader + PendingQueue 子面板 + 患者搜索），注册在 ClinicalModule
- **导航入口**: WorkspaceNavigationHandler.ExecuteBackAsync — 从 MedicalCaseWorkspace 返回时导航到此处
- **功能**: 患者搜索选择 + 读身份证（CardReader 硬件集成未实现但 UI 已有）+ 待诊队列面板（PendingQueue 子 VM）
- **需求**: US-REG-004（查看排队，Must ✅）、US-REG-005（开始就诊，Must ✅）
- **结论**: **需要独立设计稿**。详细设计 §4.6 有规格但无 .pen

### 3.2 pending-queue（PendingQueueView）→ ❌ 不需要设计

- **代码状态**: XAML 存在，但 **ClinicalModule 未注册**（RegisterForNavigation 中无此条目）
- **导航引用**: **零** — ViewNames.cs 中无 PendingQueue 常量，无任何代码 NavigateTo 到此视图
- **功能现状**: PendingQueueViewModel 存活但作为 **子 VM 内嵌** 在 PatientSelectionViewModel 中（line 131: `PendingQueue = new PendingQueueViewModel(...)`），UI 嵌入在 PatientSelectionView.xaml 左侧面板（待诊队列 + 刷新/接诊按钮）
- **结论**: **不需要设计稿**（死代码视图），其功能已被 PatientSelectionView 吸收。建议后续清理孤儿 XAML

### 3.3 clinical-home（ClinicalHomeView）→ ❌ 不需要设计

- **代码状态**: XAML 存在，注册在 ClinicalModule（line 30），ViewModel 有 StartMedicalCase/NavigateToPatientManagement 等命令
- **导航引用**: **仅 fallback** — RoleRegistry.DefaultHomeView = ViewNames.ClinicalHome（未注册角色的保底），NavigationCoordinator.role==null fallback
- **角色归属**: **无角色使用** — DoctorHome = ClinicalWorkspace, AdminHome = AdminHome, ReceptionistHome = ReceptionistHome, SuperAdminHome = SysadminHome
- **结论**: **不需要设计稿**（半死状态，仅 null-user fallback）。建议产品决策：删除或保留为安全网

### 3.4 receptionist-home（ReceptionistHomeView）→ ✅ 需要设计

- **代码状态**: XAML 存在，注册在 ClinicalModule（line 43），ViewModel 完整（NavigateToRegistrationQueue、CreateNewPatient、LoadTodayStats）
- **角色归属**: ReceptionistRoleDefinition.HomeViewName = ViewNames.ReceptionistHome（**活跃角色主页**）
- **需求**: 详细设计 §3.4（叫号横幅 + 挂号/患者快捷入口），REG-BR-006（v1.0 前台端可见候诊队列）
- **结论**: **需要独立设计稿**。详细设计 §4.13 有简短规格但无 .pen

### 3.5 audit-log（AuditLogView）→ ✅ 需要设计

- **代码状态**: XAML 存在，注册在 MedicalCaseModule（line 60），ViewModel 完整（LoadLogsCommand + 10 列 DataGrid）
- **导航入口**: 3 处 — AdminHomeViewModel.NavigateToAuditLog、ClinicalHomeViewModel.NavigateToAuditLog、MedicalCaseWorkspaceViewModel:522（带医案 ID 参数）
- **需求**: US-MC-017（查询审计日志，Must ✅ 已实现）
- **结论**: **需要独立设计稿**。详细设计 §4.15 有规格但无 .pen

---

## 4. 原始清单遗漏：ClinicalWorkspaceView（医生主页）

### 原始分析未识别到此缺口

原始 5 页缺失清单为：patient-selection、pending-queue、clinical-home、receptionist-home、audit-log。  
**遗漏了 ClinicalWorkspaceView** — 这是 **DoctorRoleDefinition.HomeViewName**（`ViewNames.ClinicalWorkspace`），即医生登录后的**第一个界面**。

### 证据链

```
DoctorRoleDefinition.HomeViewName => ViewNames.ClinicalWorkspace
    ↓
ClinicalModule.cs:33 RegisterForNavigation<ClinicalWorkspaceView>
    ↓
ClinicalHomeViewModel.StartMedicalCase() → NavigateTo(ViewNames.ClinicalWorkspace)
    ↓
ClinicalWorkspaceView.xaml: 左列 PatientSelectionControl（嵌入患者列表）+ 右列看诊工作区
```

### 设计稿对比

| 对比项 | ClinicalWorkspaceView（无设计稿） | MedicalCaseWorkspaceView（有 medical-case.pen） |
|--------|----------------------------------|----------------------------------------------|
| 功能 | 一体化临床工作台：左侧患者选择 + 右侧快捷操作 | 医案工作台：患者信息卡 + 辨证/处方编辑 |
| 角色 | 医生主页（登录首个界面） | 医生看诊工作界面 |
| XAML 位置 | Roles/Clinical/Views | Roles/Clinical/Views |
| ViewNames 常量 | ClinicalWorkspace | MedicalCaseWorkspace |

### 结论

**需要独立设计稿**。main-window.pen 中的「主界面 · 今日工作台」仅为 shell 框架层 Mock（含统计卡片 + 快捷操作组件），不包含 ClinicalWorkspaceView 的核心布局（左患者列表 + 右工作区一体化）。

---

## 5. 需求文档交叉验证

### US-REG-004（分页查询挂号 + 查看排队）— Must ✅

- **需求**: 医生查看个人队列（Waiting + DoctorId=当前医生），Receptionist 查看全部队列
- **UI 实现**: 队列嵌入在 PatientSelectionView 左侧面板（PendingQueueViewModel 子面板），不是独立页面
- **设计影响**: PatientSelection 设计稿必须包含队列面板区域（刷新 + 接诊按钮 + 超时分级着色）

### US-REG-005（开始就诊）— Must ✅

- **需求**: 医生选中 Waiting 挂号 → StartVisit → MedicalCase 自动创建
- **UI 入口**: PatientSelectionView 内的接诊按钮 / ClinicalWorkspace 的双击患者
- **设计影响**: PatientSelection 设计稿必须包含接诊流程的视觉指引

### US-MC-017（查询审计日志）— Must ✅

- **需求**: 管理员查看医案变更历史（含字段级 diff）
- **UI 入口**: AuditLogView（从 AdminHome 导航、MedicalCaseWorkspace 参数化导航）
- **设计影响**: AuditLogView 需要独立设计稿

### REG-BR-006（v1.0 候诊队列仅前台端与医生端）

- **ReceptionistHomeView** 需要展示叫号横幅 + 待诊计数 → 独立设计稿
- **ClinicalWorkspaceView** / **PatientSelectionView** 医生端队列 → 已嵌入 PatientSelectionView

---

## 6. 最终结论

### 需要补设计稿的页面（4 个）

| 优先级 | 页面 | 文件名建议 | 理由 |
|--------|------|-----------|------|
| **P0** | ClinicalWorkspaceView | `clinical-workspace.pen` | 医生主页（DoctorRoleDefinition.HomeViewName），登录第一个界面，核心流程入口 |
| **P1** | ReceptionistHomeView | `receptionist-home.pen` | 前台首页（ReceptionistRoleDefinition.HomeViewName），叫号横幅 + 快捷入口 |
| **P1** | PatientSelectionView | `patient-selection.pen` | 患者选择 + 待诊队列面板 + 读卡器，US-REG-004/005 核心界面 |
| **P2** | AuditLogView | `audit-log.pen` | 审计日志查询，US-MC-017 Must，三处导航可达 |

### 不需要设计的页面及原因

| 页面 | 原因 |
|------|------|
| PendingQueueView | **死代码**：未注册导航，零引用。PendingQueueViewModel 已内嵌到 PatientSelectionViewModel。建议删除孤儿 XAML |
| ClinicalHomeView | **半死状态**：已注册但无角色使用，仅 RoleRegistry.DefaultHomeView fallback（null-user 保底）。建议产品决策后删除或保留 |
| ConfirmationDialog / InputDialog / MessageDialog | 通用 Shell 对话框，无需独立页面设计（已在 main-window.pen 组件库覆盖） |

### 对详细设计文档 §4.13 的修正建议

当前 §4.13 将 ClinicalWorkspaceView、ClinicalHomeView、ReceptionistHomeView 合并为一个短条目（「快捷入口 新建患者/挂号/待诊」），但实际代码中三者功能完全不同：
- **ClinicalWorkspaceView**: 一体化临床工作台（左患者列表 + 右工作区）← 需要独立规格
- **ReceptionistHomeView**: 前台首页（叫号横幅 + 统计 + 快捷入口）← 需要独立规格
- **ClinicalHomeView**: 临床首页（半死 fallback，可简化为一段注释）

建议拆分为三个独立 §4 小节，与代码实际结构对齐。
