# UltraThink控制器设计模式 - 统一架构标准

**文档版本**: v1.0  
**创建日期**: 2025-08-17  
**最后更新**: 2025-08-17  
**架构师**: UltraThink AI System  

## 📋 概述

本文档定义了LYBT系统中控制器的统一设计模式和架构标准，确保所有开发者遵循一致的代码规范，提升系统的可维护性和扩展性。

## 🏗️ 三层控制器架构体系

### 架构层次图

```
BaseControllerCore (核心基础层)
    ├── BaseApiController (业务API层)
    │   ├── AuthController
    │   ├── UsersController
    │   ├── PatientsController
    │   ├── ConsultationController
    │   ├── MedicalCaseController
    │   ├── PrescriptionsController
    │   ├── HerbsController
    │   ├── FormulasController
    │   └── HerbImportExportController
    └── BaseSystemController (系统管理层)
        ├── HealthController
        ├── MonitoringController
        ├── SecurityController
        ├── CacheController
        └── PerformanceController
```

## 🔧 核心基类详解

### 1. BaseControllerCore - 控制器核心基类

**位置**: `src/Server/Core/LYBT.Infrastructure/Web/BaseControllerCore.cs`

**职责**: 
- 提供所有控制器共享的核心功能
- 统一操作者信息获取
- 统一日志记录
- 基础验证方法
- 请求链路追踪

**核心方法**:
```csharp
// 获取当前操作者信息
protected (Guid operatorId, string operatorName, string operatorRole) GetOperator()

// 统一日志记录
protected void LogOperation(string operation, object? data = null, Guid? targetId = null)

// 核心异常处理
protected void HandleExceptionCore(Exception ex, string operation, object? context = null)

// 获取请求ID（链路追踪）
protected string GetRequestId()

// 模型验证
protected List<string> GetModelErrors()
protected bool IsValidGuid(Guid id)
```

### 2. BaseApiController - 业务API基类

**位置**: `src/Server/Core/LYBT.Infrastructure/Web/BaseApiController.cs`

**职责**:
- 业务API的标准化响应处理
- ServiceResult<T>自动解包
- 统一的API响应格式
- 业务异常处理

**核心特性**:
- 继承自 `BaseControllerCore`
- 使用 `ApiResponse<T>` 统一响应格式
- 自动处理 `ServiceResult<T>` 包装和解包
- 支持分页响应 `PagedApiResponse<T>`

**标准响应方法**:
```csharp
// 成功响应
protected ActionResult<ApiResponse<T>> Success<T>(T data, string message = "操作成功")

// 业务失败响应
protected ActionResult<ApiResponse<T>> BusinessFail<T>(string message)

// 验证失败响应
protected ActionResult<ApiResponse<T>> ValidationFail<T>(string message)

// ServiceResult自动处理
protected ActionResult<ApiResponse<T>> HandleServiceResult<T>(ServiceResult<T> serviceResult, string? successMessage = null)

// 统一异常处理
protected ActionResult<ApiResponse<T>> HandleException<T>(Exception ex, string operation, object? context = null)
```

### 3. BaseSystemController - 系统管理基类

**位置**: `src/Server/Core/LYBT.Infrastructure/Web/BaseSystemController.cs`

**职责**:
- 系统管理功能的简化响应处理
- 健康检查、监控、性能等系统级功能
- 简化的响应格式（非ApiResponse<T>）

**核心特性**:
- 继承自 `BaseControllerCore`
- 使用简化的响应格式
- 系统级异常处理
- 管理员权限验证

**系统响应方法**:
```csharp
// 系统正常响应
protected IActionResult SystemOk(object data, string message = "系统正常")
protected IActionResult SystemOk(string message = "系统正常")

// 系统错误响应
protected IActionResult SystemError(string message, int statusCode = 500)

// 系统警告响应
protected IActionResult SystemWarning(object data, string message)

// 系统异常处理
protected IActionResult HandleSystemException(Exception ex, string operation, object? context = null)
```

## 📝 控制器开发规范

### 1. 业务API控制器规范

**适用场景**: 
- 所有业务功能API（8个核心模块）
- 需要统一响应格式的功能
- 面向前端应用的接口

**必须遵循**:
```csharp
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class ExampleController : BaseApiController
{
    public ExampleController(IExampleService service, ILogger<ExampleController> logger, IMemoryCache cache)
        : base(logger, cache)
    {
        _service = service;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ExampleDto>>> GetById(Guid id)
    {
        try
        {
            var validation = ValidateGuid<ExampleDto>(id, "示例ID");
            if (validation != null) return validation;

            var result = await _service.GetByIdAsync(id);
            return HandleServiceResult(result, "查询成功");
        }
        catch (Exception ex)
        {
            return HandleException<ExampleDto>(ex, "获取示例详情", id);
        }
    }
}
```

### 2. 系统管理控制器规范

**适用场景**:
- 健康检查、监控、性能管理
- 系统配置和管理功能
- 运维相关接口

**必须遵循**:
```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "Admin")] // 通常需要管理员权限
public class ExampleSystemController : BaseSystemController
{
    public ExampleSystemController(ILogger<ExampleSystemController> logger)
        : base(logger)
    {
    }

    [HttpGet]
    public async Task<IActionResult> GetSystemStatus()
    {
        try
        {
            var status = await GetSystemInfo();
            return SystemOk(status, "系统状态正常");
        }
        catch (Exception ex)
        {
            return HandleSystemException(ex, "获取系统状态");
        }
    }
}
```

## 🔄 ServiceResult模式

### ServiceResult<T> 处理标准

业务API控制器必须使用ServiceResult模式：

```csharp
// 服务层返回 ServiceResult<T>
var serviceResult = await _service.GetDataAsync(id);

// 控制器层自动处理
return HandleServiceResult(serviceResult, "查询成功");

// 自动转换为 ApiResponse<T> 格式
{
    "success": true,
    "message": "查询成功",
    "data": { ... },
    "timestamp": "2025-08-17T10:30:00Z",
    "requestId": "req-123456"
}
```

## ⚠️ 异常处理模式

### 1. 业务API异常处理

```csharp
try
{
    // 业务逻辑
    var result = await _service.ProcessAsync(data);
    return HandleServiceResult(result, "处理成功");
}
catch (ArgumentException ex)
{
    // 参数异常 -> 400 BadRequest
    return ValidationFail<T>(ex.Message);
}
catch (UnauthorizedAccessException ex)
{
    // 权限异常 -> 401 Unauthorized
    return Unauthorized(ex.Message);
}
catch (Exception ex)
{
    // 其他异常 -> 统一处理
    return HandleException<T>(ex, "处理数据", data);
}
```

### 2. 系统管理异常处理

```csharp
try
{
    // 系统操作
    var systemData = await GetSystemDataAsync();
    return SystemOk(systemData, "获取成功");
}
catch (Exception ex)
{
    // 统一系统异常处理
    return HandleSystemException(ex, "获取系统数据");
}
```

## 📊 响应格式标准

### 1. 业务API响应格式 (ApiResponse<T>)

```json
{
    "success": true,
    "message": "操作成功",
    "data": { "id": "123", "name": "示例" },
    "errors": null,
    "timestamp": "2025-08-17T10:30:00Z",
    "requestId": "req-123456"
}
```

### 2. 分页响应格式 (PagedApiResponse<T>)

```json
{
    "success": true,
    "message": "查询成功",
    "data": {
        "items": [{ "id": "123", "name": "示例" }],
        "totalCount": 100,
        "currentPage": 1,
        "pageSize": 20,
        "totalPages": 5
    },
    "timestamp": "2025-08-17T10:30:00Z",
    "requestId": "req-123456"
}
```

### 3. 系统管理响应格式

```json
{
    "success": true,
    "message": "系统正常",
    "data": { "status": "healthy", "uptime": "2 days" },
    "timestamp": 1692261000,
    "requestId": "req-123456"
}
```

## 🛡️ 安全性规范

### 1. 认证授权

```csharp
[Authorize] // 基础认证
[Authorize(Roles = "Admin")] // 角色授权
[Authorize(Policy = "RequireSpecialPermission")] // 策略授权
[AllowAnonymous] // 匿名访问（仅限健康检查等）
```

### 2. 参数验证

```csharp
// GUID验证
var validation = ValidateGuid<T>(id, "资源ID");
if (validation != null) return validation;

// 模型验证
var modelValidation = ValidateModel<T>();
if (modelValidation != null) return modelValidation;

// 自定义验证
if (string.IsNullOrWhiteSpace(keyword))
    return ValidationFail<T>("搜索关键词不能为空");
```

## 📈 性能优化模式

### 1. 缓存策略

```csharp
public ExampleController(IExampleService service, ILogger<ExampleController> logger, IMemoryCache cache)
    : base(logger, cache)
{
    _cache = cache; // 基类提供缓存支持
}

// 使用缓存
private async Task<T> GetCachedDataAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null)
{
    if (_cache?.TryGetValue(key, out var cached) == true && cached is T result)
    {
        return result;
    }

    var data = await factory();
    _cache?.Set(key, data, expiry ?? TimeSpan.FromMinutes(10));
    return data;
}
```

### 2. 异步模式

```csharp
// 所有API方法必须是异步的
public async Task<ActionResult<ApiResponse<T>>> GetAsync()

// 避免同步调用
// ❌ var result = _service.GetData();
// ✅ var result = await _service.GetDataAsync();
```

## 🔍 调试和监控

### 1. 日志记录

```csharp
// 自动记录操作日志
LogOperation("创建用户", createDto, createdUser.Id);

// 记录关键业务操作
LogOperation("用户登录", new { Username = request.Username }, userId);
```

### 2. 链路追踪

```csharp
// 自动生成RequestId
var requestId = GetRequestId(); // 基类提供
// RequestId 自动包含在响应中
```

## ✅ 开发检查清单

### 新建业务API控制器时

- [ ] 继承 `BaseApiController`
- [ ] 添加正确的 `[ApiVersion]` 和路由配置
- [ ] 使用 `HandleServiceResult` 处理服务结果
- [ ] 使用 `HandleException` 处理异常
- [ ] 添加适当的 `[Authorize]` 配置
- [ ] 参数验证使用基类方法
- [ ] 记录关键操作日志

### 新建系统管理控制器时

- [ ] 继承 `BaseSystemController`
- [ ] 使用 `SystemOk/SystemError` 响应方法
- [ ] 使用 `HandleSystemException` 处理异常
- [ ] 添加管理员权限检查
- [ ] 返回类型使用 `IActionResult`

## 🚫 反模式警告

### 避免的做法

1. **直接继承 ControllerBase**
   ```csharp
   // ❌ 错误
   public class BadController : ControllerBase
   
   // ✅ 正确
   public class GoodController : BaseApiController
   ```

2. **混合使用响应格式**
   ```csharp
   // ❌ 在业务API中直接返回 Ok()
   return Ok(data);
   
   // ✅ 使用标准方法
   return Success(data, "操作成功");
   ```

3. **忽略异常处理**
   ```csharp
   // ❌ 不处理异常
   public async Task<ActionResult> BadMethod()
   {
       var result = await _service.ProcessAsync();
       return Ok(result);
   }
   
   // ✅ 标准异常处理
   public async Task<ActionResult<ApiResponse<T>>> GoodMethod()
   {
       try
       {
           var result = await _service.ProcessAsync();
           return HandleServiceResult(result, "处理成功");
       }
       catch (Exception ex)
       {
           return HandleException<T>(ex, "处理数据");
       }
   }
   ```

---

## 📚 相关文档

- [API响应标准文档](./ultrathink-api-response-standards-20250817.md)
- [控制器开发模板](../templates/controller-templates-20250817.md)
- [最佳实践指南](../guides/controller-best-practices-20250817.md)
- [开发规范总览](../development/DEVELOPMENT_STANDARDS.md)

---

**维护说明**: 本文档应随着系统架构的演进而更新，确保始终反映最新的设计模式和最佳实践。