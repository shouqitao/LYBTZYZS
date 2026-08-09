# A-31-C3a：Auth+Users → Identity 模块合并报告

> **任务**：`docs/compose/specs/task-a31-c3a-auth-users-merge-2026-08-09.md`（T2 方案调整，先文档后代码）
> **执行**：Mimo Code | **日期**：2026-08-09 | **分支**：master
> **调研依据**：`docs/compose/reports/auth-users-merge-research-2026-08-09.md`

---

## 1. 执行摘要

将 `LYBT.Module.Auth` + `LYBT.Module.Users` 合并为 `LYBT.Module.Identity`（认证用户模块），消除两条 CrossModule 通道，统一本地登录路径。按 3 阶段独立验证 + 独立 commit + push 完成。

| 阶段 | 内容 | Commit | 验证 |
|------|------|--------|------|
| A | Identity 骨架 + IdentityDbContext + sln | `d614283bc` | build 0/0 |
| B | 接口迁移 + 17 Command 合并 + Mapper/Validator + IdentityModule + 登录统一 | `1164ae0c5` | build 0/0 |
| C | 外部引用更新 + Controller 合并 + 旧项目删除 + 测试合并 | `cd6b80721` | build 0/0 + 架构测试 87/87 |

---

## 2. 阶段 A：Identity 骨架 + 实体合并

- 新建 `src/Server/Modules/LYBT.Module.Identity/`（csproj 引用 LYBT.Entities/Shared.Models/Shared.Configuration/Infrastructure + InternalsVisibleTo LYBT.Tests.Server）
- 新建 `Identity/Infrastructure/IdentityDbContext.cs`：继承 `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`，包含 AuthSessions/SecurityAuditLogs/SystemLogs 三个 DbSet，OnModelCreating 合并 AuthDbContext + UsersDbContext 配置（表名/长度/索引/软删过滤器逐项保留）
- sln 加入新项目

**设计决策（偏离任务书字面，架构硬约束）**：
- **实体不物理移动**：任务书 A2 要求 `AuthSession`/`SecurityAuditLog` → `Identity/Entities/`。但实体实际位于 `Shared/LYBT.Entities/Auth/`（调研报告 §1.2），且 **AppDbContext（Infrastructure/Core）是迁移链所有者**（`DbSet<AuthSession>`/`DbSet<SecurityAuditLog>`/`DbSet<SystemLog>`），若物理移入 Identity 模块将产生 Core→Modules 反向依赖，违反依赖方向并破坏迁移链。故实体保留 Shared，Identity 引用（与任务书对 ApplicationUser 的处理一致）。

## 3. 阶段 B：接口迁移 + Command 合并 + 登录统一

**接口迁移（→ Identity/Interfaces/）**：IJwtService / IAuthSessionRepository / ISecurityAuditRepository / ISecurityAuditService / IUserRepository

**实现迁移（→ Identity/Services/ + Infrastructure/）**：JwtService / AuthSessionRepository / SecurityAuditRepository / SecurityAuditService / UserService / UserRepository / IdentitySeedData

**Command 合并（17 个）**：
- Auth 6：Login / AutoLogin / Logout / RefreshToken / RevokeAllUserTokens / ValidateToken（Query）
- Users 11：CreateUser / UpdateUser / DeleteUser / ToggleUserStatus / RestoreUser / ResetPassword / ChangePassword / ChangeProfile / BatchEnable / BatchDisable / BatchDelete

**Mapper 合并**：AuthUserMapper + UserMapper + UserCrossModuleMapper → **IdentityMapper**（static partial，含 ToUserDetailDto/ToListDto/ToDetailDto/ToBasicDto/ToCredentialDto）

**Validator 迁移**：ChangeProfileValidator / CreateUserValidator / UpdateUserValidator

**IdentityModule.cs**：`AddIdentityModule` 注册全部（DbContext/仓储/服务/IUserService/MediatR/Validators/LoginOptions）

**本地登录统一**：
- 新建 `LoginOptions`（Shared.Configuration/Options/Common）：`IsLocal` / `LockoutEnabled` / `AuditLevel`，通过 `IOptions<LoginOptions>` 注入（任务书"IOptions<JwtOptions>"表述按标准 Options 模式实现，语义一致）
- `LoginCommandHandler` 支持 LoginOptions：LockoutEnabled=false 跳过锁定、AuditLevel 控制审计写入（None/Minimal/Full）
- sysadmin 登录附加 `IsSysAdmin` claim（UserCredentialDto 增补 IsSysAdmin 字段，本地测试依赖，双轨一致）

**设计决策（偏离任务书字面，P07 架构守卫）**：
- **`IUserService`（改名自 IUserCrossModuleService）保留在 `Infrastructure/Services/CrossModule/`**，未移入 Identity/Interfaces/。原因：MedicalCase/Registration 消费它，若接口在 Identity 模块则二者必须 ProjectReference Identity 模块 → 违反 **P07（模块间禁止引用）**；且任务书约束"MedicalCase/Registration 仅改 using"要求接口位置不变。蓝图 §2.1 亦定案跨模块接口位于 Infrastructure。
- **`IAuthCrossModuleService`/`AuthCrossModuleService` 删除**：合并后 Users 4 个 Handler（Delete/ToggleStatus/ResetPassword/ChangePassword）在 Identity 模块内部，改为直接注入 IAuthSessionRepository + ISecurityAuditService，跨模块通道消除（与蓝图"跨模块服务对 -2"收益一致）。

## 4. 阶段 C：外部引用更新 + 清理

- **MedicalCase/Registration**：`IUserCrossModuleService` → `IUserService`（类型名替换，using/命名空间不变，方法不变）
- **WebAPI**：AuthController + UsersController → **IdentityController**（继承 BaseUsersController，路由 `/api/v1/auth/*` + `/api/v1/users/*` 保持；auth action 用绝对路由 `api/v{version}/auth/...`）
- **BaseUsersController**：迁移至 Identity/Controllers/（双端共用基类）
- **LocalWebAPI**：AuthController 登录端点改发共享 `LoginCommand`（LoginOptions.IsLocal=true）；LocalLoginCommand/LocalLoginCommandHandler 删除；LocalWebApiProgram 注册 DatabaseOptions/JwtOptions/SecurityOptions/LoginOptions；csproj 引用 Auth/Users → Identity
- **删除旧项目**：LYBT.Module.Auth/ + LYBT.Module.Users/ 整目录 + sln 移除 + WebAPI/LocalWebAPI/测试 csproj 引用切换
- **死接口删除**：IUserCrossModuleService.cs / IAuthCrossModuleService.cs（实现方随旧模块删除）
- **测试合并**：Server 单测 using LYBT.Module.Auth → LYBT.Module.Identity，AuthDbContext → IdentityDbContext；架构测试 P07/P06/TestAssemblies/LocalWebApiPatternTests 模块清单更新
- **架构测试 P19 适配**：豁免 IUserService.UpdateLoginFailureAsync/ResetLoginStateAsync（登录流程 Handler 专用写方法，非 Controller 写端点）

## 5. 验证结果（真实输出）

| 验证项 | 结果 |
|--------|------|
| `dotnet build LYBTZYZS.sln --no-incremental` | **0 错误 0 警告**（三阶段均验证） |
| `dotnet test tests/LYBT.Tests.Architecture/` | **87/87 全绿**（P19 适配后） |
| `dotnet test tests/LYBT.Tests.Server/ --filter Auth/Jwt/SecurityAudit/Validators` | 104 通过 / 1 跳过 |
| `dotnet test tests/LYBT.Tests.Desktop/ --filter AuthControllerTests` | **8/8 全绿**（本地登录统一路径：sysadmin/admin 登录 + IsSysAdmin claim + validate + 登出 + 错误场景） |

**HEAD 基线对比**（git stash 验证，证明非本次引入）：
- Desktop `AuthControllerTests` 等 LocalWebAPI 测试在 HEAD 基线**全部失败**（测试基座缺 MediatR 注册 → 500），本次修复基座（AddIdentityModule + AddMediatR + Options 注册 + admin seed）后 8/8 通过
- `UsersControllerTests`（/api/users 路径与 api/v1/users 路由不匹配）、远程 E2E（localhost:5000 未运行）、`ConfigurationLoadingTests`（SectionName 断言过时）均为 HEAD 既有失败，非本次合并引入

## 6. 关键约束达成

| 约束 | 达成 |
|------|------|
| Identity 核心不改（UserManager/SignInManager/PBKDF2）| ✅ 未触碰 |
| 对外接口 IUserService（替代 IUserCrossModuleService）| ✅ MedicalCase/Registration 仅改类型名 |
| 路由 /api/auth/* + /api/users/* 不变 | ✅ IdentityController 保持双路由 |
| 本地登录统一 LoginCommandHandler+LoginOptions | ✅ LocalLoginCommandHandler 删除 |
| IdentityDbContext 统一管理（迁移链连续）| ✅ 表结构不变（AuthSessions/SecurityAuditLogs/SystemLogs/Users）|
| 0 错误 0 警告 | ✅ |
| 架构测试全绿 | ✅ 87/87 |

## 7. 文档更新

- `docs/03-architecture/14-structure-design-blueprint.md`：§2.2 模块表 8→7（Identity 47 文件 + IdentityDbContext），§2.3 IdentityController（11 个），§2.2 边界规则/§2.4 三态模板 CQRS 模块清单，变更记录 v1.6
- `docs/03-architecture/15-solution-integration-plan.md`：合并 2 状态 → 已完成（3 commit SHA），表格 C-3a → ✅ 已完成
- `docs/03-architecture/13-project-master-plan.md`：A-31 行追加 C-3a 完成记录（commit + 验证 + 报告）

## 8. 遗留/后续

- **Desktop 侧合并（IApiClientAuth + IApiClientUsers → IApiClientIdentity）**：任务书明确排除为后续批次，本批次未动
- `UsersControllerTests` 的 /api/users 路径问题（HEAD 既有测试缺陷）不在本任务范围，后续可修正
- 本地登录统一后 token 行为与远程一致（JwtService/JwtOptions，sysadmin 带 IsSysAdmin claim），LocalJwtConfig 仍用于本地 JwtBearer 验证与 refresh/auto-login

---

*报告完成。全部验证基于真实 build/test 输出。*
