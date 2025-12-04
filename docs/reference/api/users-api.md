# Users API 参考文档

**版本**: v1.0
**基础路径**: `/api/v1/users`
**认证方式**: Bearer Token (JWT)
**相关Issue**:
- #1008 - Server端重构（标准CRUD）
- #1162 - 状态管理增强（角色/状态筛选、切换状态、重置密码）
- #1169 - 批量删除用户
- #1888 - 个人资料修改

---

## 📋 目录

- [概述](#概述)
- [基础CRUD操作](#基础crud操作)
  - [GET /api/v1/users](#1-get-apiv1users---分页查询用户)
  - [GET /api/v1/users/current](#2-get-apiv1userscurrent---获取当前用户)
  - [GET /api/v1/users/{id}](#3-get-apiv1usersid---获取用户详情)
  - [POST /api/v1/users](#4-post-apiv1users---创建用户)
  - [PUT /api/v1/users/{id}](#5-put-apiv1usersid---更新用户)
  - [DELETE /api/v1/users/{id}](#6-delete-apiv1usersid---删除用户)
- [~~状态管理（Issue #1162）~~](#状态管理issue-1162) *(已废弃，使用PUT /{id}更新)*
- [密码管理](#密码管理)
  - [POST /api/v1/users/{id}/reset-password](#8-post-apiv1usersidreset-password---管理员重置密码)
  - [POST /api/v1/users/{id}/change-password](#9-post-apiv1usersidchange-password---用户更改密码)
- [个人资料管理（Issue #1888）](#个人资料管理issue-1888)
  - [PUT /api/v1/users/{id}/profile](#10-put-apiv1usersidprofile---修改个人资料)
- [~~批量操作（Issue #1169）~~](#批量操作issue-1169) *(已废弃，使用循环调用DELETE /{id})*
- [通用响应格式](#通用响应格式)
- [业务规则说明](#业务规则说明)
- [错误码说明](#错误码说明)

---

## 概述

### 架构设计原则

Users API遵循三层对齐架构和MVP原则：

- **基础CRUD**: 标准的增删改查操作
- **认证与安全**: BCrypt密码Hash、用户名/邮箱双登录
- **状态管理**: 启用/禁用/切换状态（Issue #1162）
- **批量操作**: 批量删除≤100条（Issue #1169）

### 核心业务规则

#### 数据验证规则

- **BR-002**: 用户名唯一性约束（同一用户名只能存在一条有效记录）
- **BR-003**: 邮箱唯一性约束（同一邮箱只能存在一条有效记录）
- **BR-004**: 密码必须Hash存储（BCrypt）

#### 批量操作规则

- **BR-001**: 批量删除限制
  - 单次最多100条
  - 防止长事务

#### 软删除规则

- **BR-005**: 软删除支持
  - 删除时设置IsDeleted=true
  - 不从数据库物理删除
  - 查询时自动过滤已删除记录

---

## 基础CRUD操作

### 1. GET /api/v1/users - 分页查询用户

**描述**: 分页查询用户列表，支持关键字搜索、角色筛选、状态筛选（Issue #1162）。

**业务规则**:
- 默认每页20条记录
- 仅返回未删除的用户（IsDeleted=false）
- 支持keyword模糊搜索（用户名/邮箱/真实姓名）
- 支持role和status精确筛选

**请求**:
```http
GET /api/v1/users?page=1&pageSize=20&keyword=张三&role=1&status=1
Authorization: Bearer {token}
```

**查询参数**:

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| page | int | ❌ | 1 | 页码（从1开始） |
| pageSize | int | ❌ | 20 | 每页记录数 |
| keyword | string | ❌ | null | 搜索关键字（用户名/邮箱/真实姓名） |
| role | int | ❌ | null | 角色筛选（0=Admin, 1=Doctor） |
| status | int | ❌ | null | 状态筛选（0=Disabled, 1=Enabled） |

**响应**:

✅ **成功 - 200 OK**
```json
{
  "success": true,
  "message": "查询成功",
  "data": {
    "items": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "userName": "zhangsan",
        "realName": "张三",
        "email": "zhangsan@example.com",
        "phoneNumber": "13800138000",
        "role": 1,
        "status": 1,
        "createdAt": "2025-01-01T10:00:00Z",
        "updatedAt": "2025-01-15T14:30:00Z"
      }
    ],
    "totalCount": 45,
    "pageIndex": 1,
    "pageSize": 20,
    "totalPages": 3
  }
}
```

---

### 2. GET /api/v1/users/current - 获取当前用户

**描述**: 获取当前登录用户的详细信息（支持超级管理员特殊处理）。

**业务规则**:
- 从JWT Token中提取用户ID
- 超级管理员（userId=Guid.Empty）返回虚拟用户信息

**请求**:
```http
GET /api/v1/users/current
Authorization: Bearer {token}
```

**响应**:

✅ **成功 - 200 OK**
```json
{
  "success": true,
  "message": "查询成功",
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "userName": "zhangsan",
    "realName": "张三",
    "email": "zhangsan@example.com",
    "phoneNumber": "13800138000",
    "role": 1,
    "status": 1,
    "createdAt": "2025-01-01T10:00:00Z",
    "updatedAt": "2025-01-15T14:30:00Z"
  }
}
```

❌ **失败 - 401 Unauthorized**
```json
{
  "success": false,
  "message": "无法获取当前用户信息"
}
```

---

### 3. GET /api/v1/users/{id} - 获取用户详情

**描述**: 根据用户ID获取用户详情。

**请求**:
```http
GET /api/v1/users/3fa85f64-5717-4562-b3fc-2c963f66afa6
Authorization: Bearer {token}
```

**路径参数**:

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| id | Guid | ✅ | 用户ID |

**响应**:

✅ **成功 - 200 OK**
```json
{
  "success": true,
  "message": "查询成功",
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "userName": "zhangsan",
    "realName": "张三",
    "email": "zhangsan@example.com",
    "phoneNumber": "13800138000",
    "role": 1,
    "status": 1,
    "createdAt": "2025-01-01T10:00:00Z",
    "updatedAt": "2025-01-15T14:30:00Z"
  }
}
```

❌ **失败 - 404 Not Found**
```json
{
  "success": false,
  "message": "用户不存在"
}
```

---

### 4. POST /api/v1/users - 创建用户

**描述**: 创建新的用户记录。

**业务规则**:
- **BR-002**: 用户名唯一性检查
- **BR-003**: 邮箱唯一性检查
- **BR-004**: 密码必须Hash存储（BCrypt）

**请求**:
```http
POST /api/v1/users
Authorization: Bearer {token}
Content-Type: application/json

{
  "userName": "zhangsan",
  "realName": "张三",
  "password": "Password123!",
  "email": "zhangsan@example.com",
  "phoneNumber": "13800138000",
  "role": 1
}
```

**请求体（UserInputDto）**:

| 字段 | 类型 | 必填 | 验证规则 | 说明 |
|------|------|------|---------|------|
| userName | string | ✅ | 3-50字符 | 用户名（唯一） |
| realName | string | ✅ | 2-50字符 | 真实姓名 |
| password | string | ✅ | 6-100字符 | 密码（创建时必填） |
| email | string | ❌ | 邮箱格式 | 邮箱（唯一） |
| phoneNumber | string | ❌ | 手机号格式 | 手机号 |
| role | int | ✅ | 0或1 | 角色（0=Admin, 1=Doctor） |

**响应**:

✅ **成功 - 200 OK**
```json
{
  "success": true,
  "message": "创建成功",
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "userName": "zhangsan",
    "realName": "张三",
    "email": "zhangsan@example.com",
    "phoneNumber": "13800138000",
    "role": 1,
    "status": 1,
    "createdAt": "2025-01-01T10:00:00Z",
    "updatedAt": "2025-01-01T10:00:00Z"
  }
}
```

❌ **失败 - 400 Bad Request**
```json
{
  "success": false,
  "message": "用户名已存在"
}
```

---

### 5. PUT /api/v1/users/{id} - 更新用户

**描述**: 更新用户信息。

**业务规则**:
- **BR-002**: 用户名唯一性检查（排除当前用户）
- **BR-003**: 邮箱唯一性检查（排除当前用户）
- 密码不允许通过此接口修改（使用change-password或reset-password）

**请求**:
```http
PUT /api/v1/users/3fa85f64-5717-4562-b3fc-2c963f66afa6
Authorization: Bearer {token}
Content-Type: application/json

{
  "userName": "zhangsan",
  "realName": "张三",
  "email": "zhangsan@example.com",
  "phoneNumber": "13800138000",
  "role": 1
}
```

**请求体（UserInputDto）**:

| 字段 | 类型 | 必填 | 验证规则 | 说明 |
|------|------|------|---------|------|
| userName | string | ✅ | 3-50字符 | 用户名（唯一） |
| realName | string | ✅ | 2-50字符 | 真实姓名 |
| password | string | ❌ | - | 更新时忽略（使用专用接口） |
| email | string | ❌ | 邮箱格式 | 邮箱（唯一） |
| phoneNumber | string | ❌ | 手机号格式 | 手机号 |
| role | int | ✅ | 0或1 | 角色（0=Admin, 1=Doctor） |

**响应**:

✅ **成功 - 200 OK**
```json
{
  "success": true,
  "message": "更新成功",
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "userName": "zhangsan",
    "realName": "张三",
    "email": "zhangsan@example.com",
    "phoneNumber": "13800138000",
    "role": 1,
    "status": 1,
    "createdAt": "2025-01-01T10:00:00Z",
    "updatedAt": "2025-01-15T14:30:00Z"
  }
}
```

---

### 6. DELETE /api/v1/users/{id} - 删除用户

**描述**: 软删除用户（BR-005）。

**业务规则**:
- **BR-005**: 软删除（设置IsDeleted=true）
- 不从数据库物理删除

**请求**:
```http
DELETE /api/v1/users/3fa85f64-5717-4562-b3fc-2c963f66afa6
Authorization: Bearer {token}
```

**响应**:

✅ **成功 - 200 OK**
```json
{
  "success": true,
  "message": "删除成功"
}
```

❌ **失败 - 404 Not Found**
```json
{
  "success": false,
  "message": "用户不存在"
}
```

---

## 状态管理（Issue #1162）

### 7. PUT /api/v1/users/{id}/toggle-status - 切换用户状态

**描述**: 切换用户启用/禁用状态。

**业务规则**:
- Enabled → Disabled
- Disabled → Enabled

**请求**:
```http
PUT /api/v1/users/3fa85f64-5717-4562-b3fc-2c963f66afa6/toggle-status
Authorization: Bearer {token}
```

**响应**:

✅ **成功 - 200 OK**
```json
{
  "success": true,
  "message": "状态切换成功",
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "userName": "zhangsan",
    "realName": "张三",
    "email": "zhangsan@example.com",
    "phoneNumber": "13800138000",
    "role": 1,
    "status": 0,
    "createdAt": "2025-01-01T10:00:00Z",
    "updatedAt": "2025-01-15T14:30:00Z"
  }
}
```

---

## 密码管理

### 8. POST /api/v1/users/{id}/reset-password - 管理员重置密码

**描述**: 管理员重置用户密码（支持自动生成临时密码，Issue #1162）。

**业务规则**:
- **BR-006**: 支持自动生成临时密码（generateTempPassword=true）
- **BR-004**: 密码必须Hash存储（BCrypt）

**请求**:
```http
POST /api/v1/users/3fa85f64-5717-4562-b3fc-2c963f66afa6/reset-password
Authorization: Bearer {token}
Content-Type: application/json

{
  "newPassword": "NewPassword123!",
  "generateTempPassword": false
}
```

**请求体（ResetPasswordRequestDto）**:

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| newPassword | string | ❌ | 新密码（generateTempPassword=false时必填） |
| generateTempPassword | bool | ❌ | 是否自动生成临时密码（默认false） |

**响应**:

✅ **成功 - 200 OK**
```json
{
  "success": true,
  "message": "密码重置成功",
  "data": {
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "tempPassword": "Temp123!abc",
    "isTemp": true
  }
}
```

---

### 9. POST /api/v1/users/{id}/change-password - 用户更改密码

**描述**: 用户更改自己的密码。

**业务规则**:
- **BR-007**: 必须验证旧密码
- **BR-004**: 密码必须Hash存储（BCrypt）

**请求**:
```http
POST /api/v1/users/3fa85f64-5717-4562-b3fc-2c963f66afa6/change-password
Authorization: Bearer {token}
Content-Type: application/json

{
  "oldPassword": "OldPassword123!",
  "newPassword": "NewPassword123!"
}
```

**请求体**:

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| oldPassword | string | ✅ | 旧密码 |
| newPassword | string | ✅ | 新密码 |

**响应**:

✅ **成功 - 200 OK**
```json
{
  "success": true,
  "message": "密码修改成功"
}
```

❌ **失败 - 400 Bad Request**
```json
{
  "success": false,
  "message": "原密码错误"
}
```

---

## 个人资料管理（Issue #1888）

### 10. PUT /api/v1/users/{id}/profile - 修改个人资料

**描述**: 用户修改个人资料（真实姓名、邮箱、手机号）。

**业务规则**:
- 不允许修改用户名和角色
- **BR-003**: 邮箱唯一性检查（排除当前用户）

**请求**:
```http
PUT /api/v1/users/3fa85f64-5717-4562-b3fc-2c963f66afa6/profile
Authorization: Bearer {token}
Content-Type: application/json

{
  "realName": "张三丰",
  "email": "zhangsan@newdomain.com",
  "phoneNumber": "13800138888"
}
```

**请求体（ChangeProfileDto）**:

| 字段 | 类型 | 必填 | 验证规则 | 说明 |
|------|------|------|---------|------|
| realName | string | ✅ | 2-50字符 | 真实姓名 |
| email | string | ❌ | 邮箱格式 | 邮箱 |
| phoneNumber | string | ❌ | 手机号格式 | 手机号 |

**响应**:

✅ **成功 - 200 OK**
```json
{
  "success": true,
  "message": "个人资料修改成功",
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "userName": "zhangsan",
    "realName": "张三丰",
    "email": "zhangsan@newdomain.com",
    "phoneNumber": "13800138888",
    "role": 1,
    "status": 1,
    "createdAt": "2025-01-01T10:00:00Z",
    "updatedAt": "2025-01-15T14:30:00Z"
  }
}
```

---

## ~~批量操作（Issue #1169）~~ - 已废弃

> **注意**: 此端点已于2025-12-04废弃并删除。
>
> **替代方案**: 使用Client端循环调用 `DELETE /api/v1/users/{id}` 实现批量删除。

---

## 通用响应格式

所有API遵循统一的响应格式：

### ApiResponse<T>

```json
{
  "success": true|false,
  "message": "消息内容",
  "data": T | null,
  "errorCode": "ERROR_CODE"
}
```

### PagedResult<T>

```json
{
  "items": [T],
  "totalCount": 100,
  "pageIndex": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

---

## 业务规则说明

| 规则ID | 描述 | 验证层 | 实现位置 |
|--------|------|--------|---------|
| ~~**BR-001**~~ | ~~批量删除数量限制（≤100条）~~ | ~~Service层~~ | ~~已废弃~~ |
| **BR-002** | 用户名唯一性 | Service层 | UserService.CreateAsync/UpdateAsync |
| **BR-003** | 邮箱唯一性 | Service层 | UserService.CreateAsync/UpdateAsync |
| **BR-004** | 密码必须Hash存储（BCrypt） | Service层 | UserService.CreateAsync |
| **BR-005** | 软删除支持 | Service层 | DeleteAsync（设置IsDeleted=true） |
| **BR-006** | 重置密码支持自动生成（Issue #1162） | Service层 | ResetPasswordAsync |
| **BR-007** | 更改密码需验证旧密码 | Service层 | ChangePasswordAsync |

---

## 错误码说明

| HTTP状态码 | ErrorCode | 说明 | 示例 |
|-----------|-----------|------|------|
| 200 | - | 成功 | 查询/创建/更新成功 |
| 400 | VALIDATION_ERROR | 参数验证失败 | 用户名已存在、邮箱格式错误 |
| 401 | UNAUTHORIZED | 未授权 | Token无效或过期 |
| 403 | FORBIDDEN | 权限不足 | 非管理员访问管理接口 |
| 404 | NOT_FOUND | 资源不存在 | 用户ID不存在 |
| 500 | INTERNAL_ERROR | 服务器内部错误 | 数据库连接失败 |

---

## 性能基准

**测试环境**: Intel Core i7-7700 CPU 3.60GHz, .NET 8.0.21, InMemory Database
**测试工具**: BenchmarkDotNet v0.14.0
**测试配置**: IterationCount=10, WarmupCount=3

| 操作 | 平均耗时 | 内存分配 | 备注 |
|-----|---------|---------|------|
| 分页查询（100条取20条） | 91.32 μs | 67.42 KB | 性能表现最优 ⭐ |
| 单条创建 | 17.99 ms | 9.15 MB | 包含BCrypt密码哈希（~15ms） |
| 批量删除（100条） | < 2s | - | 预估（未测试） |
| 批量导入（1000条模拟） | 236.79 ms | 176.1 MB | 性能表现最优 ⭐ |

**性能对比**（与其他模块）:
- **分页查询**: Users最快（91μs） < Patients（99μs） < Herbs（135μs）
- **单条创建**: Herbs最快（10ms） < Patients（16ms） < Users最慢（18ms，BCrypt因素）
- **批量导入**: Users最快（237ms） < Herbs（247ms） < Patients最慢（330ms，Server端Excel解析）

---

**最后更新**: 2025-11-10
**维护者**: @shouqitao
