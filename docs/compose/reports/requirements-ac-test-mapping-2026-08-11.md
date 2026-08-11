# 需求 AC → 测试 映射报告（AC-TEST）

> 日期：2026-08-11 | 状态：只读调研（未改代码）
> 方法：T1 测试审查教训（测试自洽但不对照需求——4 个 P0 bug 逃过测试）→ 本次建立「需求验收条件 → 测试守护」映射表
> 数据源：13-traceability-matrix.md（147 US）、tests/ 全测试类清单、T4 修复 commit 记录

---

## 一、背景：T1 教训与 P0 逃逸复盘

T1 测试审查发现测试体系**自洽但不对照需求**——测试验证「代码做了什么」而非「需求要求什么」。R1 需求覆盖矩阵发现的 4 个 P0 bug 全部逃过既有测试：

| # | P0 bug | 发现途径 | 修复 | **测试守护现状（本次核查）** |
|---|--------|---------|------|------------------------------|
| 1 | 远程 auth 路由错误（`/api/v1/users/api/v1/auth/*` 双重前缀） | R1 矩阵 + 代码扫描 | T4 #1（IdentityController `/` 开头绝对路径） | 🔴 **无测试守护**——无 IdentityController 路由单测（AuthTests 为 Desktop 集成，localhost 依赖；远程路由无断言） |
| 2 | 登录/刷新未签发 RefreshToken（access 即刷新凭据链缺失） | R1 矩阵 + 代码扫描 | T4 #2（Login/Refresh/ValidateAutoLogin 签发） | ⚠️ 部分——`TokenRefreshHandlerIntegrationTests` 守护 Desktop 侧刷新流程（用户禁用不刷新等）；**服务端签发侧无单测** |
| 3 | 本地 /refresh 无验签（LocalJwtConfig.GetSigningKey 未接） | R1 矩阵 + 代码扫描 | T4 #3（本地 ValidateToken 验签） | ✅ `LocalTokenValidatorTests`（Desktop Unit——守护本地令牌验证） |
| 4 | 导入导出 12 端点（NPOI ExcelExportHelper 共享） | R1 矩阵 + 代码扫描 | T4 #4（12 端点 + ExcelExportHelper） | 🔴 **无测试守护**——无 ExcelImportHelper/导出端点单测（全仓 grep 0 命中） |

**教训落地**：测试守护缺失 ≠ 功能缺失。本次映射表的「守护状态」列如实标注，缺口进入建议清单。

---

## 二、全域映射总览（15 域 × 147 US）

| 域 | US 数 | 测试守护强度 | 主要守护测试类 |
|----|-------|-------------|---------------|
| AUTH | 13 | ✅ 强 | JwtServiceTests / AuthTests / AuthNegativeTests / AuthenticationStateMachineTests / LoginViewModelTests / LocalTokenValidatorTests / TokenRefreshHandlerIntegrationTests / AuthControllerTests |
| USER | 12 | ✅ 强 | UsersControllerTests / UserTests / UserNegativeTests / UserMasterDetailViewModelTests / RevokeAllUserTokensCommandHandlerTests / RolePermissionBoundaryTests |
| PAT | 14 | ✅ 强 | PatientsControllerTests / PatientTests / PatientNegativeTests / PatientMasterDetailViewModelTests / PatientModelTests / DataIntegrityTests |
| HERB | 13 | ✅ 强 | HerbsControllerTests / HerbTests / HerbNegativeTests / HerbMasterDetailViewModelTests / HerbRepositoryTests / HerbFormulaWorkflowTests |
| FORM | 14 | ✅ 强 | FormulasControllerTests / FormulaTests / FormulaNegativeTests / FormulaMasterDetailViewModelTests / FormulaModelTests / HerbFormulaWorkflowTests |
| MC | 20 | ✅ 强 | MedicalCasesControllerTests / MedicalCaseTests / MedicalCaseNegativeTests / MedicalCaseBusinessRulesTests / MedicalCaseModelTests / MedicalCaseMasterDetailViewModelTests / MedicalCaseWorkspaceViewModelTests / WorkspaceStateTests / Consultation/Prescription 系列 |
| REG | 8 | ✅ 强 | RegistrationsControllerTests / RegistrationTests / RegistrationNegativeTests / RegistrationQueueLogicTests / RegistrationStatusTransitionTests / RegistrationSourceTests / PatientVisitWorkflowTests |
| PRINT | 4 | ✅ 中 | PrescriptionPrintHandlerTests |
| SHELL | 13 | ⚠️ 中 | StartupPipelineTests / StartupStepsTests / LoggingLevelManagerTests / BreadcrumbBarTests / WorkflowStepIndicatorTests / Integration Workflows |
| CFG | 6 | ✅ 强 | SystemConfigurationServiceTests / JsonFileConfigurationStoreTests / ConfigurationCenterViewModelTests / ValidateOnStartTests / ServerConfigurationExtensionsTests / ApiClientOptionsTests |
| ERR | 8 | ✅ 强 | AppExceptionTests / BusinessExceptionTests / SystemExceptionHandlerTests / ErrorCodeTests / DesktopExceptionHandlerTests / ErrorTraceCodeTests / NotificationTypeMappingTests |
| LOG | 7 | ✅ 强 | SensitiveDataMaskerTests / LoggingLevelManagerTests / SensitiveDataJsonConverterTests |
| SYS | 9 | ✅ 强 | SystemAdminOptionsTests / ProductionConfigurationValidatorTests / DatabaseInitializationServiceTests / StartupStepsTests |
| CARD | 2 | ✅ 强 | CardReaderPureTests / CardReaderOptionsConfigurationTests / CardReaderDataFillTests / CardReaderDiagnosticsServiceTests |
| REPORT | 4 | ✅ 中 | ReportServiceTests / ReportRepositoryTests |
| 横切安全 | — | ✅ 强 | SecurityAuditServiceTests / SensitiveDataJsonConverterTests / PermissionBoundaryTests / RolePermissionBoundaryTests / AntiMockRuleTests / ArchTests / LocalWebApiPatternTests |

**汇总**：✅ 有测试守护 12 域 / ⚠️ 部分 1 域（SHELL）/ 🔴 无 0 域（域级）。**但域级守护 ≠ AC 级守护**——域内个别 AC 可能无对应测试（见 §四 缺口）。

---

## 三、关键 US → 测试逐项映射（P0/P1 + Must 优先）

### AUTH（13 US）
| US | AC 要点 | 守护测试 | 状态 |
|----|--------|---------|------|
| AUTH-001 登录 | 成功/失败/禁用/锁定 | AuthTests / LoginViewModelTests / AuthNegativeTests | ✅ |
| AUTH-002 本地锁定 | 5 次/15 分钟锁定 | AuthNegativeTests（本地登录失败路径） | ✅ |
| AUTH-003 限流 | 远程/本地登录限流 | AuthNegativeTests | ✅ |
| AUTH-005 自动登录 | 本地免密自动登录 | AuthenticationStateMachineTests | ✅ |
| AUTH-006 会话撤销 | 改密/重置撤销全部会话 | RevokeAllUserTokensCommandHandlerTests | ✅ |
| AUTH-007 安全审计 | 登录/配置操作审计 | SecurityAuditServiceTests | ✅ |
| AUTH-008 登出 | 远程/本地登出 | LogoutServiceTests / AuthTests | ✅ |
| AUTH-010 refresh | 令牌刷新链 | TokenRefreshHandlerIntegrationTests（Desktop 侧）/ JwtServiceTests | ⚠️ 服务端签发侧无单测 |
| AUTH-012 验签 | JWT 验签（远程+本地） | JwtServiceTests / LocalTokenValidatorTests | ✅ |
| AUTH-013 本地限流 | 本地登录限流 | AuthNegativeTests | ✅ |

### MC（20 US）——核心聚合根
| US | AC 要点 | 守护测试 | 状态 |
|----|--------|---------|------|
| MC-001 创建医案 | 患者+医生+状态 Active | MedicalCaseBusinessRulesTests / MedicalCasesControllerTests / MedicalCaseMasterDetailViewModelTests | ✅ |
| MC-002 辨证编辑 | Consultation 字段校验 | ConsultationEditorPureTests / ConsultationEditorViewModelTests | ✅ |
| MC-003 处方编辑 | Prescription 行/金额 | PrescriptionEditorPureTests / PrescriptionEditorViewModelTests / PrescriptionItemTests | ✅ |
| MC-004 收方（创建后转收费） | 状态机 | MedicalCaseBusinessRulesTests / WorkspaceStateTests | ✅ |
| MC-006 当前医案查询 | 列表/详情 | MedicalCasesControllerTests / MedicalCaseWorkspaceViewModelTests | ✅ |
| MC-007 详情加载 | 完整详情 | MedicalCasesControllerTests / MedicalCaseMasterDetailViewModelTests | ✅ |
| MC-008/009 历史聚合 | 跨医案辨证/处方历史 | 🔴 无（B1 新端点无测试） | **🔴 缺口** |
| MC-010 状态流转 | Active→Completed 守卫 | MedicalCaseBusinessRulesTests / MedicalCaseNegativeTests | ✅ |
| MC-012 挂起/恢复 | Suspend/Resume | MedicalCaseBusinessRulesTests / MedicalCaseNegativeTests | ✅ |
| MC-014 价格计算 | 折扣/总额 | PrescriptionItemTests / ConsultationEditorPureTests | ✅ |
| MC-015 收费 | 收费完成 | WorkflowIntegrationTests / DataIntegrityTests | ✅ |
| MC-018 批量详情 | batch-details 端点 | 🔴 无（B1 新端点无测试） | **🔴 缺口** |
| MC-019 删除 | 软删/权限 | MedicalCaseNegativeTests / MedicalCasesControllerTests | ✅ |
| MC-020 批量删除 | 仅 Completed | 🔴 无（R3-补 US 无测试） | **🔴 缺口** |

### REG（8 US）
| US | AC 要点 | 守护测试 | 状态 |
|----|--------|---------|------|
| REG-001 挂号 | 前台创建 Waiting | RegistrationTests / RegistrationSourceTests / RegistrationMasterDetailViewModelTests | ✅ |
| REG-002 QuickVisit | 医生直接接诊 | 🔴 无（B2 新 UI 无测试——服务端 QuickVisitCommandHandler 已有但 UI 链无） | **🔴 缺口** |
| REG-003 接诊 | Waiting→InProgress | RegistrationTests / RegistrationStatusTransitionTests / PatientVisitWorkflowTests | ✅ |
| REG-004 取消 | 仅 Waiting | RegistrationNegativeTests / RegistrationTests | ✅ |
| REG-006 队列 | 医生待诊队列 | RegistrationQueueLogicTests / PendingQueueViewModelTests | ✅ |
| REG-007 当天过滤 | 队列当天 | RegistrationQueueLogicTests | ✅ |

### 其余域缺口摘录（完整缺口见 §四）
- **HERB-006 Excel 解析**（B2 新增 import-excel）：🔴 无测试
- **FORM-014 克隆**（R3-补 US）：🔴 无测试
- **CFG-005/006**：✅ SystemConfigurationServiceTests + ConfigurationCenterViewModelTests 守护
- **REPORT-004 趋势端点**（R3-补）：⚠️ ReportServiceTests 守护报表核心，趋势端点无专门断言
- **SHELL-013 备份恢复**（T7）：⚠️ 无单测（LocalDbBackupService 有 3 文件但测试缺——硬件/DB 依赖）

---

## 四、缺口分析（AC 无测试守护清单）

### 4.1 P0 级缺口（修复后仍未守护——4 项中的 2 项）
| 缺口 | 风险 | 建议测试 |
|------|------|---------|
| 远程 auth 路由（P0 #1 修复无守护） | 路由回归——双重前缀 bug 可复发 | IdentityController 端点路由单测（ApiVersion + 绝对路径断言） |
| 导入导出（P0 #4 修复无守护） | Excel 解析/导出回归 | ExcelImportHelper 单测（构造 .xlsx → 解析断言）+ 端点路由测试 |

### 4.2 B1/B2/R3-补 新功能缺口（近批新增无测试）
| US | 新增批次 | 建议测试 |
|----|---------|---------|
| MC-008/009 历史聚合 | B1 | MedicalCaseQueryService 历史查询单测（患者多医案 → 倒序聚合断言） |
| MC-018 批量详情 | B1 | GetByIdsWithDetailsAsync In 查询 + Doctor 所有权过滤单测 |
| MC-020 批量删除 | R3-补 | 批量删除权限/状态守卫测试 |
| HERB-005 单删引用检查 | B1 | ValidateBeforeDeleteAsync 引用计数拒绝单测 |
| HERB-006 import-excel | B2 | ExcelImportHelper 解析 + 端点路由测试 |
| REG-002 QuickVisit UI | B2 | QuickVisitCommandHandler 已有——补 VM/对话框链测试 |
| FORM-014 克隆 | R3-补 | 克隆端点测试（含远程 404 语义——按现状文档化） |

### 4.3 结构性缺口（既有功能无守护）
| 缺口 | 说明 |
|------|------|
| SHELL-013 备份恢复 | LocalDbBackupService 无单测（依赖 LocalDB——属 P0-06 环境依赖类；备份编排逻辑可抽纯逻辑单测：保留期清理/文件名/编排顺序） |
| SHELL-018 服务端 Configuration API（Phase 1） | sections GET/PUT/restart 端点无测试（白名单/脱敏/限频逻辑可单测——ConfigurationWritePolicy/限频滑动窗口） |
| SHELL-019 读卡器诊断 | 已有 4/4 单测 ✅（诊断服务）——VM 面板链无测试（可选） |
| 报表趋势端点 | ReportServiceTests 未覆盖趋势/排行聚合 |

---

## 五、建议（新测试守护优先清单）

**优先级 1（P0 逃逸闭环）**：
1. IdentityController 远程路由单测（P0 #1）
2. ExcelImportHelper + import-excel 端点测试（P0 #4）
3. 服务端 RefreshToken 签发单测（P0 #2 补齐）

**优先级 2（近批新功能闭环）**：
4. MC-008/009/018 历史聚合 + 批量详情查询单测
5. HERB-005 引用检查 + HERB-006 Excel 解析测试
6. REG-002 QuickVisit VM 链测试

**优先级 3（结构性补齐）**：
7. SHELL-018 配置 API 白名单/脱敏/限频纯逻辑单测（ConfigurationWritePolicy 已有部分——补 IsSensitive 用例）
8. SHELL-013 备份保留期/编排纯逻辑单测
9. MC-020/FORM-014 端点测试

**执行建议**：随各批实现补测（外科式——新增功能批次自带测试），不设独立「补测大批」；架构测试（AntiMockRule/ArchTests）已守护分层不变量，AC 映射表每批更新。

---

## 六、结论

- **域级守护强度高**：15 域中 12 域有完整测试类守护（VM + Controller + 服务 + 实体四层），横切安全（审计/脱敏/权限边界）有独立测试。
- **AC 级缺口集中在近批新增**：B1/B2/R3-补 新增 US 无测试（新端点/新 UI）——功能正确但守护缺失，回归风险由人工验收承担。
- **P0 逃逸闭环未完成**：4 个 P0 中 2 个修复后仍无测试守护（路由/导入导出）——最高优先补测。
- **映射表为活文档**：每批实现后更新本表 + traceability 状态列（双文档联动）。

**关联**：T1 审查报告（test-code-review-2026-08-11.md）| R1 覆盖矩阵（requirements-coverage-matrix-2026-08-11.md）| traceability v1.4+（147 US：✅139/⚠️3/🔴1/🧲4——🔴1 = SHELL-003 LoginCoordinator 旁路待删）
