---
feature: b-21-security-enhancement
status: delivered
specs: []
plans:
  - docs/compose/plans/2026-08-05-v1.0-completion-plan.md
branch: master
commits: e2cedf6a2..2dc002e4f
---

# B-21 安全增强（Token 族旋转 + 安全审计）— Final Report

## What Was Built

补全两个 v1.0 安全需求：**Token 族旋转（US-AUTH-006）** 与 **安全审计（US-AUTH-007）**。

- **Token 族旋转**：新增批量撤销用户全部有效会话的能力。`IAuthSessionRepository.RevokeAllUserSessionsAsync` 将用户所有活跃会话标记为撤销（`IsRevoked=true`、`RevokedReason`、`LogoutTime`）。新增 `RevokeAllUserTokensCommand`/Handler（Auth 模块 MediatR 命令，撤销 + 记录审计事件 `AllTokensRevoked`）。**登录踢出**：每次成功登录先撤销该用户全部旧会话，旧设备的 RefreshToken 立即失效。
- **安全审计完善**：Users 模块 4 个操作现在记录审计日志——删除用户（`UserDeleted`）、重置密码（`PasswordReset`）、修改密码（`PasswordChanged`）、禁用/启用（`UserStatusChanged`），且删除/重置/修改/禁用时同步撤销目标用户全部会话。审计写入 `SecurityAuditLogs` 表，沿用 `ISecurityAuditService.RecordEventAsync`。

## Architecture

**数据流（以删除用户为例）**：`DeleteUserCommandHandler`（Users 模块）→ `IAuthCrossModuleService.RevokeAllUserSessionsAsync` + `RecordSecurityAuditAsync`（Auth 模块实现）→ `AuthSessionRepository`（AuthDbContext 的 `AuthSessions` 表）+ `SecurityAuditService`（AppDbContext 的 `SecurityAuditLogs` 表）。

关键文件：

| 文件 | 角色 |
|------|------|
| `src/Shared/LYBT.Entities/Auth/AuthSessionModel.cs` | `AuthSession` 实体新增 `RevokedReason` + `Revoke(string reason)` |
| `src/Server/Core/LYBT.Infrastructure/Migrations/20260806001815_AddRevokedReasonToAuthSessions.cs` | 新增 `AuthSessions.RevokedReason` nvarchar(256) 列 |
| `src/Server/Modules/LYBT.Module.Auth/Interfaces/IAuthSessionRepository.cs` + `Infrastructure/AuthSessionRepository.cs` | `RevokeAllUserSessionsAsync`（批量撤销活跃会话，单次 SaveChanges） |
| `src/Server/Modules/LYBT.Module.Auth/Application/Commands/RevokeAllUserTokensCommand.cs` + `RevokeAllUserTokensCommandHandler.cs` | 撤销 + 审计（`AllTokensRevoked`） |
| `src/Server/Modules/LYBT.Module.Auth/Application/Commands/LoginCommandHandler.cs` | 登录踢出：新登录先发 `RevokeAllUserTokensCommand` |
| `src/Server/Core/LYBT.Infrastructure/Services/CrossModule/IAuthCrossModuleService.cs` + `src/Server/Modules/LYBT.Module.Auth/Services/AuthCrossModuleService.cs` | 跨模块服务（Users → Auth），AuthModule 注册 |
| `src/Shared/LYBT.Shared.Models/Contracts/Auth/SecurityAuditEvent.cs` | 审计 DTO 从 Auth 模块 Models 移入 Shared（跨模块共享） |
| `src/Server/Modules/LYBT.Module.Users/Application/Commands/*`（4 个 Handler） | 删除/重置密码/修改密码/禁用 → 撤销 + 审计 |

**模块边界**：Users 模块不引用 Auth 模块。跨模块通信走 `IAuthCrossModuleService`（接口定义于 `LYBT.Infrastructure.Services.CrossModule`，镜像既有 `IUserCrossModuleService` 模式），由 Auth 模块实现并注册——架构测试 92/92 验证边界未破坏。

### Design Decisions

- **跨模块机制选 ICrossModuleService 而非 MediatR 命令直达**：任务描述为"在现有 CommandHandler 中调用 [Auth 的 Command]"，但模块禁止互相引用，Users 模块无法直接发送 Auth 模块的 MediatR 命令。故新增 `IAuthCrossModuleService`（Infrastructure 接口 + Auth 实现），Users Handler 经其触发撤销与审计。
- **`SecurityAuditEvent` 移入 Shared**：跨模块接口的参数类型必须可被 Core/Infrastructure 引用；DTO 归位 `LYBT.Shared.Models.Contracts.Auth` 符合"所有 DTO 在 Shared"惯例。
- **登录踢出为无条件**：每次登录即撤销该用户全部旧会话（单一活跃会话策略），符合任务"新设备登录时撤销旧设备"的表述。
- **`RevokedReason` 新增列而非复用现有字段**：`AuthSessions` 表原无撤销原因列（`RevokedReason` 仅存在于 `AutoLoginTokens`），按任务要求新增 nvarchar(256) 可空列。

## Usage

无新增 API 端点。行为触发点：

| 场景 | 位置 | 效果 |
|------|------|------|
| 新登录 | `LoginCommandHandler` | 撤销旧会话 + 审计 `AllTokensRevoked` |
| 删除用户 | `DeleteUserCommandHandler` | 撤销会话 + 审计 `UserDeleted` |
| 重置密码（管理员） | `ResetPasswordCommandHandler` | 撤销会话 + 审计 `PasswordReset` |
| 修改密码（本人） | `ChangePasswordCommandHandler` | 撤销会话 + 审计 `PasswordChanged` |
| 禁用用户 | `ToggleUserStatusCommandHandler` | 撤销会话（仅禁用时）+ 审计 `UserStatusChanged` |

数据库迁移：`dotnet ef database update`（新迁移 `20260806001815_AddRevokedReasonToAuthSessions`）。

**未实现场景**：角色变更（`UpdateRoleCommandHandler`）。服务端不存在角色修改操作（`UserService.UpdateAsync` 走 `UpdateProfile`，不修改 `Role`；角色仅在创建时指定），新增角色修改功能超出本任务范围，已记录待产品决策。

## Verification

- `dotnet build LYBTZYZS.sln --no-incremental` → **0 错误 0 警告**
- `dotnet test tests/LYBT.Tests.Architecture/` → **92/92 通过**（含模块隔离/AntiMock 守卫）
- 新单元测试 `tests/LYBT.Tests.Server/Unit/Auth/` → **8/8 通过**（`AuthSessionTests`、`AuthSessionRepositoryTests`、`SecurityAuditServiceTests`、`RevokeAllUserTokensCommandHandlerTests`；EF InMemory 真实实现零 mock，遵循 `Unit/Repositories` 既有模式）
- `dotnet ef migrations has-pending-model-changes` → 无差异
- LocalDB 全新库完整迁移链应用成功（含新迁移），验证后测试库已删除

## Journey Log

- [lesson] `dotnet ef migrations add --no-build` 会用启动项目 bin 中的**旧二进制**生成空迁移——必须先构建，去掉 `--no-build` 让 ef 工具自行构建启动项目。
- [pivot] 任务清单中的 `UpdateRoleCommandHandler.cs` 实际不存在（服务端无角色修改操作），该触发场景无法挂载，改为如实记录。
- [lesson] EF 工具重写快照文件会引入 BOM 伪差异，提交前需剥离保持 diff 干净。

## Source Materials

| File | Role | Notes |
|------|------|-------|
| `docs/compose/plans/2026-08-05-v1.0-completion-plan.md` | 实施计划 | B-21 部分已交付，B-23 待实现 |
| `docs/03-architecture/13-project-master-plan.md` | 项目总账 | §八 状态 + §九 决策已更新 |
