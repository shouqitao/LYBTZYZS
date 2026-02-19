# 用户 API

> Controller: `UsersController` | 路由前缀: `/api/v1/users` | 默认权限: `[Authorize(Policy = "AdminOnly")]`

## 概述

用户管理 CRUD、密码管理、状态切换、批量操作。仅限 Admin/SuperAdmin 角色访问。

---

## GET /users

获取用户列表 (分页)。

**查询参数**:

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `page` | int | 1 | 页码 |
| `pageSize` | int | 20 | 每页大小 |
| `keyword` | string? | null | 搜索关键词 |
| `role` | UserRole? | null | 角色筛选 (Doctor/Admin) |
| `status` | CommonStatus? | null | 状态筛选 (Enabled/Disabled) |

**成功响应** (200): `ApiResponse<PagedResult<UserListDto>>`

---

## GET /users/current

获取当前登录用户信息。支持 SuperAdmin 特殊处理 (Id=Guid.Empty)。

**成功响应** (200): `ApiResponse<UserDetailDto>`

**错误响应**: 401 (未登录或用户信息无效)

---

## GET /users/{id}

获取单个用户详情。

**路径参数**: `id` (Guid) -- 用户 ID

**成功响应** (200): `ApiResponse<UserDetailDto>`

**UserDetailDto**:

```json
{
  "id": "guid",
  "userName": "string",
  "realName": "string",
  "role": "Doctor|Admin",
  "email": "string",
  "phoneNumber": "string",
  "status": "Enabled|Disabled",
  "createdAt": "datetime",
  "updatedAt": "datetime"
}
```

**错误响应**: 404 (用户不存在)

---

## POST /users

创建新用户。

**请求体** (`UserInputDto`):

```json
{
  "userName": "string",
  "realName": "string",
  "password": "string",
  "role": "Doctor|Admin",
  "email": "string",
  "phoneNumber": "string"
}
```

**成功响应** (201): `ApiResponse<UserDetailDto>` + `Location` 头

**错误响应**: 400 (创建失败，如用户名重复)

---

## PUT /users/{id}

更新用户信息。

**路径参数**: `id` (Guid)

**请求体**: `UserInputDto` (同创建)

**成功响应** (200): `ApiResponse<UserDetailDto>`

---

## DELETE /users/{id}

删除用户 (软删除)。

**路径参数**: `id` (Guid)

**成功响应** (200): `ApiResponse` ("删除成功")

**错误响应**: 404 (用户不存在)

---

## POST /users/{id}/reset-password

管理员重置用户密码。

**路径参数**: `id` (Guid)

**请求体** (`ResetPasswordRequestDto`):

```json
{
  "newPassword": "string"   // 可选，不传则自动生成
}
```

**成功响应** (200): `ApiResponse<ResetPasswordResponseDto>`

```json
{
  "data": {
    "newPassword": "string",    // 新密码 (自动生成时返回)
    "message": "string"
  }
}
```

---

## PUT /users/{id}/profile

修改个人资料。

**路径参数**: `id` (Guid)

**请求体** (`ChangeProfileDto`):

```json
{
  "realName": "string",
  "phoneNumber": "string",
  "email": "string"
}
```

**成功响应** (200): `ApiResponse<UserDetailDto>`

---

## PUT /users/{id}/change-password

用户修改密码。

**路径参数**: `id` (Guid)

**请求体** (`ChangePasswordRequest`):

```json
{
  "oldPassword": "string",   // 必填，旧密码
  "newPassword": "string"    // 必填，新密码
}
```

**成功响应** (200): `ApiResponse` ("密码修改成功")

**错误响应**: 400 (旧密码错误等)

---

## POST /users/{id}/toggle-status

切换用户状态 (启用/禁用)。

> **权限**: `[Authorize(Policy = "AdminOnly")]`

**路径参数**: `id` (Guid)

**业务规则**:
1. **最后管理员保护** (USER-D03): 不能禁用最后一个 SuperAdmin 或最后一个 Admin (返回 403)
2. 禁用用户时所有 Token Family 失效 (AUTH-D06 复用)
3. 禁用后当前会话立即失效，尝试登录返回 ERR-10006 (UserDisabled)
4. 不能修改自己的状态 (返回 400)

**成功响应** (200): `ApiResponse<UserDetailDto>`

响应 message 示例: "用户已启用" 或 "用户已禁用"

**错误响应**:
- 400: 不能修改自己的状态
- 403: 最后管理员保护 -- "不能禁用最后一个{超级管理员|管理员}" (USER-D03)
- 404: 用户不存在 (ERR-10001)

---

## POST /users/{id}/restore

恢复已删除的用户。

**路径参数**: `id` (Guid)

**成功响应** (200): `ApiResponse<UserDetailDto>` ("用户已恢复")

---

## POST /users/batch-delete

批量删除用户。自动排除当前登录用户 (防止删除自己)。

**请求体** (`BatchDeleteInputDto`):

```json
{
  "ids": ["guid1", "guid2", ...]   // 至少 1 个
}
```

**成功响应** (200): `ApiResponse<BatchOperationResultDto>`

```json
{
  "data": {
    "successCount": 3,
    "failureCount": 0,
    "message": "批量删除完成"
  }
}
```

---

## POST /users/batch-enable

批量启用用户。

**请求体**: `BatchDeleteInputDto` (同 batch-delete)

**成功响应** (200): `ApiResponse<BatchOperationResultDto>`

---

## POST /users/batch-disable

批量禁用用户。

**请求体**: `BatchDeleteInputDto` (同 batch-delete)

**成功响应** (200): `ApiResponse<BatchOperationResultDto>`

---

## 错误码

> 完整错误码定义见 [users.md PRD](../02-requirements/users.md)。错误码分区: 1xxxx。

| 错误码 | 枚举名 | HTTP | 用户消息 | 触发端点 |
|--------|--------|------|----------|----------|
| ERR-10001 | UserNotFound | 404 | 用户不存在 | GET/PUT/DELETE /{id}, POST /reset-password, POST /change-password, POST /change-profile, PUT /{id}/toggle-status, POST /{id}/restore |
| ERR-10002 | UserNameExists | 409 | 用户名已被使用 | POST / |
| ERR-10003 | EmailExists | 409 | 邮箱已被使用 | POST /, PUT /{id} |
| ERR-10004 | InvalidPassword | 401 | 用户名或密码错误 | POST /change-password |
| ERR-10005 | PasswordPolicyViolation | 400 | 密码不符合安全策略 | POST /reset-password, POST /change-password |
| ERR-10006 | UserDisabled | 403 | 用户账号已被禁用 | 登录验证 |
| ERR-00003 | ValidationFailed | 400 | 输入数据验证失败 | POST /, PUT /{id} |

---

**变更记录**

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本，14 个端点 |
| 2026-02-18 | v1.1 | 新增错误码章节: 补充端点级 MCCEE 错误码 (ERR-10001~10006, ERR-00003) |
| 2026-02-19 | v1.2 | toggle-status 端点补充业务规则: USER-D03 最后管理员保护、Token Family 失效、错误响应 |
