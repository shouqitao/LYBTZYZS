# 全 Sprint 技术设计方案 (v2)

> **创建时间**: 2026-02-22
> **数据来源**: 代码深度调研 (4 个并行代理) + Sprint 路线图 (329 项任务，含 D2/D5 深化新增 24 项)
> **编译基准**: 0 错误 / 35 警告
> **文档结构**: 按 Sprint 分章节，每个任务精确到文件/行号/修改方案

---

## 目录

- [Sprint 1: 安全加固与数据完整性 (33 项)](#一sprint-1-安全加固与数据完整性-33-项)
- [Sprint 2: 核心功能修复 (51 项)](#二sprint-2-核心功能修复-51-项)
- [Sprint 3: 体系统一与文档同步 (85 项)](#三sprint-3-体系统一与文档同步-85-项)
- [Sprint 4: 本地模式补齐 (62 项)](#四sprint-4-本地模式补齐-62-项)
- [Sprint 5+: 细节完善 (98 项)](#五sprint-5-细节完善-98-项)
- [跨 Sprint 技术决策](#六跨-sprint-技术决策)

---

## 一、Sprint 1: 安全加固与数据完整性 (33 项)

**风险等级**: 中 | **前置依赖**: 无 | **就绪度**: 可直接执行

### 1.1 S1: 密码哈希 Bug 修复 (1 项)

**问题根因**: `UserService.ChangePasswordAsync` (第 458 行)

```csharp
// BUG: 哈希升级场景下 verificationResult.NewHashedPassword 是旧密码的新算法哈希
var newHashedPassword = verificationResult.NewHashedPassword ??
                       PasswordHelper.HashPassword(newPassword, entity.Role, _logger);
entity.PasswordHash = newHashedPassword;
```

当密码从旧算法 (如 MD5) 升级到 BCrypt 时，`NewHashedPassword` 返回的是**旧密码**的 BCrypt 哈希，三元表达式优先使用它，`newPassword` 被完全忽略。

**修复方案**:

```csharp
// 修复: 始终使用用户输入的新密码进行哈希
entity.PasswordHash = PasswordHelper.HashPassword(newPassword, entity.Role, _logger);
```

**修改文件**: `src/Server/Modules/LYBT.Module.Users/Services/UserService.cs` (第 458 行)
**测试**: 新增单元测试验证哈希升级场景下新密码正确保存

---

### 1.2 A1: 架构新增 (3 项)

| 任务 ID | 修改文件 | 方案 | 复杂度 |
|---------|----------|------|--------|
| A1-01 | `Infrastructure/Data/Configurations/MedicalCaseConfiguration.cs` (L31-36) | 唯一索引条件已修复为仅 `Active`，确认即可 | 低 |
| A1-02 | `Server/Core/LYBT.Entities/Users/UserModel.cs` | PhoneNumber 和 Email 添加 `[SensitiveData(DataType = SensitiveDataType.ContactInfo)]` | 低 |
| A1-03 | `Server/Services/LYBT.WebAPI/Extensions/DatabaseServiceCollectionExtensions.cs` | 移除 fallback 硬编码连接串，缺失时抛出 `InvalidOperationException` | 低 |

---

### 1.3 X3: Token Family 撤销 (6 项) [重点设计]

#### 现状分析

Token 撤销基础设施**已完整**:
- `RefreshToken.FamilyId` 已存在 (RefreshToken.cs:115)
- `RevokeTokenFamilyAsync` 已实现 (AuthService.cs:728-750)
- 重放攻击检测已实现 (AuthService.cs:301-325, IsUsed 标记 + Family 撤销)
- 登出时已调用 Family 撤销 (AuthService.cs:218-273)
- Token 轮换: MarkAsUsed + 继承 FamilyId (AuthService.cs:389-410)

**缺口**: 用户管理场景 (角色变更/删除/禁用/密码操作) 后未调用 Token 撤销。

#### 技术方案

**跨模块调用**: 通过 `ICrossModuleService` 暴露 Token 撤销能力，避免 Users 模块直接依赖 Auth 模块。

```csharp
// ICrossModuleService 新增方法
public interface ICrossModuleService
{
    // ... 现有方法 ...
    Task RevokeUserTokensAsync(Guid userId, string reason);
}
```

**实现**: 在 `CrossModuleService` 中注入 `IRefreshTokenRepository`，按 UserId 查询所有未撤销 Token 并逐个 Revoke。同时撤销 `AutoLoginToken` (AuthService.cs:657-692 定义，30 天有效期)。

#### 6 个调用点

| 任务 ID | 触发场景 | 修改文件 | 调用位置 | 撤销原因 |
|---------|---------|----------|----------|----------|
| T1-X3-01 | 登录时撤销旧会话 | AuthService.cs | LoginAsync (第 158 行后) | "新登录，撤销旧会话" |
| T1-X3-02 | 角色变更 | UserService.cs | UpdateAsync (检测 role changed) | "角色变更，权限已改变" |
| T1-X3-03 | 删除用户 | UserService.cs | DeleteAsync (软删除后) | "用户已删除" |
| T1-X3-04 | 重置密码 | UserService.cs | ResetPasswordAsync (第 381 行后) | "密码已重置" |
| T1-X3-05 | 修改密码 | UserService.cs | ChangePasswordAsync (修复后) | "密码已修改" |
| T1-X3-06 | 禁用用户 | UserService.cs | ToggleStatusAsync (->Disabled 时) | "用户已禁用" |

**注意事项**:
- T1-X3-01: 仅撤销**同一用户**的旧 Token
- T1-X3-06: 仅在 `Enabled -> Disabled` 方向触发
- 撤销操作失败时记录 Warning 日志，**不阻塞**主操作 (try-catch 包裹)

#### 测试策略

每个场景 1 个集成测试:
1. 操作前创建有效 Token
2. 执行触发操作
3. 验证旧 Token 的 `IsRevoked == true`
4. 验证使用旧 RefreshToken 刷新返回 401

---

### 1.4 X7: 引用检查修复 (10 项) [重点设计]

#### 现状分析

| 模块 | Service 实现 | CanDelete | Controller 端点 | 删除时检查 |
|------|-------------|-----------|----------------|-----------|
| Patient | CheckReferenceAsync 已实现 (L725-740) | 硬编码 `true` (L749) | **缺失** | **未调用** |
| Herb | CheckReferenceAsync 已实现 (L517-537) | 硬编码 `true` (L546) | 已存在 (L281-331) | **未调用** |

#### 技术方案

**CanDelete 规则** (软删除系统):
```csharp
CanDelete = !hasReferences;  // 有引用时不允许删除
```

**患者模块 (6 项)**:

| 任务 | 修改文件 | 方案 |
|------|----------|------|
| T1-X7-01 | PatientService.DeleteAsync | 调用 CheckReferenceAsync，`hasReferences` 时返回 422 |
| T1-X7-02 | PatientService.BatchDeleteAsync | 逐个检查，收集失败项，返回 `BatchOperationResultDto` |
| T1-X7-03 | PatientsController | 新增 `GET /{id}/check-reference` 端点 |
| T1-X7-04 | PatientService.CheckReferenceAsync (L749) | `CanDelete = !hasReferences` 替换硬编码 |
| T1-X7-05 | PatientsController | 新增 `POST /batch-check-reference` 端点 (最多 100 条) |
| T1-X7-06 | PatientService.BatchCheckReferenceAsync | 确认实际查询逻辑正确 |

**药材模块 (4 项)**:

| 任务 | 修改文件 | 方案 |
|------|----------|------|
| T1-X7-07 | HerbService.DeleteAsync | 调用 CheckReferenceAsync，`hasReferences` 时返回 422 |
| T1-X7-08 | HerbService.BatchDeleteAsync | 逐个检查，收集失败项 |
| T1-X7-09 | HerbService.CheckReferenceAsync (L546) | `CanDelete = !hasReferences` 替换硬编码 |
| T1-X7-10 | HerbService.DeleteAsync | 有引用时返回 422 (UnprocessableEntity) |

#### 删除保护流程

```
DeleteAsync(id, isAdmin)
  |
  +-- CheckReferenceAsync(id)
  |   +-- hasReferences?
  |       +-- false -> 执行软删除
  |       +-- true
  |           +-- isAdmin && forceDelete -> 执行软删除 + 警告日志
  |           +-- else -> 返回 422 + 引用详情
```

#### 测试策略

| 测试 | 验证点 |
|------|--------|
| Delete_WithReferences_ShouldReturn422 | 有引用时拒绝删除 |
| Delete_NoReferences_ShouldSucceed | 无引用时正常删除 |
| Delete_AdminForce_WithReferences_ShouldSucceed | Admin 强制删除 |
| BatchDelete_PartialReferences_ShouldReturnMixed | 部分有引用的批量结果 |

---

### 1.5 S2: 权限矩阵修复 (9 项)

#### 核心修改: CanManageUser

**当前** (UserService.cs:78-90):
```csharp
UserRole.Admin => targetUserRole.Value == UserRole.Doctor,  // 只能管理 Doctor
```

**修复**:
```csharp
UserRole.Admin => targetUserRole.Value is UserRole.Doctor or UserRole.Receptionist,
```

**修复后权限矩阵**:

| 操作者 | -> Receptionist | -> Doctor | -> Admin | -> SuperAdmin |
|--------|----------------|-----------|----------|---------------|
| SuperAdmin | O | O | O | O |
| Admin | **O** | O | X | X |
| Doctor | X | X | X | X |
| Receptionist | X | X | X | X |

#### 9 项任务分解

| 任务 | 修改文件 | 方案 |
|------|----------|------|
| T1-S2-01 | UserService.CanManageUser | 添加 Receptionist 到 Admin 可管理列表 |
| T1-S2-02 | UserService.UpdateAsync | 角色变更时复用 CanManageUser |
| T1-S2-03 | UserService.DeleteAsync | 添加 `if (id == currentUserId) return Failure("不能删除自己")` |
| T1-S2-04 | UserService.ChangePasswordAsync | 调用 `PasswordPolicyValidator.Validate(newPassword)` |
| T1-S2-05 | UsersController | ChangePassword 端点方法级 `[Authorize]` 替代类级 AdminOnly |
| T1-S2-06 | UsersController | UpdateProfile 方法级 `[Authorize]` |
| T1-S2-07 | UserService.ToggleStatusAsync | 最后管理员保护: 禁用前检查 Admin 数量 > 1 |
| T1-S2-08 | UserService + Controller | 新增 `BatchUpdateStatusAsync`，复用 CanManageUser + 保护 |
| T1-S2-09 | UsersController | GetCurrentUser 方法级 `[Authorize]` |

#### 授权调整方案

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
}
```

#### 最后管理员保护

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

#### 测试策略

| 测试 | 验证点 |
|------|--------|
| Admin_CanManage_Receptionist | Admin 可管理 Receptionist |
| Admin_CannotManage_Admin | Admin 不能管理 Admin |
| DeleteSelf_ShouldFail | 不能删除自己 |
| DisableLastAdmin_ShouldFail | 不能禁用最后一个管理员 |
| NonAdmin_CanGetCurrentUser | 非管理员可获取自己信息 |
| NonAdmin_CanChangeOwnPassword | 非管理员可修改自己密码 |

---

### 1.6 S3: EditReason 强制校验 (4 项)

#### 现状

- `RequiresEditReason` 在 PermissionService 中已实现 (MedicalCasePermissionService.cs:155-162)
- 当前仅检查 `IsLocked` (Completed + 跨日)
- CommandService 的 Update 方法签名中**没有** EditReason 参数

#### 技术方案

**1. 扩展 RequiresEditReason 逻辑** (T1-S3-04):

```csharp
public bool RequiresEditReason(MedicalCase mc, Guid currentUserId)
{
    if (mc.IsLocked) return true;                // 原有: 跨日锁定
    if (mc.UserId != currentUserId) return true; // 新增: 非本人编辑
    if (mc.IsCompleted) return true;             // 新增: 当天已完成
    return false;
}
```

**2. CommandService 方法签名扩展** (T1-S3-01/02):

```csharp
// UpdateConsultationAsync (L144-186) 和 UpdatePrescriptionAsync 均新增 editReason 参数
public async Task<MedicalCase?> UpdateConsultationAsync(
    Guid medicalCaseId, ConsultationInputDto request,
    Guid currentUserId, bool isAdmin = false,
    string? editReason = null)  // 新增
{
    if (_permissionService.RequiresEditReason(medicalCase, currentUserId)
        && string.IsNullOrWhiteSpace(editReason))
    {
        throw new BusinessException(EC.ValidationFailed, "修改已完成/锁定的医案需要提供修改原因");
    }
    // ... 现有逻辑 ...
}
```

**3. 审计日志存储** (T1-S3-03):
将 `editReason` 传递给 `MedicalCaseAuditService.LogChangeAsync` (L113-203)，存入 `Reason` 字段。

| 任务 | 修改文件 |
|------|----------|
| T1-S3-01 | MedicalCaseCommandService.UpdateConsultationAsync (L144) |
| T1-S3-02 | MedicalCaseCommandService.UpdatePrescriptionAsync (L291) |
| T1-S3-03 | MedicalCaseAuditService.LogChangeAsync (L113) |
| T1-S3-04 | MedicalCasePermissionService.RequiresEditReason (L155) |

---

## 二、Sprint 2: 核心功能修复 (51 项)

**风险等级**: 高 | **前置依赖**: S1-A1-01 (索引修复) | **就绪度**: 可直接执行

### 2.1 X8: 打印层级重构 (12 项) [高风险 -- 深入设计]

#### 现状分析

**当前打印状态字段位于 Prescription 实体** (PrescriptionModel.cs):
- `PrintVersion`: L66-68 (默认 1)
- `LastPrintedAt`: L70-72
- `PrintCount`: L74-76 (默认 0)
- `IsPrinted`: L78-80 (默认 false)

**打印日志**: `PrescriptionPrintLog` (PrescriptionPrintLog.cs:1-66)
- 10 个字段: PrescriptionId / PrintVersion / PrintedAt / PrintedBy / PrintedByName / PrinterName / IsSuccess / ErrorMessage / Remark
- 配置: HasOne(Prescription).WithMany(PrintLogs).Cascade (PrescriptionPrintLogConfiguration.cs:17-22)

**打印服务**: `PrescriptionPrintService` (565 行)
- PrintAsync (L38-73): 主流程，调用 BuildFixedDocument + ExecutePrint
- **无打印后回写逻辑**: PrintAsync 仅返回 `bool success`，不更新 IsPrinted 等字段
- A5/A4 常量: A5=559x794, A4=794x1123 (L26-28)

**PRD 要求**: 打印属于 MedicalCase 聚合根能力，不是 Prescription 独立行为。

#### 技术方案: 3 阶段迁移

**阶段 1: 新增 MedicalCase 打印字段 + 新实体**

```csharp
// MedicalCase 实体新增字段 (MedicalCase.cs)
public bool IsPrinted { get; set; } = false;
public int PrintVersion { get; set; } = 0;
public int PrintCount { get; set; } = 0;
public DateTime? LastPrintedAt { get; set; }

// 新增 PrintType 枚举
public enum PrintType
{
    Prescription = 1,  // 处方打印
}

// 新增 MedicalCasePrintLog 实体 (复制 PrescriptionPrintLog 结构，改外键)
public class MedicalCasePrintLog : BaseEntity
{
    public Guid MedicalCaseId { get; set; }
    public PrintType PrintType { get; set; }
    public int PrintVersion { get; set; }
    public DateTime PrintedAt { get; set; }
    public Guid? PrintedBy { get; set; }
    public string? PrintedByName { get; set; }
    public string? PrinterName { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}
```

**阶段 2: EF Migration + 数据迁移**

```sql
-- 1. 添加 MedicalCase 打印字段
ALTER TABLE MedicalCases ADD IsPrinted BIT NOT NULL DEFAULT 0;
ALTER TABLE MedicalCases ADD PrintVersion INT NOT NULL DEFAULT 0;
ALTER TABLE MedicalCases ADD PrintCount INT NOT NULL DEFAULT 0;
ALTER TABLE MedicalCases ADD LastPrintedAt DATETIME2 NULL;

-- 2. 创建 MedicalCasePrintLogs 表
CREATE TABLE MedicalCasePrintLogs (...);

-- 3. 数据迁移: 从 Prescription 复制到 MedicalCase
UPDATE mc SET
    mc.IsPrinted = p.IsPrinted,
    mc.PrintVersion = p.PrintVersion,
    mc.PrintCount = p.PrintCount,
    mc.LastPrintedAt = p.LastPrintedAt
FROM MedicalCases mc
JOIN Prescriptions p ON mc.Id = p.MedicalCaseId;

-- 4. 迁移打印日志
INSERT INTO MedicalCasePrintLogs (...)
SELECT mc.Id, 1/*Prescription*/, ppl.PrintVersion, ...
FROM PrescriptionPrintLogs ppl
JOIN Prescriptions p ON ppl.PrescriptionId = p.Id
JOIN MedicalCases mc ON p.MedicalCaseId = mc.Id;
```

**阶段 3: 打印回写链重构**

当前 `PrescriptionPrintService.PrintAsync` (L38-73) 仅返回 `bool success`，需新增回调:

```csharp
// 修改 PrintAsync，新增打印成功后回调
if (success)
{
    await OnPrintCompletedAsync(model.MedicalCaseId, model.PrintedByUserId, model.PrintedByName, options?.PrinterName);
}

private async Task OnPrintCompletedAsync(Guid medicalCaseId, Guid? printedBy, string? printedByName, string? printerName)
{
    // 通过 IMedicalCaseDataSource 或 Repository 更新
    var mc = await _repository.GetByIdAsync(medicalCaseId);
    mc.IsPrinted = true;
    mc.PrintCount++;
    mc.PrintVersion++;
    mc.LastPrintedAt = DateTime.UtcNow;
    await _repository.UpdateAsync(mc);

    // 创建打印日志
    var log = new MedicalCasePrintLog { ... };
    await _printLogRepository.AddAsync(log);
}
```

#### 12 项任务映射

| 任务 ID | 阶段 | 描述 | 修改文件 |
|---------|------|------|----------|
| T2-X8-02 | 1 | MedicalCase 实体添加 4 个打印字段 | `Entities/MedicalCases/MedicalCase.cs` |
| T2-X8-12 | 1 | 创建 MedicalCasePrintLog 实体 | `Entities/MedicalCases/MedicalCasePrintLog.cs` (新建) |
| T2-X8-03 | 1 | PrescriptionPrintLog 重构为 MedicalCasePrintLog | Configuration + Migration |
| T2-X8-01 | 2 | 实现打印保护逻辑 (已打印不可修改处方) | MedicalCaseCommandService.cs:323,383 |
| T2-X8-04 | 3 | PrintHandler 打印后设置 IsPrinted=true | PrescriptionPrintService.cs:38-73 |
| T2-X8-05 | 3 | 打印后更新 PrintCount++ 和 LastPrintedAt | PrescriptionPrintService.cs |
| T2-X8-06 | 3 | PrintCount 递增逻辑 | PrescriptionPrintService.cs |
| T2-X8-07 | 3 | IsPrinted=true 回写 | PrescriptionPrintService.cs |
| T2-X8-08 | 3 | LastPrintedAt 时间戳更新 | PrescriptionPrintService.cs |
| T2-X8-09 | 3 | 打印层级从处方层迁移到医案层 | PrintService + Mapper |
| T2-X8-10 | 3 | PrintVersion 递增 | PrescriptionPrintService.cs |
| T2-X8-11 | 3 | 打印版本号快照记录 | MedicalCasePrintLog |

#### 测试策略

- 集成测试: 打印后验证 MedicalCase.IsPrinted/PrintCount/PrintVersion 更新
- 集成测试: 已打印医案修改处方返回 BusinessException
- 数据迁移测试: 验证现有 Prescription 打印数据正确迁移到 MedicalCase

---

### 2.2 X5: 字段验证值对齐 (15 项)

**修改模式统一**: 修改 Validator/DTO/配置中的常量值。

| 任务 ID | 当前值 | PRD 值 | 修改文件:行号 | 修改方式 |
|---------|--------|--------|--------------|----------|
| T2-X5-01 | MinLength=8 (已正确) | 8 | PasswordPolicyValidator.cs:19 | 确认无需修改 |
| T2-X5-02 | 密码最小 6 | 8 | UserInputDtoValidator.cs:32 | `.MinimumLength(8)` |
| T2-X5-03 | IdNumber/Phone/Address 选填 | Required | PatientInputDtoValidator.cs:32-46 | 移除 `.When()` 条件 |
| T2-X5-04 | Effect 用 RemarkMaxLength(500) | 500 | HerbInputDtoValidator.cs:58-62 | 已正确 (通过常量) |
| T2-X5-05 | Usage 200 (UsageMaxLength) | 500 | HerbInputDtoValidator.cs:64-68 | 修改 UsageMaxLength 常量为 500 |
| T2-X5-06 | Spec 50 (硬编码) | 100 | HerbInputDtoValidator.cs:39-43 | `.MaximumLength(100)` |
| T2-X5-07 | Unit 20 | 10 | HerbInputDtoValidator.cs:45-49 | `.MaximumLength(10)` |
| T2-X5-08 | Formula.Effect 200 | 500 | FormulaInputDtoValidator.cs:17-19 | `.MaximumLength(500)` |
| T2-X5-09 | 功效/用法必填 | 选填 | Desktop FormulaValidator | 添加 `.When()` 条件 |
| T2-X5-10 | Formula.Usage 500 | 500 | FormulaInputDtoValidator.cs:25-27 | 已正确 |
| T2-X5-11 | DosageCount 无校验 | >0 | MedicalCaseValidator | 添加 `.GreaterThan(0)` |
| T2-X5-12 | OperatorName 50 | 100 | MedicalCaseValidator | `.MaximumLength(100)` |
| T2-X5-13 | DefaultRole "Staff" | "Doctor" | appsettings.json:109 | 改为 `"Doctor"` |
| T2-X5-14 | InactivityTimeout 5 分钟 | 15 分钟 | ClientSessionOptions.cs:17 | `= 15` |
| T2-X5-15 | Shell 读取超时值 | 确认使用 ClientSessionOptions | Shell 配置读取点 |

**注意**: T2-X5-02 密码最小长度从 6 改为 8 需同步更新 Desktop 端 LoginViewModel 的校验逻辑 (LoginViewModel.cs:50 附近)。

---

### 2.3 S4: 功能 Bug/审计 (15 项)

| 任务 ID | 问题 | 方案 | 修改文件:行号 |
|---------|------|------|-------------|
| T2-S4-01 | FormulaMapper Herbs 列表 Server 端忽略 | 移除 `[MapperIgnoreTarget(nameof(Herbs))]` (L49)，在 Service 层填充 Herbs | FormulaMapper.cs:49 |
| T2-S4-02 | TotalPrice 始终为 0 | PrescriptionItem.Amount (L71) 已定义 `UnitPrice * Dosage`，需在 Prescription 级别累加: `TotalPrice = Items.Sum(i => i.Amount) * Discount` | Prescription 计算/Mapper |
| T2-S4-03 | PrescriptionItem.Usage 错误赋值 | 检查 PrescriptionMapper 中 Usage 字段映射，确保从 DTO 正确映射到 Entity 的 Usage (L78-83) | PrescriptionMapper |
| T2-S4-04 | 审计日志保留 30->365 天 | 修改 `AddDays(-30)` 为 `AddDays(-365)` | SecurityAuditCleanupService.cs:90 |
| T2-S4-05 | SensitiveDataAttribute 冲突 | 代码调研显示仅 1 份定义，确认无冲突 | 验证即可 |
| T2-S4-06 | CleanupService 硬编码 | `LogCleanupOptions` 已存在 (L128-130, RetentionDays=90)，确认 Service 已使用 IOptions | LogCleanupService.cs |
| T2-S4-07 | CleanupService 改分批删除 | 确认已实现 BatchSize=1000 | 验证即可 |
| T2-S4-08 | 实现患者状态管理 | Patient 已有 Status 字段 (PatientModel.cs:106-108 CommonStatus)，需添加 ToggleStatus API + Desktop UI | PatientService + Controller + ViewModel |
| T2-S4-09 | Unhealthy 映射修正 | HealthController.CheckDatabase (L93-96) 返回 "Unhealthy"，上层 (L73) 映射为 "Degraded" + 503，应改为 "Unhealthy" + 503 | HealthController.cs:73 |
| T2-S4-10 | 健康检查详细响应补充 | 添加 DB 版本、迁移状态、连接池等信息到 GetDetailedHealth (L56-83) | HealthController.cs:56 |
| T2-S4-11 | 异常到通知类型映射 | 实现 ExceptionSeverity -> NotificationDisplayMode (Toast/Dialog) | ExceptionSeverityMapper (新建) |
| T2-S4-12 | 创建 PrintType 枚举 | 见 X8 阶段 1 | 与 X8 合并 |
| T2-S4-13 | 实现打印日志写入 | 见 X8 阶段 3 | 与 X8 合并 |
| T2-S4-14 | 不活跃超时确认 NFR 引用点 | 确认 NFR 文档引用 ClientSessionOptions | 文档更新 |
| T2-S4-15 | 密码过期 30->90 天 | PasswordPolicyOptions.PasswordExpirationDays | PasswordPolicyValidator.cs |

---

### 2.4 架构新增 (4 项) + PRD 修订 (5 项)

| 任务 | 方案 | 修改文件:行号 |
|------|------|-------------|
| A2-01 | 3 个 import-template 端点移除 `[AllowAnonymous]` | FormulasController.cs:209, HerbsController.cs:204, PatientsController.cs:204 |
| A2-02 | Program.cs 启用 `app.UseRateLimiter()` | Program.cs (ConfigureAllMiddleware 中) -- 配置已存在 appsettings.json:54-77 |
| A2-03 | MedicalCaseConfiguration 补充 `IX_MedicalCases_UserId` 索引 | MedicalCaseConfiguration.cs (L36 后) |
| A2-04 | Desktop 架构规则迁移到主测试项目 | `tests/LYBT.Tests.Architecture/` |
| PRD 5 项 | 药材/打印细节 PRD 接受当前代码实现 | PRD 文档更新 |

---

## 三、Sprint 3: 体系统一与文档同步 (85 项)

**风险等级**: 高 | **前置依赖**: Sprint 2 | **建议拆分**: 3a (代码 47 项) + 3b (文档+标准 38 项)

> **设计文档**: [design-deepening-phase3.md](2026-02-22-design-deepening-phase3.md) 4.1-4.3 节 + [d2-d5-design](2026-02-22-d2-d5-design-patterns-dependencies.md) D5-1/D5-2 节

### 3.1 X1: 错误码 MCCEE 统一 (15 项) [高风险 -- 深入设计]

#### 现状分析

**ErrorCode.cs** (384 行, 66 个错误码):
```
0xxxx (通用):  0-12 共 13 个 -- 不符合 5 位编码规范
1xxxx (用户):  10001-10015 共 15 个
2xxxx (患者):  20001-20006 共 6 个
3xxxx (医案):  30001-30008 共 8 个
4xxxx (处方):  40001-40007 共 7 个
5xxxx (草药):  50001-50006 共 6 个
6xxxx (配方):  60001-60006 共 6 个
7xxxx (问诊):  70001-70005 共 5 个
8xxxx (同步):  缺失
```

**ErrorMessages.cs** (89 条中英文映射):
- 映射方法: `Get(ErrorCode, useEnglish)` (L97)
- 格式化方法: `GetFormatted(ErrorCode, useEnglish, args)` (L111)

**ClientErrorMessageMapper.cs** (161 条 Desktop 映射):
- 前缀映射: `ErrorCodePrefixMessages` (L66-75)
- 精确映射: `ErrorCodeMessages` (L77-161)

**ApiErrorCodes.cs**: 字符串常量 (VALIDATION_ERROR, UNAUTHORIZED 等)，注释标注"错误码统一后删除"

#### 技术方案

**阶段 1: 通用错误码迁移 (0-12 -> 0xxxx)**

```csharp
// 旧: Unknown = 0, InvalidRequest = 1, NotFound = 2, ...
// 新: Unknown = 00000, InvalidRequest = 00001, NotFound = 00002, ...
```

需要更新 `ErrorCode.ToFormattedString()` 扩展方法:
```csharp
public static string ToFormattedString(this ErrorCode code)
    => $"ERR-{(int)code:D5}";  // 格式化为 ERR-00001
```

**阶段 2: 按模块补齐缺失错误码**

| 模块 | 当前范围 | 需补齐 |
|------|---------|--------|
| Auth | 10001-10015 | Auth 专用错误码归属到 1xxxx |
| Patients | 20001-20006 | 补齐 ERR-20002/20004/20005/20006 |
| Herbs | 50001-50006 | 对齐编号规范 |
| Formulas | 60001-60006 | 17 个错误码对齐 |
| MedicalCase | 30001-30008 | 迁移到 ERR-3xxxx |
| Sync | 无 | 新增 8xxxx 范围 20 个错误码 |

**阶段 3: 客户端同步更新**

ClientErrorMessageMapper 的 `ErrorCodeMessages` 字典需同步更新键值。

#### 术语违规同步清理

调研精确定位:

| 文件 | 行号 | 违规内容 | 修正 |
|------|------|---------|------|
| ErrorMessages.cs | L48 | `// 病历模块` | `// 医案模块` |
| ErrorMessages.cs | L49-56 | 多处 "病历" | "医案" |
| ClientErrorMessageMapper.cs | L71 | `"病历相关错误"` | `"医案相关错误"` |
| ClientErrorMessageMapper.cs | L116 | `"就诊记录"` | `"医案"` |
| ClientErrorMessageMapper.cs | L121-128 | 多处 "病历" | "医案" |
| NotFoundException.cs | L70 | `"病历不存在"` | `"医案不存在"` |

#### 15 项任务映射

| 任务 ID | 模块 | 描述 |
|---------|------|------|
| T3-X1-01 | auth | Auth 错误码迁移到 5 位 MCCEE |
| T3-X1-02~06 | patients | 实现 ERR-20002/20004/20005/20006 + 删除返回 422 |
| T3-X1-07~10 | herbs | Herbs 错误码编号对齐 + 4 个新错误码 |
| T3-X1-11 | formulas | Formulas 17 个错误码对齐 |
| T3-X1-12 | medical-cases | MedicalCase 错误码迁移到 ERR-3xxxx |
| T3-X1-13 | sync | 同步模块 20 个 PRD 错误码全部实现 |
| T3-X1-14 | error-handling | ErrorCode 7xxxx 语义重新对应 |
| T3-X1-15 | error-handling | 修复 ClientErrorMessageMapper 解析 ERR-10004 |

---

### 3.2 X4: Service 层 ErrorCode 替代 (5 项) [深入设计]

#### 精确定位: InvalidOperationException 分布

**MedicalCaseCommandService.cs** (726 行, 6 处):

| 行号 | 当前代码 | 替换方案 |
|------|---------|---------|
| L170 | `throw new InvalidOperationException("医案的辨证信息不存在")` | `throw NotFoundException.Consultation(medicalCaseId)` |
| L254 | `throw new InvalidOperationException("未标记需要开处方...")` | `throw new BusinessException(EC.InvalidMedicalCaseState, "...")` |
| L257 | `throw new InvalidOperationException("医案已存在处方...")` | `throw new BusinessException(EC.MedicalCaseConflict, "...")` |
| L323 | `throw new InvalidOperationException("处方已打印，不允许修改")` | `throw new BusinessException(EC.PrescriptionAlreadyPrinted, "...")` |
| L383 | `throw new InvalidOperationException("处方已打印，不允许删除")` | `throw new BusinessException(EC.PrescriptionAlreadyPrinted, "...")` |
| L459 | `throw new InvalidOperationException("医案不存在")` | `throw NotFoundException.MedicalCase(medicalCaseId)` |

**MedicalCaseServiceHelper.cs** (222 行, 5 处):

| 行号 | 当前代码 | 替换方案 |
|------|---------|---------|
| L120 | `"患者不存在"` | `throw NotFoundException.Patient(patientId)` |
| L123 | `"医生不存在"` | `throw NotFoundException.User(doctorId)` |
| L134 | `"该患者已有进行中的医案"` | `throw new BusinessException(EC.MedicalCaseConflict, "...")` |
| L142 | `"该患者已有暂存的医案"` | `throw new BusinessException(EC.MedicalCaseConflict, "...")` |
| L180 | `"操作失败，请稍后重试"` | `throw new BusinessException(EC.ServiceUnavailable, "...")` |

**MedicalCaseStateService**: 8 处 (状态流转异常)

**JwtService.cs**: 3 处 (L37-55, SecretKey 验证)

**已有异常类** (可直接使用):
- `AppException` (基类, 95 行): ErrorCode + UserMessage + ShowDetailToUser + GetHttpStatusCode
- `BusinessException` (49 行): 400 + BusinessRule
- `NotFoundException` (77 行): 404 + ResourceType/ResourceId + 静态工厂方法

#### 5 项任务

| 任务 | 修改文件 | 替换数量 |
|------|----------|---------|
| T3-X4-01 | UserService | 5+ 处 -> BusinessException/NotFoundException |
| T3-X4-02 | UserService | 用户名重复返回 409 (ConflictException) |
| T3-X4-03 | HerbService | 5+ 处 -> ErrorCode |
| T3-X4-04 | FormulaService | 5+ 处 -> ErrorCode |
| T3-X4-05 | AuthService | TokenRevoked 提示精确化 |

**A3-07**: FormulaService 补齐 BaseService 继承，享受 `ExecuteAsync<T>()` 统一异常处理。

---

### 3.3 X6: 分页筛选迁移 Repository (6 项) [深入设计]

#### 精确定位: 内存过滤代码

**HerbService.GetPagedAsync** (L43-67):
```csharp
// 数据库级别搜索
var pagedResult = await _repository.GetPagedAsync(page, pageSize, keyword);
// 内存过滤 category (L53-58)
if (!string.IsNullOrWhiteSpace(category))
{
    dtos = dtos.Where(h => h.Category.Contains(category, StringComparison.OrdinalIgnoreCase)).ToList();
}
// TotalCount 错误: 内存过滤后重新计算 (L63)
TotalCount = !string.IsNullOrWhiteSpace(category) ? dtos.Count : pagedResult.TotalCount
```

**MedicalCaseQueryService.GetListAsync** (L49-100):
```csharp
var result = await _repository.GetPagedWithDetailsAsync(page, pageSize);
// 内存过滤 status/patientId/keyword/doctorId (L56-80)
var filteredItems = result.Items.AsQueryable();
if (status.HasValue) filteredItems = filteredItems.Where(m => m.CaseStatus == status.Value);
if (patientId.HasValue) filteredItems = filteredItems.Where(m => m.PatientId == patientId.Value);
// TotalCount 错误 (L95): 使用过滤后的 filteredItems.Count()
```

**FormulaService.GetPagedAsync** (L35-91):
```csharp
var pagedResult = await _repository.GetPagedWithDetailsAsync(page, pageSize, keyword);
// 内存过滤 角色 + 分类 (L50-65)
if (!isAdmin && currentUserId.HasValue) { ... }
if (!string.IsNullOrWhiteSpace(category)) { ... }
```

**FormulaService.GetPendingValidationFormulasAsync** (L282-298):
```csharp
var allFormulas = await _repository.GetAllAsync();  // 全量加载!
var pendingFormulas = allFormulas.Where(f => f.ValidationStatus == FormulaValidationStatus.Draft).ToList();
```

#### 技术方案

利用 `BaseRepository.GetPagedAsync` 高级重载 (L243-278):

```csharp
public virtual async Task<PagedResult<TEntity>> GetPagedAsync(
    int pageNumber, int pageSize,
    Expression<Func<TEntity, bool>>? predicate = null,
    Expression<Func<TEntity, object>>? orderBy = null,
    bool ascending = false)
```

**修复示例** (HerbService):

```csharp
public async Task<Result<PagedResult<HerbListDto>>> GetPagedAsync(
    int page, int pageSize, string? keyword, string? category)
{
    Expression<Func<Herb, bool>>? predicate = null;
    if (!string.IsNullOrWhiteSpace(category))
    {
        predicate = h => h.Category != null && h.Category.Contains(category);
    }
    var pagedResult = await _repository.GetPagedAsync(page, pageSize, predicate);
    // TotalCount 现在由数据库正确计算
    ...
}
```

| 任务 | Service | 筛选字段 | 修改方式 |
|------|---------|---------|---------|
| T3-X6-01 | UserService | role + status | predicate 参数 |
| T3-X6-02 | HerbService | category | predicate 参数 |
| T3-X6-03 | FormulaService | category | predicate 参数 |
| T3-X6-04 | FormulaService | GetPendingValidation 改分页 | predicate + GetPagedAsync |
| T3-X6-05 | MedicalCaseQueryService | status + patientId + doctorId | predicate 参数 |
| T3-X6-06 | ErrorHandling | HTTP 429 映射到错误码 | RateLimiting + ErrorCode |

---

### 3.4 其他分组 (架构 9 + 文档 16 + PRD 16 + 标准 6)

**架构新增 9 项**:

| 任务 | 描述 | 核心修改 |
|------|------|---------|
| A3-01 | ErrorCode.cs 术语违规修复 | 替换 "病历"->"医案", "问诊"->"辨证" |
| A3-02 | ErrorMessages.cs + NotFoundException.cs 术语修复 | 同上 |
| A3-03 | Service 层全面采用 BusinessException/NotFoundException | X4 的根因修复 |
| A3-04 | 两套架构测试合并 (ArchTests 24 + ServerArchTests 15 消除重复) | `tests/LYBT.Tests.Architecture/` |
| A3-05 | 添加 Shared 内部依赖架构规则 | 同上 |
| A3-06 | 术语铁律违规系统清理 (136 处/39 文件) | 全局搜索替换 |
| A3-07 | FormulaService 补齐 BaseService 继承 | FormulaService.cs |
| A3-08 | FallbackPolicy 设置 (当前注释状态 L110-113) | AuthenticationServiceCollectionExtensions.cs |
| A3-09 | 补齐 Shared.Logging/Desktop.Sync 零覆盖测试 | 新建测试文件 |

**文档同步 16 项**: DOC3-01~16，代码变更完成后更新 system-overview / desktop / server / data-model / shared 等文档。

**PRD 修订 16 项**: 认证/错误/日志/同步/配置模块 PRD 接受当前代码实现。

**标准固化 6 项**: 写入 NetArchTest 架构测试规则 (P-01 DualMode / P-02 Repository / P-03 ViewModel / P-06 Dependencies / P-08 CrossModule / P-09 Controllers)。

---

### 3.5 D5-1: ICrossModuleService ISP 拆分 (8 项) [新增 -- 详见 d2-d5-design]

> 来源: [d2-d5-design-patterns-dependencies.md](2026-02-22-d2-d5-design-patterns-dependencies.md) D5-1 节

将单一 `ICrossModuleService` (3 域 7 方法) 按域拆分为 3 个专用接口，符合 ISP 原则。

| 任务 ID | 描述 | 修改文件 |
|---------|------|----------|
| T3-D5-01 | 创建 IPatientCrossModuleService 接口 | `Infrastructure/Services/CrossModule/` (新建) |
| T3-D5-02 | 创建 IHerbCrossModuleService 接口 | 同上 |
| T3-D5-03 | 创建 IUserCrossModuleService 接口 | 同上 |
| T3-D5-04 | 创建 ReferenceCheckDto | 同上 |
| T3-D5-05 | CrossModuleService 实现 3 个接口 | `CrossModuleService.cs` 移入新目录 |
| T3-D5-06 | 更新 DI 注册 (1 实现 -> 3 接口) | `DatabaseServiceCollectionExtensions.cs` |
| T3-D5-07 | 更新 6 个消费者 Service 注入类型 | Formula/Auth/MedicalCase 等 Service |
| T3-D5-08 | 旧 ICrossModuleService 标记 `[Obsolete]` | `ICrossModuleQueryService.cs` |

---

### 3.6 D5-2: Sync 模块编译期依赖解耦 (4 项) [新增 -- 详见 d2-d5-design]

> 来源: [d2-d5-design-patterns-dependencies.md](2026-02-22-d2-d5-design-patterns-dependencies.md) D5-2 节

移除 Sync 模块对 Herbs/Patients/Formula 三个业务模块的 ProjectReference，CheckReference 方法迁移到跨模块接口。

| 任务 ID | 描述 | 修改文件 |
|---------|------|----------|
| T3-D5-09 | 实现 CheckHerbReferenceAsync | `CrossModuleService.cs` |
| T3-D5-10 | 实现 CheckPatientReferenceAsync | `CrossModuleService.cs` |
| T3-D5-11 | SyncService 改用 IHerb/IPatientCrossModuleService | `SyncService.cs` |
| T3-D5-12 | 删除 Sync.csproj 的 3 个业务模块 ProjectReference | `LYBT.Module.Sync.csproj` |

**前置依赖**: T3-D5-01~08 (D5-1 ISP 拆分完成后)

---

## 四、Sprint 4: 本地模式补齐 (62 项)

**风险等级**: 中 | **前置依赖**: S2-X8 (打印重构) | **就绪度**: 可直接执行

> **设计文档**: [dual-mode.md](../../03-architecture/05-dual-mode.md) SYNC-D01~D04
>
> **SYNC-D02 阶段说明**: S4 的 X2 工作包是"补全现有 DataSource 接口方法"(过渡态维护)，SYNC-D02 目标态"废除 DataSource 层、统一 DbContext Provider"需在 X2 完成后作为独立工作包启动。当前 X2 与 SYNC-D02 最终目标**不矛盾** -- X2 保证本地模式功能完整，SYNC-D02 在功能完整后执行架构简化。
>
> **SYNC-D03 运行时切换**: 预估 5~8 项任务 (DI 热替换 + 导航回首页 + 数据源切换提示 + 重连机制)，依赖 SYNC-D02 完成。当前列为 S4 可选增强项。

### 4.1 X2: IDataSource + 导入导出 (22 项)

#### 现状分析: Local 实现已基本完成

**重要发现**: 5 个 Local DataSource 的核心方法**均已实现**:

| 接口 | 方法数 | 已实现 | 缺口 |
|------|--------|--------|------|
| IUserDataSource (L9-44) | 6+基类 | 全部 (L25-222) | BatchDelete/Restore/ResetPassword/BatchToggle 未在接口中 |
| IPatientDataSource (L10-31) | 4+基类 | 全部 (L25-202) | 导入/导出未在接口中 |
| IHerbDataSource (L9-40) | 4+基类 | 全部 (L25-159) | 导入/导出/BatchToggle 未在接口中 |
| IFormulaDataSource (L9-40) | 5+基类 | 全部 (L25-236) | 验证/待验证列表/导入/导出 未在接口中 |
| IMedicalCaseDataSource (L10-57) | 6+基类 | 全部 (L29-307) | 聚合根 SaveAsync 统一入口 |

**DI 注册**: DataSourceRegistrationExtensions.cs
- Remote: L62-69 (5 个接口 -> 5 个 Remote 实现)
- Local: L74-91 (5 个接口 -> 5 个 Local 实现 + LocalAuthService + SyncService)

#### 技术方案: 接口扩展 + 方法实现

**用户模块 (8 项)**:

| 任务 | 接口方法 | 实现方案 |
|------|---------|---------|
| T4-X2-01 | ChangePasswordAsync | **已实现** (LocalUserDataSource:134-159, BCrypt 验证+更新) |
| T4-X2-02 | DeleteAsync 保护 | 添加 SuperAdmin 保护 + 最后管理员保护 |
| T4-X2-03 | RestoreAsync | 新增到 IUserDataSource，Local: IgnoreQueryFilters + IsDeleted=false |
| T4-X2-04 | BatchDeleteAsync | 新增，逐个检查保护条件 |
| T4-X2-05 | ResetPasswordAsync | 新增，BCrypt 哈希默认密码 |
| T4-X2-06 | ToggleStatusAsync 保护 | **已实现** (L161-182)，添加最后管理员保护 |
| T4-X2-07 | BatchToggleStatusAsync | 新增批量启用/禁用 |
| T4-X2-08 | GetCurrentUser | 从 LocalAuthService 获取当前用户 ID |

**患者模块 (4 项)**:

| 任务 | 方案 |
|------|------|
| T4-X2-09 | 本地模式批量导入 (NPOI Excel -> SQLite) |
| T4-X2-10 | 本地模式导出 (SQLite -> Excel/JSON) |
| T4-X2-11 | Desktop 端引用检查 (查询 LocalMedicalCaseDataSource) |
| T4-X2-12 | Desktop 端批量引用检查 |

**药材模块 (6 项)**: T4-X2-13~18 (BatchToggle / Excel 导入 / JSON 导入 / 导出 / 引用检查 / 模板下载)

**验方模块 (4 项)**: T4-X2-19~22 (延迟绑定验证 / 待验证列表 / 批量导入 / 导出)

---

### 4.2 S5: 打印模板完善 (11 项) [深入设计]

#### 现状: PrescriptionPrintTemplate.xaml 结构

```
XAML 模板 (268 行):
- 页面: A5 148mm x 210mm, 边距 57px(左右 15mm) x 38px(上下 10mm)
- 字体: FontFamily="STKaiti, 华文楷体, KaiTi, SimKai, Microsoft YaHei" (L21-23)
- Grid: 11 行 (L83-95)
  Row 0: 标题
  Row 1-5: 患者信息 (姓名/性别/年龄/时间/门诊号/科别/电话/住址/诊断/诊见)
  Row 6: 处方内容 (ItemsControl + WrapPanel, 每味药 Width=95)
  Row 7: 弹性空白
  Row 8: 分隔线
  Row 9: 签名行 (医师/审核/调配)
  Row 10: 费用行 (诊疗费/药费/治疗费/合计)
```

**常量**: A5PageSize=559x794, A4PageSize=794x1123 (PrescriptionPrintService.cs:26-28)

#### 修改清单

| 任务 | 当前 | PRD | 修改文件:行号 | 修改方式 |
|------|------|-----|-------------|----------|
| T4-S5-04 | STKaiti (L21-23) | SimSun 宋体 | PrescriptionPrintTemplate.xaml:21-23 | `FontFamily="SimSun, 宋体, Microsoft YaHei"` |
| T4-S5-05 | 边距 57px/38px (L83) | 8mm (~30px) | PrescriptionPrintTemplate.xaml + PrintService | `Margin="30,30,30,30"` |
| T4-S5-06 | 无诊所信息区 | 添加 | XAML Row 0 改为诊所信息 (名称/地址/电话) | 新增 Grid Row |
| T4-S5-07 | 诊断信息简略 (L170-193) | 完善四诊 | XAML Row 4-5 | 添加 望/闻/问/切 字段绑定 |
| T4-S5-08 | 无煎法标注 | 渲染 Usage | XAML Row 6 下方 | 添加 TextBlock 绑定 Advice |
| T4-S5-09 | 无分页 (>12 味) | >12 味分页 | BuildFixedDocument (L238-250) | 药材超过 12 个时创建新 FixedPage |
| T4-S5-10 | DoctorName 手填 (L224-239) | 自动绑定当前用户 | PrintModel 构建 | 从 SessionManager.CurrentUser 获取 |
| T4-S5-11 | 费用无 Discount | 纳入 Discount | TotalPrice 计算 (L242-269) | `TotalPrice = Sum(Amount) * Discount` |
| T4-S5-01 | 无失败日志 | 记录打印失败 | PrescriptionPrintService.PrintAsync:L62-66 | catch 块写入 MedicalCasePrintLog(IsSuccess=false) |
| T4-S5-02 | 无远程日志 API | 新增 API | MedicalCaseController | `POST /api/v1/medical-cases/{id}/print-logs` |
| T4-S5-03 | 无本地日志存储 | SQLite 存储 | LocalMedicalCaseDataSource | 新增 AddPrintLogAsync 方法 |

---

### 4.3 S6: Desktop Shell (4 项)

#### 现状

- **MenuManager.cs**: 12 个命令 (L35-81)，无角色可见性控制
- **NavigationCoordinator.cs**: `_navigationHistory` (L25-26)，List<string> 无上限
- **App.xaml.cs**: 模块加载 (L338-363) 无角色过滤
- **ConnectionMode**: Remote/Local 枚举 (LYBT.Desktop.Foundation)

| 任务 | 方案 | 修改文件:行号 |
|------|------|-------------|
| T4-S6-01 | MenuManager 根据 `CurrentUser.Role` 控制 Visibility | MenuManager.cs:85-104 |
| T4-S6-02 | ConnectionMode.Local 时禁用同步/用户管理菜单 | MenuManager.cs 新增 ConnectionMode 判断 |
| T4-S6-03 | `_navigationHistory` 超过 20 条时 RemoveAt(0) | NavigationCoordinator.cs:86-118 |
| T4-S6-04 | 本地模式隐藏密码修改/远程相关选项 | ShellViewModel/AccountSettings |

---

### 4.4 架构/PRD/标准 (25 项)

**架构新增 7 项**:

| 任务 | 描述 | 修改文件 |
|------|------|---------|
| A4-01 | RFC URI 映射重复定义 DRY 合并 | 搜索重复 URI 常量 |
| A4-02 | Patient.EmergencyContactPhone 添加 SensitiveData | PatientModel.cs:98-100 (当前无标记，PhoneNumber L58-64 已标记) |
| A4-03 | Patient/Herb 缺少资源级 AuthorizationHandler 评估 | 评估文档 |
| A4-04 | ExtractUserInfo 方法重复 DRY 合并 | BaseService.cs:88-115 (实际仅 1 处定义，无需合并) |
| A4-05~07 | Desktop.CardReader/Admin/Clinical 零覆盖补齐 | 新建测试文件 |

**PRD 修订 12 项**: 医案/桌面/同步/读卡器模块 PRD 接受。

**标准固化 6 项**: 6 条开发规范写入文档 (CQRS-Boundary / CorrelationId / CrossModule / SensitiveData / AAA-Test / JWT-Security)。

---

## 五、Sprint 5+: 细节完善 (98 项)

**风险等级**: 低 | **前置依赖**: 部分依赖 Sprint 4 | **就绪度**: 可直接执行

> **设计文档**: [d2-d5-design](2026-02-22-d2-d5-design-patterns-dependencies.md) D2-1/D2-2/D5-3 节

### 5.1 P2: 功能完善 (45 项) -- 按模块分组

#### 认证增强 (8 项)

| 任务 | 描述 | 现状 | 方案 |
|------|------|------|------|
| T5-P2-01 | FailedLoginCount (远程) | 本地已实现 (LocalAuthService:61-94, 5 次/15 分钟)，远程缺失 | AuthService.LoginAsync 添加计数逻辑 |
| T5-P2-02 | UserDisabled 返回 403 替代 401 | 当前统一 401 | 检查 User.Status 后返回 `Forbid()` |
| T5-P2-03 | HMAC 篡改清除凭据 | AutoLoginToken 无篡改检测 | HMAC 签名校验，失败时清除 |
| T5-P2-04 | 30 天绝对过期 | AutoLoginToken 已有 30 天 (L675) | 确认 RefreshToken 也有绝对过期 |
| T5-P2-05 | TokenExpired 时尝试 AutoLogin 降级 | 无降级逻辑 | Desktop AuthService 捕获 401 后尝试 AutoLogin |
| T5-P2-06 | 过期 Token 区分 Expired vs Invalid | 已实现 3 种区分 (AuthService:327-361) | 确认前端消息映射 |
| T5-P2-07 | "记住密码" 自动勾选 "记住用户名" | 当前独立 (LoginViewModel:40-92) | RememberPassword setter 中设置 RememberUsername=true |
| T5-P2-08 | 本地模式简化版状态机 | LocalAuthService 已有基础 (L36-100) | 确认状态转换完整性 |

#### 医案增强 (15 项)

| 任务 | 描述 | 现状 | 方案 |
|------|------|------|------|
| T5-P2-09 | 创建医案时检查患者状态 | CreateFromInputDtoAsync (L70-98) 无患者状态检查 | 在 ValidateAndFetchCreationContextAsync 中添加 `if (patient.Status != Enabled) throw` |
| T5-P2-10 | TcmDiagnosis 非空校验 | ConsultationInputDtoValidator:15-21 已有 `NotEmpty()` | 确认服务端也做校验 |
| T5-P2-11 | 医案编号自动生成 | 本地已实现 (LocalMedicalCaseDataSource:312-323, MC+YYYYMMDD+001) | 服务端实现相同逻辑 |
| T5-P2-12 | HasPrescription=false 时清除处方 | 当前仅检查 NeedsPrescription (L253-258) | SetPrescriptionFlag(false) 时软删除关联处方 |
| T5-P2-13 | 处方编号自动生成 | 无 | 参考医案编号: RX+YYYYMMDD+序号 |
| T5-P2-14 | 处方 Items 为空时验证 | 无 | Validator 添加 `RuleFor(x => x.Items).NotEmpty()` |
| T5-P2-15 | 完成操作验证 Items 非空 | 无 | CompleteAsync 中检查 Prescription.Items.Count > 0 |
| T5-P2-16 | 非当天本人取消需 Reason | 无 | CancelAsync 参数 `string? reason`，非当天/非本人时 required |
| T5-P2-17~20 | 验方导入过滤 (4 项) | 无过滤逻辑 | 导入时检查 ValidationStatus/Status/药材是否禁用/实时获取价格 |
| T5-P2-21~23 | 历史复制过滤 (3 项) | 无过滤逻辑 | 跳过禁用药材/实时获取价格/记录 ReferencedFormulas |

#### 患者增强 (7 项)

| 任务 | 描述 | 方案 |
|------|------|------|
| T5-P2-24 | 身份证号必填+唯一性 | PatientInputDtoValidator 移除 When 条件; Service 查重 |
| T5-P2-25 | 更新时手机号唯一性 | PatientService.UpdateAsync 添加 `_repository.AnyAsync(p => p.PhoneNumber == dto.PhoneNumber && p.Id != id)` |
| T5-P2-26 | 更新时身份证号唯一性 | 同上，查 IdNumber |
| T5-P2-27 | Receptionist 过滤 Disabled 患者 | PatientService.GetPagedAsync 对非 Admin 角色默认 `Status == Enabled` |
| T5-P2-28 | 导入时身份证号唯一性 | BatchImportAsync 中逐条检查 |
| T5-P2-29 | 创建返回 201 替代 200 | PatientsController.Create 返回 `CreatedAtAction(...)` |
| T5-P2-30 | Receptionist 添加 CRU 权限 | CanManagePatient 方法新增 Receptionist 支持 |

#### 药材/验方增强 (4 项)

| 任务 | 描述 | 方案 |
|------|------|------|
| T5-P2-33 | CreateAsync 添加拼音码自动生成 | HerbService.CreateAsync (L80-96) 添加 `entity.PinYinCode = PinYinHelper.GetPinYinCode(entity.Name)` |
| T5-P2-34 | 名称变更时重新生成拼音码 | UpdateAsync 中检查 Name 变更，调用 PinYinHelper (L25-56) |
| T5-P2-35 | Server 端校验 Herbs 列表非空 | FormulaInputDtoValidator 添加 `RuleFor(x => x.Herbs).NotEmpty()` |
| T5-P2-36 | 导出 Excel 包含药材组成详情 | ExportAsync 中 Include Herbs 列表 |

#### 同步增强 (5 项)

| 任务 | 描述 | 现状 | 方案 |
|------|------|------|------|
| T5-P2-39 | SyncMetadataDto 补全 | 4 字段 (EntityId/Checksum/LastModifiedAt/IsDeleted) | 添加 EntityType/ChangedFields/Version |
| T5-P2-40 | GetMetadataAsync 使用 IgnoreQueryFilters | 当前未使用 | 同步需要包含软删除记录 |
| T5-P2-41 | OverwriteConflicts 改为配置项 | 无 | SyncOptions 添加 ConflictResolution 配置 |
| T5-P2-42 | 同步前添加网络/Token 检查 | 未实现 | `await _healthApi.CheckAsync()` + Token 有效期检查 |
| T5-P2-43 | 完善同步结果汇总 | SyncService:71-154 三向比对 | 返回 Added/Updated/Deleted/Conflicted 计数 |

#### 配置增强 (2 项)

| 任务 | 描述 | 现状 | 方案 |
|------|------|------|------|
| T5-P2-44 | FeatureToggle CardReaderEnabled | FeatureToggleOptions (15 开关) 无 CardReader | 添加 `bool CardReaderEnabled { get; set; } = false` |
| T5-P2-45 | JWT SecretKey 验证 | JwtService:37-55 已验证 >=32 字符 | 确认 Production 环境不使用默认密钥 |

---

### 5.2 P3: 细节修复 (21 项) + 架构/文档/PRD (20 项)

#### P3 关键项

| 任务 | 描述 | 方案 |
|------|------|------|
| T5-P3-07 | 审计字段补充 Prescription.Usage | MedicalCaseAuditService:113-203 当前 19 字段，添加 Prescription.Usage |
| T5-P3-08 | pending 端点添加 doctorId 参数 | MedicalCaseController 增加查询参数 |
| T5-P3-09 | 历史复制包含 DosageCount/Discount | CopyFromHistory 方法补全字段 |
| T5-P3-11 | 导入行数限制 off-by-one | PatientImportService 修复边界条件 |
| T5-P3-13 | PatientStatus 复用 CommonStatus | 确认已使用 (PatientModel.cs:106-108) |
| T5-P3-14 | A4/A5 排版差异处理 | PrescriptionPrintService GetPageSize (L228-236) 分支处理 |
| T5-P3-17 | 登出时清除导航历史 | NavigationCoordinator.ClearHistory (L208-212) 在登出事件中调用 |
| T5-P3-20 | Checksum 字段类型/长度对齐 | SyncMetadataDto.Checksum (string, 无长度限制) -> 统一 SHA256 64 字符 |

#### 架构新增 (6 项)

| 任务 | 描述 | 方案 |
|------|------|------|
| A5-01 | Mock 框架统一 (Moq vs NSubstitute) | 选择 NSubstitute (项目已用)，迁移 Moq 用例 |
| A5-02 | MedicalCase 直接引用 Patients+Users 优化评估 | 评估是否需要通过 ICrossModuleService |
| A5-04 | 3 个空壳项目清理 (Consultation/Prescriptions/Server.Interfaces) | 删除空壳，更新引用 |
| A5-05 | [Obsolete] 标记 7 处清理 | 搜索 `[Obsolete]` 确认可安全删除 |
| A5-06 | 外键关系补充 Fluent API | 检查隐式外键，补充显式配置 |
| A5-07 | OpenSpec 标记清理机制 | 建立定期 `grep -c "OpenSpec"` 跟踪 |

---

### 5.3 D2-1: Service 基类继承统一 (3 项) [新增 -- 详见 d2-d5-design]

> 来源: [d2-d5-design-patterns-dependencies.md](2026-02-22-d2-d5-design-patterns-dependencies.md) D2-1 节
> **注意**: FormulaService 已在 S3 A3-07 中处理，此处仅含剩余 3 个 Service。

| 任务 ID | 描述 | 修改文件 |
|---------|------|----------|
| T5-D2-01 | FormulaImportExportService 继承 BaseService\<Formula\> | `FormulaImportExportService.cs` |
| T5-D2-02 | AuthService 继承 BaseService (非泛型) | `AuthService.cs` |
| T5-D2-03 | SyncService 继承 BaseService (非泛型) | `SyncService.cs` |

### 5.4 D2-2: SyncService 返回类型统一 (3 项) [新增 -- 详见 d2-d5-design]

> 来源: [d2-d5-design-patterns-dependencies.md](2026-02-22-d2-d5-design-patterns-dependencies.md) D2-2 节

| 任务 ID | 描述 | 修改文件 |
|---------|------|----------|
| T5-D2-04 | ISyncService 接口返回类型 ServiceResult\<T\> -> Result\<T\> | `ISyncService.cs` |
| T5-D2-05 | SyncService 实现替换工厂方法 (~20 处) | `SyncService.cs` |
| T5-D2-06 | SyncController 适配新返回类型 | `SyncController.cs` |

### 5.5 D5-3: Desktop MedicalCase 跨模块解耦 (6 项) [新增 -- 详见 d2-d5-design]

> 来源: [d2-d5-design-patterns-dependencies.md](2026-02-22-d2-d5-design-patterns-dependencies.md) D5-3 节

| 任务 ID | 描述 | 修改文件 |
|---------|------|----------|
| T5-D5-01 | 创建 IHerbSearchProvider 接口 | `LYBT.Desktop.Contracts/Services/` (新建) |
| T5-D5-02 | 创建 IFormulaSearchProvider 接口 | 同上 |
| T5-D5-03 | Herbs 模块实现 HerbSearchProvider | `LYBT.Desktop.Herbs/Services/` (新建) |
| T5-D5-04 | Formula 模块实现 FormulaSearchProvider | `LYBT.Desktop.Formula/Services/` (新建) |
| T5-D5-05 | MedicalCaseMasterDetailViewModel 改用 IHerbSearchProvider | `MedicalCaseMasterDetailViewModel.cs` |
| T5-D5-06 | FormulaImportDialogViewModel 改用 IFormulaSearchProvider | `FormulaImportDialogViewModel.cs` |

**前置依赖**: 无 (可与 D2-1/D2-2 并行)

---

## 六、跨 Sprint 技术决策

### 6.1 设计原则

| 原则 | 应用 |
|------|------|
| **向后兼容** | Token 撤销、引用检查均为新增逻辑，不破坏现有 API |
| **渐进迁移** | 打印重构分 3 阶段，先新增后迁移再清理 |
| **DRY** | ICrossModuleService 统一跨模块通信 |
| **SOLID-D** | 依赖接口 (ICrossModuleService, IDataSource) 而非具体实现 |

### 6.2 EF Migration 策略

| Sprint | Migration 内容 |
|--------|---------------|
| S1 | 索引条件修改 (A1-01) -- 已确认为仅 Active |
| S2 | MedicalCase 打印字段 + MedicalCasePrintLog 表 + Patient.Status 字段 (已存在) |
| S3 | 无数据库变更 |
| S4 | 无数据库变更 |
| S5 | 外键关系补充 Fluent API |

### 6.3 测试增量目标

| Sprint | 新增测试 | 覆盖重点 |
|--------|---------|---------|
| S1 | +33 | Token 撤销 6 场景 + 引用检查 + 权限矩阵 + EditReason |
| S2 | +80 | 打印回写链 + 字段验证 + 功能 Bug |
| S3 | +50 | 错误码映射 + 异常体系 + 分页筛选 + 架构规则 6 条 |
| S4 | +100 | IDataSource 本地实现 + 打印模板 + Shell |
| S5 | +30 | 功能完善各项 |

### 6.4 关键文件索引

| 功能域 | 核心文件 | 行号范围 |
|--------|---------|---------|
| Token 撤销 | AuthService.cs | L158, L218-273, L301-325, L389-410, L657-692, L728-750 |
| 引用检查 | PatientService.cs:725-749, HerbService.cs:517-546 | |
| 密码处理 | UserService.cs:458, PasswordPolicyValidator.cs:19 | |
| 权限矩阵 | UserService.cs:78-90 | |
| EditReason | MedicalCasePermissionService.cs:155-162, CommandService.cs:144-186 | |
| 打印体系 | PrescriptionPrintService.cs:38-73, PrescriptionModel.cs:66-80 | |
| 打印模板 | PrescriptionPrintTemplate.xaml (268 行), PrintService.cs:26-28 | |
| 错误码 | ErrorCode.cs (384 行, 66 码), ErrorMessages.cs (89 条), ClientErrorMessageMapper.cs (161 条) | |
| 异常体系 | AppException.cs:95, BusinessException.cs:49, NotFoundException.cs:77 | |
| InvalidOp 分布 | CommandService.cs:170/254/257/323/383/459, Helper.cs:120/123/134/142/180 | |
| 分页筛选 | BaseRepository.cs:210-278, HerbService.cs:43-67, MedicalCaseQueryService.cs:49-100 | |
| DataSource | IUserDataSource.cs:9-44, LocalUserDataSource.cs:25-222 (全部实现) | |
| Shell | MenuManager.cs:35-81, NavigationCoordinator.cs:25-26/86-118 | |
| 同步 | SyncMetadataDto.cs:1-28, SyncService.cs:50-154 | |
| 认证 | LocalAuthService.cs:61-94, LoginViewModel.cs:40-92, JwtService.cs:37-55 | |
| 拼音码 | PinYinHelper.cs:25-56 (hyjiacan.pinyin4net) | |
| 审计 | MedicalCaseAuditService.cs:113-203 (19 字段) | |
| FeatureToggle | FeatureToggleOptions.cs:1-34 (15 开关) | |

### 6.5 跨 Sprint 依赖链

```
安全链:  S1-X3 (Token撤销) --> S1-S2 (权限矩阵) --> 完整验证
打印链:  S1-A1 (索引修复) --> S2-X8 (打印重构) --> S4-S5 (打印模板)
错误链:  S3-X1 (错误码注册) --> S3-X4 (Service替换) --> S3-A3 (异常体系切换)
解耦链:  S3-D5-1 (ISP拆分) --> S3-D5-2 (Sync解耦) --> S5-D2-1 (基类统一) --> S5-D2-2 (返回类型)
本地链:  S4-X2 (IDataSource) --> S5-P2 (唯一性校验)
Desktop链: S5-D5-3 (MedicalCase解耦, 独立)
```

### 6.6 可并行执行的分组

| Sprint | 可并行分组 |
|--------|-----------|
| S1 | X7 (引用检查) // S3 (EditReason) // A1 (架构) |
| S2 | X5 (字段验证) // S4 (功能 Bug) -- 与 X8 无依赖 |
| S3 | X6 (分页迁移) // PRD 修订 // 标准固化 -- 与 X1/X4 无依赖 |
| S4 | X2 内部各模块可并行 // S6 (Shell) |
| S5+ | 几乎全部可并行 |

---

> **最后更新**: 2026-02-22
> **文档版本**: v2.1 -- 含 D2/D5 设计深化任务 (S3 +12, S5 +12)
> **状态**: 待用户审阅
