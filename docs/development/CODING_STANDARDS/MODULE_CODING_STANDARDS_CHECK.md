# 模块编码规范检查报告

基于用户模块的成功实践，本报告检查其他模块是否符合统一的编码规范。

## 一、用户模块编码规范（基准）

### 1. 控制器层规范
```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class UsersController : ControllerBase
```

**关键特征**：
- 统一的API版本控制
- 标准化路由格式
- 全局认证保护
- 继承自ControllerBase

### 2. API路由规范
```csharp
[HttpPost("paged")]  // 分页查询
[HttpPost("add")]    // 新增
[HttpPut("{id}")]    // 更新
[HttpGet("{id}")]    // 根据ID获取
```

### 3. 返回值规范
```csharp
return Ok(ApiResponse<T>.Success(data));
return BadRequest(ApiResponse<T>.Fail(message));
```

### 4. 操作者信息获取
```csharp
private (Guid operatorId, string operatorName, UserRole operatorRole) GetOperator()
```

## 二、各模块规范符合度检查

### 1. 患者模块 (PatientsController) ✅
**符合度**: 90%

**符合项**:
- ✅ API版本控制和路由格式正确
- ✅ 认证保护已启用
- ✅ GetOperator()方法实现一致
- ✅ 返回值使用ApiResponse包装

**不符合项**:
- ❌ 使用标准REST路由 `[HttpPost]` 而非 `[HttpPost("add")]`
- ❌ 缺少分页查询的 `[HttpPost("paged")]` 路由

### 2. 药材模块 (HerbsController) ⚠️
**符合度**: 70%

**符合项**:
- ✅ API版本控制和路由格式正确
- ✅ 认证保护已启用
- ✅ GetOperator()方法实现一致

**不符合项**:
- ❌ 存在本地DTO和共享DTO的混合使用
- ❌ 有临时的映射方法 `MapToSharedDto`
- ❌ 注释表明"Temporary: Keep using local DTOs"

### 3. 医生模块 (DoctorsController) ✅
**符合度**: 95%

**符合项**:
- ✅ API版本控制和路由格式正确
- ✅ 认证保护已启用
- ✅ GetOperator()方法实现一致
- ✅ 使用 `[HttpPost("paged")]` 路由
- ✅ 返回值使用ApiResponse包装

**不符合项**:
- ⚠️ 可能需要检查是否所有方法都遵循相同模式

## 三、发现的问题

### 1. 路由不一致问题
- **用户模块**: `/users/paged`, `/users/add`
- **患者模块**: 直接使用 `POST /patients`
- **医生模块**: `/doctors/paged`

### 2. DTO使用不一致
- **用户模块**: 有本地DTO到共享DTO的映射
- **药材模块**: 临时保留本地DTO，有复杂的映射逻辑
- **患者模块**: 直接使用共享DTO

### 3. 服务层调用模式
需要进一步检查各模块的服务层是否使用统一的接口定义和实现模式。

## 四、建议的统一规范

### 1. 控制器基础结构
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
    
    // GetOperator() 方法
}
```

### 2. 标准API路由
```csharp
[HttpPost("paged")]      // POST /api/v1/xxx/paged - 分页查询
[HttpPost("add")]        // POST /api/v1/xxx/add - 新增
[HttpGet("{id}")]        // GET /api/v1/xxx/{id} - 根据ID获取
[HttpPut("{id}")]        // PUT /api/v1/xxx/{id} - 更新
[HttpPatch("{id}/enable")]  // PATCH /api/v1/xxx/{id}/enable - 启用
[HttpPatch("{id}/disable")] // PATCH /api/v1/xxx/{id}/disable - 禁用
```

### 3. 返回值格式
```csharp
// 成功
return Ok(ApiResponse<T>.Success(data, "操作成功"));

// 失败
return BadRequest(ApiResponse<T>.Fail("错误信息"));

// 未找到
return NotFound(ApiResponse<T>.Fail("资源不存在"));
```

## 五、修复优先级

1. **高优先级**：
   - 统一所有模块的路由命名（如统一使用 `/paged` 和 `/add`）
   - 完成药材模块的DTO迁移

2. **中优先级**：
   - 确保所有模块使用相同的返回值包装格式
   - 检查并统一服务层接口定义

3. **低优先级**：
   - 添加缺失的XML文档注释
   - 统一日志记录模式

## 六、下一步行动

1. 先修复路由不一致的问题
2. 完成药材模块的DTO统一
3. 创建控制器基类，提取公共方法
4. 逐个模块进行代码审查和修复