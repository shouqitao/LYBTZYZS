# Auth + Users 模块合并前深度调研报告

> **任务**：docs/compose/specs/auth-users-merge-research-2026-08-09.md（只读调研，未修改任何 src/tests 代码）
> **日期**：2026-08-09
> **工具**：serena MCP（符号/引用精确分析）、grep/glob、子代理并行调研（测试覆盖 + Desktop/LocalWebAPI 双轨）
> **行号约定**：`文件:行号` 全部为 1-based（经 grep/read 复核；serena 0-based 输出已统一换算）
> **范围**：src/Server/Modules/LYBT.Module.Auth、LYBT.Module.Users、src/Server/Core/LYBT.Infrastructure（跨模块接口）、src/Shared/LYBT.Entities、src/Shared/LYBT.Shared.Models/Contracts、双端 Controller 与 Desktop API Client、tests/、docs/

---

## 1. Auth 模块全貌（LYBT.Module.Auth）

### 1.1 文件清单（源码，24 个文件）

| 层 | 文件 |
|---|---|
| 模块注册 | `AuthModule.cs` |
| Interfaces (4) | `Interfaces/IJwtService.cs`、`IAuthSessionRepository.cs`、`ISecurityAuditRepository.cs`、`ISecurityAuditService.cs` |
| Infrastructure (3) | `Infrastructure/AuthDbContext.cs`、`AuthSessionRepository.cs`、`SecurityAuditRepository.cs` |
| Services (3) | `Services/JwtService.cs`、`SecurityAuditService.cs`、`AuthCrossModuleService.cs` |
| Application/Commands (10) | `LoginCommand(.cs|Handler)`、`AutoLoginCommand(.cs|Handler)`、`LogoutCommand(.cs|Handler)`、`RefreshTokenCommand(.cs|Handler)`、`RevokeAllUserTokensCommand(.cs|Handler)` |
| Application/Queries (3) | `ValidateTokenQuery.cs`、`ValidateTokenQueryHandler.cs`、`ValidateTokenResult.cs` |
| Application/Mappers (1) | `Mappers/AuthUserMapper.cs` |
| 文档 | `AGENTS.md`、`README.md`（README 含大量过期描述，见 §5.2） |

`Models/` 目录为空。**无独立 Domain 实体文件**——实体位于共享库 `src/Shared/LYBT.Entities/Auth/`（AuthSession、SecurityAuditLog）与 `Common/SystemLog.cs`。

### 1.2 实体关系

**AuthSession**（`src/Shared/LYBT.Entities/Auth/AuthSessionModel.cs`）
- `Id`(Guid PK)、`UserId`(Guid, 逻辑外键指向 ApplicationUser，**无 EF FK 约束**，仅 `HasIndex(UserId)`)、`TokenHash`(256)、`LoginTime`、`LogoutTime?`、`ExpiryTime`、`IpAddress`(45)、`UserAgent`(500)、`IsRevoked`、`RevokedReason`(256)、`Status`(CommonStatus)
- 领域方法：`Create()`(静态)、`Logout()`、`Revoke()`、`Revoke(reason)`、`IsValid()`、`IsExpired()`
- **外键结论**：模型快照 `AppDbContextModelSnapshot.cs:25-70` 显示 AuthSessions 表仅有 `UserId` 列，无 `FK_` 定义 → **AuthSession.UserId 是逻辑引用，非数据库外键**（模块隔离设计，两个 DbContext 分表不建 FK）

**SecurityAuditLog**（`src/Shared/LYBT.Entities/Auth/SecurityAuditLog.cs`）：继承 `BaseEntity`（Id/CreatedAt/UpdatedAt/CreatedBy/UpdatedBy/RowVersion/IsDeleted），业务字段 UserId?/UserName?/EventType/IpAddress?/UserAgent?/Details?/IsSuccess/FailureReason?

**SystemLog**（`src/Shared/LYBT.Entities/Common/SystemLog.cs`）：int Id、Timestamp、Level(50)、Message、Exception?、LoggerName?(255)、UserId?、RequestId?(36)、CorrelationId?(36)、MachineName?(100)、ThreadId?、Properties?

### 1.3 AuthDbContext 布局（`Infrastructure/AuthDbContext.cs:13-49`）

- 继承 `DbContext`（非 IdentityDbContext）
- **3 个 DbSet**：`AuthSessions`、`SecurityAuditLogs`、`SystemLogs`（:16/:19/:22）
- `OnModelCreating`（:28-48）：
  - AuthSession：`ToTable("AuthSessions")`、HasKey(Id)、TokenHash(256)/IpAddress(45)/UserAgent(500)/Status 列配置、索引 UserId/TokenHash/ExpiryTime
  - 复用共享配置类：`SecurityAuditLogConfiguration`、`SystemLogConfiguration`（与 AppDbContext 一致）
  - `SecurityAuditLog` 软删除查询过滤器 `!IsDeleted`
- 文档注明：**物理表由 AppDbContext 单一迁移链管理（ADR-0017 方案 A）**，本上下文仅逻辑隔离（同库不同 DbContext）

### 1.4 接口清单 + 实现位置

**IJwtService**（`Interfaces/IJwtService.cs`）→ 实现 `Services/JwtService.cs`
| 方法 | 签名 | 实现行 |
|---|---|---|
| GenerateToken | `string GenerateToken(string userId, string userName, UserRole role, string userType = "user")` | :79 |
| GenerateToken(重载) | `string GenerateToken(string userId, string userName, UserRole role, Dictionary<string,string> additionalClaims, string userType = "user")` | :130 |
| ValidateToken | `ClaimsPrincipal? ValidateToken(string token)` | :190 |
| RefreshToken | `Result<LoginResponse> RefreshToken(string expiredToken)` | :225 |
| ValidateAutoLoginToken | `Result<LoginResponse> ValidateAutoLoginToken(string autoLoginToken)` | :303 |

**IAuthSessionRepository**（`Interfaces/IAuthSessionRepository.cs`）→ 实现 `Infrastructure/AuthSessionRepository.cs`
| 方法 | 签名 | 实现行 |
|---|---|---|
| GetByTokenHashAsync | `Task<AuthSession?> GetByTokenHashAsync(string tokenHash, CancellationToken ct)` | :20 |
| AddAsync | `Task AddAsync(AuthSession session, CancellationToken ct)` | :27 |
| UpdateAsync | `Task UpdateAsync(AuthSession session, CancellationToken ct)` | :34 |
| RevokeAllUserSessionsAsync | `Task RevokeAllUserSessionsAsync(Guid userId, string reason, CancellationToken ct)` | :41 |

**ISecurityAuditRepository**（`Interfaces/ISecurityAuditRepository.cs`）→ 实现 `Infrastructure/SecurityAuditRepository.cs`：`AddAsync(log, ct)` / `SaveChangesAsync(ct)`

**ISecurityAuditService**（`Interfaces/ISecurityAuditService.cs`）→ 实现 `Services/SecurityAuditService.cs`：`RecordEventAsync(SecurityAuditEvent, ct)`

### 1.5 Command/Query/Handler 清单

| Command/Query | Handler | 响应 |
|---|---|---|
| `LoginCommand(LoginRequest Input)` :10 | `LoginCommandHandler`（注入 IJwtService、**IUserCrossModuleService**、IAuthSessionRepository、ISecurityAuditService、ISender、SecurityOptions、JwtOptions、AuthUserMapper）| `Result<LoginResponse>` |
| `AutoLoginCommand(Token, IpAddress?, UserAgent?)` | `AutoLoginCommandHandler`（仅 IJwtService）| `Result<LoginResponse>` |
| `LogoutCommand(LogoutRequest Input)` | `LogoutCommandHandler`（IAuthSessionRepository + ISecurityAuditService）| `Result<bool>` |
| `RefreshTokenCommand(string Token)` | `RefreshTokenCommandHandler`（IJwtService + IAuthSessionRepository + ISecurityAuditService）| `Result<LoginResponse>` |
| `RevokeAllUserTokensCommand(Guid UserId, string Reason)` | `RevokeAllUserTokensCommandHandler`（IAuthSessionRepository + ISecurityAuditService）| `Result<bool>` |
| `ValidateTokenQuery(string Token)` | `ValidateTokenQueryHandler`（IJwtService + IAuthSessionRepository）| `Result<ValidateTokenResult>` |

所有 Handler 实现 `IRequestHandler<,>`（MediatR）；Login/Logout/RefreshToken Handler 内各有私有 `ComputeTokenHash()`（SHA256 小写十六进制）。

### 1.6 Auth 模块内的跨模块注入（核心）

**`IUserCrossModuleService` 在 Auth 内的唯一注入点是 `LoginCommandHandler`**（`Application/Commands/LoginCommandHandler.cs:23,34`），调用 4 个方法：
- `GetUserByUsernameAsync(username, ct)` :62（用户不存在/禁用/锁定校验）
- `VerifyPasswordAsync(username, password, ct)` :111
- `UpdateLoginFailureAsync(userId, failedCount, lockoutEnd, ct)` :141（失败计数 + 锁定）
- `ResetLoginStateAsync(userId, ct)` :145（成功登录清锁定）

Auth 模块**不直接引用** ApplicationUser/UsersDbContext/IUserRepository——只通过跨模块 DTO `UserCredentialDto` 消费。

### 1.7 DI 注册：`AuthModule.cs` AddAuthModule（:21-50）

```csharp
services.AddModuleDbContext<AuthDbContext>(configuration);                       // :24
services.AddScoped<IAuthSessionRepository, AuthSessionRepository>();             // :27
services.AddScoped<ISecurityAuditRepository, SecurityAuditRepository>();         // :28
services.AddScoped<IJwtService, JwtService>();                                   // :31
services.AddScoped<ISecurityAuditService, SecurityAuditService>();               // :32
services.AddScoped<IAuthCrossModuleService, AuthCrossModuleService>();           // :35
services.AddMediatR(RegisterServicesFromAssembly(LoginCommand) + ValidationBehavior); // :38-42
services.AddSingleton<AuthUserMapper>();                                         // :45
services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();           // :48 (Shared SSOT)
```

### 1.8 测试覆盖（详见 §5.1）

Server 侧 Auth 相关：`Unit/Auth/` 5 文件共 27 用例（JwtServiceTests 19、SecurityAuditServiceTests 2、AuthSessionTests 3、AuthSessionRepositoryTests 2、RevokeAllUserTokensCommandHandlerTests 1）+ Validators 9 + JwtOptions 配置类 23 + PasswordPolicy/PasswordHelper 28 + DatabaseInitializationServiceTests（IdentitySeedData）。


---

## 2. Users 模块全貌（LYBT.Module.Users）

### 2.1 文件清单（源码，36 个文件）

| 层 | 文件 |
|---|---|
| 模块注册 | `UsersModule.cs` |
| Interfaces (2) | `Interfaces/IUserService.cs`、`IUserRepository.cs` |
| Infrastructure (2) | `Infrastructure/UsersDbContext.cs`、`UserRepository.cs` |
| Services (3) | `Services/UserService.cs`（internal）、`UserCrossModuleService.cs`、`IdentitySeedData.cs` |
| Application/Commands (22) | 11 个 Command + 11 个 Handler（见 §2.5） |
| Application/Validators (3) | `CreateUserValidator.cs`、`UpdateUserValidator.cs`、`ChangeProfileValidator.cs` |
| Application/Mappers (2) | `Mappers/UserMapper.cs`、`UserCrossModuleMapper.cs` |
| Controllers (1) | `Controllers/BaseUsersController.cs` |
| 文档 | `README.md`（含过期描述，见 §5.2） |

### 2.2 ApplicationUser 完整字段（`src/Shared/LYBT.Entities/Users/ApplicationUser.cs`）

继承 `IdentityUser<Guid>`，实现 `IAuditableEntity, ISoftDeletable`。

**业务字段**：`RealName`(100)｜`PinYinCode?`(50)｜`Role`(UserRole, 默认 Doctor)｜`IsSysAdmin`(bool)｜`Status`(CommonStatus, 默认 Enabled)｜`MustChangeOnNextLogin`(bool, T5-P2-31)｜`LastLoginAt?`(UTC)｜`RegistrationFee`(decimal(10,2), REG-BR-009)｜`Remark?`(500)
**审计字段**：`CreatedAt`(默认 UtcNow)｜`UpdatedAt?`｜`CreatedBy?`｜`UpdatedBy?`
**软删除+并发**：`IsDeleted`(bool)｜`RowVersion`([Timestamp])
**领域方法**：`Create()`(静态工厂, 校验用户名3-32/真实姓名)｜`UpdateProfile()`｜`ChangeStatus()`(sysadmin 禁禁用)｜`SoftDelete()`(sysadmin 禁删)

Identity 基类自带：Id(Guid)、UserName、NormalizedUserName、Email/NormalizedEmail、PhoneNumber、PasswordHash、SecurityStamp、ConcurrencyStamp、EmailConfirmed、PhoneNumberConfirmed、TwoFactorEnabled、LockoutEnd(DateTimeOffset?)、LockoutEnabled、AccessFailedCount。

### 2.3 UsersDbContext 布局（`Infrastructure/UsersDbContext.cs:11-42`）

- 继承 `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`（:11）
- OnModelCreating（:17-42）：`ApplicationUser` 配置 `ToTable("Users")`(:24)，UserName(32 唯一索引)/RealName(50)/PinYinCode(50)/PhoneNumber(20)/Email(100)/Remark(500) 长度、`Role`/`Status` 转 int、索引 PinYinCode/Role/Status、软删除过滤器 `!IsDeleted`
- 注意：UserConfiguration（Infrastructure/Data/Configurations/UserConfiguration.cs）在 AppDbContext 也配置了同一实体（RealName(100)、RegistrationFee、RowVersion、审计字段默认值）——**同一实体在两个 DbContext 有重叠配置**（UsersDbContext 与 AppDbContext）

### 2.4 接口清单 + 实现位置

**IUserService**（`Interfaces/IUserService.cs`）→ 实现 `Services/UserService.cs`（**internal class**，供 Controller 注入）
| 方法 | 签名 | 实现行 |
|---|---|---|
| GetPagedAsync | `Task<Result<PagedResult<UserListDto>>> GetPagedAsync(int page, int pageSize, string? keyword, CancellationToken ct)` | :21 |
| GetByIdAsync | `Task<Result<UserDetailDto>> GetByIdAsync(Guid id, CancellationToken ct)` | :35 |
| GetCurrentUserAsync | `Task<Result<UserDetailDto>> GetCurrentUserAsync(Guid userId, CancellationToken ct)` | :43 |

**IUserRepository**（`Interfaces/IUserRepository.cs`）→ 实现 `Infrastructure/UserRepository.cs`
| 方法 | 签名 | 实现行 |
|---|---|---|
| GetByIdAsync | `Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken ct)` | :22 |
| GetByIdIncludingDeletedAsync | `Task<ApplicationUser?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct)`（IgnoreQueryFilters）| :29 |
| GetPagedAsync | `Task<PagedResult<ApplicationUser>> GetPagedAsync(page, pageSize, keyword, UserRole? role, CommonStatus? status, ct)` | :37 |
| UpdateAsync | `Task UpdateAsync(ApplicationUser user, CancellationToken ct)` | :80 |

**IUserCrossModuleService**（接口位于 `src/Server/Core/LYBT.Infrastructure/Services/CrossModule/IUserCrossModuleService.cs`）→ 实现 `Services/UserCrossModuleService.cs`
| 方法 | 签名 | 实现行 |
|---|---|---|
| GetUserBasicInfoAsync | `Task<UserBasicDto?> GetUserBasicInfoAsync(Guid userId, ct)` | :29 |
| GetUserByUsernameAsync | `Task<UserCredentialDto?> GetUserByUsernameAsync(string username, ct)`（含 PasswordHash）| :41 |
| UpdateLoginFailureAsync | `Task UpdateLoginFailureAsync(Guid userId, int failedLoginCount, DateTime? lockoutEnd, ct)` | :53 |
| ResetLoginStateAsync | `Task ResetLoginStateAsync(Guid userId, ct)`（清 AccessFailedCount/LockoutEnd + 更新 LastLoginAt）| :67 |
| VerifyPasswordAsync | `Task<bool> VerifyPasswordAsync(string username, string password, ct)`（UserManager.CheckPasswordAsync, Identity PBKDF2）| :80 |

### 2.5 Command/Handler 完整列表（11 组）

| Command | Handler | 主要注入 | 对应 API 端点（BaseUsersController） |
|---|---|---|---|
| `CreateUserCommand(Input, CurrentUserId, IsAdmin)` | `CreateUserCommandHandler` | UserManager | `POST /api/v1/users` :65 |
| `UpdateUserCommand(Id, Input, CurrentUserId)` | `UpdateUserCommandHandler` | IUserRepository | `PUT /{id:guid}` :78 |
| `DeleteUserCommand(Id, CurrentUserId, IsAdmin)` | `DeleteUserCommandHandler` | IUserRepository + **IAuthCrossModuleService** | `DELETE /{id:guid}` :96 |
| `ToggleUserStatusCommand(Id, CurrentUserId, IsAdmin)` | `ToggleUserStatusCommandHandler` | IUserRepository + **IAuthCrossModuleService** | `POST /{id}/toggle-status` :115 |
| `RestoreUserCommand(UserId, OperatorId, OperatorRole)` | `RestoreUserCommandHandler` | IUserRepository | `POST /{id}/restore` :131 |
| `ResetPasswordCommand(Id)` → `ResetPasswordResult(TemporaryPassword)` | `ResetPasswordCommandHandler` | UserManager + **IAuthCrossModuleService** | `POST /{id}/reset-password` :192 |
| `ChangePasswordCommand(Id, OldPassword, NewPassword, CurrentUserId)` | `ChangePasswordCommandHandler` | UserManager + **IAuthCrossModuleService** | `PUT /{id}/change-password` :243 |
| `ChangeProfileCommand(Id, Dto, CurrentUserId)` | `ChangeProfileCommandHandler` | IUserRepository | `PUT /{id}/profile` :219 |
| `BatchEnableUsersCommand(List<Guid> Ids)` | `BatchEnableUsersCommandHandler`（继承 `BatchOperationHandlerBase<ApplicationUser>`）| IUserRepository | `POST /batch-enable` :268 |
| `BatchDisableUsersCommand(List<Guid> Ids)` | `BatchDisableUsersCommandHandler` | IUserRepository | `POST /batch-disable` :289 |
| `BatchDeleteUsersCommand(Ids, CurrentUserId, IsAdmin)` | `BatchDeleteUsersCommandHandler` | IUserRepository | `POST /batch-delete` :152 |

**Validators**：CreateUserValidator（用户名 3-32 字母数字下划线/手机号/邮箱）、UpdateUserValidator、ChangeProfileValidator（均继承 `AbstractValidator<T>`，经 `AddValidatorsFromAssemblyContaining<CreateUserValidator>` 注册，ValidationBehavior 统一执行）。

### 2.6 Users 内部跨模块注入（核心）

**`IAuthCrossModuleService` 在 Users 内的注入点（4 个 Handler）**：
- `DeleteUserCommandHandler.cs:16,20` → 调用 `RevokeAllUserSessionsAsync(user.Id, "用户已删除")` :43 + `RecordSecurityAuditAsync(UserDeleted)` :44-51
- `ToggleUserStatusCommandHandler.cs:16,20` → 禁用时 `RevokeAllUserSessionsAsync(user.Id, "用户已被禁用")` :49 + `RecordSecurityAuditAsync(UserStatusChanged)` :52-57
- `ResetPasswordCommandHandler.cs:15,19` → `RevokeAllUserSessionsAsync(user.Id, "管理员重置密码")` :42 + `RecordSecurityAuditAsync(PasswordReset)` :43-50
- `ChangePasswordCommandHandler.cs:14,18` → `RevokeAllUserSessionsAsync(user.Id, "密码已修改")` :42 + `RecordSecurityAuditAsync(PasswordChanged)` :43-50

### 2.7 DI 注册：`UsersModule.cs` AddUsersModule（:24-48）

```csharp
services.AddModuleDbContext<UsersDbContext>(configuration);                      // :27
services.AddScoped<IUserRepository, UserRepository>();                           // :30
services.AddScoped<IUserService, UserService>();                                 // :33
services.AddScoped<IUserCrossModuleService, UserCrossModuleService>();           // :36
services.AddMediatR(RegisterServicesFromAssembly(CreateUserCommand) + ValidationBehavior); // :39-43
services.AddValidatorsFromAssemblyContaining<CreateUserValidator>();             // :46
```

**注意**：UsersModule 中**未注册 Identity 本身**（UserManager/SignInManager/RoleManager）——Identity 注册在两端 Program 中（见 §3.5）。

### 2.8 Identity 集成（UserManager / SignInManager）

- **Remote**：`src/Server/Services/LYBT.WebAPI/Program.cs`（经 RegisterAllApplicationServices → 见 §3.5）
- **Local**：`LocalWebApiProgram.cs:86-97` 显式 `AddIdentity<ApplicationUser, IdentityRole<Guid>>(...).AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders()`，Lockout 关闭（MaxFailedAccessAttempts=int.MaxValue）
- **消费方**：CreateUserCommandHandler（UserManager.CreateAsync）、ResetPasswordCommandHandler（GeneratePasswordResetTokenAsync/ResetPasswordAsync）、ChangePasswordCommandHandler（ChangePasswordAsync）、UserCrossModuleService.VerifyPasswordAsync（CheckPasswordAsync）、IdentitySeedData（UserManager/RoleManager）

---

---

## 3. 跨模块关系（serena 符号引用核心）

### 3.1 Auth→Users 依赖链（Auth 消费 Users）

```
AuthModule (LYBT.Module.Auth)
  └── LoginCommandHandler (Application/Commands/LoginCommandHandler.cs:23,34)
        ├── IUserCrossModuleService.GetUserByUsernameAsync()   → UserCrossModuleService.GetUserByUsernameAsync (UsersModule.Users/Services/UserCrossModuleService.cs:41)
        │                                                         → UsersDbContext.Users 查询 (AsNoTracking, !IsDeleted)
        │                                                         → UserCrossModuleMapper.ToCredentialDto (含 PasswordHash)
        ├── IUserCrossModuleService.VerifyPasswordAsync()      → UserCrossModuleService.VerifyPasswordAsync (:80)
        │                                                         → UserManager<ApplicationUser>.CheckPasswordAsync (Identity PBKDF2)
        ├── IUserCrossModuleService.UpdateLoginFailureAsync()  → UserCrossModuleService.UpdateLoginFailureAsync (:53) → UsersDbContext 更新
        └── IUserCrossModuleService.ResetLoginStateAsync()     → UserCrossModuleService.ResetLoginStateAsync (:67)   → UsersDbContext 更新
```

- 接口定义：`src/Server/Core/LYBT.Infrastructure/Services/CrossModule/IUserCrossModuleService.cs`（5 方法）
- 实现：`src/Server/Modules/LYBT.Module.Users/Services/UserCrossModuleService.cs`（注入了 **UsersDbContext + UserManager**）
- DI 绑定：`UsersModule.cs:36` `AddScoped<IUserCrossModuleService, UserCrossModuleService>()`
- Auth 侧消费 DTO：`UserCredentialDto`（`src/Shared/LYBT.Shared.Models/Contracts/Users/UserBasicDto.cs:37`，含 PasswordHash，继承 UserBasicDto）
- 映射：`AuthUserMapper.ToUserDetailDto(UserCredentialDto)`（Auth/Application/Mappers/AuthUserMapper.cs:16，Mapperly，RequiredMappingStrategy.Target）

### 3.2 Users→Auth 依赖链（Users 消费 Auth）

```
UsersModule (LYBT.Module.Users)
  ├── DeleteUserCommandHandler        (Application/Commands/DeleteUserCommandHandler.cs:16,20)
  ├── ToggleUserStatusCommandHandler  (Application/Commands/ToggleUserStatusCommandHandler.cs:16,20)
  ├── ResetPasswordCommandHandler     (Application/Commands/ResetPasswordCommandHandler.cs:15,19)
  └── ChangePasswordCommandHandler    (Application/Commands/ChangePasswordCommandHandler.cs:14,18)
        └── IAuthCrossModuleService
              ├── RevokeAllUserSessionsAsync(userId, reason)  → AuthCrossModuleService (Auth/Services/AuthCrossModuleService.cs:23)
              │                                                  → IAuthSessionRepository.RevokeAllUserSessionsAsync
              │                                                    → AuthSessionRepository (Auth/Infrastructure/AuthSessionRepository.cs:41)
              │                                                      → AuthDbContext.AuthSessions 批量撤销
              └── RecordSecurityAuditAsync(SecurityAuditEvent) → AuthCrossModuleService (:26)
                                                                  → ISecurityAuditService.RecordEventAsync
                                                                    → SecurityAuditService (Auth/Services/SecurityAuditService.cs:19)
                                                                      → ISecurityAuditRepository → SecurityAuditRepository (AuthDbContext)
```

- 接口定义：`src/Server/Core/LYBT.Infrastructure/Services/CrossModule/IAuthCrossModuleService.cs`（2 方法）
- 实现：`src/Server/Modules/LYBT.Module.Auth/Services/AuthCrossModuleService.cs`（注入 IAuthSessionRepository + ISecurityAuditService）
- DI 绑定：`AuthModule.cs:35` `AddScoped<IAuthCrossModuleService, AuthCrossModuleService>()`
- 审计 DTO：`SecurityAuditEvent`（`src/Shared/LYBT.Shared.Models/Contracts/Auth/SecurityAuditEvent.cs`）

**循环依赖分析**：Auth(LoginHandler)→IUserCrossModuleService→UserCrossModuleService→UsersDbContext/UserManager；Users(4 Handlers)→IAuthCrossModuleService→AuthCrossModuleService→AuthDbContext。**编译期无环**（双方都只依赖 Infrastructure 接口 + Shared DTO，不直接引用对方模块程序集），运行时 DI 图也无环（作用域内单次解析）。

### 3.3 外部模块对 Auth/Users 的依赖

**IUserCrossModuleService（Users 对外提供）的消费者**：
| 模块 | 注入点 | 调用方法 |
|---|---|---|
| MedicalCase | `MedicalCaseCommandService.cs:30,41` | GetUserBasicInfoAsync 等 |
| MedicalCase | `MedicalCaseServiceHelper.cs:25-26,60` | GetOperatorInfoAsync（GetUserBasicInfoAsync）|
| MedicalCase | `MedicalCaseStateService.cs:28,34` | GetUserBasicInfoAsync |
| Registration | `QuickVisitCommandHandler.cs:16` | GetUserBasicInfoAsync / 挂号费（ApplicationUser.RegistrationFee，REG-BR-009）|

> 证据：`IUserCrossModuleService` 接口注释（`IUserCrossModuleService.cs:7` "供 MedicalCase + Auth 模块使用"，接口声明 :9）；README 记录消费者 MedicalCase + Auth（`Infrastructure/README.md:417`）。csproj 注释 D5-1（`LYBT.Module.MedicalCase.csproj:15`）。

**Auth 对外的 IAuthCrossModuleService 消费者**：仅 Users 模块 4 个 Handler（见 §3.2）。

**其他模块对 Auth 的依赖**：仅通过 HTTP 认证（JwtBearer 中间件 + 授权策略，`AuthenticationServiceCollectionExtensions.cs`）与共享 DTO，无程序集级引用。`ICrossModuleAuthService`/`ITokenRevocationService` **仅存在于 README 历史描述中，代码中已删除**（见 §5.2）。

### 3.4 Desktop 侧对应（IApiClientAuth / IApiClientUsers）

**接口（`src/Client/Desktop/Core/LYBT.Desktop.Contracts/ApiClient/`）**：
- `IApiClientAuth.cs`：`LoginAsync(LoginRequest)`、`LoginWithAutoTokenAsync(AutoLoginRequest)`、`LogoutAsync(LogoutRequest)`、`RefreshTokenAsync(RefreshTokenRequest)`、`ValidateTokenAsync()`、`HealthCheckAsync()`（均 ApiResponse）
- `IApiClientUsers.cs`：继承 `IEntityApiSegment<UserListDto,UserDetailDto,UserInputDto>`，含 GetUsers/GetById/Create/Update/Delete/ChangeProfile/ChangePassword/ResetPassword/ToggleStatus/BatchDelete/Restore/BatchEnable/BatchDisable + 本地专属 `GetCurrentUserAsync()→UserDetailDto`（远程 UserApiClient 该实现抛 NotSupportedException）
- 聚合口：`IApiClient.cs:23,26` 暴露 `Auth`/`Users`

**实现（`src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http/Clients/`）**：
| 实现 | 接口 | 模式 |
|---|---|---|
| `AuthApiClient.cs:18` | IApiClientAuth | Refit→`IAuthApi`（远程 WebAPI）|
| `AuthHttpApiClient.cs:15` | IApiClientAuth | HttpClient→LocalWebAPI `/api/v1/auth/...` |
| `UserApiClient.cs:19` | IApiClientUsers | Refit→`IUserApi`（远程）|
| `UsersHttpApiClient.cs:16` | IApiClientUsers | HttpClient→LocalWebAPI `/api/v1/users/...` |
| `SwitchingApiClient.cs:75-77` | — | IsLocal 判断切换 HttpClientApiClient / RefitApiClient |

**Desktop 注入点**：AuthenticationService.cs:21,29、LogoutService.cs:21,40、TokenLifecycleService.cs:24,46、RefitApiClient.cs:38-39,68-72、HttpClientApiClient.cs:27-28,50-53、SwitchingApiClient.cs:86-89、Sysadmin/AuthHealthService.cs:12-14。

### 3.5 LocalWebAPI / Remote WebAPI 双轨实现

**Remote（`src/Server/Services/LYBT.WebAPI/Controllers/`）**：
- `AuthController.cs` — `[Route("api/v{version}/[controller]")]`=api/v1/auth，类级 [Authorize]；action：POST login(:39, AllowAnonymous+RateLimit Login)、POST logout(:68)、POST refresh(:89)、POST auto-login(:111)、GET validate(:125)、GET 拒绝(:165)。**直接 Send Auth 模块 MediatR Command**（LoginCommand/LogoutCommand/RefreshTokenCommand/AutoLoginCommand/ValidateTokenQuery）
- `UsersController.cs:15` — `[Route("api/v{version:apiVersion}/users")]`，**继承 `BaseUsersController`**，无自有 action

**Local（`src/Client/Desktop/LocalWebAPI/Controllers/`）**：
- `AuthController.cs` — `[Route("api/v1/[controller]")]`=api/v1/auth；action：POST login(:30, LocalLoginCommand)、POST logout(:40, 仅记日志)、POST refresh(:48, LocalRefreshTokenCommand)、POST auto-login(:58, LocalAutoLoginCommand)、GET validate(:67, LocalValidateTokenQuery)。**走 Local*Command（独立 CQRS）**，不复用 Auth 模块 Command
- `UsersController.cs:11` — `[Route("api/v1/[controller]")]`=api/v1/users，**同样继承 `BaseUsersController`**（共享 Users 模块 Controller 基类，双端 action 完全一致）

**BaseUsersController 完整 action 清单**（`src/Server/Modules/LYBT.Module.Users/Controllers/BaseUsersController.cs`，双端共用）：GET /(:37, AdminOrSuperAdmin)、GET /{id:guid}(:53)、POST /(:65)、PUT /{id:guid}(:78)、DELETE /{id:guid}(:96)、POST /{id}/toggle-status(:115)、POST /{id}/restore(:131)、POST /batch-delete(:152)、GET /current(:177)、POST /{id}/reset-password(:192)、PUT /{id}/profile(:219)、PUT /{id}/change-password(:243)、POST /batch-enable(:268)、POST /batch-disable(:289)。注入 IUserService + ISender；重写 BaseCrudController 并加 `[Authorize(Policy=AdminOrSuperAdmin)]`。

**LocalWebAPI Auth 本地 Handler**（`src/Client/Desktop/LocalWebAPI/Handlers/`）：
- `LocalLoginCommandHandler.cs`：直接 UserManager.FindByNameAsync + SignInManager.CheckPasswordSignInAsync + LocalJwtConfig.GenerateToken——**未走 Auth 模块登录流程**（无 AuthSession 落库、无安全审计、无账户锁定逻辑，仅更新 LastLoginAt）
- `LocalRefreshTokenCommandHandler.cs`：ReadJwtToken + UserManager.FindByIdAsync + 重新签发（无会话验证）
- `LocalAutoLoginCommandHandler.cs`、`LocalValidateTokenQueryHandler.cs` 同理

**两端启动注册顺序**：
- Remote：`ServiceCollectionExtensions.cs:93,100`（RegisterBusinessModules 中 AddAuthModule → AddRegistrationModule → AddUsersModule → ...，**Auth 先于 Users**）
- Local：`LocalWebApiProgram.cs:66-67`（AddAuthModule → AddUsersModule）——两端注册顺序一致，且 Local 额外在 :86-97 注册 Identity（AppDbContext 存储，:96-97）；Remote 的 Identity 注册位于 `Program.cs:124-135`（AddIdentity → AddEntityFrameworkStores → AddDefaultTokenProviders）。

---

---

## 4. 基础设施层

### 4.1 SecurityAuditRepository / SecurityAuditService 职责

**SecurityAuditRepository**（`src/Server/Modules/LYBT.Module.Auth/Infrastructure/SecurityAuditRepository.cs`，实现 ISecurityAuditRepository）
- 职责：审计日志的**写入通道**（仅 Add + Save，无查询方法）
- `AddAsync(SecurityAuditLog, ct)` :12 — 追加日志实体（不落库）
- `SaveChangesAsync(ct)` :17 — 提交到 AuthDbContext.SecurityAuditLogs
- 注入 AuthDbContext；无软删除/查询能力（查询能力在 AppDbContext 侧 SystemLogRepository，与审计日志无关）

**SecurityAuditService**（`src/Server/Modules/LYBT.Module.Auth/Services/SecurityAuditService.cs`，实现 ISecurityAuditService）
- 职责：**审计事件门面**，将 `SecurityAuditEvent`（Shared DTO）转换为 `SecurityAuditLog` 实体并落库
- `RecordEventAsync(SecurityAuditEvent, ct)` :19 — 构造 SecurityAuditLog（字段逐一映射）→ repository.AddAsync → SaveChangesAsync；catch 后 LogError 不抛出（审计失败不阻断业务）
- 注入 ISecurityAuditRepository + ILogger

**审计链**：`XxxCommandHandler → ISecurityAuditService.RecordEventAsync(SecurityAuditEvent) → SecurityAuditService → ISecurityAuditRepository → AuthDbContext.SecurityAuditLogs`
（跨模块入口：`IAuthCrossModuleService.RecordSecurityAuditAsync` → 同一条链）

### 4.2 JwtService 完整方法（`src/Server/Modules/LYBT.Module.Auth/Services/JwtService.cs`）

| 成员 | 可见性 | 职责 | 行 |
|---|---|---|---|
| 构造 `JwtService(IOptionsMonitor<JwtOptions>, IWebHostEnvironment)` | public | 启动时 ValidateSecretKeyStrength | :28 |
| `CurrentOptions` | private | 热重载配置快照 | :39 |
| `ValidateSecretKeyStrength()` | private | 密钥 ≥32 字符；Production 禁用默认密钥（T5-P2-45） | :44 |
| `GenerateToken(userId, userName, role, userType)` | public | 基础 Claims（NameIdentifier/Name/Role/user_type/Jti/Iat）+ HMAC-SHA256 签发 | :79 |
| `GenerateToken(userId, userName, role, additionalClaims, userType)` | public | 支持额外 Claims（IsSysAdmin） | :130 |
| `ValidateToken(token)` | public | ValidateLifetime=true，失败返回 null | :190 |
| `RefreshToken(expiredToken)` | public | ValidateLifetime=false（接受过期令牌）+ 重签 | :225 |
| `ValidateAutoLoginToken(autoLoginToken)` | public | 完整校验 + 重签（自动登录） | :303 |

配置来源：`JwtOptions`（LYBT.Shared.Configuration.Options.Common，含 SecretKey/Issuer/Audience/AccessTokenExpirationMinutes/ClockSkewSeconds），IOptionsMonitor 热重载。

### 4.3 IdentitySeedData 初始化逻辑（`src/Server/Modules/LYBT.Module.Users/Services/IdentitySeedData.cs`）

- `SeedRolesAndAdminAsync(IServiceProvider)` :14 — 从 DI 取 RoleManager<UserIdentityRole>/UserManager/DefaultPasswordOptions/IHostEnvironment；创建 4 角色（Receptionist/Doctor/Admin/SuperAdmin，RoleConstants）；确保 sysadmin 用户（"系统运维"/sysadmin@lybtzyzs.local/SuperAdmin，IsSysAdmin=true）
- `ResolveSysAdminPassword(env, configuredPassword)` :40 — **生产强制环境变量**（DefaultPasswords__SysAdminPassword 或 ${SYSADMIN_PASSWORD} 兼容），缺失抛异常；非生产用配置默认（K4 安全加固）
- `EnsureUserAsync(...)` :61 — 用户不存在→CreateAsync+AddToRoleAsync；已存在且从未登录→校验/重置密码；同步 IsSysAdmin 与 Role 字段；补角色
- 调用点：Local `LocalWebApiProgram.cs:142`（InitializeDatabaseAsync）；Remote `Program.cs`（数据库初始化流程，含 DatabaseInitializationService）

### 4.4 双轨 DbContext 与迁移

- `AddModuleDbContext<TContext>`（`src/Server/Core/LYBT.Infrastructure/Data/ModuleDbContextExtensions.cs:20`）：各模块 DbContext 与 AppDbContext **同库同连接串**，仅逻辑隔离
- AuthDbContext/UsersDbContext 的物理表全部由 **AppDbContext 单一迁移链**管理（ADR-0017 方案 A）：AuthSessions/SecurityAuditLogs/SystemLogs/Users（Identity 表）均在 `AppDbContextModelSnapshot.cs` 中；相关迁移：20260405123033_InitialCreate、20260617124932_AddIdentityTables、20260620064056_AddIsSysAdmin、20260803072745_AddRegistrationFeeToApplicationUser、20260805132335_RecreateDroppedAuditTables、20260806001815_AddRevokedReasonToAuthSessions
- **关键点**：两个模块 DbContext 无自己的迁移链（不参与 Database.Migrate），仅模块注册时 `AddDbContext` 绑定到同一 SQL Server 连接

---

## 5. 历史重构记录与文档一致性

### 5.1 测试覆盖清单

**Server（tests/LYBT.Tests.Server/，Auth/Users 相关 87+ 用例）**
| 文件 | 类 | 用例 | 覆盖点 |
|---|---|---|---|
| Unit/Auth/JwtServiceTests.cs | JwtServiceTests | 19 | 签发/刷新/校验/过期 |
| Unit/Auth/SecurityAuditServiceTests.cs | SecurityAuditServiceTests | 2 | 审计落库 |
| Unit/Auth/AuthSessionTests.cs | AuthSessionTests | 3 | 会话实体域逻辑 |
| Unit/Auth/AuthSessionRepositoryTests.cs | AuthSessionRepositoryTests | 2 | 会话仓储 |
| Unit/Auth/RevokeAllUserTokensCommandHandlerTests.cs | RevokeAllUserTokensCommandHandlerTests | 1 | 批量撤销+审计 |
| Unit/Validators/Validators/Auth/LoginRequestValidatorTests.cs | — | 9 | 登录请求验证 |
| Unit/Shared/.../JwtOptionsTests(+Validator/Validation) | — | 23 | Jwt 配置校验 |
| Unit/Utilities/PasswordPolicyValidatorTests.cs | — | 20 | 密码策略（Identity PBKDF2）|
| Unit/Utilities/PasswordHelperTests.cs | — | 8 | 密码生成 |
| Unit/Infrastructure/DatabaseInitializationServiceTests.cs | — | — | IdentitySeedData 管理员初始化 |

**Desktop（tests/LYBT.Tests.Desktop/，167+ 用例）**
| 文件 | 用例 | 覆盖点 |
|---|---|---|
| Unit/Auth/LoginViewModelTests.cs | 21 | 登录 VM |
| Unit/Foundation/AuthenticationStateMachineTests.cs | 33 | 认证状态机 |
| Unit/Foundation/LogoutServiceTests.cs | 17 | 登出 |
| Unit/Foundation/CredentialVaultTests.cs | 15 | DPAPI 凭据 |
| Unit/Foundation/LocalTokenValidatorTests.cs | 6 | 本地 JWT |
| Unit/Foundation/AuthEventPublishingTests.cs | 4 | 认证事件 |
| Unit/Users/UserMasterDetailViewModelTests.cs | 15 | 用户主从 VM |
| Integration/Modules/UserTests.cs + UserNegativeTests.cs | 10+6 | 用户 E2E |
| Integration/Foundation/AuthTests + AuthNegativeTests + AuthenticationIntegrationTests + TokenRefreshHandlerIntegrationTests | 7+5+4+5 | 认证/刷新 E2E |
| Integration/LocalWebAPI/AuthControllerTests + UsersControllerTests + LocalJwtConfigTests | 8+6+5 | 本地双轨控制器 |

### 5.2 历史重构决策（ADR / 总账 / 蓝图 / 审计报告）

**ADR（docs/03-architecture/decisions/）**
| 决策 | 日期 | 摘要 |
|---|---|---|
| ADR-0004 | 2025-11-01 | 用户上下文：Controller 显式提取 userId，禁 IHttpContextAccessor |
| ADR-0005 | 2025-12-01（2026-06-28 废弃）| SuperAdmin 原归 Auth 模块 → 后迁 Users 表 `ApplicationUser.IsSysAdmin` |
| ADR-0008 | 2026-02-21 | Token 安全：族旋转/限流/登出撤销 |
| ADR-0017 | 2026-06-29 | 模块化单体：Auth/Users 各自独立三层 + DbContext，跨模块走 ICrossModuleService |
| ADR-0009/0010 | — | 双轨架构两端复用 JWT 与 Auth/Users Service |

**总账（docs/03-architecture/13-project-master-plan.md）**
- :386 (B-21, 2026-08-06)：新增 `IAuthCrossModuleService`——Users 经其触发撤销/审计，不直引 Auth
- :75 (A-28)：跨模块服务改注入模块 DbContext；删 Auth 版 LoginRequestValidator（Shared 为 SSOT）；Jwt Section 统一
- :405 (A-26, 2026-08-08)：Auth/Users 写走 Handler、读走 Service 定案

**合并可行性结论（compose/reports/）**
- `method-audit-server-2026-08-08.md:282`：判定**合并可行**（凭证域高耦合），建议并成认证用户模块
- `03-architecture/15-solution-integration-plan.md:31,135`：定案 `Auth+Users → Identity`，但 **C-3 ⏸ 待定（用户延期决策，2026-08-08 暂缓）**
- `method-audit-desktop-2026-08-08.md:521`：**Desktop 侧 Auth 不合并**（LoginViewModel 与 MD 模板不同构）
- 当前任务书 `docs/compose/specs/auth-users-merge-research-2026-08-09.md`：合并前调研进行中

### 5.3 文档-代码不一致（合并前需修复的文档债务）

- `LYBT.Module.Auth/README.md` 描述的 `IAuthService`、`ITokenRevocationService`、`IAutoLoginService`、`ITokenManagementService`、`IAutoLoginTokenRepository`、`IRefreshTokenRepository`（README :9,96,111,143,144,191,207 及 Infrastructure/README.md :132,193,194,409,424）**在代码中均不存在**——已重构删除但文档未更新
- `LYBT.Module.Users/README.md:202` 引用的 `ICrossModuleAuthService` 同理已不存在
- Auth README 称 `IJwtService(Singleton)`，实际 DI 为 `AddScoped`（AuthModule.cs:31）——README 过期
- `AuthSessionConfiguration.cs`（AppDbContext 侧）与 AuthDbContext.OnModelCreating 对 AuthSession 有**重复配置**（HasKey/ToTable/Status 转换），是 Issue #1765 后遗留的双配置
- `UserConfiguration.cs`（AppDbContext 侧）与 UsersDbContext.OnModelCreating 对 ApplicationUser 有重叠配置（RealName 长度 100 vs 50 不一致：AppDbContext 侧 100，UsersDbContext 侧 50）——合并时需统一

---

## 6. 合并决策要点（供后续设计参考，非本次改动）

1. **凭证域耦合高**（method-audit-server-2026-08-08:282）：Auth 登录依赖 Users 的 5 个跨模块方法，Users 4 个 Handler 依赖 Auth 的撤销+审计——合并可消除两条 CrossModule 通道（IUserCrossModuleService 的 Auth 消费者 + IAuthCrossModuleService 全部消费者），但 **MedicalCase/Registration 仍消费 IUserCrossModuleService**（GetUserBasicInfoAsync），该接口**必须保留**。
2. **双 DbContext 合并**：AuthDbContext(3 DbSet) + UsersDbContext(Identity 表) 合并为一个 DbContext 时需处理：AppDbContext 侧重复配置（AuthSessionConfiguration/UserConfiguration）、RealName 长度冲突（50 vs 100）、迁移链重组（当前表全在 AppDbContext 链上，方案 A 下物理表已同库，合并主要是逻辑层收敛）。
3. **Local 侧 Auth 未走共享流程**：LocalLoginCommandHandler 直接 UserManager+SignInManager，跳过 AuthSession/审计/锁定——若合并为认证用户模块，Local 登录应统一走共享 Handler，否则双轨行为继续漂移。
4. **Identity 注册差异**：Remote（Program.cs:124-135，锁定 5 次/15 分钟）vs Local（LocalWebApiProgram.cs:86-97，锁定关闭 int.MaxValue）——合并后需统一。
5. **Desktop 不合并**（method-audit-desktop-2026-08-08:521）：合并范围限于 Server 端 Auth/Users 模块。
6. **C-3 决策待定**：solution-integration-plan 中 Auth+Users→Identity 合并已定案但用户延期，本报告为决策提供事实基础。

---

*报告完成。全部结论基于源码符号与引用分析（serena MCP + grep + 子代理交叉验证），未修改任何 src/tests 代码。*
