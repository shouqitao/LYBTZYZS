# LYBT 系统编码标准规范

本文档定义了LYBT中医诊所管理系统的编码标准，所有开发和修改必须严格遵循这些规范。

## 一、控制器层规范

### 1.1 基础结构
```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class XxxController : ControllerBase
{
    private readonly IXxxService _service;
    private readonly IMemoryCache _cache;
    private readonly ILogger<XxxController> _logger;
    
    public XxxController(
        IXxxService service, 
        IMemoryCache cache,
        ILogger<XxxController> logger)
    {
        _service = service;
        _cache = cache;
        _logger = logger;
    }
    
    // 必须包含的辅助方法
    private (Guid operatorId, string operatorName, UserRole operatorRole) GetOperator()
    {
        var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userName = User?.Identity?.Name;
        var roleStr = User?.FindFirst(ClaimTypes.Role)?.Value;

        if (Guid.TryParse(userId, out var opId) && !string.IsNullOrEmpty(userName))
        {
            var role = Enum.TryParse<UserRole>(roleStr, out var parsedRole) ? parsedRole : UserRole.Staff;
            return (opId, userName, role);
        }
        throw new UnauthorizedAccessException("未登录或用户信息无效");
    }
}
```

### 1.2 标准API路由

#### 查询操作
```csharp
// 分页查询（POST方法，支持复杂查询条件）
[HttpPost("paged")]
public async Task<IActionResult> GetPaged([FromBody] XxxPagedQueryDto query)
{
    var (_, _, operatorRole) = GetOperator();
    var result = await _service.GetPagedAsync(query, operatorRole);
    return Ok(ApiResponse<PaginatedResult<XxxDto>>.Success(result));
}

// 根据ID获取
[HttpGet("{id}")]
public async Task<IActionResult> GetById(Guid id)
{
    var result = await _service.GetByIdAsync(id);
    return result != null 
        ? Ok(ApiResponse<XxxDto>.Success(result))
        : NotFound(ApiResponse<XxxDto>.Fail("资源不存在"));
}

// 获取活跃资源列表
[HttpGet("active")]
public async Task<IActionResult> GetActive()
{
    var result = await _service.GetActiveAsync();
    return Ok(ApiResponse<List<XxxDto>>.Success(result));
}
```

#### 创建操作
```csharp
[HttpPost("add")]
public async Task<IActionResult> Add([FromBody] XxxCreateDto dto)
{
    var (operatorId, operatorName, _) = GetOperator();
    try
    {
        var result = await _service.AddAsync(dto, operatorId, operatorName);
        return Ok(ApiResponse<XxxDto>.Success(result, "创建成功"));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "创建失败");
        return BadRequest(ApiResponse<object>.Fail($"创建失败：{ex.Message}"));
    }
}
```

#### 更新操作
```csharp
[HttpPut("{id}")]
public async Task<IActionResult> Update(Guid id, [FromBody] XxxUpdateDto dto)
{
    var (operatorId, operatorName, _) = GetOperator();
    dto.Id = id; // 确保ID一致
    try
    {
        var result = await _service.UpdateAsync(dto, operatorId, operatorName);
        return Ok(ApiResponse<XxxDto>.Success(result, "更新成功"));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "更新失败");
        return BadRequest(ApiResponse<object>.Fail($"更新失败：{ex.Message}"));
    }
}
```

#### 软删除操作（禁用/启用）
```csharp
[HttpPatch("{id}/enable")]
public async Task<IActionResult> Enable(Guid id)
{
    var (operatorId, operatorName, _) = GetOperator();
    var result = await _service.EnableAsync(id, operatorId, operatorName);
    return result 
        ? Ok(ApiResponse<object>.Success(null, "启用成功"))
        : NotFound(ApiResponse<object>.Fail("资源不存在"));
}

[HttpPatch("{id}/disable")]
public async Task<IActionResult> Disable(Guid id)
{
    var (operatorId, operatorName, _) = GetOperator();
    var result = await _service.DisableAsync(id, operatorId, operatorName);
    return result 
        ? Ok(ApiResponse<object>.Success(null, "禁用成功"))
        : NotFound(ApiResponse<object>.Fail("资源不存在"));
}
```

#### 批量操作
```csharp
[HttpPatch("batch-enable")]
public async Task<IActionResult> BatchEnable([FromBody] BatchOperationDto dto)
{
    var (operatorId, operatorName, _) = GetOperator();
    var result = await _service.BatchEnableAsync(dto.Ids, operatorId, operatorName);
    return Ok(ApiResponse<int>.Success(result, $"成功启用 {result} 条记录"));
}

[HttpPatch("batch-disable")]
public async Task<IActionResult> BatchDisable([FromBody] BatchOperationDto dto)
{
    var (operatorId, operatorName, _) = GetOperator();
    var result = await _service.BatchDisableAsync(dto.Ids, operatorId, operatorName);
    return Ok(ApiResponse<int>.Success(result, $"成功禁用 {result} 条记录"));
}
```

### 1.3 禁止使用的操作
```csharp
// ❌ 禁止使用 DELETE 方法
// ❌ 禁止硬删除数据
// ❌ 禁止不带认证的接口（除了登录接口）
```

## 二、返回值规范

### 2.1 统一返回格式
所有API必须返回 `ApiResponse<T>` 格式：

```csharp
// 成功返回
return Ok(ApiResponse<T>.Success(data, "操作成功"));

// 失败返回
return BadRequest(ApiResponse<T>.Fail("错误信息"));

// 未找到
return NotFound(ApiResponse<T>.Fail("资源不存在"));

// 未授权
return Unauthorized(ApiResponse<T>.Fail("未授权"));
```

### 2.2 ApiResponse 结构
```json
{
    "success": true,
    "statusCode": 200,
    "message": "操作成功",
    "data": { },
    "timestamp": "2025-08-02T00:00:00Z"
}
```

## 三、服务层规范

### 3.1 接口定义
```csharp
public interface IXxxService
{
    // 查询
    Task<PaginatedResult<XxxDto>> GetPagedAsync(XxxPagedQueryDto query, UserRole operatorRole);
    Task<XxxDto?> GetByIdAsync(Guid id);
    Task<List<XxxDto>> GetActiveAsync();
    
    // 创建
    Task<XxxDto> AddAsync(XxxCreateDto dto, Guid operatorId, string operatorName);
    
    // 更新
    Task<XxxDto> UpdateAsync(XxxUpdateDto dto, Guid operatorId, string operatorName);
    
    // 软删除
    Task<bool> EnableAsync(Guid id, Guid operatorId, string operatorName);
    Task<bool> DisableAsync(Guid id, Guid operatorId, string operatorName);
    
    // 批量操作
    Task<int> BatchEnableAsync(List<Guid> ids, Guid operatorId, string operatorName);
    Task<int> BatchDisableAsync(List<Guid> ids, Guid operatorId, string operatorName);
}
```

### 3.2 服务实现规范
- 所有方法必须是异步的（async/await）
- 必须记录操作日志
- 必须进行数据验证
- 必须处理并发冲突

## 四、DTO规范

### 4.1 DTO分层
```
LYBT.Shared.Models.Contracts.Xxx/
├── XxxDto.cs              // 基础DTO
├── XxxDetailDto.cs        // 详情DTO
├── XxxCreateDto.cs        // 创建DTO
├── XxxUpdateDto.cs        // 更新DTO
├── XxxPagedQueryDto.cs    // 分页查询DTO
└── XxxBatchOperationDto.cs // 批量操作DTO
```

### 4.2 DTO验证
```csharp
public class XxxCreateDto
{
    [Required(ErrorMessage = "名称不能为空")]
    [StringLength(50, ErrorMessage = "名称长度不能超过50个字符")]
    public string Name { get; set; } = string.Empty;
    
    [Range(0, double.MaxValue, ErrorMessage = "价格必须大于0")]
    public decimal Price { get; set; }
    
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    public string? Email { get; set; }
}
```

## 五、实体模型规范

### 5.1 基础实体
```csharp
public class XxxEntity : BaseEntity
{
    [Column("Id")]
    public Guid Id { get; set; }
    
    [Column("Name")]
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;
    
    [Column("IsActive")]
    public bool IsActive { get; set; } = true;
    
    [Column("CreateTime")]
    public DateTime CreateTime { get; set; }
    
    [Column("UpdateTime")]
    public DateTime? UpdateTime { get; set; }
    
    [Column("CreatedBy")]
    public string CreatedBy { get; set; } = string.Empty;
    
    [Column("UpdatedBy")]
    public string? UpdatedBy { get; set; }
}
```

## 六、数据库规范

### 6.1 命名规范
- 表名：复数形式（Users, Patients, Herbs）
- 字段名：PascalCase（UserName, CreateTime）
- 主键：Id (GUID类型)
- 外键：XxxId（如：PatientId, DoctorId）

### 6.2 必须字段
- Id：主键
- IsActive：软删除标记
- CreateTime：创建时间
- UpdateTime：更新时间
- CreatedBy：创建人
- UpdatedBy：更新人

## 七、日志规范

### 7.1 日志级别
```csharp
_logger.LogInformation("操作成功：{Operation}", operationName);
_logger.LogWarning("警告信息：{Warning}", warningMessage);
_logger.LogError(ex, "操作失败：{Operation}", operationName);
```

### 7.2 审计日志
所有数据变更必须记录审计日志，包括：
- 操作类型（创建/更新/删除）
- 操作时间
- 操作人
- 变更前后的数据

## 八、异常处理规范

### 8.1 业务异常
```csharp
public class BusinessException : Exception
{
    public BusinessException(string message) : base(message) { }
}
```

### 8.2 全局异常处理
通过 `GlobalExceptionMiddleware` 统一处理异常，返回标准格式。

## 九、缓存规范

### 9.1 缓存键命名
```csharp
public static class CacheKeys
{
    public const string UserById = "user:id:{0}";
    public const string UserList = "user:list";
    public const string HerbById = "herb:id:{0}";
}
```

### 9.2 缓存时间
- 实体数据：5分钟
- 列表数据：2分钟
- 配置数据：30分钟

## 十、测试规范

### 10.1 单元测试
- 覆盖所有公共方法
- 测试正常和异常情况
- Mock外部依赖

### 10.2 集成测试
- 测试完整的API流程
- 验证数据持久化
- 检查权限控制

---

**重要提醒**：
1. 所有新代码必须遵循此规范
2. 修改现有代码时必须将其改造为符合规范
3. 代码审查时必须检查规范符合度
4. 定期更新此文档以反映最佳实践