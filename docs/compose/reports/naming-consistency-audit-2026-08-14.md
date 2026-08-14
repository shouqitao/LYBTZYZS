# 命名一致性审计报告

> **日期**: 2026-08-14
> **范围**: Controller / Service / Handler / DTO / Namespace
> **审查对象**: Server (`src/Server/`) + Local (`src/Client/Desktop/LocalWebAPI/`) + Shared (`src/Shared/`)
> **方法论**: 逐文件 grep + 交叉对比 Server/Local 同类 Controller、DTO 字段名、命名空间

---

## 审计摘要

| 类别 | 不一致数 | P0 | P1 | P2 |
|------|---------|----|----|-----|
| Controller 路由 | 9 | 2 | 4 | 3 |
| Controller 方法命名 | 8 | 1 | 3 | 4 |
| DTO 命名/字段 | 7 | 2 | 3 | 2 |
| Namespace 命名 | 6 | 2 | 3 | 1 |
| **合计** | **30** | **7** | **13** | **10** |

---

## 一、Controller 路由一致性

### R-01 [P0] Server IdentityController 路由与 Controller 名称不匹配

| 位置 | 当前命名 | 建议统一命名 |
|------|---------|------------|
| `src/Server/Services/LYBT.WebAPI/Controllers/IdentityController.cs:24` | `[Route("api/v{version:apiVersion}/users")]` | Controller 名改为 `UsersController`，或路由改为 `/identity` |

**影响**: Controller 名为 `Identity` 但路由挂载在 `/users` 下，同文件内还有 `/auth/*` 子路由（Login/Logout/Refresh/AutoValidate），语义混乱。Local 端拆分为 `AuthController` + `UsersController`，Server 端合并在一个文件但路由前缀写错。

---

### R-02 [P0] Server CatalogController 路由与 Controller 名称不匹配

| 位置 | 当前命名 | 建议统一命名 |
|------|---------|------------|
| `src/Server/Services/LYBT.WebAPI/Controllers/CatalogController.cs:27` | `[Route("api/v{version:apiVersion}/herbs")]` | 路由拆为 `/herbs` 和 `/formulas` 两个 Controller，或统一为 `/catalog` |

**影响**: Controller 名为 `Catalog` 但基础路由是 `/herbs`，Formulas 端点使用**硬编码绝对路径** `/api/v{version:apiVersion}/formulas/{...}` 而非相对于 `/herbs` 的子路径。这导致路由声明方式与项目其他 Controller 完全不同。

---

### R-03 [P1] Server Route 声明方式不统一：`[controller]` token vs 硬编码

| 位置 | 当前方式 | 建议统一命名 |
|------|---------|------------|
| `PatientsController.cs:22` | `[controller]` → 解析为 `patients` | ✅ 统一用 `[controller]` |
| `RegistrationsController.cs:19` | `[controller]` → 解析为 `registrations` | ✅ 统一用 `[controller]` |
| `MedicalCasesController.cs:23` | 硬编码 `medicalcases` | 改为 `[controller]` |
| `CatalogController.cs:27` | 硬编码 `herbs` | 改为 `[controller]` 或拆分 |
| `IdentityController.cs:24` | 硬编码 `users` | 改为 `[controller]`（需先改 Controller 名） |
| `ConfigurationController.cs:21` | 硬编码 `configuration` | 改为 `[controller]` |
| `DeployController.cs:18` | 硬编码 `deploy` | 改为 `[controller]` |
| `DiagnosticsController.cs:19` | 硬编码 `diagnostics` | 改为 `[controller]` |
| `HealthController.cs:21` | 硬编码 `health` | 改为 `[controller]` |
| `ReportsController.cs:18` | 硬编码 `reports` | 改为 `[controller]` |

**影响**: 5 个 Server Controller 用硬编码，2 个用 `[controller]` token，风格不统一。Local 端基本统一用 `[controller]`，Server 应对齐。

---

### R-04 [P1] Server MedicalCasesController 端点路径风格不统一：`{id:guid}` vs `{id}`

| 位置 | 当前命名 | 建议统一命名 |
|------|---------|------------|
| `MedicalCasesController.cs:73` | `[HttpGet("{id:guid}")]` | ✅ 保留 `{id:guid}` |
| `MedicalCasesController.cs:269` | `[HttpPut("{id}/status")]` | 改为 `{id:guid}/status` |
| `MedicalCasesController.cs:296` | `[HttpPut("{id}/close")]` | 改为 `{id:guid}/close` |
| `MedicalCasesController.cs:315` | `[HttpPut("{id}/suspend")]` | 改为 `{id:guid}/suspend` |
| `MedicalCasesController.cs:339` | `[HttpPut("{id}/cancel")]` | 改为 `{id:guid}/cancel` |

**影响**: 同一 Controller 内，基础 CRUD 用 `{id:guid}` 约束，但状态操作端点用 `{id}` 无约束，路由约束不一致。

---

### R-05 [P1] Server CatalogController Formula 端点使用硬编码绝对路径

| 位置 | 当前命名 | 建议统一命名 |
|------|---------|------------|
| `CatalogController.cs:476` | `[HttpGet("/api/v{version:apiVersion}/formulas")]` | 拆分为独立的 `FormulasController`，路由 `/api/v{version:apiVersion}/formulas` |
| `CatalogController.cs:510` | `[HttpGet("/api/v{version:apiVersion}/formulas/import-template")]` | 同上 |
| `CatalogController.cs:595` | `[HttpGet("/api/v{version:apiVersion}/formulas/{id}")]` | 同上 |
| 等 12 处 | 所有 formula 端点 | 同上 |

**影响**: 在已声明路由 `/herbs` 的 Controller 上用绝对路径声明 `/formulas/*` 端点，违反 RESTful 路由设计原则。Local 端同样问题（`api/v1/formulas` 硬编码在 `/herbs` Controller 上）。

---

### R-06 [P1] Server vs Local Controller 名称不统一

| 功能 | Server Controller | Local Controller | 建议 |
|------|------------------|-----------------|------|
| 认证 | `IdentityController` (合并 Auth+Users) | `AuthController` + `UsersController` (分离) | Server 保持合并但 Controller 名改为 `UsersController`，或拆分 |

---

### R-07 [P2] DownloadController 路由为空字符串

| 位置 | 当前命名 | 建议统一命名 |
|------|---------|------------|
| `Server/DownloadController.cs:13` | `[Route("")]` | 明确路由前缀如 `[Route("api/v{version:apiVersion}/download")]` |
| `Local/DownloadController.cs:12` | `[Route("")]` | 同上 |

---

### R-08 [P2] Server RegistrationsController 缺少 `start-visit` 端点路由声明

| 位置 | 当前命名 | 建议统一命名 |
|------|---------|------------|
| `Server/RegistrationsController.cs:52` | `[HttpPut("{id:guid}/start-visit")]` | ✅ 正确 |
| `Local/RegistrationsController.cs:30` | 继承 Base 方法但无显式路由 | 应显式声明 `[HttpPut("{id}/start-visit")]` |

---

### R-09 [P2] Local MedicalCasesController 缺少 `batch-details` 端点

| 位置 | 当前命名 | 建议统一命名 |
|------|---------|------------|
| `Server/MedicalCasesController.cs:93` | `[HttpPost("batch-details")]` | Local 应同步添加或在 Server 标记为可选 |

---

## 二、Controller 方法命名一致性

### M-01 [P0] Server IdentityController 方法名带 `Async` 后缀，Local AuthController 不带

| 位置 | 当前命名 | 建议统一命名 |
|------|---------|------------|
| `Server/IdentityController.cs:46` | `LoginAsync()` | 改为 `Login()` |
| `Server/IdentityController.cs:75` | `LogoutAsync()` | 改为 `Logout()` |
| `Server/IdentityController.cs:96` | `RefreshTokenAsync()` | 改为 `RefreshToken()` |
| `Server/IdentityController.cs:118` | `AutoLoginAsync()` | 改为 `AutoLogin()` |
| `Server/IdentityController.cs:132` | `ValidateTokenFromHeaderAsync()` | 改为 `ValidateToken()` |
| `Local/AuthController.cs:32` | `Login()` | ✅ 已正确 |

**影响**: 同一 API 操作（登录/登出）在 Server 和 Local 用不同方法名。C# 惯例中 Controller 公开方法不加 `Async` 后缀（后缀仅用于内部实现），Server 端违反此惯例。

---

### M-02 [P1] Server vs Local ConfigurationController 方法名不统一

| 操作 | Server 方法名 | Local 方法名 | 建议统一命名 |
|------|-------------|-------------|------------|
| 获取所有配置 | `GetConfiguration()` | `GetAll()` | 统一为 `GetAll()` |
| 获取单个配置 | `GetValue()` | `Get()` | 统一为 `Get()` |
| 设置配置 | `SetValue()` | `Set()` | 统一为 `Set()` |
| 验证配置 | `ValidateProduction()` | `Validate()` | 统一为 `Validate()` |

---

### M-03 [P1] Server vs Local HealthController 方法名不统一

| 操作 | Server 方法名 | Local 方法名 | 建议统一命名 |
|------|-------------|-------------|------------|
| 详细健康检查 | `GetDetailedHealth()` | `GetDetails()` | 统一为 `GetDetails()` |

---

### M-04 [P1] Server vs Local MedicalCasesController 方法名不统一

| 操作 | Server 方法名 | Local 方法名 | 建议统一命名 |
|------|-------------|-------------|------------|
| Token 验证 | `ValidateTokenFromHeaderAsync()` | `ValidateToken()` | 统一为 `ValidateToken()` |

---

### M-05 [P2] Server vs Local ConfigurationController 方法命名风格

| 位置 | 当前命名 | 建议统一命名 |
|------|---------|------------|
| `Server/ConfigurationController.cs:149` | `ValidateProduction()` | 统一为 `Validate()` |

---

### M-06 [P2] MedicalCasesController 状态操作方法名风格不一致

| 位置 | 当前命名 | 建议统一命名 |
|------|---------|------------|
| `Local/MedicalCasesController.cs:210` | `CloseCase()` | 统一为 `Close()` 与其他状态操作对齐 |
| `Local/MedicalCasesController.cs:226` | `SuspendCase()` | 统一为 `Suspend()` |
| `Local/MedicalCasesController.cs:244` | `CancelCase()` | 统一为 `Cancel()` |

---

### M-07 [P2] Server RegistrationsController Create 方法返回值不统一

| 位置 | 当前命名 | 建议统一命名 |
|------|---------|------------|
| `Server/RegistrationsController.cs:55` | `CreatedAtAction(...)` → 201 | ✅ RESTful 正确做法 |
| `Local/RegistrationsController.cs:41` | `Success(...)` → 200 in ApiResponse | 应对齐为 201 + CreatedAtAction |

---

### M-08 [P2] Server BaseMedicalCasesController Search 方法在 Local 无对应

| 位置 | 当前命名 | 建议统一命名 |
|------|---------|------------|
| `Server/Modules/.../BaseMedicalCasesController.cs:43` | `Search()` | Local MedicalCasesController 用 `Query()` 方法，名称不同但功能类似，应统一 |

---

## 三、DTO 命名/字段一致性

### D-01 [P0] MedicalCaseListDto/DetailDto: `UserId` + `DoctorName` 混用

| 位置 | 当前命名 | 建议统一命名 |
|------|---------|------------|
| `MedicalCaseListDto.cs:38` | `UserId` (注释: 重命名自 DoctorId) | 统一为 `DoctorId` |
| `MedicalCaseListDto.cs:42` | `DoctorName` | 保持 `DoctorName` |
| `MedicalCaseDetailDto.cs:55` | `UserId` | 统一为 `DoctorId` |
| `MedicalCaseDetailDto.cs:58` | `DoctorName` | 保持 `DoctorName` |

**影响**: 同一 DTO 中 ID 字段叫 `UserId`，Name 字段叫 `DoctorName`，语义不对称。`RegistrationDetailDto` 仍用 `DoctorId`，两端不一致。

---

### D-02 [P0] RegistrationDetailDto 用 `DoctorId`，MedicalCaseDetailDto 用 `UserId`

| 位置 | 当前命名 | 建议统一命名 |
|------|---------|------------|
| `RegistrationDetailDto.cs:27` | `DoctorId` | ✅ 保持 `DoctorId` |
| `MedicalCaseDetailDto.cs:55` | `UserId` | 改为 `DoctorId` |

**影响**: 同一概念（医生标识）在不同 DTO 中字段名不同，增加前后端映射复杂度。

---

### D-03 [P1] DTO 后缀命名不统一：Dto vs Request vs Response

| 后缀 | 示例 | 建议统一命名 |
|------|------|------------|
| `Dto` | `ChangePasswordDto`, `ChangeProfileDto` | 统一为 `Request`（因为它们都是输入 DTO） |
| `Request` | `LoginRequest`, `LogoutRequest`, `ResetPasswordRequest` | ✅ 正确 |
| `InputDto` | `PatientInputDto`, `HerbInputDto`, `BatchDeleteInputDto` | 统一为 `Request` 或保留 `InputDto` |
| `ResultDto` | `PatientBatchImportResultDto`, `ConfigUpdateResultDto` | 统一为 `Response` 或保留 `ResultDto` |
| `ResponseDto` | `ResetPasswordResponseDto` | 统一为 `Response` |

---

### D-04 [P1] Patients/BatchImportResultDto.cs 文件名与类名不匹配

| 位置 | 当前命名 | 建议统一命名 |
|------|---------|------------|
| `Contracts/Patients/BatchImportResultDto.cs` | 文件名: `BatchImportResultDto` | 重命名为 `PatientBatchImportResultDto.cs` |

**影响**: 文件名无 `Patient` 前缀，而类名为 `PatientBatchImportResultDto`。其他 Patients/ 下的文件都有 `Patient` 前缀。

---

### D-05 [P1] BatchDeleteInputDto vs BatchIdsRequest 语义重叠

| 位置 | 当前命名 | 建议统一命名 |
|------|---------|------------|
| `Contracts/Common/BatchDeleteInputDto.cs` | `BatchDeleteInputDto` | 合并或统一命名 |
| `Contracts/Common/BatchIdsRequest.cs` | `BatchIdsRequest` | 合并或统一命名 |

**影响**: 两者都接收 ID 列表，但一个叫 `InputDto`，一个叫 `Request`。`BatchIdsRequest` 用于批量查询，`BatchDeleteInputDto` 用于批量删除。

---

### D-06 [P2] MedicalCaseQueryDto 只在 Local 端使用

| 位置 | 当前命名 | 建议统一命名 |
|------|---------|------------|
| `Contracts/MedicalCase/MedicalCaseQueryDto.cs` | `MedicalCaseQueryDto` | Server 端应同步支持或文档标注为 Local-only |

---

### D-07 [P2] Local MedicalCasesController 的 `query` 端点方法名 `Query()` 与其他 Controller 风格不一致

| 位置 | 当前命名 | 建议统一命名 |
|------|---------|------------|
| `Local/MedicalCasesController.cs:80` | `Query()` | 统一为 `Search()` 或 `GetList()` 与 Server 端对齐 |

---

## 四、Namespace 命名一致性

### N-01 [P0] Server Module 文件夹名 `LYBT.Module.Registration`（单数）vs 命名空间 `LYBT.Module.Registrations`（复数）

| 位置 | 当前命名 | 建议统一命名 |
|------|---------|------------|
| `src/Server/Modules/LYBT.Module.Registration/` (文件夹) | 单数 `Registration` | 重命名为 `LYBT.Module.Registrations` |
| 命名空间（文件内部） | 复数 `LYBT.Module.Registrations` | ✅ 已正确 |

**影响**: 文件夹名与命名空间不一致，IDE 导航和 `dotnet` 工具链期望文件夹与命名空间匹配。Desktop 端已正确用 `LYBT.Desktop.Registrations`（复数）。

---

### N-02 [P0] Server 存在空文件夹 `LYBT.Module.MedicalCase`（单数）

| 位置 | 当前命名 | 建议统一命名 |
|------|---------|------------|
| `src/Server/Modules/LYBT.Module.MedicalCase/` | 孤立文件夹（仅 bin/obj，无 .cs 源文件） | 删除此文件夹 |

**影响**: 与 `LYBT.Module.MedicalCases`（复数，有源代码）并存，是历史遗留。应删除避免混淆。

---

### N-03 [P1] Contracts 命名空间单数 vs 复数不统一

| Contracts 文件夹 | 命名空间 | 单/复数 |
|-----------------|---------|--------|
| `Auth/` | `Contracts.Auth` | 单数 |
| `Common/` | `Contracts.Common` | 单数 |
| `Consultation/` | `Contracts.Consultation` | 单数 |
| `Diagnostics/` | `Contracts.Diagnostics` | **复数** |
| `Formula/` | `Contracts.Formula` | 单数 |
| `Health/` | `Contracts.Health` | 单数 |
| `Herbs/` | `Contracts.Herbs` | **复数** |
| `MedicalCase/` | `Contracts.MedicalCase` | 单数 |
| `Patients/` | `Contracts.Patients` | **复数** |
| `Prescriptions/` | `Contracts.Prescriptions` | **复数** |
| `Registration/` | `Contracts.Registration` | 单数 |
| `Reports/` | `Contracts.Reports` | **复数** |
| `Users/` | `Contracts.Users` | **复数** |

**建议**: 参照 lybtzys-csharp-audit skill，Contracts 命名空间（~2956 引用）不建议大规模重命名（ROI 极低）。但应**新增 Contracts 统一用单数**，保持现有不变。

---

### N-04 [P1] Desktop 模块命名空间 vs Server 模块命名空间不统一

| 功能 | Server 命名空间 | Desktop 命名空间 | 建议 |
|------|----------------|-----------------|------|
| 挂号 | `LYBT.Module.Registration` (文件夹) / `LYBT.Module.Registrations` (代码) | `LYBT.Desktop.Registrations` | Server 文件夹统一为复数 |
| 医案 | `LYBT.Module.MedicalCases` | `LYBT.Desktop.MedicalCase` (单数) | Desktop 统一为复数 |
| 患者 | `LYBT.Module.Patients` | `LYBT.Desktop.Patients` | ✅ 一致 |
| 用户 | `LYBT.Module.Identity` | `LYBT.Desktop.Users` | 命名不同（Identity vs Users），但语义可接受 |
| 药材/验方 | `LYBT.Module.Catalog` | `LYBT.Desktop.Catalog` | ✅ 一致 |

---

### N-05 [P1] Desktop MedicalCase 命名空间单数，Server MedicalCases 复数

| 位置 | 当前命名 | 建议统一命名 |
|------|---------|------------|
| `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/` | 单数 `MedicalCase` | 重命名为 `LYBT.Desktop.MedicalCases`（复数） |

---

### N-06 [P2] Contracts/Health/ 命名空间与文件夹不一致

| 位置 | 当前命名 | 建议统一命名 |
|------|---------|------------|
| `Contracts/Health/` | 命名空间 `LYBT.Shared.Models.Contracts.Health` | 文件夹内无 `Health` DTO，实际文件在 `Common/HealthCheckResponse.cs` 等处。应清理空文件夹或明确归属 |

---

## 五、建议优先级排序

### 立即修复（P0 — 7 项）

| 编号 | 类别 | 问题 | 修复方案 |
|------|------|------|---------|
| R-01 | 路由 | IdentityController 路由 `/users` 与名不匹配 | Controller 改名 `UsersController` 或调整路由 |
| R-02 | 路由 | CatalogController 路由 `/herbs` 与名不匹配 | 拆分为 `HerbsController` + `FormulasController` |
| M-01 | 方法 | Server 方法名带 `Async` 后缀 | 删除 `Async` 后缀 |
| D-01 | DTO | `UserId` + `DoctorName` 混用 | 统一为 `DoctorId` + `DoctorName` |
| D-02 | DTO | MedicalCase 用 `UserId`，Registration 用 `DoctorId` | MedicalCase 统一为 `DoctorId` |
| N-01 | Namespace | 文件夹 `Registration` vs 命名空间 `Registrations` | 重命名文件夹为复数 |
| N-02 | Namespace | 空文件夹 `LYBT.Module.MedicalCase` | 删除 |

### 近期修复（P1 — 13 项）

| 编号 | 类别 | 问题 | 修复方案 |
|------|------|------|---------|
| R-03 | 路由 | Server Route 声明方式不统一 | 统一用 `[controller]` token |
| R-04 | 路由 | `{id:guid}` vs `{id}` 约束不一致 | 统一用 `{id:guid}` |
| R-05 | 路由 | CatalogController Formula 绝对路径 | 拆分 Controller |
| R-06 | 路由 | Server/Local Controller 名称不统一 | 对齐命名 |
| M-02 | 方法 | ConfigurationController 方法名不统一 | Server 方法名对齐 Local |
| M-03 | 方法 | HealthController 方法名不统一 | Server 对齐 Local |
| M-04 | 方法 | MedicalCasesController 方法名不统一 | 对齐命名 |
| D-03 | DTO | Dto/Request/Response 后缀混用 | 统一命名规范 |
| D-04 | DTO | Patients/BatchImportResultDto 文件名 | 重命名文件加前缀 |
| D-05 | DTO | BatchDeleteInputDto vs BatchIdsRequest 语义重叠 | 合并或文档标注 |
| N-03 | Namespace | Contracts 单复数不统一 | 新增统一用单数 |
| N-04 | Namespace | Server/Desktop 模块命名空间不统一 | 对齐复数 |
| N-05 | Namespace | Desktop MedicalCase 单数 vs Server MedicalCases 复数 | 重命名为复数 |

### 远期优化（P2 — 10 项）

| 编号 | 类别 | 问题 | 修复方案 |
|------|------|------|---------|
| R-07 | 路由 | DownloadController 路由为空 | 添加明确路由前缀 |
| R-08 | 路由 | Local RegistrationsController 缺显式路由 | 添加路由声明 |
| R-09 | 路由 | Local MedicalCasesController 缺 batch-details | 同步或标注 |
| M-05 | 方法 | ConfigurationController ValidateProduction | 改为 Validate |
| M-06 | 方法 | CloseCase/SuspendCase/CancelCase 命名 | 改为 Close/Suspend/Cancel |
| M-07 | 方法 | RegistrationsController 返回值不统一 | Local 对齐 201 |
| M-08 | 方法 | Search vs Query 方法名不同 | 统一命名 |
| D-06 | DTO | MedicalCaseQueryDto Local-only | 文档标注 |
| D-07 | DTO | Query() 方法名风格 | 对齐 Search/GetList |
| N-06 | Namespace | Contracts/Health 空文件夹 | 清理 |

---

## 六、已确认一致的模式（无需修改）

以下模式在 Server/Local 间已保持一致，无需调整：

- ✅ CRUD 基础方法名：`GetList` / `GetById` / `Create` / `Update` / `Delete` / `ToggleStatus` / `Restore` / `BatchDelete`
- ✅ 药材/验方 DTO 命名：`HerbDetailDto` / `HerbListDto` / `HerbInputDto` / `FormulaDetailDto` / `FormulaListDto` / `FormulaInputDto` — 统一用 `*Dto` 后缀
- ✅ 患者 DTO 命名：`PatientDetailDto` / `PatientListDto` / `PatientInputDto` — 统一用 `*Dto` 后缀
- ✅ 用户 DTO 命名：`UserDetailDto` / `UserListDto` / `UserInputDto` — 统一用 `*Dto` 后缀
- ✅ 挂号 DTO 命名：`RegistrationDetailDto` / `RegistrationListDto` / `RegistrationInputDto` — 统一用 `*Dto` 后缀
- ✅ auth 请求命名：`LoginRequest` / `LogoutRequest` / `RefreshTokenRequest` / `AutoLoginRequest` — 统一用 `*Request` 后缀
- ✅ 路由复数：patients / registrations / herbs / users / reports — 均为复数
- ✅ 基础 CRUD 路由模式：`{id:guid}` + `batch-*` + `toggle-status` + `restore`

---

## 附录：文件清单

| 类别 | 涉及文件数 |
|------|-----------|
| Server Controllers | 11 |
| Local Controllers | 12 |
| Server Base Controllers | 3 |
| Shared DTOs | 50+ |
| Server Module Namespaces | 45 |
| Desktop Module Namespaces | 65 |
