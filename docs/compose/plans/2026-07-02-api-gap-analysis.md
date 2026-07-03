# WebAPI 功能清单完整性审计

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 对照 141 个 US 需求，逐一审计 WebAPI 端点覆盖度，识别缺失/错位的功能点，输出实施计划。

**Architecture:** 基于追溯矩阵 (`docs/02-requirements/13-traceability-matrix.md`) + 实际 Controller 代码，逐 US 对比设计需求 vs 实际端点。

**Tech Stack:** ASP.NET Core, MediatR CQRS, EF Core, SQL Server

## Global Constraints

- 所有新端点必须使用 MediatR ISender 分发
- 授权策略：DoctorOrAdmin / DoctorOrAdminOrReceptionist / AdminOrSuperAdmin
- 响应格式统一：`ApiResponse<T>` 信封
- 本地/远程双模式：WebAPI (port 5000) + LocalWebAPI (port 5300)
- 编码规范：中文业务注释，英文标识符

---

## [S1] 审计总览

### 按模块统计

| 模块 | US 总数 | ✅ 已实现 | 🔴 缺失 | ⚠️ 部分 | 🧲 待实现 |
|------|:---:|:---:|:---:|:---:|:---:|
| AUTH | 13 | 10 | 0 | 1 | 2 |
| USER | 12 | 10 | 0 | 2 | 0 |
| PAT | 13 | 12 | 1 | 0 | 0 |
| HERB | 13 | 7 | 5 | 0 | 1 |
| FORM | 13 | 9 | 2 | 2 | 0 |
| MC | 19 | 12 | 4 | 0 | 3 |
| REG | 8 | 3 | 3 | 0 | 2 |
| PRINT | 4 | 3 | 0 | 0 | 1 |
| CFG | 4 | 4 | 0 | 0 | 0 |
| ERR | 8 | 8 | 0 | 0 | 0 |
| LOG | 7 | 7 | 0 | 0 | 0 |
| SYS | 9 | 9 | 0 | 0 | 0 |
| CARD | 2 | 2 | 0 | 0 | 0 |
| REPORT | 3 | 0 | 0 | 3 | 0 |

---

## [S2] 🔴 缺失端点清单 (需新建)

### 2.1 Patients 模块

| US | 端点 | 说明 | 优先级 |
|----|------|------|--------|
| US-PAT-005 | DELETE /patients/{id} | 删除前需引用检查（有医案不可删） | Must |

**现状**: DeletePatientCommand 已有引用检查逻辑（检查 MedicalCases），追溯矩阵标注🔴。需要验证是否正确连接。

### 2.2 Herbs 模块

| US | 端点 | 说明 | 优先级 |
|----|------|------|--------|
| US-HERB-008 | GET /herbs/{id}/check-reference | 单个药材引用检查 | Should |
| US-HERB-009 | POST /herbs/batch-check-reference | 批量引用检查 | Should |
| US-HERB-011 | POST /herbs/{id}/restore | 恢复软删除药材 | Should |

**现状**: HerbsController 无 check-reference 和 restore 端点。

### 2.3 Formulas 模块

| US | 端点 | 说明 | 优先级 |
|----|------|------|--------|
| US-FORM-012 | POST /formulas/{id}/restore | 恢复软删除验方 | Should |

**现状**: FormulasController 无 restore 端点。

### 2.4 MedicalCases 模块

| US | 端点 | 说明 | 优先级 |
|----|------|------|--------|
| US-MC-008 | GET /medicalcases/{patientId}/consultations | 患者诊断历史聚合 | Should |
| US-MC-009 | GET /medicalcases/{patientId}/prescriptions | 患者处方历史聚合 | Should |
| US-MC-016 | GET /medicalcases/{id}/permissions | 医案操作权限查询 | Should |
| US-MC-017 | GET /medicalcases/{id}/audit-logs | 审计日志查询 | Must |
| US-MC-018 | POST /medicalcases/batch-details | 批量详情查询 | Should |

**现状**: MedicalCasesController 无 consultations/prescriptions/permissions/audit-logs/batch-details 端点。

### 2.5 Registrations 模块

| US | 端点 | 说明 | 优先级 |
|----|------|------|--------|
| US-REG-002 | POST /registrations/quick-visit | 快速就诊（急诊通道） | Must |
| US-REG-008 | SignalR Hub | 待诊列表实时推送 | Must |

**现状**: QuickVisit 端点已定义但标记为"待激活"；SignalR 未实现。

---

## [S3] ⚠️ 权限/实现问题清单

### 3.1 权限错配 (D7)

| US | 当前策略 | 正确策略 | 模块 |
|----|----------|----------|------|
| US-MC-001 | DoctorOrAdmin | DoctorOnly | MedicalCases |
| US-REG-005 | DoctorOrAdmin | DoctorOrAdminOrReceptionist | Registrations |
| US-REG-006 | DoctorOrAdmin | DoctorOrAdminOrReceptionist | Registrations |

### 3.2 部分实现

| US | 问题 | 模块 |
|----|------|------|
| US-AUTH-002 | 双轨锁定未对齐 (D8) | Auth |
| US-USER-001 | TotalCount 内存筛选 (D8) | Users |
| US-USER-012 | 仅 batch-delete，缺 batch-enable/disable | Users |
| US-FORM-011 | 仅 toggle-status，缺 batch-toggle | Formulas |
| US-REPORT-001~003 | 时间范围参数未传入查询 | Reports |

---

## [S4] 🧲 v1.0 待实现清单 (D 补回项)

| US | 端点 | 说明 | 优先级 |
|----|------|------|--------|
| US-AUTH-006 | ITokenRevocationService | 令牌族旋转撤销 | Must |
| US-AUTH-007 | ISecurityAuditService | 安全审计日志 | Should |
| US-AUTH-013 | 本地限流中间件 | 5 次/分 | Must |
| US-USER-011 | POST /users/{id}/restore | 恢复软删除用户 | Should |
| US-PRINT-004 | PUT /print-completed + POST /print-log | 打印记录回写 | Must |
| US-REG-008 | SignalR Hub | 待诊实时推送 | Must |

---

## [S5] 实施计划

### Task 1: Herbs 引用检查端点

**Covers:** US-HERB-008, US-HERB-009

**Files:**
- Create: `src/Server/Modules/LYBT.Module.Herbs/Application/Queries/CheckHerbReferenceQuery.cs`
- Create: `src/Server/Modules/LYBT.Module.Herbs/Application/Queries/CheckHerbReferenceQueryHandler.cs`
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/HerbsController.cs`
- Modify: `src/Client/Desktop/LocalWebAPI/Controllers/HerbsController.cs`

**Step 1: 创建 CheckHerbReferenceQuery + Handler**

```csharp
// CheckHerbReferenceQuery.cs
public record CheckHerbReferenceQuery(Guid HerbId) : IRequest<Result<HerbReferenceCheckResultDto>>;

// CheckHerbReferenceQueryHandler.cs
public class CheckHerbReferenceQueryHandler : IRequestHandler<CheckHerbReferenceQuery, Result<HerbReferenceCheckResultDto>>
{
    private readonly IHerbRepository _herbRepository;
    // 检查 Formulas 是否引用该药材
}
```

**Step 2: 添加 Controller 端点**

```csharp
// WebAPI HerbsController
[HttpGet("{id}/check-reference")]
public async Task<IActionResult> CheckReference(Guid id) { ... }

[HttpPost("batch-check-reference")]
public async Task<IActionResult> BatchCheckReference([FromBody] BatchCheckReferenceInputDto dto) { ... }
```

**Step 3: 同步 LocalWebAPI HerbsController**

**Step 4: 编译验证**
Run: `dotnet build LYBTZYZS.sln`

**Step 5: Commit**
```bash
git commit -m "feat(herbs): add reference check endpoints (US-HERB-008/009)"
```

### Task 2: Herbs Restore 端点

**Covers:** US-HERB-011

**Files:**
- Create: `src/Server/Modules/LYBT.Module.Herbs/Application/Commands/RestoreHerbCommand.cs`
- Create: `src/Server/Modules/LYBT.Module.Herbs/Application/Commands/RestoreHerbCommandHandler.cs`
- Modify: HerbsController (WebAPI + LocalWebAPI)

**Step 1: 创建 RestoreHerbCommand + Handler**

```csharp
// RestoreHerbCommand.cs
public record RestoreHerbCommand(Guid HerbId, Guid OperatorId) : IRequest<Result<HerbDetailDto>>;

// RestoreHerbCommandHandler.cs
// 恢复 IsDeleted = false，验证无重复启用名称
```

**Step 2: 添加 Controller 端点**

```csharp
[HttpPost("{id}/restore")]
public async Task<IActionResult> Restore(Guid id) { ... }
```

**Step 3: 编译验证**
**Step 4: Commit**

### Task 3: Formulas Restore 端点

**Covers:** US-FORM-012

**Files:**
- Create: `src/Server/Modules/LYBT.Module.Formula/Application/Commands/RestoreFormulaCommand.cs`
- Create: `src/Server/Modules/LYBT.Module.Formula/Application/Commands/RestoreFormulaCommandHandler.cs`
- Modify: FormulasController (WebAPI + LocalWebAPI)

同 Task 2 模式。

### Task 4: MedicalCases 历史聚合端点

**Covers:** US-MC-008, US-MC-009

**Files:**
- Create: `src/Server/Modules/LYBT.Module.MedicalCase/Application/Queries/GetPatientConsultationsQuery.cs`
- Create: `src/Server/Modules/LYBT.Module.MedicalCase/Application/Queries/GetPatientConsultationsQueryHandler.cs`
- Create: `src/Server/Modules/LYBT.Module.MedicalCase/Application/Queries/GetPatientPrescriptionsQuery.cs`
- Create: `src/Server/Modules/LYBT.Module.MedicalCase/Application/Queries/GetPatientPrescriptionsQueryHandler.cs`
- Modify: MedicalCasesController (WebAPI + LocalWebAPI)

**Step 1: 创建诊断历史 Query**

```csharp
public record GetPatientConsultationsQuery(Guid PatientId, int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedResult<ConsultationHistoryDto>>>;

// 查询该患者所有医案的 Consultation 部分（含诊断信息）
```

**Step 2: 创建处方历史 Query**

```csharp
public record GetPatientPrescriptionsQuery(Guid PatientId, int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedResult<PrescriptionHistoryDto>>>;

// 查询该患者所有医案的 Prescription 部分（含处方药材）
```

**Step 3: 添加 Controller 端点**

```csharp
[HttpGet("patient/{patientId}/consultations")]
public async Task<IActionResult> GetPatientConsultations(Guid patientId, ...) { ... }

[HttpGet("patient/{patientId}/prescriptions")]
public async Task<IActionResult> GetPatientPrescriptions(Guid patientId, ...) { ... }
```

**Step 4: 编译验证**
**Step 5: Commit**

### Task 5: MedicalCases 权限/审计端点

**Covers:** US-MC-016, US-MC-017

**Files:**
- Create: `src/Server/Modules/LYBT.Module.MedicalCase/Application/Queries/GetMedicalCasePermissionsQuery.cs`
- Create: `src/Server/Modules/LYBT.Module.MedicalCase/Application/Queries/GetMedicalCaseAuditLogsQuery.cs`
- Modify: MedicalCasesController (WebAPI + LocalWebAPI)

**Step 1: 创建权限查询 Query**

```csharp
public record GetMedicalCasePermissionsQuery(Guid CaseId, Guid UserId)
    : IRequest<Result<MedicalCasePermissionsDto>>;

// 返回 CanEdit, CanComplete, CanSuspend, CanCancel, CanDelete 等
```

**Step 2: 创建审计日志 Query**

```csharp
public record GetMedicalCaseAuditLogsQuery(Guid CaseId, int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedResult<AuditLogDto>>>;

// 查询医案变更历史（需先实现 AuditLog 实体存储）
```

**Step 3: 添加 Controller 端点**

```csharp
[HttpGet("{id}/permissions")]
public async Task<IActionResult> GetPermissions(Guid id) { ... }

[HttpGet("{id}/audit-logs")]
public async Task<IActionResult> GetAuditLogs(Guid id, ...) { ... }
```

**Step 4: 编译验证**
**Step 5: Commit**

### Task 6: MedicalCases 批量详情

**Covers:** US-MC-018

**Files:**
- Create: `src/Server/Modules/LYBT.Module.MedicalCase/Application/Queries/GetMedicalCasesBatchQuery.cs`
- Create: `src/Server/Modules/LYBT.Module.MedicalCase/Application/Queries/GetMedicalCasesBatchQueryHandler.cs`
- Modify: MedicalCasesController (WebAPI + LocalWebAPI)

**Step 1: 创建批量查询 Query**

```csharp
public record GetMedicalCasesBatchQuery(List<Guid> Ids) : IRequest<Result<List<MedicalCaseDetailDto>>>;

// 限制 ≤50 个 ID，解决 N+1 问题
```

**Step 2: 添加 Controller 端点**

```csharp
[HttpPost("batch-details")]
public async Task<IActionResult> GetBatchDetails([FromBody] List<Guid> ids) { ... }
```

**Step 3: 编译验证**
**Step 4: Commit**

### Task 7: MedicalCases 权限策略修复

**Covers:** US-MC-001, US-REG-005, US-REG-006 (D7)

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCasesController.cs` — 创建端点策略
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/RegistrationsController.cs` — start/cancel 策略
- Modify: LocalWebAPI 对应 Controller

**Step 1: MedicalCasesController 创建策略改为 DoctorOnly**

```csharp
[HttpPost]
[Authorize(Policy = PolicyConstants.DoctorOrAdmin)]  // 当前是这个
// 应改为 DoctorOnly（仅医生可创建医案）
```

**Step 2: RegistrationsController start/cancel 改为 DoctorOrAdminOrReceptionist**

```csharp
[HttpPut("{id}/start")]
// 当前策略可能不含 Receptionist，需检查
```

**Step 3: 编译验证**
**Step 4: Commit**

### Task 8: Reports 时间范围参数修复

**Covers:** US-REPORT-001, US-REPORT-002, US-REPORT-003

**Files:**
- Modify: `src/Server/Modules/LYBT.Module.Reports/Application/Queries/GetDailyIncomeQuery.cs`
- Modify: `src/Server/Modules/LYBT.Module.Reports/Application/Queries/GetDailyConsultationsQuery.cs`
- Modify: `src/Server/Modules/LYBT.Module.Reports/Application/Queries/GetDailyHerbUsageQuery.cs`
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/ReportsController.cs`

**Step 1: 给 Query 添加日期参数**

```csharp
public record GetDailyIncomeQuery(DateTime? StartDate = null, DateTime? EndDate = null)
    : IRequest<Result<DailyIncomeDto>>;

// Controller 传递 [FromQuery] DateTime? startDate, DateTime? endDate
```

**Step 2: Handler 实现日期范围过滤**

```csharp
public async Task<Result<DailyIncomeDto>> Handle(GetDailyIncomeQuery request, CancellationToken ct)
{
    var start = request.StartDate ?? DateTime.Today;
    var end = request.EndDate?.AddDays(1) ?? DateTime.Today.AddDays(1);
    // 按范围查询
}
```

**Step 3: 编译验证**
**Step 4: Commit**

---

## [S6] 不在 v1.0 WebAPI 范围内的项目

以下功能属于客户端或非 API 层，不在本次 WebAPI 审计范围：

| US | 原因 |
|----|------|
| US-AUTH-006~007,013 | 安全服务层，非 API 端点 |
| US-USER-011 | 恢复端点（建议实现，已列 Task） |
| US-SHELL-* | Desktop 客户端功能 |
| US-CARD-* | 本地硬件接口 |
| US-PRINT-004 | 打印回写需 PrintController（建议独立 Task） |
| US-REG-008 | SignalR Hub 需独立专项设计 |

---

## 统计

- **缺失端点**: 12 个 (Tasks 1-8 覆盖)
- **权限错配**: 3 个 (Task 7)
- **实现问题**: 5 个 (Task 8 + 其他 Task)
- **总 Tasks**: 8 个，可按依赖顺序执行
