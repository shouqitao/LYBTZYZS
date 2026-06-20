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
| 密码策略 | 8位+大小写+数字+特殊字符 | 6位+小写 |
| 锁定策略 | 5次失败锁定15分钟 | 无限失败不锁定 |
| JWT 有效期 | 7天 | 365天 |
| SecurityStamp | 启用 | 未配置 |

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
| `GET /users` (分页列表) | ❌ | ❌ | ✅ | ✅ | AdminOnly |
| `GET /users/current` (当前用户) | ✅ | ✅ | ✅ | ✅ | 仅本人 |
| `GET /users/{id}` (用户详情) | ❌ | ❌ | ✅ | ✅ | AdminOnly |
| `POST /users` (创建用户) | ❌ | ❌ | ✅ | ✅ | AdminOnly |
| `PUT /users/{id}` (更新用户) | ❌ | ❌ | ✅ | ✅ | AdminOnly |
| `DELETE /users/{id}` (删除用户) | ❌ | ❌ | ✅ | ✅ | AdminOnly |
| `POST /users/{id}/reset-password` (重置密码) | ❌ | ❌ | ✅ | ✅ | AdminOnly |
| `PUT /users/{id}/profile` (修改资料) | ✅* | ✅* | ✅* | ✅* | 仅本人 |
| `PUT /users/{id}/change-password` (修改密码) | ✅* | ✅* | ✅* | ✅* | 仅本人 |
| `POST /users/{id}/toggle-status` (启用/禁用) | ❌ | ❌ | ✅ | ✅ | AdminOnly |
| `POST /users/batch-delete` (批量删除) | ❌ | ❌ | ✅ | ✅ | AdminOnly |

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
| `GET /users` | 分页查询 | AdminOnly | 支持 keyword/role/status 筛选 |
| `GET /users/current` | 当前用户 | 所有角色 | JWT Claims 提取 userId |
| `GET /users/{id}` | 用户详情 | AdminOnly | 含角色信息 |
| `POST /users` | 创建用户 | AdminOnly | 密码默认 Lybt2025@TempPass! |
| `PUT /users/{id}` | 更新用户 | AdminOnly | UserName 不可改 |
| `DELETE /users/{id}` | 删除用户 | AdminOnly | 软删除，不可删自己 |
| `POST /users/{id}/reset-password` | 重置密码 | AdminOnly | 返回临时密码 |
| `PUT /users/{id}/profile` | 修改资料 | 本人 | IDOR 防护 |
| `PUT /users/{id}/change-password` | 修改密码 | 本人 | IDOR 防护 |
| `POST /users/{id}/toggle-status` | 启用/禁用 | AdminOnly | Lockout 机制 |
| `POST /users/batch-delete` | 批量删除 | AdminOnly | 逐项检查权限 |

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
5. **密码默认值**：新用户创建时默认 `Lybt2025@TempPass!`
6. **角色层级**：Admin 不能创建/修改 SuperAdmin；Doctor/Receptionist 不能管理任何角色
7. **保留用户名**：admin, administrator, root, system, superadmin, sysadmin

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

## 变更日志

| 日期 | 变更 | 原因 |
|------|------|------|
| 2026-06-20 | 从 v2.0 重写为 v3.0 | 用户模块重构：统一到 Identity，删除 User 实体 |
| 2026-06-15 | v2.0 重建 | Phase 1 简化后重建 |
| 2026-06-08 | v1.0 初始 | 初始需求文档 |
