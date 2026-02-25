# Sprint 1: 安全加固与数据完整性 -- 技术设计

> **创建时间**: 2026-02-22
> **任务总数**: 33 项 (X3:6 + X7:10 + S1:1 + S2:9 + S3:4 + 架构:3)
> **风险等级**: 中
> **前置依赖**: 无

---

## 一、总体架构影响

Sprint 1 不引入新实体或 EF Migration，所有修改在现有代码基础上补全逻辑。

```
影响模块:
  Server: Auth (X3) + Users (S1/S2/X3) + Patients (X7) + Herbs (X7) + MedicalCase (S3)
  Shared: ICrossModuleService (X3)
  Desktop: 无直接修改
  测试: +33 集成测试 (每任务至少 1 个)
```

---

## 二、S1: 密码哈希 Bug 修复 (1 项)

### 2.1 问题根因

**文件**: `UserService.cs:458`

```csharp
// BUG: 密码升级场景下，verificationResult.NewHashedPassword 是旧密码的新哈希
var newHashedPassword = verificationResult.NewHashedPassword ??
                       PasswordHelper.HashPassword(newPassword, entity.Role, _logger);
```

当 `PasswordHelper.VerifyPassword` 触发哈希算法升级 (如 MD5->BCrypt) 时，`NewHashedPassword` 返回**旧密码**用新算法重新计算的哈希值。三元表达式优先使用它，完全忽略用户输入的 `newPassword`。

### 2.2 修复方案

```csharp
// 修复: 始终对新密码重新哈希
entity.PasswordHash = PasswordHelper.HashPassword(newPassword, entity.Role, _logger);
```

如果旧密码需要升级存储 (即正常登录时触发)，应在登录流程中处理，不在 ChangePassword 中。

### 2.3 测试策略

| 测试 | 验证点 |
|------|--------|
| ChangePassword_WithHashUpgrade_ShouldUseNewPassword | 哈希升级场景下新密码生效 |
| ChangePassword_Normal_ShouldUseNewPassword | 正常场景下新密码生效 |
| ChangePassword_WrongOldPassword_ShouldFail | 旧密码错误返回失败 |

---

## 三、A1: 架构修复 (3 项)

### 3.1 A1-01: 唯一索引条件修复

**文件**: `MedicalCaseConfiguration.cs:32-35`

**现状**: `UX_MedicalCases_Patient_ActiveOnly` 索引过滤条件包含 Draft+Active 状态。
**修复**: 仅过滤 Active 状态 (Draft 不应参与唯一性约束，患者可以有多个草稿)。

```csharp
// 修复前
.HasFilter("[CaseStatus] IN (0, 1) AND [IsDeleted] = 0")
// 修复后
.HasFilter("[CaseStatus] = 1 AND [IsDeleted] = 0")
```

> 注: 此修改不需要 EF Migration，索引过滤条件变更通过手动 SQL 脚本处理。

### 3.2 A1-02: SensitiveData 标记

**文件**: `UserModel.cs`

```csharp
[SensitiveData(DataType = SensitiveDataType.ContactInfo)]
public string? PhoneNumber { get; set; }

[SensitiveData(DataType = SensitiveDataType.ContactInfo)]
public string? Email { get; set; }
```

### 3.3 A1-03: 硬编码连接串移除

**文件**: `DatabaseServiceCollectionExtensions.cs`

移除 fallback 硬编码连接字符串，仅从 `IConfiguration` 读取。缺失配置时抛出 `InvalidOperationException` 并提示配置路径。

---

## 四、X3: Token Family 撤销 (6 项)

### 4.1 现状分析

Token 撤销基础设施**已完整**:
- `RefreshToken.FamilyId` 字段存在 (RefreshToken.cs:115)
- `RevokeTokenFamilyAsync` 方法已实现 (AuthService.cs:728-750)
- 登出时已调用 Family 撤销 (AuthService.cs:218-273)

**缺口**: 用户管理操作 (角色变更/删除/禁用/密码修改/重置) 后未调用 Token 撤销。

### 4.2 技术方案

**跨模块调用**: Users 模块不能直接依赖 Auth 模块。通过 `ICrossModuleService` 暴露撤销能力:

```csharp
// 文件: ICrossModuleService.cs -- 新增方法
Task RevokeUserTokensAsync(Guid userId, string reason);
```

**实现**: 在 Auth 模块的 `CrossModuleService` 实现中，查询该用户所有未撤销的 RefreshToken，按 FamilyId 分组逐组撤销。

### 4.3 六个调用点

| 任务 ID | 触发场景 | 修改文件 | 调用时机 | 撤销原因 |
|---------|---------|----------|----------|----------|
| T1-X3-01 | 登录 | AuthService.LoginAsync | 生成新 Token 前 | "新登录，撤销旧会话" |
| T1-X3-02 | 角色变更 | UserService.UpdateAsync | role 字段变更后 | "角色变更，权限已变化" |
| T1-X3-03 | 删除用户 | UserService.DeleteAsync | 软删除后 | "用户已删除" |
| T1-X3-04 | 重置密码 | UserService.ResetPasswordAsync | 密码更新后 | "密码已重置" |
| T1-X3-05 | 修改密码 | UserService.ChangePasswordAsync | 密码更新后 | "密码已修改" |
| T1-X3-06 | 禁用用户 | UserService.ToggleStatusAsync | 状态变为 Disabled 后 | "用户已禁用" |

### 4.4 注意事项

- T1-X3-01 (登录撤销): 仅撤销**同一用户**的旧 Token，不影响其他用户
- T1-X3-06 (禁用撤销): 仅在 `Enabled -> Disabled` 方向触发，`Disabled -> Enabled` 不触发
- 撤销操作失败时记录 Warning 日志，**不阻塞**主操作 (try-catch 包裹)
- 同时撤销 `AutoLoginToken` (记住密码 Token)

### 4.5 测试策略

每个场景 1 个集成测试:
1. 执行触发操作 (如角色变更)
2. 尝试用旧 RefreshToken 调用 Refresh 端点
3. 断言返回 401 或 Token 已撤销错误

---

## 五、X7: 引用检查修复 (10 项)

### 5.1 现状分析

**患者模块**:
- `PatientService.CheckReferenceAsync` 已实现引用计数查询 (PatientService.cs:725-740)
- `CanDelete = true` 硬编码 (PatientService.cs:749)
- PatientsController **缺少** check-reference / batch-check-reference 端点
- 删除操作**未调用**引用检查

**药材模块**:
- `HerbService.CheckReferenceAsync` 已实现处方引用查询 (HerbService.cs:517-537)
- `CanDelete = true` 硬编码 (HerbService.cs:546)
- HerbsController **已有** check-reference 端点 (HerbsController.cs:281-331)
- 删除操作**未调用**引用检查

### 5.2 CanDelete 规则

在软删除系统中，`CanDelete` 的语义:
- `CanDelete = true`: 无活跃引用，可安全软删除
- `CanDelete = false`: 有活跃引用，需用户确认强制删除 (仅 Admin)

```csharp
// 修复: 动态判断
CanDelete = !hasReferences,  // 替换硬编码 true
```

### 5.3 患者模块修复 (6 项)

| 任务 | 修改文件 | 方案 |
|------|----------|------|
| T1-X7-01 | PatientService.DeleteAsync | 调用 CheckReferenceAsync，有引用且非 Admin 时返回 422 |
| T1-X7-02 | PatientService.BatchDeleteAsync | 逐个检查，收集失败项返回 BatchOperationResultDto |
| T1-X7-03 | PatientsController | 新增 `GET /{id}/check-reference` 端点 |
| T1-X7-04 | PatientService.CheckReferenceAsync | `CanDelete = !hasReferences` 替换硬编码 |
| T1-X7-05 | PatientsController | 新增 `POST /batch-check-reference` 端点 (最多 100 条) |
| T1-X7-06 | PatientService.BatchCheckReferenceAsync | 确认实际查询逻辑正确 (已实现，仅需修复 CanDelete) |

**新增端点签名**:

```csharp
// T1-X7-03
[HttpGet("{id}/check-reference")]
public async Task<ActionResult<PatientReferenceCheckDto>> CheckReference(Guid id)

// T1-X7-05
[HttpPost("batch-check-reference")]
public async Task<ActionResult<List<PatientReferenceCheckDto>>> BatchCheckReference(
    [FromBody] BatchCheckReferenceRequest request)  // request.Ids, 最多 100 条
```

### 5.4 药材模块修复 (4 项)

| 任务 | 修改文件 | 方案 |
|------|----------|------|
| T1-X7-07 | HerbService.DeleteAsync | 调用 CheckReferenceAsync，有引用时返回 422 |
| T1-X7-08 | HerbService.BatchDeleteAsync | 逐个检查，收集失败项 |
| T1-X7-09 | HerbService.CheckReferenceAsync | `CanDelete = !hasReferences` 替换硬编码 |
| T1-X7-10 | HerbService.DeleteAsync | 有引用时返回 422 + 引用详情 |

### 5.5 删除保护流程

```
DeleteAsync(id, isAdmin)
  │
  ├─ CheckReferenceAsync(id)
  │   └─ hasReferences?
  │       ├─ false → 执行软删除
  │       └─ true
  │           ├─ isAdmin && forceDelete → 执行软删除 + 警告日志
  │           └─ else → 返回 422 + 引用详情
```

### 5.6 测试策略

| 测试 | 验证点 |
|------|--------|
| Delete_WithReferences_ShouldReturn422 | 有引用时拒绝删除 |
| Delete_NoReferences_ShouldSucceed | 无引用时正常删除 |
| Delete_AdminForce_WithReferences_ShouldSucceed | Admin 强制删除 |
| BatchDelete_PartialReferences_ShouldReturnMixed | 部分有引用的批量结果 |
| CheckReference_ShouldReturnAccurateCount | 引用计数准确 |

---

## 六、S2: 权限矩阵修复 (9 项)

### 6.1 CanManageUser 修复

**文件**: `UserService.cs:78-90`

```csharp
// 修复: Admin 可管理 Doctor 和 Receptionist
return currentUserRole.Value switch
{
    UserRole.SuperAdmin => true,
    UserRole.Admin => targetUserRole.Value is UserRole.Doctor
                      or UserRole.Receptionist,  // 新增 Receptionist
    UserRole.Doctor => false,
    UserRole.Receptionist => false,              // 显式处理
    _ => false
};
```

**修复后权限矩阵**:

| 操作者 | -> Receptionist | -> Doctor | -> Admin | -> SuperAdmin |
|--------|----------------|-----------|----------|---------------|
| SuperAdmin | O | O | O | O |
| Admin | **O** | O | X | X |
| Doctor | X | X | X | X |
| Receptionist | X | X | X | X |

### 6.2 九项任务分解

| 任务 | 修改文件 | 方案 |
|------|----------|------|
| T1-S2-01 | UserService.CanManageUser | 添加 Receptionist (见 6.1) |
| T1-S2-02 | UserService.UpdateAsync | 角色变更时复用 CanManageUser |
| T1-S2-03 | UserService.DeleteAsync | 新增 `if (id == currentUserId) return Failure("不能删除自己")` |
| T1-S2-04 | UserService.ChangePasswordAsync | 调用 `PasswordPolicyValidator.Validate(newPassword)` |
| T1-S2-05 | UsersController | `ChangePassword` 方法级 `[Authorize]` 替代类级 AdminOnly |
| T1-S2-06 | UsersController | `UpdateProfile` 方法级 `[Authorize]` |
| T1-S2-07 | UserService.ToggleStatusAsync | 新增最后管理员保护: 查询 Admin 用户数，仅剩 1 个时拒绝禁用 |
| T1-S2-08 | UserService + Controller | 新增 `BatchUpdateStatusAsync` 方法，逐个校验权限 |
| T1-S2-09 | UsersController | `GetCurrentUser` 方法级 `[Authorize]` |

### 6.3 授权调整方案

**现状**: `UsersController` 类级 `[Authorize(Policy = "AdminOnly")]`，导致所有端点都需要 Admin 权限。

**修复**: 类级改为 `[Authorize]` (任意已认证用户)，敏感端点方法级添加 `[Authorize(Policy = "AdminOnly")]`:

```csharp
[ApiController]
[Route("api/v1/users")]
[Authorize]  // 类级: 任意已认证用户
public class UsersController : BaseApiController
{
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]  // 方法级: 仅管理员
    public async Task<ActionResult<PagedResult<UserListDto>>> GetList(...)

    [HttpGet("current")]
    // 无额外策略 -- 任意用户可获取自己的信息
    public async Task<ActionResult<UserDetailDto>> GetCurrentUser()

    [HttpPost("{id}/change-password")]
    // 无额外策略 -- Service 层检查 id == currentUserId
    public async Task<IActionResult> ChangePassword(Guid id, ...)

    [HttpPut("profile")]
    // 无额外策略 -- Service 层检查 id == currentUserId
    public async Task<IActionResult> UpdateProfile(...)
}
```

### 6.4 最后管理员保护

```csharp
// T1-S2-07: ToggleStatusAsync 中新增
if (entity.Role >= UserRole.Admin && entity.Status == CommonStatus.Enabled)
{
    var adminCount = await _repository.CountAsync(
        u => u.Role >= UserRole.Admin && u.Status == CommonStatus.Enabled && !u.IsDeleted);
    if (adminCount <= 1)
        return Result<UserDetailDto>.Failure("不能禁用最后一个管理员");
}
```

### 6.5 测试策略

| 测试 | 验证点 |
|------|--------|
| Admin_CanManage_Receptionist | Admin 可管理 Receptionist |
| Admin_CannotManage_Admin | Admin 不能管理 Admin |
| DeleteSelf_ShouldFail | 不能删除自己 |
| DisableLastAdmin_ShouldFail | 不能禁用最后一个管理员 |
| NonAdmin_CanGetCurrentUser | 非管理员可获取自己信息 |
| NonAdmin_CanChangeOwnPassword | 非管理员可修改自己密码 |

---

## 七、S3: EditReason 强制校验 (4 项)

### 7.1 现状分析

- `RequiresEditReason` 在 PermissionService 中已实现 (MedicalCasePermissionService.cs:155-162)
- 当前仅检查 `IsLocked` (Completed + 跨日)
- CommandService 的 Update 方法签名中**没有** EditReason 参数
- 审计日志中未记录 EditReason

### 7.2 技术方案

**步骤 1**: 扩展 `RequiresEditReason` 规则

```csharp
// T1-S3-04: 补充两个新场景
public bool RequiresEditReason(MedicalCase mc, Guid currentUserId)
{
    if (mc == null) return false;
    if (mc.IsLocked) return true;              // 原有: 跨日锁定
    if (mc.UserId != currentUserId) return true; // 新增: 非本人医案
    if (mc.IsCompleted) return true;            // 新增: 当天已完成
    return false;
}
```

**步骤 2**: CommandService 方法签名新增 EditReason

```csharp
// T1-S3-01 + T1-S3-02
public async Task<MedicalCase?> UpdateConsultationAsync(
    Guid medicalCaseId,
    ConsultationInputDto request,
    Guid currentUserId,
    bool isAdmin = false,
    string? editReason = null)  // 新增参数
{
    var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);

    // EditReason 校验
    if (_permissionService.RequiresEditReason(medicalCase, currentUserId)
        && string.IsNullOrWhiteSpace(editReason))
    {
        throw new BusinessException(EC.ValidationFailed, "修改已完成/锁定的医案需要提供修改原因");
    }

    // ... 现有逻辑 ...
}
```

**步骤 3**: 审计日志中传递 EditReason

```csharp
// T1-S3-03: AuditService.LogChangeAsync 新增 editReason 参数
await _auditService.LogChangeAsync(
    medicalCase.Id,
    "UpdateConsultation",
    changes,
    currentUserId,
    editReason: editReason);  // 新增: 记录修改原因
```

### 7.3 Controller 层适配

```csharp
// MedicalCaseController -- Update 端点
[HttpPut("{id}")]
public async Task<IActionResult> Update(
    Guid id,
    [FromBody] MedicalCaseUpdateRequest request)  // request 中新增 EditReason 字段
{
    var result = await _commandService.UpdateConsultationAsync(
        id, request.Consultation, currentUserId, isAdmin, request.EditReason);
}
```

### 7.4 测试策略

| 测试 | 验证点 |
|------|--------|
| Update_LockedCase_WithoutReason_ShouldFail | 锁定医案无原因时 400 |
| Update_LockedCase_WithReason_ShouldSucceed | 锁定医案有原因时成功 |
| Update_OtherDoctorCase_WithoutReason_ShouldFail | 非本人医案无原因时 400 |
| Update_OwnActiveCase_WithoutReason_ShouldSucceed | 自己的活跃医案无需原因 |
| AuditLog_ShouldContainEditReason | 审计日志包含修改原因 |

---

## 八、执行顺序与依赖

```
阶段 1 (无依赖，可并行):
  ├── T1-S1-01  密码哈希 Bug
  ├── A1-01/02/03  架构修复 3 项
  └── T1-S3-01~04  EditReason 4 项

阶段 2 (无依赖，可并行):
  ├── T1-X7-01~10  引用检查 10 项
  └── T1-X3-01~06  Token 撤销 6 项 (需新增 ICrossModuleService 方法)

阶段 3 (依赖 X3):
  └── T1-S2-01~09  权限矩阵 9 项 (S2-05/06/09 授权调整依赖 X3 的 Token 撤销保障)
```

### 并行机会

- 阶段 1 的 3 组任务完全独立，可 3 个子代理并行
- 阶段 2 的 X7 和 X3 完全独立，可 2 个子代理并行
- X7 内部: 患者 (6 项) 和药材 (4 项) 可并行

---

## 九、修改文件清单

| 文件 | 修改类型 | 涉及任务 |
|------|----------|----------|
| `UserService.cs` | 修改 ChangePasswordAsync / CanManageUser / DeleteAsync / ToggleStatusAsync / 新增 BatchUpdateStatusAsync | S1 + S2 + X3 |
| `UsersController.cs` | 授权属性调整 / 新增 BatchUpdateStatus 端点 | S2 |
| `PatientService.cs` | 修改 CheckReferenceAsync / DeleteAsync / BatchDeleteAsync | X7 |
| `PatientsController.cs` | 新增 check-reference / batch-check-reference 端点 | X7 |
| `HerbService.cs` | 修改 CheckReferenceAsync / DeleteAsync / BatchDeleteAsync | X7 |
| `ICrossModuleService.cs` | 新增 RevokeUserTokensAsync 方法 | X3 |
| `CrossModuleService.cs` | 实现 RevokeUserTokensAsync | X3 |
| `AuthService.cs` | LoginAsync 新增旧 Token 撤销 | X3 |
| `MedicalCaseCommandService.cs` | UpdateConsultation/Prescription 新增 editReason 参数 | S3 |
| `MedicalCasePermissionService.cs` | RequiresEditReason 扩展 | S3 |
| `MedicalCaseController.cs` | Update 请求体新增 EditReason | S3 |
| `MedicalCaseConfiguration.cs` | 索引过滤条件修复 | A1 |
| `UserModel.cs` | SensitiveData 标记 | A1 |
| `DatabaseServiceCollectionExtensions.cs` | 移除硬编码 | A1 |

---

## 十、验收标准

1. 全部 33 项任务通过对应测试
2. 编译 0 错误
3. 现有 ~1,948 测试全部通过
4. 新增 ~33 个测试 (每项任务至少 1 个)

---

> **最后更新**: 2026-02-22
> **文档版本**: v1.0
