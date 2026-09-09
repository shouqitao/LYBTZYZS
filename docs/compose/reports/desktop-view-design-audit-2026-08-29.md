# Desktop 层 View 按设计完成度审计

> **日期**: 2026-08-29  
> **范围**: 存在性 + 已知缺口（不做深挖样式合规）  
> **设计 SSOT**: `docs/compose/specs/desktop-view-inventory.md` + `docs/07-ui-ux/desktop-ui-requirements.md` + `docs/07-ui-ux/second-level-design-checklist.md` + `docs/02-requirements/` 相关 US  
> **代码基准**: 当前 master（HEAD 前工作树，只读审计）  
> **对照方法**: XAML 存在 → VM 对应 → `RegisterForNavigation`/`RegisterDialog` 注册 → 导航入口/角色 Home 引用

---

## 结论摘要

**一级导航视图总体按设计落地**：约 **25 个活跃页面/对话框** 已注册并可导航；核心诊疗链路（登录 → 选患者/待诊 → 医案工作台 → 打印）与管理 CRUD 链路齐全。

**未完全按设计完成的有 4 类**：

| 类别 | 数量 | 代表项 |
|------|:---:|--------|
| 需求设计有、代码缺失的独立页 | 2 | `SecurityAuditLogView`（US-SHELL-014）、`ConfigExportImportView`（US-SHELL-016） |
| 设计清单有、代码无 | 1 | `SessionTimeoutWarningDialog`（US-AUTH-005） |
| 有 XAML 但未接线（死页） | 1 | `PendingQueueView`（功能已内嵌 `PatientSelectionView`） |
| 文档状态滞后于代码 | 若干 | 13c 视图表、二级清单 A2/A3、Registration 路径名 |

**角色首页实际接线**（与部分文档表述不一致，以代码为准）：

| 角色 | `HomeViewName` | 备注 |
|------|----------------|------|
| Admin | `AdminHomeView` | ✅ |
| Doctor | `ClinicalWorkspaceView` | ⚠️ **非** `ClinicalHomeView`（后者仅 `RoleRegistry.DefaultHomeView` 保底） |
| Receptionist | `ReceptionistHomeView` | ✅ |
| SuperAdmin | `SysadminHomeView` | ✅ |

---

## 一、完整对照矩阵

### 1.1 Shell / Auth（导航或对话框）

| 设计项 | XAML | 注册 | 状态 | 说明 |
|--------|:---:|:---:|:---:|------|
| MainWindow | ✅ | — | ✅ | 组合根窗口 |
| LoginView | ✅ | AuthModule `RegisterForNavigation` | ✅ | 活跃 |
| ServerConfigView | ✅ | AuthModule `RegisterDialog` | ✅ | 活跃对话框 |
| FirstRunSetupView | ✅ | AuthModule `RegisterDialog` | ⚠️ | 已接线；设计侧 `W-01 InitializationWizardView`（登录后强制 5 步全屏）未另建，需求文档亦标「部分实现」 |
| AccountSettingsView | ✅ | App `RegisterForNavigation` | ✅ | 活跃 |
| SessionTimeoutWarningDialog | ❌ | — | 🔴 | **缺失**；全仓无 SessionTimeout 对话框/VM（仅有 `SystemConstants.SessionTimeoutMinutes`） |

### 1.2 Admin / Sysadmin

| 设计项 | XAML | 注册 | 状态 | 说明 |
|--------|:---:|:---:|:---:|------|
| AdminHomeView | ✅ | AdminModule | ✅ | Admin Home |
| UserManagementView | ✅ | AdminModule | ✅ | |
| SystemSettingsView | ✅ | AdminModule | ✅ | 13c 标「仅读取」属功能深度问题，页本身存在 |
| SysadminHomeView | ✅ | SysadminModule | ✅ | 配置中心 + 模式感知 Tab |
| BackupManagementView | ✅ | SysadminModule | ✅ | **13c §4.2 视图表漏列**（任务记录里有） |
| DeploymentView | ✅ | SysadminModule | ✅ | |
| LogLevelControlView | ✅ | SysadminModule | ✅ | |
| SecurityAuditLogView（SY-06） | ❌ | — | 🔴 | US-SHELL-014 🧲；写入侧 `SecurityAuditService` 有，**查看页+查询 API 均无**；勿与医案 `AuditLogView` 混淆 |
| ConfigExportImportView（SY-07） | ❌ | — | 🔴 | US-SHELL-016 🧲；业务导入导出已分散在患者/药材/验方页，**配置包导出/导入**无独立页 |
| CardReaderDiagnostics 独立 View（SY-05） | ⚠️ 面板 | — | ✅ 设计内简化 | VM+面板嵌在 SysadminHome（SHELL-019 已落地），非独立导航页——**符合当前设计取向** |
| ServerConfig 面板（SY-08） | ⚠️ 子 VM | — | ✅ 设计内简化 | `ServerConfigSectionViewModel` 嵌 SysadminHome（SHELL-018），非独立页 |

### 1.3 临床 / 前台

| 设计项 | XAML | 注册 | 状态 | 说明 |
|--------|:---:|:---:|:---:|------|
| ClinicalWorkspaceView | ✅ | ClinicalModule | ✅ | **Doctor 真实 Home** |
| ClinicalHomeView | ✅ | ClinicalModule | ⚠️ 半死 | 仅默认 Home fallback，无角色绑定 |
| PatientSelectionView | ✅ | ClinicalModule | ✅ | 活跃；内嵌待诊队列 + 读卡 |
| MedicalCaseWorkspaceView | ✅ | ClinicalModule | ✅ | 核心工作台，活跃 |
| MedicalCaseManagementView | ✅ | ClinicalModule | ✅ | wrapper → MasterDetail |
| MedicalCaseMasterDetailView | ✅ | MedicalCaseModule | ✅ | |
| PatientManagementView | ✅ | ClinicalModule | ✅ | |
| HerbManagementView | ✅ | ClinicalModule | ✅ | |
| FormulaManagementView | ✅ | ClinicalModule | ✅ | |
| RegistrationListView | ✅ | RegistrationModule | ✅ | 13c 路径误写为 `Registration/`（单数），实际 `Registrations/` |
| ReceptionistHomeView | ✅ | ClinicalModule | ✅ | Receptionist Home |
| ReportsHomeView | ✅ | ReportsModule | ✅ | 13c/需求标「待完善」属功能深度 |
| AuditLogView（医案） | ✅ | MedicalCaseModule | ✅ | US-MC-017；**不是**安全审计 |
| PendingQueueView | ✅ | **未注册** | ❌ 死页 | 未 `RegisterForNavigation`；队列 UI 已内嵌 `PatientSelectionView.xaml`（`PendingQueue.Queue` 等绑定）。**建议删除 XAML 或改为内嵌控件复用** |

### 1.4 业务对话框 / 共享控件（抽样）

| 设计项 | XAML | 状态 |
|--------|:---:|:---:|
| RegistrationCreateDialog | ✅ | ✅ |
| FormulaImportDialog | ✅ | ✅ |
| HistoryCopyDialog | ✅ | ✅ |
| UnsavedChangesDialog | ✅ | ✅ |
| Confirmation / Input / Message Dialog | ✅ | ✅ |
| MasterDetailLayout / DataGridToolbar / SearchBox / UnifiedPaginationBar / HerbList 等 | ✅ | ✅（`desktop-ui-requirements` §6 已标齐） |

---

## 二、缺口明细（按优先级）

### P0 — 需求有、页面无（影响合规/完整度）

1. **SecurityAuditLogView（US-SHELL-014）**  
   - 设计：`desktop-view-inventory` SY-06、二级清单 D1、`11a-shell.md`  
   - 代码：无 View；`ISecurityAuditRepository` 仅写入无查询；SysadminHome 卡片为规划入口  
   - 建议：先补查询 API + View，再接 Sysadmin 导航（勿复用医案 AuditLogView）

2. **ConfigExportImportView（US-SHELL-016）**  
   - 设计：SY-07、二级清单 D2  
   - 代码：无独立页；业务 JSON 导入导出已在各 MasterDetail  
   - 建议：若落地，范围限定 **appsettings + clinic-settings 打包**，避免与业务数据导出概念混用（middle-zone-audit-sysadmin 已指出设计稿混淆）

### P1 — 设计清单有、代码无

3. **SessionTimeoutWarningDialog（US-AUTH-005）**  
   - 设计：inventory AD-01（30 秒倒计时）  
   - 代码：常量有、对话框无  
   - 建议：会话超时是否已有其他提醒机制待产品确认；若无则属真实缺口

### P2 — 死页 / 文档滞后

4. **PendingQueueView 死代码**  
   - XAML 完整但零导航注册、零引用（除自身）  
   - 功能由 `PatientSelectionViewModel.PendingQueue` 吸收  
   - 建议：删除 `PendingQueueView.xaml(.cs)`，或抽取为可复用 UserControl 并删除独立 View 命名

5. **ClinicalHomeView 半死**  
   - 已注册，但 Doctor Home 已改 `ClinicalWorkspaceView`  
   - 仅 `RoleRegistry.DefaultHomeView` 使用  
   - 建议：文档写明「fallback only」；产品决定是否删除或恢复为 Doctor 入口

6. **13c §4 视图清单滞后**  
   - 缺 `ClinicalWorkspaceView`（真实 Doctor Home）、`BackupManagementView`  
   - Registration 路径写错（Registration → Registrations）  
   - 未区分「安全审计 vs 医案审计」  
   - 建议：按本报告矩阵回写 13c（属文档一致性红线）

7. **`second-level-design-checklist` A2/A3**  
   - A2 PendingQueue「疑似死代码」→ 本审计确认 **死代码**  
   - A3 ClinicalHome「半死」→ 确认 **半死（fallback only）**  
   - 可关闭确认状态

### 设计侧缺口（非代码，供 UI 稿 backlog）

二级清单已列，本审计不重复出稿任务，仅确认仍有效：

- PatientSelectionView、ReceptionistHomeView、AuditLogView 等 **无 .pen 设计稿**（有代码）
- ClinicalWorkspaceView（Doctor 主页）为关键设计缺口（design-coverage-check 2026-08-23 结论）

---

## 三、按设计「已完成」的判定标准（本审计采用）

同时满足：

1. XAML 存在且与设计页面职责一致  
2. 对应 ViewModel 存在  
3. `RegisterForNavigation` 或 `RegisterDialog` 注册（或明确为内嵌面板）  
4. 存在至少一条运行时导航/角色 Home/入口命令  

不满足 3 或 4 → 死页/半死；不满足 1 → 代码缺失。

---

## 四、建议后续动作（不自动执行）

| 优先级 | 动作 | 类型 |
|--------|------|------|
| P1 | 回写 13c 视图表 + 二级清单 A2/A3 状态 | 文档 |
| P1 | 删除或收编 `PendingQueueView` 死 XAML | 代码清理 |
| P2 | 产品确认 SessionTimeout 提醒是否要做 | 需求 |
| P2 | US-SHELL-014/016 进需求深化（查询 API + View） | 需求→设计→代码 |
| P3 | ClinicalWorkspaceView / PatientSelection 等补设计稿 | UI 设计 |

---

## 五、证据索引

| 证据 | 位置 |
|------|------|
| 导航注册全集 | `src/Client/Desktop/**/*Module.cs` + `Shell/App.xaml.cs` |
| ViewNames | `Core/LYBT.Desktop.Infrastructure/Constants/ViewNames.cs` |
| 角色 Home | `Roles/Definitions/*RoleDefinition.cs` |
| 懒加载映射 | `Infrastructure/Navigation/ModuleLazyLoader.cs` |
| 设计清单 | `docs/compose/specs/desktop-view-inventory.md` |
| UI 需求 | `docs/07-ui-ux/desktop-ui-requirements.md` §七 |
| 二级清单 | `docs/07-ui-ux/second-level-design-checklist.md` |
| 安全审计/配置导出 US | `docs/02-requirements/11a-shell.md` US-SHELL-014/016 |
| 当前态视图表 | `docs/03-architecture/13c-current-status.md` §四 |
| 历史覆盖核查 | `docs/compose/reports/design-coverage-check-2026-08-23.md` |

---

*审计只读，未改代码。文档滞后项建议另开任务同步。*
