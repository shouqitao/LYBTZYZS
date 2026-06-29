# Server/Services 架构优化

> 日期: 2026-06-29
> 状态: 草稿
> 范围: LYBT.WebAPI

## [O1] 当前架构问题

### 高优先级（HIGH）

| # | 问题 | 位置 | 影响 |
|---|------|------|------|
| H1 | Controller 重复代码 | WebAPI + LocalWebAPI | 10 对 Controller 几乎相同 |
| H2 | 医疗 Case Processing Controller 分离 | MedicalCaseProcessingController.cs | 应合并到 MedicalCasesController |
| H3 | 权限策略不一致 | 各 Controller | 部分使用 DoctorOrAdmin，部分使用 DoctorOrReceptionist |

### 中优先级（MEDIUM）

| # | 问题 | 位置 | 影响 |
|---|------|------|------|
| M1 | 部分 Controller 无 BaseApiController | 各 Controller | 缺乏统一的响应处理 |
| M2 | 版本控制不一致 | Route 模板 | 部分使用 apiVersion，部分使用固定 v1 |

## [O2] Controller 清单

| Controller | 行数 | 权限 | 问题 |
|------------|------|------|------|
| AuthController | ~200 | 匿名/Bearer | 正常 |
| UsersController | 610 | AdminOrSuperAdmin | 过长，包含权限逻辑 |
| PatientsController | ~300 | DoctorOrAdmin | 正常 |
| HerbsController | ~350 | DoctorOrAdmin | 正常 |
| FormulasController | ~400 | DoctorOrAdmin | 正常 |
| MedicalCasesController | ~500 | DoctorOrAdmin | 正常 |
| MedicalCaseProcessingController | ~200 | DoctorOrAdmin | 应合并 |
| RegistrationsController | ~350 | DoctorOrAdmin | 正常 |
| ReportsController | ~100 | DoctorOrAdmin | 正常 |
| HealthController | ~50 | 匿名 | 正常 |

## [O3] 优化方案

### 方案 1: Controller 去重（H1）

```markdown
当前: WebAPI 和 LocalWebAPI 各有独立的 Controller
问题: 10 对 Controller 几乎相同，维护成本高

解决方案: 提取共享基类

1. 在 LYBT.Infrastructure 中定义 BaseApiController（当前已有）
2. WebAPI 和 LocalWebAPI 的 Controller 继承同一基类
3. 共享逻辑放在基类中
4. 各 Controller 仅包含特有逻辑

示例:
public abstract class BaseMedicalCasesController : BaseApiController
{
    protected readonly IMedicalCaseFacade _facade;
    
    [HttpGet("{id}")]
    public virtual async Task<IActionResult> GetById(Guid id)
    {
        var result = await _facade.GetByIdAsync(id);
        return HandleResult(result);
    }
}

// WebAPI
[ApiController]
[Route("api/v{version:apiVersion}/medicalcases")]
[Authorize(Policy = PolicyConstants.DoctorOrAdmin)]
public class MedicalCasesController : BaseMedicalCasesController
{
    // WebAPI 特有逻辑
}

// LocalWebAPI
[ApiController]
[Route("api/v1/medicalcases")]
[Authorize]
public class MedicalCasesController : BaseMedicalCasesController
{
    // LocalWebAPI 特有逻辑
}
```

### 方案 2: 合并 MedicalCaseProcessingController（H2）

```csharp
// 当前: MedicalCaseProcessingController 独立
// 优化: 合并到 MedicalCasesController

[ApiController]
[Route("api/v{version:apiVersion}/medicalcases")]
[Authorize(Policy = PolicyConstants.DoctorOrAdmin)]
public class MedicalCasesController : BaseApiController
{
    // CRUD 操作
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] MedicalCaseInputDto dto) { }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id) { }
    
    // ... 其他 CRUD
    
    // 状态操作（原 MedicalCaseProcessingController）
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] MedicalCaseStatusInputDto dto) { }
    
    [HttpPut("{id}/close")]
    public async Task<IActionResult> Close(Guid id) { }
    
    [HttpPut("{id}/suspend")]
    public async Task<IActionResult> Suspend(Guid id, [FromBody] ConsultationInputDto? dto) { }
    
    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelMedicalCaseRequestDto dto) { }
}
```

### 方案 3: 统一权限策略（H3）

```markdown
当前问题: 权限策略不一致
- 大部分 Controller: DoctorOrAdmin
- 部分操作: DoctorOrReceptionist
- 注册创建: 应该是 DoctorOrReceptionist

解决方案: 统一权限矩阵

| 操作 | 权限 | 说明 |
|------|------|------|
| 查看列表 | DoctorOrAdmin | 医生和管理员可查看 |
| 创建 | DoctorOrAdmin | 仅医生和管理员可创建 |
| 编辑 | DoctorOrAdmin | 仅医生和管理员可编辑 |
| 删除 | AdminOrSuperAdmin | 仅管理员可删除 |
| 状态变更 | DoctorOrAdmin | 仅医生和管理员可变更 |
| 挂号创建 | DoctorOrReceptionist | 前台和医生可创建 |
| 挂号取消 | DoctorOrReceptionist | 前台和医生可取消 |
```

### 方案 4: 统一版本控制（M2）

```csharp
// 当前: 部分使用 apiVersion，部分使用固定 v1
// 优化: 统一使用 apiVersion

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1")]
public class MedicalCasesController : BaseApiController
{
    // 所有端点使用 apiVersion
}

// LocalWebAPI 也使用 apiVersion
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1")]
public class MedicalCasesController : BaseApiController
{
    // 与 WebAPI 保持一致
}
```

## [O4] 实施优先级

| 批次 | 任务 | 工作量 | 风险 |
|------|------|--------|------|
| 1 | H2: 合并 MedicalCaseProcessingController | 低 | 低 |
| 2 | H1: Controller 去重 | 中 | 中 |
| 3 | H3: 统一权限策略 | 低 | 低 |
| 4 | M2: 统一版本控制 | 低 | 低 |

## [O5] 成功标准

1. **Controller 去重**: 共享基类减少重复代码 50%+
2. **合并**: MedicalCaseProcessingController 合并到 MedicalCasesController
3. **权限**: 统一权限矩阵，无策略不一致
4. **版本**: 统一使用 apiVersion
