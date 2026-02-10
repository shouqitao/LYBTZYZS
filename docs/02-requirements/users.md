# 用户管理 需求规格

## 概述

用户管理模块负责系统用户的创建、维护、状态管理和密码管理。实现四层角色体系 (SuperAdmin > Admin > Doctor > Receptionist) 和严格的权限隔离。

---

## 用户角色

| 角色 | 在本模块中的操作权限 |
|------|---------------------|
| SuperAdmin | CRUD 全部用户，可管理 Admin |
| Admin | CRUD Doctor 和 Receptionist 用户，不可管理 Admin/SuperAdmin |
| Doctor | 仅修改自己的密码和个人资料 |
| Receptionist | 仅修改自己的密码和个人资料 |

> 整个 `/api/v1/users` 端点受 `AdminOnly` 策略保护，Doctor/Receptionist 访问返回 403。

---

## 功能清单

### FR-USER-001: 创建用户

- **描述**: 管理员创建新系统用户
- **业务规则**:
  1. 用户名唯一，仅允许字母、数字、下划线 (3-32 字符)
  2. 系统保留用户名不可使用: admin, administrator, root, system, superadmin, sysadmin
  3. 不提供密码时使用配置默认密码
  4. Admin 只能创建 Doctor/Receptionist，SuperAdmin 可创建任意角色
  5. 自动生成拼音码 (PinYinCode) 用于快速搜索
  6. 默认状态为 Enabled，默认角色为 Doctor
- **远程模式**: POST `/api/v1/users`，返回 UserDetailDto (201)
- **本地模式**: 支持 (LocalUserDataSource)。本地 SQLite 存储，功能与远程模式对等
- **验收标准**:
  - [ ] 用户名重复时返回错误
  - [ ] 保留用户名被拒绝
  - [ ] Admin 创建 Admin 角色时被拒绝
  - [ ] 拼音码自动生成

### FR-USER-002: 查看用户列表

- **描述**: 分页查看用户列表，支持关键词搜索和筛选
- **业务规则**:
  1. 支持按用户名、真实姓名搜索 (keyword)
  2. 支持按角色 (role) 和状态 (status) 筛选
  3. 默认分页: page=1, pageSize=20
  4. 返回 UserListDto (不含敏感信息)
- **远程模式**: GET `/api/v1/users?keyword=&role=&status=&page=&pageSize=`
- **本地模式**: 支持 (LocalUserDataSource)。本地 SQLite 存储，功能与远程模式对等
- **验收标准**:
  - [ ] 分页参数正确
  - [ ] 搜索按用户名和真实姓名匹配

### FR-USER-003: 查看用户详情

- **描述**: 获取单个用户的完整信息
- **业务规则**:
  1. 返回 UserDetailDto (含审计字段，不含密码)
  2. 用户不存在返回 404
- **远程模式**: GET `/api/v1/users/{id}`
- **本地模式**: 支持 (LocalUserDataSource)。本地 SQLite 存储，功能与远程模式对等
- **验收标准**:
  - [ ] 返回完整用户信息
  - [ ] 不返回 PasswordHash

### FR-USER-004: 更新用户信息

- **描述**: 管理员修改用户的基本信息
- **业务规则**:
  1. 用户名创建后不可修改
  2. 真实姓名变更时自动重新生成拼音码
  3. Admin 只能更新 Doctor/Receptionist
  4. 不能修改比自己权限高的角色
- **远程模式**: PUT `/api/v1/users/{id}`
- **本地模式**: 支持 (LocalUserDataSource)。本地 SQLite 存储，功能与远程模式对等
- **验收标准**:
  - [ ] 用户名字段忽略修改
  - [ ] 拼音码随姓名自动更新

### FR-USER-005: 删除用户

- **描述**: 软删除用户 (IsDeleted=true)
- **业务规则**:
  1. 软删除，数据保留
  2. 不能删除自己
  3. 不能删除最后一个 SuperAdmin
  4. 不能删除最后一个 Admin
  5. 删除后所有 Token Family 失效
  6. 清理所有 RefreshToken
  7. 记录审计日志
- **远程模式**: DELETE `/api/v1/users/{id}`
- **本地模式**: 支持 (LocalUserDataSource)。本地 SQLite 存储，功能与远程模式对等
- **验收标准**:
  - [ ] 删除后用户无法登录
  - [ ] 删除最后一个 Admin 被拒绝
  - [ ] 删除自己被拒绝

### FR-USER-006: 恢复已删除用户

- **描述**: 恢复软删除的用户
- **业务规则**:
  1. 查询已删除用户 (IgnoreQueryFilters)
  2. 恢复 IsDeleted=false
  3. 状态恢复为之前的值
- **远程模式**: POST `/api/v1/users/{id}/restore`
- **本地模式**: 支持 (LocalUserDataSource)。本地 SQLite 存储，功能与远程模式对等
- **验收标准**:
  - [ ] 恢复后用户可正常登录
  - [ ] 非已删除用户调用恢复返回错误

### FR-USER-007: 批量删除

- **描述**: 批量软删除多个用户
- **业务规则**:
  1. 逐个检查权限和删除保护
  2. 防止删除自己 (currentUserId 检查)
  3. 返回详细的成功/失败报告 (BatchOperationResultDto)
  4. 部分失败不影响其他用户的删除
- **远程模式**: POST `/api/v1/users/batch-delete`
- **本地模式**: 支持 (LocalUserDataSource)。本地 SQLite 存储，功能与远程模式对等
- **验收标准**:
  - [ ] 返回成功数、失败数和失败原因
  - [ ] 不能批量删除自己

### FR-USER-008: 管理员重置密码

- **描述**: 管理员将用户密码重置为默认密码
- **业务规则**:
  1. 无需提供旧密码
  2. 使用配置文件中的默认密码或自动生成临时密码
  3. 重置后所有 Token Family 失效
  4. 用户需要重新登录
  5. 可设置 MustChangeOnNextLogin 标记
- **远程模式**: POST `/api/v1/users/{id}/reset-password`
- **本地模式**: 支持 (LocalUserDataSource)。本地 SQLite 存储，功能与远程模式对等
- **验收标准**:
  - [ ] 重置后返回临时密码
  - [ ] 旧 Token 全部失效
  - [ ] 用户可使用临时密码登录

### FR-USER-009: 用户修改密码

- **描述**: 用户自行修改密码
- **业务规则**:
  1. 验证旧密码正确
  2. 密码策略: 最小 8 位，必须包含大小写字母、数字、特殊字符
  3. 修改后所有 Token Family 失效
  4. 用户需要重新登录
- **远程模式**: PUT `/api/v1/users/{id}/change-password`
- **本地模式**: 支持 (LocalUserDataSource)。本地 SQLite 存储，功能与远程模式对等
- **验收标准**:
  - [ ] 旧密码错误时拒绝修改
  - [ ] 新密码不符合策略时返回明确提示
  - [ ] 修改后旧 Token 失效

### FR-USER-010: 修改个人资料

- **描述**: 用户修改自己的基本资料 (真实姓名、电话)
- **业务规则**:
  1. 仅可修改 RealName 和 PhoneNumber
  2. UserName、Email 等字段暂不支持自助修改
- **远程模式**: PUT `/api/v1/users/{id}/profile`
- **本地模式**: 支持 (LocalUserDataSource)。本地 SQLite 存储，功能与远程模式对等
- **验收标准**:
  - [ ] 修改后拼音码自动更新
  - [ ] 其他字段忽略修改

### FR-USER-011: 启用/禁用用户

- **描述**: 切换用户的启用/禁用状态
- **业务规则**:
  1. 禁用用户时所有 Token Family 失效
  2. 禁用后当前会话立即失效
  3. 禁用用户尝试登录返回 UserDisabled 错误
  4. 支持批量启用/禁用
- **远程模式**: POST `/api/v1/users/{id}/toggle-status`，批量: POST `/api/v1/users/batch-enable` 或 `/batch-disable`
- **本地模式**: 支持 (LocalUserDataSource)。本地 SQLite 存储，功能与远程模式对等
- **验收标准**:
  - [ ] 禁用后立即生效
  - [ ] 批量操作返回详细结果

### FR-USER-012: 获取当前用户

- **描述**: 获取当前已登录用户的详细信息
- **业务规则**:
  1. 从 JWT Token 中提取 UserId
  2. 返回 UserDetailDto
  3. 无需 AdminOnly 权限，任何已认证用户可调用
- **远程模式**: GET `/api/v1/users/current`
- **本地模式**: 从本地会话获取
- **验收标准**:
  - [ ] 返回当前用户完整信息
  - [ ] 未认证时返回 401

---

## 数据模型

### User (用户实体)

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| Id | Guid | PK | 用户ID |
| UserName | string(50) | Required, Unique | 用户名 (创建后不可改) |
| RealName | string(50) | Required | 真实姓名 |
| PinYinCode | string(50)? | - | 拼音码 (系统生成) |
| PhoneNumber | string(20)? | - | 电话号码 |
| Email | string(100)? | - | 邮箱 |
| Role | UserRole | Default: Doctor | 用户角色 |
| Status | CommonStatus | Default: Enabled | 用户状态 |
| PasswordHash | string(256) | Required | 密码哈希 (仅后端) |
| FailedLoginCount | int | Default: 0 | 失败登录次数 |
| LockoutEnd | DateTime? | - | 锁定结束时间 |
| LastLoginTime | DateTime? | - | 最后登录时间 |
| Remark | string(500)? | - | 备注 |

> 继承 BaseEntity (Id, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, IsDeleted, RowVersion)

---

## 决策记录

| 编号 | 问题 | 影响范围 | 状态 |
|------|------|----------|------|
| 1 | 本地模式下用户管理的支持范围 | 所有 FR-USER | 已确定: 完整支持。LocalUserDataSource 11/11 方法全覆盖，DI 注册为 IUserDataSource 本地实现 |
| 2 | Receptionist 角色的具体功能边界 | FR-USER-001 | 已确定: 仅查看权限 (患者列表 + 医案列表)。不在 DoctorOrAdmin / AdminOnly 策略中，无任何写操作权限 |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本，从 user-management spec + UsersController 代码提取 |
