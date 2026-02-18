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
  - [ ] 用户名已存在 -> 返回 409 + ERR-10002
  - [ ] 使用 admin/root 等保留名 -> 返回 400
  - [ ] Admin 创建 Admin 角色 -> 返回 403
  - [ ] 创建成功 -> 拼音码自动生成

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
  - [ ] page=1, pageSize=20 -> 返回前20条用户
  - [ ] keyword="张" -> 返回用户名或真实姓名包含"张"的结果

### FR-USER-003: 查看用户详情

- **描述**: 获取单个用户的完整信息
- **业务规则**:
  1. 返回 UserDetailDto (含审计字段，不含密码)
  2. 用户不存在返回 404
- **远程模式**: GET `/api/v1/users/{id}`
- **本地模式**: 支持 (LocalUserDataSource)。本地 SQLite 存储，功能与远程模式对等
- **验收标准**:
  - [ ] 有效ID -> 返回 200 + UserDetailDto (含审计字段)
  - [ ] 响应 JSON 中 -> 不包含 PasswordHash 字段

### FR-USER-004: 更新用户信息

- **描述**: 管理员修改用户的基本信息
- **业务规则**:
  1. 用户名创建后不可修改
  2. 真实姓名变更时自动重新生成拼音码
  3. Admin 只能更新 Doctor/Receptionist
  4. 不能修改比自己权限高的角色
  5. **角色变更时立即撤销该用户 Token Family，强制重登录** (AUTH-D07，见 [auth.md](auth.md))
- **远程模式**: PUT `/api/v1/users/{id}`
- **本地模式**: 支持 (LocalUserDataSource)。本地 SQLite 存储，功能与远程模式对等
- **验收标准**:
  - [ ] 请求体含 UserName 修改 -> 忽略，UserName 不变
  - [ ] RealName 变更 -> PinYinCode 自动重新生成

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
  - [ ] 删除后用户登录 -> 返回 404 (软删除过滤)
  - [ ] 删除最后一个 Admin -> 返回 403
  - [ ] 删除当前登录用户 -> 返回 400

### FR-USER-006: 恢复已删除用户

- **描述**: 恢复软删除的用户
- **业务规则**:
  1. 查询已删除用户 (IgnoreQueryFilters)
  2. 恢复 IsDeleted=false
  3. 状态恢复为之前的值
- **远程模式**: POST `/api/v1/users/{id}/restore`
- **本地模式**: 支持 (LocalUserDataSource)。本地 SQLite 存储，功能与远程模式对等
- **验收标准**:
  - [ ] 恢复成功 -> 用户可正常登录，状态恢复
  - [ ] 对未删除用户调用恢复 -> 返回 400 "该用户未被删除，无需恢复"

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
  - [ ] 批量删除 -> 返回 BatchOperationResultDto (successCount/failureCount/failedItems)
  - [ ] 批量中包含自己 -> 该项失败，原因"不能删除自己"

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
  - [ ] 重置成功 -> 返回临时密码或使用配置默认密码
  - [ ] 重置后 -> 该用户所有 Token Family 失效
  - [ ] 使用临时密码登录 -> 登录成功

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
  - [ ] 旧密码错误 -> 返回 401 + ERR-10004
  - [ ] 新密码不符合策略 -> 返回 400 + 具体不满足项
  - [ ] 修改成功后 -> 所有 Token Family 失效，需重新登录

### FR-USER-010: 修改个人资料

- **描述**: 用户修改自己的基本资料 (真实姓名、电话)
- **业务规则**:
  1. 仅可修改 RealName 和 PhoneNumber
  2. UserName、Email 等字段暂不支持自助修改
- **远程模式**: PUT `/api/v1/users/{id}/profile`
- **本地模式**: 支持 (LocalUserDataSource)。本地 SQLite 存储，功能与远程模式对等
- **验收标准**:
  - [ ] RealName 变更 -> PinYinCode 自动重新生成
  - [ ] 请求体含 UserName/Email 修改 -> 忽略

### FR-USER-011: 启用/禁用用户

- **描述**: 切换用户的启用/禁用状态
- **业务规则**:
  1. **不能禁用最后一个 SuperAdmin/Admin** (USER-D03，与删除保护一致)
  2. 禁用用户时所有 Token Family 失效
  3. 禁用后当前会话立即失效
  4. 禁用用户尝试登录返回 UserDisabled 错误
  5. 支持批量启用/禁用
- **远程模式**: POST `/api/v1/users/{id}/toggle-status`，批量: POST `/api/v1/users/batch-enable` 或 `/batch-disable`
- **本地模式**: 支持 (LocalUserDataSource)。本地 SQLite 存储，功能与远程模式对等
- **验收标准**:
  - [ ] 禁用用户 -> 当前会话立即失效，Token Family 作废
  - [ ] 批量启用/禁用 -> 返回 BatchOperationResultDto

### FR-USER-012: 获取当前用户

- **描述**: 获取当前已登录用户的详细信息
- **业务规则**:
  1. 从 JWT Token 中提取 UserId
  2. 返回 UserDetailDto
  3. 无需 AdminOnly 权限，任何已认证用户可调用
- **远程模式**: GET `/api/v1/users/current`
- **本地模式**: 从本地会话获取
- **验收标准**:
  - [ ] 已认证用户 -> 返回 200 + UserDetailDto
  - [ ] 未携带 Token -> 返回 401

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

## 错误码

> 错误码分区: 1xxxx (Users 模块)。Service 层采用 Result 模式统一返回，由 IExceptionHandler 映射为 HTTP 响应。

### 结构化错误码

| 错误码 | 枚举名 | HTTP | 用户消息 | 触发条件 |
|--------|--------|------|----------|----------|
| ERR-10001 | UserNotFound | 404 | 用户不存在 | GetById/Update/Delete/ResetPassword/ChangePassword/ChangeProfile/ToggleStatus/Restore 时 ID 无效 |
| ERR-10002 | UserNameExists | 409 | 用户名已被使用 | 创建时用户名已存在 |
| ERR-10003 | EmailExists | 409 | 邮箱已被使用 | 创建/更新时邮箱已存在 |
| ERR-10004 | InvalidPassword | 401 | 用户名或密码错误 | 登录密码错误、修改密码时旧密码错误 |
| ERR-10005 | PasswordPolicyViolation | 400 | 密码不符合安全策略 | 密码不满足长度/复杂度要求 |
| ERR-10006 | UserDisabled | 403 | 用户账号已被禁用，请联系管理员 | 已禁用用户尝试登录 |
| ERR-00003 | ValidationFailed | 400 | 输入数据验证失败，请检查后重试 | FluentValidation 验证不通过 |

### 业务规则错误

| 场景 | HTTP | 用户消息 | 触发条件 |
|------|------|----------|----------|
| 保留用户名 | 400 | 用户名 '{UserName}' 为系统保留用户名，不可使用 | 使用 admin/root/system 等保留名 |
| 无权创建角色 | 403 | 您没有权限创建{角色}账户 | Admin 创建 Admin，Doctor 创建任意角色 |
| 无权更新用户 | 403 | 您没有权限更新该用户 | 低权限用户修改高权限用户 |
| 无权修改角色 | 403 | 您没有权限将用户角色修改为该级别 | 角色提升越权 |
| 无权删除用户 | 403 | 您没有权限删除该用户 | 低权限用户删除高权限用户 |
| 无权修改状态 | 403 | 您没有权限修改该用户状态 | 低权限用户修改高权限用户状态 |
| 无权恢复用户 | 403 | 您没有权限恢复该用户 | 低权限用户恢复高权限用户 |
| 最后管理员保护 (删除) | 403 | 不能删除最后一个{超级管理员\|管理员} | 删除后无 SuperAdmin 或 Admin |
| 最后管理员保护 (禁用) | 403 | 不能禁用最后一个{超级管理员\|管理员} | 禁用后无可用 SuperAdmin 或 Admin (USER-D03) |
| 不能删除自己 | 400 | 不能删除自己 | 当前用户尝试删除自己 |
| 不能修改自己状态 | 400 | 不能{启用\|禁用}当前登录用户 | 批量操作中包含自己 |
| 用户未被删除 | 400 | 该用户未被删除，无需恢复 | 恢复未软删除的用户 |
| 批量操作为空 | 400 | 请至少选择一个用户 | 批量删除/启用/禁用时 ID 列表为空 |
| 空值验证 | 400 | 用户名不能为空 / 密码不能为空 / 真实姓名不能为空 | 必填字段为空 |
| 删除失败 | 500 | 删除失败 | 数据库操作异常 |

### 安全设计

- **登录失败隐藏策略**: 用户不存在和密码错误统一返回 "用户名或密码错误"，防止用户名枚举攻击
- **批量操作部分失败**: 返回 BatchOperationResultDto，单项失败不中断整个操作

---

## 决策记录

| 编号 | 问题 | 影响范围 | 状态 |
|------|------|----------|------|
| 1 | 本地模式下用户管理的支持范围 | 所有 FR-USER | 已确定: 完整支持。LocalUserDataSource 11/11 方法全覆盖，DI 注册为 IUserDataSource 本地实现 |
| 2 | Receptionist 角色的具体功能边界 | FR-USER-001 | 已确定: 患者 CRU (创建/查看/更新，无删除) + 读卡器使用 + 未完成医案简要提示 (时间+医生，不含诊断/处方详情)。不在 AdminOnly 策略中 |
| USER-D03 | 最后一个 Admin/SuperAdmin 禁用保护 | FR-USER-011 | 已确定: 与删除保护一致，禁止禁用最后一个管理员 |
| AUTH-D07 | 角色变更即时生效 | FR-USER-004 | 已确定: 角色变更时立即撤销 Token Family，强制重登录 (见 auth.md AUTH-D07) |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本，从 user-management spec + UsersController 代码提取 |
| 2026-02-11 | v1.1 | 新增错误码章节，从 UserService.cs + ErrorCode.cs 提取 19 个错误场景 |
| 2026-02-17 | v1.2 | Round 10: FR-USER-004 补充角色变更即时生效 (AUTH-D07)，FR-USER-011 补充最后管理员禁用保护 (USER-D03)，新增错误码 |
| 2026-02-17 | v1.3 | PRD审查修复: A2-Receptionist功能边界更新(患者CRU+读卡器+医案提示), D3-补全ERR-10003(EmailExists)+ERR-10005(PasswordPolicyViolation) |
