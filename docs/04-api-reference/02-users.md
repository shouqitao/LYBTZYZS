# 用户 API

> Controller: `UsersController` | 路由前缀: `/api/v1/users` | 默认权限: `[Authorize]` (类级别，允许所有认证用户)

## 概述

用户管理 CRUD、密码管理、状态切换、批量操作。管理端点使用 `[Authorize(Policy = "AdminOrSuperAdmin")]`，自助端点 (`current`/`profile`/`change-password`) 允许所有认证用户。

> **响应信封**：所有响应为 `ApiResponse<T>`，字段定义与通用错误码见 [README](README.md)。下文成功响应示例仅展示 `data` 内容。

---

## GET /users

获取用户列表 (分页)。

> **权限**: `[Authorize(Policy = "AdminOrSuperAdmin")]`

**查询参数**:

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `page` | int | 1 | 页码 |
| `pageSize` | int | 20 | 每页大小 |
| `keyword` | string? | null | 搜索关键词 (匹配用户名/姓名/邮箱/手机号) |
| `role` | UserRole? | null | 角色筛选 (Doctor/Admin/Receptionist/SuperAdmin) |
| `status` | CommonStatus? | null | 状态筛选 (Enabled/Disabled) |

**curl 示例**:

```bash
# 默认查询
curl -H "Authorization: Bearer <token>" \
  http://localhost:5000/api/v1/users

# 按关键词 + 角色筛选
curl -H "Authorization: Bearer <token>" \
  "http://localhost:5000/api/v1/users?keyword=张&page=1&pageSize=10&role=Doctor"

# 按状态筛选
curl -H "Authorization: Bearer <token>" \
  "http://localhost:5000/api/v1/users?status=Enabled"
```

**成功响应** (200): `ApiResponse<PagedResult<UserListDto>>`

```json
{
  "items": [
    {
      "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "userName": "doctor_zhang",
      "realName": "张医生",
      "phoneNumber": "13800138001",
      "role": "Doctor",
      "status": "Enabled",
      "lastLoginTime": "2026-06-20T14:30:00Z",
      "createdAt": "0001-01-01T00:00:00"
    },
    {
      "id": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
      "userName": "admin_li",
      "realName": "李管理",
      "phoneNumber": "13800138002",
      "role": "Admin",
      "status": "Enabled",
      "lastLoginTime": "2026-06-21T09:15:00Z",
      "createdAt": "0001-01-01T00:00:00"
    }
  ],
  "totalCount": 2,
  "page": 1,
  "pageSize": 20,
  "totalPages": 1
}
```

**错误码：**

| HTTP 状态码 | 说明 |
|------------|------|
| 400 | 分页参数验证失败 (page < 1 或 pageSize < 1) (ERR-00003) |
| 401/403/404 | — | 通用错误码见 [README](README.md#通用-http-状态码) |

---

## GET /users/current

获取当前登录用户信息。SuperAdmin 使用 `Id=Guid.Empty` 特殊处理。

> **权限**: `[Authorize]` (所有认证用户，自助端点)

**curl 示例**:

```bash
curl -H "Authorization: Bearer <token>" \
  http://localhost:5000/api/v1/users/current
```

**成功响应** (200): `ApiResponse<UserDetailDto>`

```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "userName": "doctor_zhang",
  "realName": "张医生",
  "role": "Doctor",
  "email": "zhang@lybt.com",
  "phoneNumber": "13800138001",
  "isEnabled": true,
  "status": "Enabled",
  "pinYinCode": "ZS",
  "lastLoginTime": "2026-06-20T14:30:00Z",
  "failedLoginCount": 0,
  "remark": "",
  "createdAt": "0001-01-01T00:00:00",
  "updatedAt": null
}
```

**SuperAdmin 响应示例**:

```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "userName": "sysadmin",
  "realName": "系统超级管理员",
  "role": "Admin",
  "email": "sysadmin@lybt.com",
  "phoneNumber": null,
  "isEnabled": true,
  "status": "Enabled",
  "pinYinCode": null,
  "lastLoginTime": null,
  "failedLoginCount": 0,
  "remark": null,
  "createdAt": "0001-01-01T00:00:00",
  "updatedAt": "2026-06-25T00:00:00Z"
}
```

**错误码：**

| HTTP 状态码 | 说明 |
|------------|------|
| 401/403/404 | — | 通用错误码见 [README](README.md#通用-http-状态码) |

---

## GET /users/{id}

获取单个用户详情。

> **权限**: `[Authorize(Policy = "AdminOrSuperAdmin")]`

**路径参数**: `id` (Guid) -- 用户 ID

**curl 示例**:

```bash
curl -H "Authorization: Bearer <token>" \
  http://localhost:5000/api/v1/users/a1b2c3d4-e5f6-7890-abcd-ef1234567890
```

**成功响应** (200): `ApiResponse<UserDetailDto>`

```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "userName": "doctor_zhang",
  "realName": "张医生",
  "role": "Doctor",
  "email": "zhang@lybt.com",
  "phoneNumber": "13800138001",
  "isEnabled": true,
  "status": "Enabled",
  "pinYinCode": "ZS",
  "lastLoginTime": "2026-06-20T14:30:00Z",
  "failedLoginCount": 0,
  "remark": "",
  "createdAt": "0001-01-01T00:00:00",
  "updatedAt": null
}
```

**UserDetailDto 字段说明**:

| 字段 | 类型 | 说明 |
|------|------|------|
| `id` | Guid | 用户 ID |
| `userName` | string | 用户名 |
| `realName` | string | 真实姓名 |
| `role` | UserRole | 角色: Unknown/Doctor/Admin/SuperAdmin/Receptionist |
| `email` | string? | 邮箱 |
| `phoneNumber` | string? | 手机号码 |
| `isEnabled` | bool | 是否启用 |
| `status` | CommonStatus | 状态: Enabled/Disabled |
| `pinYinCode` | string? | 拼音码 |
| `lastLoginTime` | DateTime? | 最后登录时间 |
| `failedLoginCount` | int | 连续失败登录次数 |
| `remark` | string? | 备注 |
| `createdAt` | DateTime | 创建时间 |
| `updatedAt` | DateTime? | 更新时间 |

**错误码：**

| HTTP 状态码 | 说明 |
|------------|------|
| 404 | 用户不存在 (ERR-10001) |
| 401/403 | — | 通用错误码见 [README](README.md#通用-http-状态码) |

---

## POST /users

创建新用户。

> **权限**: `[Authorize(Policy = "AdminOrSuperAdmin")]`

**请求体** (`UserInputDto`):

```json
{
  "userName": "doctor_wang",
  "realName": "王医生",
  "password": "Wang@2026!",
  "role": "Doctor",
  "email": "wang@lybt.com",
  "phoneNumber": "13800138003",
  "remark": "新入职医生"
}
```

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `userName` | string | 是 | 用户名 (3-32 字符，字母/数字/下划线) |
| `realName` | string | 是 | 真实姓名 |
| `password` | string | 否 | 密码 (6-128 字符，不传则使用默认密码) |
| `role` | UserRole | 否 | 角色，默认 Doctor |
| `email` | string | 否 | 邮箱 |
| `phoneNumber` | string | 否 | 手机号码 |
| `remark` | string | 否 | 备注 |

**curl 示例**:

```bash
curl -X POST \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "userName": "doctor_wang",
    "realName": "王医生",
    "password": "Wang@2026!",
    "role": "Doctor",
    "email": "wang@lybt.com",
    "phoneNumber": "13800138003"
  }' \
  http://localhost:5000/api/v1/users
```

**成功响应** (201): `ApiResponse<UserDetailDto>` + `Location` 头

```json
{
  "id": "c3d4e5f6-a7b8-9012-cdef-123456789012",
  "userName": "doctor_wang",
  "realName": "王医生",
  "role": "Doctor",
  "email": "wang@lybt.com",
  "phoneNumber": "13800138003",
  "isEnabled": true,
  "status": "Enabled",
  "pinYinCode": null,
  "lastLoginTime": null,
  "failedLoginCount": 0,
  "remark": "",
  "createdAt": "0001-01-01T00:00:00",
  "updatedAt": null
}
```

**错误码：**

| HTTP 状态码 | 说明 |
|------------|------|
| 400 | 用户名已被使用 (ERR-10002) / 验证失败 (ERR-00003) / 系统保留名 |
| 401/403 | — | 通用错误码见 [README](README.md#通用-http-状态码) |

---

## PUT /users/{id}

更新用户信息。sysadmin 账号不可修改。

> **权限**: `[Authorize(Policy = "AdminOrSuperAdmin")]`

**路径参数**: `id` (Guid) -- 用户 ID

**请求体** (`UserInputDto`):

```json
{
  "realName": "张医生(已更名)",
  "email": "zhang_new@lybt.com",
  "phoneNumber": "13900139001",
  "role": "Admin",
  "remark": "晋升为管理员"
}
```

**curl 示例**:

```bash
curl -X PUT \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "realName": "张医生(已更名)",
    "email": "zhang_new@lybt.com",
    "role": "Admin"
  }' \
  http://localhost:5000/api/v1/users/a1b2c3d4-e5f6-7890-abcd-ef1234567890
```

**成功响应** (200): `ApiResponse<UserDetailDto>`

```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "userName": "doctor_zhang",
  "realName": "张医生(已更名)",
  "role": "Admin",
  "email": "zhang_new@lybt.com",
  "phoneNumber": "13800138001",
  "isEnabled": true,
  "status": "Enabled",
  "pinYinCode": null,
  "lastLoginTime": "2026-06-20T14:30:00Z",
  "failedLoginCount": 0,
  "remark": "",
  "createdAt": "0001-01-01T00:00:00",
  "updatedAt": null
}
```

**错误码：**

| HTTP 状态码 | 说明 |
|------------|------|
| 404 | 用户不存在 (ERR-10001) |
| 403 | 系统管理员不可修改 / 权限不足 |
| 401 | — | 通用错误码见 [README](README.md#通用-http-状态码) |

---

## DELETE /users/{id}

删除用户 (软删除)。不能删除自己或 sysadmin。

> **权限**: `[Authorize(Policy = "AdminOrSuperAdmin")]`

**路径参数**: `id` (Guid) -- 用户 ID

**curl 示例**:

```bash
curl -X DELETE \
  -H "Authorization: Bearer <token>" \
  http://localhost:5000/api/v1/users/a1b2c3d4-e5f6-7890-abcd-ef1234567890
```

**成功响应** (200): `ApiResponse` — `data: null`

**错误码：**

| HTTP 状态码 | 说明 |
|------------|------|
| 404 | 用户不存在 (ERR-10001) |
| 403 | 不能删除自己 / 系统管理员不可删 / 权限不足 |
| 401 | — | 通用错误码见 [README](README.md#通用-http-状态码) |

---

## POST /users/{id}/reset-password

管理员重置用户密码，自动生成临时密码。

> **权限**: `[Authorize(Policy = "AdminOrSuperAdmin")]`

**路径参数**: `id` (Guid) -- 用户 ID

**请求体** (`ResetPasswordRequestDto`):

```json
{
  "mustChangeOnNextLogin": true
}
```

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `mustChangeOnNextLogin` | bool | true | 是否强制用户下次登录时修改密码 |

**curl 示例**:

```bash
curl -X POST \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"mustChangeOnNextLogin": true}' \
  http://localhost:5000/api/v1/users/a1b2c3d4-e5f6-7890-abcd-ef1234567890/reset-password
```

**成功响应** (200): `ApiResponse<ResetPasswordResponseDto>`

```json
{
  "success": true,
  "temporaryPassword": "Lybt2025@TempPass!"
}
```

**错误码：**

| HTTP 状态码 | 说明 |
|------------|------|
| 404 | 用户不存在 (ERR-10001) |
| 401/403 | — | 通用错误码见 [README](README.md#通用-http-状态码) |

---

## PUT /users/{id}/profile

修改个人资料。只能修改自己的资料。

> **权限**: `[Authorize]` (所有认证用户，自助端点)

**路径参数**: `id` (Guid) -- 当前用户 ID

**请求体** (`ChangeProfileDto`):

```json
{
  "realName": "张医生",
  "phoneNumber": "13900139001",
  "email": "zhang@lybt.com"
}
```

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `realName` | string | 是 | 真实姓名 |
| `phoneNumber` | string | 否 | 电话号码 |
| `email` | string | 否 | 邮箱 |

**curl 示例**:

```bash
curl -X PUT \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "realName": "张医生",
    "phoneNumber": "13900139001",
    "email": "zhang@lybt.com"
  }' \
  http://localhost:5000/api/v1/users/a1b2c3d4-e5f6-7890-abcd-ef1234567890/profile
```

**成功响应** (200): `ApiResponse<UserDetailDto>`

```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "userName": "doctor_zhang",
  "realName": "张医生",
  "role": "Doctor",
  "email": "zhang@lybt.com",
  "phoneNumber": "13900139001",
  "isEnabled": true,
  "status": "Enabled",
  "pinYinCode": null,
  "lastLoginTime": "2026-06-20T14:30:00Z",
  "failedLoginCount": 0,
  "remark": "",
  "createdAt": "0001-01-01T00:00:00",
  "updatedAt": null
}
```

**错误码：**

| HTTP 状态码 | 说明 |
|------------|------|
| 404 | 用户不存在 (ERR-10001) |
| 403 | 只能修改自己的个人资料 |
| 401 | — | 通用错误码见 [README](README.md#通用-http-状态码) |

---

## PUT /users/{id}/change-password

用户修改密码。只能修改自己的密码。

> **权限**: `[Authorize]` (所有认证用户，自助端点)

**路径参数**: `id` (Guid) -- 当前用户 ID

**请求体** (`Auth.ChangePasswordRequest`):

```json
{
  "oldPassword": "OldPass@2026!",
  "newPassword": "NewPass@2026!"
}
```

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `oldPassword` | string | 是 | 旧密码 |
| `newPassword` | string | 是 | 新密码 (8-50 字符) |

**curl 示例**:

```bash
curl -X PUT \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "oldPassword": "OldPass@2026!",
    "newPassword": "NewPass@2026!"
  }' \
  http://localhost:5000/api/v1/users/a1b2c3d4-e5f6-7890-abcd-ef1234567890/change-password
```

**成功响应** (200): `ApiResponse` — `data: null`

**错误码：**

| HTTP 状态码 | 说明 |
|------------|------|
| 400 | 旧密码错误 (ERR-10004) / 密码不符合要求 (ERR-10005) |
| 404 | 用户不存在 (ERR-10001) |
| 403 | 只能修改自己的密码 |
| 401 | — | 通用错误码见 [README](README.md#通用-http-状态码) |

---

## POST /users/{id}/toggle-status

切换用户状态 (启用/禁用)。sysadmin 不可被禁用。

> **权限**: `[Authorize(Policy = "AdminOrSuperAdmin")]`

**路径参数**: `id` (Guid) -- 用户 ID

**curl 示例**:

```bash
curl -X POST \
  -H "Authorization: Bearer <token>" \
  http://localhost:5000/api/v1/users/a1b2c3d4-e5f6-7890-abcd-ef1234567890/toggle-status
```

**成功响应** (200): `ApiResponse<UserDetailDto>` — 返回切换后的用户详情（`isEnabled`/`status` 取反）

**错误码：**

| HTTP 状态码 | 说明 |
|------------|------|
| 404 | 用户不存在 (ERR-10001) |
| 403 | 系统管理员不可禁用 / 权限不足 |
| 401 | — | 通用错误码见 [README](README.md#通用-http-状态码) |

---

## POST /users/{id}/restore

> 🚧 **v1.0 待实现（D4 Restore 软删除恢复补回，基线§1）**：基础设施已就绪，端点待补。

恢复已删除的用户。

> **权限**: `[Authorize(Policy = "AdminOrSuperAdmin")]`

**路径参数**: `id` (Guid) -- 用户 ID

**curl 示例**:

```bash
curl -X POST \
  -H "Authorization: Bearer <token>" \
  http://localhost:5000/api/v1/users/a1b2c3d4-e5f6-7890-abcd-ef1234567890/restore
```

**成功响应** (200): `ApiResponse<UserDetailDto>`

**错误码：**

| HTTP 状态码 | 说明 |
|------------|------|
| 404 | 用户不存在 (ERR-10001) |
| 401/403 | — | 通用错误码见 [README](README.md#通用-http-状态码) |

---

## POST /users/batch-delete

批量删除用户。自动排除当前登录用户 (防止删除自己) 和 sysadmin。

> **权限**: `[Authorize(Policy = "AdminOrSuperAdmin")]`

**请求体** (`BatchDeleteInputDto`):

```json
{
  "ids": [
    "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "b2c3d4e5-f6a7-8901-bcde-f12345678901"
  ]
}
```

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `ids` | Guid[] | 是 | 用户 ID 列表 (至少 1 个) |

**curl 示例**:

```bash
curl -X POST \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "ids": [
      "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "b2c3d4e5-f6a7-8901-bcde-f12345678901"
    ]
  }' \
  http://localhost:5000/api/v1/users/batch-delete
```

**成功响应** (200): `ApiResponse<BatchOperationResultDto>` — 部分失败时 `failedItems` 含各失败原因

**错误码：**

| HTTP 状态码 | 说明 |
|------------|------|
| 400 | ID 列表为空 (ERR-00003) |
| 401/403 | — | 通用错误码见 [README](README.md#通用-http-状态码) |

---

## POST /users/batch-enable

> 🚧 **v1.0 待实现（D4 Restore/批量操作补回，基线§1）**。

批量启用用户。

> **权限**: `[Authorize(Policy = "AdminOrSuperAdmin")]`

**请求体** (`BatchDeleteInputDto`):

```json
{
  "ids": [
    "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "b2c3d4e5-f6a7-8901-bcde-f12345678901"
  ]
}
```

**curl 示例**:

```bash
curl -X POST \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "ids": [
      "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "b2c3d4e5-f6a7-8901-bcde-f12345678901"
    ]
  }' \
  http://localhost:5000/api/v1/users/batch-enable
```

**成功响应** (200): `ApiResponse<BatchOperationResultDto>`

**业务规则**:
1. 不能启用/禁用当前登录用户
2. 每个目标用户都需通过 `CanManageUser` 校验
3. 部分失败不影响其他项

**错误码：**

| HTTP 状态码 | 说明 |
|------------|------|
| 400 | ID 列表为空 (ERR-00003) |
| 401/403 | — | 通用错误码见 [README](README.md#通用-http-状态码) |

---

## POST /users/batch-disable

> 🚧 **v1.0 待实现（D4 Restore/批量操作补回，基线§1）**。

批量禁用用户。

> **权限**: `[Authorize(Policy = "AdminOrSuperAdmin")]`

**请求体** (`BatchDeleteInputDto`):

```json
{
  "ids": [
    "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "b2c3d4e5-f6a7-8901-bcde-f12345678901"
  ]
}
```

**curl 示例**:

```bash
curl -X POST \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "ids": [
      "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "b2c3d4e5-f6a7-8901-bcde-f12345678901"
    ]
  }' \
  http://localhost:5000/api/v1/users/batch-disable
```

**成功响应** (200): `ApiResponse<BatchOperationResultDto>` — 部分失败时 `failedItems` 含各失败原因

**业务规则**:
1. 不能禁用当前登录用户
2. 每个目标用户都需通过 `CanManageUser` 校验
3. 禁用 Admin/SuperAdmin 时，若活跃管理员仅剩 1 人，该项标记为失败
4. 禁用成功的用户触发 Token 撤销
5. 部分失败不影响其他项

**`FailedItems` 可能的失败原因**:
- "不能禁用当前登录用户"
- "无权限禁用该用户"
- "不能禁用最后一个管理员"
- "用户不存在"

**错误码：**

| HTTP 状态码 | 说明 |
|------------|------|
| 400 | ID 列表为空 (ERR-00003) |
| 401/403 | — | 通用错误码见 [README](README.md#通用-http-状态码) |

---

## 错误码

> 完整错误码定义见 [users.md PRD](../02-requirements/03-users.md)。错误码分区: 1xxxx。

| 错误码 | 枚举名 | HTTP | 用户消息 | 触发端点 |
|--------|--------|------|----------|----------|
| ERR-10001 | UserNotFound | 404 | 用户不存在 | GET/PUT/DELETE /{id}, POST /reset-password, PUT /profile, PUT /change-password, POST /toggle-status, POST /restore |
| ERR-10002 | UserNameExists | 400 | 用户名已被使用 | POST / |
| ERR-10003 | EmailExists | 409 | 邮箱已被使用 | POST /, PUT /{id} |
| ERR-10004 | InvalidPassword | 400 | 旧密码错误 | PUT /change-password |
| ERR-10005 | PasswordPolicyViolation | 400 | 密码不符合安全策略 | POST /reset-password, PUT /change-password |
| ERR-10006 | UserDisabled | 403 | 用户账号已被禁用 | 登录验证 |
| ERR-00003 | ValidationFailed | 400 | 输入数据验证失败 | POST /, PUT /{id} |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本，14 个端点 |
| 2026-02-18 | v1.1 | 新增错误码章节: 补充端点级 MCCEE 错误码 (ERR-10001~10006, ERR-00003) |
| 2026-02-19 | v1.2 | toggle-status 端点补充业务规则: USER-D03 最后管理员保护、Token Family 失效、错误响应 |
| 2026-02-23 | v1.3 | S2-07/08: toggle-status 权限层级说明 + 错误码修正 (422); batch-enable/disable 业务规则 (逐项权限、管理员保护、Token撤销) |
| 2026-06-12 | v1.4 | 权限标注对齐实际代码: 类级别 [Authorize] + 方法级策略; current/profile/change-password 标注自助端点; reset-password/restore 改为 AdminOrSuperAdmin; toggle-status 422 标注 Service 层动态返回; change-password 请求类型改为 Auth.ChangePasswordRequest |
| 2026-06-12 | v1.5 | UserDetailDto: role 补全 4 角色; 新增 isEnabled/pinYinCode/lastLoginTime/failedLoginCount/remark 字段 |
| 2026-06-25 | v1.6 | 补充所有端点完整 curl 命令 + 请求/响应 JSON 示例 + 字段说明表; 权限策略统一为 AdminOnly (对齐实际代码); reset-password 请求体修正为 MustChangeOnNextLogin (对齐 ResetPasswordRequestDto) |
| 2026-06-28 | v1.7 | 文档对齐基线：权限策略 AdminOnly→AdminOrSuperAdmin（对齐 PolicyConstants）；响应信封 code→success（基线§6）；restore/batch-enable/batch-disable 标 D4 v1.0 待实现 |
| 2026-06-28 | v1.8 | 文档结构优化批次1：JSON 示例去 ApiResponse 外壳只留 data；错误响应 JSON 块合并到错误码表；删除「通用响应格式」节（README 已集中化）；curl 引用 README TOKEN |
