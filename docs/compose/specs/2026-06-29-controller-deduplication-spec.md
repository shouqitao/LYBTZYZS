# Controller 去重规格

> 日期: 2026-06-29
> 状态: 草稿
> 优先级: HIGH
> 来源: server-services-architecture-optimization.md [O1] H1/H2

## [S1] 问题

WebAPI 和 LocalWebAPI 有 10 对重复 Controller，维护成本高：

| Controller | WebAPI 行数 | LocalWebAPI 行数 | 重复度 |
|------------|------------|-----------------|--------|
| AuthController | ~200 | ~150 | 75% |
| UsersController | 610 | 599 | 95% |
| PatientsController | ~300 | ~280 | 90% |
| HerbsController | ~350 | ~320 | 90% |
| FormulasController | ~400 | ~380 | 95% |
| MedicalCasesController | ~500 | ~480 | 95% |
| RegistrationsController | ~350 | ~330 | 95% |
| ReportsController | ~100 | ~90 | 90% |
| HealthController | ~50 | ~50 | 100% |
| DiagnosticsController | ~80 | ~80 | 100% |

**额外问题**: MedicalCaseProcessingController 独立存在，应合并到 MedicalCasesController。

## [S2] 目标

1. 提取共享基类，减少重复代码 50%+
2. 合并 MedicalCaseProcessingController
3. 统一权限策略
4. 统一版本控制

## [S3] 共享基类设计

### BaseMedicalCasesController

```csharp
// 位置: LYBT.Infrastructure/Web/BaseMedicalCasesController.cs
public abstract class BaseMedicalCasesController : BaseApiController
{
    protected readonly IMedicalCaseFacade _facade;
    
    protected BaseMedicalCasesController(IMedicalCaseFacade facade)
    {
        _facade = facade;
    }
    
    [HttpGet("{id}")]
    public virtual async Task<IActionResult> GetById(Guid id)
    {
        var result = await _facade.GetByIdAsync(id);
        return HandleResult(result);
    }
    
    [HttpGet]
    public virtual async Task<IActionResult> GetList(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? keyword = null,
        [FromQuery] MedicalCaseStatus? status = null,
        [FromQuery] Guid? patientId = null)
    {
        var query = new MedicalCaseQueryDto
        {
            PageIndex = pageIndex,
            PageSize = pageSize,
            Keyword = keyword,
            Status = status,
            PatientId = patientId
        };
        var result = await _facade.GetListAsync(query);
        return HandleResult(result);
    }
    
    [HttpPost]
    public virtual async Task<IActionResult> Create([FromBody] MedicalCaseInputDto dto)
    {
        var result = await _facade.CreateAsync(dto);
        return HandleResult(result, 201);
    }
    
    [HttpPut("{id}")]
    public virtual async Task<IActionResult> Save(Guid id, [FromBody] MedicalCaseInputDto dto)
    {
        var result = await _facade.SaveAsync(id, dto);
        return HandleResult(result);
    }
    
    [HttpDelete("{id}")]
    public virtual async Task<IActionResult> Delete(Guid id)
    {
        var result = await _facade.DeleteAsync(id);
        return HandleResult(result);
    }
    
    // 状态操作（原 MedicalCaseProcessingController）
    [HttpPut("{id}/status")]
    public virtual async Task<IActionResult> UpdateStatus(Guid id, [FromBody] MedicalCaseStatusInputDto dto)
    {
        var result = await _facade.UpdateStatusAsync(id, dto);
        return HandleResult(result);
    }
    
    [HttpPut("{id}/close")]
    public virtual async Task<IActionResult> Close(Guid id)
    {
        var result = await _facade.CloseAsync(id);
        return HandleResult(result);
    }
    
    [HttpPut("{id}/suspend")]
    public virtual async Task<IActionResult> Suspend(Guid id, [FromBody] ConsultationInputDto? dto)
    {
        var result = await _facade.SuspendAsync(id, dto);
        return HandleResult(result);
    }
    
    [HttpPut("{id}/cancel")]
    public virtual async Task<IActionResult> Cancel(Guid id, [FromBody] CancelMedicalCaseRequestDto dto)
    {
        var result = await _facade.CancelAsync(id, dto);
        return HandleResult(result);
    }
}
```

### WebAPI 实现

```csharp
// 位置: LYBT.WebAPI/Controllers/MedicalCasesController.cs
[ApiController]
[Route("api/v{version:apiVersion}/medicalcases")]
[ApiVersion("1")]
[Authorize(Policy = PolicyConstants.DoctorOrAdmin)]
public class MedicalCasesController : BaseMedicalCasesController
{
    public MedicalCasesController(IMedicalCaseFacade facade) : base(facade) { }
    
    // WebAPI 特有逻辑（如有）
}
```

### LocalWebAPI 实现

```csharp
// 位置: LYBT.LocalWebAPI/Controllers/MedicalCasesController.cs
[ApiController]
[Route("api/v{version:apiVersion}/medicalcases")]
[ApiVersion("1")]
[Authorize]
public class MedicalCasesController : BaseMedicalCasesController
{
    public MedicalCasesController(IMedicalCaseFacade facade) : base(facade) { }
    
    // LocalWebAPI 特有逻辑（如有）
}
```

## [S4] 权限策略统一

| 操作 | 权限 | 说明 |
|------|------|------|
| 查看列表 | DoctorOrAdmin | 医生和管理员可查看 |
| 创建 | DoctorOrAdmin | 仅医生和管理员可创建 |
| 编辑 | DoctorOrAdmin | 仅医生和管理员可编辑 |
| 删除 | AdminOrSuperAdmin | 仅管理员可删除 |
| 状态变更 | DoctorOrAdmin | 仅医生和管理员可变更 |
| 挂号创建 | DoctorOrReceptionist | 前台和医生可创建 |
| 挂号取消 | DoctorOrReceptionist | 前台和医生可取消 |

## [S5] 版本控制统一

```csharp
// 统一使用 apiVersion
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1")]
public abstract class BaseMedicalCasesController : BaseApiController
{
    // 所有端点使用 apiVersion
}
```

## [S6] 实施步骤

1. 在 `LYBT.Infrastructure/Web/` 创建 `BaseMedicalCasesController`
2. 修改 WebAPI MedicalCasesController 继承基类
3. 修改 LocalWebAPI MedicalCasesController 继承基类
4. 删除 MedicalCaseProcessingController（合并到基类）
5. 为其他 Controller 创建类似的基类（可选）
6. 统一权限策略
7. 统一版本控制
8. 运行 `dotnet build` 和 `dotnet test` 验证

## [S7] 验收标准

1. MedicalCasesController 重复代码减少 50%+
2. MedicalCaseProcessingController 已合并
3. 权限策略统一
4. 版本控制统一
5. 所有现有测试通过
6. `dotnet build` 成功
