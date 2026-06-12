# Users 模块概述

> 用户管理 — RBAC角色体系、密码策略、跨模块令牌撤销

---

## 定位

| 属性 | 说明 |
|------|------|
| 服务端模块 | `LYBT.Module.Users` |
| 桌面模块 | `LYBT.Desktop.Users` |
| 实体 | `User` (继承 `BaseEntity`) |
| 聚合关系 | 被 MedicalCase (`DoctorId`) 和 Auth 模块引用 |
| 权限策略 | `AdminOnly` / `SuperAdminOnly` |

---

## 角色与权限

### 四级角色

| 角色 | 枚举值 | 权限级别 | 管理范围 |
|------|--------|---------|---------|
| Receptionist | 0 | 40 | 仅自助（改密/改资料） |
| Doctor | 1 | 60 | 仅自助 |
| Admin | 10 | 80 | 管理 Doctor + Receptionist |
| SuperAdmin | 100 | 100 | 管理所有用户（除自身sysadmin） |

**权限模型**: `operator.PermissionLevel > target.PermissionLevel` 才能操作目标用户。

### sysadmin 保护 (USER-D05)

- 固定种子账户，不可被任何人管理
- 仅可自助修改密码和个人资料
- `/users/current` 对 SuperAdmin 返回合成 DTO（`Id=Guid.Empty`）

---

## 核心功能

| 功能 | 服务端方法 | 权限 |
|------|-----------|------|
| 分页查询 | `GetPagedAsync(page, pageSize, keyword, role, status)` | AdminOnly |
| 用户详情 | `GetByIdAsync(id)` | AdminOnly |
| 关键字搜索 | `SearchAsync(keyword)` | — |
| 创建用户 | `CreateAsync(dto, currentRole)` | AdminOnly |
| 更新用户 | `UpdateAsync(id, dto, currentRole)` | AdminOnly |
| 删除用户 | `DeleteAsync(id, currentUserId, currentRole)` | AdminOnly |
| 恢复用户 | `RestoreAsync(id, currentRole)` | SuperAdminOnly |
| 重置密码 | `ResetPasswordAsync(id, request)` | SuperAdminOnly |
| 自助改密 | `ChangePasswordAsync(id, oldPassword, newPassword)` | Any authenticated |
| 自助改资料 | `ChangeProfileAsync(userId, dto)` | Any authenticated |
| 启禁用 | `ToggleStatusAsync(id, currentRole)` | AdminOnly |
| 批量删除 | `BatchDeleteAsync(ids, ...)` | AdminOnly |
| 批量启禁用 | `BatchUpdateStatusAsync(ids, status, ...)` | AdminOnly |

---

## 实体字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `UserName` | string(50) | 不可变，唯一 |
| `RealName` | string(50) | 触发拼音码自动生成 |
| `PinYinCode` | string(50)? | 自动生成，快速搜索 |
| `PhoneNumber` | string(20)? | `[SensitiveData]` 日志脱敏 |
| `Email` | string(100)? | `[SensitiveData]` 日志脱敏 |
| `Role` | UserRole | 默认 Doctor |
| `Status` | CommonStatus | Enabled/Disabled |
| `PasswordHash` | string(256) | ASP.NET Identity PasswordHasher |
| `FailedLoginCount` | int | 登录失败计数 |
| `LockoutEnd` | DateTime? | 锁定截止时间 |
| `MustChangeOnNextLogin` | bool | 管理员重置密码后为 true |
| `LastLoginTime` | DateTime? | 最后登录时间 |

---

## 密码策略

- 最少8字符，需包含大小写字母、数字、特殊字符
- 哈希算法: ASP.NET Core Identity `PasswordHasher<User>`
- 管理员重置: 使用配置默认密码或自动生成临时密码
- 重置后强制下次登录改密 (`MustChangeOnNextLogin = true`)

---

## 业务规则

| 规则 | 说明 |
|------|------|
| 保留用户名 | `admin`, `administrator`, `root`, `system`, `superadmin`, `sysadmin` 禁止注册 |
| UserName不可变 | 创建后不可修改 |
| 自删除保护 | 不能删除或禁用自己 |
| 令牌撤销 | 角色/状态/密码变更时触发 `ICrossModuleAuthService.RevokeAllUserTokensAsync()` |
| 软删除 | `IsDeleted=true`，可通过 `RestoreAsync` 恢复 |
| 医生删除 | MedicalCase数据保留（DoctorId不变），管理员手动处理 |
| 批量操作 | 支持部分失败 (`BatchOperationResultDto`) |

---

## 服务端架构

```
UsersController (AdminOnly / SuperAdminOnly)
    │
    ├── IUserService (Facade)
    │   ├── IUserQueryService        — 查询
    │   ├── IUserPasswordService     — 密码操作
    │   ├── IUserStatusService       — 启禁用/恢复
    │   └── IUserBatchOperationService — 批量操作
    │
    └── IUserRepository → UserRepository (internal)
        └── BaseRepository<User> → AppDbContext
```

Mapper: `UserMapper` (Mapperly 编译时源生成)

---

## 桌面架构

```
AdminWorkspace
  └── UserMasterDetailControl (嵌入)
        │
        UserMasterDetailViewModel
          ├── RemoteUserService → IUserRepository → IApiClient
          ├── IUserPasswordHandler → 重置密码对话框
          ├── IUserStatusHandler → 启禁用/恢复
          └── IUserImportExportHandler → Excel导入导出
```

---

## API 端点

| 方法 | 路由 | 权限 | 说明 |
|------|------|------|------|
| GET | `/api/v1/users` | AdminOnly | 分页列表 |
| GET | `/api/v1/users/current` | Any | 当前用户 |
| GET | `/api/v1/users/{id}` | AdminOnly | 用户详情 |
| POST | `/api/v1/users` | AdminOnly | 创建 |
| PUT | `/api/v1/users/{id}` | AdminOnly | 更新 |
| DELETE | `/api/v1/users/{id}` | AdminOnly | 删除 |
| POST | `/api/v1/users/{id}/reset-password` | SuperAdminOnly | 重置密码 |
| PUT | `/api/v1/users/{id}/profile` | Any | 改资料 |
| PUT | `/api/v1/users/{id}/change-password` | Any | 改密码 |
| POST | `/api/v1/users/{id}/toggle-status` | AdminOnly | 启禁用 |
| POST | `/api/v1/users/{id}/restore` | SuperAdminOnly | 恢复 |
| POST | `/api/v1/users/batch-delete` | AdminOnly | 批量删除 |
| POST | `/api/v1/users/batch-enable` | AdminOnly | 批量启用 |
| POST | `/api/v1/users/batch-disable` | AdminOnly | 批量禁用 |

---

## 跨模块关系

| 方向 | 模块 | 接口 | 用途 |
|------|------|------|------|
| 依赖 | Auth | `ICrossModuleAuthService` | 角色/状态变更时撤销令牌 |
| 被依赖 | Auth | `IUserService.ValidatePasswordAsync` | 登录验证 |
| 被依赖 | MedicalCase | `DoctorId` 外键 | 医案关联医生 |
| 被依赖 | Desktop Shell | `IUserRepository` | 自助改密/改资料 |
