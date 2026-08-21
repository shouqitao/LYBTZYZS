# LYBTZYZS 架构审查综合报告（5轮）

> **审查日期**: 2026-08-20  
> **审查人**: Hermes Agent（统筹，架构师视角 `intended vs implemented`）  
> **执行人**: `codebase-memory` + `Server/Desktop` 全仓实测  
> **审查范围**: 5 轮次 · 覆盖 `数据流 · 权限 · 状态机 · 错误处理 · 架构合规`  
> **基线**: `LYBTZYZS` `28,122节点 / 67,287边 / 1,832文件&模块 / 1,314类 / 169接口` · `dotnet build 6 warnings` · `架构 87/87` · `Shared.Models/Enums` 单源 · `ADR-0010` 双模式

**分轮明细**:
| 轮次 | 主题 | 文件 | 报告 |
|------|------|------|------|
| R1 | 数据流完整性验证 | `architecture-review-2026-08-20-R1-dataflow-integrity.md` | CRITICAL 3 · HIGH 5 · MEDIUM 4 · LOW 2 |
| R2 | 权限策略一致性验证 | `architecture-review-2026-08-20-R2-permission-consistency.md` | CRITICAL 3 · HIGH 3 · MEDIUM 3 |
| R3 | 状态机实现一致性验证 | `architecture-review-2026-08-20-R3-state-machine.md` | CRITICAL 2 · HIGH 3 · MEDIUM 3 |
| R4 | 错误处理链完整性验证 | `architecture-review-2026-08-20-R4-error-handling.md` | CRITICAL 2 · HIGH 3 · MEDIUM 3 |
| R5 | 架构合规性验证 | `architecture-review-2026-08-20-R5-architecture-compliance.md` | CRITICAL 1 · HIGH 2 · MEDIUM 3 |

---

## 一、总体结论

**一句话结论**：系统骨架合规（87/87 守卫全绿，三层单向、模块隔离、Mapperly Target、双模式同一 Service），但在**实体完整性 / 批量授权 / 状态机守卫 / 错误码链 / 0/0 门禁**五轴上存在 11 项 P0 阻断，`Patient→MedicalCase聚合→Prescription` 主链与 `Registration↔MedicalCase` 联动链未闭环，**不建议发布**，建议按 P0 顺序 4-5 天集中修复后复审。

| 维度 | 意图 | 实现 | 结论 |
|------|------|------|------|
| 数据流 | `Controller→Service→Repository→AppDbContext` + `ValidationBehavior` + `双栈99%对齐` | `MedicalCase.Create`绕过验证，`Patient`裁剪13字段，`Local`脱敏断裂，`Reports 5趋势`缺 | 🔴 |
| 权限 | `04-permissions SSOT` 7 Policies + `FallbackPolicy` | `BatchDelete×3`漏授权，`Registrations.Cancel`越权，`Admin缺RegistrationModule` | 🔴 |
| 状态机 | `MedicalCase Active↔Suspended→Completed` + `Registration Waiting→InProgress→Completed` | `Doctor取消联动必抛`，`Admin Completed免EditReason` | 🔴 |
| 错误处理 | `Business→System IExceptionHandler` + `CorrelationId` + `ErrorCode→HTTP` | `InvalidOperation裸异常→500`，`无码BusinessException→500` | 🔴 |
| 架构合规 | `0/0` + `87/87` + `Status/State` + `单一契约` | `6×CS0105`，`Herb Export ApiResponse vs HttpResponseMessage` | 🔴 |

---

## 二、量化总览

### 2.1 严重度汇总（去重前 5 轮叠加）

| 严重度 | R1 | R2 | R3 | R4 | R5 | 合计 | 去重后* |
|--------|----|----|----|----|----|------|---------|
| 🔴 CRITICAL | 3 | 3 | 2 | 2 | 1 | 11 | **11** |
| 🟠 HIGH | 5 | 3 | 3 | 3 | 2 | 16 | **14** |
| 🟡 MEDIUM | 4 | 3 | 3 | 3 | 3 | 16 | **15** |
| 🟢 LOW | 2 | 1 | 0 | 0 | 0 | 3 | 2 |
| ✅ PASS | 6 | 7 | 5 | 6 | 6 | 30 | — |

*去重规则：`Build 0/0` 跨 R1/R5 合并为 1；`BatchDelete` 在 R2 出现后 R5 不重复计数。

### 2.2 工程门禁

| 门禁 | 基线 | 实测 | 结论 |
|------|------|------|------|
| `dotnet build --no-incremental` | 0错误0警告 | **6 warnings CS0105** `LYBT.Tests.Desktop.Infrastructure` 重复 using | ❌ |
| `dotnet test tests/LYBT.Tests.Architecture` | 87/87 | **87/87 pass** 20s | ✅ |
| `Server ↔ Client` 依赖 | 禁止互引 | `DP01` 不依赖 `Infrastructure`，`P01c` 不依赖 `WebAPI` | ✅ |
| `Module` 隔离 | 禁直引 `LYBT.Module.*` | 仅 `IXxxCrossModuleService` | ✅ |

---

## 三、跨轮共性根因（5 轮串联）

| 根因 | 涉及轮次 | 典型表现 | 影响面 |
|------|----------|----------|--------|
| **SSOT 滞后** | R1 C1, R5 M3 | `Patient` 文档 20字段 vs 代码 7字段；`76 tests` vs 实测 87 | 需求-代码背离，审计失效 |
| **批量操作模板漏授权** | R1 H5, R2 C1/C2, R5 H1 | `Patients/Catalog BatchDelete` 继承类级策略，`Doctor` 可批量删 | 提权面，3 控制器双端 |
| **状态机守卫与错误码链断裂** | R3 C1/C2, R4 C1/C2 | `Registration.Cancel` 抛 `InvalidOperation` → `SystemHandler 500`；`ValidateEditReason !isAdmin` 放行 `Admin` | 诊疗/挂号孤儿，审计失真 |
| **双栈/双端一致性** | R1 C3/H1, R2, R5 H1 | `Local` 脱敏/路由/权限与 `Remote` 分叉；`Herb Export` 类型分叉 | 本地模式 PII 泄露，导出不可回灌 |
| **门禁与守卫的“字符串匹配”脆弱性** | R4 M2, R3 M3 | `Contains("数据已被其他用户修改")`，`HasSameDayWaiting` 仅查 `Waiting` | 并发/重复挂号漏判 |

---

## 四、分轮精要（每轮一句话 + 关键证据）

### R1 数据流 — C1/C2/C3 截断主链
- **C1 Patient 裁剪**：`PatientModel.cs:14-61` 7字段 vs 文档 20+，`Mapper.ToEntity` 静默丢 `Address/Allergy`。
- **C2 验证绕过**：`MedicalCaseCommandService.cs:123` 注释自证 `CreateFromInputDtoAsync` 直接映射不走 `ValidationBehavior`。
- **C3 双栈断层**：`MedicalCases Local 缺 7端点`，`Reports Local 缺 5趋势`，`print-completed 404`。
- **H1 脱敏断裂**：`LocalWebAPI` 无 `SensitiveDataJsonConverterFactory` 注册，明文 `IdNumber/PhoneNumber`。

### R2 权限 — 批量越权是最大提权面
- **C1/C2 BatchDelete**：`PatientsController:323` / `CatalogController:360,795` 无 `Authorize`，`Doctor/Receptionist` 可批量删。
- **C3 Cancel 越权**：`Registrations.Cancel: DoctorOrReceptionist` vs 意图 `仅 Receptionist`。
- **H2 Admin 看挂号**：`AdminRoleDefinition` 缺 `RegistrationModule`，`Admin` 无 UI 入口。
- **PASS7**：`Patients Delete/AdminOrSuperAdmin`, `Catalog Create/AdminOrSuperAdmin`, `MedicalCases Create/DoctorOnly`, `Users 层级`, `Local 12 Controllers 补齐`, `FallbackPolicy` 均已对齐。

### R3 状态机 — Doctor 取消必抛是事务杀手
- **C1 联动抛异常**：`RegistrationCrossModuleService:44 Doctor分支调 Cancel()`，而 `Registration.Cancel()` 仅 `Waiting` 可调，`InProgress` 必抛 `InvalidOperation`，留下孤儿。
- **C2 Admin 免 EditReason**：`ValidateEditReason: isCompletedEdit = Completed && !isAdmin`，`Admin` 编辑 `Completed/IsLocked` 无需原因。
- **H1 `Complete()` 无守卫**：`Waiting→Completed` 直跳。

### R4 错误处理 — 500 掩盖业务语义
- **C1 裸 `InvalidOperation`**：`Registration.Cancel/StartVisit` + `ValidateEditReason` 抛裸串，`SystemHandler →500` 而非 `422 ERR-80301`。
- **C2 无码 `BusinessException`**：`BusinessException(string)` → `AppException 500`，`BatchOperationHandler` 丢失 `TypedErrorCode`。
- **H3 桌面丢追踪**：`CrudServiceBase` 吞异常后 `GetSafeOperationFailureMessage` 丢 `correlationId/code`。

### R5 架构 — 87/87 绿但 0/0 红
- **C1 0/0**：6×CS0105。
- **H1 契约分叉**：`Server HerbExport → ApiResponse<List>` vs `IApiClientHerbs → HttpResponseMessage`，导出包装体不可回灌。
- **PASS6**：3-Layer、模块隔离、Desktop 分层、`Mapperly Target`、`LocalWebAPI P20-P22`、`API v1` 全绿。

---

## 五、统一修复 backlog（去重后 11 P0 + 8 P1 + 7 P2）

### P0 阻断（发布前必修，4-5 天）

| # | 缺陷 | 来源 | 文件 | 工作量 |
|---|------|------|------|--------|
| P0-1 | `Patients/Catalog BatchDelete×3` 补 `AdminOrSuperAdmin` 双端 | R2 C1/C2 | `PatientsController:323` + `CatalogController:360,795` Server+Local | 0.5d |
| P0-2 | `Registrations.Cancel` 收紧为 `ReceptionistOnly` | R2 C3 | `RegistrationsController:70` Server+Local | 0.3d |
| P0-3 | `Registration` Doctor 取消联动 `InProgress→Cancelled` | R3 C1 + R4 C1 | `RegistrationModel` 新增 `CancelFromMedicalCase()` + `RegistrationCrossModuleService:44` | 0.5d |
| P0-4 | `ValidateEditReason` 去 `&&!isAdmin`，补 `IsLocked/isForeignEdit` | R3 C2 | `MedicalCaseCommandService:251` | 0.3d |
| P0-5 | 领域守卫改 `BusinessException(TypedErrorCode)` | R4 C1 | `RegistrationModel` + `MedicalCaseCommandService.ValidateEditReason` | 0.5d |
| P0-6 | `BusinessException` 禁无码，批量透传 `TypedErrorCode` | R4 C2 | `BusinessException.cs` + `BatchOperationHandlerBase:96` | 0.4d |
| P0-7 | `Patient` 裁剪决策：补实体或改文档+ADR | R1 C1 | `04-data-model.md` + `PatientModel.cs` + 迁移 | 1d |
| P0-8 | `MedicalCase.Create` 补 `IValidator<ConsultationInputDto>` 手动校验 | R1 C2 | `MedicalCaseCommandService:123` | 0.5d |
| P0-9 | `Local` 脱敏注册 + 双栈端点对齐（`Reports 5趋势` + `MedicalCases 7端点`） | R1 C3/H1 | `LocalWebApiProgram` + `LocalWebAPI/Controllers` + `05-dual-mode.md` | 1d |
| P0-10 | `Registration.Complete()` 加 `InProgress` 守卫 | R3 H1 | `RegistrationModel:98` | 0.2d |
| P0-11 | `build 0/0` 6×CS0105 + 文档 `76→87` | R1 L1, R5 C1 | `tests/LYBT.Tests.Desktop/Unit/**/*.cs` + `01-system-overview.md` | 0.2d |

### P1 重要（提权/可观测性，下次迭代）

| # | 缺陷 | 来源 | 文件 |
|---|------|------|------|
| P1-1 | `Prescription Discount 3,2→5,4` | R1 H2 | `PrescriptionConfiguration:22` |
| P1-2 | `MedicalCase ToInputDto` 6字段 `MapperIgnore` 改必填参 | R1 H3 | `MedicalCaseDetailModelMapper:153` |
| P1-3 | `AppDbContext SetAuditFields` Http 兜底 + 显式 `operatorId` | R1 H4 | `AppDbContext:133` |
| P1-4 | `RegistrationFee` 原子事务 L3 | R1 H5 | `MedicalCaseCommandService:152` |
| P1-5 | `Admin RegistrationModule` 只读 | R2 H2 | `AdminRoleDefinition:13` |
| P1-6 | `M2` 验证双通道 `400 vs 422 → 422` | R4 H2 | `ValidationBehavior` + `SystemHandler` |
| P1-7 | `H3` 桌面透传 `correlationId/code` | R4 H3 | `CrudServiceBase:126` |
| P1-8 | `H1` 审计 `catch` 升 `LogError + Counter` | R4 H1 | `ConfigurationController:176` |

### P2 优化（文档/可维护性）

| # | 缺陷 | 来源 |
|---|------|------|
| P2-1 | `IsLocked` 未在 Server Guard 引用 | R3 C2 |
| P2-2 | `IsValidStatusTransition` 终态幂等 | R3 H2 |
| P2-3 | `Auth ValidatingToken→Failed` 分叉 | R3 H3 |
| P2-4 | `EditModeStateMachine` `TransitionBlocked→Saving` | R3 M1 |
| P2-5 | `HasSameDayWaiting → HasSameDayActive(Waiting,InProgress)` | R3 M3 |
| P2-6 | `429→ERR-00012` + `DbUpdateConcurrency→ConflictException` | R4 M1/M2 |
| P2-7 | `Herb Export` 返回类型统一 + `Status/State` 注释清理 | R5 H1/H2, M1-M3 |

---

## 六、风险评估

| 风险 | 概率 | 影响 | 暴露面 | 缓解 |
|------|------|------|--------|------|
| 批量删除提权（P0-1） | 高 | 数据丢失 | `Doctor/Receptionist` 任意有效 JWT | P0-1 立即补授权 + `ArchTest: BatchDelete must have AdminOrSuperAdmin` |
| 挂号孤儿（P0-3） | 中 | 队列不一致 | `Doctor QuickVisit 取消` | P0-3 域方法 + `HandleMedicalCaseCancelledAsync` 集成测试 |
| 审计失真（P0-4/C2） | 中 | 纠纷不可追溯 | `Admin` 编辑 `Completed` 无原因 | P0-4 `IsLocked` 守卫 + `MedicalCaseAuditLog` 强校验 |
| PII 泄露（P0-9 H1） | 低 | 合规 | `Local` 明文 `IdNumber` | P0-9 `LocalWebApiProgram` 脱敏 |
| 发布阻塞（P0-11） | 高 | 门禁 | CI `0/0` | P0-11 删重复 using |

---

## 七、验证计划（每项 P0 修复后必跑）

```bash
dotnet build LYBTZYZS.sln --no-incremental  # 0/0
dotnet test tests/LYBT.Tests.Architecture   # 87/87 (P01/P07/P10/B02/DP01/P20-P22)
dotnet test tests/LYBT.Tests.Server         # 736/736 (含 BR-001, BR-DEL-001, Auth 104)
# 手动冒烟（5 轮串联）
# R2: Receptionist → POST /patients/batch-delete → 403
# R2: Doctor → PUT /registrations/{id}/cancel → 403
# R3: Doctor QuickVisit → StartVisit → CancelMedicalCase → Registration Cancelled (非 500)
# R4: curl -H "X-Correlation-Id: test-123" POST /registrations/.../cancel (非 Waiting) → {"code":"ERR-80301","correlationId":"test-123"}
# R5: GET /api/v1/herbs/export → ApiResponse 解析 → BatchImport 回灌
# 双模式: SwitchingApiClient Remote 5000 ↔ Local 127.0.0.1:5300
```

---

## 八、结论与建议

- **架构底座健康**：分层、隔离、共享单源、Mapperly、双模式、87 守卫均已达标，具备持续交付基础。
- **发布建议**：**不建议当前提交发布**。按 P0 11 项 4-5 天集中修复后，复测 `build 0/0 + 87/87 + 手动 5 场景` 再进入预发布。
- **过程改进**：① 新增 `BatchDelete ArchTest` 与 `ReturnTypeParity` 守卫防批量/契约回退；② `Domain Guard` 禁 `InvalidOperation` 裸抛（`ArchTest: Domain must throw BusinessException`）；③ 文档测试数单源化 `TestAssemblies`。

---

## 九、附录

- **分轮报告**: `architecture-review-2026-08-20-R1-dataflow-integrity.md` / `R2-permission-consistency.md` / `R3-state-machine.md` / `R4-error-handling.md` / `R5-architecture-compliance.md`
- **核心文件**: `src/Shared/LYBT.Entities/Patients/PatientModel.cs` / `MedicalCases/MedicalCaseModel.cs` / `Registrations/RegistrationModel.cs` / `ServerArchTests.cs` / `DesktopLayerArchTests.cs` / `UnifiedMiddlewareConfiguration.cs` / `Business/SystemExceptionHandler.cs`
- **架构决策**: `ADR-0010` (LocalWebAPI 同一 Service) · `A-26` (Status/State) · `A-31-C8` (跨模块单轨)
