# P-02 + Q-02 性能/代码质量修复 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** ① 医案查询 4 处内存分页改 DB 层分页（消费 Repository 既有分页方法）；② 移除 TryDeserializeDto 双轨验证，6 个 Controller 改强类型绑定。

**Architecture:** P-02 走既有 Repository 分页方法（`QueryPagedAsync`/`GetByPatientIdPagedAsync`）+ 新增 2 个带 Include 的患者维度 DB 分页方法；Q-02 用 `[FromBody] TInputDto` 强类型绑定 + `[ApiController]` 自动 400（`InvalidModelStateResponseFactory` 已配置为 ApiResponse 格式，与旧 ValidationFail 一致）。C# `override` 要求基类 `BaseCrudController.Create/Update` 同步删除（保持非泛型，避免架构测试 A-09/P09c 按名称匹配失败）。

**Tech Stack:** .NET 8 / ASP.NET Core / EF Core 8 / MediatR / System.Text.Json

## Global Constraints

- 0 错误 0 警告：验证用 `dotnet build LYBTZYZS.sln --no-incremental`
- 不要修改任务范围外的文件；worktree 存在他人未提交改动，提交时只 stage 本任务文件
- Desktop 客户端 JSON 契约：camelCase + PropertyNameCaseInsensitive（已验证 `HttpApiClientBase.cs:30`、`ApiService.cs:63`），服务端 `PropertyNameCaseInsensitive=true`（`ServiceCollectionExtensions.cs:171`）→ 强类型绑定兼容
- `SuppressModelStateInvalidFilter=false`（`ServiceCollectionExtensions.cs:194`）→ 模型验证失败自动 400，格式为 ApiResponse
- 完成后在总账 §九 追加一行（日期/决策/理由/决策人）+ Commit SHA

---

## Task 1: P-02 — MedicalCase 查询 DB 层分页

**Covers:** P-02（webapi-deep-analysis-mimo-2026-08-07.md:61-66）

**Files:**
- Modify: `src/Server/Modules/LYBT.Module.MedicalCase/Interfaces/IMedicalCaseRepository.cs`（新增 2 方法）
- Modify: `src/Server/Modules/LYBT.Module.MedicalCase/Repositories/MedicalCaseRepository.cs`（新增 2 方法实现）
- Modify: `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseQueryService.cs`（4 处调用替换）

**Interfaces:**
- Consumes: `PagedResult<T>`（`Items` 为 `List<T>`）、`GetPagedResultAsync`（Extensions/QueryablePagingExtensions.cs:16）、`_mapper.ToDetailDtos(List<MedicalCase>)` / `ToListDtos(List<MedicalCase>)`
- Produces: `IMedicalCaseRepository.GetPatientConsultationsPagedAsync(Guid,int,int,CancellationToken)`、`IMedicalCaseRepository.GetPatientPrescriptionsPagedAsync(Guid,int,int,CancellationToken)`，均返回 `PagedResult<MedicalCase>`

- [ ] **Step 1: Repository 接口新增 2 方法声明**

```csharp
/// <summary>
/// 分页获取患者辨证记录（DB层分页，仅含未删除的Consultation）
/// </summary>
Task<PagedResult<MedicalCase>> GetPatientConsultationsPagedAsync(Guid patientId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

/// <summary>
/// 分页获取患者处方历史（DB层分页，仅含未删除的Prescription）
/// </summary>
Task<PagedResult<MedicalCase>> GetPatientPrescriptionsPagedAsync(Guid patientId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
```

- [ ] **Step 2: Repository 实现（加在 `GetByPatientIdPagedAsync` 之后）**

```csharp
public async Task<PagedResult<MedicalCase>> GetPatientConsultationsPagedAsync(
    Guid patientId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
{
    var query = GetDetailQuery()
        .Where(m => m.PatientId == patientId)
        .Where(m => m.Consultation != null && !m.Consultation.IsDeleted)
        .OrderByDescending(m => m.Consultation!.CreatedAt);

    return await query.GetPagedResultAsync(pageNumber, pageSize, cancellationToken);
}

public async Task<PagedResult<MedicalCase>> GetPatientPrescriptionsPagedAsync(
    Guid patientId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
{
    var query = GetDetailQuery()
        .Where(m => m.PatientId == patientId)
        .Where(m => m.Prescription != null && !m.Prescription.IsDeleted)
        .OrderByDescending(m => m.Prescription!.CreatedAt);

    return await query.GetPagedResultAsync(pageNumber, pageSize, cancellationToken);
}
```

- [ ] **Step 3: Service 替换 4 处**

`SearchMedicalCasesAsync`（原 L218-237）：`QueryAsync` + 内存 Skip/Take → `QueryPagedAsync`：

```csharp
var paged = await _repository.QueryPagedAsync(
    patientName, startDate, endDate, diagnosisKeyword, page, pageSize, cancellationToken);

var dtos = _mapper.ToDetailDtos(paged.Items);

_logger.LogInformation("[SVC] MedicalCase.Search completed - TotalCount={TotalCount} ReturnedCount={ReturnedCount}",
    paged.TotalCount, dtos.Count);

return new PagedResult<MedicalCaseDetailDto>(dtos, paged.TotalCount, page, pageSize);
```

`QueryByPatientAsync`（原 L318-326）：`GetByPatientIdAsync` + 内存分页 → `GetByPatientIdPagedAsync`：

```csharp
var paged = await _repository.GetByPatientIdPagedAsync(
    query.PatientId.Value, query.PageIndex, query.PageSize, cancellationToken);
var dtos = _mapper.ToListDtos(paged.Items);
return new PagedResult<MedicalCaseListDto>(dtos, paged.TotalCount, query.PageIndex, query.PageSize);
```

`GetPatientConsultationsAsync`（原 L438-464）：内存过滤+分页 → 新分页方法，保留映射体：

```csharp
var paged = await _repository.GetPatientConsultationsPagedAsync(patientId, page, pageSize, cancellationToken);

var consultations = paged.Items.Select(mc =>
{
    var dto = _mapper.ToConsultationDetailDto(mc.Consultation!);
    dto.MedicalCaseId = mc.Id;
    dto.PatientId = mc.PatientId;
    dto.UserId = mc.UserId;
    dto.PatientName = mc.PatientName;
    dto.DoctorName = mc.DoctorName;
    dto.CreatedAt = mc.Consultation!.CreatedAt;
    dto.UpdatedAt = mc.Consultation.UpdatedAt;
    dto.CreatedBy = mc.Consultation.CreatedBy;
    return dto;
}).ToList();

return new PagedResult<ConsultationDetailDto>(consultations, paged.TotalCount, page, pageSize);
```

`GetPatientPrescriptionsAsync`（原 L473-500）：同样替换，保留映射体（含 Items/计算字段）。

- [ ] **Step 4: Build 验证** — `dotnet build LYBTZYZS.sln --no-incremental`（0 错误 0 警告）

---

## Task 2: Q-02 — TryDeserializeDto 去重，强类型绑定

**Covers:** Q-02（webapi-deep-analysis-mimo-2026-08-07.md:159-164）

**Files:**
- Modify: `src/Server/Core/LYBT.Infrastructure/Web/BaseCrudController.cs`（删除 `Create`/`Update` virtual）
- Modify: `src/Server/Core/LYBT.Infrastructure/Web/ControllerBaseExtensions.cs`（删除 `TryDeserializeDto`）
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/FormulasController.cs`、`HerbsController.cs`、`PatientsController.cs`、`MedicalCasesController.cs`
- Modify: `src/Server/Modules/LYBT.Module.Users/Controllers/BaseUsersController.cs`
- Modify: `src/Server/Modules/LYBT.Module.Registration/Controllers/BaseRegistrationsController.cs` + `src/Server/Services/LYBT.WebAPI/Controllers/RegistrationsController.cs`

**Interfaces:**
- Consumes: 6 个 InputDto（`FormulaInputDto`/`HerbInputDto`/`PatientInputDto`/`MedicalCaseInputDto`/`UserInputDto`/`RegistrationInputDto`）
- Produces: 无新接口；`BaseCrudController` 不再声明 `Create`/`Update`，各 Controller 自行声明（去 `override`）

- [ ] **Step 1: BaseCrudController 删除 `Create`/`Update` virtual 方法**（L43-55，含两个 `[HttpPost]`/`[HttpPut]` + 抛 NotSupportedException 体）。基类保持非泛型 → 架构测试 A-01/P09c 不受影响。

- [ ] **Step 2: 6 个 Controller 改签名**（每处：删 `override`、`[FromBody] object dto` → `[FromBody] TInputDto input`、删 2 行 TryDeserializeDto + null 检查、`inputDto` 引用改 `input`）：

FormulasController（Create L80 / Update L104）、HerbsController（L75 / L101）、PatientsController（L78 / L102）、MedicalCasesController（L97 / L130，保留 `input.Id = null` 与 `input.Id != id` 判断）、BaseUsersController（L65 / L81）、RegistrationsController（L59，仅 Create）。

示例（FormulasController.Create）：

```csharp
public async Task<IActionResult> Create([FromBody] FormulaInputDto input, CancellationToken ct)
{
    var (operatorId, _, _) = GetOperator();
    var result = await Sender.Send(new CreateFormulaCommand(input, operatorId), ct);
    ...
```

- [ ] **Step 3: BaseRegistrationsController.Update 同步改签名**（L68-70，删 `override`，`object dto` → `RegistrationInputDto dto`，保留 NotFound 语义）：

```csharp
[HttpPut("{id:guid}")]
public Task<IActionResult> Update(Guid id, [FromBody] RegistrationInputDto dto, CancellationToken ct)
    => Task.FromResult<IActionResult>(NotFound("挂号不支持更新操作"));
```

- [ ] **Step 4: ControllerBaseExtensions 删除 `TryDeserializeDto`**（L150-180，含 `DTO 反序列化 + 验证` 注释块）。

- [ ] **Step 5: Build 验证** — `dotnet build LYBTZYZS.sln --no-incremental`（0 错误 0 警告）+ 架构测试 `dotnet test tests/LYBT.Tests.Architecture/`

---

## 收尾

- [ ] **总账 §九 追加行**（`docs/03-architecture/13-project-master-plan.md`）：记录 P-02 + Q-02 完成 + Commit SHA
- [ ] **提交**：`git add` 本任务涉及文件（明确按文件名，不含 worktree 他人改动），commit 一条
