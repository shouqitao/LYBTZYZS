# LYBTZYZS WebAPI 后端架构深度审查报告

**审查日期**: 2026-08-14  
**审查范围**: src/Server/（Core/Infrastructure/Modules/Services）+ src/Client/Desktop/LocalWebAPI/  
**架构**: ASP.NET Core 8 + MediatR CQRS + EF Core + Modular Monolith

---

## 审查维度总览

| 维度 | 状态 | 关键发现 |
|------|------|----------|
| 1. 模块隔离 | ✅ 通过 | 6 模块无直接引用，跨模块走 IXxxCrossModuleService |
| 2. 依赖方向 | ✅ 通过 | 严格 Services→Modules→Core→Shared，无逆向依赖 |
| 3. CQRS 规范 | ⚠️ 需改进 | 读操作普遍绕过 MediatR 直查 Service |
| 4. EF Core 使用 | 🔴 有违规 | 2 处 P10 违规（Service 直注 DbContext）+ 潜在 N+1 |
| 5. 认证/授权 | ✅ 通过 | 所有 Controller 端点均有策略覆盖 |
| 6. 配置管理 | ✅ 通过 | SensitiveData 属性 + 写入策略 + 生产校验完整 |
| 7. 双模式一致性 | ⚠️ 需改进 | Remote/LocalWebAPI 控制器结构不一致 |

---

## 维度 1：模块隔离（P07/P08）

### 审查结论：✅ 通过

**P07（模块间禁止直接引用）**：6 个 Server 模块（Identity、Catalog、Patients、MedicalCases、Registration、Reports）的 `.csproj` 均只引用 `Core/Infrastructure` 和 `Shared` 项目，**无任何模块间直接 ProjectReference**。

```
各模块引用链：
Module.* → Core/LYBT.Infrastructure → Shared/Entities + Shared.Models
Module.* → Shared/Entities
Module.* → Shared/Models
（无 Module → Module 引用）
```

**P08（跨模块必须用接口）**：已建立 5 个 CrossModuleService 接口，位于 `Infrastructure/Services/CrossModule/`：

| 接口 | 提供方 | 消费方 |
|------|--------|--------|
| `ICatalogCrossModuleService` | Catalog | MedicalCases（药材查询） |
| `IMedicalCaseCrossModuleService` | MedicalCases | Patients（医案引用计数） |
| `IPatientCrossModuleService` | Patients | MedicalCases（患者验证） |
| `IRegistrationCrossModuleService` | Registration | Identity/MedicalCases（挂号状态） |
| `IUserCrossModuleService` | Identity | MedicalCases（用户信息） |

**已解耦的历史痕迹**：Patients 模块 `.csproj` 中有注释掉的 MedicalCases 引用（`<!-- Phase 4: 已通过 IMedicalCaseCrossModuleService 接口解耦 -->`），说明之前存在直接引用但已正确迁移。

**架构测试覆盖**：P07/P08 已有架构测试守卫（`tests/LYBT.Tests.Architecture/`），87+ 条测试基线。

---

## 维度 2：依赖方向

### 审查结论：✅ 通过

依赖方向严格遵循 **Services → Modules → Core → Shared**：

```
LYBT.WebAPI (Services)
  → Module.Identity / Module.Catalog / Module.Patients
  → Module.MedicalCases / Module.Registration / Module.Reports
    → Core/LYBT.Infrastructure (AppDbContext, BaseRepository)
      → Shared/Entities, Shared.Models, Shared.Configuration, Shared.Logging
```

**逆向依赖检查**：未发现任何逆向引用。所有模块只向上引用 Core 和 Shared，不引用其他模块。

**Reports 模块特殊性**：Reports 模块直接使用 `AppDbContext`（来自 Core/Infrastructure）进行跨表报表查询。虽然这违反了"每个模块有自己的 DbContext"的设计原则（ADR-0017），但报表场景需要跨表聚合查询，且 Reports 模块是只读的，属于合理的架构例外。

---

## 维度 3：CQRS 规范

### 审查结论：⚠️ 需改进

**Command 侧（写操作）**：✅ 良好。5 个模块（Identity、Catalog、Patients、MedicalCases、Registration）均使用 MediatR Handler 处理写操作，遵循 `IRequestHandler<TCommand, TResponse>` 模式。

**Query 侧（读操作）**：⚠️ 不一致。各模块读操作实现方式差异较大：

| 模块 | 读操作方式 | 是否走 MediatR |
|------|-----------|---------------|
| Identity | `UserService` 直查 | ❌ 绕过 MediatR |
| Catalog | `ICatalogQueryService` 直查 | ❌ 绕过 MediatR |
| Patients | `PatientService` 直查 | ❌ 绕过 MediatR |
| MedicalCases | `MedicalCaseQueryService` 直查 | ❌ 绕过 MediatR |
| Registration | `IRegistrationService` 直查 | ❌ 绕过 MediatR |
| Reports | `ReportService` 直查 | ❌ 无 MediatR |

**架构说明**：项目采用"Command 走 MediatR，Query 直查 Service"的混合 CQRS 模式（蓝图 §2.2）。这是有意设计——读操作不需要 MediatR 管道（ValidationBehavior）的开销。

**Catalog 模块 MediatR 包引用**：`LYBT.Module.Catalog.csproj` 未直接声明 MediatR 包引用，但通过 `Core/LYBT.Infrastructure` 传递（Infrastructure 引用了 MediatR）。`CatalogModule.cs` 中正确注册了 `services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(...))`。

**MedicalCases 模块特殊情况**：MedicalCases 模块将读写拆分为 `MedicalCaseCommandService`、`MedicalCaseQueryService`、`MedicalCaseStateService` 三个独立 Service（而非 MediatR Handler），属于 Service 层 CQRS 而非 MediatR CQRS。这与蓝图 §2.2 一致，但与其他模块的 MediatR Handler 模式不一致。

---

## 维度 4：EF Core 使用

### 审查结论：🔴 有违规

#### 4.1 P10 违规：Service 层直注 DbContext

**违规 1：Identity 模块 UserService**

- **文件**: `src/Server/Modules/LYBT.Module.Identity/Services/UserService.cs`
- **严重级别**: P1
- **详情**: `UserService` 构造函数注入 `IdentityDbContext _context`，在 `GetUserBasicInfoAsync`、`GetUserByUsernameAsync`、`UpdateLoginFailureAsync`、`ResetLoginFailureAsync` 等方法中直接使用 `_context.Users` 查询。
- **影响**: Service 层绕过 Repository 直接操作 DbContext，违反 P10 架构约束。
- **修复建议**: 将这些查询移入 `IUserRepository`，Service 层只调用 Repository 接口。

**违规 2：Reports 模块 ReportRepository**

- **文件**: `src/Server/Modules/LYBT.Module.Reports/Infrastructure/ReportRepository.cs`
- **严重级别**: P2（可接受的架构例外）
- **详情**: `ReportRepository` 注入 `AppDbContext`（非模块自己的 DbContext），直接查询 MedicalCases、Prescriptions、Registrations 等跨模块表。
- **影响**: 报表需要跨表聚合，使用 AppDbContext 是合理的架构例外（只读查询）。但违反了"每个模块有自己的 DbContext"的原则（ADR-0017）。
- **修复建议**: 如果未来需要更严格的隔离，可考虑将报表查询拆分为多个 Repository（每个对应一个模块的 DbContext），或通过 CrossModuleService 获取数据。

#### 4.2 潜在 N+1 查询风险

**风险 1：MedicalCaseQueryService 的内存过滤**

- **文件**: `src/Server/Modules/LYBT.Module.MedicalCases/Services/MedicalCaseQueryService.cs`（L179）
- **严重级别**: P2
- **详情**: `SearchMedicalCasesAsync` 在 DB 分页后，于内存中按 `CreatedBy` 过滤（`paged.Items.Where(c => c.CreatedBy == operatorId.Value).ToList()`）。这导致：
  - `TotalCount` 可能不准确（DB 返回的总数包含非本人医案）
  - 分页结果可能少于预期
- **影响**: Doctor 用户搜索时，返回的分页数据可能不完整。
- **修复建议**: 将 `CreatedBy` 过滤下推到 DB 查询层（在 `QueryPagedAsync` 中添加参数）。

**风险 2：MedicalCaseQueryService 的多级嵌套映射**

- **文件**: `src/Server/Modules/LYBT.Module.MedicalCases/Services/MedicalCaseQueryService.cs`（L71, L350）
- **严重级别**: P2
- **详情**: `GetListDtoAsync` 和 `SearchMedicalCasesAsync` 中，先 `.ToList()` 将实体加载到内存，再逐条调用 `_mapper.ToListDtos()` / `_mapper.MapToMedicalCaseDetailDto()`。如果映射逻辑包含关联查询（如 Consultation、Prescription），可能产生 N+1。
- **影响**: 列表页加载大量医案时可能有性能问题。
- **修复建议**: 确保 Mapperly 映射器只做属性映射，不触发额外查询；或在 Repository 层使用 `.Include()` 预加载。

**风险 3：Reports 模块的 GetDoctorPerformanceAsync**

- **文件**: `src/Server/Modules/LYBT.Module.Reports/Infrastructure/ReportRepository.cs`（L111-160）
- **严重级别**: P2
- **详情**: `GetDoctorPerformanceAsync` 执行 4 次独立的 DB 查询（consultationCounts、registrationFees、medicineFees、prescriptionCounts），然后在内存中通过 `FirstOrDefault` 匹配。虽然每次查询都做了聚合（GroupBy），但 4 次查询可合并为 1 次。
- **影响**: 报表查询性能可优化。
- **修复建议**: 合并为单次聚合查询，或使用 CTE（Common Table Expression）。

#### 4.3 优点

- **Include 使用得当**：`MedicalCaseRepository` 和 `FormulaRepository` 正确使用 `.Include()` 预加载关联实体（Consultation、Prescription、Herbs），避免了常见的 N+1 问题。
- **Repository 模式良好**：大部分模块通过 Repository 接口访问数据，Service 层不直接操作 DbContext（除上述 2 处违规）。

---

## 维度 5：认证/授权

### 审查结论：✅ 通过

所有 11 个 Remote WebAPI Controller 均有完整的认证/授权策略覆盖：

| Controller | 类级策略 | 方法级豁免/覆盖 | 状态 |
|-----------|---------|---------------|------|
| HealthController | `[Authorize]` | `[AllowAnonymous]` (GET /, ping) | ✅ |
| DownloadController | `[Authorize]` | `[AllowAnonymous]` (GET /) | ✅ |
| IdentityController | `[Authorize]` | `[AllowAnonymous]` (login, refresh, auto-login, validate) | ✅ |
| CatalogController | `[Authorize(DoctorOrAdmin)]` | 方法级 `[Authorize(AdminOrSuperAdmin)]` | ✅ |
| PatientsController | `[Authorize(DoctorOrAdminOrReceptionist)]` | 方法级 `[Authorize(AdminOrSuperAdmin)]` | ✅ |
| MedicalCasesController | `[Authorize(DoctorOrAdmin)]` | 方法级 `[Authorize(DoctorOnly)]` | ✅ |
| RegistrationsController | `[Authorize(DoctorOrAdminOrReceptionist)]` | 方法级 `[Authorize(DoctorOrReceptionist)]` | ✅ |
| ReportsController | `[Authorize(DoctorOrAdmin)]` | — | ✅ |
| ConfigurationController | `[Authorize(SysAdminOnly)]` | — | ✅ |
| DeployController | `[Authorize(SysAdminOnly)]` | — | ✅ |
| DiagnosticsController | `[Authorize(AdminOrSuperAdmin)]` | — | ✅ |

**策略定义**（`PolicyConstants`）：
- `DoctorOrReceptionist`: 0,1
- `DoctorOnly`: 1
- `AdminOrSuperAdmin`: 10,100
- `DoctorOrAdminOrReceptionist`: 0,1,10
- `DoctorOrAdmin`: 1,10
- `SysAdminOnly`: IsSysAdmin=true（独立用户，非角色）
- `AdminBusinessOnly`: 10（排除 SuperAdmin）

**发现的潜在问题**：

- **CatalogController 方法级权限不一致**：类级 `DoctorOrAdmin`，但 `Create`、`Update`、`Delete`、`ToggleStatus`、`BatchEnable`、`BatchDisable` 等写操作使用 `AdminOrSuperAdmin`。这意味着 Doctor 可以读取但不能写入药材/验方。**这是正确的业务设计**（Doctor 只读药材目录，Admin 管理药材），但需要确认产品需求。
- **MedicalCasesController 关闭医案权限**：`Close` 方法使用 `[Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]`（P1-11 2026-08-14），限制只有 Admin/SuperAdmin 可以强制关闭医案。这是合理的权限控制。

---

## 维度 6：配置管理

### 审查结论：✅ 通过

#### 6.1 SensitiveData 处理

**SensitiveData 属性**：`src/Server/Core/LYBT.Infrastructure/Serialization/SensitiveDataJsonConverterFactory.cs` 提供自定义 JSON 转换器，自动屏蔽标记为 `[SensitiveData]` 的属性。

**SensitiveDataMasker**：用于 API 响应中敏感数据的掩码处理。

#### 6.2 配置写入策略

**ConfigurationWritePolicy**（`src/Server/Core/LYBT.Infrastructure/Configuration/Security/ConfigurationWritePolicy.cs`）：
- 精确禁止的配置键：`ConnectionStrings:DefaultConnection`、`Jwt:SecretKey`、`DefaultPasswords` 节
- 白名单节：只允许特定业务配置节（如 `DesktopUpdate`、`Serilog` 等）
- 启发式检测：键名包含 `Key/Secret/Password/ConnectionString/Token` 的自动禁止

#### 6.3 生产环境校验

**ProductionConfigurationValidator**（`src/Server/Core/LYBT.Infrastructure/Configuration/Validation/ProductionConfigurationValidator.cs`）：
- 强制检查 `ConnectionStrings:DefaultConnection`（环境变量 `ConnectionStrings__DefaultConnection`）
- 强制检查 `Jwt:SecretKey`（环境变量 `Jwt__SecretKey`）
- 强制检查 `DefaultPasswords:SysAdminPassword`、`DefaultPasswords:NewUserPassword`
- 启动时验证，缺失则阻止启动

#### 6.4 配置优先级

Remote WebAPI 使用 `.env` 文件 + 环境变量 + `appsettings.*.json` 的标准 ASP.NET Core 配置优先级。LocalWebAPI 使用 `WebApplication.CreateBuilder` + 硬编码 `UseEnvironment("Development")`。

---

## 维度 7：双模式一致性

### 审查结论：⚠️ 需改进

#### 7.1 控制器结构差异

| 功能 | Remote WebAPI | LocalWebAPI | 差异 |
|------|-------------|-------------|------|
| 认证 | `IdentityController`（合并 Auth + Users） | `AuthController` + `UsersController`（分离） | 结构不同 |
| 药材/验方 | `CatalogController` | `CatalogController` | ✅ 一致 |
| 患者 | `PatientsController` | `PatientsController` | ✅ 一致 |
| 医案 | `MedicalCasesController` | `MedicalCasesController` | ✅ 一致 |
| 挂号 | `RegistrationsController` | `RegistrationsController` | ✅ 一致 |
| 报表 | `ReportsController` | `ReportsController` | ✅ 一致 |
| 配置 | `ConfigurationController` | `ConfigurationController` | ✅ 一致 |
| 健康检查 | `HealthController` | `HealthController` | ✅ 一致 |
| 诊断 | `DiagnosticsController` | `DiagnosticsController` | ✅ 一致 |
| 部署 | `DeployController` | `DeployController` | ✅ 一致 |
| 下载 | `DownloadController` | 无 | ⚠️ 缺失 |

**关键差异**：
- **Remote IdentityController** 是合并后的控制器（路由 `/api/v1/users` + `/api/v1/auth/*`），**LocalWebAPI** 保留了分离的 `AuthController`（路由 `/api/v1/auth`）和 `UsersController`（路由 `/api/v1/users`）。功能上等价，但结构不一致。
- **LocalWebAPI 缺少 DownloadController**（下载页功能仅在 Remote 端有意义，本地模式不需要）。
- **LocalWebAPI 缺少 DiagnosticsController 的部分功能**（日志级别管理在本地模式下通过 `LoggingLevelManager` 实现）。

#### 7.2 服务层一致性

✅ **服务层统一**：LocalWebAPI 的所有 Controller 均使用与 Remote WebAPI 相同的 Service 接口（`IUserService`、`ICatalogQueryService`、`IPatientService`、`IMedicalCaseCommandService` 等），通过 ADR-0010 的跨层引用例外实现。这是正确的架构决策。

#### 7.3 JWT 配置差异

| 配置项 | Remote WebAPI | LocalWebAPI |
|--------|-------------|-------------|
| Token 有效期 | 配置驱动（通常 30 分钟） | 1 年（硬编码） |
| Refresh Token | 支持 | 简化（LocalRefreshTokenCommand） |
| 密码哈希 | Identity UserManager (PBKDF2) | 同上（共享 LoginCommandHandler） |
| Rate Limiting | `"Login"` | `"LocalLogin"` |
| 环境 | Production/Test/Development | 固定 Development |

这是合理的设计差异——本地模式面向单用户桌面场景，不需要严格的 Token 过期和 Rate Limiting。

#### 7.4 依赖图差异

LocalWebAPI 的 `.csproj` 引用了 6 个 Server Module（通过 ADR-0010 豁免），这是 Client → Server 的唯一跨层引用路径。架构测试 `P21` 已跳过（有意设计）。

---

## 发现汇总

### P0（必须修复）
无

### P1（应该修复）

| # | 维度 | 问题 | 文件 | 说明 |
|---|------|------|------|------|
| 1 | EF Core | P10 违规：UserService 直注 IdentityDbContext | `Modules/LYBT.Module.Identity/Services/UserService.cs` | Service 层绕过 Repository 直接操作 DbContext，违反架构约束 |
| 2 | CQRS | MedicalCaseQueryService 内存过滤导致 TotalCount 不准 | `Modules/LYBT.Module.MedicalCases/Services/MedicalCaseQueryService.cs:179` | Doctor 搜索医案时，分页总数可能偏大 |

### P2（建议改进）

| # | 维度 | 问题 | 文件 | 说明 |
|---|------|------|------|------|
| 1 | EF Core | Reports 模块使用 AppDbContext（架构例外） | `Modules/LYBT.Module.Reports/Infrastructure/ReportRepository.cs` | 跨表报表查询使用主库 DbContext，可接受但建议监控 |
| 2 | EF Core | GetDoctorPerformanceAsync 4 次独立查询可合并 | `Modules/LYBT.Module.Reports/Infrastructure/ReportRepository.cs:111` | 报表性能可优化 |
| 3 | 双模式 | Remote/LocalWebAPI 控制器结构不一致 | `Services/LYBT.WebAPI/Controllers/IdentityController.cs` vs `Client/Desktop/LocalWebAPI/Controllers/AuthController.cs` | 功能等价但结构不同，增加维护成本 |
| 4 | CQRS | Reports 模块无 MediatR，纯 Service→Repository 模式 | `Modules/LYBT.Module.Reports/` | 与其他模块的 CQRS 模式不一致 |

---

## 修复建议

### P1-1：UserService DbContext 注入

**方案 A（推荐）**：将 `_context.Users` 查询移入 `IUserRepository`，新增 `GetUserBasicInfoAsync`、`GetUserByUsernameAsync`、`UpdateLoginFailureAsync`、`ResetLoginFailureAsync` 方法。Service 层只调用 Repository 接口。

**方案 B（快速修复）**：将 `IdentityDbContext` 替换为 `IUserRepository`，在 Repository 中封装这些查询。

### P1-2：MedicalCaseQueryService 内存过滤

**修复**：在 `IMedicalCaseRepository.QueryPagedAsync` 中添加 `doctorId` 参数，将 `CreatedBy` 过滤下推到 DB 层。同时更新 `TotalCount` 逻辑。

---

## 附录：架构测试覆盖

| 测试 ID | 规则 | 状态 |
|---------|------|------|
| P07 | 模块间禁止直接引用 | ✅ 通过 |
| P08 | 跨模块必须用接口 | ✅ 通过 |
| P10 | Service 禁注入 AppDbContext | ⚠️ Identity UserService 违规 |
| P21 | LocalWebAPI Server Module 引用匹配 ADR-0010 | ✅ SKIPPED（有意设计） |
| DP10 | VM 禁注入 IApiClient 子接口 | ✅ 通过 |
| DP-M1 | DTO 逃逸检查 | ✅ 通过 |

---

*报告生成时间: 2026-08-14*  
*审查工具: Hermes Agent (manual code review + static analysis)*  
*下次审查建议: 修复 P1 问题后重新审查维度 3 和 4*
