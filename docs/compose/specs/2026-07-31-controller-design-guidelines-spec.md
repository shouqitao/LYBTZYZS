# Controller 设计统一化规范

## [S1] 问题

当前 Controller 层存在多处设计不一致：

| 维度 | 现状问题 |
|------|----------|
| **继承路径** | Herbs/Formulas/Patients 用 `BaseCrudController`；MedicalCases/Registrations/Users 用各自专用基类，未复用通用 CRUD |
| **错误处理** | 有的检查 `result.IsSuccess`，有的检查 `result.Value == null`；"不存在"有的用 `NotFound`，有的用 `BusinessFail` |
| **验证** | 有的开头验证 `ValidateGuid`，有的没有；有的验证分页，有的没有 |
| **日志** | 有的调用 `LogOperation`，有的没有 |
| **授权** | 有的类级别 `[Authorize]`，有的方法级别；Policy 用法不统一 |

## [S2] 目标

统一所有 Controller 的设计模式，消除重复代码，确保一致性和可维护性。

## [S3] 设计决策

### 3.1 继承结构

**决策：** 所有业务 Controller 统一继承 `BaseCrudController`（或其变体）。

**当前层级：**
```
BaseApiController
├── BaseCrudController<TListDto, TDetailDto, TInputDto, TQuery>  ← 通用 CRUD
│   ├── HerbsController ✓
│   ├── FormulasController ✓
│   └── PatientsController ✓
├── BaseMedicalCasesController  ← 重复 CRUD 代码
├── BaseRegistrationsController  ← 重复 CRUD 代码
├── BaseUsersController  ← 重复 CRUD 代码
```

**目标层级：**
```
BaseApiController
├── BaseCrudController<TListDto, TDetailDto, TInputDto, TQuery>
│   ├── HerbsController ✓
│   ├── FormulasController ✓
│   ├── PatientsController ✓
│   ├── BaseMedicalCasesController  ← 轻薄子类，只保留特化方法
│   ├── BaseRegistrationsController  ← 轻薄子类，只保留特化方法
│   └── BaseUsersController  ← 轻薄子类，只保留特化方法
```

### 3.2 非标准方法处理

**决策：** 保留在子类作为特化方法，`BaseCrudController` 只提供标准 CRUD。

`BaseCrudController` 提供的标准方法：
- `GetList` — 分页查询
- `GetById` — 根据 ID 获取详情
- `Create` — 创建资源
- `Update` — 更新资源
- `Delete` — 删除资源（软删除）
- `ToggleStatus` — 切换状态
- `Restore` — 恢复已删除资源
- `BatchDelete` — 批量删除

业务特化方法由子类自己添加，例如：
- `BaseMedicalCasesController`: `GetPatientConsultations`, `GetPatientPrescriptions`, `GetConsultations`, `GetPrescriptions`, `GetBatchDetails`, `GetPermissions`, `GetAuditLogs`, `SetPrescriptionFlag`, `RecordPrint`
- `BaseRegistrationsController`: `GetQueue`, `StartVisit`, `Cancel`, `QuickVisit`
- `BaseUsersController`: `GetCurrentUser`, `ResetPassword`, `ChangeProfile`, `ChangePassword`, `BatchEnable`, `BatchDisable`

### 3.3 错误处理

**决策：** 智能映射错误类型。

```csharp
// 统一的错误处理模式
if (!result.IsSuccess)
{
    if (result.Error?.Contains("不存在") == true)
        return NotFound(result.Error);
    return BusinessFail(result.Error ?? "操作失败");
}

if (result.Value == null)
    return NotFound("资源不存在");
```

**规则：**
- `result.IsSuccess == false && result.Error` 包含"不存在" → `NotFound`
- `result.IsSuccess == false` 其他情况 → `BusinessFail`
- `result.IsSuccess == true && result.Value == null` → `NotFound`（理论上不应发生）

### 3.4 验证模式

**决策：** 基类统一处理，子类必须遵循。

**规则：**
- 所有接收 `Guid id` 参数的方法必须在开头验证 `ValidateGuid`
- 所有接收分页参数的方法必须在开头验证 `ValidatePagination`
- 使用模式：`if (ValidateGuid(id, "资源ID") is { } error) return error;`

### 3.5 日志记录

**决策：** 写操作必须记录，查询不记录。

**规则：**
- 必须调用 `LogOperation` 的操作：`Create`, `Update`, `Delete`, `ToggleStatus`, `Restore`, `BatchDelete`
- 不需要日志的操作：`GetList`, `GetById`, 其他查询操作
- 使用模式：`LogOperation("操作描述", result.Value, id);`

### 3.6 授权模式

**决策：** 类级别 `[Authorize]` + 方法级别 Policy。

**规则：**
- 类级别使用 `[Authorize]` 表示需要登录
- 方法级别使用 `[Authorize(Policy = PolicyConstants.XXX)]` 表示需要特定角色
- 移除冗余的 `[AllowAnonymous]`（如果类级别已经有 `[Authorize]`）

**常用 Policy：**
- `PolicyConstants.AdminOrSuperAdmin` — 管理员或超级管理员
- `PolicyConstants.DoctorOrReceptionist` — 医生或前台
- `PolicyConstants.DoctorOrAdminOrReceptionist` — 医生、管理员或前台

## [S4] BaseCrudController 标准签名

```csharp
public abstract class BaseCrudController<TListDto, TDetailDto, TInputDto, TQuery>
    : BaseApiController
    where TQuery : IRequest<Result<PagedResult<TListDto>>>
{
    // 标准方法签名
    virtual Task<IActionResult> GetList(int page, int pageSize, string? keyword, CancellationToken ct);
    virtual Task<IActionResult> GetById(Guid id, CancellationToken ct);
    virtual Task<IActionResult> Create(TInputDto dto, CancellationToken ct);
    virtual Task<IActionResult> Update(Guid id, TInputDto dto, CancellationToken ct);
    virtual Task<IActionResult> Delete(Guid id, CancellationToken ct);
    virtual Task<IActionResult> ToggleStatus(Guid id, CancellationToken ct);
    virtual Task<IActionResult> Restore(Guid id, CancellationToken ct);
    virtual Task<IActionResult> BatchDelete(BatchDeleteInputDto dto, CancellationToken ct);

    // 子类必须实现的抽象方法
    abstract TQuery CreateGetListQuery(int page, int pageSize, string? keyword);
    abstract IRequest<Result<TDetailDto>> CreateCreateCommand(TInputDto dto, Guid operatorId);
    abstract IRequest<Result<TDetailDto>> CreateUpdateCommand(Guid id, TInputDto dto, Guid operatorId);
    abstract IRequest<Result> CreateDeleteCommand(Guid id, Guid operatorId);
    abstract IRequest<Result<TDetailDto>> CreateToggleStatusCommand(Guid id, Guid operatorId);
    abstract IRequest<Result<TDetailDto>> CreateRestoreCommand(Guid id, Guid operatorId);
    abstract IRequest<Result<BatchOperationResultDto>> CreateBatchDeleteCommand(List<Guid> ids, Guid operatorId);
}
```

## [S5] 子类实现模板

```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]  // 类级别：需要登录
public class XxxController : BaseCrudController<XxxListDto, XxxDetailDto, XxxInputDto,GetXXxsQuery>
{
    public XxxController(ISender sender, ILogger<XxxController> logger)
        : base(sender, logger) { }

    // 1. Override GetById（如有特殊逻辑）
    [HttpGet("{id:guid}")]
    [Authorize(Policy = PolicyConstants.DoctorOrReceptionist)]  // 方法级别：需要特定角色
    public override async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        if (ValidateGuid(id, "资源ID") is { } error) return error;
        // 特殊逻辑...
    }

    // 2. 添加业务特化方法
    [HttpGet("special-action")]
    public async Task<IActionResult> SpecialAction(CancellationToken ct)
    {
        // 业务逻辑...
    }

    // 3. 实现基类抽象方法
    #region 基类抽象方法实现
    protected override GetXxxsQuery CreateGetListQuery(int page, int pageSize, string? keyword)
        => new GetXxxsQuery(page, pageSize, keyword);
    // ... 其他抽象方法
    #endregion
}
```

## [S6] 重构范围

### 需要重构的文件

1. **BaseMedicalCasesController** (`src/Server/Modules/LYBT.Module.MedicalCase/Controllers/BaseMedicalCasesController.cs`)
   - 移除重复的 CRUD 代码（GetList, GetById, Create, Save/Update, Delete, BatchDelete）
   - 继承 `BaseCrudController<MedicalCaseListDto, MedicalCaseDetailDto, MedicalCaseInputDto, GetMedicalCasesQuery>`
   - 保留特化方法：GetPatientConsultations, GetPatientPrescriptions, GetConsultations, GetPrescriptions, GetBatchDetails, GetPermissions, GetAuditLogs, SetPrescriptionFlag, RecordPrint

2. **BaseRegistrationsController** (`src/Server/Modules/LYBT.Module.Registration/Controllers/BaseRegistrationsController.cs`)
   - 移除重复的 CRUD 代码（GetList, GetById, Create）
   - 继承 `BaseCrudController<RegistrationListDto, RegistrationDetailDto, RegistrationInputDto, GetRegistrationsQuery>`
   - 保留特化方法：GetQueue, StartVisit, Cancel, QuickVisit

3. **BaseUsersController** (`src/Server/Modules/LYBT.Module.Users/Controllers/BaseUsersController.cs`)
   - 移除重复的 CRUD 代码（GetList, GetById, Create, Update, Delete, ToggleStatus, Restore, BatchDelete）
   - 继承 `BaseCrudController<UserListDto, UserDetailDto, UserInputDto, GetUsersQuery>`
   - 保留特化方法：GetCurrentUser, ResetPassword, ChangeProfile, ChangePassword, BatchEnable, BatchDisable

### 不需要修改的文件

- `BaseApiController` — 保持不变
- `BaseCrudController` — 保持不变（已提供标准 CRUD）
- `HerbsController`, `FormulasController`, `PatientsController` — 已正确继承 `BaseCrudController`
- `HealthController`, `AuthController`, `ConfigurationController`, `DiagnosticsController` — 不是业务 CRUD Controller，保持不变

## [S7] 验收标准

1. 所有业务 Controller 统一继承 `BaseCrudController`
2. 重复的 CRUD 代码被消除
3. 错误处理统一（"不存在" → NotFound，其他 → BusinessFail）
4. 验证统一（ValidateGuid, ValidatePagination）
5. 日志统一（写操作必须 LogOperation）
6. 授权统一（类级别 Authorize + 方法级别 Policy）
7. 编译通过，无回归错误
8. 现有功能保持不变

## [S8] 风险与缓解

| 风险 | 缓解措施 |
|------|----------|
| 重构引入回归错误 | 每次重构后运行 `dotnet build` 验证编译通过 |
| 破坏现有 API 签名 | 保持方法签名不变，只改变继承结构 |
| 子类 override 方法遗漏验证 | 代码审查时检查每个 override 方法的验证逻辑 |
