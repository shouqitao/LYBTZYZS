# Controller 继承体系简化 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 简化 Controller 继承体系，消除路由冲突和过度抽象。

**Architecture:** 两个改动：(1) 合并 MedicalCaseProcessingController 到 MedicalCasesController；(2) 将 BaseCrudController 的 ToggleStatus/Restore 从 abstract 改为 virtual（默认 throw NotSupportedException），消除中间层 BaseSoftDeleteCrudController 的需要。

**Tech Stack:** ASP.NET Core, C#, MediatR

## Global Constraints

- 中文业务注释，英文标识符
- `dotnet build` 必须通过
- 最小改动原则

---

### Task 1: 合并 MedicalCaseProcessingController 到 MedicalCasesController

**Covers:** [S2.1]

**Files:**
- Delete: `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseProcessingController.cs`
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCasesController.cs`

**Interfaces:**
- Consumes: `MedicalCaseProcessingController` 的 4 个端点方法（UpdateStatus, CloseMedicalCase, Suspend, CancelMedicalCase）
- Produces: MedicalCasesController 包含所有端点

- [ ] **Step 1: 将 MedicalCaseProcessingController 的 4 个方法添加到 MedicalCasesController**

在 `MedicalCasesController.cs` 的 `#endregion` 之前添加以下代码：

```csharp
#region 状态流转（从 MedicalCaseProcessingController 合入）

/// <summary>
/// 更新医案状态
/// </summary>
[HttpPut("{id}/status")]
[ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 200)]
[ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 404)]
[ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 422)]
public async Task<IActionResult> UpdateStatus(
    Guid id,
    [FromBody] MedicalCaseStatusInputDto request, CancellationToken ct)
{
    var (operatorId, _, operatorRole) = GetOperator();
    var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

    if (request.Status == MedicalCaseStatus.Completed)
    {
        var completeResult = await Sender.Send(new CompleteMedicalCaseCommand(id, operatorId, isAdmin), ct);
        if (!completeResult.IsSuccess)
            return NotFound(completeResult.Error ?? "医案不存在");
        return Success("医案已完成");
    }

    var result = await Sender.Send(new UpdateMedicalCaseStatusCommand(id, request.Status, operatorId, isAdmin), ct);
    if (!result.IsSuccess)
        return NotFound(result.Error ?? "医案不存在");

    _logger.LogInformation("医案状态更新成功，MedicalCaseId: {Id}, NewStatus: {Status}", id, request.Status);
    return Success(result.Value!, "状态更新成功");
}

/// <summary>
/// 关闭医案（直接标记为Completed）
/// </summary>
[HttpPut("{id}/close")]
[ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 200)]
[ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 404)]
public async Task<IActionResult> CloseMedicalCase(Guid id, CancellationToken ct)
{
    var (operatorId, _, operatorRole) = GetOperator();
    var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;
    var result = await Sender.Send(new CompleteMedicalCaseCommand(id, operatorId, isAdmin), ct);
    if (!result.IsSuccess)
        return NotFound(result.Error ?? "医案不存在");

    _logger.LogInformation("医案关闭，MedicalCaseId: {Id}", id);
    return Success("医案已关闭");
}

/// <summary>
/// 挂起医案
/// </summary>
[HttpPut("{id}/suspend")]
[ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 200)]
[ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 404)]
[ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 422)]
[ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 403)]
public async Task<IActionResult> Suspend(
    Guid id,
    [FromBody] ConsultationInputDto? request = null, CancellationToken ct = default)
{
    var (operatorId, _, operatorRole) = GetOperator();
    var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

    var result = await Sender.Send(new SuspendMedicalCaseCommand(id, operatorId, isAdmin), ct);
    if (!result.IsSuccess)
        return NotFound(result.Error ?? "医案不存在");

    _logger.LogInformation("医案暂存成功，MedicalCaseId: {Id}", id);
    return Success("医案已暂存");
}

/// <summary>
/// 取消医案（统一为软删除 + 审计日志）
/// </summary>
[HttpPut("{id}/cancel")]
[ProducesResponseType(204)]
[ProducesResponseType(typeof(ApiResponse), 404)]
[ProducesResponseType(typeof(ApiResponse), 403)]
public async Task<IActionResult> CancelMedicalCase(
    Guid id,
    [FromBody] CancelMedicalCaseRequestDto? request = null, CancellationToken ct = default)
{
    var (operatorId, _, operatorRole) = GetOperator();
    var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

    var result = await Sender.Send(new CancelMedicalCaseCommand(id, operatorId, isAdmin, request?.Reason), ct);
    if (!result.IsSuccess)
        return NotFound(result.Error ?? "医案不存在");

    _logger.LogInformation("医案取消成功(软删除)，MedicalCaseId: {Id}", id);
    return Success(true, "医案已取消");
}

#endregion
```

还需要在文件顶部添加 `using LYBT.Module.MedicalCases.Application.Commands;` 中的 `UpdateMedicalCaseStatusCommand` — 检查是否已包含该 using。MedicalCaseProcessingController 引用了 `MedicalCaseStatusInputDto`，确认 `LYBT.Shared.Models.Contracts.MedicalCase` using 已存在。

- [ ] **Step 2: 删除 MedicalCaseProcessingController.cs**

```bash
rm src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseProcessingController.cs
```

- [ ] **Step 3: 编译验证**

```bash
dotnet build LYBTZYZS.sln
```

Expected: BUILD SUCCESSFUL

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "refactor(MedicalCase): merge MedicalCaseProcessingController into MedicalCasesController"
```

---

### Task 2: 简化 BaseCrudController — ToggleStatus/Restore 改为 virtual

**Covers:** [S2.2]

**Files:**
- Modify: `src/Server/Core/LYBT.Infrastructure/Web/BaseCrudController.cs`

**Interfaces:**
- Consumes: 无
- Produces: BaseCrudController 的 ToggleStatus/Restore 不再是 abstract，改为 virtual（默认 throw NotSupportedException）

**设计决策**：不创建 BaseSoftDeleteCrudController 中间层。将 ToggleStatus/Restore 从 abstract 改为 virtual（默认 throw），继承者按需 override。更简洁。

- [ ] **Step 1: 修改 BaseCrudController.cs**

将 ToggleStatus 和 Restore 方法从 abstract 改为 virtual，body 改为 throw：

```csharp
/// <summary>
/// 切换状态（启用/禁用）— 默认不支持，子类按需 override
/// </summary>
[HttpPost("{id:guid}/toggle-status")]
public virtual async Task<IActionResult> ToggleStatus(Guid id, CancellationToken ct)
{
    throw new NotSupportedException("此资源不支持切换状态操作");
}

/// <summary>
/// 恢复已删除的资源 — 默认不支持，子类按需 override
/// </summary>
[HttpPost("{id:guid}/restore")]
public virtual async Task<IActionResult> Restore(Guid id, CancellationToken ct)
{
    throw new NotSupportedException("此资源不支持恢复操作");
}
```

删除抽象方法区域中的：
```csharp
protected abstract IRequest<Result<TDetailDto>> CreateToggleStatusCommand(Guid id, Guid operatorId);
protected abstract IRequest<Result<TDetailDto>> CreateRestoreCommand(Guid id, Guid operatorId);
```

- [ ] **Step 2: 编译验证**

```bash
dotnet build LYBTZYZS.sln
```

Expected: 编译错误 — 继承者实现了已删除的抽象方法。下一步修复。

---

### Task 3: 修复所有继承者的编译错误

**Covers:** [S2.2]

**Files:**
- Modify: `src/Server/Modules/LYBT.Module.MedicalCase/Controllers/BaseMedicalCasesController.cs`
- Modify: `src/Server/Modules/LYBT.Module.Registration/Controllers/BaseRegistrationsController.cs`
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/HerbsController.cs`
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/FormulasController.cs`
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/PatientsController.cs`
- Modify: `src/Client/Desktop/LocalWebAPI/Controllers/HerbsController.cs`
- Modify: `src/Client/Desktop/LocalWebAPI/Controllers/FormulasController.cs`
- Modify: `src/Client/Desktop/LocalWebAPI/Controllers/PatientsController.cs`

**Interfaces:**
- Consumes: BaseCrudController 的新 virtual ToggleStatus/Restore（默认 throw）
- Produces: 所有继承者编译通过

#### 3a. BaseMedicalCasesController — 删除 ToggleStatus/Restore override 和抽象方法实现

**当前**（第 26-35 行）：
```csharp
#region Override ToggleStatus/Restore (医案不支持)

[HttpPost("{id:guid}/toggle-status")]
public override Task<IActionResult> ToggleStatus(Guid id, CancellationToken ct)
    => Task.FromResult<IActionResult>(NotFound("医案不支持切换状态"));

[HttpPost("{id:guid}/restore")]
public override Task<IActionResult> Restore(Guid id, CancellationToken ct)
    => Task.FromResult<IActionResult>(NotFound("医案不支持恢复操作"));

#endregion
```

**改为**：删除整个 `#region Override ToggleStatus/Restore` 块（第 26-36 行）。基类的 virtual 方法已默认 throw，不再需要 override。

同时删除抽象方法实现中的：
```csharp
protected override IRequest<Result<MedicalCaseDetailDto>> CreateToggleStatusCommand(Guid id, Guid operatorId)
    => throw new NotSupportedException("医案不支持切换状态");

protected override IRequest<Result<MedicalCaseDetailDto>> CreateRestoreCommand(Guid id, Guid operatorId)
    => throw new NotSupportedException("医案不支持恢复操作");
```

#### 3b. BaseRegistrationsController — 删除 5 个 override 和抽象方法实现

**当前**（第 67-88 行，Override 不支持的操作）：
```csharp
#region Override 不支持的操作（挂号不支持 Update/Delete/ToggleStatus/Restore/BatchDelete）

[HttpPut("{id:guid}")]
public override Task<IActionResult> Update(...)
[HttpDelete("{id:guid}")]
public override Task<IActionResult> Delete(...)
[HttpPost("{id:guid}/toggle-status")]
public override Task<IActionResult> ToggleStatus(...)
[HttpPost("{id:guid}/restore")]
public override Task<IActionResult> Restore(...)
[HttpPost("batch-delete")]
public override Task<IActionResult> BatchDelete(...)

#endregion
```

**改为**：删除 Update/Delete/ToggleStatus/Restore 的 override（这些方法在基类已改为 virtual 默认 throw）。BatchDelete 保留 override（基类仍为 abstract）。

删除抽象方法实现中的：
```csharp
protected override IRequest<Result<RegistrationDetailDto>> CreateUpdateCommand(...)
    => throw new NotSupportedException("挂号不支持更新操作");
protected override IRequest<Result> CreateDeleteCommand(...)
    => throw new NotSupportedException("挂号不支持删除操作");
protected override IRequest<Result<RegistrationDetailDto>> CreateToggleStatusCommand(...)
    => throw new NotSupportedException("挂号不支持切换状态");
protected override IRequest<Result<RegistrationDetailDto>> CreateRestoreCommand(...)
    => throw new NotSupportedException("挂号不支持恢复操作");
```

BatchDelete 的 CreateBatchDeleteCommand 保留（基类仍为 abstract）。

#### 3c. HerbsController (WebAPI + LocalWebAPI) — 无需修改

HerbsController 没有 override ToggleStatus/Restore，基类的 virtual 默认 throw 已满足。但 Herbs 需要这两个功能——检查是否有 CreateToggleStatusCommand 等抽象方法实现。

**实际**：HerbsController 直接继承 BaseCrudController，实现了所有 7 个抽象方法（包括 CreateToggleStatusCommand/CreateRestoreCommand）。ToggleStatus/Restore 从 abstract 变为 virtual 后，这些实现方法不再被调用（基类不再调用它们），但代码仍能编译。**无需修改**。

#### 3d. FormulasController (WebAPI + LocalWebAPI) — 同 3c，无需修改

#### 3e. PatientsController (WebAPI + LocalWebAPI) — 同 3c，无需修改

- [ ] **Step 1: 修改 BaseMedicalCasesController**

删除：
1. `#region Override ToggleStatus/Restore` 整个块（第 26-36 行）
2. `CreateToggleStatusCommand` 和 `CreateRestoreCommand` 方法（第 68-72 行）

- [ ] **Step 2: 修改 BaseRegistrationsController**

删除：
1. Update/Delete/ToggleStatus/Restore 的 override 方法（第 69-83 行，保留 BatchDelete override）
2. `CreateUpdateCommand`/`CreateDeleteCommand`/`CreateToggleStatusCommand`/`CreateRestoreCommand`（第 101-111 行）

- [ ] **Step 3: 编译验证**

```bash
dotnet build LYBTZYZS.sln
```

Expected: BUILD SUCCESSFUL

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "refactor(BaseCrudController): convert ToggleStatus/Restore from abstract to virtual with default throw"
```

---

### Task 4: 最终验证

**Covers:** [S2.1], [S2.2]

- [ ] **Step 1: 全量编译**

```bash
dotnet build LYBTZYZS.sln
```

Expected: BUILD SUCCESSFUL

- [ ] **Step 2: 运行测试**

```bash
dotnet test tests/LYBT.Tests.Server/ --no-build
```

Expected: 所有测试通过（无新增测试，回归验证）
