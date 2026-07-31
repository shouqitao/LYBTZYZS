# Controller 基类下沉实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 5 对 Controller（Patients/Herbs/Formulas/Registrations/MedicalCases）的重复 CRUD 代码合并到共享基类，Server 和 LocalWebAPI 的 Controller 继承它。

**Architecture:** 创建 `BaseCrudController<TListDto, TDetailDto, TInputDto, TQuery, TCommand>` 泛型基类，提供统一的 CRUD 方法。Server 版本通过 override 添加 OutputCache/RateLimiting/Ownership 检查，LocalWebAPI 版本保持简化。

**Tech Stack:** C# 12, .NET 8, ASP.NET Core, MediatR, CommunityToolkit.Mvvm

## Global Constraints

- 所有 Controller 必须继承 `BaseApiController` 或新的 `BaseCrudController`
- Server 版本必须保持现有 API 行为（OutputCache、RateLimiting、Ownership 检查）
- LocalWebAPI 版本保持简化（无装饰器、无所有权检查）
- 每个 Controller 重构后必须通过 `dotnet test`
- 特殊端点（如 `GetByIdNumber`、`ValidateHerb`）保留在子类中

---

## File Structure

| 文件 | 职责 |
|------|------|
| `src/Server/Core/LYBT.Infrastructure/Web/BaseCrudController.cs` | 新建：泛型 CRUD 基类 |
| `src/Server/Services/LYBT.WebAPI/Controllers/PatientsController.cs` | 重构：继承 BaseCrudController |
| `src/Server/Services/LYBT.WebAPI/Controllers/HerbsController.cs` | 重构：继承 BaseCrudController |
| `src/Server/Services/LYBT.WebAPI/Controllers/FormulasController.cs` | 重构：继承 BaseCrudController |
| `src/Server/Services/LYBT.WebAPI/Controllers/RegistrationsController.cs` | 重构：继承 BaseCrudController |
| `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCasesController.cs` | 重构：继承 BaseCrudController |
| `src/Client/Desktop/LocalWebAPI/Controllers/PatientsController.cs` | 重构：继承 BaseCrudController |
| `src/Client/Desktop/LocalWebAPI/Controllers/HerbsController.cs` | 重构：继承 BaseCrudController |
| `src/Client/Desktop/LocalWebAPI/Controllers/FormulasController.cs` | 重构：继承 BaseCrudController |
| `src/Client/Desktop/LocalWebAPI/Controllers/RegistrationsController.cs` | 重构：继承 BaseCrudController |
| `src/Client/Desktop/LocalWebAPI/Controllers/MedicalCasesController.cs` | 重构：继承 BaseCrudController |

---

## Task 1: 创建 BaseCrudController 泛型基类

**Covers:** [S1, S2]

**Files:**
- Create: `src/Server/Core/LYBT.Infrastructure/Web/BaseCrudController.cs`

**Interfaces:**
- Consumes: `BaseApiController`, `ISender`, `MediatR.IRequest<T>`
- Produces: `BaseCrudController<TListDto, TDetailDto, TInputDto, TQuery, TCommand>`

- [ ] **Step 1: 创建 BaseCrudController 基类**

```csharp
using MediatR;
using LYBT.Infrastructure.Web;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Web;

/// <summary>
/// 通用 CRUD Controller 基类
/// 提供统一的分页查询、详情、创建、更新、删除、切换状态、恢复、批量删除等方法
/// </summary>
/// <typeparam name="TListDto">列表 DTO 类型</typeparam>
/// <typeparam name="TDetailDto">详情 DTO 类型（必须实现 IAuditable）</typeparam>
/// <typeparam name="TInputDto">输入 DTO 类型</typeparam>
/// <typeparam name="TQuery">查询请求类型</typeparam>
/// <typeparam name="TCommand">命令请求类型</typeparam>
public abstract class BaseCrudController<TListDto, TDetailDto, TInputDto, TQuery, TCommand> 
    : BaseApiController
    where TQuery : IRequest<Result<PagedResult<TListDto>>>
    where TCommand : IRequest<Result<TDetailDto>>
{
    private readonly ISender _sender;

    protected BaseCrudController(ISender sender, ILogger logger)
        : base(logger)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <summary>
    /// 获取分页列表
    /// </summary>
    [HttpGet]
    public virtual async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? keyword = null,
        CancellationToken ct = default)
    {
        if (ValidatePagination(page, pageSize) is { } error) return error;

        var query = CreateGetListQuery(page, pageSize, keyword);
        var result = await _sender.Send(query, ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "查询失败");
        return SuccessPaged(result.Value, "查询成功");
    }

    /// <summary>
    /// 根据 ID 获取详情
    /// </summary>
    [HttpGet("{id:guid}")]
    public virtual async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        if (ValidateGuid(id, "ID") is { } error) return error;

        var query = CreateGetByIdQuery(id);
        var result = await _sender.Send(query, ct);
        if (!result.IsSuccess || result.Value == null)
            return NotFound(result.Error ?? "资源不存在");
        return Success(result.Value, "查询成功");
    }

    /// <summary>
    /// 创建资源
    /// </summary>
    [HttpPost]
    public virtual async Task<IActionResult> Create([FromBody] TInputDto dto, CancellationToken ct)
    {
        var (operatorId, _, _) = GetOperator();
        var command = CreateCreateCommand(dto, operatorId);
        var result = await _sender.Send(command, ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "创建失败");
        LogOperation("创建成功", result.Value, result.Value.Id);
        return CreatedAtAction(nameof(GetById),
            new { id = result.Value.Id },
            result.Value);
    }

    /// <summary>
    /// 更新资源
    /// </summary>
    [HttpPut("{id:guid}")]
    public virtual async Task<IActionResult> Update(Guid id, [FromBody] TInputDto dto, CancellationToken ct)
    {
        if (ValidateGuid(id, "ID") is { } error) return error;

        var (operatorId, _, _) = GetOperator();
        var command = CreateUpdateCommand(id, dto, operatorId);
        var result = await _sender.Send(command, ct);
        if (!result.IsSuccess || result.Value == null)
        {
            if (result.Error?.Contains("不存在") == true)
                return NotFound(result.Error);
            return BusinessFail(result.Error ?? "更新失败");
        }
        LogOperation("更新成功", result.Value, id);
        return Success(result.Value, "更新成功");
    }

    /// <summary>
    /// 删除资源（软删除）
    /// </summary>
    [HttpDelete("{id:guid}")]
    public virtual async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (ValidateGuid(id, "ID") is { } error) return error;

        var (operatorId, _, _) = GetOperator();
        var command = CreateDeleteCommand(id, operatorId);
        var result = await _sender.Send(command, ct);
        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("不存在") == true)
                return NotFound(result.Error);
            return BusinessFail(result.Error ?? "删除失败");
        }
        LogOperation("删除成功", null, id);
        return Success("删除成功");
    }

    /// <summary>
    /// 切换状态（启用/禁用）
    /// </summary>
    [HttpPost("{id:guid}/toggle-status")]
    public virtual async Task<IActionResult> ToggleStatus(Guid id, CancellationToken ct)
    {
        if (ValidateGuid(id, "ID") is { } error) return error;

        var (operatorId, _, _) = GetOperator();
        var command = CreateToggleStatusCommand(id, operatorId);
        var result = await _sender.Send(command, ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "切换状态失败");
        LogOperation("切换状态", new { NewStatus = result.Value.Status }, id);
        return Success(result.Value, $"状态已切换");
    }

    /// <summary>
    /// 恢复已删除的资源
    /// </summary>
    [HttpPost("{id:guid}/restore")]
    public virtual async Task<IActionResult> Restore(Guid id, CancellationToken ct)
    {
        if (ValidateGuid(id, "ID") is { } error) return error;

        var (operatorId, _, _) = GetOperator();
        var command = CreateRestoreCommand(id, operatorId);
        var result = await _sender.Send(command, ct);
        if (!result.IsSuccess || result.Value == null)
        {
            if (result.Error?.Contains("未被删除") == true)
                return BusinessFail(result.Error);
            return NotFound(result.Error ?? "资源不存在");
        }
        LogOperation("恢复成功", result.Value, id);
        return Success(result.Value, "恢复成功");
    }

    /// <summary>
    /// 批量删除
    /// </summary>
    [HttpPost("batch-delete")]
    public virtual async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto dto, CancellationToken ct)
    {
        if (dto.Ids == null || dto.Ids.Count == 0)
            return ValidationFail("请至少选择一个资源");

        var (operatorId, _, _) = GetOperator();
        var command = CreateBatchDeleteCommand(dto.Ids, operatorId);
        var result = await _sender.Send(command, ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "批量删除失败");
        LogOperation("批量删除", new { Ids = dto.Ids, Result = result.Value.Message }, null);
        return Success(result.Value, result.Value.Message);
    }

    #region 抽象方法 - 子类必须实现

    /// <summary>
    /// 创建分页查询请求
    /// </summary>
    protected abstract TQuery CreateGetListQuery(int page, int pageSize, string? keyword);

    /// <summary>
    /// 创建获取详情查询请求
    /// </summary>
    protected abstract TQuery CreateGetByIdQuery(Guid id);

    /// <summary>
    /// 创建创建命令
    /// </summary>
    protected abstract TCommand CreateCreateCommand(TInputDto dto, Guid operatorId);

    /// <summary>
    /// 创建更新命令
    /// </summary>
    protected abstract TCommand CreateUpdateCommand(Guid id, TInputDto dto, Guid operatorId);

    /// <summary>
    /// 创建删除命令
    /// </summary>
    protected abstract TCommand CreateDeleteCommand(Guid id, Guid operatorId);

    /// <summary>
    /// 创建切换状态命令
    /// </summary>
    protected abstract TCommand CreateToggleStatusCommand(Guid id, Guid operatorId);

    /// <summary>
    /// 创建恢复命令
    /// </summary>
    protected abstract TCommand CreateRestoreCommand(Guid id, Guid operatorId);

    /// <summary>
    /// 创建批量删除命令
    /// </summary>
    protected abstract TCommand CreateBatchDeleteCommand(List<Guid> ids, Guid operatorId);

    #endregion
}
```

- [ ] **Step 2: 验证编译**

Run: `dotnet build src/Server/Core/LYBT.Infrastructure/LYBT.Infrastructure.csproj`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add src/Server/Core/LYBT.Infrastructure/Web/BaseCrudController.cs
git commit -m "feat(infra): add BaseCrudController generic base class"
```

---

## Task 2: 重构 PatientsController（Server + LocalWebAPI）

**Covers:** [S3]

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/PatientsController.cs`
- Modify: `src/Client/Desktop/LocalWebAPI/Controllers/PatientsController.cs`

**Interfaces:**
- Consumes: `BaseCrudController<PatientListDto, PatientDetailDto, PatientInputDto, GetPatientsQuery, CreatePatientCommand>`
- Produces: 简化后的 PatientsController

- [ ] **Step 1: 重构 Server PatientsController**

```csharp
using Asp.Versioning;
using MediatR;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.Patients.Application.Commands;
using LYBT.Module.Patients.Application.Queries;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;

namespace LYBT.WebAPI.Controllers
{
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Policy = PolicyConstants.DoctorOrAdminOrReceptionist)]
    public class PatientsController : BaseCrudController<PatientListDto, PatientDetailDto, PatientInputDto, GetPatientsQuery, CreatePatientCommand>
    {
        public PatientsController(ISender sender, ILogger<PatientsController> logger)
            : base(sender, logger)
        {
        }

        // 重写 GetList 添加 OutputCache 和 IsAdmin 检查
        [HttpGet]
        [OutputCache(PolicyName = "PatientsCache")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<PatientListDto>>), 200)]
        public override async Task<IActionResult> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            CancellationToken ct = default)
        {
            if (ValidatePagination(page, pageSize) is { } error) return error;

            var isAdmin = User?.IsInRole(RoleConstants.Admin) == true || User?.IsInRole(RoleConstants.SuperAdmin) == true;
            var query = new GetPatientsQuery(page, pageSize, keyword, FilterDisabled: !isAdmin);
            var result = await Sender.Send(query, ct);
            if (!result.IsSuccess || result.Value == null)
                return BusinessFail(result.Error ?? "查询失败");
            return SuccessPaged(result.Value, "查询成功");
        }

        // 重写 Update 添加 Ownership 检查
        [HttpPut("{id:guid}")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<PatientDetailDto>), 200)]
        public override async Task<IActionResult> Update(Guid id, [FromBody] PatientInputDto dto, CancellationToken ct)
        {
            if (ValidateGuid(id, "患者ID") is { } guidError) return guidError;

            var (ownerDto, ownershipError) = await CheckOwnershipAsync(id, ct);
            if (ownershipError != null) return ownershipError;

            var (operatorId, _, _) = GetOperator();
            var command = new UpdatePatientCommand(id, dto, operatorId);
            var result = await Sender.Send(command, ct);
            if (!result.IsSuccess || result.Value == null)
            {
                if (result.Error?.Contains("不存在") == true)
                    return NotFound(result.Error);
                return BusinessFail(result.Error ?? "更新失败");
            }
            LogOperation("更新患者成功", result.Value, id);
            return Success(result.Value, "患者更新成功");
        }

        // 重写 Delete 添加 Ownership 检查
        [HttpDelete("{id:guid}")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        public override async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "患者ID") is { } guidError) return guidError;

            var (ownerDto, ownershipError) = await CheckOwnershipAsync(id, ct);
            if (ownershipError != null) return ownershipError;

            var (operatorId, _, _) = GetOperator();
            var command = new DeletePatientCommand(id, operatorId);
            var result = await Sender.Send(command, ct);
            if (!result.IsSuccess)
            {
                if (result.Error?.Contains("医案记录") == true)
                    return BusinessFail(result.Error);
                return NotFound("患者不存在");
            }
            LogOperation("删除患者成功", null, id);
            return Success(true, "删除成功");
        }

        // 重写 ToggleStatus 添加 Ownership 检查
        [HttpPost("{id:guid}/toggle-status")]
        [ProducesResponseType(typeof(ApiResponse<PatientDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public override async Task<IActionResult> ToggleStatus(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "患者ID") is { } guidError) return guidError;

            var (ownerDto, ownershipError) = await CheckOwnershipAsync(id, ct);
            if (ownershipError != null) return ownershipError;

            var (operatorId, _, _) = GetOperator();
            var command = new TogglePatientStatusCommand(id, operatorId);
            var result = await Sender.Send(command, ct);
            if (!result.IsSuccess || result.Value == null)
                return BusinessFail(result.Error ?? "操作失败");
            LogOperation("切换患者状态", new { NewStatus = result.Value.Status }, id);
            return Success(result.Value, $"患者已{(result.Value.Status == CommonStatus.Enabled ? "启用" : "禁用")}");
        }

        // 特殊端点：根据身份证号查询
        [HttpGet("by-id-number/{idNumber}")]
        [ProducesResponseType(typeof(ApiResponse<PatientDetailDto>), 200)]
        public async Task<IActionResult> GetByIdNumber(string idNumber, CancellationToken ct)
        {
            var result = await Sender.Send(new SearchPatientByIdNumberQuery(idNumber), ct);
            if (!result.IsSuccess || result.Value == null)
                return NotFound(result.Error ?? "未找到匹配的患者");
            return Success(result.Value, "查询成功");
        }

        // 特殊端点：检查引用
        [HttpGet("{id:guid}/check-reference")]
        [ProducesResponseType(typeof(ApiResponse<PatientReferenceCheckDto>), 200)]
        public async Task<IActionResult> CheckReference(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "患者ID") is { } guidError) return guidError;
            var result = await Sender.Send(new CheckPatientReferenceQuery(id), ct);
            if (!result.IsSuccess || result.Value == null)
                return NotFound(result.Error ?? "患者不存在");
            return Success(result.Value, "引用检查完成");
        }

        // 特殊端点：批量检查引用
        [HttpPost("batch-check-reference")]
        [ProducesResponseType(typeof(ApiResponse<List<PatientReferenceCheckDto>>), 200)]
        public async Task<IActionResult> BatchCheckReference([FromBody] PatientBatchCheckReferenceInputDto dto, CancellationToken ct)
        {
            if (dto.PatientIds == null || dto.PatientIds.Count == 0)
                return ValidationFail("请至少选择一个患者");
            if (dto.PatientIds.Count > 100)
                return ValidationFail("批量检查最多支持100条");
            var result = await Sender.Send(new BatchCheckPatientReferenceQuery(dto.PatientIds), ct);
            if (!result.IsSuccess || result.Value == null)
                return BusinessFail(result.Error ?? "批量检查失败");
            return Success(result.Value, "批量引用检查完成");
        }

        // 辅助方法：检查所有权
        private async Task<(PatientDetailDto? dto, IActionResult? error)> CheckOwnershipAsync(Guid id, CancellationToken ct)
        {
            var result = await Sender.Send(new GetPatientQuery(id), ct);
            if (!result.IsSuccess || result.Value == null)
                return (null, NotFound("患者不存在"));
            if (ValidateOwnership(result.Value.CreatedBy, "患者") is { } ownerError)
                return (null, ownerError);
            return (result.Value, null);
        }

        #region 基类抽象方法实现
        protected override GetPatientsQuery CreateGetListQuery(int page, int pageSize, string? keyword)
            => new GetPatientsQuery(page, pageSize, keyword);

        protected override GetPatientsQuery CreateGetByIdQuery(Guid id)
            => new GetPatientQuery(id);

        protected override CreatePatientCommand CreateCreateCommand(PatientInputDto dto, Guid operatorId)
            => new CreatePatientCommand(dto, operatorId);

        protected override UpdatePatientCommand CreateUpdateCommand(Guid id, PatientInputDto dto, Guid operatorId)
            => new UpdatePatientCommand(id, dto, operatorId);

        protected override DeletePatientCommand CreateDeleteCommand(Guid id, Guid operatorId)
            => new DeletePatientCommand(id, operatorId);

        protected override TogglePatientStatusCommand CreateToggleStatusCommand(Guid id, Guid operatorId)
            => new TogglePatientStatusCommand(id, operatorId);

        protected override RestorePatientCommand CreateRestoreCommand(Guid id, Guid operatorId)
            => new RestorePatientCommand(id, operatorId);

        protected override BatchDeletePatientsCommand CreateBatchDeleteCommand(List<Guid> ids, Guid operatorId)
            => new BatchDeletePatientsCommand(ids, operatorId);
        #endregion
    }
}
```

- [ ] **Step 2: 重构 LocalWebAPI PatientsController**

```csharp
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.Patients.Application.Commands;
using LYBT.Module.Patients.Application.Queries;
using LYBT.Shared.Models.Contracts.Patients;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.LocalWebAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = PolicyConstants.DoctorOrAdminOrReceptionist)]
public class PatientsController : BaseCrudController<PatientListDto, PatientDetailDto, PatientInputDto, GetPatientsQuery, CreatePatientCommand>
{
    public PatientsController(
        ISender sender,
        ILogger<PatientsController> logger) : base(sender, logger)
    {
    }

    #region 基类抽象方法实现
    protected override GetPatientsQuery CreateGetListQuery(int page, int pageSize, string? keyword)
        => new GetPatientsQuery(page, pageSize, keyword);

    protected override GetPatientsQuery CreateGetByIdQuery(Guid id)
        => new GetPatientQuery(id);

    protected override CreatePatientCommand CreateCreateCommand(PatientInputDto dto, Guid operatorId)
        => new CreatePatientCommand(dto, operatorId);

    protected override UpdatePatientCommand CreateUpdateCommand(Guid id, PatientInputDto dto, Guid operatorId)
        => new UpdatePatientCommand(id, dto, operatorId);

    protected override DeletePatientCommand CreateDeleteCommand(Guid id, Guid operatorId)
        => new DeletePatientCommand(id, operatorId);

    protected override TogglePatientStatusCommand CreateToggleStatusCommand(Guid id, Guid operatorId)
        => new TogglePatientStatusCommand(id, operatorId);

    protected override RestorePatientCommand CreateRestoreCommand(Guid id, Guid operatorId)
        => new RestorePatientCommand(id, operatorId);

    protected override BatchDeletePatientsCommand CreateBatchDeleteCommand(List<Guid> ids, Guid operatorId)
        => new BatchDeletePatientsCommand(ids, operatorId);
    #endregion

    // 特殊端点：根据身份证号查询
    [HttpGet("by-id-number/{idNumber}")]
    public async Task<IActionResult> GetByIdNumber(string idNumber, CancellationToken ct)
    {
        var result = await Sender.Send(new SearchPatientByIdNumberQuery(idNumber), ct);
        if (!result.IsSuccess || result.Value == null)
            return NotFound(result.Error ?? "未找到匹配的患者");
        return Success(result.Value);
    }

    // 特殊端点：检查引用
    [HttpGet("{id:guid}/check-reference")]
    public async Task<IActionResult> CheckReference(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new CheckPatientReferenceQuery(id), ct);
        if (!result.IsSuccess || result.Value == null)
            return NotFound(result.Error ?? "患者不存在");
        return Success(result.Value, "引用检查完成");
    }

    // 特殊端点：批量检查引用
    [HttpPost("batch-check-reference")]
    public async Task<IActionResult> BatchCheckReference([FromBody] PatientBatchCheckReferenceInputDto dto, CancellationToken ct)
    {
        if (dto.PatientIds == null || dto.PatientIds.Count == 0)
            return ValidationFail("请至少选择一个患者");
        if (dto.PatientIds.Count > 100)
            return ValidationFail("批量检查最多支持100条");
        var result = await Sender.Send(new BatchCheckPatientReferenceQuery(dto.PatientIds), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "批量检查失败");
        return Success(result.Value, "批量引用检查完成");
    }
}
```

- [ ] **Step 3: 验证编译**

Run: `dotnet build LYBTZYZS.sln`
Expected: Build succeeded

- [ ] **Step 4: 运行测试**

Run: `dotnet test tests/LYBT.Tests.Server/`
Expected: All tests pass

- [ ] **Step 5: Commit**

```bash
git add src/Server/Services/LYBT.WebAPI/Controllers/PatientsController.cs src/Client/Desktop/LocalWebAPI/Controllers/PatientsController.cs
git commit -m "refactor(patients): consolidate PatientsController using BaseCrudController"
```

---

## Task 3: 重构 HerbsController（Server + LocalWebAPI）

**Covers:** [S4]

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/HerbsController.cs`
- Modify: `src/Client/Desktop/LocalWebAPI/Controllers/HerbsController.cs`

**Interfaces:**
- Consumes: `BaseCrudController<HerbListDto, HerbDetailDto, HerbInputDto, GetHerbsQuery, CreateHerbCommand>`
- Produces: 简化后的 HerbsController

- [ ] **Step 1: 重构 Server HerbsController**

```csharp
using Asp.Versioning;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.Herbs.Application.Commands;
using LYBT.Module.Herbs.Application.Queries;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;

namespace LYBT.WebAPI.Controllers
{
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Policy = PolicyConstants.DoctorOrReceptionist)]
    public class HerbsController : BaseCrudController<HerbListDto, HerbDetailDto, HerbInputDto, GetHerbsQuery, CreateHerbCommand>
    {
        public HerbsController(ISender sender, ILogger<HerbsController> logger)
            : base(sender, logger)
        {
        }

        // 重写 GetList 添加 OutputCache 和分类筛选
        [HttpGet]
        [OutputCache(PolicyName = "HerbsCache")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<HerbListDto>>), 200)]
        public override async Task<IActionResult> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            [FromQuery] string? category = null,
            CancellationToken ct = default)
        {
            if (ValidatePagination(page, pageSize) is { } error) return error;
            var query = new GetHerbsQuery(page, pageSize, keyword, category);
            var result = await Sender.Send(query, ct);
            if (!result.IsSuccess) return BusinessFail(result.Error ?? "查询失败");
            return Success(result.Value!, "查询成功");
        }

        // 重写 Update 添加 Ownership 检查
        [HttpPut("{id}")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<HerbDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public override async Task<IActionResult> Update(Guid id, [FromBody] HerbInputDto dto, CancellationToken ct)
        {
            if (ValidateGuid(id, "药材ID") is { } error) return error;
            var (operatorId, _, _) = GetOperator();
            var getResult = await Sender.Send(new GetHerbQuery(id), ct);
            if (!getResult.IsSuccess || getResult.Value == null)
                return NotFound(getResult.Error ?? "药材不存在");
            if (ValidateOwnership(getResult.Value.CreatedBy, "药材") is { } ownerError)
                return ownerError;
            var command = new UpdateHerbCommand(id, dto, operatorId);
            var result = await Sender.Send(command, ct);
            if (!result.IsSuccess || result.Value == null)
                return BusinessFail(result.Error ?? "更新失败");
            LogOperation("更新药材", result.Value, result.Value.Id);
            return Success(result.Value, "药材更新成功");
        }

        // 重写 Delete 添加 Ownership 检查
        [HttpDelete("{id}")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public override async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "药材ID") is { } error) return error;
            var (operatorId, _, _) = GetOperator();
            var getResult = await Sender.Send(new GetHerbQuery(id), ct);
            if (!getResult.IsSuccess || getResult.Value == null)
                return NotFound(getResult.Error ?? "药材不存在");
            if (ValidateOwnership(getResult.Value.CreatedBy, "药材") is { } ownerError)
                return ownerError;
            var command = new DeleteHerbCommand(id, operatorId);
            var result = await Sender.Send(command, ct);
            if (!result.IsSuccess)
                return BusinessFail(result.Error ?? "删除失败");
            LogOperation("删除药材", new { Id = id }, id);
            return Success<object?>(null, "药材删除成功");
        }

        // 重写 ToggleStatus 添加 Ownership 检查
        [HttpPost("{id}/toggle-status")]
        [ProducesResponseType(typeof(ApiResponse<HerbDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public override async Task<IActionResult> ToggleStatus(Guid id, CancellationToken ct)
        {
            var (operatorId, _, _) = GetOperator();
            var getResult = await Sender.Send(new GetHerbQuery(id), ct);
            if (!getResult.IsSuccess || getResult.Value == null)
                return NotFound(getResult.Error ?? "药材不存在");
            if (ValidateOwnership(getResult.Value.CreatedBy, "药材") is { } ownerError)
                return ownerError;
            var command = new ToggleHerbStatusCommand(id, operatorId);
            var result = await Sender.Send(command, ct);
            if (!result.IsSuccess || result.Value == null)
                return BusinessFail(result.Error ?? "切换状态失败");
            LogOperation("切换药材状态", new { NewStatus = result.Value.Status }, id);
            return Success(result.Value, $"药材已{(result.Value.Status == CommonStatus.Enabled ? "启用" : "禁用")}");
        }

        // 特殊端点：批量导入
        [HttpPost("batch-import")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<HerbBatchImportResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> BatchImport([FromBody] HerbBatchImportInputDto request, CancellationToken ct)
        {
            if (request?.Herbs == null || request.Herbs.Count == 0)
                return ValidationFail("导入列表不能为空");
            var (operatorId, _, _) = GetOperator();
            var result = await Sender.Send(new BatchImportHerbsCommand(request.Herbs, request.Strategy, operatorId), ct);
            if (!result.IsSuccess || result.Value == null)
                return BusinessFail(result.Error ?? "导入失败");
            LogOperation("批量导入药材", new { Count = request.Herbs.Count, Strategy = request.Strategy }, null);
            return Success(result.Value, $"成功导入 {result.Value.SuccessCount} 条药材");
        }

        // 特殊端点：检查引用
        [HttpGet("{id}/check-reference")]
        [ProducesResponseType(typeof(ApiResponse<HerbReferenceCheckDto>), 200)]
        public async Task<IActionResult> CheckReference(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "药材ID") is { } error) return error;
            var result = await Sender.Send(new CheckHerbReferenceQuery(id), ct);
            if (!result.IsSuccess || result.Value == null)
                return NotFound(result.Error ?? "药材不存在");
            return Success(result.Value, "引用检查完成");
        }

        // 特殊端点：批量检查引用
        [HttpPost("batch-check-reference")]
        [ProducesResponseType(typeof(ApiResponse<List<HerbReferenceCheckDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> BatchCheckReference([FromBody] HerbBatchCheckReferenceInputDto dto, CancellationToken ct)
        {
            if (dto?.HerbIds == null || dto.HerbIds.Count == 0)
                return ValidationFail("药材ID列表不能为空");
            if (dto.HerbIds.Count > 100)
                return ValidationFail("单次最多检查100条药材");
            var result = await Sender.Send(new BatchCheckHerbReferenceQuery(dto.HerbIds), ct);
            if (!result.IsSuccess || result.Value == null)
                return BusinessFail(result.Error ?? "批量引用检查失败");
            return Success(result.Value, "批量引用检查完成");
        }

        // 特殊端点：批量启用
        [HttpPost("batch-enable")]
        [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
        [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
        public async Task<IActionResult> BatchEnable([FromBody] BatchDeleteInputDto dto, CancellationToken ct)
        {
            if (dto?.Ids == null || dto.Ids.Count == 0)
                return ValidationFail("药材ID列表不能为空");
            var result = await Sender.Send(new BatchEnableHerbsCommand(dto.Ids), ct);
            if (!result.IsSuccess || result.Value == null)
                return BusinessFail(result.Error ?? "批量启用失败");
            LogOperation("批量启用药材", new { Count = dto.Ids.Count }, null);
            return Success(result.Value, result.Value.Message);
        }

        // 特殊端点：批量禁用
        [HttpPost("batch-disable")]
        [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
        [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
        public async Task<IActionResult> BatchDisable([FromBody] BatchDeleteInputDto dto, CancellationToken ct)
        {
            if (dto?.Ids == null || dto.Ids.Count == 0)
                return ValidationFail("药材ID列表不能为空");
            var result = await Sender.Send(new BatchDisableHerbsCommand(dto.Ids), ct);
            if (!result.IsSuccess || result.Value == null)
                return BusinessFail(result.Error ?? "批量禁用失败");
            LogOperation("批量禁用药材", new { Count = dto.Ids.Count }, null);
            return Success(result.Value, result.Value.Message);
        }

        #region 基类抽象方法实现
        protected override GetHerbsQuery CreateGetListQuery(int page, int pageSize, string? keyword)
            => new GetHerbsQuery(page, pageSize, keyword);

        protected override GetHerbsQuery CreateGetByIdQuery(Guid id)
            => new GetHerbQuery(id);

        protected override CreateHerbCommand CreateCreateCommand(HerbInputDto dto, Guid operatorId)
            => new CreateHerbCommand(dto, operatorId);

        protected override UpdateHerbCommand CreateUpdateCommand(Guid id, HerbInputDto dto, Guid operatorId)
            => new UpdateHerbCommand(id, dto, operatorId);

        protected override DeleteHerbCommand CreateDeleteCommand(Guid id, Guid operatorId)
            => new DeleteHerbCommand(id, operatorId);

        protected override ToggleHerbStatusCommand CreateToggleStatusCommand(Guid id, Guid operatorId)
            => new ToggleHerbStatusCommand(id, operatorId);

        protected override RestoreHerbCommand CreateRestoreCommand(Guid id, Guid operatorId)
            => new RestoreHerbCommand(id, operatorId);

        protected override BatchDeleteHerbsCommand CreateBatchDeleteCommand(List<Guid> ids, Guid operatorId)
            => new BatchDeleteHerbsCommand(ids, operatorId);
        #endregion
    }
}
```

- [ ] **Step 2: 重构 LocalWebAPI HerbsController**

```csharp
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.Herbs.Application.Commands;
using LYBT.Module.Herbs.Application.Queries;
using LYBT.Shared.Models.Contracts.Herbs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.LocalWebAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = PolicyConstants.DoctorOrReceptionist)]
public class HerbsController : BaseCrudController<HerbListDto, HerbDetailDto, HerbInputDto, GetHerbsQuery, CreateHerbCommand>
{
    public HerbsController(ISender sender, ILogger<HerbsController> logger)
        : base(sender, logger)
    {
    }

    // 特殊端点：批量导入
    [HttpPost("batch-import")]
    public async Task<IActionResult> BatchImport([FromBody] HerbBatchImportInputDto request, CancellationToken ct)
    {
        if (request?.Herbs == null || request.Herbs.Count == 0)
            return ValidationFail("导入列表不能为空");
        var (operatorId, _, _) = GetOperator();
        var result = await Sender.Send(new BatchImportHerbsCommand(request.Herbs, request.Strategy, operatorId), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "导入失败");
        LogOperation("批量导入药材", new { Count = request.Herbs.Count, Strategy = request.Strategy }, null);
        return Success(result.Value, $"成功导入 {result.Value.SuccessCount} 条药材");
    }

    // 特殊端点：检查引用
    [HttpGet("{id}/check-reference")]
    public async Task<IActionResult> CheckReference(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new CheckHerbReferenceQuery(id), ct);
        if (!result.IsSuccess || result.Value == null)
            return NotFound(result.Error ?? "药材不存在");
        return Success(result.Value, "引用检查完成");
    }

    // 特殊端点：批量检查引用
    [HttpPost("batch-check-reference")]
    public async Task<IActionResult> BatchCheckReference([FromBody] HerbBatchCheckReferenceInputDto dto, CancellationToken ct)
    {
        if (dto?.HerbIds == null || dto.HerbIds.Count == 0)
            return ValidationFail("药材ID列表不能为空");
        if (dto.HerbIds.Count > 100)
            return ValidationFail("单次最多检查100条药材");
        var result = await Sender.Send(new BatchCheckHerbReferenceQuery(dto.HerbIds), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "批量引用检查失败");
        return Success(result.Value, "批量引用检查完成");
    }

    // 特殊端点：批量启用
    [HttpPost("batch-enable")]
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    public async Task<IActionResult> BatchEnable([FromBody] BatchDeleteInputDto dto, CancellationToken ct)
    {
        if (dto?.Ids == null || dto.Ids.Count == 0)
            return ValidationFail("药材ID列表不能为空");
        var result = await Sender.Send(new BatchEnableHerbsCommand(dto.Ids), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "批量启用失败");
        LogOperation("批量启用药材", new { Count = dto.Ids.Count }, null);
        return Success(result.Value, result.Value.Message);
    }

    // 特殊端点：批量禁用
    [HttpPost("batch-disable")]
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    public async Task<IActionResult> BatchDisable([FromBody] BatchDeleteInputDto dto, CancellationToken ct)
    {
        if (dto?.Ids == null || dto.Ids.Count == 0)
            return ValidationFail("药材ID列表不能为空");
        var result = await Sender.Send(new BatchDisableHerbsCommand(dto.Ids), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "批量禁用失败");
        LogOperation("批量禁用药材", new { Count = dto.Ids.Count }, null);
        return Success(result.Value, result.Value.Message);
    }

    #region 基类抽象方法实现
    protected override GetHerbsQuery CreateGetListQuery(int page, int pageSize, string? keyword)
        => new GetHerbsQuery(page, pageSize, keyword);

    protected override GetHerbsQuery CreateGetByIdQuery(Guid id)
        => new GetHerbQuery(id);

    protected override CreateHerbCommand CreateCreateCommand(HerbInputDto dto, Guid operatorId)
        => new CreateHerbCommand(dto, operatorId);

    protected override UpdateHerbCommand CreateUpdateCommand(Guid id, HerbInputDto dto, Guid operatorId)
        => new UpdateHerbCommand(id, dto, operatorId);

    protected override DeleteHerbCommand CreateDeleteCommand(Guid id, Guid operatorId)
        => new DeleteHerbCommand(id, operatorId);

    protected override ToggleHerbStatusCommand CreateToggleStatusCommand(Guid id, Guid operatorId)
        => new ToggleHerbStatusCommand(id, operatorId);

    protected override RestoreHerbCommand CreateRestoreCommand(Guid id, Guid operatorId)
        => new RestoreHerbCommand(id, operatorId);

    protected override BatchDeleteHerbsCommand CreateBatchDeleteCommand(List<Guid> ids, Guid operatorId)
        => new BatchDeleteHerbsCommand(ids, operatorId);
    #endregion
}
```

- [ ] **Step 3: 验证编译**

Run: `dotnet build LYBTZYZS.sln`
Expected: Build succeeded

- [ ] **Step 4: 运行测试**

Run: `dotnet test tests/LYBT.Tests.Server/`
Expected: All tests pass

- [ ] **Step 5: Commit**

```bash
git add src/Server/Services/LYBT.WebAPI/Controllers/HerbsController.cs src/Client/Desktop/LocalWebAPI/Controllers/HerbsController.cs
git commit -m "refactor(herbs): consolidate HerbsController using BaseCrudController"
```

---

## Task 4: 重构 FormulasController（Server + LocalWebAPI）

**Covers:** [S5]

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/FormulasController.cs`
- Modify: `src/Client/Desktop/LocalWebAPI/Controllers/FormulasController.cs`

**Interfaces:**
- Consumes: `BaseCrudController<FormulaListDto, FormulaDetailDto, FormulaInputDto, GetFormulasQuery, CreateFormulaCommand>`
- Produces: 简化后的 FormulasController

- [ ] **Step 1: 重构 Server FormulasController**

```csharp
using Asp.Versioning;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.Formulas.Application.Commands;
using LYBT.Module.Formulas.Application.Queries;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;

namespace LYBT.WebAPI.Controllers
{
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Policy = PolicyConstants.DoctorOrReceptionist)]
    public class FormulasController : BaseCrudController<FormulaListDto, FormulaDetailDto, FormulaInputDto, GetFormulasQuery, CreateFormulaCommand>
    {
        public FormulasController(ISender sender, ILogger<FormulasController> logger)
            : base(sender, logger)
        {
        }

        // 重写 GetList 添加 OutputCache
        [HttpGet]
        [OutputCache(PolicyName = "FormulasCache")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<FormulaListDto>>), 200)]
        public override async Task<IActionResult> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            CancellationToken ct = default)
        {
            if (ValidatePagination(page, pageSize) is { } error) return error;
            var query = new GetFormulasQuery(page, pageSize, keyword);
            var result = await Sender.Send(query, ct);
            if (!result.IsSuccess) return BusinessFail(result.Error ?? "查询失败");
            return SuccessPaged(result.Value!, "查询成功");
        }

        // 重写 GetById 添加所有权检查（医生只能查看自己和共享的）
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<FormulaDetailDto>), 200)]
        public override async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "验方ID") is { } error) return error;
            var query = new GetFormulaQuery(id);
            var result = await Sender.Send(query, ct);
            if (!result.IsSuccess || result.Value == null)
                return NotFound(result.Error ?? "验方不存在");
            var (operatorId, _, operatorRole) = GetOperator();
            if (operatorRole == UserRole.Doctor && result.Value.CreatedBy != operatorId && !result.Value.IsShared)
                return Forbid("无权限查看此验方");
            return Success(result.Value, "查询成功");
        }

        // 重写 Update 添加 Ownership 检查
        [HttpPut("{id}")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<FormulaDetailDto>), 200)]
        public override async Task<IActionResult> Update(Guid id, [FromBody] FormulaInputDto dto, CancellationToken ct)
        {
            if (ValidateGuid(id, "验方ID") is { } error) return error;
            var getResult = await Sender.Send(new GetFormulaQuery(id), ct);
            if (!getResult.IsSuccess || getResult.Value == null)
                return NotFound("验方不存在");
            if (ValidateOwnership(getResult.Value.CreatedBy, "验方") is { } ownershipError)
                return ownershipError;
            var (operatorId, _, _) = GetOperator();
            var command = new UpdateFormulaCommand(id, dto, operatorId);
            var result = await Sender.Send(command, ct);
            if (!result.IsSuccess || result.Value == null)
                return BusinessFail(result.Error ?? "更新失败");
            LogOperation("更新验方成功", result.Value, id);
            return Success(result.Value, "验方更新成功");
        }

        // 重写 Delete 添加 Ownership 检查
        [HttpDelete("{id}")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        public override async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "验方ID") is { } error) return error;
            var getResult = await Sender.Send(new GetFormulaQuery(id), ct);
            if (!getResult.IsSuccess || getResult.Value == null)
                return NotFound("验方不存在");
            if (ValidateOwnership(getResult.Value.CreatedBy, "验方") is { } ownershipError)
                return ownershipError;
            var (operatorId, _, _) = GetOperator();
            var command = new DeleteFormulaCommand(id, operatorId);
            var result = await Sender.Send(command, ct);
            if (!result.IsSuccess)
                return NotFound(result.Error ?? "验方不存在");
            LogOperation("删除验方成功", null, id);
            return Success(true, "删除成功");
        }

        // 重写 ToggleStatus 添加 Ownership 检查
        [HttpPost("{id}/toggle-status")]
        [ProducesResponseType(typeof(ApiResponse<FormulaDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public override async Task<IActionResult> ToggleStatus(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "验方ID") is { } error) return error;
            var getResult = await Sender.Send(new GetFormulaQuery(id), ct);
            if (!getResult.IsSuccess || getResult.Value == null)
                return NotFound("验方不存在");
            if (ValidateOwnership(getResult.Value.CreatedBy, "验方") is { } ownershipError)
                return ownershipError;
            var (operatorId, _, _) = GetOperator();
            var command = new ToggleFormulaStatusCommand(id, operatorId);
            var result = await Sender.Send(command, ct);
            if (!result.IsSuccess || result.Value == null)
                return BusinessFail(result.Error ?? "切换状态失败");
            LogOperation("切换验方状态", new { NewStatus = result.Value.Status }, id);
            return Success(result.Value, $"验方已{(result.Value.Status == CommonStatus.Enabled ? "启用" : "禁用")}");
        }

        // 特殊端点：批量导入
        [HttpPost("batch-import")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<FormulaBatchImportResultDto>), 200)]
        public async Task<IActionResult> Import([FromBody] FormulaBatchImportInputDto request, CancellationToken ct)
        {
            if (request == null || request.Formulas == null || !request.Formulas.Any())
                return ValidationFail("导入数据不能为空");
            var result = await Sender.Send(new BatchImportFormulasCommand(request.Formulas, request.FileName), ct);
            if (!result.IsSuccess || result.Value == null)
                return BusinessFail(result.Error ?? "导入失败");
            LogOperation("批量导入验方", new { FileName = request.FileName, TotalCount = result.Value.TotalCount, SuccessCount = result.Value.SuccessCount }, null);
            return Success(result.Value, result.Value.Message);
        }

        // 特殊端点：获取待校验列表
        [HttpGet("pending-validation")]
        [ProducesResponseType(typeof(ApiResponse<List<FormulaDetailDto>>), 200)]
        public async Task<IActionResult> GetPendingValidation(CancellationToken ct)
        {
            var result = await Sender.Send(new GetPendingValidationQuery(), ct);
            if (!result.IsSuccess || result.Value == null)
                return BusinessFail(result.Error ?? "查询失败");
            return Success(result.Value, $"查询成功，共{result.Value.Count}个待校验验方");
        }

        // 特殊端点：校验药材匹配
        [HttpPost("{formulaId}/herbs/{herbItemId}/validate")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ValidateHerb(Guid formulaId, Guid herbItemId, [FromBody] ValidateFormulaHerbInputDto request, CancellationToken ct)
        {
            if (ValidateGuid(formulaId, "验方ID") is { } error1) return error1;
            if (ValidateGuid(herbItemId, "药材项ID") is { } error2) return error2;
            if (ValidateGuid(request.SelectedHerbId, "系统药材ID") is { } error3) return error3;
            var result = await Sender.Send(new ValidateFormulaHerbCommand(formulaId, herbItemId, request.SelectedHerbId), ct);
            if (!result.IsSuccess)
                return BusinessFail(result.Error ?? "验证失败");
            LogOperation("验证验方药材", new { FormulaId = formulaId, HerbItemId = herbItemId, SelectedHerbId = request.SelectedHerbId }, formulaId);
            return Success("药材验证成功");
        }

        // 特殊端点：批量启用
        [HttpPost("batch-enable")]
        [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
        [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
        public async Task<IActionResult> BatchEnable([FromBody] BatchDeleteInputDto dto, CancellationToken ct = default)
        {
            if (dto?.Ids == null || dto.Ids.Count == 0)
                return ValidationFail("验方ID列表不能为空");
            var result = await Sender.Send(new BatchEnableFormulasCommand(dto.Ids), ct);
            if (!result.IsSuccess || result.Value == null)
                return BusinessFail(result.Error ?? "批量启用失败");
            LogOperation("批量启用药方", new { Count = dto.Ids.Count }, null);
            return Success(result.Value, result.Value.Message);
        }

        // 特殊端点：批量禁用
        [HttpPost("batch-disable")]
        [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
        [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
        public async Task<IActionResult> BatchDisable([FromBody] BatchDeleteInputDto dto, CancellationToken ct = default)
        {
            if (dto?.Ids == null || dto.Ids.Count == 0)
                return ValidationFail("验方ID列表不能为空");
            var result = await Sender.Send(new BatchDisableFormulasCommand(dto.Ids), ct);
            if (!result.IsSuccess || result.Value == null)
                return BusinessFail(result.Error ?? "批量禁用失败");
            LogOperation("批量禁用药方", new { Count = dto.Ids.Count }, null);
            return Success(result.Value, result.Value.Message);
        }

        #region 基类抽象方法实现
        protected override GetFormulasQuery CreateGetListQuery(int page, int pageSize, string? keyword)
            => new GetFormulasQuery(page, pageSize, keyword);

        protected override GetFormulasQuery CreateGetByIdQuery(Guid id)
            => new GetFormulaQuery(id);

        protected override CreateFormulaCommand CreateCreateCommand(FormulaInputDto dto, Guid operatorId)
            => new CreateFormulaCommand(dto, operatorId);

        protected override UpdateFormulaCommand CreateUpdateCommand(Guid id, FormulaInputDto dto, Guid operatorId)
            => new UpdateFormulaCommand(id, dto, operatorId);

        protected override DeleteFormulaCommand CreateDeleteCommand(Guid id, Guid operatorId)
            => new DeleteFormulaCommand(id, operatorId);

        protected override ToggleFormulaStatusCommand CreateToggleStatusCommand(Guid id, Guid operatorId)
            => new ToggleFormulaStatusCommand(id, operatorId);

        protected override RestoreFormulaCommand CreateRestoreCommand(Guid id, Guid operatorId)
            => new RestoreFormulaCommand(id, operatorId);

        protected override BatchDeleteFormulasCommand CreateBatchDeleteCommand(List<Guid> ids, Guid operatorId)
            => new BatchDeleteFormulasCommand(ids, operatorId);
        #endregion
    }
}
```

- [ ] **Step 2: 重构 LocalWebAPI FormulasController**

```csharp
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.Formulas.Application.Commands;
using LYBT.Module.Formulas.Application.Queries;
using LYBT.Shared.Models.Contracts.Formula;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.LocalWebAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = PolicyConstants.DoctorOrReceptionist)]
public class FormulasController : BaseCrudController<FormulaListDto, FormulaDetailDto, FormulaInputDto, GetFormulasQuery, CreateFormulaCommand>
{
    public FormulasController(ISender sender, ILogger<FormulasController> logger)
        : base(sender, logger)
    {
    }

    // 特殊端点：批量导入
    [HttpPost("batch-import")]
    public async Task<IActionResult> Import([FromBody] FormulaBatchImportInputDto request, CancellationToken ct)
    {
        if (request == null || request.Formulas == null || !request.Formulas.Any())
            return ValidationFail("导入数据不能为空");
        var result = await Sender.Send(new BatchImportFormulasCommand(request.Formulas, request.FileName), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "导入失败");
        LogOperation("批量导入验方", new { FileName = request.FileName, TotalCount = result.Value.TotalCount, SuccessCount = result.Value.SuccessCount }, null);
        return Success(result.Value, result.Value.Message);
    }

    // 特殊端点：获取待校验列表
    [HttpGet("pending-validation")]
    public async Task<IActionResult> GetPendingValidation(CancellationToken ct)
    {
        var result = await Sender.Send(new GetPendingValidationQuery(), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "查询失败");
        return Success(result.Value, $"查询成功，共{result.Value.Count}个待校验验方");
    }

    // 特殊端点：校验药材匹配
    [HttpPost("{formulaId}/herbs/{herbItemId}/validate")]
    public async Task<IActionResult> ValidateHerb(Guid formulaId, Guid herbItemId, [FromBody] ValidateFormulaHerbInputDto request, CancellationToken ct)
    {
        if (ValidateGuid(formulaId, "验方ID") is { } error1) return error1;
        if (ValidateGuid(herbItemId, "药材项ID") is { } error2) return error2;
        if (ValidateGuid(request.SelectedHerbId, "系统药材ID") is { } error3) return error3;
        var result = await Sender.Send(new ValidateFormulaHerbCommand(formulaId, herbItemId, request.SelectedHerbId), ct);
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "验证失败");
        LogOperation("验证验方药材", new { FormulaId = formulaId, HerbItemId = herbItemId, SelectedHerbId = request.SelectedHerbId }, formulaId);
        return Success("药材验证成功");
    }

    // 特殊端点：批量启用
    [HttpPost("batch-enable")]
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    public async Task<IActionResult> BatchEnable([FromBody] BatchDeleteInputDto dto, CancellationToken ct = default)
    {
        if (dto?.Ids == null || dto.Ids.Count == 0)
            return ValidationFail("验方ID列表不能为空");
        var result = await Sender.Send(new BatchEnableFormulasCommand(dto.Ids), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "批量启用失败");
        LogOperation("批量启用药方", new { Count = dto.Ids.Count }, null);
        return Success(result.Value, result.Value.Message);
    }

    // 特殊端点：批量禁用
    [HttpPost("batch-disable")]
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    public async Task<IActionResult> BatchDisable([FromBody] BatchDeleteInputDto dto, CancellationToken ct = default)
    {
        if (dto?.Ids == null || dto.Ids.Count == 0)
            return ValidationFail("验方ID列表不能为空");
        var result = await Sender.Send(new BatchDisableFormulasCommand(dto.Ids), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "批量禁用失败");
        LogOperation("批量禁用药方", new { Count = dto.Ids.Count }, null);
        return Success(result.Value, result.Value.Message);
    }

    #region 基类抽象方法实现
    protected override GetFormulasQuery CreateGetListQuery(int page, int pageSize, string? keyword)
        => new GetFormulasQuery(page, pageSize, keyword);

    protected override GetFormulasQuery CreateGetByIdQuery(Guid id)
        => new GetFormulaQuery(id);

    protected override CreateFormulaCommand CreateCreateCommand(FormulaInputDto dto, Guid operatorId)
        => new CreateFormulaCommand(dto, operatorId);

    protected override UpdateFormulaCommand CreateUpdateCommand(Guid id, FormulaInputDto dto, Guid operatorId)
        => new UpdateFormulaCommand(id, dto, operatorId);

    protected override DeleteFormulaCommand CreateDeleteCommand(Guid id, Guid operatorId)
        => new DeleteFormulaCommand(id, operatorId);

    protected override ToggleFormulaStatusCommand CreateToggleStatusCommand(Guid id, Guid operatorId)
        => new ToggleFormulaStatusCommand(id, operatorId);

    protected override RestoreFormulaCommand CreateRestoreCommand(Guid id, Guid operatorId)
        => new RestoreFormulaCommand(id, operatorId);

    protected override BatchDeleteFormulasCommand CreateBatchDeleteCommand(List<Guid> ids, Guid operatorId)
        => new BatchDeleteFormulasCommand(ids, operatorId);
    #endregion
}
```

- [ ] **Step 3: 验证编译**

Run: `dotnet build LYBTZYZS.sln`
Expected: Build succeeded

- [ ] **Step 4: 运行测试**

Run: `dotnet test tests/LYBT.Tests.Server/`
Expected: All tests pass

- [ ] **Step 5: Commit**

```bash
git add src/Server/Services/LYBT.WebAPI/Controllers/FormulasController.cs src/Client/Desktop/LocalWebAPI/Controllers/FormulasController.cs
git commit -m "refactor(formulas): consolidate FormulasController using BaseCrudController"
```

---

## Task 5: 重构 RegistrationsController（Server + LocalWebAPI）

**Covers:** [S6]

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/RegistrationsController.cs`
- Modify: `src/Client/Desktop/LocalWebAPI/Controllers/RegistrationsController.cs`

**Interfaces:**
- Consumes: `BaseCrudController<RegistrationListDto, RegistrationDetailDto, RegistrationInputDto, GetRegistrationsQuery, CreateRegistrationCommand>`
- Produces: 简化后的 RegistrationsController

- [ ] **Step 1: 重构 Server RegistrationsController**

```csharp
using Asp.Versioning;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.Registrations.Application.Commands;
using LYBT.Module.Registrations.Application.Queries;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Registrations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;

namespace LYBT.WebAPI.Controllers
{
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Policy = PolicyConstants.DoctorOrReceptionist)]
    public class RegistrationsController : BaseCrudController<RegistrationListDto, RegistrationDetailDto, RegistrationInputDto, GetRegistrationsQuery, CreateRegistrationCommand>
    {
        public RegistrationsController(ISender sender, ILogger<RegistrationsController> logger)
            : base(sender, logger)
        {
        }

        // 重写 GetList 添加 OutputCache
        [HttpGet]
        [OutputCache(PolicyName = "RegistrationsCache")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<RegistrationListDto>>), 200)]
        public override async Task<IActionResult> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            CancellationToken ct = default)
        {
            if (ValidatePagination(page, pageSize) is { } error) return error;
            var query = new GetRegistrationsQuery(page, pageSize, keyword);
            var result = await Sender.Send(query, ct);
            if (!result.IsSuccess) return BusinessFail(result.Error ?? "查询失败");
            return SuccessPaged(result.Value!, "查询成功");
        }

        // 特殊端点：快速挂号
        [HttpPost("quick-visit")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<RegistrationDetailDto>), 201)]
        public async Task<IActionResult> QuickVisit([FromBody] QuickVisitInputDto dto, CancellationToken ct)
        {
            var (operatorId, _, _) = GetOperator();
            var command = new QuickVisitCommand(dto.PatientId, dto.DoctorId, operatorId);
            var result = await Sender.Send(command, ct);
            if (!result.IsSuccess || result.Value == null)
                return BusinessFail(result.Error ?? "快速挂号失败");
            LogOperation("快速挂号", result.Value, result.Value.Id);
            return CreatedAtAction(nameof(GetById),
                new { id = result.Value.Id },
                result.Value);
        }

        // 特殊端点：开始就诊
        [HttpPost("{id:guid}/start-visit")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<RegistrationDetailDto>), 200)]
        public async Task<IActionResult> StartVisit(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "挂号ID") is { } error) return error;
            var (operatorId, _, _) = GetOperator();
            var command = new StartVisitCommand(id, operatorId);
            var result = await Sender.Send(command, ct);
            if (!result.IsSuccess || result.Value == null)
                return BusinessFail(result.Error ?? "开始就诊失败");
            LogOperation("开始就诊", result.Value, id);
            return Success(result.Value, "就诊已开始");
        }

        // 特殊端点：取消挂号
        [HttpPost("{id:guid}/cancel")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<RegistrationDetailDto>), 200)]
        public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "挂号ID") is { } error) return error;
            var (operatorId, _, _) = GetOperator();
            var command = new CancelRegistrationCommand(id, operatorId);
            var result = await Sender.Send(command, ct);
            if (!result.IsSuccess || result.Value == null)
                return BusinessFail(result.Error ?? "取消挂号失败");
            LogOperation("取消挂号", result.Value, id);
            return Success(result.Value, "挂号已取消");
        }

        // 特殊端点：获取队列
        [HttpGet("queue")]
        [ProducesResponseType(typeof(ApiResponse<List<RegistrationDetailDto>>), 200)]
        public async Task<IActionResult> GetQueue(CancellationToken ct)
        {
            var result = await Sender.Send(new GetRegistrationQueueQuery(), ct);
            if (!result.IsSuccess || result.Value == null)
                return BusinessFail(result.Error ?? "获取队列失败");
            return Success(result.Value, "查询成功");
        }

        #region 基类抽象方法实现
        protected override GetRegistrationsQuery CreateGetListQuery(int page, int pageSize, string? keyword)
            => new GetRegistrationsQuery(page, pageSize, keyword);

        protected override GetRegistrationsQuery CreateGetByIdQuery(Guid id)
            => new GetRegistrationQuery(id);

        protected override CreateRegistrationCommand CreateCreateCommand(RegistrationInputDto dto, Guid operatorId)
            => new CreateRegistrationCommand(dto, operatorId);

        protected override UpdateRegistrationCommand CreateUpdateCommand(Guid id, RegistrationInputDto dto, Guid operatorId)
            => new UpdateRegistrationCommand(id, dto, operatorId);

        protected override DeleteRegistrationCommand CreateDeleteCommand(Guid id, Guid operatorId)
            => new DeleteRegistrationCommand(id, operatorId);

        protected override ToggleRegistrationStatusCommand CreateToggleStatusCommand(Guid id, Guid operatorId)
            => new ToggleRegistrationStatusCommand(id, operatorId);

        protected override RestoreRegistrationCommand CreateRestoreCommand(Guid id, Guid operatorId)
            => new RestoreRegistrationCommand(id, operatorId);

        protected override BatchDeleteRegistrationsCommand CreateBatchDeleteCommand(List<Guid> ids, Guid operatorId)
            => new BatchDeleteRegistrationsCommand(ids, operatorId);
        #endregion
    }
}
```

- [ ] **Step 2: 重构 LocalWebAPI RegistrationsController**

```csharp
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.Registrations.Application.Commands;
using LYBT.Module.Registrations.Application.Queries;
using LYBT.Shared.Models.Contracts.Registrations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.LocalWebAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = PolicyConstants.DoctorOrReceptionist)]
public class RegistrationsController : BaseCrudController<RegistrationListDto, RegistrationDetailDto, RegistrationInputDto, GetRegistrationsQuery, CreateRegistrationCommand>
{
    public RegistrationsController(ISender sender, ILogger<RegistrationsController> logger)
        : base(sender, logger)
    {
    }

    // 特殊端点：快速挂号
    [HttpPost("quick-visit")]
    public async Task<IActionResult> QuickVisit([FromBody] QuickVisitInputDto dto, CancellationToken ct)
    {
        var (operatorId, _, _) = GetOperator();
        var command = new QuickVisitCommand(dto.PatientId, dto.DoctorId, operatorId);
        var result = await Sender.Send(command, ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "快速挂号失败");
        LogOperation("快速挂号", result.Value, result.Value.Id);
        return CreatedAtAction(nameof(GetById),
            new { id = result.Value.Id },
            result.Value);
    }

    // 特殊端点：开始就诊
    [HttpPost("{id:guid}/start-visit")]
    public async Task<IActionResult> StartVisit(Guid id, CancellationToken ct)
    {
        if (ValidateGuid(id, "挂号ID") is { } error) return error;
        var (operatorId, _, _) = GetOperator();
        var command = new StartVisitCommand(id, operatorId);
        var result = await Sender.Send(command, ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "开始就诊失败");
        LogOperation("开始就诊", result.Value, id);
        return Success(result.Value, "就诊已开始");
    }

    // 特殊端点：取消挂号
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        if (ValidateGuid(id, "挂号ID") is { } error) return error;
        var (operatorId, _, _) = GetOperator();
        var command = new CancelRegistrationCommand(id, operatorId);
        var result = await Sender.Send(command, ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "取消挂号失败");
        LogOperation("取消挂号", result.Value, id);
        return Success(result.Value, "挂号已取消");
    }

    // 特殊端点：获取队列
    [HttpGet("queue")]
    public async Task<IActionResult> GetQueue(CancellationToken ct)
    {
        var result = await Sender.Send(new GetRegistrationQueueQuery(), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "获取队列失败");
        return Success(result.Value, "查询成功");
    }

    #region 基类抽象方法实现
    protected override GetRegistrationsQuery CreateGetListQuery(int page, int pageSize, string? keyword)
        => new GetRegistrationsQuery(page, pageSize, keyword);

    protected override GetRegistrationsQuery CreateGetByIdQuery(Guid id)
        => new GetRegistrationQuery(id);

    protected override CreateRegistrationCommand CreateCreateCommand(RegistrationInputDto dto, Guid operatorId)
        => new CreateRegistrationCommand(dto, operatorId);

    protected override UpdateRegistrationCommand CreateUpdateCommand(Guid id, RegistrationInputDto dto, Guid operatorId)
        => new UpdateRegistrationCommand(id, dto, operatorId);

    protected override DeleteRegistrationCommand CreateDeleteCommand(Guid id, Guid operatorId)
        => new DeleteRegistrationCommand(id, operatorId);

    protected override ToggleRegistrationStatusCommand CreateToggleStatusCommand(Guid id, Guid operatorId)
        => new ToggleRegistrationStatusCommand(id, operatorId);

    protected override RestoreRegistrationCommand CreateRestoreCommand(Guid id, Guid operatorId)
        => new RestoreRegistrationCommand(id, operatorId);

    protected override BatchDeleteRegistrationsCommand CreateBatchDeleteCommand(List<Guid> ids, Guid operatorId)
        => new BatchDeleteRegistrationsCommand(ids, operatorId);
    #endregion
}
```

- [ ] **Step 3: 验证编译**

Run: `dotnet build LYBTZYZS.sln`
Expected: Build succeeded

- [ ] **Step 4: 运行测试**

Run: `dotnet test tests/LYBT.Tests.Server/`
Expected: All tests pass

- [ ] **Step 5: Commit**

```bash
git add src/Server/Services/LYBT.WebAPI/Controllers/RegistrationsController.cs src/Client/Desktop/LocalWebAPI/Controllers/RegistrationsController.cs
git commit -m "refactor(registrations): consolidate RegistrationsController using BaseCrudController"
```

---

## Task 6: 重构 MedicalCasesController（Server + LocalWebAPI）

**Covers:** [S7]

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCasesController.cs`
- Modify: `src/Client/Desktop/LocalWebAPI/Controllers/MedicalCasesController.cs`

**Interfaces:**
- Consumes: `BaseCrudController<MedicalCaseListDto, MedicalCaseDetailDto, MedicalCaseInputDto, GetMedicalCasesQuery, CreateMedicalCaseCommand>`
- Produces: 简化后的 MedicalCasesController

- [ ] **Step 1: 重构 Server MedicalCasesController**

```csharp
using Asp.Versioning;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.MedicalCases.Application.Commands;
using LYBT.Module.MedicalCases.Application.Queries;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;

namespace LYBT.WebAPI.Controllers
{
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Policy = PolicyConstants.DoctorOrReceptionist)]
    public class MedicalCasesController : BaseCrudController<MedicalCaseListDto, MedicalCaseDetailDto, MedicalCaseInputDto, GetMedicalCasesQuery, CreateMedicalCaseCommand>
    {
        public MedicalCasesController(ISender sender, ILogger<MedicalCasesController> logger)
            : base(sender, logger)
        {
        }

        // 重写 GetList 添加 OutputCache
        [HttpGet]
        [OutputCache(PolicyName = "MedicalCasesCache")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<MedicalCaseListDto>>), 200)]
        public override async Task<IActionResult> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            CancellationToken ct = default)
        {
            if (ValidatePagination(page, pageSize) is { } error) return error;
            var query = new GetMedicalCasesQuery(page, pageSize, keyword);
            var result = await Sender.Send(query, ct);
            if (!result.IsSuccess) return BusinessFail(result.Error ?? "查询失败");
            return SuccessPaged(result.Value!, "查询成功");
        }

        // 特殊端点：搜索医案
        [HttpGet("search")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<MedicalCaseListDto>>), 200)]
        public async Task<IActionResult> Search([FromQuery] SearchMedicalCasesQuery query, CancellationToken ct)
        {
            var result = await Sender.Send(query, ct);
            if (!result.IsSuccess) return BusinessFail(result.Error ?? "搜索失败");
            return SuccessPaged(result.Value!, "搜索成功");
        }

        // 特殊端点：按状态查询
        [HttpGet("by-status/{status}")]
        [ProducesResponseType(typeof(ApiResponse<List<MedicalCaseListDto>>), 200)]
        public async Task<IActionResult> GetByStatus(string status, CancellationToken ct)
        {
            var result = await Sender.Send(new GetMedicalCasesByStatusQuery(status), ct);
            if (!result.IsSuccess || result.Value == null)
                return BusinessFail(result.Error ?? "查询失败");
            return Success(result.Value, "查询成功");
        }

        // 特殊端点：获取待处理医案
        [HttpGet("pending")]
        [ProducesResponseType(typeof(ApiResponse<List<MedicalCaseListDto>>), 200)]
        public async Task<IActionResult> GetPending(CancellationToken ct)
        {
            var result = await Sender.Send(new GetPendingMedicalCasesQuery(), ct);
            if (!result.IsSuccess || result.Value == null)
                return BusinessFail(result.Error ?? "查询失败");
            return Success(result.Value, "查询成功");
        }

        // 特殊端点：保存医案草稿
        [HttpPost("{id:guid}/save-draft")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 200)]
        public async Task<IActionResult> SaveDraft(Guid id, [FromBody] MedicalCaseInputDto dto, CancellationToken ct)
        {
            if (ValidateGuid(id, "医案ID") is { } error) return error;
            var (operatorId, _, _) = GetOperator();
            var command = new SaveMedicalCaseDraftCommand(id, dto, operatorId);
            var result = await Sender.Send(command, ct);
            if (!result.IsSuccess || result.Value == null)
                return BusinessFail(result.Error ?? "保存草稿失败");
            LogOperation("保存医案草稿", result.Value, id);
            return Success(result.Value, "草稿保存成功");
        }

        // 特殊端点：提交医案
        [HttpPost("{id:guid}/submit")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 200)]
        public async Task<IActionResult> Submit(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "医案ID") is { } error) return error;
            var (operatorId, _, _) = GetOperator();
            var command = new SubmitMedicalCaseCommand(id, operatorId);
            var result = await Sender.Send(command, ct);
            if (!result.IsSuccess || result.Value == null)
                return BusinessFail(result.Error ?? "提交医案失败");
            LogOperation("提交医案", result.Value, id);
            return Success(result.Value, "医案已提交");
        }

        // 特殊端点：获取医案统计
        [HttpGet("statistics")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseStatisticsDto>), 200)]
        public async Task<IActionResult> GetStatistics(CancellationToken ct)
        {
            var result = await Sender.Send(new GetMedicalCaseStatisticsQuery(), ct);
            if (!result.IsSuccess || result.Value == null)
                return BusinessFail(result.Error ?? "获取统计失败");
            return Success(result.Value, "查询成功");
        }

        #region 基类抽象方法实现
        protected override GetMedicalCasesQuery CreateGetListQuery(int page, int pageSize, string? keyword)
            => new GetMedicalCasesQuery(page, pageSize, keyword);

        protected override GetMedicalCasesQuery CreateGetByIdQuery(Guid id)
            => new GetMedicalCaseQuery(id);

        protected override CreateMedicalCaseCommand CreateCreateCommand(MedicalCaseInputDto dto, Guid operatorId)
            => new CreateMedicalCaseCommand(dto, operatorId);

        protected override UpdateMedicalCaseCommand CreateUpdateCommand(Guid id, MedicalCaseInputDto dto, Guid operatorId)
            => new UpdateMedicalCaseCommand(id, dto, operatorId);

        protected override DeleteMedicalCaseCommand CreateDeleteCommand(Guid id, Guid operatorId)
            => new DeleteMedicalCaseCommand(id, operatorId);

        protected override ToggleMedicalCaseStatusCommand CreateToggleStatusCommand(Guid id, Guid operatorId)
            => new ToggleMedicalCaseStatusCommand(id, operatorId);

        protected override RestoreMedicalCaseCommand CreateRestoreCommand(Guid id, Guid operatorId)
            => new RestoreMedicalCaseCommand(id, operatorId);

        protected override BatchDeleteMedicalCasesCommand CreateBatchDeleteCommand(List<Guid> ids, Guid operatorId)
            => new BatchDeleteMedicalCasesCommand(ids, operatorId);
        #endregion
    }
}
```

- [ ] **Step 2: 重构 LocalWebAPI MedicalCasesController**

```csharp
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.MedicalCases.Application.Commands;
using LYBT.Module.MedicalCases.Application.Queries;
using LYBT.Shared.Models.Contracts.MedicalCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.LocalWebAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = PolicyConstants.DoctorOrReceptionist)]
public class MedicalCasesController : BaseCrudController<MedicalCaseListDto, MedicalCaseDetailDto, MedicalCaseInputDto, GetMedicalCasesQuery, CreateMedicalCaseCommand>
{
    public MedicalCasesController(ISender sender, ILogger<MedicalCasesController> logger)
        : base(sender, logger)
    {
    }

    // 特殊端点：搜索医案
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] SearchMedicalCasesQuery query, CancellationToken ct)
    {
        var result = await Sender.Send(query, ct);
        if (!result.IsSuccess) return BusinessFail(result.Error ?? "搜索失败");
        return SuccessPaged(result.Value!, "搜索成功");
    }

    // 特殊端点：按状态查询
    [HttpGet("by-status/{status}")]
    public async Task<IActionResult> GetByStatus(string status, CancellationToken ct)
    {
        var result = await Sender.Send(new GetMedicalCasesByStatusQuery(status), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "查询失败");
        return Success(result.Value, "查询成功");
    }

    // 特殊端点：获取待处理医案
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending(CancellationToken ct)
    {
        var result = await Sender.Send(new GetPendingMedicalCasesQuery(), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "查询失败");
        return Success(result.Value, "查询成功");
    }

    // 特殊端点：保存医案草稿
    [HttpPost("{id:guid}/save-draft")]
    public async Task<IActionResult> SaveDraft(Guid id, [FromBody] MedicalCaseInputDto dto, CancellationToken ct)
    {
        if (ValidateGuid(id, "医案ID") is { } error) return error;
        var (operatorId, _, _) = GetOperator();
        var command = new SaveMedicalCaseDraftCommand(id, dto, operatorId);
        var result = await Sender.Send(command, ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "保存草稿失败");
        LogOperation("保存医案草稿", result.Value, id);
        return Success(result.Value, "草稿保存成功");
    }

    // 特殊端点：提交医案
    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct)
    {
        if (ValidateGuid(id, "医案ID") is { } error) return error;
        var (operatorId, _, _) = GetOperator();
        var command = new SubmitMedicalCaseCommand(id, operatorId);
        var result = await Sender.Send(command, ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "提交医案失败");
        LogOperation("提交医案", result.Value, id);
        return Success(result.Value, "医案已提交");
    }

    // 特殊端点：获取医案统计
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics(CancellationToken ct)
    {
        var result = await Sender.Send(new GetMedicalCaseStatisticsQuery(), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "获取统计失败");
        return Success(result.Value, "查询成功");
    }

    #region 基类抽象方法实现
    protected override GetMedicalCasesQuery CreateGetListQuery(int page, int pageSize, string? keyword)
        => new GetMedicalCasesQuery(page, pageSize, keyword);

    protected override GetMedicalCasesQuery CreateGetByIdQuery(Guid id)
        => new GetMedicalCaseQuery(id);

    protected override CreateMedicalCaseCommand CreateCreateCommand(MedicalCaseInputDto dto, Guid operatorId)
        => new CreateMedicalCaseCommand(dto, operatorId);

    protected override UpdateMedicalCaseCommand CreateUpdateCommand(Guid id, MedicalCaseInputDto dto, Guid operatorId)
        => new UpdateMedicalCaseCommand(id, dto, operatorId);

    protected override DeleteMedicalCaseCommand CreateDeleteCommand(Guid id, Guid operatorId)
        => new DeleteMedicalCaseCommand(id, operatorId);

    protected override ToggleMedicalCaseStatusCommand CreateToggleStatusCommand(Guid id, Guid operatorId)
        => new ToggleMedicalCaseStatusCommand(id, operatorId);

    protected override RestoreMedicalCaseCommand CreateRestoreCommand(Guid id, Guid operatorId)
        => new RestoreMedicalCaseCommand(id, operatorId);

    protected override BatchDeleteMedicalCasesCommand CreateBatchDeleteCommand(List<Guid> ids, Guid operatorId)
        => new BatchDeleteMedicalCasesCommand(ids, operatorId);
    #endregion
}
```

- [ ] **Step 3: 验证编译**

Run: `dotnet build LYBTZYZS.sln`
Expected: Build succeeded

- [ ] **Step 4: 运行测试**

Run: `dotnet test tests/LYBT.Tests.Server/`
Expected: All tests pass

- [ ] **Step 5: Commit**

```bash
git add src/Server/Services/LYBT.WebAPI/Controllers/MedicalCasesController.cs src/Client/Desktop/LocalWebAPI/Controllers/MedicalCasesController.cs
git commit -m "refactor(medicalcases): consolidate MedicalCasesController using BaseCrudController"
```

---

## Task 7: 最终验证和文档更新

**Covers:** [S8]

**Files:**
- Modify: `docs/AGENTS.md`（更新架构部分）
- Modify: `docs/compose/plans/TODO-backlog.md`（标记 T8 完成）

**Interfaces:**
- Consumes: 所有重构后的 Controller
- Produces: 更新后的文档

- [ ] **Step 1: 运行完整测试套件**

Run: `dotnet test`
Expected: All tests pass (Server 711/712, Architecture 82/83)

- [ ] **Step 2: 更新 AGENTS.md**

在 `docs/AGENTS.md` 的 Key Patterns 部分添加：

```markdown
- **BaseCrudController&lt;TListDto, TDetailDto, TInputDto, TQuery, TCommand&gt;** 泛型 CRUD 基类 — Server/LocalWebAPI 共享
```

- [ ] **Step 3: 更新 TODO-backlog.md**

在 `docs/compose/plans/TODO-backlog.md` 中将 T8 标记为完成：

```markdown
| **T8** | Controller 共享基类下沉（6 对合并） | Phase 2 T17 deferred — Herbs/Formula/Patients/Users/Registrations/MedicalCases Controller 的重复 CRUD 模式合并到基类。复杂度高。 | **大 (1-2d)** | **状态: DONE** | 0 |
```

- [ ] **Step 4: 最终 Commit**

```bash
git add docs/AGENTS.md docs/compose/plans/TODO-backlog.md
git commit -m "docs: update AGENTS.md and TODO-backlog for T8 completion"
```

---

## Summary

**Total Tasks:** 7

**Estimated Effort:** 1-2 days

**Key Deliverables:**
1. `BaseCrudController<TListDto, TDetailDto, TInputDto, TQuery, TCommand>` 泛型基类
2. 5 对 Controller 重构（Patients/Herbs/Formulas/Registrations/MedicalCases）
3. Server 版本保留 OutputCache/RateLimiting/Ownership 检查
4. LocalWebAPI 版本保持简化
5. 所有测试通过
6. 文档更新

**Risk Mitigation:**
- 每个 Task 完成后立即运行测试
- 保留特殊端点在子类中
- 保持向后兼容
