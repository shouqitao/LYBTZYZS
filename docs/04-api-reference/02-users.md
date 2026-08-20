# 用户 API

> Controller: `UsersController` | 路由前缀: `/api/v1/users` | 默认权限: `[Authorize]`

## 概述

用户管理 CRUD、密码管理、状态切换、批量操作。管理端点使用 `[Authorize(Policy = "AdminOrSuperAdmin")]`，自助端点 (`current`/`profile`/`change-password`) 允许所有认证用户。

> **响应信封**：所有响应为 `ApiResponse<T>`，字段定义与通用错误码见 [README](README.md)。下文成功响应示例仅展示 `data` 内容。

---

## UserDetailDto 字段说明

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

---

## GET /users

获取用户列表（分页）。> **权限**: AdminOrSuperAdmin

**查询参数**:

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `page` | int | 1 | 页码 |
| `pageSize` | int | 20 | 每页大小 |
| `keyword` | string? | null | 搜索关键词 (匹配用户名/姓名/邮箱/手机号) |
| `role` | UserRole? | null | 角色筛选 |
| `status` | CommonStatus? | null | 状态筛选 |

```bash
curl -H "Authorization: Bearer ***" \
  "http://localhost:5000/api/v1/users?keyword=张&role=Doctor&page=1&pageSize=10"
```

**成功响应** (200): `ApiResponse<PagedResult<UserListDto>>`

```json
{
  "items": [
    { "id": "...", "userName": "doctor_zhang", "realName": "张医生",
      "role": "Doctor", "status": "Enabled", "lastLoginTime": "...", ... },
    { "id": "...", "userName": "admin_li", "realName": "李管理",
      "role": "Admin", "status": "Enabled", ... }
  ],
  "totalCount": 2, "page": 1, "pageSize": 20, "totalPages": 1
}
```

| 错误码 | 说明 |
|--------|------|
| 400 | 分页参数验证失败 (ERR-00003) |
| 401/403/404 | 通用错误码见 [README](README.md#通用-http-状态码) |

---

## GET /users/current

获取当前登录用户信息。SuperAdmin 使用 `Id=Guid.Empty` 特殊处理。> **权限**: 所有认证用户（自助端点）

```bash
curl -H "Authorization: Bearer ***" http://localhost:5000/api/v1/users/current
```

**成功响应** (200): `ApiResponse<UserDetailDto>`

```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "userName": "doctor_zhang", "realName": "张医生", "role": "Doctor",
  "email": "zhang@lybt.com", "phoneNumber": "13800138001",
  "isEnabled": true, "status": "Enabled", ...
}
```

**SuperAdmin 特殊响应**: `id` 为 `"00000000-0000-0000-0000-000000000000"`，`role` 固定返回 `"Admin"`。

| 错误码 | 说明 |
|--------|------|
| 401/403/404 | 通用错误码见 [README](README.md#通用-http-状态码) |

---

## GET /users/{id}

获取单个用户详情。> **权限**: AdminOrSuperAdmin

**路径参数**: `id` (Guid) -- 用户 ID

```bash
curl -H "Authorization: Bearer ***" \
  http://localhost:5000/api/v1/users/a1b2c3d4-e5f6-7890-abcd-ef1234567890
```

**成功响应** (200): `ApiResponse<UserDetailDto>` — 返回完整用户详情（字段见上方 Dto 说明）。

| 错误码 | 说明 |
|--------|------|
| 404 | 用户不存在 (ERR-10001) |
| 401/403 | 通用错误码见 [README](README.md#通用-http-状态码) |

---

## POST /users

创建新用户。> **权限**: AdminOrSuperAdmin

**请求体** (`UserInputDto`):

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `userName` | string | 是 | 用户名 (3-32 字符，字母/数字/下划线) |
| `realName` | string | 是 | 真实姓名 |
| `password` | string | 否 | 密码 (6-128 字符，不传则使用默认密码) |
| `role` | UserRole | 否 | 角色，默认 Doctor |
| `email` | string | 否 | 邮箱 |
| `phoneNumber` | string | 否 | 手机号码 |
| `remark` | string | 否 | 备注 |

```bash
curl -X POST -H "Authorization: Bearer ***" \
  -H "Content-Type: application/json" \
  -d '{"userName":"doctor_wang","realName":"王医生","role":"Doctor"}' \
  http://localhost:5000/api/v1/users
```

**成功响应** (201): `ApiResponse<UserDetailDto>` + `Location` 头

```json
{
  "id": "c3d4e5f6-a7b8-9012-cdef-123456789012",
  "userName": "doctor_wang", "realName": "王医生", "role": "Doctor",
  "isEnabled": true, "status": "Enabled", ...
}
```

| 错误码 | 说明 |
|--------|------|
| 400 | 用户名已被使用 (ERR-10002) / 验证失败 (ERR-00003) / 系统保留名 |
| 401/403 | 通用错误码见 [README](README.md#通用-http-状态码) |

---

## PUT /users/{id}

更新用户信息。sysadmin 账号不可修改。> **权限**: AdminOrSuperAdmin

**路径参数**: `id` (Guid) -- 用户 ID

**请求体** (`UserInputDto`): 同 POST /users，字段均为可选。

| 错误码 | 说明 |
|--------|------|
| 404 | 用户不存在 (ERR-10001) |
| 403 | 系统管理员不可修改 / 权限不足 |
| 401 | 通用错误码见 [README](README.md#通用-http-状态码) |

---

## DELETE /users/{id}

删除用户（软删除）。不能删除自己或 sysadmin。> **权限**: AdminOrSuperAdmin

**路径参数**: `id` (Guid) -- 用户 ID

**成功响应** (200): `ApiResponse` — `data: null`

| 错误码 | 说明 |
|--------|------|
| 404 | 用户不存在 (ERR-10001) |
| 403 | 不能删除自己 / 系统管理员不可删 / 权限不足 |
| 401 | 通用错误码见 [README](README.md#通用-http-状态码) |

---

## POST /users/{id}/reset-password

管理员重置用户密码，自动生成临时密码。> **权限**: AdminOrSuperAdmin

**路径参数**: `id` (Guid) -- 用户 ID

**请求体** (`ResetPasswordRequestDto`):

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `mustChangeOnNextLogin` | bool | true | 是否强制用户下次登录时修改密码 |

```bash
curl -X POST -H "Authorization: Bearer ***" \
  -H "Content-Type: application/json" \
  -d '{"mustChangeOnNextLogin":true}' \
  http://localhost:5000/api/v1/users/{id}/reset-password
```

**成功响应** (200): `ApiResponse<ResetPasswordResponseDto>`

```json
{ "success": true, "temporaryPassword": "Lybt2025@TempPass!" }
```

| 错误码 | 说明 |
|--------|------|
| 404 | 用户不存在 (ERR-10001) |
| 401/403 | 通用错误码见 [README](README.md#通用-http-状态码) |

---

## PUT /users/{id}/profile

修改个人资料。只能修改自己的资料。> **权限**: 所有认证用户（自助端点）

**路径参数**: `id` (Guid) -- 当前用户 ID

**请求体** (`ChangeProfileDto`):

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `realName` | string | 是 | 真实姓名 |
| `phoneNumber` | string | 否 | 电话号码 |
| `email` | string | 否 | 邮箱 |

**成功响应** (200): `ApiResponse<UserDetailDto>` — 返回更新后的用户详情。

| 错误码 | 说明 |
|--------|------|
| 404 | 用户不存在 (ERR-10001) |
| 403 | 只能修改自己的个人资料 |
| 401 | 通用错误码见 [README](README.md#通用-http-状态码) |

---

## PUT /users/{id}/change-password

用户修改密码。只能修改自己的密码。> **权限**: 所有认证用户（自助端点）

**路径参数**: `id` (Guid) -- 当前用户 ID

**请求体** (`Auth.ChangePasswordRequest`):

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `oldPassword` | string | 是 | 旧密码 |
| `newPassword` | string | 是 | 新密码 (8-50 字符) |

**成功响应** (200): `ApiResponse` — `data: null`

| 错误码 | 说明 |
|--------|------|
| 400 | 旧密码错误 (ERR-10004) / 密码不符合要求 (ERR-10005) |
| 404 | 用户不存在 (ERR-10001) |
| 403 | 只能修改自己的密码 |
| 401 | 通用错误码见 [README](README.md#通用-http-状态码) |

---

## POST /users/{id}/toggle-status

切换用户状态（启用/禁用）。sysadmin 不可被禁用。> **权限**: AdminOrSuperAdmin

**路径参数**: `id` (Guid) -- 用户 ID

**成功响应** (200): `ApiResponse<UserDetailDto>` — 返回切换后的用户详情（`isEnabled`/`status` 取反）。

| 错误码 | 说明 |
|--------|------|
| 404 | 用户不存在 (ERR-10001) |
| 403 | 系统管理员不可禁用 / 权限不足 |
| 401 | 通用错误码见 [README](README.md#通用-http-状态码) |

---

## POST /users/{id}/restore

恢复已删除的用户。> **权限**: AdminOrSuperAdmin

**路径参数**: `id` (Guid) -- 用户 ID

**成功响应** (200): `ApiResponse<UserDetailDto>`

| 错误码 | 说明 |
|--------|------|
| 404 | 用户不存在 (ERR-10001) |
| 401/403 | 通用错误码见 [README](README.md#通用-http-状态码) |

---

## 批量操作

以下三个端点共享相同的请求体结构和业务规则。

**请求体** (`BatchDeleteInputDto`):

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `ids` | Guid[] | 是 | 用户 ID 列表 (至少 1 个) |

**共同业务规则**:
- 不能对当前登录用户操作
- 每个目标用户都需通过 `CanManageUser` 校验
- 部分失败不影响其他项（`failedItems` 含各失败原因）

```bash
curl -X POST -H "Authorization: Bearer ***" \
  -H "Content-Type: application/json" \
  -d '{"ids":["a1b2c3d4-e5f6-7890-abcd-ef1234567890"]}' \
  http://localhost:5000/api/v1/users/batch-delete
```

### POST /users/batch-delete

批量删除用户。自动排除当前登录用户和 sysadmin。

**成功响应** (200): `ApiResponse<BatchOperationResultDto>` — 部分失败时 `failedItems` 含各失败原因。

### POST /users/batch-enable

批量启用用户。

**成功响应** (200): `ApiResponse<BatchOperationResultDto>`

### POST /users/batch-disable

批量禁用用户。禁用最后一个管理员时该项标记为失败；禁用成功的用户触发 Token 撤销。

**`FailedItems` 可能的失败原因**: 不能禁用当前登录用户 / 无权限禁用该用户 / 不能禁用最后一个管理员 / 用户不存在

**成功响应** (200): `ApiResponse<BatchOperationResultDto>` — 部分失败时 `failedItems` 含各失败原因。

**共同错误码:**

| 错误码 | 说明 |
|--------|------|
| 400 | ID 列表为空 (ERR-00003) |
| 401/403 | 通用错误码见 [README](README.md#通用-http-状态码) |

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
