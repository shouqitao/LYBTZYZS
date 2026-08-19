# Desktop 基于最新文档的实现检查报告（2026-08-19）

> 任务书：`.hermes-task-desktop-doc-check.md`（已删除）
> 依据：扁平化需求文档（docs/02-requirements/）+ 追溯矩阵 WebAPI/Desktop 分列（v1.7）+ desktop-ui-requirements.md（v1.0）
> 方法：代码扫描逐 US 验证 ViewModel/View 消费 + XAML 命令绑定核对 + UI 需求文档交叉对照
> 产出：追溯矩阵 v1.8（修正 6 处 Desktop 状态）+ 本报告

---

## 一、Desktop 状态列准确性复核（逐项验证）

### ✅ 验证正确（保持原状态）

| US | Desktop 状态 | 验证证据 |
|----|:---:|---------|
| US-AUTH-001/004/005/008/009/010/012 | ✅ | LoginViewModel/AuthenticationService/TokenRefreshHandler/AutoLogin/LogoutService 全链 |
| US-USER-001~011 | ✅ | UserMasterDetailViewModel + UserService CRUD 完整（Restore 命令在 MasterDetailViewModelBase） |
| US-PAT-001~007/013/014 | ✅ | PatientMasterDetailViewModel CRUD + PatientCardReaderIntegration（身份证查询链完整） |
| US-HERB-001~005/010/011/014 | ✅ | HerbMasterDetailViewModel CRUD + DesktopCacheManager.InvalidateHerbCaches |
| US-FORM-001~004/010~012 | ✅ | FormulaMasterDetailViewModel CRUD + FormulaImportDialog（验方导入到处方） |
| US-MC-001~007/010/011/013/014/017/019 | ✅ | MedicalCaseWorkspaceViewModel + LifecycleService（挂起/完成/取消）+ HistoryCopyDialog + AuditLogView |
| US-REG-001/003~006/008 | ✅ | RegistrationCreateDialog + RegistrationListViewModel + PendingQueueView + SignalRClient 轮询降级 |
| US-PRINT-001~004 | ✅ | PrescriptionPrintService + PrescriptionPrintHandler（回写全链） |
| US-SHELL-001/003/004/005/007/010/012/013/018/019/020 | ✅ | App.xaml.cs + ApplicationBootstrapper + AccountSettingsControl + NavigationCoordinator + ConnectionStatusViewModel + Velopack + ILocalDbBackupService + SysadminHomeView + CardReaderDiagnosticsViewModel + DeploymentView |
| US-CFG-001/002/004/006 | ✅ | ServerConfigSectionViewModel/ConfigurationCenterViewModel + FeatureToggleService + ClinicSettingsService |
| US-ERR-002/003/008 | ✅ | ClientErrorMessageMapper + DesktopExceptionHandler（各 VM ShowErrorAsync 反馈完备） |
| US-LOG-001/002/003/005 | ✅ | 双端 Serilog + bootstrap + SensitiveData 消费 + LogLevelControlView |
| US-SYS-001/002/005~008 | ✅ | ApiHealthMonitor + LogLevelControlView 消费 |
| US-CARD-001 | ✅ | ICardReaderService + PatientCardReaderIntegration |

### ⚠️ 修正为部分实现（本批修正，原标记 ✅）

| US | 原 Desktop | 修正为 | 证据 |
|----|:---:|:---:|------|
| US-USER-012 | ✅ | ⚠️ | MasterDetailCommandGroup 有批量删除/启用/禁用 UI 交互（DataGridToolbar 多选），但 **DeleteBatchAsync/EnableBatchAsync/DisableBatchAsync 循环单条调用 DeleteItemAsync/SetItemEnabledAsync**，未消费 POST /users/batch-delete 等批量端点 |
| US-PAT-008 | ✅ | ⚠️ | 同上——UI 批量交互存在但未调 POST /patients/batch-delete |
| US-PAT-009/010 | ✅ | ⚠️ | check-reference/batch-check-reference 端点 LocalWebAPI 有实现，但 **ViewModel 零消费**（grep 无调用点） |
| US-HERB-012 | ✅ | ⚠️ | 同上——UI 批量交互存在但未调批量端点 |
| US-FORM-005 | ✅ | ⚠️ | 单删 ✅（DeleteCommand）；批量删除走循环单删，未消费 batch-delete |

### ⚠️ 确认正确（上批已标，本批代码级验证）

| US | Desktop 状态 | 验证证据 |
|----|:---:|---------|
| US-PAT-011/012 | ⚠️ | PatientMasterDetailViewModel **无 Import/Export 命令**（上批已删死菜单项）——导入/导出 UI 未接线 |
| US-HERB-006/007/013 | ⚠️ | HerbMasterDetailControl.xaml 绑定 `ImportHerbsCommand`/`ExportHerbsCommand`——**命令不存在（死绑定）**，按钮点击无效 |
| US-FORM-006/013 | ⚠️ | FormulaMasterDetailViewModel 无导入导出命令；FormulaImportDialog 是「验方导入到处方」非批量导入 |
| US-FORM-007/008/009 | ⚠️ | 无 pending-validation/validate UI（仅 FormulaEditor.Validate 表单验证） |
| US-MC-008/009 | ⚠️ | history 端点 Desktop 零消费（HistoryCopyDialog 用 Query ByPatient + batch-details 替代） |
| US-MC-012 | ⚠️ | CompleteMedicalCaseAsync 无 skipWorkflowValidation/force 参数，强制关闭无 UI |
| US-MC-015/020 | ⚠️ | 单删 ✅；批量删除循环单删未消费 batch-delete 端点 |
| US-MC-016 | ⚠️ | 权限判断走本地 WorkspaceState 计算，未调 GetPermissionsAsync 端点 |
| US-MC-018 | ⚠️ | batch-details 端点 Desktop 零消费（HistoryCopyDialog 分页循环取数） |
| US-REG-002 | ⚠️ | CanCreateRegistration 限 `IsReceptionist`——Doctor 无 Source=Doctor 建号入口（第 1 步无 UI，StartVisit 第 2 步有） |
| US-REPORT-004 | ⚠️ | ReportsHomeViewModel 仅消费 3 个 daily 端点，trend/绩效端点未消费（与 UI 文档「仅 3/8 端点」一致） |
| US-SYS-003 | ⚠️ | health/details 由 ApiHealthMonitor 消费部分；详细健康检查 UI 未完整接线 |
| US-CARD-002 | ⚠️ | 实现已移除（A-31-C7），待完善（与 UI 文档一致） |
| US-ERR-007 | ⚠️ | 异常体系 Desktop 侧部分（AppException 3 实体） |

### ❌ 完全未实现（验证确认）

| US | Desktop 状态 | 证据 |
|----|:---:|------|
| US-SHELL-011 | 🧲 | FirstRunSetupView 仅基础框架（UI 文档 §1.3 明确 ⚠️ 部分实现，5 步向导未完善） |
| US-SHELL-014 | 🧲 | 安全审计日志查看 UI 缺失（UI 文档 §七.3 明确 🔴 缺失） |
| US-SHELL-016 | 🧲 | 配置导出/导入 UI 缺失 |
| US-SHELL-021/022/023 | 🧲 | 上线迁移/Go-Live/培训材料（非代码功能） |

---

## 二、功能完整性检查

### 1. 批量导入/导出 UI 接线（重点检查项 1）

| 域 | Service/Repository 层 | ViewModel/View 层 | 结论 |
|----|:---:|:---:|------|
| 患者（PAT-011/012） | ✅ 完整（BatchImportAsync/ExportTemplateAsync/ExportPatientsAsync） | ❌ 无命令（菜单已删） | ⚠️ 未接线 |
| 药材（HERB-006/007/013） | ✅ 完整 | ❌ ImportHerbsCommand/ExportHerbsCommand **死绑定** | ⚠️ 未接线（按钮无效） |
| 验方（FORM-006/013） | ✅ 完整 | ❌ 无命令 | ⚠️ 未接线 |
| 用户（USER-012 导出） | ✅ 完整 | ❌ UserMasterDetailControl ExportCommand **死绑定** | ⚠️ 未接线 |

**结论**：批量导入/导出 API 双端完整，但 **Desktop 全部未接线**——UI 需求文档 §七.4「数据导入导出 🔴 缺失（需新建 Sysadmin 数据管理页）」确认。**需创建独立任务书**。

### 2. 错误处理 UI 反馈（重点检查项 2）✅

- 各 VM 统一用 `MasterDetailServices.Dialog.ShowErrorAsync` + `ClientErrorMessageMapper.GetSafeOperationFailureMessage`（安全消息映射，不泄露内部异常）
- Toast 反馈（ToastService）覆盖操作成功/失败
- 表单验证错误（Validate）提示明确
- **结论**：US-ERR-002/003 Desktop ✅ 正确

### 3. 权限矩阵 UI 限制（重点检查项 3）✅

- `MasterDetailViewModelBase.IsAdmin`（SessionManager.HasPermission）驱动 CanRestore/CanDelete 等 CanExecute
- 角色感知菜单（ApplicationBootstrapper.LoadModulesForRoleAsync + NavigationManager）
- 命令级限制（如 CanCreateRegistration 限 Receptionist、CanStartVisit 限 Doctor）
- **结论**：权限 UI 限制与 04-permissions.md 一致 ✅

### 4. 双模式切换 UI（重点检查项 4）✅

- ConnectionStatusViewModel 模式切换按钮 + CheckRemoteAvailableAsync 探测
- ConnectionModeService 守卫（未完成医案阻断 + 回退）
- 状态栏模式标识（StatusBarManager）
- **结论**：US-SHELL-007 Desktop ✅ 正确

---

## 三、文档一致性

| 文档 | 与代码一致性 | 说明 |
|------|:---:|------|
| desktop-ui-requirements.md | ✅ | 页面清单与代码一致；「待完善/缺失」表（FirstRunSetup ⚠️/Reports 3/8 端点/审计日志缺失/数据导入导出缺失）与本次代码验证完全吻合 |
| 追溯矩阵 Desktop 列（v1.7） | ⚠️ | 6 处不准确（USER-012/PAT-008/PAT-009/PAT-010/HERB-012/FORM-005 标 ✅ 实为 ⚠️）→ **已修正为 v1.8** |
| 扁平化需求文档 | ✅ | US 状态与代码一致（US-HERB-005 等此前批次已校准） |

---

## 四、发现的代码问题（需独立任务书）

| # | 问题 | 位置 | 建议 |
|---|------|------|------|
| D-1 | **XAML 死绑定**：`ImportHerbsCommand`/`ExportHerbsCommand` 绑定不存在的命令（按钮无效） | HerbMasterDetailControl.xaml:52/60 | 删除死绑定或实现命令接线批量导入导出 UI |
| D-2 | **XAML 死绑定**：`ExportCommand` 绑定不存在的命令 | UserMasterDetailControl.xaml:49 | 同上 |
| D-3 | **批量端点未消费**：MasterDetailCommandGroup 批量操作循环单删，未用 batch-delete/batch-enable/batch-disable 端点 | MasterDetailViewModelBase.cs:251-266 | 评估是否切换为批量端点（需确认业务语义：循环单删返回逐项结果 vs 批量端点一次性） |
| D-4 | **批量导入/导出 UI 整体缺失**：患者/药材/验方/用户导入导出无 UI 入口（UI 文档 §七.4 已登记） | 各 MasterDetail VM | 新建 Sysadmin/管理端数据管理页（对接已就绪的 Service/API 层） |

> 按任务书「如果有代码需要修复，创建独立任务书」——以上 4 项建议作为 backlog 登记，其中 D-1/D-2 死绑定可直接修（外科手术），D-3/D-4 需产品确认 UI 意图（是否补批量导入导出页）。

---

## 五、结论

**追溯矩阵 Desktop 状态列复核完成**：v1.7 → v1.8，修正 6 处（USER-012/PAT-008/PAT-009/PAT-010/HERB-012/FORM-005 由 ✅ 改为 ⚠️），其余 148 处状态经代码验证确认准确。

**核心发现**：
1. Desktop 批量操作是「UI 交互级批量」（多选+循环单条），**未消费服务端批量端点**（batch-delete/enable/disable）
2. 批量导入/导出 API 完整但 Desktop **全部未接线**（含 2 处 XAML 死绑定）
3. 错误处理/权限 UI/双模式切换三类横切能力**实现完备** ✅

---

*检查完成时间: 2026-08-19*
*方法: 代码扫描（ViewModel 命令/Service 消费/XAML 绑定）+ UI 需求文档交叉对照 + 追溯矩阵逐 US 验证*
