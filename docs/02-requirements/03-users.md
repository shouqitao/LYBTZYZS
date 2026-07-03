# 用户管理 (User Management)

> 版本: v3.0 | 日期: 2026-06-20 | 状态: 已重构

## 模块概述

用户管理模块采用标准 ASP.NET Core Identity 架构。单一用户模型 `ApplicationUser : IdentityUser<Guid>` 同时承载认证（Identity 内置）和业务字段（Role、Status、PinYinCode 等）。Remote 和 Local 两端通过同一套 `IUserManagerService` 接口操作同一数据模型，零重复实现。

## 核心架构

```
ApplicationUser : IdentityUser<Guid>, IAuditableEntity, ISoftDeletable
  ├── Identity 字段: UserName, Email, PhoneNumber, PasswordHash, LockoutEnd, AccessFailedCount, SecurityStamp
  ├── 业务扩展: RealName, Role (UserRole enum), Status (CommonStatus), PinYinCode, Remark
  ├── 审计字段: CreatedAt, UpdatedAt, CreatedBy, UpdatedBy (via IAuditableEntity)
  └── 软删除: IsDeleted, RowVersion (via ISoftDeletable)

IUserManagerService → UserManagerService → UserManager<ApplicationUser> (Identity)

Remote: UsersController → IUserManagerService → UserManager → AppDbContext → SQL Server
Local:  UsersController → IUserManagerService → UserManager → AppDbContext → LocalDB
```

**双模式差异仅在配置层：**
| 配置项 | 远程 (Remote) | 本地 (Local) |
|--------|-------------|-------------|
| 密码策略 | 8位+大小写+数字+特殊字符 | 同左（统一 Service 层） |
| 锁定策略 | 可配置（默认5次失败锁定15分钟） | 同左（统一 Service 层） |
| JWT 有效期 | Access 60 分钟 / Refresh 7 天 | 365 天 |
| SecurityStamp | 启用 | 未配置 |

> 密码策略与锁定策略远程/本地一致（与 [02-auth.md](02-auth.md) AUTH-002、[12-nfr.md](12-nfr.md) NFR-SEC-002 统一）。

## 权限模型

**4 级角色**：`Receptionist(0)` < `Doctor(1)` < `Admin(10)` < `SuperAdmin(100)`

**2 条授权策略**（Phase 1 从 4 条简化）：
| 策略 | 允许角色 | 用途 |
|------|---------|------|
| `DoctorOrReceptionist` | 所有4角色 | 患者/医案/挂号/药材/验方 |
| `AdminOrSuperAdmin` | Admin + SuperAdmin | 用户管理/系统设置/报表 |

**控制器层权限控制**（CanManageUser 逻辑）：
```
SuperAdmin → 可管理所有角色
Admin → 可管理 Doctor + Receptionist，不可管理 Admin/SuperAdmin
Doctor/Receptionist → 不可管理任何角色
```

## 权限矩阵

### 认证端点 (AuthController)

| 端点 | Receptionist | Doctor | Admin | SuperAdmin | 未登录 |
|------|:---:|:---:|:---:|:---:|:---:|
| `POST /auth/login` | ✅ | ✅ | ✅ | ✅ | ✅ |
| `POST /auth/logout` | ✅ | ✅ | ✅ | ✅ | ❌ |
| `GET /auth/validate` | ✅ | ✅ | ✅ | ✅ | ✅ |

### 用户管理端点 (UsersController)

| 端点 | Receptionist | Doctor | Admin | SuperAdmin | 策略 |
|------|:---:|:---:|:---:|:---:|------|
| `GET /users` (分页列表) | ❌ | ❌ | ✅ | ✅ | AdminOrSuperAdmin |
| `GET /users/current` (当前用户) | ✅ | ✅ | ✅ | ✅ | 仅本人 |
| `GET /users/{id}` (用户详情) | ❌ | ❌ | ✅ | ✅ | AdminOrSuperAdmin |
| `POST /users` (创建用户) | ❌ | ❌ | ✅ | ✅ | AdminOrSuperAdmin |
| `PUT /users/{id}` (更新用户) | ❌ | ❌ | ✅ | ✅ | AdminOrSuperAdmin |
| `DELETE /users/{id}` (删除用户) | ❌ | ❌ | ✅ | ✅ | AdminOrSuperAdmin |
| `POST /users/{id}/reset-password` (重置密码) | ❌ | ❌ | ✅ | ✅ | AdminOrSuperAdmin |
| `PUT /users/{id}/profile` (修改资料) | ✅* | ✅* | ✅* | ✅* | 仅本人 |
| `PUT /users/{id}/change-password` (修改密码) | ✅* | ✅* | ✅* | ✅* | 仅本人 |
| `POST /users/{id}/toggle-status` (启用/禁用) | ❌ | ❌ | ✅ | ✅ | AdminOrSuperAdmin |
| `POST /users/batch-delete` (批量删除) | ❌ | ❌ | ✅ | ✅ | AdminOrSuperAdmin |

> `*` 标注：profile/change-password 仅限本人操作（IDOR 防护：`id == currentUserId`），Admin 也无法修改他人资料。

### 业务端点权限概览

| 模块 | Receptionist | Doctor | Admin | SuperAdmin |
|------|:---:|:---:|:---:|:---:|
| 患者管理 | ✅ CRUD | ✅ CRUD | ✅ CRUD | ✅ CRUD |
| 挂号 | ✅ CRUD | ✅ 查看 | ✅ CRUD | ✅ CRUD |
| 读卡器 | ✅ | ❌ | ❌ | ❌ |
| 医案（创建） | ❌ | ✅ | ❌ | ❌ |
| 医案（查看/编辑） | ❌ | ✅ 自己的 | ✅ 所有 | ✅ 所有 |
| 药材管理 | ✅ 查看 | ✅ 查看 | ✅ CRUD | ✅ CRUD |
| 验方管理 | ✅ 查看 | ✅ 查看 | ✅ CRUD | ✅ CRUD |
| 用户管理 | ❌ | ❌ | ✅ | ✅ |
| 系统设置 | ❌ | ❌ | ❌ | ✅ |
| 报表 | ❌ | ❌ | ✅ | ✅ |
| 系统诊断 | ❌ | ❌ | ❌ | ✅ |
| 日志级别 | ❌ | ❌ | ❌ | ✅ |

## API 端点（11 个，Remote 和 Local 统一）

| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| `GET /users` | 分页查询 | AdminOrSuperAdmin | 支持 keyword/role/status 筛选 |
| `GET /users/current` | 当前用户 | 所有角色 | JWT Claims 提取 userId |
| `GET /users/{id}` | 用户详情 | AdminOrSuperAdmin | 含角色信息 |
| `POST /users` | 创建用户 | AdminOrSuperAdmin | 密码默认见 `appsettings:DefaultPasswords` |
| `PUT /users/{id}` | 更新用户 | AdminOrSuperAdmin | UserName 不可改 |
| `DELETE /users/{id}` | 删除用户 | AdminOrSuperAdmin | 软删除，不可删自己 |
| `POST /users/{id}/reset-password` | 重置密码 | AdminOrSuperAdmin | 返回临时密码 |
| `PUT /users/{id}/profile` | 修改资料 | 本人 | IDOR 防护 |
| `PUT /users/{id}/change-password` | 修改密码 | 本人 | IDOR 防护 |
| `POST /users/{id}/toggle-status` | 启用/禁用 | AdminOrSuperAdmin | Lockout 机制 |
| `POST /users/batch-delete` | 批量删除 | AdminOrSuperAdmin | 逐项检查权限 |

## Desktop 端 UI

| 视图 | 角色 | 功能 |
|------|------|------|
| UserMasterDetailControl | Admin | 分页列表 + 详情 + 新建/编辑/删除 |
| UserEditControl | Admin | 表单：用户名、姓名、角色、状态、密码 |
| UserViewControl | Admin | 只读详情展示 |
| AccountSettingsControl | 所有用户 | 个人资料编辑 + 密码修改 |
| SystemSettingsView | SuperAdmin | 系统级配置 |

## 关键业务规则

1. **用户名不可变**：创建后不可修改
2. **IDOR 防护**：/profile 和 /change-password 验证 `id == currentUserId`
3. **不可删除自己**：删除端点校验 `id != currentUserId`
4. **sysadmin 保护**：系统管理员账号不可修改/删除/禁用
5. **密码默认值**：新用户创建时默认密码见 `appsettings.json:DefaultPasswords`（开发环境：`admin/Admin@123456`、`sysadmin/SysAdmin@2026!`；生产环境由 `DefaultPasswordService.GetOrGeneratePassword()` 随机生成）
6. **角色层级**：Admin 不能创建/修改 SuperAdmin；Doctor/Receptionist 不能管理任何角色
7. **保留用户名**：admin, administrator, root, system, superadmin, sysadmin

## 边界条件验收标准

### 并发会话处理

- [ ] 同一用户在多个终端同时登录 → 允许（多会话共存，JWT 无状态）
- [ ] 用户被管理员禁用后 → 已登录会话的 JWT 在到期前仍有效（本地模式无 Lockout 策略，远程模式 LockoutEnd 到期后下次请求拒绝）

### 禁用用户中断操作

- [ ] 用户在操作过程中被管理员禁用 → 当前请求完成处理，后续请求返回 403/401
- [ ] 已禁用用户调用 `PUT /users/{id}/change-password` → 返回 403（Identity Lockout 机制）
- [ ] 已禁用用户调用 `PUT /users/{id}/profile` → 返回 403（Identity Lockout 机制）

## 数据流

```
Desktop UI → IUserRepository (HTTP) → SwitchingApiClient
  ├─ Remote → RefitApiClient → /api/v1/users/*
  └─ Local  → HttpClientApiClient → /api/users/*

Server/Local → UsersController → IUserManagerService
  └→ UserManagerService → UserManager<ApplicationUser> → AppDbContext → EF Core → SQL Server/LocalDB
```

## 文件清单

| 文件 | 层 | 说明 |
|------|---|------|
| `ApplicationUser.cs` | Entities | 用户实体（IdentityUser + 业务字段） |
| `IUserManagerService.cs` | Module.Users | 用户管理服务接口 |
| `UserManagerService.cs` | Module.Users | UserManager 包装实现 |
| `UsersController.cs` | WebAPI + LocalWebAPI | 11 个 REST 端点 |
| `UserManagerServiceTests` | Tests | 服务层测试 |
| `US_Auth_MustHaveTests` | Tests | 认证必须功能测试 |
| `US_User_MustHaveTests` | Tests | 用户管理必须功能测试 |
| `US_User_ShouldHaveTests` | Tests | 用户管理建议功能测试 |

## 已知问题（2026-06-28 审计）

| 问题 | 严重度 | 说明 |
|------|:---:|------|
| **分页+筛选 TotalCount 错误** | 🟠 | `GetList` 在内存执行 role/status 筛选（L73-74），但 TotalCount 基于筛选前计数（L58），导致前端分页数量不一致 |
| **Restore 完全缺失** | 🔴 | Desktop `ExecuteRestoreAsync` 返回 null，Server 无端点；测试类仍引用但无实现 |
| **CreatedAt 始终 MinValue** | ⚠️ | `MapToDetailDtoAsync`（L605-606）写死 `DateTime.MinValue`，未映射实际创建时间 |
| **UpdatedAt 始终 null** | ⚠️ | `ListDto.CreatedAt` 同样写死 MinValue |

## 用户故事

### US-USER-001: 分页查询用户列表

**角色**: Admin / SuperAdmin
**优先级**: Must
**状态**: ⚠️ 部分实现（TotalCount 内存筛选 bug）

**作为** 管理员，**我想要** 分页查询用户列表（支持关键字/角色/状态筛选），**以便** 高效管理用户。

**验收标准**:
- [ ] 支持 keyword/role/status 筛选 + 分页
- [ ] 返回 TotalCount 与筛选后一致
- [ ] 策略：`AdminOrSuperAdmin`

---

### US-USER-002: 查看用户详情

**角色**: Admin / SuperAdmin
**优先级**: Must
**状态**: ⚠️ 部分实现（CreatedAt 始终 MinValue）

**作为** 管理员，**我想要** 查看单个用户完整信息（含角色），**以便** 了解用户配置。

**验收标准**:
- [ ] 返回含角色信息的详情 DTO
- [ ] CreatedAt/UpdatedAt 正确映射

---

### US-USER-003: 查看当前用户资料

**角色**: 所有用户
**优先级**: Must
**状态**: ✅ 已实现

**作为** 用户，**我想要** 查看自己的资料，**以便** 确认个人信息。

**验收标准**:
- [ ] JWT Claims 提取 userId 返回当前用户资料
- [ ] 所有角色可访问（仅本人）

---

### US-USER-004: 创建用户

**角色**: Admin / SuperAdmin
**优先级**: Must
**状态**: ✅ 已实现

**作为** 管理员，**我想要** 创建新用户并指定角色，**以便** 为员工分配系统账号。

**验收标准**:
- [ ] 用户名唯一 + 保留用户名校验
- [ ] 默认密码见 `appsettings:DefaultPasswords`
- [ ] 受 USER-D04 层级规则约束

---

### US-USER-005: 更新用户（用户名不可变）

**角色**: Admin / SuperAdmin
**优先级**: Must
**状态**: ✅ 已实现

**作为** 管理员，**我想要** 更新用户信息（UserName 不可改），**以便** 维护准确的人员信息。

**验收标准**:
- [ ] UserName 创建后不可修改
- [ ] 角色变更受层级规则约束
- [ ] sysadmin 不可修改

---

### US-USER-006: 删除用户（软删除，不可删自己）

**角色**: Admin / SuperAdmin
**优先级**: Must
**状态**: ✅ 已实现

**作为** 管理员，**我想要** 软删除用户（不可删自己、不可删 sysadmin），**以便** 离职员工数据可追溯但不可登录。

**验收标准**:
- [ ] 软删除（`IsDeleted=true`）
- [ ] 校验 `id != currentUserId`
- [ ] sysadmin 拒绝删除

---

### US-USER-007: 重置用户密码（SuperAdmin）

**角色**: SuperAdmin / sysadmin
**优先级**: Must
**状态**: ✅ 已实现

**作为** 管理员，**我想要** 重置用户密码为临时密码，**以便** 用户忘记密码时可恢复访问。

**验收标准**:
- [ ] 返回临时密码
- [ ] 受层级规则约束
- [ ] sysadmin 密码不可由他人重置

---

### US-USER-008: 修改个人资料（IDOR 防护）

**角色**: 所有用户
**优先级**: Must
**状态**: ✅ 已实现

**作为** 用户，**我想要** 修改自己的个人资料（不可改他人），**以便** 保持信息准确。

**验收标准**:
- [ ] IDOR 防护：`id == currentUserId`
- [ ] UserName 不可改

---

### US-USER-009: 修改密码（需旧密码）

**角色**: 所有用户
**优先级**: Must
**状态**: ✅ 已实现

**作为** 用户，**我想要** 修改自己的密码（需验证旧密码），**以便** 安全地更换密码。

**验收标准**:
- [ ] 需提供旧密码验证
- [ ] 新密码符合策略（8位+大小写+数字+特殊字符）
- [ ] IDOR 防护：仅本人

---

### US-USER-010: 启用/禁用用户

**角色**: Admin / SuperAdmin
**优先级**: Must
**状态**: ✅ 已实现

**作为** 管理员，**我想要** 启用/禁用用户账号，**以便** 临时停权而不删除。

**验收标准**:
- [ ] 通过 Identity Lockout 机制
- [ ] sysadmin 不可禁用
- [ ] 医生有名下 Waiting 挂号时阻止禁用（REG-BR-006）

---

### US-USER-011: 恢复软删除用户（SuperAdmin）

**角色**: SuperAdmin / sysadmin
**优先级**: Should
**状态**: ✅ 已实现

**作为** 管理员，**我想要** 恢复软删除的用户，**以便** 误删后可还原。

**验收标准**:
- [ ] `POST /users/{id}/restore` 端点
- [ ] 仅 SuperAdmin 可执行（Admin 不可，与删除权限区分以避免越权还原）
- [ ] 恢复后用户 `IsDeleted=false`，状态回 Active（LockoutEnd 清除，可登录）
- [ ] 关联历史医案归属保留不变（CreatedBy/DoctorName 字段不重写，审计链完整）
- [ ] 已硬删除用户（物理删除）不可恢复 → 返回 404（ERR-USER-404）
- [ ] 恢复操作写审计日志（记录操作人/时间/目标用户 ID/原因）
- [ ] 🚧 v1.0 补回（D4 决策）

---

### US-USER-012: 批量操作（删除/启用/禁用）

**角色**: Admin / SuperAdmin
**优先级**: Should
**状态**: ✅ 已实现

**作为** 管理员，**我想要** 批量删除/启用/禁用用户，**以便** 高效管理多个账号。

**验收标准**:

**批量删除**:
- [ ] 逐项权限检查（受 USER-D04 层级规则约束，Admin 不可删 SuperAdmin）
- [ ] sysadmin 账号自动跳过且不计入失败数
- [ ] 不可删除自己（`id == currentUserId` 该项跳过）
- [ ] 部分失败 → 已成功项不回滚，返回 `{successIds, failedIds, skippedIds}` 结构
- [ ] 单次上限 100 条，超出返回 400
- [ ] 每条删除写审计日志

**批量启用**:
- [ ] 逐项权限检查（AdminOrSuperAdmin 策略）
- [ ] sysadmin 账号自动跳过
- [ ] 清除 LockoutEnd、`AccessFailedCount=0` → 用户恢复可登录
- [ ] 部分失败 → 返回成功/失败/跳过 ID 列表
- [ ] 每条启用写审计日志

**批量禁用**:
- [ ] 逐项权限检查（AdminOrSuperAdmin 策略）
- [ ] sysadmin 账号自动跳过（保护系统账号）
- [ ] 医生名下有 `Waiting` 挂号时该条目跳过并提示（REG-BR-006）
- [ ] 设置 `LockoutEnd=max` → 用户不可登录，已签发 JWT 到期前仍有效
- [ ] 部分失败 → 返回成功/失败/跳过 ID 列表
- [ ] 每条禁用写审计日志

---

## 变更日志

| 日期 | 变更 | 原因 |
|------|------|------|
| 2026-06-28 | v3.1 | 文档对齐：默认密码改为引用 `appsettings:DefaultPasswords`；本地密码/锁定策略与 auth/nfr 统一；补 12 个 US-USER 故事块 | 文档一致性修复 |
| 2026-06-28 | v3.2 | US-USER-011 验收从 3 条扩展至 6 条（状态/归属/权限/硬删/审计）；US-USER-012 验收拆分删除/启用/禁用三组独立条件 | plan Task 7 边缘 US 修正 |
| 2026-06-25 | 补充边界条件验收标准（并发会话、禁用用户中断操作） | 需求文档验收标准完善 |
| 2026-06-20 | 从 v2.0 重写为 v3.0 | 用户模块重构：统一到 Identity，删除 User 实体 |
| 2026-06-15 | v2.0 重建 | Phase 1 简化后重建 |
| 2026-06-08 | v1.0 初始 | 初始需求文档 |
