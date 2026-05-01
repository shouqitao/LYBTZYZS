# LocalWebAPI API 端点

## 概述

LocalWebAPI 提供 8 个控制器，共约 40 个端点。所有端点位于 `http://127.0.0.1:{dynamicPort}/api/`。

端口号由 OS 动态分配，通过 `LocalWebApiHost.Port` 获取。

## 认证端点

### POST /api/auth/login

登录端点。

**请求体:**
```json
{ "Username": "admin", "Password": "admin" }
```

**成功响应 (200):**
```json
{ "Token": "eyJ...", "UserId": "...", "Username": "admin", "Role": "Admin" }
```

**失败响应 (401):** 用户名或密码错误

## 用户端点

| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| GET | /api/users | Required | 用户列表 |
| GET | /api/users/{id} | Required | 用户详情 |
| POST | /api/users | Required | 创建用户 |
| PUT | /api/users/{id} | Required | 更新用户 |
| DELETE | /api/users/{id} | Required | 软删除用户 |

## 患者端点

| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| GET | /api/patients?keyword=&page=&pageSize= | Required | 患者列表 (分页) |
| GET | /api/patients/{id} | Required | 患者详情 |
| POST | /api/patients | Required | 创建患者 |
| PUT | /api/patients/{id} | Required | 更新患者 |
| DELETE | /api/patients/{id} | Required | 软删除患者 |

## 药材端点

| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| GET | /api/herbs?keyword=&page=&pageSize= | Required | 药材列表 (分页) |
| GET | /api/herbs/{id} | Required | 药材详情 |
| POST | /api/herbs | Required | 创建药材 |
| PUT | /api/herbs/{id} | Required | 更新药材 |
| DELETE | /api/herbs/{id} | Required | 软删除药材 |

## 验方端点

| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| GET | /api/formulas?keyword=&page=&pageSize= | Required | 验方列表 (分页) |
| GET | /api/formulas/{id} | Required | 验方详情 |
| POST | /api/formulas | Required | 创建验方 |
| PUT | /api/formulas/{id} | Required | 更新验方 |
| DELETE | /api/formulas/{id} | Required | 软删除验方 |

## 挂号端点

| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| GET | /api/registrations?date=&page=&pageSize= | Required | 挂号列表 |
| GET | /api/registrations/{id} | Required | 挂号详情 |
| POST | /api/registrations | Required | 创建挂号 |
| PUT | /api/registrations/{id} | Required | 更新挂号 |
| DELETE | /api/registrations/{id} | Required | 软删除挂号 |

## 医案端点

| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| GET | /api/medicalcases?patientId=&page=&pageSize= | Required | 医案列表 (分页) |
| GET | /api/medicalcases/{id} | Required | 医案详情 (含 Consultations + Prescriptions) |
| POST | /api/medicalcases | Required | 创建医案 |
| PUT | /api/medicalcases/{id} | Required | 更新医案 |
| DELETE | /api/medicalcases/{id} | Required | 软删除医案 |

## 健康检查端点

| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| GET | /api/health | **Anonymous** | 健康检查 |

**响应:**
```json
{ "status": "Healthy", "timestamp": "2026-04-26T...", "database": "Connected" }
```

## 响应格式

LocalWebAPI 控制器直接返回实体/DTO，**不使用** Server WebAPI 的 `ApiResponse<T>` 包装。

**成功:**
```json
{ "Id": "...", "Name": "...", ... }
```

**列表:**
```json
{ "Items": [...], "TotalCount": 100, "CurrentPage": 1, "PageSize": 20 }
```

**错误:**
- 404: 资源不存在
- 400: 参数验证失败
- 401: 未认证
- 500: 服务器错误 (由全局异常处理器捕获)

## JSON 序列化

- 属性名: PascalCase (`PropertyNamingPolicy = null`)
- 大小写不敏感: `PropertyNameCaseInsensitive = true`
- 枚举: 字符串格式

## 与 Server WebAPI 的区别

| 特性 | Server WebAPI | LocalWebAPI |
|------|---------------|-------------|
| 响应包装 | ApiResponse<T> | 直接返回实体 |
| API 版本 | `/api/v{version}/[controller]` | `/api/[controller]` |
| 认证策略 | Policy-based (AdminOnly, DoctorOrAdmin) | 简单 [Authorize] |
| 分页参数 | page, pageSize, keyword | page, pageSize, keyword |
| 批量操作 | 支持 (batch-delete, batch-import 等) | 不支持 (返回 null) |
| 导入/导出 | 支持 (Excel) | 不支持 |
