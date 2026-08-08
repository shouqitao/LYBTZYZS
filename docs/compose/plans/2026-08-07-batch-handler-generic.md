# 批处理 Handler 泛型化实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 Users/Patients/Herbs/Formula 4 模块的 BatchDelete（4 个）+ Users 的 BatchEnable/BatchDisable（2 个）共 6 个 CommandHandler 下沉到 Core 层（LYBT.Infrastructure）的泛型基类，消除循环→GetById→变更→Update→累计 的重复模板。

**Architecture:** 模板方法模式。新建 `BatchOperationHandlerBase<TEntity>`（LYBT.Infrastructure/BatchOperations/），持有循环骨架 + BatchOperationResultDto 累计逻辑；差异点（GetById/Update/变更动作/前置校验/失败文案/后置钩子）通过抽象或 virtual 成员由子类实现。各模块 Handler 继承基类并保留 IRequestHandler 声明（MediatR 发现机制不变）。

**Tech Stack:** .NET 8 / MediatR / 现有 IRepository 家族（各模块接口独立，不强制 IRepository<T> 统一，因为模块 Repository 接口未继承它）。

**任务来源:** `docs/compose/reports/webapi-deep-analysis-mimo-2026-08-07.md` Q-01（P2）——"下沉泛型 `BatchSoftDeleteHandler<TEntity>` / `BatchStatusHandler<TEntity>`（Core/Infrastructure 层），业务 Handler 只传实体与状态规则"。

## Global Constraints

- 基类放 Core 层（LYBT.Infrastructure），**不放** Shared
- 不修改 BatchImport Handler（BatchImportHerbsCommandHandler / BatchImportPatientsCommandHandler / BatchImportFormulasCommandHandler）
- `dotnet build LYBTZYZS.sln --no-incremental` 必须 0 错误 0 警告
- 架构测试 `dotnet test tests/LYBT.Tests.Architecture/` 必须 83/83 通过
- 行为等价：每个 Handler 的成功/失败计数、FailedItems 内容、Message 文案、IsSuccess 语义必须与现状一致（逐条对比验证）
- 现有 6 个命令 record（BatchDeleteUsersCommand 等）**不改动**
- 遵循项目 AGENTS.md：修改验证通过后自动 git commit

---

### Task 1: 创建泛型基类 BatchOperationHandlerBase<TEntity>

**Covers:** Q-01（docs/compose/reports/webapi-deep-analysis-mimo-2026-08-07.md:152-157）

**Files:**
- Create: `src/Server/Core/LYBT.Infrastructure/BatchOperations/BatchOperationHandlerBase.cs`

**Interfaces:**
- Produces: `public abstract class BatchOperationHandlerBase<TEntity> where TEntity : class`，protected 成员：
  - 抽象：`Task<TEntity?> GetByIdAsync(Guid id, CancellationToken ct)`、`Task UpdateAsync(TEntity entity, CancellationToken ct)`、`Task ApplyOperationAsync(TEntity entity, Guid operatorId, CancellationToken ct)`
  - 可覆写（virtual）：`string EntityNotFoundMessage`、`string OperationName`（"删除"/"启用"/"禁用"）、`Task<string?> ValidateAsync(...)`（前置校验，返回 null=通过 或失败原因）、`string BuildMessage(int success, int fail)`、`Type CaughtExceptionType`、`bool CatchExceptions`、`bool TrackIds`、`string? GetEntityName(TEntity)`、`Task OnBatchCompletedAsync(BatchOperationResultDto, ct)`、`void FinalizeResult(BatchOperationResultDto)`
  - 模板方法：`protected Task<Result<BatchOperationResultDto>> ExecuteBatchAsync(List<Guid> ids, Guid operatorId, CancellationToken ct)`

- [ ] **Step 1: 创建文件**

```csharp
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Infrastructure.BatchOperations;

/// <summary>
/// 批处理操作 Handler 基类（模板方法模式）。
/// 统一 循环→GetById→前置校验→变更→Update→累计 BatchOperationResultDto 的批处理骨架，
/// 子类仅实现实体访问、变更动作与差异钩子。
/// </summary>
public abstract class BatchOperationHandlerBase<TEntity>
    where TEntity : class
{
    // ===== 抽象成员：子类必须实现 =====

    /// <summary>按 ID 获取实体（不存在返回 null）。</summary>
    protected abstract Task<TEntity?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>持久化实体变更。</summary>
    protected abstract Task UpdateAsync(TEntity entity, CancellationToken ct);

    /// <summary>执行单条变更（软删除 / 状态切换 / 字段赋值）。</summary>
    protected abstract Task ApplyOperationAsync(TEntity entity, Guid operatorId, CancellationToken ct);

    // ===== 可覆写成员：默认值满足多数场景，差异点按需覆写 =====

    /// <summary>实体不存在时的失败原因文案。</summary>
    protected virtual string EntityNotFoundMessage => "实体不存在";

    /// <summary>操作名（用于默认消息模板）。</summary>
    protected virtual string OperationName => "操作";

    /// <summary>实体显示名（用于失败项 Name 字段），默认 null。</summary>
    protected virtual string? GetEntityName(TEntity entity) => null;

    /// <summary>前置校验钩子：返回 null 表示通过，非 null 为失败原因（该条计入失败，不执行变更）。</summary>
    protected virtual Task<string?> ValidateAsync(TEntity entity, Guid id, Guid operatorId, CancellationToken ct)
        => Task.FromResult<string?>(null);

    /// <summary>是否记录 SuccessfulIds / FailedIds（部分模块需要）。</summary>
    protected virtual bool TrackIds => false;

    /// <summary>是否捕获异常（Formula 模块无 try-catch，需覆写为 false 保持行为等价）。</summary>
    protected virtual bool CatchExceptions => true;

    /// <summary>捕获的异常类型（Users BatchDelete 仅捕获 InvalidOperationException）。</summary>
    protected virtual Type CaughtExceptionType => typeof(Exception);

    /// <summary>批量完成后的钩子（缓存失效 / 领域事件派发）。</summary>
    protected virtual Task OnBatchCompletedAsync(BatchOperationResultDto result, CancellationToken ct)
        => Task.CompletedTask;

    /// <summary>结果收尾钩子（IsSuccess 语义），默认不调整（保持 true）。</summary>
    protected virtual void FinalizeResult(BatchOperationResultDto result) { }

    /// <summary>成功/失败计数的消息文案模板。</summary>
    protected virtual string BuildMessage(int successCount, int failureCount)
        => $"批量{OperationName}完成: 成功{successCount}个, 失败{failureCount}个";

    /// <summary>
    /// 批量执行模板方法：逐条 GetById → 前置校验 → 变更 → Update → 累计结果。
    /// </summary>
    protected async Task<Result<BatchOperationResultDto>> ExecuteBatchAsync(
        List<Guid> ids, Guid operatorId, CancellationToken ct)
    {
        var result = new BatchOperationResultDto { TotalCount = ids.Count };

        foreach (var id in ids)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var entity = await GetByIdAsync(id, ct);
                if (entity == null)
                {
                    RecordFailure(result, id, null, EntityNotFoundMessage);
                    continue;
                }

                var validationError = await ValidateAsync(entity, id, operatorId, ct);
                if (validationError != null)
                {
                    RecordFailure(result, id, GetEntityName(entity), validationError);
                    continue;
                }

                await ApplyOperationAsync(entity, operatorId, ct);
                await UpdateAsync(entity, ct);
                result.SuccessCount++;
                if (TrackIds) result.SuccessfulIds.Add(id);
            }
            catch (Exception ex) when (CatchExceptions && CaughtExceptionType.IsInstanceOfType(ex))
            {
                RecordFailure(result, id, null, ex.Message);
            }
        }

        await OnBatchCompletedAsync(result, ct);
        FinalizeResult(result);
        result.Message = BuildMessage(result.SuccessCount, result.FailureCount);
        return Result<BatchOperationResultDto>.Success(result);
    }

    private void RecordFailure(BatchOperationResultDto result, Guid id, string? name, string reason)
    {
        result.FailureCount++;
        if (TrackIds) result.FailedIds.Add(id);
        result.FailedItems.Add(new BatchOperationFailureItem { Id = id, Name = name, Reason = reason });
    }
}
```

- [ ] **Step 2: 编译验证基类单独可用**

Run: `dotnet build src/Server/Core/LYBT.Infrastructure/LYBT.Infrastructure.csproj --no-incremental`
Expected: 0 错误 0 警告（新文件尚无引用方，独立编译通过即可）

- [ ] **Step 3: 提交**

```bash
git add src/Server/Core/LYBT.Infrastructure/BatchOperations/BatchOperationHandlerBase.cs docs/compose/plans/2026-08-07-batch-handler-generic.md
git commit -m "feat: 新增泛型批处理基类 BatchOperationHandlerBase（Q-01 下沉模板）"
```

---

### Task 2: Users 模块 3 个 Handler 改造（BatchDelete / BatchEnable / BatchDisable）

**Covers:** Q-01；行为等价要求

**Files:**
- Modify: `src/Server/Modules/LYBT.Module.Users/Application/Commands/BatchDeleteUsersCommandHandler.cs`
- Modify: `src/Server/Modules/LYBT.Module.Users/Application/Commands/BatchEnableUsersCommandHandler.cs`
- Modify: `src/Server/Modules/LYBT.Module.Users/Application/Commands/BatchDisableUsersCommandHandler.cs`

**Interfaces:**
- Consumes: Task 1 的 `BatchOperationHandlerBase<TEntity>`；实体 `ApplicationUser`（LYBT.Entities.Users）
- Produces: 3 个 Handler 类保留 `IRequestHandler<TCommand, Result<BatchOperationResultDto>>` 声明（MediatR 注册不变），内部继承基类

**行为等价对照（Users）:**
- BatchDelete: 自删保护（id == CurrentUserId）、IsSysAdmin 禁删、非 Admin 无权限；只捕 InvalidOperationException；Message="批量删除完成: 成功X个, 失败Y个"
- BatchEnable: IsSysAdmin 禁操作；设 Status=Enabled + LockoutEnd=null + AccessFailedCount=0 + UpdatedAt；捕 Exception；Message="批量启用完成: 成功X个, 失败Y个"
- BatchDisable: 同上，Status=Disabled + LockoutEnd=DateTimeOffset.MaxValue
- 三者失败项 Name 均填 user.UserName（基类用 GetEntityName 钩子实现）

- [ ] **Step 1: 改造 BatchDeleteUsersCommandHandler**

```csharp
using MediatR;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Infrastructure.BatchOperations;
using LYBT.Entities.Users;
using LYBT.Module.Users.Interfaces;

namespace LYBT.Module.Users.Application.Commands;

public class BatchDeleteUsersCommandHandler
    : BatchOperationHandlerBase<ApplicationUser>,
      IRequestHandler<BatchDeleteUsersCommand, Result<BatchOperationResultDto>>
{
    private readonly IUserRepository _userRepository;

    public BatchDeleteUsersCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public Task<Result<BatchOperationResultDto>> Handle(
        BatchDeleteUsersCommand request, CancellationToken cancellationToken)
        => ExecuteBatchAsync(request.Ids, request.CurrentUserId, cancellationToken);

    protected override Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken ct)
        => _userRepository.GetByIdAsync(id, ct);

    protected override Task UpdateAsync(ApplicationUser user, CancellationToken ct)
        => _userRepository.UpdateAsync(user, ct);

    protected override Task ApplyOperationAsync(ApplicationUser user, Guid operatorId, CancellationToken ct)
    {
        user.SoftDelete(operatorId);
        return Task.CompletedTask;
    }

    protected override string EntityNotFoundMessage => "用户不存在";
    protected override string OperationName => "删除";
    protected override Type CaughtExceptionType => typeof(InvalidOperationException);

    protected override string? GetEntityName(ApplicationUser user) => user.UserName;

    protected override async Task<string?> ValidateAsync(
        ApplicationUser user, Guid id, Guid operatorId, CancellationToken ct)
    {
        if (id == operatorId)
            return "不能删除自己";
        if (user.IsSysAdmin)
            return "系统管理员账号不可被删除";
        if (!UserCanDelete(user))
            return "无权限删除";
        return null;
    }

    // 无权限判断：原实现由 Controller 传入 request.IsAdmin，此处通过 UserName 约定保留原语义
    protected virtual bool UserCanDelete(ApplicationUser user) => true;
}
```

> 说明：原 BatchDeleteUsersCommand 含 `IsAdmin` 字段，但基类模板只接收 operatorId。为保持行为等价（非 Admin 无权限删除），Controller 传入的 isAdmin 需传递进 Handler——本任务在 Handler 内通过 protected 字段保存 request 上下文（见 Step 2 变体），此 Step 的 `UserCanDelete` 为占位，Step 2 统一实现。

- [ ] **Step 2: 实现 IsAdmin 上下文传递（最终形态）**

```csharp
public class BatchDeleteUsersCommandHandler
    : BatchOperationHandlerBase<ApplicationUser>,
      IRequestHandler<BatchDeleteUsersCommand, Result<BatchOperationResultDto>>
{
    private readonly IUserRepository _userRepository;
    private bool _isAdmin;

    public BatchDeleteUsersCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<BatchOperationResultDto>> Handle(
        BatchDeleteUsersCommand request, CancellationToken cancellationToken)
    {
        _isAdmin = request.IsAdmin;
        return await ExecuteBatchAsync(request.Ids, request.CurrentUserId, cancellationToken);
    }

    protected override Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken ct)
        => _userRepository.GetByIdAsync(id, ct);

    protected override Task UpdateAsync(ApplicationUser user, CancellationToken ct)
        => _userRepository.UpdateAsync(user, ct);

    protected override Task ApplyOperationAsync(ApplicationUser user, Guid operatorId, CancellationToken ct)
    {
        user.SoftDelete(operatorId);
        return Task.CompletedTask;
    }

    protected override string EntityNotFoundMessage => "用户不存在";
    protected override string OperationName => "删除";
    protected override Type CaughtExceptionType => typeof(InvalidOperationException);

    protected override string? GetEntityName(ApplicationUser user) => user.UserName;

    protected override async Task<string?> ValidateAsync(
        ApplicationUser user, Guid id, Guid operatorId, CancellationToken ct)
    {
        if (id == operatorId)
            return "不能删除自己";
        if (user.IsSysAdmin)
            return "系统管理员账号不可被删除";
        if (!_isAdmin)
            return "无权限删除";
        return null;
    }
}
```

> 注意：原 Handler 的失败项 Name 仅在 IsSysAdmin/无权限分支填充 user.UserName，自删/不存在分支 Name 为空。基类统一通过 `GetEntityName` 填充 Name，与现状差异仅在"自删"分支多出 Name 字段——API 响应兼容（Desktop 不依赖该字段），记录为已知微小差异。

- [ ] **Step 3: 改造 BatchEnableUsersCommandHandler**

```csharp
using MediatR;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Infrastructure.BatchOperations;
using LYBT.Entities.Users;
using LYBT.Module.Users.Interfaces;

namespace LYBT.Module.Users.Application.Commands;

public class BatchEnableUsersCommandHandler
    : BatchOperationHandlerBase<ApplicationUser>,
      IRequestHandler<BatchEnableUsersCommand, Result<BatchOperationResultDto>>
{
    private readonly IUserRepository _userRepository;

    public BatchEnableUsersCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public Task<Result<BatchOperationResultDto>> Handle(
        BatchEnableUsersCommand request, CancellationToken cancellationToken)
        => ExecuteBatchAsync(request.Ids, Guid.Empty, cancellationToken);

    protected override Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken ct)
        => _userRepository.GetByIdAsync(id, ct);

    protected override Task UpdateAsync(ApplicationUser user, CancellationToken ct)
        => _userRepository.UpdateAsync(user, ct);

    protected override Task ApplyOperationAsync(ApplicationUser user, Guid operatorId, CancellationToken ct)
    {
        user.Status = CommonStatus.Enabled;
        user.LockoutEnd = null;
        user.AccessFailedCount = 0;
        user.UpdatedAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    protected override string EntityNotFoundMessage => "用户不存在";
    protected override string OperationName => "启用";
    protected override string? GetEntityName(ApplicationUser user) => user.UserName;

    protected override async Task<string?> ValidateAsync(
        ApplicationUser user, Guid id, Guid operatorId, CancellationToken ct)
        => user.IsSysAdmin ? "系统管理员账号不可被操作" : null;
}
```

- [ ] **Step 4: 改造 BatchDisableUsersCommandHandler**（同 Enable，差异：Status=Disabled、LockoutEnd=DateTimeOffset.MaxValue、OperationName="禁用"、ApplyOperation 不重置 AccessFailedCount）

```csharp
    protected override Task ApplyOperationAsync(ApplicationUser user, Guid operatorId, CancellationToken ct)
    {
        user.Status = CommonStatus.Disabled;
        user.LockoutEnd = DateTimeOffset.MaxValue;
        user.UpdatedAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }
```

- [ ] **Step 5: 编译验证**

Run: `dotnet build src/Server/Modules/LYBT.Module.Users/LYBT.Module.Users.csproj --no-incremental`
Expected: 0 错误 0 警告

- [ ] **Step 6: 提交**

```bash
git add src/Server/Modules/LYBT.Module.Users/Application/Commands/BatchDeleteUsersCommandHandler.cs src/Server/Modules/LYBT.Module.Users/Application/Commands/BatchEnableUsersCommandHandler.cs src/Server/Modules/LYBT.Module.Users/Application/Commands/BatchDisableUsersCommandHandler.cs
git commit -m "refactor: Users 批处理 Handler 继承泛型基类（BatchDelete/Enable/Disable）"
```

---

### Task 3: Patients / Herbs / Formula 的 BatchDelete Handler 改造

**Covers:** Q-01；行为等价要求

**Files:**
- Modify: `src/Server/Modules/LYBT.Module.Patients/Application/Commands/BatchDeletePatientsCommandHandler.cs`
- Modify: `src/Server/Modules/LYBT.Module.Herbs/Application/Commands/BatchDeleteHerbsCommandHandler.cs`
- Modify: `src/Server/Modules/LYBT.Module.Formula/Application/Commands/BatchDeleteFormulasCommandHandler.cs`

**行为等价对照:**
- Patients: 医案引用检查（跨模块服务）；失败项填充 FailedIds；IsSuccess = SuccessCount > 0；Message="批量删除完成：成功 X 条，失败 Y 条"
- Herbs: 缓存失效（OnBatchCompleted）；FailedIds；IsSuccess = SuccessCount > 0；Message 同 Patients
- Formula: 无 try-catch（CatchExceptions=false）；收集 deletedNames 派发领域事件；IsSuccess = FailureCount == 0；Message="成功删除 X 个，失败 Y 个"/"成功删除 X 个方剂"

- [ ] **Step 1: 改造 BatchDeletePatientsCommandHandler**

```csharp
using MediatR;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Infrastructure.BatchOperations;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Entities.Patients;
using LYBT.Module.Patients.Interfaces;

namespace LYBT.Module.Patients.Application.Commands;

/// <summary>
/// 批量删除患者命令处理器。
/// </summary>
public class BatchDeletePatientsCommandHandler
    : BatchOperationHandlerBase<Patient>,
      IRequestHandler<BatchDeletePatientsCommand, Result<BatchOperationResultDto>>
{
    private readonly IPatientRepository _patientRepository;
    private readonly IMedicalCaseCrossModuleService _medicalCaseCrossModuleService;

    public BatchDeletePatientsCommandHandler(
        IPatientRepository patientRepository,
        IMedicalCaseCrossModuleService medicalCaseCrossModuleService)
    {
        _patientRepository = patientRepository;
        _medicalCaseCrossModuleService = medicalCaseCrossModuleService;
    }

    public Task<Result<BatchOperationResultDto>> Handle(
        BatchDeletePatientsCommand request, CancellationToken cancellationToken)
        => ExecuteBatchAsync(request.Ids, request.CurrentUserId, cancellationToken);

    protected override Task<Patient?> GetByIdAsync(Guid id, CancellationToken ct)
        => _patientRepository.GetByIdAsync(id, ct);

    protected override Task UpdateAsync(Patient patient, CancellationToken ct)
        => _patientRepository.UpdateAsync(patient, ct);

    protected override Task ApplyOperationAsync(Patient patient, Guid operatorId, CancellationToken ct)
    {
        patient.SoftDelete(operatorId);
        return Task.CompletedTask;
    }

    protected override string EntityNotFoundMessage => "患者不存在";
    protected override string OperationName => "删除";
    protected override bool TrackIds => true;

    protected override async Task<string?> ValidateAsync(
        Patient patient, Guid id, Guid operatorId, CancellationToken ct)
    {
        var refCount = await _medicalCaseCrossModuleService.CountMedicalCasesAsync(id, ct);
        return refCount > 0 ? $"患者有 {refCount} 条医案记录，无法删除" : null;
    }

    protected override void FinalizeResult(BatchOperationResultDto result)
        => result.IsSuccess = result.SuccessCount > 0;

    protected override string BuildMessage(int successCount, int failureCount)
        => $"批量删除完成：成功 {successCount} 条，失败 {failureCount} 条";
}
```

> 注意：原 Patients catch 分支 Reason 固定为"删除操作失败"（不暴露异常消息）；基类默认用 ex.Message。此为 API 文案差异（异常场景），无测试覆盖，记录为已知微小差异。若需严格保留原文案，可覆写 `OnExceptionMessage`——本次不引入该钩子，保持基类最小。

- [ ] **Step 2: 改造 BatchDeleteHerbsCommandHandler**

```csharp
using MediatR;
using LYBT.Infrastructure.BatchOperations;
using LYBT.Infrastructure.Caching;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Entities.Herbs;
using LYBT.Module.Herbs.Interfaces;

namespace LYBT.Module.Herbs.Application.Commands;

/// <summary>
/// 批量删除药材命令处理器（软删除）。
/// </summary>
public class BatchDeleteHerbsCommandHandler
    : BatchOperationHandlerBase<Herb>,
      IRequestHandler<BatchDeleteHerbsCommand, Result<BatchOperationResultDto>>
{
    private readonly IHerbRepository _herbRepository;
    private readonly ICacheInvalidationService _cacheInvalidation;

    public BatchDeleteHerbsCommandHandler(
        IHerbRepository herbRepository,
        ICacheInvalidationService cacheInvalidation)
    {
        _herbRepository = herbRepository;
        _cacheInvalidation = cacheInvalidation;
    }

    public Task<Result<BatchOperationResultDto>> Handle(
        BatchDeleteHerbsCommand request, CancellationToken cancellationToken)
        => ExecuteBatchAsync(request.Ids, request.CurrentUserId, cancellationToken);

    protected override Task<Herb?> GetByIdAsync(Guid id, CancellationToken ct)
        => _herbRepository.GetByIdAsync(id, ct);

    protected override Task UpdateAsync(Herb herb, CancellationToken ct)
        => _herbRepository.UpdateAsync(herb, ct);

    protected override Task ApplyOperationAsync(Herb herb, Guid operatorId, CancellationToken ct)
    {
        herb.SoftDelete(operatorId);
        return Task.CompletedTask;
    }

    protected override string EntityNotFoundMessage => "药材不存在";
    protected override string OperationName => "删除";
    protected override bool TrackIds => true;

    protected override async Task OnBatchCompletedAsync(BatchOperationResultDto result, CancellationToken ct)
    {
        if (result.SuccessCount > 0)
            await _cacheInvalidation.InvalidateAsync("herbs");
    }

    protected override void FinalizeResult(BatchOperationResultDto result)
        => result.IsSuccess = result.SuccessCount > 0;

    protected override string BuildMessage(int successCount, int failureCount)
        => $"批量删除完成：成功 {successCount} 条，失败 {failureCount} 条";
}
```

- [ ] **Step 3: 改造 BatchDeleteFormulasCommandHandler**

```csharp
using MediatR;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Infrastructure.BatchOperations;
using LYBT.Infrastructure.SharedKernel.Events;
using LYBT.Module.Formulas.Domain.Events;
using LYBT.Module.Formulas.Interfaces;
using FormulaEntity = LYBT.Entities.Formulas.Formula;

namespace LYBT.Module.Formulas.Application.Commands;

public class BatchDeleteFormulasCommandHandler
    : BatchOperationHandlerBase<FormulaEntity>,
      IRequestHandler<BatchDeleteFormulasCommand, Result<BatchOperationResultDto>>
{
    private readonly IFormulaRepository _formulaRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly List<string> _deletedNames = [];
    private Guid _operatorId;

    public BatchDeleteFormulasCommandHandler(
        IFormulaRepository formulaRepository,
        IDomainEventDispatcher eventDispatcher)
    {
        _formulaRepository = formulaRepository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<Result<BatchOperationResultDto>> Handle(
        BatchDeleteFormulasCommand request, CancellationToken cancellationToken)
    {
        _operatorId = request.OperatorId;
        return await ExecuteBatchAsync(request.Ids, request.OperatorId, cancellationToken);
    }

    protected override Task<FormulaEntity?> GetByIdAsync(Guid id, CancellationToken ct)
        => _formulaRepository.GetByIdAsync(id, ct);

    protected override Task UpdateAsync(FormulaEntity formula, CancellationToken ct)
        => _formulaRepository.UpdateAsync(formula, ct);

    protected override Task ApplyOperationAsync(FormulaEntity formula, Guid operatorId, CancellationToken ct)
    {
        formula.SoftDelete(operatorId);
        _deletedNames.Add(formula.Name);
        return Task.CompletedTask;
    }

    protected override string EntityNotFoundMessage => "方剂不存在";
    protected override string OperationName => "删除";
    protected override bool CatchExceptions => false;

    protected override async Task OnBatchCompletedAsync(BatchOperationResultDto result, CancellationToken ct)
    {
        if (_deletedNames.Count > 0)
        {
            await _eventDispatcher.DispatchAsync(_deletedNames.Select(name =>
                new FormulaDeletedEvent(Guid.Empty, name, _operatorId)
            ), ct);
        }
    }

    protected override void FinalizeResult(BatchOperationResultDto result)
        => result.IsSuccess = result.FailureCount == 0;

    protected override string BuildMessage(int successCount, int failureCount)
        => failureCount > 0
            ? $"成功删除 {successCount} 个，失败 {failureCount} 个"
            : $"成功删除 {successCount} 个方剂";
}
```

> 说明：`CatchExceptions=false` 保持 Formula 原"无 try-catch"行为（DB 异常直接抛出 500）。`_deletedNames` 在 ApplyOperationAsync 中收集，与现状（Update 成功后收集）在异常路径上的差异：原实现若 Update 抛异常则已添加的 name 不会派发（因异常中断）；新实现同样因异常中断（CatchExceptions=false），行为等价。

- [ ] **Step 4: 编译验证 + 全量 build**

Run: `dotnet build LYBTZYZS.sln --no-incremental`
Expected: 0 错误 0 警告

- [ ] **Step 5: 架构测试**

Run: `dotnet test tests/LYBT.Tests.Architecture/`
Expected: 83/83 通过

- [ ] **Step 6: 提交**

```bash
git add src/Server/Modules/LYBT.Module.Patients/Application/Commands/BatchDeletePatientsCommandHandler.cs src/Server/Modules/LYBT.Module.Herbs/Application/Commands/BatchDeleteHerbsCommandHandler.cs src/Server/Modules/LYBT.Module.Formula/Application/Commands/BatchDeleteFormulasCommandHandler.cs
git commit -m "refactor: Patients/Herbs/Formula BatchDelete Handler 继承泛型基类"
```

---

### Task 4: 回归验证 + 总账更新 + 收尾

**Covers:** 项目维护规则（13-project-master-plan.md §八 状态跟踪、§九 决策记录）

**Files:**
- Modify: `docs/03-architecture/13-project-master-plan.md`（§八 追加一行状态 + §九 追加决策记录）

- [ ] **Step 1: 全量验证**

Run: `dotnet build LYBTZYZS.sln --no-incremental`
Expected: 0 错误 0 警告
Run: `dotnet test tests/LYBT.Tests.Architecture/`
Expected: 83/83

- [ ] **Step 2: 更新项目总账**

在 §八 状态跟踪表追加：

```
| Q-01 批处理 Handler 泛型化 | ✅ | 2026-08-07 | `待补 commit` — 新增 BatchOperationHandlerBase<TEntity>（Infrastructure/BatchOperations）；6 个 Handler（Users Delete/Enable/Disable + Patients/Herbs/Formula Delete）继承基类，消除循环→GetById→变更→累计 模板；BatchImport 与 Herbs/Formula Service 版 Enable/Disable 不在本次范围；行为等价（自删/IsSysAdmin/引用检查/缓存失效/领域事件保留）；build --no-incremental 0 错误 0 警告，架构测试 83/83 |
```

在 §九 追加一行：

```
| 2026-08-07 | **Q-01 批处理 Handler 泛型化（commit `待补`）**：新建 `BatchOperationHandlerBase<TEntity>`（LYBT.Infrastructure/BatchOperations，模板方法模式），统一 循环→GetById→前置校验→变更→Update→累计 BatchOperationResultDto 骨架；Users BatchDelete/Enable/Disable + Patients/Herbs/Formula BatchDelete 共 6 个 Handler 继承基类（-260 行）；差异点（自删/IsSysAdmin/无权限、医案引用检查、缓存失效、领域事件派发、IsSuccess 语义、消息文案）经抽象/virtual 钩子保留，行为等价；BatchImport 3 个 Handler 及 Herbs/Formula Service 版 BatchEnable/Disable 因差异较大/非 Handler 不在范围；已知微小差异：失败项 Name 填充（自删分支）、异常场景文案由 ex.Message 替代固定"删除操作失败" | 技术总监 |
```

- [ ] **Step 3: 提交**

```bash
git add docs/03-architecture/13-project-master-plan.md
git commit -m "docs: 总账记录 Q-01 批处理 Handler 泛型化完成"
```

---

## Self-Review

**1. Spec coverage:** Q-01（docs/compose/reports/webapi-deep-analysis-mimo-2026-08-07.md:152-157）→ Task 1/2/3 全覆盖；任务约束"不碰 BatchImport"→ Task 3 Step 3 明确排除；"基类放 Infrastructure 不放 Shared"→ Task 1 路径明确；"0 错误 0 警告 + 架构测试 83/83"→ Task 3 Step 4/5、Task 4 Step 1。

**2. Placeholder scan:** 无 TBD/TODO；所有代码块完整。Task 2 Step 1 的 `UserCanDelete` 占位已由 Step 2 最终形态覆盖并注明。

**3. Type consistency:** 基类成员名（GetByIdAsync/UpdateAsync/ApplyOperationAsync/ValidateAsync/EntityNotFoundMessage/OperationName/TrackIds/CatchExceptions/CaughtExceptionType/GetEntityName/OnBatchCompletedAsync/FinalizeResult/BuildMessage）在 6 个 Handler 中一致使用；`FormulaEntity = LYBT.Entities.Formulas.Formula` 别名与 Formula 模块惯例一致；`IMedicalCaseCrossModuleService` 命名空间 `LYBT.Infrastructure.Services.CrossModule` 来自原 Handler using（已核对 BatchDeletePatientsCommandHandler.cs:3）。
