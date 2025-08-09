# API响应标准化重构指南

> 版本：1.0  
> 更新：2025-01-08  
> 目标：将现有API控制器重构为统一的响应格式，实现前后端契约标准化

## 🎯 重构目标

### 统一响应格式
- 所有API统一使用`ApiResponse<T>`包装响应数据
- 标准化错误处理和错误代码
- 统一分页响应格式
- 规范化状态码使用

### 提升可维护性
- 减少响应格式不一致导致的前端适配成本
- 建立统一的错误处理机制
- 标准化操作日志记录
- 提高API可预测性

---

## 🏗️ 重构步骤

### 1. 继承BaseApiController
```csharp
// 重构前
public class UsersController : BaseController
{
    // ...
}

// 重构后
public class UsersController : BaseApiController
{
    public UsersController(
        IUserService userService,
        IMemoryCache cache,
        ILogger<UsersController> logger)
        : base(logger, cache)
    {
        _userService = userService;
    }
}
```

### 2. 统一成功响应格式

#### 单个资源响应
```csharp
// 重构前
[HttpGet("{id}")]
public async Task<ActionResult<UserDto>> GetById(Guid id)
{
    var user = await _userService.GetByIdAsync(id);
    if (user == null)
        return NotFound();
    return Ok(user);
}

// 重构后
[HttpGet("{id}")]
public async Task<ActionResult<ApiResponse<UserDto>>> GetById(Guid id)
{
    try
    {
        var validation = ValidateGuid(id, "用户ID");
        if (validation != null) return validation;

        var user = await _userService.GetByIdAsync(id);
        if (user == null)
            return NotFound("用户不存在", ApiErrorCodes.USER_NOT_FOUND);

        return Success(user, "查询成功");
    }
    catch (Exception ex)
    {
        return HandleException(ex, "查询用户", id);
    }
}
```

#### 分页响应
```csharp
// 重构前
[HttpGet]
public async Task<ActionResult<PaginatedResult<UserDto>>> GetPaged([FromQuery] UserPagedQueryDto query)
{
    var result = await _userService.GetPagedAsync(query);
    return Ok(result);
}

// 重构后
[HttpGet]
public async Task<ActionResult<PagedApiResponse<UserDto>>> GetPaged([FromQuery] UserPagedQueryDto query)
{
    try
    {
        var validation = ValidateModel();
        if (validation != null) return validation;

        var result = await _userService.GetPagedAsync(query);
        return Success(result, "查询成功");
    }
    catch (Exception ex)
    {
        return HandleException(ex, "分页查询用户", query);
    }
}
```

#### 创建操作响应
```csharp
// 重构前
[HttpPost]
public async Task<IActionResult> Create([FromBody] UserCreateDto dto)
{
    var user = await _userService.AddAsync(dto, operatorId, operatorName);
    if (user != null)
        return Ok(user);
    return BadRequest("创建失败");
}

// 重构后
[HttpPost]
public async Task<ActionResult<ApiResponse<UserDto>>> Create([FromBody] UserCreateDto dto)
{
    try
    {
        var validation = ValidateModel();
        if (validation != null) return validation;

        var (operatorId, operatorName, _) = GetOperator();
        var user = await _userService.AddAsync(dto, operatorId, operatorName);
        
        if (user == null)
            return BusinessFail<UserDto>("创建失败", ApiErrorCodes.DATA_SAVE_FAILED);

        LogOperation("创建用户", user, user.Id);
        return Success(user, "创建成功");
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("已存在"))
    {
        return BusinessFail<UserDto>(ex.Message, ApiErrorCodes.USERNAME_EXISTS);
    }
    catch (Exception ex)
    {
        return HandleException(ex, "创建用户", dto);
    }
}
```

#### 状态操作响应
```csharp
// 重构前
[HttpPatch("{id}/toggle-status")]
public async Task<IActionResult> ToggleStatus(Guid id)
{
    // ... 业务逻辑
    if (result)
        return Ok(new { message });
    return BadRequest(new ProblemDetails { ... });
}

// 重构后
[HttpPatch("{id}/status")]
public async Task<ActionResult<ApiResponse>> ToggleStatus(Guid id)
{
    try
    {
        var validation = ValidateGuid(id, "用户ID");
        if (validation != null) return validation;

        // ... 业务逻辑
        if (!result)
            return BusinessFail("状态切换失败", ApiErrorCodes.DATA_UPDATE_FAILED);

        LogOperation(message, null, id);
        return Success(message);
    }
    catch (Exception ex)
    {
        return HandleException(ex, "切换状态", id);
    }
}
```

### 3. 统一错误处理

#### 验证错误
```csharp
// 重构前
if (!ModelState.IsValid)
{
    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
    return BadRequest(new ProblemDetails
    {
        Title = "参数验证失败",
        Detail = string.Join("; ", errors)
    });
}

// 重构后
var validation = ValidateModel();
if (validation != null) return validation;
```

#### 业务错误
```csharp
// 重构前
if (await _userService.ExistsByUsernameAsync(dto.Username))
{
    return BadRequest("用户名已存在");
}

// 重构后
try
{
    // 业务逻辑
}
catch (InvalidOperationException ex) when (ex.Message.Contains("用户名已存在"))
{
    return BusinessFail<UserDto>(ex.Message, ApiErrorCodes.USERNAME_EXISTS);
}
```

#### 资源未找到
```csharp
// 重构前
if (user == null)
    return NotFound();

// 重构后
if (user == null)
    return NotFound("用户不存在", ApiErrorCodes.USER_NOT_FOUND);
```

### 4. 规范化HTTP状态码使用

| 场景 | 旧方式 | 新方式 | 说明 |
|------|--------|--------|------|
| 业务成功 | `Ok(data)` | `Success(data, message)` | 统一包装格式 |
| 业务失败 | `BadRequest(message)` | `BusinessFail(message, errorCode)` | 业务失败仍返回200，通过success字段区分 |
| 验证失败 | `BadRequest(errors)` | `ValidationFail(message)` | 返回400状态码 |
| 资源未找到 | `NotFound()` | `NotFound(message, errorCode)` | 返回404状态码 |
| 服务器错误 | `StatusCode(500, ...)` | `InternalError(message, errorCode)` | 返回500状态码 |

---

## 📊 响应格式对比

### 成功响应对比
```json
// 重构前
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "username": "testuser",
  "realName": "测试用户"
}

// 重构后
{
  "success": true,
  "data": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "username": "testuser",
    "realName": "测试用户"
  },
  "message": "查询成功",
  "timestamp": 1641859200000,
  "requestId": "req_123456789"
}
```

### 错误响应对比
```json
// 重构前
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "参数验证失败",
  "status": 400,
  "detail": "用户名不能为空"
}

// 重构后
{
  "success": false,
  "data": null,
  "message": "用户名不能为空",
  "errorCode": "VALIDATION_ERROR",
  "timestamp": 1641859200000,
  "requestId": "req_123456789"
}
```

### 分页响应对比
```json
// 重构前
{
  "items": [...],
  "totalCount": 100,
  "currentPage": 1,
  "pageSize": 20
}

// 重构后
{
  "success": true,
  "data": {
    "items": [...],
    "totalCount": 100,
    "currentPage": 1,
    "pageSize": 20,
    "totalPages": 5,
    "hasNext": true,
    "hasPrevious": false
  },
  "message": "查询成功",
  "timestamp": 1641859200000,
  "requestId": "req_123456789"
}
```

---

## 🛠️ 重构工具和辅助方法

### BaseApiController提供的方法
```csharp
// 成功响应
Success<T>(data, message)           // 带数据的成功响应
Success(message)                    // 无数据的成功响应
Success<T>(pagedResult, message)    // 分页成功响应

// 业务错误响应（200状态码）
BusinessFail<T>(message, errorCode)
BusinessFail(message, errorCode)

// HTTP错误响应
ValidationFail(message, errorCode)  // 400
Unauthorized(message, errorCode)    // 401
Forbidden(message, errorCode)       // 403
NotFound(message, errorCode)        // 404
InternalError(message, errorCode)   // 500

// 辅助方法
ValidateModel()                     // 模型验证
ValidateGuid(id, paramName)        // GUID验证
HandleException(ex, operation, context) // 异常处理
LogOperation(operation, data, targetId) // 操作日志
GetOperator()                      // 获取操作者信息
```

### 错误代码使用
```csharp
// 使用预定义的错误代码常量
ApiErrorCodes.VALIDATION_ERROR
ApiErrorCodes.USER_NOT_FOUND
ApiErrorCodes.USERNAME_EXISTS
ApiErrorCodes.DATA_SAVE_FAILED
// ... 更多错误代码见ApiErrorCodes类
```

---

## 🔍 重构检查清单

### 控制器层面
- [ ] 继承BaseApiController而不是BaseController
- [ ] 所有方法返回统一的ApiResponse格式
- [ ] 使用标准化的错误处理方法
- [ ] 添加操作日志记录
- [ ] 添加适当的参数验证

### 响应格式
- [ ] 成功响应包装在ApiResponse<T>中
- [ ] 错误响应包含错误代码和消息
- [ ] 分页响应使用PagedApiResponse<T>
- [ ] 包含请求ID用于链路追踪

### 异常处理
- [ ] 使用try-catch包装业务逻辑
- [ ] 区分业务异常和系统异常
- [ ] 返回合适的HTTP状态码
- [ ] 记录详细的错误日志

### 日志记录
- [ ] 重要操作添加操作日志
- [ ] 异常记录详细上下文信息
- [ ] 敏感信息不记录到日志中

---

## 📈 重构收益

### 开发效率提升
- **前端开发**：统一响应格式减少适配代码
- **调试效率**：标准错误代码便于问题定位
- **团队协作**：统一标准减少沟通成本

### 系统可维护性
- **错误处理**：集中化错误处理逻辑
- **日志追踪**：统一的请求ID支持链路追踪
- **监控告警**：标准化错误代码便于监控

### 用户体验
- **错误信息**：友好的中文错误提示
- **响应一致**：前端可预期的响应结构
- **性能优化**：统一的缓存和日志策略

---

*"通过API响应标准化，我们不仅提升了代码质量，更建立了可持续发展的前后端协作模式。"*