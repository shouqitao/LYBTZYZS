# 需求追溯矩阵 (Traceability Matrix)

> 版本: v1.0 | 日期: 2026-06-28 | 状态: ✅ 建立（v1.0 范围冻结基线）
>
> **用途**：建立「需求 → 设计 → 实现」的双向追溯基础设施。本矩阵是 v1.0 范围冻结、变更影响分析、缺口补全追踪的权威索引。
> **覆盖**：全部 138 个 User Story（US）+ 13 个 ADR + 5 个业务 Flow + 54 个访谈问题点。
> **数据来源**：各模块 US 正文的「优先级 / 状态 / 实现参考 / 双模式端点」+ `2026-06-28-prd-code-reconciliation.md`（D1-D10 决策）+ `2026-06-28-scenario-functional-map.md`（✅⚠️🔴）+ `2026-06-28-user-expectation-interview.md`。

## 图例

| 列 | 含义 |
|------|------|
| US ID | `US-{域}-{NNN}` 编号 |
| 优先级 | Must / Should / Could |
| 关联 ADR | 影响该 US 的架构决策（见 `03-architecture/decisions/`） |
| 关联 Flow | `11-business-flows.md` 中覆盖该 US 的 Flow |
| 关联 API | 远程/本地双模式端点（Controller 或路径） |
| 实现文件 | US「实现参考」中的关键代码文件 |
| 访谈问题点 | `user-expectation-interview.md` 中对应的问题编号 |
| 状态 | ✅已实现 / 🧲v1.0待实现 / 🔴代码待对齐 / ⚠️部分实现 / v2.0规划 |

**状态判定依据**：D1-D10 决策的「补回项」= 🧲v1.0待实现；D7 权限错配 / 端点缺失 = 🔴代码待对齐；scenario-map ⚠️ = ⚠️部分实现；Sync 整模块 / SHELL-012 = v2.0。

---

## 一、认证与会话（US-AUTH × 13）

| US ID | 优先级 | 关联 ADR | 关联 Flow | 关联 API | 实现文件 | 访谈问题点 | 状态 |
|-------|:---:|------|------|------|------|------|------|
| US-AUTH-001 | Must | ADR-0004/0008 | Flow 1/2/3/4 | POST /auth/login | AuthController.cs:44 | D19 | ✅ 已实现 |
| US-AUTH-002 | Must | ADR-0008 | — | ITokenManagementService | AuthController.cs:44 | A11 | ⚠️ 部分实现（双轨锁定，D8） |
| US-AUTH-003 | Must | ADR-0008 | — | [EnableRateLimiting("Login")] | AuthController.cs:41 | — | ✅ 已实现 |
| US-AUTH-004 | Must | ADR-0008 | — | POST /auth/refresh | AuthController.cs:128 | — | ✅ 已实现 |
| US-AUTH-005 | Must | ADR-0004 | — | GET /auth/validate + JWT 中间件 | AuthController.cs:151 | — | ✅ 已实现 |
| US-AUTH-006 | Must | ADR-0008 | — | ITokenRevocationService | AuthController.cs:128 | — | 🧲 v1.0 待实现（D3：族旋转补回，重放 v2.0） |
| US-AUTH-007 | Should | ADR-0008 | — | ISecurityAuditService | AuthService.cs | A12 | 🧲 v1.0 待实现（D3） |
| US-AUTH-008 | Must | ADR-0008 | Flow 1 | POST /auth/logout | AuthController.cs:104 | — | ✅ 已实现 |
| US-AUTH-009 | Must | ADR-0002/0009 | Flow 3 | POST /auth/auto-login | AuthController.cs:79 | D19 | ✅ 已实现 |
| US-AUTH-010 | Should | ADR-0008 | — | ITokenManagementService | AuthController.cs:79 | — | ✅ 已实现 |
| US-AUTH-011 | Must | ADR-0005 | — | AuthService.cs 保留名校验 | AuthService.cs | — | ✅ 已实现 |
| US-AUTH-012 | Must | ADR-0002/0009/0010 | Flow 3 | LocalWebAPI/AuthController.cs | LocalWebAPI/Controllers/AuthController.cs:19 | D19 | ✅ 已实现 |
| US-AUTH-013 | Must | ADR-0010 | Flow 3 | 本地限流中间件 | LocalWebAPI/Controllers/AuthController.cs:19 | — | 🧲 v1.0 待实现（D3） |

## 二、用户管理（US-USER × 12）

| US ID | 优先级 | 关联 ADR | 关联 Flow | 关联 API | 实现文件 | 访谈问题点 | 状态 |
|-------|:---:|------|------|------|------|------|------|
| US-USER-001 | Must | ADR-0005 | — | GET /users | UsersController | A8 | ⚠️ 部分实现（TotalCount 内存筛选，D8） |
| US-USER-002 | Must | ADR-0005 | — | GET /users/{id} | UsersController | — | ⚠️ 部分实现（CreatedAt MinValue，D8） |
| US-USER-003 | Must | ADR-0004 | — | GET /users/current | UsersController | — | ✅ 已实现 |
| US-USER-004 | Must | ADR-0005 | — | POST /users | UsersController | A8 | ✅ 已实现 |
| US-USER-005 | Must | ADR-0005 | — | PUT /users/{id} | UsersController | — | ✅ 已实现 |
| US-USER-006 | Must | ADR-0005 | — | DELETE /users/{id} | UsersController | A9 | ✅ 已实现 |
| US-USER-007 | Must | ADR-0005 | — | POST /users/{id}/reset-password | UsersController | — | ✅ 已实现 |
| US-USER-008 | Must | ADR-0004 | Flow 4 | PUT /users/{id}/profile | UsersController | — | ✅ 已实现 |
| US-USER-009 | Must | ADR-0005 | — | PUT /users/{id}/change-password | UsersController | — | ✅ 已实现 |
| US-USER-010 | Must | ADR-0005 | — | POST /users/{id}/toggle-status | UsersController | A9 | ✅ 已实现 |
| US-USER-011 | Should | ADR-0005 | — | POST /users/{id}/restore | UsersController | A10 | 🧲 v1.0 待实现（D4：基础设施已就绪） |
| US-USER-012 | Should | ADR-0005 | — | POST /users/batch-delete 等 | UsersController | — | ⚠️ 部分实现（仅 batch-delete） |

## 三、患者管理（US-PAT × 13）

| US ID | 优先级 | 关联 ADR | 关联 Flow | 关联 API | 实现文件 | 访谈问题点 | 状态 |
|-------|:---:|------|------|------|------|------|------|
| US-PAT-001 | Must | ADR-0010 | Flow 2 | GET /patients | PatientsController.cs:38 | R7 | ✅ 已实现 |
| US-PAT-002 | Must | ADR-0010 | — | GET /patients/{id} | PatientsController.cs:65 | — | ✅ 已实现 |
| US-PAT-003 | Must | ADR-0010 | Flow 1 | POST /patients | PatientsController.cs:89 | R3 | ✅ 已实现 |
| US-PAT-004 | Must | ADR-0010 | — | PUT /patients/{id} | PatientsController.cs:114 | — | ✅ 已实现 |
| US-PAT-005 | Must | ADR-0001 | — | DELETE /patients/{id} | PatientsController.cs:146 | — | 🔴 代码待对齐（D5：单删引用检查缺失） |
| US-PAT-006 | Must | — | — | POST /patients/{id}/toggle-status | PatientsController.cs:175 | — | ✅ 已实现 |
| US-PAT-007 | Should | — | — | POST /patients/{id}/restore | PatientsController.cs:197 | A10 | ✅ 已实现 |
| US-PAT-008 | Should | ADR-0001 | — | POST /patients/batch-delete | PatientsController.cs:223 | — | ✅ 已实现 |
| US-PAT-009 | Must | ADR-0001 | — | GET /patients/{id}/check-reference | PatientsController.cs:248 | — | ✅ 已实现 |
| US-PAT-010 | Should | ADR-0001 | — | POST /patients/batch-check-reference | PatientsController.cs:267 | — | ✅ 已实现 |
| US-PAT-011 | Should | ADR-0010 | — | GET /patients/import-template | PatientsController.cs:296 | — | ✅ 已实现 |
| US-PAT-012 | Should | ADR-0010 | — | GET /patients/export | PatientsController.cs:313 | — | ✅ 已实现 |
| US-PAT-013 | Must | ADR-0008 | — | [SensitiveData] 序列化管道 | PatientsController.cs | X3.1 | ✅ 已实现 |

## 四、药材管理（US-HERB × 13）

| US ID | 优先级 | 关联 ADR | 关联 Flow | 关联 API | 实现文件 | 访谈问题点 | 状态 |
|-------|:---:|------|------|------|------|------|------|
| US-HERB-001 | Must | ADR-0007 | Flow 2 | GET /herbs | HerbsController.cs:37 | R12/A4/D10 | ✅ 已实现 |
| US-HERB-002 | Must | ADR-0007 | — | GET /herbs/{id} | HerbsController.cs:57 | — | ✅ 已实现 |
| US-HERB-003 | Must | ADR-0010 | — | POST /herbs | HerbsController.cs:75 | — | ✅ 已实现 |
| US-HERB-004 | Must | ADR-0001 | — | PUT /herbs/{id} | HerbsController.cs:96 | A2/D13 | ✅ 已实现 |
| US-HERB-005 | Must | ADR-0001 | — | DELETE /herbs/{id} | HerbsController.cs:121 | — | 🔴 代码待对齐（D5：引用检查缺失） |
| US-HERB-006 | Must | ADR-0010 | Flow 5(none) | POST /herbs/batch-import | HerbsController.cs:148 | A1 | 🧲 v1.0 待实现（D6：Excel 服务不存在） |
| US-HERB-007 | Should | ADR-0010 | — | GET /herbs/export-all | HerbsController.cs:186 | — | ✅ 已实现 |
| US-HERB-008 | Should | ADR-0001 | — | GET /herbs/{id}/check-reference | HerbsController.cs:206 | — | 🔴 代码待对齐（端点不存在） |
| US-HERB-009 | Should | ADR-0001 | — | POST /herbs/batch-check-reference | HerbsController.cs:226 | — | 🔴 代码待对齐（端点不存在） |
| US-HERB-010 | Must | — | — | POST /herbs/{id}/toggle-status | HerbsController.cs:264 | A3 | ✅ 已实现 |
| US-HERB-011 | Should | — | — | POST /herbs/{id}/restore | HerbsController.cs:288 | A10 | 🔴 代码待对齐（D4：完全未实现） |
| US-HERB-012 | Should | ADR-0001 | — | POST /herbs/batch-enable 等 | HerbsController.cs:314 | — | ✅ 已实现 |
| US-HERB-013 | Should | ADR-0010 | — | GET /herbs/export + import-template | HerbsController.cs:386 | — | 🔴 代码待对齐（Controller 无路由） |

## 五、验方管理（US-FORM × 13）

| US ID | 优先级 | 关联 ADR | 关联 Flow | 关联 API | 实现文件 | 访谈问题点 | 状态 |
|-------|:---:|------|------|------|------|------|------|
| US-FORM-001 | Must | ADR-0007 | — | GET /Formulas | FormulasController.cs:23 | D11 | ✅ 已实现 |
| US-FORM-002 | Must | ADR-0007 | — | GET /Formulas/{id} | FormulasController.cs:23 | — | ⚠️ 部分实现（无所有权检查，D8） |
| US-FORM-003 | Must | ADR-0007 | — | POST /Formulas | FormulasController.cs:23 | D11 | ✅ 已实现 |
| US-FORM-004 | Must | ADR-0007 | — | PUT /Formulas/{id} | FormulasController.cs:23 | — | ✅ 已实现 |
| US-FORM-005 | Must | ADR-0007 | — | DELETE /Formulas/{id} | FormulasController.cs:23 | — | ✅ 已实现 |
| US-FORM-006 | Must | ADR-0010 | — | POST /Formulas/batch-import | FormulasController.cs:23 | — | ✅ 已实现 |
| US-FORM-007 | Must | ADR-0007 | — | GET /Formulas/pending-validation | IFormulaService.cs:11 | D11 | ✅ 已实现 |
| US-FORM-008 | Must | ADR-0007 | — | POST /Formulas/{fid}/herbs/{hid}/validate | IFormulaService.cs:11 | — | ✅ 已实现 |
| US-FORM-009 | Must | ADR-0007 | — | IFormulaService.ValidateFormulaHerbAsync | IFormulaService.cs:11 | — | ✅ 已实现 |
| US-FORM-010 | Must | ADR-0007 | — | IFormulaService.UpdateAsync | IFormulaService.cs:11 | — | ✅ 已实现 |
| US-FORM-011 | Must | ADR-0007 | — | POST /Formulas/{id}/toggle-status | FormulasController.cs:23 | — | ⚠️ 部分实现（batch 端点缺失） |
| US-FORM-012 | Should | ADR-0007 | — | POST /Formulas/{id}/restore | FormulasController.cs:23 | A10 | 🔴 代码待对齐（D4：完全未实现） |
| US-FORM-013 | Should | ADR-0010 | — | GET /Formulas/export + import-template | FormulasController.cs:23 | — | 🔴 代码待对齐（端点缺失） |

## 六、医案管理（US-MC × 19，核心聚合根）

| US ID | 优先级 | 关联 ADR | 关联 Flow | 关联 API | 实现文件 | 访谈问题点 | 状态 |
|-------|:---:|------|------|------|------|------|------|
| US-MC-001 | Must | ADR-0001 | Flow 1/2 | POST /medicalcases | MedicalCasesController.cs:26 | D2 | 🔴 代码待对齐（D7：DoctorOrAdmin 应 Doctor-only） |
| US-MC-002 | Must | ADR-0001 | Flow 1/2/5 | PUT /medicalcases/{id} | MedicalCasesController.cs:26 | D7/D8 | ✅ 已实现 |
| US-MC-003 | Must | ADR-0001 | Flow 1 | PUT /medicalcases/{id}/prescription-flag | MedicalCasesController.cs:26 | D12 | ✅ 已实现 |
| US-MC-004 | Must | ADR-0001 | Flow 2 | GET /medicalcases/{id} | MedicalCasesController.cs:26 | D3 | ✅ 已实现 |
| US-MC-005 | Must | ADR-0001 | — | GET /medicalcases | MedicalCasesController.cs:26 | X2.3 | ✅ 已实现 |
| US-MC-006 | Must | ADR-0001 | — | GET /medicalcases/query?type= | MedicalCasesController.cs:26 | — | ✅ 已实现 |
| US-MC-007 | Must | ADR-0001 | — | GET /medicalcases/search | MedicalCasesController.cs:26 | — | ✅ 已实现 |
| US-MC-008 | Should | ADR-0001 | Flow 2 | GET /medicalcases/{pid}/consultations | MedicalCasesController.cs:26 | D3/D4/D9 | 🧲 v1.0 待实现（D9：历史聚合缺失） |
| US-MC-009 | Should | ADR-0001 | Flow 2 | GET /medicalcases/{pid}/prescriptions | MedicalCasesController.cs:26 | D4/D5 | 🧲 v1.0 待实现（D9） |
| US-MC-010 | Must | ADR-0001 | — | PUT /medicalcases/{id}/suspend | MedicalCaseProcessingController.cs:25 | — | ✅ 已实现 |
| US-MC-011 | Must | ADR-0001 | Flow 1 | PUT /medicalcases/{id}/close | MedicalCaseProcessingController.cs:25 | — | ✅ 已实现 |
| US-MC-012 | Should | ADR-0001 | — | PUT /medicalcases/{id}/close?force=true | MedicalCaseProcessingController.cs:25 | — | ✅ 已实现 |
| US-MC-013 | Must | ADR-0001 | — | PUT /medicalcases/{id}/suspend | MedicalCaseProcessingController.cs:25 | — | ✅ 已实现 |
| US-MC-014 | Must | ADR-0001 | Flow 5 | PUT /medicalcases/{id}/cancel | MedicalCaseProcessingController.cs:25 | D16 | ✅ 已实现 |
| US-MC-015 | Must | ADR-0001 | — | DELETE /medicalcases/{id} + batch-delete | MedicalCasesController.cs:26 | — | ✅ 已实现 |
| US-MC-016 | Should | ADR-0001 | — | GET /medicalcases/{id}/permissions | MedicalCaseAuditController.cs:23 | X2.3 | 🔴 代码待对齐（端点不存在） |
| US-MC-017 | Must | ADR-0001 | — | GET /medicalcases/{id}/audit-logs | MedicalCaseAuditController.cs:23 | A11 | 🧲 v1.0 待实现（D1：实体已删，Audit Service 缺） |
| US-MC-018 | Should | ADR-0001 | — | POST /medicalcases/batch-details | MedicalCasesController.cs:26 | — | 🔴 代码待对齐（Service 有，Controller 无端点） |
| US-MC-019 | Should | ADR-0001 | Flow 2 | 复用 US-MC-009 处方历史 | MedicalCasesController.cs:26 | D6 | 🧲 v1.0 待实现（D6） |

## 七、挂号管理（US-REG × 8）

| US ID | 优先级 | 关联 ADR | 关联 Flow | 关联 API | 实现文件 | 访谈问题点 | 状态 |
|-------|:---:|------|------|------|------|------|------|
| US-REG-001 | Must | ADR-0001 | Flow 1 | POST /Registrations | RegistrationsController.cs:24 | R3/R6/R11 | 🔴 代码待对齐（D7：DoctorOrAdmin 阻断 Receptionist） |
| US-REG-002 | Must | ADR-0001 | — | POST /Registrations/quick-visit | RegistrationsController.cs:24 | R13 | 🧲 v1.0 待激活（急诊通道[远程]+本地常规；当前死代码，见 R10 spec S6） |
| US-REG-003 | Must | ADR-0010 | — | GET /Registrations/{id} | RegistrationsController.cs:24 | — | ✅ 已实现 |
| US-REG-004 | Must | ADR-0010 | Flow 1 | GET /Registrations/queue | RegistrationsController.cs:24 | R8/R9 | ✅ 已实现 |
| US-REG-005 | Must | ADR-0001 | Flow 1 | PUT /Registrations/{id}/start | RegistrationsController.cs:24 | — | 🔴 代码待对齐（D8：StartVisit 待原子创建医案[MedicalCase(Active)+Registration(InProgress)+返回 MedicalCaseId]，见 R10 spec S5） |
| US-REG-006 | Must | ADR-0010 | Flow 1 | PUT /Registrations/{id}/cancel | RegistrationsController.cs:24 | R11 | 🔴 代码待对齐（D7：权限阻断 Receptionist） |
| US-REG-007 | Must | ADR-0001 | Flow 1 | MedicalCaseService 内部触发 | RegistrationsController.cs:24 | — | ✅ 已实现 |
| US-REG-008 | Must | ADR-0013 | Flow 1 | SignalR Hub | SignalR Hub（待专项 spec） | R10/X2.1 | 🧲 v1.0 待实现（SignalR 推送） |

## 八、处方打印（US-PRINT × 4）

| US ID | 优先级 | 关联 ADR | 关联 Flow | 关联 API | 实现文件 | 访谈问题点 | 状态 |
|-------|:---:|------|------|------|------|------|------|
| US-PRINT-001 | Must | ADR-0001 | Flow 1/5 | PrescriptionPrintService | PrescriptionPrintService.cs:23 | D14/D15/D17 | ✅ 已实现 |
| US-PRINT-002 | Must | — | — | PrescriptionPrintService | PrescriptionPrintService.cs | — | ✅ 已实现 |
| US-PRINT-003 | Should | — | — | PrescriptionPdfExporter | PrescriptionPdfExporter | — | ✅ 已实现 |
| US-PRINT-004 | Must | ADR-0001 | Flow 1/5 | PUT /print-completed + POST /print-log | MedicalCasePrintController.cs:44 | D16 | 🧲 v1.0 待实现（D2：实体已删，回写缺失） |

## 九、平台基础设施 — Shell（US-SHELL × 14，v1.0 有效）

| US ID | 优先级 | 关联 ADR | 关联 Flow | 关联 API | 实现文件 | 访谈问题点 | 状态 |
|-------|:---:|------|------|------|------|------|------|
| US-SHELL-001 | Must | ADR-0006/0007 | Flow 4 | App.xaml.cs 启动管线 | App.xaml.cs:41 | S1 | ✅ 已实现 |
| US-SHELL-003 | Must | ADR-0006/0007 | Flow 4 | ApplicationBootstrapper | ApplicationBootstrapper.cs:35 | — | 🔴 代码待对齐（C1：LoginCoordinator 旁路待删） |
| US-SHELL-004 | Could | ADR-0007 | — | AccountSettingsControl | AccountSettingsControl | — | ✅ 已实现 |
| US-SHELL-005 | Must | ADR-0006/0007 | — | NavigationCoordinator | NavigationCoordinator | — | ✅ 已实现 |
| US-SHELL-007 | Must | ADR-0002/0009 | Flow 3 | SwitchingApiClient + ModeSwitchValidator | IConnectionModeProvider.SwitchModeAsync | D18/D19/S5/X1.1 | ✅ 已实现 |
| US-SHELL-010 | Must | ADR-0006 | — | Velopack 打包 | velopack NuGet | S1 | 🧲 v1.0 待实现 |
| US-SHELL-011 | Must | ADR-0006 | — | FirstRunSetupViewModel 扩展 | FirstRunSetupViewModel | S1/S2 | 🧲 v1.0 待实现 |
| US-SHELL-012 | Should | ADR-0006 | — | UpdateManager | UpdateManager.CheckForUpdatesAsync | — | v2.0 规划 |
| US-SHELL-013 | Should | — | — | ILocalDbBackupService + 恢复 UI | ILocalDbBackupService | S4/X3.2 | ⚠️ 部分实现（备份有，恢复 UI 缺） |
| US-SHELL-014 | Should | ADR-0008 | — | SecurityAuditLog + Service | SysadminHomeView | A12 | 🧲 v1.0 待实现（D3） |
| US-SHELL-016 | Could | — | — | 导出/导入 JSON 按钮 | SysadminHomeView | X3.2 | 🧲 v1.0 待实现 |
| US-SHELL-017 | Must | ADR-0005/0008 | — | SystemAdminOptions + DefaultPasswordService | SystemAdminOptions.cs | — | ✅ 已实现 |
| US-SHELL-018 | Must | ADR-0006 | — | SysadminHomeView | SysadminHomeView | S3 | 🧲 v1.0 待实现 |
| US-SHELL-019 | Should | — | — | ICardReaderDiagnostics | ICardReader/ICardReaderFactory | S3 | 🧲 v1.0 待实现 |

> US-SHELL-015 已撤销（并入 US-SHELL-013），不计入总数。

## 十、平台基础设施 — Configuration（US-CFG × 4）

| US ID | 优先级 | 关联 ADR | 关联 Flow | 关联 API | 实现文件 | 访谈问题点 | 状态 |
|-------|:---:|------|------|------|------|------|------|
| US-CFG-001 | Must | ADR-0005 | — | GET /configuration | ConfigurationController.cs:15 | S3 | ✅ 已实现 |
| US-CFG-002 | Must | ADR-0005 | — | GET /configuration/{section} | ConfigurationController.cs:15 | S3 | ✅ 已实现 |
| US-CFG-003 | Should | — | — | ProductionConfigurationValidator | ProductionConfigurationValidator | — | ✅ 已实现 |
| US-CFG-004 | Should | — | — | FeatureToggleOptions + ConfigurationOptionsMonitor | PrismConfigurationExtensions.cs:80 | — | ✅ 已实现 |

## 十一、平台基础设施 — Error Handling（US-ERR × 8）

| US ID | 优先级 | 关联 ADR | 关联 Flow | 关联 API | 实现文件 | 访谈问题点 | 状态 |
|-------|:---:|------|------|------|------|------|------|
| US-ERR-001 | Must | — | — | BusinessExceptionHandler + DesktopExceptionHandler | LYBT.Shared.ExceptionHandling | — | ✅ 已实现 |
| US-ERR-002 | Must | — | — | ClientErrorMessageMapper | ClientErrorMessageMapper | — | ✅ 已实现 |
| US-ERR-003 | Should | — | — | DesktopExceptionHandler | DesktopExceptionHandler | — | ✅ 已实现 |
| US-ERR-004 | Should | ADR-0004 | — | AsyncLocalCorrelationIdProvider | CorrelationIdEnricher | S3 | ✅ 已实现 |
| US-ERR-005 | Must | — | — | Business/SystemExceptionHandler | BusinessExceptionHandler | — | ✅ 已实现 |
| US-ERR-006 | Should | — | — | ValidationException | BusinessExceptionHandler | — | ✅ 已实现 |
| US-ERR-007 | Should | — | — | AppException 体系 | LYBT.Shared.ExceptionHandling | — | ✅ 已实现 |
| US-ERR-008 | Should | — | — | ErrorSeverity/ErrorCategory | DesktopExceptionHandler | — | ✅ 已实现 |

## 十二、平台基础设施 — Logging & Audit（US-LOG × 7）

| US ID | 优先级 | 关联 ADR | 关联 Flow | 关联 API | 实现文件 | 访谈问题点 | 状态 |
|-------|:---:|------|------|------|------|------|------|
| US-LOG-001 | Should | — | — | Serilog 配置 | Program.cs + SystemLog | S3 | ✅ 已实现 |
| US-LOG-002 | Should | — | — | bootstrap logger | Program.cs + App.xaml.cs:41 | — | ✅ 已实现 |
| US-LOG-003 | Should | ADR-0008 | — | SensitiveDataMasker | SensitiveDataAttribute | — | ✅ 已实现 |
| US-LOG-004 | Should | ADR-0008 | — | SecurityAuditLog 表 | SecurityAuditService | A12 | ✅ 已实现 |
| US-LOG-005 | Could | — | — | LoggingLevelManager | LoggingLevelManager | S3 | ✅ 已实现 |
| US-LOG-006 | Should | ADR-0004 | — | ApiLoggingFilter | CorrelationIdEnricher | — | ✅ 已实现 |
| US-LOG-007 | Could | — | — | LogCleanupService | LogCleanupService | — | ✅ 已实现 |

## 十三、平台基础设施 — Health & Diagnostics（US-SYS × 9）

| US ID | 优先级 | 关联 ADR | 关联 Flow | 关联 API | 实现文件 | 访谈问题点 | 状态 |
|-------|:---:|------|------|------|------|------|------|
| US-SYS-001 | Could | — | — | GET /api/v1/health | HealthController.cs:19 | S3 | ✅ 已实现 |
| US-SYS-002 | Could | — | — | GET /api/v1/health/ping | HealthController.cs:19 | — | ✅ 已实现 |
| US-SYS-003 | Should | — | — | GET /api/v1/health/details | HealthController.cs:19 | S3 | ✅ 已实现 |
| US-SYS-004 | Should | — | — | /health/details 503 | HealthController.cs:19 | — | ✅ 已实现 |
| US-SYS-005 | Could | — | — | GET /api/v1/diagnostics/logging/status | DiagnosticsController.cs:20 | S3 | ✅ 已实现 |
| US-SYS-006 | Could | — | — | POST /diagnostics/logging/debug/enable | DiagnosticsController.cs:20 | S3 | ✅ 已实现 |
| US-SYS-007 | Could | — | — | POST /diagnostics/logging/debug/disable | DiagnosticsController.cs:20 | — | ✅ 已实现 |
| US-SYS-008 | Could | — | — | POST /diagnostics/logging/level | DiagnosticsController.cs:20 | — | ✅ 已实现 |
| US-SYS-009 | Could | — | — | LoggingLevelManager Timer | LoggingLevelManager | — | ✅ 已实现 |

## 十四、平台基础设施 — Card Reader（US-CARD × 2）

| US ID | 优先级 | 关联 ADR | 关联 Flow | 关联 API | 实现文件 | 访谈问题点 | 状态 |
|-------|:---:|------|------|------|------|------|------|
| US-CARD-001 | Should | — | Flow 1 | ICardReaderService（客户端硬件） | ICardReaderService.cs:10 | R3/R4 | ✅ 已实现 |
| US-CARD-002 | Should | ADR-0001 | Flow 1 | IPatientCardReaderIntegration.MatchPatientAsync | MatchPatientAsync | R5 | ✅ 已实现 |

---

## 统计汇总

| 域 | US 数 | ✅已实现 | 🧲v1.0待实现 | 🔴代码待对齐 | ⚠️部分实现 | v2.0 |
|------|:---:|:---:|:---:|:---:|:---:|:---:|
| AUTH | 13 | 9 | 3 | 0 | 1 | 0 |
| USER | 12 | 9 | 1 | 0 | 2 | 0 |
| PAT | 13 | 12 | 0 | 1 | 0 | 0 |
| HERB | 13 | 7 | 1 | 5 | 0 | 0 |
| FORM | 13 | 9 | 0 | 2 | 2 | 0 |
| MC | 19 | 12 | 4 | 3 | 0 | 0 |
| REG | 8 | 3 | 2 | 3 | 0 | 0 |
| PRINT | 4 | 3 | 1 | 0 | 0 | 0 |
| Shell | 14 | 5 | 6 | 1 | 1 | 1 |
| CFG | 4 | 4 | 0 | 0 | 0 | 0 |
| ERR | 8 | 8 | 0 | 0 | 0 | 0 |
| LOG | 7 | 7 | 0 | 0 | 0 | 0 |
| SYS | 9 | 9 | 0 | 0 | 0 | 0 |
| CARD | 2 | 2 | 0 | 0 | 0 | 0 |
| **合计** | **139** | **99** | **18** | **15** | **6** | **1** |

> 注：本矩阵列出 **139 行** = 138 个 v1.0 有效 US + 1 个 v2.0 US（US-SHELL-012 自动更新，列出以保完整）。README 的「138」仅计 v1.0 有效 US。
> 🧲v1.0待实现 18 项 = D1-D10 决策补回项 + SignalR/初始化/配置中心等已设计待开发项 + US-REG-002 QuickVisit 待激活；🔴代码待对齐 15 项 = D7 权限错配 + D8 P0 Bug + 端点暴露缺失；⚠️部分实现 6 项；v2.0 1 项 = SHELL-012（Sync 整模块不列入 US 总数）。

## 反向追溯说明

- **ADR 反链**：见 `03-architecture/decisions/0001-0013` 各文件末尾「## 关联 US」段。
- **Flow 覆盖**：见 `03-architecture/11-business-flows.md` 每个 Flow 标题下「**覆盖 US**」行。
- **访谈标注**：见 `compose/specs/2026-06-28-user-expectation-interview.md` 每个问题点后「→ US-XXX」标注。
- **变更影响分析**：修改某 US 时，沿「关联 ADR / Flow / 访谈点」三向反查受影响范围；修改某 ADR 时，沿其「关联 US」段反查。

## 变更记录

| 日期 | 变更 | 原因 |
|------|------|------|
| 2026-06-28 | US-REG-002 ⚠️→🧲（QuickVisit 待激活：急诊+本地常规）；US-REG-005 D8 注细化；REG/合计统计同步 | R10 spec S8 文档更新 |
| 2026-06-28 | 建立追溯矩阵（138 US × 8 列），整合 D1-D10 决策与 scenario-map 状态 | plan Task 1：追溯基础设施 |
