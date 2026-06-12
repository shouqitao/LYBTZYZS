# 用户管理 产品需求文档

---

## 1. Problem Statement

### 1.1 问题描述

中医诊所电子化后，多角色人员 (医生、管理员、前台) 共享同一系统访问患者敏感医疗数据。缺乏用户管理和权限隔离意味着无法控制"谁能做什么" -- 任何人都可能创建、修改甚至删除其他用户账号，导致权限混乱和安全隐患。同时，诊所人员流动 (离职、调岗) 需要及时禁用或删除账号，否则离职人员仍可访问系统。

### 1.2 用户痛点

| 角色 | 痛点 | 影响 |
|------|------|------|
| Admin | 无法限制不同角色的操作范围，医生和前台拥有相同权限 | 数据误操作风险高，责任无法界定 |
| Admin | 员工离职/调岗后无法及时撤销系统访问 | 敏感医疗数据泄露风险 |
| Admin | 手工管理密码重置，口头通知临时密码 | 效率低，安全性差 |
| Doctor | 无法自行修改密码和个人资料，依赖管理员操作 | 响应慢，影响日常使用 |
| SuperAdmin | 无法防止 Admin 越权修改其他 Admin 或系统管理员账号 | 权限体系不可靠 |

### 1.3 证据

- 卫生部门信息化安全要求: 医疗系统必须实现基于角色的访问控制 (RBAC)
- 诊所运营观察: 年均人员流动 2-3 人，账号生命周期管理是刚需
- 产品需求分析: 系统包含 4 级角色 (SuperAdmin/Admin/Doctor/Receptionist)，需要统一的用户管理入口

---

## 2. Target Users

| 角色 | 在本模块中的操作权限 |
|------|---------------------|
| SuperAdmin | 创建/编辑/删除/禁用 Admin/Doctor/Receptionist；自助修改密码和个人资料 |
| Admin | 创建/编辑/删除/禁用 Doctor/Receptionist；自助修改密码和个人资料 |
| Doctor | 自助修改密码和个人资料；无权进入用户管理 |
| Receptionist | 自助修改密码和个人资料；无权进入用户管理 |

> 整个 `/api/v1/users` 端点受 `AdminOnly` 策略保护，Doctor/Receptionist 访问返回 403。自助操作 (修改密码、个人资料) 不受此限制。

---

## 3. Strategic Context

### 3.1 业务目标

| 目标 | 对应 |
|------|------|
| 权限隔离 | 4 级角色体系 + 权限值模型确保"谁能管谁"清晰可控 |
| 账号生命周期管理 | 创建/禁用/删除/恢复完整流程，覆盖人员入职到离职全生命周期 |
| 操作安全 | 权限值检查 + sysadmin 不可被管理 + API 层兜底，防止越权操作 |
| 自助服务 | 医生/前台可自行修改密码和个人资料，减轻管理员负担 |
| 离线可用 | 本地模式完整支持用户管理，确保离线场景下人员管理不中断 |

### 3.2 Why Now

认证模块 ([auth.md](auth.md)) 已实现身份验证和会话管理，但"谁能登录"需要用户管理模块来控制。没有用户管理，就无法创建账号、分配角色、禁用离职人员 -- 认证模块是"锁"，用户管理模块是"钥匙管理"。

---

## 4. Solution Overview

用户管理模块实现完整的系统用户生命周期管理，基于权限值层级模型 (USER-D04) 实现严格的操作权限隔离:

**核心能力:**
- **用户 CRUD**: 创建/查看/编辑/软删除/恢复系统用户
- **权限值层级**: SuperAdmin(100) > Admin(80) > Doctor(60) > Receptionist(40)，高权限管低权限
- **sysadmin 保护**: 系统唯一固定账号，不可被任何人管理 (USER-D05)
- **密码管理**: 管理员重置密码 + 用户自助修改密码，强制密码策略
- **批量操作**: 批量删除/启用/禁用，单事务提交
- **跨模块联动**: 角色变更/禁用/删除时通过 ICrossModuleAuthService 撤销 Token Family (AUTH-D07)
- **双模式支持**: 远程 (SQL Server + HTTP API) + 本地 (LocalDB)，功能对等

**权限值层级模型 (USER-D04):**
```
SuperAdmin (100) ─── 可管理 ──→ Admin (80) / Doctor (60) / Receptionist (40)
Admin (80)       ─── 可管理 ──→ Doctor (60) / Receptionist (40)
Doctor (60)      ─── 无用户管理权限
Receptionist (40)─── 无用户管理权限

统一判断公式: operator.PermissionLevel > target.PermissionLevel → 允许操作
```

---

## 5. Success Metrics

| 指标 | 当前 (手工管理) | v1.0 目标 | 衡量方式 |
|------|----------------|----------|---------|
| 用户创建耗时 | 5-10 分钟 (手工配置) | < 30 秒 | 操作日志 |
| 离职账号禁用及时率 | 不确定 (依赖口头通知) | 100% 当天禁用 | 审计日志 |
| 越权操作发生率 | 无法统计 | 0 (API 层兜底) | SecurityAuditLog |
| 密码重置自助率 | 0% (全部依赖管理员) | > 50% 用户自助修改 | API 调用统计 |
| 本地模式功能覆盖率 | N/A | 100% (11/11 方法) | 测试覆盖 |

---

## 6. Epic Hypothesis

We believe that 实现基于权限值层级的用户管理系统 (CRUD + 批量操作 + 密码管理 + sysadmin 保护) for 诊所管理员和系统管理员 will achieve 安全可控的人员管理和权限隔离。We'll know we're right when 越权操作发生率为 0、离职账号 100% 当天禁用、且本地模式功能覆盖率 100%。

---

## 7. User Stories

### 优先级汇总

| US 编号 | 名称 | Priority |
|---------|------|----------|
| US-USER-001 | 创建用户 | Must |
| US-USER-002 | 查看用户列表 | Must |
| US-USER-003 | 查看用户详情 | Must |
| US-USER-004 | 更新用户信息 | Must |
| US-USER-005 | 删除用户 | Must |
| US-USER-006 | 恢复已删除用户 | Could |
| US-USER-007 | 批量删除 | Could |
| US-USER-008 | 管理员重置密码 | Should |
| US-USER-009 | 用户修改密码 | Should |
| US-USER-010 | 修改个人资料 | Should |
| US-USER-011 | 启用/禁用用户 | Should |
| US-USER-012 | 获取当前用户 | Should |

---

### US-USER-001: 创建用户

> As a Admin/SuperAdmin, I want to 创建新系统用户并分配角色,
> so that 新入职人员可以使用系统且权限受控。

**Acceptance Criteria:**
- [ ] 用户名已存在 → 返回 409 + ERR-10002
- [ ] 使用 admin/root 等保留名 → 返回 400
- [ ] Admin 创建 Admin 角色 → 返回 403 (权限值检查)
- [ ] 创建成功 → 拼音码自动生成

**Business Rules:**
1. 用户名唯一，仅允许字母、数字、下划线 (3-32 字符)
2. 系统保留用户名不可使用: admin, administrator, root, system, superadmin, sysadmin
3. 不提供密码时使用配置默认密码
4. 权限值检查: 只能创建权限值低于自己的角色 (USER-D04)。Admin(80) 可创建 Doctor(60)/Receptionist(40)，SuperAdmin(100) 可创建 Admin(80)/Doctor(60)/Receptionist(40)
5. 自动生成拼音码 (PinYinCode) 用于快速搜索
6. 默认状态为 Enabled，默认角色为 Doctor

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | POST `/api/v1/users`，返回 UserDetailDto (201) |
| 本地 | LocalUserRepository，本地 LocalDB 存储，功能与远程模式对等 |

### US-USER-002: 查看用户列表

> As a Admin/SuperAdmin, I want to 分页查看用户列表并按关键词/角色/状态筛选,
> so that 我可以快速找到需要管理的用户。

**Acceptance Criteria:**
- [ ] page=1, pageSize=20 → 返回前 20 条用户
- [ ] keyword="张" → 返回用户名或真实姓名包含"张"的结果
- [ ] 列表仅显示权限值严格低于操作者的用户，且过滤自己

**Business Rules:**
1. 支持按用户名、真实姓名搜索 (keyword)
2. 支持按角色 (role) 和状态 (status) 筛选
3. 默认分页: page=1, pageSize=20
4. 返回 UserListDto (不含敏感信息)
5. 用户管理列表中只显示权限值严格低于操作者的用户，且过滤自己 (USER-D04)

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | GET `/api/v1/users?keyword=&role=&status=&page=&pageSize=` |
| 本地 | LocalUserRepository，本地 LocalDB 存储，功能与远程模式对等 |

### US-USER-003: 查看用户详情

> As a Admin/SuperAdmin, I want to 查看单个用户的完整信息,
> so that 我可以了解该用户的账号状态和历史记录。

**Acceptance Criteria:**
- [ ] 有效 ID → 返回 200 + UserDetailDto (含审计字段)
- [ ] 响应 JSON 中 → 不包含 PasswordHash 字段
- [ ] 用户不存在 → 返回 404

**Business Rules:**
1. 返回 UserDetailDto (含审计字段，不含密码)
2. 用户不存在返回 404

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | GET `/api/v1/users/{id}` |
| 本地 | LocalUserRepository，本地 LocalDB 存储，功能与远程模式对等 |

### US-USER-004: 更新用户信息

> As a Admin/SuperAdmin, I want to 修改用户的基本信息和角色,
> so that 用户信息保持准确且角色变更能即时生效。

**Acceptance Criteria:**
- [ ] 请求体含 UserName 修改 → 忽略，UserName 不变
- [ ] RealName 变更 → PinYinCode 自动重新生成
- [ ] 角色变更 → 该用户所有 Token Family 失效，强制重登录
- [ ] 修改 sysadmin → 返回 403

**Business Rules:**
1. 用户名创建后不可修改
2. 真实姓名变更时自动重新生成拼音码
3. 权限值检查: operator.PermissionLevel > target.PermissionLevel (USER-D04)
4. 不能修改 sysadmin (USER-D05: sysadmin 不可被管理)
5. 角色变更时通过 ICrossModuleAuthService.RevokeAllUserTokensAsync() 撤销该用户 Token Family，强制重登录 (AUTH-D07，见 [auth.md](auth.md))

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | PUT `/api/v1/users/{id}` |
| 本地 | LocalUserRepository，本地 LocalDB 存储，功能与远程模式对等 |

### US-USER-005: 删除用户

> As a Admin/SuperAdmin, I want to 软删除用户,
> so that 离职人员无法继续访问系统且数据保留可追溯。

**Acceptance Criteria:**
- [ ] 删除后用户登录 → 返回 404 (软删除过滤)
- [ ] 删除最后一个 Admin → 返回 403
- [ ] 删除当前登录用户 → 返回 400

**Business Rules:**
1. 软删除，数据保留 (IsDeleted=true)
2. 不能删除自己 (列表已过滤自己，API 层兜底校验)
3. 不能删除 sysadmin (USER-D05: sysadmin 不可被管理，列表不可见 + API 层兜底)
4. 权限值检查: operator.PermissionLevel > target.PermissionLevel (USER-D04)
5. 删除后通过 ICrossModuleAuthService.RevokeAllUserTokensAsync() 撤销所有 Token Family (AUTH-D07)
6. 记录审计日志
7. 该医生名下的医案数据保留 (DoctorId 不变)，由管理员手动处理 (USER-D06)

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | DELETE `/api/v1/users/{id}` |
| 本地 | LocalUserRepository，本地 LocalDB 存储，功能与远程模式对等 |

### US-USER-006: 恢复已删除用户

> As a Admin/SuperAdmin, I want to 恢复软删除的用户,
> so that 误删或重新入职的人员可以恢复系统访问。

**Acceptance Criteria:**
- [ ] 恢复成功 → 用户可正常登录，状态恢复
- [ ] 对未删除用户调用恢复 → 返回 400 "该用户未被删除，无需恢复"

**Business Rules:**
1. 查询已删除用户 (IgnoreQueryFilters)
2. 恢复 IsDeleted=false
3. 状态恢复为之前的值

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | POST `/api/v1/users/{id}/restore` |
| 本地 | LocalUserRepository，本地 LocalDB 存储，功能与远程模式对等 |

### US-USER-007: 批量删除

> As a Admin/SuperAdmin, I want to 批量软删除多个用户,
> so that 大批人员调整时可以高效处理。

**Acceptance Criteria:**
- [ ] 批量删除 → 返回 BatchOperationResultDto (successCount/failureCount/failedItems)
- [ ] 批量中包含自己 → 该项失败，原因"不能删除自己"

**Business Rules:**
1. 逐个检查权限值 (USER-D04) 和 sysadmin 保护 (USER-D05)
2. 列表已过滤自己和不可操作的用户，API 层兜底校验
3. 返回详细的成功/失败报告 (BatchOperationResultDto)
4. 单事务提交: 业务规则检查在内存中完成，通过检查的一次性 SaveChanges，技术失败则整批回滚

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | POST `/api/v1/users/batch-delete` |
| 本地 | LocalUserRepository，本地 LocalDB 存储，功能与远程模式对等 |

### US-USER-008: 管理员重置密码

> As a Admin/SuperAdmin, I want to 将用户密码重置为默认密码,
> so that 忘记密码的用户可以快速恢复登录能力。

**Acceptance Criteria:**
- [ ] 重置成功 → 返回临时密码或使用配置默认密码
- [ ] 重置后 → 该用户所有 Token Family 失效
- [ ] 使用临时密码登录 → 登录成功

**Business Rules:**
1. 无需提供旧密码
2. 使用配置文件中的默认密码或自动生成临时密码
3. 重置后通过 ICrossModuleAuthService.RevokeAllUserTokensAsync() 撤销所有 Token Family (AUTH-D07)
4. 用户需要重新登录
5. 可设置 MustChangeOnNextLogin 标记

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | POST `/api/v1/users/{id}/reset-password` |
| 本地 | LocalUserRepository，本地 LocalDB 存储，功能与远程模式对等 |

### US-USER-009: 用户修改密码

> As a 已登录用户 (任意角色), I want to 自行修改密码,
> so that 我可以定期更新密码保障账号安全。

**Acceptance Criteria:**
- [ ] 旧密码错误 → 返回 401 + ERR-10004
- [ ] 新密码不符合策略 → 返回 400 + 具体不满足项
- [ ] 修改成功后 → 所有 Token Family 失效，需重新登录

**Business Rules:**
1. 验证旧密码正确
2. 密码策略: 最小 8 位，必须包含大小写字母、数字、特殊字符
3. 修改后通过 ICrossModuleAuthService.RevokeAllUserTokensAsync() 撤销所有 Token Family (AUTH-D07)
4. 用户需要重新登录

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | PUT `/api/v1/users/{id}/change-password` |
| 本地 | LocalUserRepository，本地 LocalDB 存储，功能与远程模式对等 |

### US-USER-010: 修改个人资料

> As a 已登录用户 (任意角色), I want to 修改自己的真实姓名和电话,
> so that 我的个人信息保持准确而不需要麻烦管理员。

**Acceptance Criteria:**
- [ ] RealName 变更 → PinYinCode 自动重新生成
- [ ] 请求体含 UserName/Email 修改 → 忽略

**Business Rules:**
1. 仅可修改 RealName 和 PhoneNumber
2. UserName、Email 等字段暂不支持自助修改

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | PUT `/api/v1/users/{id}/profile` |
| 本地 | LocalUserRepository，本地 LocalDB 存储，功能与远程模式对等 |

### US-USER-011: 启用/禁用用户

> As a Admin/SuperAdmin, I want to 切换用户的启用/禁用状态,
> so that 可以临时冻结问题账号或恢复正常使用。

**Acceptance Criteria:**
- [ ] 禁用用户 → 当前会话立即失效，Token Family 作废
- [ ] 批量启用/禁用 → 返回 BatchOperationResultDto
- [ ] 禁用 sysadmin → 返回 403

**Business Rules:**
1. 不能禁用 sysadmin (USER-D05: sysadmin 不可被管理)
2. 权限值检查: operator.PermissionLevel > target.PermissionLevel (USER-D04)
3. 禁用用户时通过 ICrossModuleAuthService.RevokeAllUserTokensAsync() 撤销所有 Token Family
4. 禁用后当前会话立即失效
5. 禁用用户尝试登录返回 UserDisabled 错误
6. 支持批量启用/禁用

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | POST `/api/v1/users/{id}/toggle-status`，批量: POST `/api/v1/users/batch-enable` 或 `/batch-disable` |
| 本地 | LocalUserRepository，本地 LocalDB 存储，功能与远程模式对等 |

### US-USER-012: 获取当前用户

> As a 已登录用户 (任意角色), I want to 获取自己的账号详细信息,
> so that 我可以确认当前登录身份和账号状态。

**Acceptance Criteria:**
- [ ] 已认证用户 → 返回 200 + UserDetailDto
- [ ] 未携带 Token → 返回 401

**Business Rules:**
1. 从 JWT Token 中提取 UserId
2. 返回 UserDetailDto
3. 无需 AdminOnly 权限，任何已认证用户可调用

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | GET `/api/v1/users/current` |
| 本地 | 从本地会话获取 |

> **[Sprint 4 已实现]** User DataSource 扩展: IUserDataSource 新增 RestoreAsync/BatchDeleteAsync/ResetPasswordAsync/BatchToggleStatusAsync/GetCurrentUserAsync 方法，Local/Remote 双模式实现完整覆盖 (T4-X2-01~08)

---

## 8. Out of Scope

| 排除项 | 原因 |
|--------|------|
| 用户组/部门管理 | 小型诊所 (4-10 人) 无需组织架构管理，4 级角色足够 |
| LDAP/AD 集成 | 诊所无 IT 基础设施，增加部署复杂度 |
| 用户自助注册 | 诊所用户由管理员统一创建，防止未授权人员自行注册 |
| Email/手机自助修改 | v1.0 暂不支持，需要验证码机制 |
| 医案自动转移 (医生删除后) | 涉及医疗责任归属，不适合系统自动决定 (USER-D06) |
| 密码过期强制修改 | v1.0 不启用，MustChangeOnNextLogin 标记已预留 |

---

## 9. Dependencies & Risks

| 风险 | 影响 | 缓解措施 |
|------|------|---------|
| 认证模块依赖 | 角色变更/禁用/删除需要撤销 Token Family | 通过 ICrossModuleAuthService 接口解耦 (ISP 原则) |
| 权限值绕过 (直接调 API) | 低权限用户绕过 UI 管理高权限用户 | API 层兜底校验 + sysadmin 硬规则 (USER-D05) |
| sysadmin 密码遗忘 | 系统唯一超级管理员无法登录 | sysadmin 可通过 /change-password 自助修改；极端情况需数据库直接重置 |
| 批量操作误删 | 管理员误选大量用户执行删除 | 软删除可恢复 + BatchOperationResultDto 明确报告 |
| 本地模式数据同步 | 本地创建的用户在联网后需同步到远程 | v1.0 本地/远程独立运行，数据同步 v2.0+ 考虑 |

---

## 10. Open Questions

| ID | 问题 | 状态 |
|----|------|------|
| OQ-USER-01 | 密码过期策略 (MustChangeOnNextLogin) 何时启用? | 预留设计，v1.0 不启用 |
| OQ-USER-02 | Email 自助修改是否需要验证码机制? | 延期。v1.0 不支持自助修改 Email |
| OQ-USER-03 | 本地模式用户数据与远程模式的同步策略? | 延期。v1.0 双模式独立运行 |
| OQ-USER-04 | 批量操作的最大用户数限制? | 待确定。目前无限制 |

---

## Data Model

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

### 权限值层级 (USER-D04)

| 角色 | 权限值 | 用户管理中可见的用户 | 自助操作 |
|------|--------|---------------------|---------|
| SuperAdmin (sysadmin) | 100 | Admin + Doctor + Receptionist (过滤自己) | 修改密码、邮箱等个人资料 |
| Admin | 80 | Doctor + Receptionist (过滤自己 + sysadmin + 其他Admin) | 修改密码、个人资料 |
| Doctor | 60 | 无权进入用户管理 | 修改密码、个人资料 |
| Receptionist | 40 | 无权进入用户管理 | 修改密码、个人资料 |

### SuperAdmin (sysadmin) 特殊规则 (USER-D05)

| 规则 | 说明 |
|------|------|
| 数量 | 系统唯一，固定账号，数据库种子数据预置 |
| 作为操作者 | 拥有 Admin 全部权限，可创建/管理 Admin/Doctor/Receptionist |
| 作为目标 | 不可被任何人管理 -- 不可修改角色、不可删除、不可禁用、不可重置密码 |
| 自助操作 | 可通过 /profile 和 /change-password 修改自己的密码、邮箱等个人信息 |
| 可见性 | Admin 用户管理列表中不可见 (权限值过滤)；sysadmin 自己的列表中也不显示自己 (过滤自己) |
| 服务端硬规则 | API 层兜底: 任何以 sysadmin 为目标的用户管理操作一律拒绝 (防止绕过 UI 直接调 API) |

---

## Error Codes

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
| 权限不足 (统一) | 403 | 您没有权限执行此操作 | operator.PermissionLevel <= target.PermissionLevel (USER-D04) |
| sysadmin 不可管理 | 403 | 系统管理员账号不可被修改 | 任何以 sysadmin 为目标的用户管理操作 (USER-D05) |
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

## Decision Log

| 编号 | 问题 | 影响范围 | 状态 |
|------|------|----------|------|
| 1 | 本地模式下用户管理的支持范围 | 所有 US-USER | 已确定: 完整支持。LocalUserRepository 11/11 方法全覆盖，DI 注册为 IUserDataSource 本地实现 |
| 2 | Receptionist 角色的具体功能边界 | US-USER-001 | 已确定: 患者 CRU (创建/查看/更新，无删除) + 读卡器使用 + 未完成医案简要提示 (时间+医生，不含诊断/处方详情)。不在 AdminOnly 策略中 |
| ~~USER-D03~~ | ~~最后一个 Admin/SuperAdmin 禁用保护~~ | ~~US-USER-011~~ | **已移除** (USER-D04/D05 替代): sysadmin 固定存在不可被管理，永远可以创建新 Admin，不可能出现"无管理员"状态 |
| AUTH-D07 | 角色变更即时生效 | US-USER-004 | 已确定: 角色变更时通过 ICrossModuleAuthService.RevokeAllUserTokensAsync() 撤销 Token Family，强制重登录 (见 auth.md AUTH-D07) |
| USER-D04 | 权限值层级模型 | 全部 US-USER | 已确定: SuperAdmin=100, Admin=80, Doctor=60, Receptionist=40。统一判断: operator.Level > target.Level -> 允许。用户管理列表只显示权限值严格低于操作者的用户，且过滤自己 |
| USER-D05 | sysadmin 不可被管理 | US-USER-004/005/007/008/011 | 已确定: sysadmin 是系统唯一固定账号 (数据库种子预置)。不可被任何人修改角色/删除/禁用/重置密码。仅可通过 /profile 和 /change-password 自助修改个人信息。API 层硬规则兜底 |
| USER-D06 | 医生删除后医案不自动转移 | US-USER-005 + MedicalCase | 已确定: 医生被禁用/删除后其名下医案数据保留 (DoctorId 不变)，由管理员手动处理。医案转移涉及医疗责任归属，不适合系统自动决定 |

### 修订历史

| 日期 | 修订项 | 原因 | 参考 |
|------|--------|------|------|
| 2026-02-21 | 旧密码错误消息对齐代码实现 | 代码错误消息 "用户名或密码错误" 符合安全设计 (防枚举)，PRD 对齐 | USER-30 |

---

## Change Log

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本，从 user-management spec + UsersController 代码提取 |
| 2026-02-11 | v1.1 | 新增错误码章节，从 UserService.cs + ErrorCode.cs 提取 19 个错误场景 |
| 2026-02-17 | v1.2 | Round 10: FR-USER-004 补充角色变更即时生效 (AUTH-D07)，FR-USER-011 补充最后管理员禁用保护 (USER-D03)，新增错误码 |
| 2026-02-17 | v1.3 | PRD审查修复: A2-Receptionist功能边界更新(患者CRU+读卡器+医案提示), D3-补全ERR-10003(EmailExists)+ERR-10005(PasswordPolicyViolation) |
| 2026-02-21 | v1.4 | PRD vs Code 偏差分析修订: 1 项修订 (USER-30 旧密码错误消息对齐代码) |
| 2026-02-21 | v1.5 | Phase 2 模块功能细化: 新增权限值层级模型 (USER-D04, 100/80/60/40)，sysadmin 不可被管理规则 (USER-D05)，移除最后管理员保护 (USER-D03)，批量删除改为单事务，医生删除后医案不自动转移 (USER-D06)，错误码简化为权限值统一判断，FR-USER-001/004/005/007/011 对齐权限值模型 |
| 2026-02-22 | v1.6 | Token 撤销接口统一 (A3): ICrossModuleService -> ICrossModuleAuthService (ISP); FR-USER-008 (重置密码) + FR-USER-009 (修改密码) 补充 ICrossModuleAuthService 显式调用; 6 个撤销场景全部对齐 AUTH-D07 |
| 2026-02-26 | v1.7 | Sprint 4 已实现标记: IUserDataSource 扩展 RestoreAsync/BatchDeleteAsync/ResetPasswordAsync/BatchToggleStatusAsync/GetCurrentUserAsync (T4-X2-01~08) |
| 2026-03-06 | v2.0 | PRD 全面重写: FR->US 格式迁移，新增 Problem Statement/Strategic Context/Success Metrics/Epic Hypothesis/Out of Scope/Dependencies & Risks/Open Questions 7 个章节，修订注释迁移到 Decision Log 修订历史 |
