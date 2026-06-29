# Users 模块设计

> 日期: 2026-06-29
> US 数量: 12 (PRD)
> 复杂度: 5/10
> 状态: 草稿

## 模块概述

Users 管理系统用户，支持四级角色权限、批量操作、sysadmin 保护。

**职责边界**:
- 用户的 CRUD、搜索、批量操作
- 角色管理（Receptionist/Doctor/Admin/SuperAdmin）
- 密码重置/修改
- Sysadmin 账户保护（不可删/禁/改）

**依赖关系**:
- 上游: 无（基础模块）
- 下游: Auth（认证时查询用户信息）、所有业务模块（权限校验）
- 使用 ASP.NET Core Identity

**关键 US 清单**:
USER-001 ~ USER-012（详见 PRD）

## 接口契约

### Server 端

无独立 Service 层。UsersController 直接使用 IUserManagerService（Identity 封装）。

### API 端点映射

| HTTP | 路由 | 方法 | 权限 |
|------|------|------|------|
| GET | `/api/v1/users` | GetList | AdminOrSuperAdmin |
| GET | `/api/v1/users/current` | GetCurrent | 任何已认证 |
| GET | `/api/v1/users/{id}` | GetById | AdminOrSuperAdmin |
| POST | `/api/v1/users` | Create | AdminOrSuperAdmin |
| PUT | `/api/v1/users/{id}` | Update | AdminOrSuperAdmin |
| DELETE | `/api/v1/users/{id}` | Delete | AdminOrSuperAdmin |
| POST | `/api/v1/users/{id}/reset-password` | ResetPassword | AdminOrSuperAdmin |
| PUT | `/api/v1/users/{id}/profile` | UpdateProfile | 任何已认证（仅自己） |
| PUT | `/api/v1/users/{id}/change-password` | ChangePassword | 任何已认证（仅自己） |
| POST | `/api/v1/users/{id}/toggle-status` | ToggleStatus | AdminOrSuperAdmin |
| POST | `/api/v1/users/batch-delete` | BatchDelete | AdminOrSuperAdmin |

### 角色层级

```
SuperAdmin (100) ── 可管理所有人
    │
Admin (10) ── 可管理 Doctor + Receptionist
    │
Doctor (1) ── 不可管理其他用户
Receptionist (0) ── 不可管理其他用户
```

**Sysadmin 特殊规则**:
- `IsSysAdmin=true` 标记，非角色
- 不可删除、禁用、修改（Controller 多处守卫）
- Desktop 端 `UserItem.CanDelete` 硬编码 "sysadmin" 检查

### DTO 结构

```
UserInputDto
├── UserName: string（必填，Regex [a-zA-Z0-9_]+，3-32字）
├── Password: string?（可选，服务器默认）
├── ConfirmPassword: string?
├── RealName: string
├── PinYinCode: string?（自动生成）
├── PhoneNumber: string?
├── Email: string?
├── Role: UserRole
├── Remark: string?
└── Id: Guid?（null=创建，有值=更新）

UserDetailDto: 所有字段 + FailedLoginCount, UpdatedAt, CreatedAt
UserListDto: Id, UserName, RealName, PhoneNumber, Role, Status, IsEnabled, LastLoginTime, CreatedAt
```

## 数据流

### 创建用户
```
Desktop → POST /api/v1/users
Controller → 检查保留用户名（admin, sysadmin 等）
  → 检查调用者可管理目标角色
  → UserManager.CreateAsync（带密码）
  → 分配 Identity 角色
  → 返回 UserDetailDto
```

### 重置密码
```
Desktop → POST /api/v1/users/{id}/reset-password
Controller → 检查 Sysadmin 保护
  → 生成临时密码（从配置读取）
  → UserManager.ResetPasswordAsync
  → 设置 MustChangeOnNextLogin=true
  → 返回 ResetPasswordResponseDto（含临时密码）
```

## 异常处理

| 异常类型 | 场景 | 处理 |
|----------|------|------|
| IdentityResult.Failed | 创建/更新失败 | 返回 400 + 错误消息 |
| BusinessException | 保留用户名 | 返回 400 |
| ForbiddenException | 无权操作目标角色 | 返回 403 |
| NotFoundException | 找不到用户 | 返回 404 |

## 业务规则

| 规则 | 描述 | 与 US 映射 |
|------|------|-----------|
| Sysadmin 保护 | 不可删/禁/改 | USER-010 |
| 角色层级 | SuperAdmin→全部, Admin→Doctor+Receptionist | USER-003 |
| 保留用户名 | admin/sysadmin/root 等不可注册 | USER-002 |
| 强制改密 | MustChangeOnNextLogin 标记 | USER-008 |

## 模块交互

| 交互模块 | 方式 | 方向 |
|----------|------|------|
| Auth | 查询用户信息（登录时） | Auth → Users |
| 所有模块 | 权限校验 | 全局 → Users |

**已知问题**:
- 无独立 Service 层，Controller 直接使用 Identity（违反三层架构）
- GetList 服务端 LINQ-to-Objects 过滤（性能隐患）
- 610 vs 599 行 Controller 重复（WebAPI/LocalWebAPI）
