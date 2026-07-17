# LYBT.LocalWebAPI

> 内嵌 ASP.NET Core WebAPI：桌面客户端本地/离线模式的后端，复用远程 WebAPI 的 Service/Repository 层。

## 项目定位

运行在 Desktop 客户端进程内的 Kestrel WebAPI（端口 5300），通过 `SwitchingApiClient` 路由切换实现本地模式。与远程 WebAPI 共享同一套 Service/Repository 层（ADR-0010 统一服务层架构），Controller 委托 `I*Service` 接口处理业务逻辑，零平行实现。是 Client → Server 唯一的跨层引用路径。

## 目录结构

```
LYBT.LocalWebAPI/
├── Auth/
│   └── LocalJwtConfig.cs           # 简化 JWT 配置（HMAC-SHA256，365天过期）
├── Controllers/
│   ├── AuthController.cs           # 登录/登出/刷新/自动登录/验证
│   ├── HealthController.cs         # /ping + /details 健康检查
│   ├── UsersController.cs          # 继承 BaseUsersController
│   ├── PatientsController.cs       # 11 个端点（CRUD + 导入导出）
│   ├── HerbsController.cs          # 11 个端点（CRUD + 批量 + 引用检查）
│   ├── FormulasController.cs       # 12 个端点（CRUD + 克隆 + 验证）
│   ├── MedicalCasesController.cs   # 22 个端点（Facade 模式）
│   ├── RegistrationsController.cs  # 7 个端点（CRUD + 队列 + 快速就诊）
│   ├── ReportsController.cs        # 3 个端点（报表统计）
│   ├── DiagnosticsController.cs    # 7 个端点（日志级别管理）
│   └── ConfigurationController.cs  # 键值对配置（内存存储）
├── Data/
│   └── LocalWebApiSeedData.cs      # 示例种子数据（Herb/Formula/Patient）
├── Handlers/                       # MediatR CQRS Handlers（Auth + Diagnostics）
├── Mappers/
│   └── LocalApiMapper.cs           # 手动映射器（Entity → ListDto/DetailDto）
├── Repositories/
│   ├── HttpHerbRepository.cs       # IApiClient 药材仓库
│   ├── HttpFormulaRepository.cs    # IApiClient 验方仓库
│   ├── HttpPatientRepository.cs    # IApiClient 患者仓库
│   ├── HttpUserRepository.cs       # IApiClient 用户仓库
│   ├── HttpMedicalCaseRepository.cs# IApiClient 医案仓库
│   └── HttpRegistrationRepository.cs# IApiClient 挂号仓库
├── LocalWebApiProgram.cs           # 入口：Builder/Builder/InitDB/Run
└── Program.cs                      # ASP.NET Core 启动入口
```

## 核心组件

| 类 | 设计依据 |
|---|---|
| **LocalWebApiProgram** (static) — 入口类 | 四步生命周期：`CreateBuilder` → `CreateApplication` → `InitializeDatabaseAsync` → `RunAsync` |

| 方法 | 说明 |
|------|------|
| `CreateBuilder(args?)` | 创建 `WebApplicationBuilder` |
| `CreateApplication(builder, connectionString)` | 注册 DbContext + Identity + 8 个 Server Module + MediatR + RateLimiter + JWT |
| `InitializeDatabaseAsync(app)` | `EnsureCreatedAsync` + `IdentitySeedData` + `LocalWebApiSeedData` |
| `RunAsync(args?, connectionString)` | 串联上述三步并启动 Kestrel |

| 类 | 设计依据 |
|---|---|
| **LocalJwtConfig** (static) — JWT 配置 | HMAC-SHA256 签名，365天过期，4 个授权策略 |

| 策略 | 角色 |
|------|------|
| `AdminOnly` | SuperAdmin, Admin |
| `DoctorOrAdmin` | SuperAdmin, Admin, Doctor |
| `AdminOrSuperAdmin` | Admin, SuperAdmin |
| `DoctorOrReceptionist` | Doctor, Receptionist |

| JWT Claim | 来源 |
|-----------|------|
| `NameIdentifier` / `Sub` | `user.Id` |
| `Name` | `user.UserName` |
| `Role` | Identity 角色名（取第一个） |
| `IsSysAdmin` | `user.IsSysAdmin == true` 时添加 |

| 类 | 设计依据 |
|---|---|
| **LocalWebApiSeedData** (static) — 种子数据 | 条件插入：Herb(Ginseng) + Formula(Sample) + Patient(Sample)，用户由 IdentitySeedData 创建 |

| 控制器 | 注入服务 | 端点数 | 说明 |
|--------|----------|--------|------|
| **AuthController** | IAuthService + IAutoLoginService | ~5 | 登录/登出/刷新/自动登录/Token验证 |
| **HealthController** | 无（直接 DbContext） | 2 | `/ping` + `/details`（DB 连通性） |
| **UsersController** | IUserService | 继承 | 继承 BaseUsersController |
| **PatientsController** | IPatientService + IPatientImportExportService | 11 | CRUD + 导入导出 |
| **HerbsController** | IHerbService | 11 | CRUD + 批量操作 + 引用检查 |
| **FormulasController** | IFormulaService + IFormulaImportExportService | 12 | CRUD + 克隆 + 验证 |
| **MedicalCasesController** | IMedicalCaseFacade + IPermissionService | 22 | Facade 模式，最多端点 |
| **RegistrationsController** | IRegistrationService | 7 | CRUD + 队列管理 + 快速就诊 |
| **ReportsController** | IReportsService | 3 | 报表统计 |
| **DiagnosticsController** | LoggingLevelManager | 7 | 日志级别运行时管理 |
| **ConfigurationController** | 无（内存存储） | — | 键值对配置 |

| 仓库 | 说明 |
|------|------|
| **Http\*Repository** (6个) | 实现 Desktop 端 `I*Repository` 接口，内部调用 `IApiClient` 转发到 LocalWebAPI Controller |
| **LocalApiMapper** (static) | 手动扩展方法：Entity → ListDto/DetailDto（Patient/User/Registration/MedicalCase） |

## 依赖关系

```
LYBT.LocalWebAPI
├── Server/Core/LYBT.Infrastructure    (AppDbContext, BaseRepository, BaseApiController)
├── Server/Core/LYBT.Entities          (领域实体)
├── Server/Modules/LYBT.Module.Auth        (IAuthService)
├── Server/Modules/LYBT.Module.Users       (IUserService, IdentitySeedData)
├── Server/Modules/LYBT.Module.Patients    (IPatientService)
├── Server/Modules/LYBT.Module.Herbs       (IHerbService)
├── Server/Modules/LYBT.Module.Formulas    (IFormulaService)
├── Server/Modules/LYBT.Module.MedicalCases(IMedicalCaseFacade)
├── Server/Modules/LYBT.Module.Registration(IRegistrationService)
├── Server/Modules/LYBT.Module.Reports     (IReportsService)
├── LYBT.Shared.Models                 (DTOs/Contracts)
├── LYBT.Shared.Logging                (LoggingLevelManager)
├── MediatR                            (CQRS Handlers)
└── Microsoft.AspNetCore.Authentication.JwtBearer
```

## 设计决策

1. **统一服务层 (ADR-0010)** — LocalWebAPI 复用远程 WebAPI 的 Service/Repository 层，Controller 委托 `I*Service`，零平行实现。这是 Client → Server 唯一的跨层引用路径，有意设计。
2. **简化 JWT** — 本地模式使用 HMAC-SHA256 + 365天过期，无 Token 刷新机制（`LocalJwtConfig`），比远程 JWT 策略更宽松。
3. **Identity 统一** — 使用 `AppDbContext` + ASP.NET Core Identity，密码哈希由 Identity 管理（BCrypt/PBKDF2），禁止在 SeedData 中直接创建用户。
4. **RateLimiter** — 登录端点限流：固定窗口 5次/分钟，无队列。
5. **条件种子数据** — `LocalWebApiSeedData.SeedAsync` 仅在表为空时插入，幂等安全。
6. **Http\*Repository** — Desktop 端仓库实现，通过 `IApiClient`（SwitchingApiClient）透明路由到本地或远程 API。

## 已知陷阱

- **不可直接注入 DbContext** — 除 AuthController（本地 JWT）、HealthController（连通性）、DiagnosticsController（日志查询）外，所有 Controller 必须通过 Service 层。
- **LocalWebApiDbContext 已删除** — 统一使用 `AppDbContext`，任何残留引用会编译失败。
- **Architecture Test P21 已跳过** — `P21_LocalWebAPI_ServerModule_References_Match_ADR0010` 因统一架构有意跳过。
- **密码哈希不兼容** — BCrypt（PasswordHelper）vs PBKDF2（Identity）不兼容，所有用户创建必须走 `UserManager`。
- **端口 5300** — 本地模式固定端口，与远程 5000 区分，`SwitchingApiClient` 据此路由。
- **AddIdentity 必在 AddAuthentication 前** — `LocalWebApiProgram.CreateApplication` 中注册顺序不可调换。
