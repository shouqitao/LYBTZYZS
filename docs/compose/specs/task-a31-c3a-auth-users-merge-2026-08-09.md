# 任务 A-31-C3a：Auth+Users → Identity 模块合并（T2 方案调整）

> 派发对象：Mimo Code
> 版本：v1.0 | 日期：2026-08-09
> 依据：`docs/03-architecture/15-solution-integration-plan.md` §2.1 合并 2 + 调研报告 `auth-users-merge-research-2026-08-09.md`
> 用户决策：**保持 ASP.NET Identity 为核心，不替换框架**；Identity 核心 + AuthSession/SecurityAudit 业务增强层
> **⚠️ T2 方案调整——先文档化（蓝图 + 总账已更新）再改代码**

## 任务

将 `LYBT.Module.Auth` + `LYBT.Module.Users` 合并为 `LYBT.Module.Identity`（认证用户模块），消除跨模块依赖，统一登录路径。

## 范围

- ✅ Server `LYBT.Module.Auth/` + `LYBT.Module.Users/` → 合并为 `LYBT.Module.Identity/`
- ✅ Shared 实体层：`Auth/AuthSessionModel` + `Users/ApplicationUser` → 合并到 Identity 实体
- ✅ Shared 契约：`IUserCrossModuleService` → 改名为 `IUserService`（MedicalCase/Registration 仅改 using）
- ✅ Server 组合根：`AuthModule` + `UsersModule` → 合并为 `IdentityModule`
- ✅ Local 登录：`LocalLoginCommandHandler` → 合并到 `LoginCommandHandler` + `LoginOptions`
- ❌ 排除：Desktop 侧合并（IApiClientAuth+IApiClientUsers→IApiClientIdentity）**后续批次**
- ❌ 排除：Herbs+Formula→Catalog（延期）
- 仓库根：`D:\source\repos\LYBTZYZS`｜分支 `master`

---

## 分阶段执行（3 个子批次，每批独立验证+commit+push）

### 阶段 A：新建 Identity 项目骨架 + 实体合并

1. **新建 `LYBT.Module.Identity/`**（空项目，引用 LYBT.Entities + LYBT.Shared.Models + LYBT.Shared.Configuration）
2. **实体合并**：
   - `AuthSession`（`Auth/AuthSessionModel.cs`）→ `Identity/Entities/AuthSession.cs`（保持表名 `AuthSessions`，属性不变）
   - `SecurityAuditLog`（`Auth/SecurityAuditLog.cs`）→ `Identity/Entities/SecurityAuditLog.cs`（保持表名，属性不变）
   - `ApplicationUser`（`Users/ApplicationUser.cs`）→ **不移动**（已在 Shared/LYBT.Entities/Users/，Identity 模块引用）
3. **DbContext 合并**：
   - 新建 `Identity/Infrastructure/IdentityDbContext.cs`（继承 `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`）
   - 包含 AuthSessions / SecurityAuditLogs / SystemLogs 三个 DbSet
   - OnModelCreating 配置从两个旧 DbContext 迁移过来
4. **sln 更新**：加入 `LYBT.Module.Identity.csproj`

### 阶段 B：接口迁移 + Command/Handler 合并

1. **接口迁移**：
   - `IJwtService` / `IAuthSessionRepository` / `ISecurityAuditRepository` / `ISecurityAuditService` → `Identity/Interfaces/`
   - `IUserService`（**改名**自 `IUserCrossModuleService`，方法签名不变）→ `Identity/Interfaces/`
   - `IUserRepository` → `Identity/Interfaces/`
2. **实现迁移**：
   - `JwtService` / `AuthSessionRepository` / `SecurityAuditRepository` / `SecurityAuditService` → `Identity/Services/` + `Identity/Infrastructure/`
   - `UserService` / `UserRepository` / `IdentitySeedData` → `Identity/Services/` + `Identity/Infrastructure/`
3. **Command/Handler 合并**（17 个 Command，从两个旧模块迁入）：
   - Auth 6 个（Login/AutoLogin/Logout/Refresh/RevokeAll/ValidateToken）
   - Users 11 个（CRUD/Password/Batch/Status/Profile）
4. **Mapper 合并**：`AuthUserMapper` + `UserMapper` + `UserCrossModuleMapper` → **单一 `IdentityMapper`**
5. **Validator**：Users 原有 Validators 迁入（ChangeProfile/CreateUser/UpdateUser）
6. **DI 注册**：新建 `IdentityModule.cs`（`AddIdentityModule`），注册全部接口/服务/Handler
7. **Local 登录统一**：
   - `LocalLoginCommandHandler` 逻辑合并到 `LoginCommandHandler`
   - 新建 `LoginOptions`：`IsLocal` / `LockoutEnabled` / `AuditLevel`（通过 `IOptions<JwtOptions>` 注入）
   - Remote vs Local 差异通过 `LoginOptions` 控制，不再走两套 Handler

### 阶段 C：外部引用更新 + 清理

1. **MedicalCase/Registration 跨模块引用更新**：
   - `IUserCrossModuleService` → `IUserService`（using 改名，方法名不变）
   - `AuthModule.cs` 中 `AddCrossModuleService` 注册调整
2. **WebAPI Controller 合并**：
   - `AuthController` + `UsersController` → `IdentityController`（路由 `/api/auth/*` + `/api/users/*` 保持不变）
3. **LocalWebAPI 同步**：本地 Controller 同步调整
4. **删除旧项目**：
   - 删除 `LYBT.Module.Auth/` 整个项目目录
   - 删除 `LYBT.Module.Users/` 整个项目目录
   - sln 中移除旧项目引用
   - `AuthModule.cs` / `UsersModule.cs` 删除
5. **测试更新**：Auth + Users 测试合并到 Identity 测试项目

---

## 关键约束

| 约束 | 内容 |
|------|------|
| **Identity 核心不改** | UserManager/SignInManager/PBKDF2 保留不动 |
| **对外接口** | `IUserService`（替代 `IUserCrossModuleService`），MedicalCase/Registration 仅改 using |
| **路由保持** | `/api/auth/*` + `/api/users/*` 路由不变，Controller 内部方法不变 |
| **本地登录** | 统一走 `LoginCommandHandler`+`LoginOptions`，删除 `LocalLoginCommandHandler` |
| **DbContext** | 新 `IdentityDbContext` 统一管理，迁移链连续（不影响现有数据库表） |
| **0 错误 0 警告** | `dotnet build LYBTZYZS.sln --no-incremental` |
| **架构测试** | `dotnet test tests/LYBT.Tests.Architecture/` 全绿 |
| **功能测试** | Auth/Tests + Users/Tests 全部通过（可能需更新 using/注入）|

## 产出

- 3 个阶段独立 commit + push：
  - `refactor(identity): A-31-C3a-A Identity 骨架+实体合并`
  - `refactor(identity): A-31-C3a-B 接口迁移+Command 合并+登录统一`
  - `refactor(identity): A-31-C3a-C 外部引用更新+清理`
- 报告：`docs/compose/reports/a31-c3a-auth-users-merge.md`
