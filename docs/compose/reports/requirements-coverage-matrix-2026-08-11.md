# 需求覆盖矩阵（R1 只读扫描，2026-08-11）

> 扫描范围：docs/02-requirements/（18 文件）对照 src/ 实际代码（Server WebAPI 81 端点 + 模块 Service/Handler + Desktop 功能）
> 方法：以 `13-traceability-matrix.md`（v1.0 基线，2026-06-28）为骨架，4 个 scout 分域逐 US 对照代码验证当前态
> 状态图例：✅实现 / ⚠️部分（有代码缺关键面）/ 🔴缺失（需求声称已实现但代码无）/ 🧲待实现（仍无代码）/ 📦v2.0 规划

---

## 〇、总览（结论摘要）

**「需求先行」治理基准：基线矩阵 ~34 处状态过时；真实缺口集中在 4 个 P0 面（认证路由/刷新链、导入导出端点、FeatureToggle、备份恢复）；文档引用失效 8+ 处。**

| 域 | US 数 | ✅ | ⚠️ | 🔴 | 🧲/📦 | 基线过时 |
|---|---|---|---|---|---|---|
| 认证 AUTH | 14 | 2 | 5 | 5 | 2 | ~8 |
| 用户 USER | 12 | 4 | 7 | 1 | 0 | ~6 |
| 患者 PAT | 13 | 5 | 5 | 2 | 1 | ~5 |
| 药材 HERB | 13 | 7 | 3 | 2 | 1 | ~6 |
| 验方 FORM | 13 | 6 | 3 | 2 | 2 | ~5 |
| 医案 MC | 19 | 7 | 7 | 3 | 2 | ~8 |
| 挂号 REG | 8 | 3 | 3 | 0 | 2 | ~4 |
| 打印 PRINT | 4 | 2 | 2 | 0 | 0 | 1 |
| 报表 REPORT | 3 | 0 | 3 | 0 | 0 | 3 |
| Shell SHELL | 13 | 5 | 2 | 1 | 5 | ~4 |
| 配置 CFG | 4 | 3 | 0 | 1 | 0 | 1 |
| 异常 ERR | 8 | 4 | 4 | 0 | 0 | 2 |
| 可观测 LOG/SYS | 16 | 15 | 1 | 0 | 0 | 0 |
| 读卡 CARD | 2 | 1 | 1 | 0 | 0 | 0 |
| **合计** | **136** | **64** | **46** | **17** | **15** | **~34** |

**US 计数核对**：任务声称 AUTH×15/USER×14/PAT×14/HERB×18/FORM×16/MC×50/REG×23 均与实际文档不符（实际 14/12/13/13/13/19/8——scout 逐文档核实）。

---

## 一、🔴 真脱节（需求声称已实现/应实现，代码缺失或损坏）

### P0 — 影响运行的功能缺口
| # | 缺口 | 证据 |
|---|---|---|
| 1 | **远程 /api/v1/auth/* 五端点路由疑似损坏**（A-31-C3a 合并后组合路由 `/api/v1/users/api/v1/auth/login` 等，桌面客户端固定调 `/api/v1/auth/login`）——远程登录/刷新/登出/validate/auto-login 可能全 404 [INFERENCE 高置信，E2E 未覆盖] | IdentityController.cs 类路由 `api/v{version}/users` + 动作模板 `api/v{version}/auth/login`；IAuthApi.cs:31 |
| 2 | **US-AUTH-004 refresh 端到端断裂**：服务端从不赋值 RefreshToken（LoginResponse.RefreshToken 恒空）→ 客户端 TokenRefreshHandler 拿空 → 远程会话无法续期；服务端 refresh 语义实为 access-token 哈希换新 | LoginCommandHandler.cs；TokenRefreshHandler.cs:150-161 |
| 3 | **本地 /api/v1/auth/refresh 无验签**（ReadJwtToken 只解析不验证签名）——任意伪造含 NameIdentifier 的 JWT 可换 365 天有效令牌；且文档称本地模式无 refresh | LocalRefreshTokenCommandHandler.cs |
| 4 | **导入导出端点双端全缺**：import-template/export/export-all（PAT-011/012、HERB-007/013、FORM-013）WebAPI+LocalWebAPI 均无，Desktop Refit 客户端已写死 URL → 用户点导出 404；服务端 Excel 能力（EPPlus/NPOI）零引用 | PatientsController/CatalogController；IPatientApi.cs:59,67 |
| 5 | **备份/恢复整体缺失**（US-SHELL-013、NFR-AVAIL-001）：无 LocalDbBackupService/BACKUP DATABASE 代码，仅 SystemSettingsViewModel 设置项占位——基线 ⚠️ 误标（实为 🔴 全无） | grep 零命中；SystemSettingsViewModel.cs:58,168 |
| 6 | **FeatureToggle 体系全库消失**（US-CFG-004）：FeatureToggleOptions/OverwriteConflicts/DuplicateHerbMergeStrategy/ConfigurationOptionsMonitor 零命中；唯一热更新为 ClinicSettingsService（诊所信息非功能开关） | grep 零命中；ClinicSettingsService.cs |

### P1 — 权限/业务规则缺口
| # | 缺口 | 证据 |
|---|---|---|
| 7 | **EditReason 机制缺失**（US-MC-002/014/016）：DTO+Desktop 传参，Server 零校验零存储——「锁定/已完成/非本人编辑需 EditReason→422」全部未实现 | MedicalCaseInputDto.cs:60；grep EditReason=0 |
| 8 | **Doctor 所有权过滤缺失**（US-MC-004/007/017）：GetById/Search/AuditLogs 无 operator 参数，任意医生可查任意医案/审计；「仅 Admin 强制关闭」（MC-012）未实现 | MedicalCaseQueryService.cs:314 |
| 9 | **挂号服务端守卫全缺**（US-REG-006）：Cancel 无条件置 Cancelled，无 Waiting-only/当天/关联医案检查——全部校验在 Desktop UI，绕过 UI 可取消任意挂号；同日唯一约束不存在（REG-BR-007） | CancelRegistrationCommandHandler.cs；RegistrationDbContext.cs |
| 10 | **保留用户名清单缺失**（US-AUTH-011）：admin/administrator/root 可被创建为用户名 | CreateUserCommandHandler.cs |
| 11 | **AutoLoginToken 轮换缺失**（US-AUTH-010）：服务端从不签发 AutoLoginToken，ValidateAutoLoginToken 只换 access token | JwtService.cs |
| 12 | **用户管理缺口**：单删「不可删自己」（USER-006）、重置密码无 sysadmin 保护（USER-007 Admin 可重置 sysadmin）、role 筛选参数未接线（USER-001）、REG-BR-006（Waiting 挂号阻止禁用）全缺 | DeleteUserCommandHandler.cs:38；ResetPasswordCommandHandler.cs；BaseUsersController.cs:37-52 |
| 13 | **患者脱敏不生效**（US-PAT-013）：[SensitiveData] 仅实体有，DTO 无——序列化管道对 API 响应不生效，身份证/电话明文 | PatientModel.cs；PatientDetailDto.cs |
| 14 | **验方单条创建/更新丢药材**（US-FORM-003/004）：CatalogDtoMapper.ToEntity 与 Formula.UpdateProfile 不处理 Herbs——只有批量导入能建出带药材的验方 | CatalogDtoMapper.cs |
| 15 | **FLAW-F1 降级逻辑缺失**（US-FORM-010）：全仓无降级 Draft 代码（需求/基线标 ✅） | ValidateFormulaHerbCommandHandler.cs |
| 16 | **审计 20 字段 diff 未实现**（US-MC-017）：ChangedFields/OldValues/NewValues 列从不填充，仅取消写审计 | MedicalCaseRepository.AuditLogs.cs |
| 17 | **报表 AC 2 项缺失**（US-REPORT-001~003）：仅传 startDate 时 endDate 未默认等于 startDate；startDate>endDate 无 400 校验 | ReportsController.cs:33,43,55 |
| 18 | **打印回写断链**（US-PRINT-004）：服务端 RecordPrint 完整，Desktop 打印流零调用（RecordPrintAsync 全链路死代码） | PrescriptionPrintHandler.cs；MedicalCaseApiClient.cs:120 |

### 📄 文档引用失效（需求/矩阵引用的代码不存在）
- `BaseMedicalCasesController.GetPatientConsultations`（US-MC-008 虚构引用）；`MedicalCasePrintController.cs`（09 实现参考）；`POST /print-log` 端点
- `PrismConfigurationExtensions.cs:80/:133`（实际 67 行）；`DefaultPasswordService.cs`（README 自标 [SUSPECT]）；`IConnectionModeProvider/ModeSwitchValidator`（US-SHELL-007）
- `HerbsController.cs`/`FormulasController.cs`（A-31-C3b 已合并为 CatalogController）；`docs/05-development/09-performance-baseline.md`（实为 07）
- WebAPI README.md:417-418 残留 batch-details/consultations/prescriptions 三端点（均不存在）
- 01-prd 护栏「100% 写操作有审计」：Restore/Create/Update/Batch 未写 SecurityAuditLog

---

## 二、⚠️ 部分实现（有代码但缺关键面，按域）

- **AUTH**：002 登录锁定（本地显式关闭 LockoutEnabled=false 与文档冲突）；003 限流（本地 auto-login 无限流）；006 族旋转降级为会话旋转（FamilyId/IsUsed 设计死代码，重放不撤销整族）；008 登出（本地 no-op）；009 本地 auto-login 无服务端签发管理
- **USER**：004 默认密码随机 GUID 非 DefaultPasswords 配置 + 层级约束缺失；005 更新不能改角色；010 启停机制为 Status 字段非 Identity Lockout；012 批量无 100 条上限
- **PAT**：002 非管理员查禁用患者未 404（与 006 医生看历史自相矛盾，代码取后者）；003/004 实现为**姓名唯一**而非需求「电话唯一」（DB 电话索引非唯一）；010 批量引用检查 N+1；013 脱敏 DTO 不生效
- **HERB**：006 批量导入仅 DTO 路径（服务端 Excel 路径缺失）；012 批量删除无逐项引用检查；001/002 需求声称 OutputCache 策略已删除
- **FORM**：001 列表无所有权过滤（Doctor 可见全部验方）；006 批量导入无 10000 上限；007 待验证列表无分页
- **MC**：002 保存无 EditReason 校验；004/007 Doctor 所有权缺失；011 完成无 isAdmin/owner 校验；012 force-close 仅 Admin 未实现；016 权限 DTO 缺 RequiresEditReason/DenialReason、CanCancel 仅 Active；017 审计仅取消写；验方导入禁用跳过无客户端提示（MC-D09）；**价格口径三处不一致**（服务端=药费×帖数×折扣 vs Desktop 编辑器无折扣 vs 打印含诊金/治疗费）
- **REG**：001 前台挂号缺患者 Enabled 校验/同日重复检查/挂号费带出（REG-BR-009 仅 QuickVisit 路径）；004 队列无当天过滤（REG-BR-012）
- **REPORT**：3 端点 AC 2 项缺口（见 P1-17）
- **SHELL**：007 双模切换守卫（ERR-70506）缺失；011 初始化向导仅单屏（非 5 步）；014 审计无查看 UI；018 配置中心未实现
- **ERR**：006 自定义 ValidationException/AddError 链不存在（仅 FluentValidation 映射）；007 6 种异常仅 3 种（Conflict/Unauthorized/ApiException/ExceptionFactory 全缺）；008 严重度映射未逐条核实
- **CARD**：002 降级链已移除（A-31-C7），现存精确匹配→创建

---

## 三、代码超前于需求（反向脱节，需求未覆盖）

| 代码功能 | 说明 |
|---|---|
| 报表 6 端点（收入/问诊趋势、医生绩效、热门药材、患者流量） | 违反 10-reports.md「v1.0 克制：不做趋势分析」明确声明 |
| 验方克隆端点（仅 LocalWebAPI 有，远程 404） | 06-formulas.md 无克隆 US |
| SignalR 实时推送（US-REG-008 服务端+桌面完整实现） | 基线 🧲 过时 |
| 安全审计服务（AUTH-007）、会话旋转重放检测（AUTH-006 部分）、本地限流（AUTH-013）、Restore 层级（USER-011） | 基线标 🧲/🔴 过时 |
| ConfigurationController PUT/validate、DeployController、ClinicSettingsService 热更新 | 11b/11a 无对应 US |
| 患者按身份证查询端点、医案批量删除、禁用患者前进行中医案检查 | 需求未列 |

---

## 四、版本/口径漂移

- Prism 实际 8.1.97 vs 文档 9.x（11a-shell/NFR-COMP-002）；QuestPDF 2025.12.4 vs 文档 2025.4.0
- 权限偏宽：Diagnostics/ConfigurationController AdminOrSuperAdmin vs 文档「仅 SuperAdmin」
- 代码注释 stale：MedicalCaseEnums.cs「取消统一软删除」与物理删除实现矛盾（2026-08-03 决策未回写）
- 本地 WebAPI 端口文档一处 5300 一处 5290

---

## 五、治理基准建议（为「需求先行」提供依据）

1. **P0 优先验证**：远程 auth 路由（合并后是否 404——需 E2E 实测，highest priority）；refresh 链补 RefreshToken 签发或改客户端语义；本地 /refresh 加签名校验或删除
2. **矩阵校准**：13-traceability-matrix.md ~34 处状态过时（本报告各域表即校准依据），建议随需求文档修订一并更新（A-31-C3a/C3b 重构后「实现文件」列全部指向已删除文件）
3. **需求-代码闭环治理**：以本报告脱节点清单为 backlog 源——🔴 17 项为「需求先行」反向案例（需求文档声称已实现但代码无，应先修代码或先改需求文档，禁止两者长期背离）
4. **文档引用失效修复**：8+ 处失效引用（虚构方法/文件/行号）应清理（US-MC-008 的 GetPatientConsultations、WebAPI README 三端点等）
5. **后续批次建议**：T4 类（修复 P0 缺口）、R2 类（需求文档状态列同步 2026-08-03 决策与 A-31 重构）

---

## 附：证据索引（每域核心文件）

| 域 | 关键证据文件 |
|---|---|
| Auth/User | IdentityController.cs、BaseUsersController.cs、LoginCommandHandler.cs、RefreshTokenCommandHandler.cs、AuthSessionRepository.cs、LocalWebApiProgram.cs、SecurityAuditService.cs |
| Patient/Herb/Formula | PatientsController.cs、CatalogController.cs、CreatePatientCommandHandler.cs、CatalogDtoMapper.cs、ValidateFormulaHerbCommandHandler.cs、AppDbContextModelSnapshot.cs（IX_Patient_Phone 非唯一） |
| MC/REG | MedicalCasesController.cs、BaseMedicalCasesController.cs、MedicalCaseStateService.cs、MedicalCaseCommandService.Deletion.cs、StartVisitCommandHandler.cs、CancelRegistrationCommandHandler.cs、RegistrationDbContext.cs |
| Print/Report/Shell | PrescriptionPrintService.cs、PrescriptionPrintHandler.cs、ReportsController.cs、App.xaml.cs、LoginCoordinator.cs、SystemSettingsViewModel.cs、PrismConfigurationExtensions.cs（67 行） |
| Config/Err/Obs | ConfigurationController.cs、SystemExceptionHandler.cs、LogCleanupService.cs、DiagnosticsController.cs、HealthController.cs |
| CardReader | CardReaderService.cs、PatientCardReaderIntegration.cs、HuaDaHD100CardReader.cs |
