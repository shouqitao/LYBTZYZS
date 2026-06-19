# 需求文档审计报告

> 日期: 2026-06-14
> 范围: docs/02-requirements/ 全部 15 模块 + 4 跨切面文档 vs src/ 实际代码
> 方法: 4 个并行审查 agent，逐模块对比 US 验收标准 vs 控制器/服务/实体代码

---

## 审计概况

| 维度 | 结果 |
|------|------|
| US 编号连续性 | ✅ 138 个，无跳号、无重号 |
| 模板完整性 | ✅ 14/15 模块完整（仅 10-sync.md 缺 Data Model） |
| 优先级分布 | ✅ Must 51 + Should 54 + Could 33 = 138 |
| DDD 聚合验证 | ✅ Consultation/Prescription 为 MedicalCase 内部实体 |
| CQRS 验证 | ✅ MedicalCaseCommandService + MedicalCaseQueryService 分离 |

---

## CRITICAL 问题（8 个）

### 1. Registration: US-REG-005 医案完成→挂号状态联动未接线
- `MedicalCaseStateService.CompleteAsync` 完成医案但不更新 Registration 状态
- `RegistrationService.CompleteByMedicalCaseAsync` 存在但是**死代码**——无任何调用

### 2. Registration: US-REG-006 取消联动绕过文档定义的 Service 契约
- 实际通过 `_registrationRepository` 直接操作，非经 `RegistrationService.HandleMedicalCaseCancelledAsync`
- 后者也是**死代码**，且逻辑不同（清除 vs 保留 MedicalCaseId）

### 3. Sync: MedicalCase 同步文档说"未实现"，代码已有基础实现
- 文档多处标注"延期/未实现"，但 `SyncService` 已完整处理 MedicalCase 的 metadata/upload/download/delete
- 设计文档（599-820行）描述的聚合级原子事务、DTO 映射、依赖排序等均未实现

### 4. Configuration: 18 个 FeatureToggle 文档化，代码仅有 2 个
- 文档 US-CFG-002 列出 18 个布尔开关，代码 `FeatureToggleOptions` 仅有 `DuplicateHerbMergeStrategy` + `OverwriteConflicts`

### 5. Role Matrix: PermissionLevel 值错误
- 文档: SuperAdmin=100 / Admin=80 / Doctor=60 / Receptionist=40
- 代码: SuperAdmin=100 / Admin=10 / Doctor=1 / Receptionist=0

### 6. ErrorHandling: 非 RFC 7807 ProblemDetails 格式
- 文档要求 `application/problem+json` + RFC 7807 结构
- 代码返回自定义 `ApiResponse` + `application/json`

### 7. MedicalCases: DecocteMethod 枚举名称和值都不匹配
- 文档 7 个值 vs 代码 7 个值，其中 5 个不同（名称和/或数值）
- 持久化的 int 值会被误解

### 8. Registration: US-REG-002 QuickVisitAsync 死代码 + 本地模式医生身份丢失
- Server Controller 内联重写了逻辑，Service 方法从未被调用
- Local QuickVisit 硬编码 `DoctorId = Guid.Empty`

---

## HIGH 问题（15 个）

### Auth
- 双模式表声称本地无 JWT，实际本地使用 `LocalJwtConfig.GenerateToken`
- AuthSession 实体文档仍在，代码已删除
- 3 个"未实现"事件实际已实现（SessionExtended/LogoutStarted/ForcedLogout）
- 账户锁定逻辑未在文档中记录

### Users
- 跨模块方法名错误：`RevokeAllUserTokensAsync` → 实际 `RevokeUserTokensAsync`
- OQ-USER-02 说"不支持 Email 修改"，代码实际支持
- US-USER-006/008 权限：文档说 Admin/SuperAdmin，代码限 SuperAdminOnly
- 禁用有待挂号医生的规则未文档化

### Patients
- 远程 import 端点完全缺失（Service 存在但 Controller 未暴露）
- 本地 import/export 实现与文档不符（JSON vs Excel）
- OQ-PAT-03 身份证去重已实现但文档说未实现

### Herbs/Formulas
- Create 返回码：文档 200，代码 201
- AllowAnonymous 文档要求但代码未加（Herbs/Formulas ExportTemplate）
- 本地模式缺少所有权检查
- Herb `Properties`（性味）字段未文档化

### Registration
- US-REG-004 前台取消：未强制 Receptionist 权限（医生也能取消）
- US-REG-003 start-visit 不创建 MedicalCase

### Logging
- "硬编码 30 天"声明已过期（代码已改为可配置 365 天）
- 配置路径错误：`Lybt:SecurityAudit:Cleanup` → 实际 `Security:AuditRetentionDays`

### CardReader
- 自动轮询读卡已实现但文档标记为 Out of Scope

### DesktopShell
- 前进导航已实现但文档标记为 Out of Scope

### HealthDiagnostics
- 本地 DiagnosticsController `[AllowAnonymous]`（应为 SuperAdminOnly）
- 本地 HealthController 详情匿名可访问
- 本地 ping 返回 `{status:"ok"}` 而非文档的 `{message:"pong"}`

---

## MEDIUM 问题（15+ 个）

### Sync
- DTO 类名/字段名全量不匹配（SyncDiffResultDto→SyncCompareResultDto 等）
- 20 个结构化错误码定义但未实现（Service 返回原始字符串）
- 缺 Data Model 章节
- ChangedFields 声明但从不填充

### MedicalCases
- PrintVersion 每次打印递增（文档说仅编辑后递增）
- 本地模式多处绕过验证（CompleteAsync guard、CancelCase Completed check）
- 审计字段数 19 vs 20
- AuditOperationType.Cancel=5 未文档化
- MC-D17 重复药材策略配置键/默认值/名称全不匹配

### Formulas
- OQ-FORM-01 说"不支持复制"，本地有 `POST /formulas/{id}/clone`
- 本地 GetPendingValidation 额外过滤 IsShared
- 本地批量操作返回 `{count}` 而非 `BatchOperationResultDto`

### ErrorHandling
- 429 未映射
- ErrorSeverity.Fatal=4 未文档化
- ErrorCategory.Network/Unknown 未文档化

### Configuration
- Prescription 配置位置文档过期

---

## 分类统计

| 类别 | 数量 | 说明 |
|------|------|------|
| 文档错误（需改文档） | ~30 | 值/名称/路径不匹配代码 |
| 代码缺陷（需改代码） | ~10 | 功能未接线、权限缺失、验证绕过 |
| 决策待定 | ~8 | 设计 vs 实现分歧，需用户裁定 |
| Open Questions 已解决 | ~12 | 文档标记 OPEN 但代码已解决 |

---

## 建议执行顺序

1. **先修文档**（低风险，高效率）— 值/名称/路径对齐到代码现状
2. **关键代码修复**（需确认）— REG-005 联动接线、本地权限缺失
3. **设计决策**（需讨论）— ProblemDetails 格式、FeatureToggle 范围、DecocteMethod 枚举
