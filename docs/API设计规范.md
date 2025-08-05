# API 设计规范

## 概述

本文档定义了凌隐宝堂中医诊所管理系统的 RESTful API 设计规范。系统已从自定义的 `ApiResponse<T>` 包装器迁移到标准的 RESTful 响应格式。

## 核心原则

1. **遵循 RESTful 设计原则**
2. **使用标准 HTTP 状态码**
3. **使用 RFC 7807 Problem Details 进行错误响应**
4. **保持接口简洁和一致性**

## 响应格式

### 成功响应

成功响应直接返回数据，不需要额外的包装：

```json
// GET /api/v1/users/123
// Status: 200 OK
{
  "id": "123",
  "username": "zhangsan",
  "realName": "张三",
  "role": "Doctor",
  "isActive": true
}
```

### 错误响应

错误响应使用 RFC 7807 Problem Details 格式：

```json
// POST /api/v1/users
// Status: 400 Bad Request
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "参数验证失败",
  "status": 400,
  "detail": "用户名已存在",
  "instance": "/api/v1/users"
}
```

## HTTP 状态码使用

### 成功状态码
- `200 OK` - 请求成功（GET、PUT、PATCH）
- `201 Created` - 资源创建成功（POST）
- `204 No Content` - 请求成功但无返回内容（DELETE）

### 客户端错误状态码
- `400 Bad Request` - 请求参数错误
- `401 Unauthorized` - 未认证
- `403 Forbidden` - 无权限
- `404 Not Found` - 资源不存在
- `409 Conflict` - 资源冲突（如重复的用户名）
- `422 Unprocessable Entity` - 语义错误

### 服务器错误状态码
- `500 Internal Server Error` - 服务器内部错误
- `502 Bad Gateway` - 网关错误
- `503 Service Unavailable` - 服务不可用

## API 端点设计

### RESTful 端点

```
GET    /api/v1/resources          # 获取资源列表
GET    /api/v1/resources/{id}     # 获取单个资源
POST   /api/v1/resources          # 创建新资源
PUT    /api/v1/resources/{id}     # 更新资源（完整更新）
PATCH  /api/v1/resources/{id}     # 更新资源（部分更新）
DELETE /api/v1/resources/{id}     # 删除资源
```

### 特殊操作端点

```
POST   /api/v1/resources/{id}/action    # 对资源执行特定操作
PATCH  /api/v1/resources/{id}/enable    # 启用资源
PATCH  /api/v1/resources/{id}/disable   # 禁用资源
PATCH  /api/v1/resources/{id}/toggle-status  # 切换状态
```

### 批量操作端点

```
PATCH  /api/v1/resources/batch-enable   # 批量启用
PATCH  /api/v1/resources/batch-disable  # 批量禁用
POST   /api/v1/resources/import         # 批量导入
GET    /api/v1/resources/export         # 批量导出
```

## 分页规范

### 请求参数

```
GET /api/v1/resources?page=1&pageSize=20&keyword=search
```

### 响应格式

```json
{
  "totalCount": 100,
  "items": [...],
  "currentPage": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

## 前端集成 (Refit)

### 接口定义

```csharp
public interface IResourceApiService
{
    [Get("/api/v1/resources")]
    Task<Refit.ApiResponse<PaginatedResult<ResourceDto>>> GetResourcesAsync(
        [Query] int page = 1,
        [Query] int pageSize = 20);

    [Get("/api/v1/resources/{id}")]
    Task<Refit.ApiResponse<ResourceDto>> GetResourceAsync(Guid id);

    [Post("/api/v1/resources")]
    Task<Refit.ApiResponse<ResourceDto>> CreateResourceAsync([Body] ResourceCreateDto dto);

    [Put("/api/v1/resources/{id}")]
    Task<Refit.ApiResponse<object>> UpdateResourceAsync(Guid id, [Body] ResourceUpdateDto dto);

    [Delete("/api/v1/resources/{id}")]
    Task<Refit.ApiResponse<object>> DeleteResourceAsync(Guid id);
}
```

### 错误处理

```csharp
try 
{
    var response = await apiService.GetResourceAsync(id);
    if (response.IsSuccessStatusCode)
    {
        var resource = response.Content;
        // 处理成功响应
    }
}
catch (Refit.ApiException ex)
{
    if (ex.StatusCode == HttpStatusCode.NotFound)
    {
        // 处理404错误
    }
    else if (ex.HasContent)
    {
        var problemDetails = await ex.GetContentAsAsync<ProblemDetails>();
        // 显示错误详情
    }
}
```

## 全局异常处理

系统使用 `GlobalExceptionHandler` 统一处理异常：

1. **ValidationException** → 400 Bad Request
2. **NotFoundException** → 404 Not Found
3. **BusinessException** → 400 Bad Request
4. **UnauthorizedAccessException** → 401 Unauthorized
5. **其他异常** → 500 Internal Server Error

## 软删除策略

系统采用软删除策略，不提供真正的 DELETE 操作：

- 使用 `PATCH /resources/{id}/disable` 禁用资源
- 使用 `PATCH /resources/{id}/enable` 启用资源
- 使用 `IsActive` 字段标记资源状态

## 版本控制

API 使用 URL 路径版本控制：

```
/api/v1/resources
/api/v2/resources
```

支持的版本读取方式：
- URL 路径：`/api/v1/resources`
- 查询字符串：`/api/resources?version=1.0`
- HTTP 头：`X-Version: 1.0`

## 安全考虑

1. **认证**：使用 JWT Bearer Token
2. **授权**：基于角色的访问控制（RBAC）
3. **输入验证**：使用 Data Annotations 和 FluentValidation
4. **防止过度提交**：使用 DTO 而非直接暴露实体
5. **审计日志**：记录所有修改操作

## 迁移指南

### 后端迁移

1. 移除 `ApiResponse<T>` 包装器
2. 更新控制器方法返回类型为 `ActionResult<T>` 或 `IActionResult`
3. 使用 `ProblemDetails` 替代自定义错误响应
4. 配置全局异常处理中间件

### 前端迁移

1. 更新 API 服务接口使用 `Refit.ApiResponse<T>`
2. 移除对 `LYBT.Shared.Models.Common.ApiResponse` 的依赖
3. 更新错误处理逻辑以处理 `ProblemDetails`
4. 配置 Refit 的 Polly 重试策略

## 示例

### 用户登录

```http
POST /api/v1/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "password",
  "rememberMe": false
}
```

成功响应：
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "user": {
    "id": "123",
    "username": "admin",
    "realName": "管理员",
    "role": "Admin"
  }
}
```

失败响应：
```http
HTTP/1.1 401 Unauthorized
Content-Type: application/problem+json

{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "认证失败",
  "status": 401,
  "detail": "用户名或密码错误"
}
```

## 最佳实践

1. **使用有意义的 HTTP 状态码**
2. **提供清晰的错误消息**
3. **保持 API 的一致性**
4. **使用标准的日期时间格式（ISO 8601）**
5. **避免在 URL 中暴露敏感信息**
6. **使用复数形式的资源名称**
7. **为集合资源提供过滤、排序和分页功能**
8. **使用 HATEOAS 原则提供相关资源链接**（可选）

## 参考资料

- [RFC 7807 - Problem Details for HTTP APIs](https://tools.ietf.org/html/rfc7807)
- [REST API Design Best Practices](https://docs.microsoft.com/en-us/azure/architecture/best-practices/api-design)
- [HTTP Status Codes](https://httpstatuses.com/)
- [Refit Documentation](https://github.com/reactiveui/refit)