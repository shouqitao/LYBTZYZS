# Controller 设计统一化实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将三个独立基类（BaseMedicalCasesController、BaseRegistrationsController、BaseUsersController）重构为继承 BaseCrudController，消除重复 CRUD 代码，统一错误处理、验证、日志和授权模式。

**Architecture:** 保持 BaseCrudController 不变，将三个独立基类改为继承 BaseCrudController，移除重复的 CRUD 方法，实现基类抽象方法，保留业务特化方法。每次重构后验证编译通过。

**Tech Stack:** ASP.NET Core, C#, MediatR, CommunityToolkit.Mvvm

## Global Constraints

- 编译必须通过：每次重构后运行 `dotnet build LYBTZYZS.sln`
- 保持 API 签名不变：方法签名、路由、授权策略保持不变
- 错误处理统一："不存在" → NotFound，其他 → BusinessFail
- 验证统一：所有 Guid id 参数必须 ValidateGuid，分页参数必须 ValidatePagination
- 日志统一：写操作（Create/Update/Delete/ToggleStatus/Restore/BatchDelete）必须 LogOperation
- 授权统一：类级别 [Authorize] + 方法级别 [Authorize(Policy = ...)]

---

## Task 1: 重构 BaseMedicalCasesController

**Covers:** [S3.1, S3.2, S3.3, S3.4, S3.5, S3.6]

**Files:**
- Modify: `src/Server/Modules/LYBT.Module.MedicalCase/Controllers/BaseMedicalCasesController.cs`

**Interfaces:**
- Consumes: `BaseCrudController<MedicalCaseListDto, MedicalCaseDetailDto, MedicalCaseInputDto, GetMedicalCasesQuery>`
- Produces: 继承 BaseCrudController 的 BaseMedicalCasesController，保留所有特化方法

- [ ] **Step 1: 备份当前文件**

```bash
cp src/Server/Modules/LYBT.Module.MedicalCase/Controllers/BaseMedicalCasesController.cs src/Server/Modules/LYBT.Module.MedicalCase/Controllers/BaseMedicalCasesController.cs.bak
```

- [ ] **Step 2: 修改类声明，继承 BaseCrudController**

将：
```csharp
public abstract class BaseMedicalCasesController : BaseApiController
```

改为：
```csharp
public abstract class BaseMedicalCasesController : BaseCrudController<MedicalCaseListDto, MedicalCaseDetailDto, MedicalCaseInputDto, GetMedicalCasesQuery>
```

- [ ] **Step 3: 移除重复的 CRUD 方法**

移除以下方法（BaseCrudController 已提供）：
- `GetList` — 由 BaseCrudController.GetList 替代
- `GetById` — 由 BaseCrudController.GetById 替代
- `Create` — 由 BaseCrudController.Create 替代
- `Save` (Update) — 由 BaseCrudController.Update 替代
- `Delete` — 由 BaseCrudController.Delete 替代
- `BatchDelete` — 由 BaseCrudController.BatchDelete 替代

保留以下特化方法：
- `Search` — 跨医案搜索
- `Query` — 统一医案查询端点
- `GetPatientConsultations` — 查询患者辨证记录历史
- `GetPatientPrescriptions` — 查询患者处方历史
- `GetConsultations` — 查询辨证记录列表
- `GetPrescriptions` — 查询处方列表
- `GetBatchDetails` — 批量查询医案详情
- `GetPermissions` — 获取医案操作权限
- `GetAuditLogs` — 获取医案审计日志
- `SetPrescriptionFlag` — 标记是否需要开处方
- `RecordPrint` — 记录打印完成

- [ ] **Step 4: 实现基类抽象方法**

```csharp
#region 基类抽象方法实现

protected override GetMedicalCasesQuery CreateGetListQuery(int page, int pageSize, string? keyword)
{
    var (operatorId, _, operatorRole) = GetOperator();
    var isAdmin = operatorRole is UserRole.SuperAdmin or UserRole.Admin;
    return new GetMedicalCasesQuery(null, null, page, pageSize, operatorId, isAdmin, keyword);
}

protected override IRequest<Result<MedicalCaseDetailDto>> CreateCreateCommand(MedicalCaseInputDto dto, Guid operatorId)
{
    dto.Id = null;
    return new CreateMedicalCaseCommand(dto, operatorId);
}

protected override IRequest<Result<MedicalCaseDetailDto>> CreateUpdateCommand(Guid id, MedicalCaseInputDto dto, Guid operatorId)
{
    var (operatorId2, _, operatorRole) = GetOperator();
    var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;
    return new SaveMedicalCaseCommand(dto, operatorId2, isAdmin);
}

protected override IRequest<Result> CreateDeleteCommand(Guid id, Guid operatorId)
{
    var (_, _, operatorRole) = GetOperator();
    var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;
    return new DeleteMedicalCaseCommand(id, operatorId, isAdmin);
}

protected override IRequest<Result<MedicalCaseDetailDto>> CreateToggleStatusCommand(Guid id, Guid operatorId)
{
    throw new NotSupportedException("医案不支持切换状态操作");
}

protected override IRequest<Result<MedicalCaseDetailDto>> CreateRestoreCommand(Guid id, Guid operatorId)
{
    throw new NotSupportedException("医案不支持恢复操作");
}

protected override IRequest<Result<BatchOperationResultDto>> CreateBatchDeleteCommand(List<Guid> ids, Guid operatorId)
{
    var (_, _, operatorRole) = GetOperator();
    var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;
    return new BatchDeleteMedicalCasesCommand(ids, operatorId, isAdmin);
}

#endregion
```

- [ ] **Step 5: 验证编译通过**

```bash
dotnet build LYBTZYZS.sln
```

预期：编译成功，无错误

- [ ] **Step 6: 提交更改**

```bash
git add src/Server/Modules/LYBT.Module.MedicalCase/Controllers/BaseMedicalCasesController.cs
git commit -m "refactor(medical-case): BaseMedicalCasesController 继承 BaseCrudController，移除重复 CRUD 代码"
```

---

## Task 2: 重构 BaseRegistrationsController

**Covers:** [S3.1, S3.2, S3.3, S3.4, S3.5, S3.6]

**Files:**
- Modify: `src/Server/Modules/LYBT.Module.Registration/Controllers/BaseRegistrationsController.cs`

**Interfaces:**
- Consumes: `BaseCrudController<RegistrationListDto, RegistrationDetailDto, RegistrationInputDto, GetRegistrationsQuery>`
- Produces: 继承 BaseCrudController 的 BaseRegistrationsController，保留所有特化方法

- [ ] **Step 1: 备份当前文件**

```bash
cp src/Server/Modules/LYBT.Module.Registration/Controllers/BaseRegistrationsController.cs src/Server/Modules/LYBT.Module.Registration/Controllers/BaseRegistrationsController.cs.bak
```

- [ ] **Step 2: 修改类声明，继承 BaseCrudController**

将：
```csharp
public abstract class BaseRegistrationsController : BaseApiController
```

改为：
```csharp
public abstract class BaseRegistrationsController : BaseCrudController<RegistrationListDto, RegistrationDetailDto, RegistrationInputDto, GetRegistrationsQuery>
```

- [ ] **Step 3: 移除重复的 CRUD 方法**

移除以下方法（BaseCrudController 已提供）：
- `GetList` — 由 BaseCrudController.GetList 替代
- `GetById` — 由 BaseCrudController.GetById 替代
- `Create` — 由 BaseCrudController.Create 替代

保留以下特化方法：
- `GetQueue` — 获取等待队列
- `StartVisit` — 接诊
- `Cancel` — 取消挂号
- `QuickVisit` — 医生快速看诊

- [ ] **Step 4: 实现基类抽象方法**

```csharp
#region 基类抽象方法实现

protected override GetRegistrationsQuery CreateGetListQuery(int page, int pageSize, string? keyword)
    => new GetRegistrationsQuery(page, pageSize, keyword, null, null, null, null);

protected override IRequest<Result<RegistrationDetailDto>> CreateCreateCommand(RegistrationInputDto dto, Guid operatorId)
    => new CreateRegistrationCommand(dto);

protected override IRequest<Result<RegistrationDetailDto>> CreateUpdateCommand(Guid id, RegistrationInputDto dto, Guid operatorId)
{
    throw new NotSupportedException("挂号不支持更新操作");
}

protected override IRequest<Result> CreateDeleteCommand(Guid id, Guid operatorId)
{
    throw new NotSupportedException("挂号不支持删除操作");
}

protected override IRequest<Result<RegistrationDetailDto>> CreateToggleStatusCommand(Guid id, Guid operatorId)
{
    throw new NotSupportedException("挂号不支持切换状态操作");
}

protected override IRequest<Result<RegistrationDetailDto>> CreateRestoreCommand(Guid id, Guid operatorId)
{
    throw new NotSupportedException("挂号不支持恢复操作");
}

protected override IRequest<Result<BatchOperationResultDto>> CreateBatchDeleteCommand(List<Guid> ids, Guid operatorId)
{
    throw new NotSupportedException("挂号不支持批量删除操作");
}

#endregion
```

- [ ] **Step 5: 验证编译通过**

```bash
dotnet build LYBTZYZS.sln
```

预期：编译成功，无错误

- [ ] **Step 6: 提交更改**

```bash
git add src/Server/Modules/LYBT.Module.Registration/Controllers/BaseRegistrationsController.cs
git commit -m "refactor(registration): BaseRegistrationsController 继承 BaseCrudController，移除重复 CRUD 代码"
```

---

## Task 3: 重构 BaseUsersController

**Covers:** [S3.1, S3.2, S3.3, S3.4, S3.5, S3.6]

**Files:**
- Modify: `src/Server/Modules/LYBT.Module.Users/Controllers/BaseUsersController.cs`

**Interfaces:**
- Consumes: `BaseCrudController<UserListDto, UserDetailDto, UserInputDto, GetUsersQuery>`
- Produces: 继承 BaseCrudController 的 BaseUsersController，保留所有特化方法

- [ ] **Step 1: 备份当前文件**

```bash
cp src/Server/Modules/LYBT.Module.Users/Controllers/BaseUsersController.cs src/Server/Modules/LYBT.Module.Users/Controllers/BaseUsersController.cs.bak
```

- [ ] **Step 2: 修改类声明，继承 BaseCrudController**

将：
```csharp
public abstract class BaseUsersController : BaseApiController
```

改为：
```csharp
public abstract class BaseUsersController : BaseCrudController<UserListDto, UserDetailDto, UserInputDto, GetUsersQuery>
```

- [ ] **Step 3: 移除重复的 CRUD 方法**

移除以下方法（BaseCrudController 已提供）：
- `GetList` — 由 BaseCrudController.GetList 替代
- `GetById` — 由 BaseCrudController.GetById 替代
- `Create` — 由 BaseCrudController.Create 替代
- `Update` — 由 BaseCrudController.Update 替代
- `Delete` — 由 BaseCrudController.Delete 替代
- `ToggleStatus` — 由 BaseCrudController.ToggleStatus 替代
- `Restore` — 由 BaseCrudController.Restore 替代
- `BatchDelete` — 由 BaseCrudController.BatchDelete 替代

保留以下特化方法：
- `GetCurrentUser` — 获取当前用户信息
- `ResetPassword` — 重置用户密码
- `ChangeProfile` — 修改个人资料
- `ChangePassword` — 修改密码
- `BatchEnable` — 批量启用用户
- `BatchDisable` — 批量禁用用户

- [ ] **Step 4: 实现基类抽象方法**

```csharp
#region 基类抽象方法实现

protected override GetUsersQuery CreateGetListQuery(int page, int pageSize, string? keyword)
    => new GetUsersQuery(page, pageSize, keyword, null, null);

protected override IRequest<Result<UserDetailDto>> CreateCreateCommand(UserInputDto dto, Guid operatorId)
{
    var (_, _, currentRole) = GetOperator();
    var isAdmin = currentRole == UserRole.SuperAdmin || currentRole == UserRole.Admin;
    return new CreateUserCommand(dto, operatorId, isAdmin);
}

protected override IRequest<Result<UserDetailDto>> CreateUpdateCommand(Guid id, UserInputDto dto, Guid operatorId)
{
    var (_, _, currentRole) = GetOperator();
    var isAdmin = currentRole == UserRole.SuperAdmin || currentRole == UserRole.Admin;
    return new UpdateUserCommand(id, dto, operatorId, isAdmin);
}

protected override IRequest<Result> CreateDeleteCommand(Guid id, Guid operatorId)
{
    var (_, _, currentRole) = GetOperator();
    var isAdmin = currentRole == UserRole.SuperAdmin || currentRole == UserRole.Admin;
    return new DeleteUserCommand(id, operatorId, isAdmin);
}

protected override IRequest<Result<UserDetailDto>> CreateToggleStatusCommand(Guid id, Guid operatorId)
{
    var (_, _, currentRole) = GetOperator();
    var isAdmin = currentRole == UserRole.SuperAdmin || currentRole == UserRole.Admin;
    return new ToggleUserStatusCommand(id, operatorId, isAdmin);
}

protected override IRequest<Result<UserDetailDto>> CreateRestoreCommand(Guid id, Guid operatorId)
    => new RestoreUserCommand(id, operatorId);

protected override IRequest<Result<BatchOperationResultDto>> CreateBatchDeleteCommand(List<Guid> ids, Guid operatorId)
{
    var (_, _, currentRole) = GetOperator();
    var isAdmin = currentRole == UserRole.SuperAdmin || currentRole == UserRole.Admin;
    return new BatchDeleteUsersCommand(ids, operatorId, isAdmin);
}

#endregion
```

- [ ] **Step 5: 验证编译通过**

```bash
dotnet build LYBTZYZS.sln
```

预期：编译成功，无错误

- [ ] **Step 6: 提交更改**

```bash
git add src/Server/Modules/LYBT.Module.Users/Controllers/BaseUsersController.cs
git commit -m "refactor(users): BaseUsersController 继承 BaseCrudController，移除重复 CRUD 代码"
```

---

## Task 4: 清理备份文件

**Covers:** 无（清理任务）

**Files:**
- Delete: `src/Server/Modules/LYBT.Module.MedicalCase/Controllers/BaseMedicalCasesController.cs.bak`
- Delete: `src/Server/Modules/LYBT.Module.Registration/Controllers/BaseRegistrationsController.cs.bak`
- Delete: `src/Server/Modules/LYBT.Module.Users/Controllers/BaseUsersController.cs.bak`

- [ ] **Step 1: 删除备份文件**

```bash
rm src/Server/Modules/LYBT.Module.MedicalCase/Controllers/BaseMedicalCasesController.cs.bak
rm src/Server/Modules/LYBT.Module.Registration/Controllers/BaseRegistrationsController.cs.bak
rm src/Server/Modules/LYBT.Module.Users/Controllers/BaseUsersController.cs.bak
```

- [ ] **Step 2: 最终验证编译通过**

```bash
dotnet build LYBTZYZS.sln
```

预期：编译成功，无错误

- [ ] **Step 3: 提交清理更改**

```bash
git add -A
git commit -m "chore: 清理重构备份文件"
```

---

## 验证检查表

完成所有 Task 后，验证以下内容：

- [ ] 所有业务 Controller 统一继承 `BaseCrudController`
- [ ] 重复的 CRUD 代码被消除
- [ ] 错误处理统一（"不存在" → NotFound，其他 → BusinessFail）
- [ ] 验证统一（ValidateGuid, ValidatePagination）
- [ ] 日志统一（写操作必须 LogOperation）
- [ ] 授权统一（类级别 Authorize + 方法级别 Policy）
- [ ] 编译通过，无回归错误
- [ ] 现有功能保持不变
