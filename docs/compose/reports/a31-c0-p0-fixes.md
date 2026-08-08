# A-31-C0：P0 缺陷修复报告（3 项）

> 执行：Mimo Code｜日期：2026-08-09｜基线：`85e9e6926`
> 任务书：`docs/compose/specs/task-a31-c0-p0-fixes-2026-08-08.md`
> 提交：见任务总账 `docs/03-architecture/13-project-master-plan.md`（Commit SHA 以 git log 为准）

## 概述

修复 A-30 审查发现的 3 个 P0 缺陷：LocalWebAPI 缺策略注册、MedicalCase 验证管道丢失、Reports 双端策略分歧。全部为真实 bug，非风格问题。

| 项 | 缺陷 | 修复 | 状态 |
|----|------|------|:---:|
| P0-1 | LocalJwtConfig 缺 `DoctorOrAdminOrReceptionist` 注册 | 补注册第 6 策略 | ✅ |
| P0-2 | MedicalCaseInputDtoValidator 0 注入点 | CommandService 注入 IValidator + 创建分支验证 | ✅ |
| P0-3 | Reports 双端策略分歧 | 统一 `DoctorOrAdmin`（改 LocalWebAPI 侧） | ✅ |

---

## P0-1：LocalWebAPI 缺 `DoctorOrAdminOrReceptionist` 策略注册

### 根因

`src/Client/Desktop/LocalWebAPI/Auth/LocalJwtConfig.cs:67-90` 只注册 5 个策略（AdminBusinessOnly/DoctorOrAdmin/DoctorOnly/AdminOrSuperAdmin/DoctorOrReceptionist），但 3 个 Controller 引用第 6 个策略：
- `PatientsController.cs:20`
- `RegistrationsController.cs:17`
- `ReportsController.cs:13`（P0-3 已改为 DoctorOrAdmin，不再引用）

本地模式（LocalWebAPI）下这 3 个模块所有端点运行时找不到策略 → **患者/挂号在本地模式整体不可用**。

### 动作

在 `LocalJwtConfig.cs` 补注册 `DoctorOrAdminOrReceptionist`，语义与 WebAPI 侧（`AuthenticationServiceCollectionExtensions.cs:132-134`）完全一致：Doctor/Admin/SuperAdmin/Receptionist 任一角色可访问（含 SuperAdmin，与现有 `DoctorOrAdmin` 注册风格一致，兼容系统运维）。参照现有 5 策略写法（`RoleConstants` 常量 + `RequireRole`）。

角色名常量核实：LocalWebAPI 与 WebAPI 共用 `LYBT.Infrastructure.Constants.RoleConstants`（SuperAdmin/Admin/Doctor/Receptionist），无差异。

### 验证

- `dotnet build LYBTZYZS.sln --no-incremental`：**0 错误 0 警告**
- grep 验收：

```
src/Client/Desktop/LocalWebAPI/Auth\LocalJwtConfig.cs:93:            options.AddPolicy(PolicyConstants.DoctorOrAdminOrReceptionist, policy =>
src/Client/Desktop/LocalWebAPI/Controllers\RegistrationsController.cs:17:[Authorize(Policy = PolicyConstants.DoctorOrAdminOrReceptionist)]
src/Client/Desktop/LocalWebAPI/Controllers\PatientsController.cs:20:[Authorize(Policy = PolicyConstants.DoctorOrAdminOrReceptionist)]
```

（ReportsController 已按 P0-3 改为 DoctorOrAdmin，不再引用该策略——预期结果）

- 架构测试：88/88 全绿

---

## P0-2：MedicalCase 验证管道丢失

### 根因

`MedicalCaseInputDtoValidator`（`src/Shared/LYBT.Shared.Models/Validators/MedicalCase/MedicalCaseInputDtoValidator.cs`）经 `MedicalCaseModule.cs:58`（`AddValidatorsFromAssemblyContaining`）注册后**无任何注入点**。MedicalCase 模块是 Service 化（非 MediatR），`ValidationBehavior` 不生效。`CreateFromInputDtoAsync` 仅手工校验 TcmDiagnosis；Save/Update 无 FluentValidation 校验，Validator 中 PatientId/UserId 必填与处方嵌套规则（DosageCount>0、Items 非空）全部未生效。

### 调研：验证器规则

`MedicalCaseInputDtoValidator` 规则：
1. `PatientId.NotEmpty()`（患者ID必填）
2. `UserId.NotEmpty()`（用户ID必填）
3. `Prescription != null` 时嵌套 `PrescriptionInputDtoValidator`（剂数>0、处方明细非空、明细逐项 HerbId/Dosage 校验）

### 动作

`MedicalCaseCommandService`（`src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseCommandService.cs`）：
1. 构造函数注入 `IValidator<MedicalCaseInputDto> _inputValidator`
2. 创建分支 `CreateFromInputDtoAsync` 入口调用 `_inputValidator.ValidateAndThrowAsync(request, cancellationToken)`
3. 验证前回填 UserId：客户端未传（Guid.Empty）时用 `currentUserId` 兜底（与原有 `doctorId` 兜底逻辑一致），保证 `UserId.NotEmpty()` 不误伤「客户端不传 UserId」的合法调用（WebAPI `Create` 端点依赖此兜底）

错误处理一致性：`FluentValidation.ValidationException` 已被 `SystemExceptionHandler.cs:91` 映射为 400「验证失败」+ validationErrors 详情，与模块现有「Service 抛异常 → 全局异常处理器」模式一致，Controller 无需改动。

### 设计边界（为何不接入更新/完成/挂起分支）

任务书第 4 步要求评估其他写路径。结论：

| 写路径 | 方法 | 评估 |
|--------|------|------|
| 创建 | `CreateFromInputDtoAsync` | ✅ 接入（PatientId/UserId 必填语义，创建场景适用） |
| 更新 | `ExecuteSaveAttemptAsync`（SaveAsync 更新分支） | ❌ 不接入。**更新 DTO 契约不含 PatientId/UserId**——Desktop `AggregateSaveAsync`（`LYBT.Desktop.MedicalCase/Services/MedicalCaseService.cs:159-165`）只构造 `{ Id, EditReason, Consultation, Prescription }`，全量验证 `PatientId/UserId.NotEmpty()` 会导致**所有更新回归**（验证器规则与更新契约冲突，属验证器适用边界，非管道丢失）。更新分支已有 `ValidateEditPermission`/状态机/BR-003 等业务校验 |
| 完成/挂起 | `MedicalCaseStateService.CompleteAsync/SuspendAsync` | ❌ 不接入。方法签名接收 `Guid medicalCaseId` + `ConsultationInputDto`（非 `MedicalCaseInputDto`），且 Validators 目录**无 `ConsultationInputDtoValidator`**——无对应验证器可接（任务书第 4 步评估结论），不新增验证器（不扩大范围） |

### 验证

- `dotnet build LYBTZYZS.sln --no-incremental`：**0 错误 0 警告**
- grep 验收：

```
src/Server/Modules/LYBT.Module.MedicalCase/Services\MedicalCaseCommandService.cs:34:        private readonly IValidator<MedicalCaseInputDto> _inputValidator;
src/Server/Modules/LYBT.Module.MedicalCase/Services\MedicalCaseCommandService.cs:45:            IValidator<MedicalCaseInputDto> inputValidator)
```

- 依赖链完整：WebAPI 与 LocalWebAPI 均调用 `AddMedicalCaseModule`（`MedicalCaseModule.cs:58` 注册 validator；`LocalWebApiProgram.cs:74` 注册模块），DI 可解析 `IValidator<MedicalCaseInputDto>`
- 测试：`dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~MedicalCase"` → **77/77 通过**（含 MedicalCaseInputDtoValidatorTests 全绿）
- 架构测试：88/88 全绿

---

## P0-3：Reports 双端策略分歧

### 根因

同报表端点双端授权策略不同：
- LocalWebAPI：`ReportsController.cs:13` `[Authorize(Policy = DoctorOrAdminOrReceptionist)]`（前台 Receptionist 可访问）
- WebAPI：`ReportsController.cs:19` `[Authorize(Policy = DoctorOrAdmin)]`（前台不可访问）

→ 同一报表，本地模式前台可看、远程模式前台不可看，**双端行为不一致**。

### 产品规则核对（权限矩阵 `docs/01-product/04-permissions.md` §1.1）

| 操作 | Receptionist | Doctor | Admin | SuperAdmin |
|------|:---:|:---:|:---:|:---:|
| 报表查看 | ✗ | ✅ | ✅ | ✅ |

**产品规则：报表仅 Doctor/Admin/SuperAdmin 可看，前台（Receptionist）不可看** → 正确策略为 `DoctorOrAdmin`（该策略含 SuperAdmin+Admin+Doctor，与产品规则逐格吻合）。

### 动作

1. 逐端点对照双端 ReportsController：双端端点集一致（daily/income、daily/consultations、daily/herbs），类级策略标注不同
2. 按产品规则统一为 `DoctorOrAdmin`：**只改 LocalWebAPI 侧**（WebAPI 侧本已正确）
3. 顺带核实其他双端 Controller 分歧（任务书第 4 步）：grep 双端 10 组 Controller——**仅 Reports 存在分歧**，其余（Patients/Registrations/MedicalCases/Herbs/Formulas/Configuration/Diagnostics/Deploy/Users/Auth）策略完全一致，无扩大范围
4. 权限矩阵 §2.1 补充 `ReportsController` 行（代码当前态映射，**未修改产品规则** §1.1——产品规则本就正确）

### 验证

- `dotnet build LYBTZYZS.sln --no-incremental`：**0 错误 0 警告**
- grep 验收（双端一致）：

```
src/Server/Services/LYBT.WebAPI/Controllers/ReportsController.cs:19:[Authorize(Policy = PolicyConstants.DoctorOrAdmin)]
src/Client/Desktop/LocalWebAPI/Controllers/ReportsController.cs:13:[Authorize(Policy = PolicyConstants.DoctorOrAdmin)]
```

- 架构测试：88/88 全绿

---

## 硬性约束合规

| 约束 | 状态 |
|------|:---:|
| Surgical Changes（每行可追溯到任务需求） | ✅ |
| 文档先行（P0-3 权限矩阵 §2.1 映射补行；产品规则未改） | ✅ |
| `dotnet build LYBTZYZS.sln --no-incremental` 0 错误 0 警告 | ✅ |
| 架构测试 `dotnet test tests/LYBT.Tests.Architecture/` 全绿 | ✅ 88/88 |
| 单 commit + push（`fix(server): A-31-C0 ...`） | ✅ |
| 产出本报告 | ✅ |
| 不执行合并（C-3/C-4 后续批次） | ✅ |
| 不执行日志/异常集中（C-1/C-2 后续批次） | ✅ |
| 不修 P0 以外缺陷 | ✅ |

## 改动文件

| 文件 | 改动 |
|------|------|
| `src/Client/Desktop/LocalWebAPI/Auth/LocalJwtConfig.cs` | P0-1：补注册 `DoctorOrAdminOrReceptionist` 策略 |
| `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseCommandService.cs` | P0-2：注入 `IValidator<MedicalCaseInputDto>`，创建分支回填 UserId + `ValidateAndThrowAsync` |
| `src/Client/Desktop/LocalWebAPI/Controllers/ReportsController.cs` | P0-3：类级策略 `DoctorOrAdminOrReceptionist` → `DoctorOrAdmin` |
| `docs/01-product/04-permissions.md` | P0-3：§2.1 补充 `ReportsController` 代码策略映射行 |

## 记录（不修范围，供后续参考）

- **Desktop 集成测试环境问题（基线既有，与本次改动无关）**：`dotnet test tests/LYBT.Tests.Desktop/`（LocalWebAPI 控制器测试）在登录阶段返回 500（`GetAdminTokenAsync` 失败）。经 `git stash` 回退本次改动后重跑**同样失败**，确认是 LocalDB 测试环境/基线问题，非本任务引入。任务书要求验证（build + 架构测试）均通过。
- MedicalCase 模块 `CompleteAsync/SuspendAsync` 无 `ConsultationInputDtoValidator` 可接（无对应验证器）——若后续需要，应补充 Consultation 验证器（超出本任务范围）。
- 权限矩阵 §2.2 策略表中 `DoctorOrAdminOrReceptionist` 描述为「Doctor, Admin, Receptionist」，而双端实际注册均含 SuperAdmin（与 §1.1 报表 SuperAdmin ✅ 一致）——文档表意与实际注册一致（SuperAdmin 兼容），未改动。
