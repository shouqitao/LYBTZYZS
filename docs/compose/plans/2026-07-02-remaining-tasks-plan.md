# 剩余任务实施计划 (Phase 2)

> [!NOTE]
> 此文档可能不反映当前实现。
> 请参阅最终报告了解最新状态：
> [Final Report](../reports/2026-07-02-remaining-gaps-report.md)

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Complete dead code cleanup, finish test coverage, and implement v1.0 quick-win features.

**Architecture:** 3 batches — (1) Desktop LocalWebAPI migration + dead service removal, (2) remaining handler tests, (3) v1.0 features (PRINT-004, AUTH-013, MC-017). Each batch produces independently testable deliverables.

**Tech Stack:** ASP.NET Core MediatR CQRS, EF Core, EPPlus, xUnit, WebApplicationFactory

## Global Constraints

- 所有新端点使用 MediatR ISender 分发
- 响应格式统一：`ApiResponse<T>` 信封
- 编码规范：中文业务注释，英文标识符
- 包版本统一在 `Directory.Packages.props`
- 本地/远程双模式：WebAPI (5000) + LocalWebAPI (5300)

---

### Task 1: Desktop LocalWebAPI Formula 端点迁移

**Covers:** US-FORM-007, US-FORM-008, 清理 IFormulaService

**Files:**
- Modify: `src/Client/Desktop/LocalWebAPI/Controllers/FormulasController.cs`
- Verify: `src/Server/Modules/LYBT.Module.Formula/FormulaModule.cs` (DI)

**Interfaces:**
- Consumes: `GetPendingValidationQuery`, `ValidateFormulaHerbCommand`, `GetFormulaQuery` (already exist in Module.Formula)
- Produces: Desktop FormulasController 无 IFormulaService 依赖

- [ ] **Step 1: 读取当前 Desktop FormulasController**

Read `src/Client/Desktop/LocalWebAPI/Controllers/FormulasController.cs`. 找到所有 `IFormulaService` 注入和调用点。

Expected: 找到 `_formulaService.GetPendingValidationFormulasAsync()` 和 `_formulaService.ValidateFormulaHerbAsync()` 调用。

- [ ] **Step 2: 迁移 GetPendingValidation**

将 `_formulaService.GetPendingValidationFormulasAsync()` 替换为 `_sender.Send(new GetPendingValidationQuery())`。

```csharp
// Before:
var formulas = await _formulaService.GetPendingValidationFormulasAsync();

// After:
var result = await _sender.Send(new GetPendingValidationQuery());
if (!result.IsSuccess || result.Value == null)
    return BusinessFail(result.Error ?? "查询失败");
var formulas = result.Value;
```

- [ ] **Step 3: 迁移 ValidateHerb**

将 `_formulaService.ValidateFormulaHerbAsync()` 替换为 `_sender.Send(new ValidateFormulaHerbCommand(...))`。

- [ ] **Step 4: 迁移 Ownership Check (GetByIdAsync)**

Update/Delete/ToggleStatus 中的 `_formulaService.GetByIdAsync()` 替换为 `_sender.Send(new GetFormulaQuery(id))` + 内联 ValidateOwnership。

- [ ] **Step 5: 移除 IFormulaService 依赖**

从构造函数中移除 `IFormulaService` 注入。

- [ ] **Step 6: 编译验证**

Run: `dotnet build src/Client/Desktop/LocalWebAPI/LYBT.LocalWebAPI.csproj`
Expected: 0 errors

---

### Task 2: Desktop LocalWebAPI Patient 端点迁移

**Covers:** US-PAT-009 (GetByIdNumber legacy endpoint), 清理 IPatientService

**Files:**
- Modify: `src/Client/Desktop/LocalWebAPI/Controllers/PatientsController.cs`

**Interfaces:**
- Consumes: `GetPatientQuery` or new query for ID number search
- Produces: PatientsController 无 IPatientService 依赖

- [ ] **Step 1: 创建 SearchPatientByIdNumberQuery**

```csharp
// src/Server/Modules/LYBT.Module.Patients/Application/Queries/SearchPatientByIdNumberQuery.cs
public record SearchPatientByIdNumberQuery(string IdNumber) : IRequest<Result<PatientDetailDto>>;

// Handler: 查询 Patients WHERE IdNumber = input AND !IsDeleted
```

- [ ] **Step 2: 迁移 GetByIdNumber 端点**

Replace `_patientService.SearchAsync(idNumber)` with `_sender.Send(new SearchPatientByIdNumberQuery(idNumber))`.

- [ ] **Step 3: 移除 IPatientService 依赖**

- [ ] **Step 4: 编译验证**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

---

### Task 3: 删除 IFormulaService + IPatientService

**Covers:** 死代码清理

**Files:**
- Delete: `src/Server/Modules/LYBT.Module.Formula/Interfaces/IFormulaService.cs`
- Delete: `src/Server/Modules/LYBT.Module.Formula/Services/FormulaService.cs`
- Modify: `src/Server/Modules/LYBT.Module.Formula/FormulaModule.cs` — remove DI registration
- Delete: `src/Server/Modules/LYBT.Module.Patients/Interfaces/IPatientService.cs` (if exists)
- Delete: `src/Server/Modules/LYBT.Module.Patients/Services/PatientService.cs` (if exists)

- [ ] **Step 1: 搜索所有引用**

Run grep for `IFormulaService` and `IPatientService` across all server + Desktop projects. 确认无引用后再删除。

- [ ] **Step 2: 删除文件**

- [ ] **Step 3: 清理 DI 注册**

从 FormulaModule.cs 和 PatientsModule.cs 中移除对应的 AddScoped/AddSingleton 注册。

- [ ] **Step 4: 编译验证**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

---

### Task 4: MedicalCase 剩余 Handler 测试

**Covers:** US-MC-010 (Suspend), US-MC-011 (Complete), US-MC-016 (Permissions), US-MC-018 (BatchDetails)

**Files:**
- Modify: `tests/LYBT.Tests.Server/Integration/MedicalCases/US_MC_StateAndQueryTests.cs` (already exists with 8 tests)
- Add: 6 more test methods to cover remaining handlers

**Interfaces:**
- Consumes: Existing test infrastructure (WebApplicationFactory, auth helpers, data builders)

- [ ] **Step 1: 补充 Suspend 测试**

```csharp
[Fact]
public async Task SuspendMedicalCase_SetsSuspendedStatus()
{
    // 1. Create active case via POST /medicalcases
    // 2. PUT /medicalcases/{id}/suspend
    // 3. GET /medicalcases/{id} → verify caseStatus == "Suspended"
}
```

- [ ] **Step 2: 补充 Complete 测试**

```csharp
[Fact]
public async Task CompleteMedicalCase_SetsCompletedStatus()
{
    // 1. Create case with valid Consultation + Prescription
    // 2. PUT /medicalcases/{id}/close
    // 3. GET /medicalcases/{id} → verify caseStatus == "Completed"
}

[Fact]
public async Task CompleteMedicalCase_Returns422_WhenMissingDiagnosis()
{
    // 1. Create case without TcmDiagnosis
    // 2. PUT /medicalcases/{id}/close → expect 422
}
```

- [ ] **Step 3: 补充 Permissions 测试**

```csharp
[Fact]
public async Task GetPermissions_ReturnsCanEdit_ForOwner()
{
    // 1. Create case as doctor
    // 2. GET /medicalcases/{id}/permissions
    // 3. Verify canEdit == true
}
```

- [ ] **Step 4: 补充 BatchDetails 测试**

```csharp
[Fact]
public async Task GetBatchDetails_ReturnsMultipleCases()
{
    // 1. Create 3 cases
    // 2. POST /medicalcases/batch-details with 3 IDs
    // 3. Verify 3 items returned
}
```

- [ ] **Step 5: 编译验证**

Run: `dotnet build tests/LYBT.Tests.Server/`
Expected: 0 errors

---

### Task 5: US-PRINT-004 打印记录回写

**Covers:** US-PRINT-004

**Files:**
- Create: `src/Server/Core/LYBT.Entities/MedicalCases/MedicalCasePrintLog.cs`
- Modify: `src/Server/Core/LYBT.Entities/MedicalCases/MedicalCaseModel.cs` (add IsPrinted/PrintCount/LastPrintedAt/PrintVersion if missing)
- Modify: `src/Server/Core/LYBT.Infrastructure/Data/AppDbContext.cs` (add DbSet)
- Create: `src/Server/Modules/LYBT.Module.MedicalCase/Application/Commands/RecordPrintCommand.cs`
- Create: `src/Server/Modules/LYBT.Module.MedicalCase/Application/Commands/RecordPrintCommandHandler.cs`
- Modify: MedicalCasesController (add PUT /{id}/print-completed endpoint)
- Modify: LocalWebAPI MedicalCasesController (mirror endpoint)

**Interfaces:**
- Produces: `RecordPrintCommand(Guid CaseId, PrintType Type, string? PrinterName)` → `Result<bool>`

- [ ] **Step 1: 创建 MedicalCasePrintLog 实体**

```csharp
// src/Server/Core/LYBT.Entities/MedicalCases/MedicalCasePrintLog.cs
namespace LYBT.Entities.MedicalCases;

public class MedicalCasePrintLog : BaseEntity
{
    public Guid MedicalCaseId { get; set; }
    public int PrintType { get; set; } // 0=Prescription
    public int PrintVersion { get; set; }
    public string? PrinterName { get; set; }
    public string? PrintedBy { get; set; }
    public DateTime PrintedAt { get; set; }
}
```

- [ ] **Step 2: 添加 DbSet**

```csharp
// AppDbContext.cs
public DbSet<MedicalCasePrintLog> MedicalCasePrintLogs { get; set; }
```

- [ ] **Step 3: 创建 RecordPrintCommand + Handler**

Handler logic:
1. Find MedicalCase by ID
2. Set IsPrinted=true, PrintCount++, LastPrintedAt=now, PrintVersion=PrintVersion
3. Create MedicalCasePrintLog record
4. SaveChangesAsync

- [ ] **Step 4: 添加 Controller 端点**

```csharp
[HttpPut("{id:guid}/print-completed")]
public async Task<IActionResult> RecordPrint(Guid id, [FromBody] RecordPrintRequest request)
{
    var (operatorId, _, _) = GetOperator();
    var result = await _sender.Send(new RecordPrintCommand(id, request.PrintType, request.PrinterName, operatorId));
    // ...
}
```

- [ ] **Step 5: 数据库迁移**

Run: `dotnet ef migrations add AddMedicalCasePrintLog --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI`

- [ ] **Step 6: 编译验证**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

---

### Task 6: US-AUTH-013 本地限流

**Covers:** US-AUTH-013

**Files:**
- Modify: `src/Client/Desktop/LocalWebAPI/LocalWebApiProgram.cs` (add rate limiting)
- Create: `src/Client/Desktop/LocalWebAPI/Middleware/LocalRateLimitMiddleware.cs` (if needed)

**Interfaces:**
- Consumes: ASP.NET Core RateLimiting

- [ ] **Step 1: 检查 LocalWebAPI 是否已有 RateLimiting**

Read `src/Client/Desktop/LocalWebAPI/LocalWebApiProgram.cs`. 检查是否已配置 `AddRateLimiter`。

- [ ] **Step 2: 添加固定窗口限流**

```csharp
// 在 LocalWebApiProgram.cs 中
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("LocalLogin", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
});

app.UseRateLimiter();
```

- [ ] **Step 3: 在 LocalWebAPI AuthController 登录端点应用**

```csharp
[HttpPost("login")]
[EnableRateLimiting("LocalLogin")]
public async Task<IActionResult> Login(...) { ... }
```

- [ ] **Step 4: 编译验证**

Run: `dotnet build src/Client/Desktop/LocalWebAPI/LYBT.LocalWebAPI.csproj`
Expected: 0 errors

---

### Task 7: US-MC-017 AuditLog 实体 + 增强审计

**Covers:** US-MC-017

**Files:**
- Create: `src/Server/Core/LYBT.Entities/MedicalCases/MedicalCaseAuditLog.cs`
- Modify: `src/Server/Core/LYBT.Infrastructure/Data/AppDbContext.cs` (add DbSet)
- Modify: `src/Server/Modules/LYBT.Module.MedicalCase/Application/Queries/GetMedicalCaseAuditLogsQueryHandler.cs` (enhance to use real entity)

**Interfaces:**
- Produces: AuditLog entity with field-level diff support

- [ ] **Step 1: 创建 MedicalCaseAuditLog 实体**

```csharp
// src/Server/Core/LYBT.Entities/MedicalCases/MedicalCaseAuditLog.cs
namespace LYBT.Entities.MedicalCases;

public class MedicalCaseAuditLog : BaseEntity
{
    public Guid MedicalCaseId { get; set; }
    public int OperationType { get; set; } // 0=Create, 1=Update, 2=StatusChange, 3=SoftDelete
    public string? PerformedBy { get; set; }
    public string? PerformedByName { get; set; }
    public string? Reason { get; set; }
    public string? OldValues { get; set; } // JSON
    public string? NewValues { get; set; } // JSON
    public string? ChangedFields { get; set; } // JSON array
}
```

- [ ] **Step 2: 添加 DbSet + 增强 Handler**

将现有的合成审计日志替换为从 MedicalCaseAuditLog 表查询。

- [ ] **Step 3: 数据库迁移**

- [ ] **Step 4: 编译验证**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

---

### Task 8: ErrorCode 对齐 + 清理

**Covers:** ErrorCode 健康度

**Files:**
- Modify: `src/Server/Modules/LYBT.Module.Registration/Application/Commands/*.cs` (4 handlers)
- Modify: `src/Shared/LYBT.Shared.Primitives/ErrorCodes/ErrorCode.cs` (remove unused)

**Interfaces:**
- Consumes: ErrorCode enum + ErrorCodeExtensions

- [ ] **Step 1: Registration handlers 使用 ErrorCode**

修改 CreateRegistrationCommandHandler、StartVisitCommandHandler、CancelRegistrationCommandHandler、QuickVisitCommandHandler，用 `ErrorCode.RegistrationNotFound` 等替代字符串错误。

- [ ] **Step 2: 删除完全未使用的 ErrorCode**

从 ErrorCode 枚举中删除 36 个从未被 handler 返回的错误码。

- [ ] **Step 3: 编译验证**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

---

### Task 9: 架构测试 + 集成测试验证

**Covers:** 全局验证

- [ ] **Step 1: 运行架构测试**

Run: `dotnet test tests/LYBT.Tests.Architecture/`
Expected: 81+ pass

- [ ] **Step 2: 运行集成测试**

Run: `dotnet test tests/LYBT.Tests.Desktop/ --filter "FullyQualifiedName~WorkflowIntegrationTests"`
Expected: 3+ pass

- [ ] **Step 3: 全量构建**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

---

## 统计

| Batch | Tasks | 工作量估计 |
|-------|-------|-----------|
| Desktop 迁移 + 死代码 | T1-T3 | 1 天 |
| 测试补全 | T4 | 0.5 天 |
| v1.0 快速功能 | T5-T8 | 2-3 天 |
| 验证 | T9 | 0.5 天 |
| **合计** | **9 tasks** | **4-5 天** |
